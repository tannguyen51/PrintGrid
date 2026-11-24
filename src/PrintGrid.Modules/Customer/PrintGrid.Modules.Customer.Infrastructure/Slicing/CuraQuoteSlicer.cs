using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PrintGrid.Infrastructure.Shared.FileStorage;
using PrintGrid.Infrastructure.Shared.Slicing;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.SharedKernel.Interfaces;
using PrintGrid.SharedKernel.ValueObjects;

namespace PrintGrid.Modules.Customer.Infrastructure.Slicing;

public sealed class CuraQuoteOptions
{
    public string ExecutablePath { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 120;
    public string PaintSlicerExecutablePath { get; init; } = "";
    public int PaintTimeoutSeconds { get; init; } = 240;
    public decimal PaintPurgeVolumeMm3 { get; init; } = 70;
    public decimal PaintColorChangeSeconds { get; init; } = 30;
    public decimal PaintWipeTowerWidthMm { get; init; } = 20;
    public decimal PaintMaxVolumetricSpeed { get; init; } = 12;
    public Dictionary<string, RegisteredCuraSettings> Machines { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed partial class CuraQuoteSlicer(IFileStorage files, IOptions<CuraQuoteOptions> options,
    ILogger<CuraQuoteSlicer> logger, ICuraProfileRegistry registry,
    IOptions<ModelUploadOptions> uploadOptions) : IQuoteSlicer
{
    private static readonly SemaphoreSlim _slots = new(2);
    private ThreeMfPackage.NativeDocument? _nativeDocument;
    private string? _nativeStorageKey;
    public Task<IReadOnlyList<Guid>> GetConfiguredMachineIdsAsync(CancellationToken cancellationToken) =>
        registry.GetApprovedMachineIdsAsync(cancellationToken);
    public async Task<QuoteModelEnvelope?> GetEnvelopeAsync(string storageKey, string format, CancellationToken cancellationToken)
    {
        if (!format.TrimStart('.').Equals("3MF", StringComparison.OrdinalIgnoreCase)) return null;
        var document = await LoadNativeAsync(storageKey, cancellationToken);
        var bounds = PrusaPaintProject.Envelope(document);
        return new(bounds.Width, bounds.Depth, bounds.Height, document.PlateCount);
    }

    private async Task<ThreeMfPackage.NativeDocument> LoadNativeAsync(string storageKey, CancellationToken ct)
    {
        if (_nativeStorageKey == storageKey && _nativeDocument is not null) return _nativeDocument;
        var split = storageKey.IndexOf('/');
        if (split <= 0) throw new InvalidDataException("File model không hợp lệ.");
        await using var source = await files.DownloadAsync(storageKey[..split], storageKey[(split + 1)..], ct);
        using var content = new MemoryStream(); var buffer = new byte[65536]; int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            if (content.Length + read > uploadOptions.Value.MaxFileBytes) throw new InvalidDataException("File vượt giới hạn dung lượng báo giá.");
            content.Write(buffer, 0, read);
        }
        content.Position = 0;
        _nativeDocument = ThreeMfPackage.ReadNative(content); _nativeStorageKey = storageKey;
        return _nativeDocument;
    }
    public async Task<QuoteSliceResult> SliceAsync(QuoteSliceRequest request, CancellationToken cancellationToken)
    {
        var config = options.Value;
        var profile = await registry.ResolveAsync(request.MachineId, request.MaterialCode, cancellationToken, request.ProfileId)
            ?? throw new InvalidOperationException("Máy chưa có profile phù hợp được quản trị viên duyệt.");
        if (request.Colors.Count > profile.MaximumColorSlots)
            throw new InvalidOperationException("Số màu yêu cầu vượt khả năng máy.");
        if (request.MaterialCode.Equals("RESIN", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CuraEngine phục vụ in sợi FDM; resin cần slicer riêng.");
        await _slots.WaitAsync(cancellationToken);
        var directory = Directory.CreateTempSubdirectory("printgrid-quote-");
        try
        {
            var settings = JsonNode.Parse(profile.SettingsJson)?.AsObject()
                ?? throw new InvalidDataException("Profile Cura không hợp lệ.");
            if (settings.Any(p => p.Key != "global" && !p.Key.StartsWith("extruder.") && p.Key != "limit_to_extruder"))
                throw new InvalidDataException("Profile Cura chỉ được chứa global, extruder.N và limit_to_extruder.");
            if (settings["global"] is not JsonObject global) throw new InvalidDataException("Profile thiếu global.");
            if (request.FileFormat.TrimStart('.').Equals("3MF", StringComparison.OrdinalIgnoreCase))
            {
                var native = await LoadNativeAsync(request.StorageKey, cancellationToken);
                if (native.HasSurfacePainting || native.PlateCount > 1)
                    return await PrusaQuoteSlicer.SliceAsync(native, request, profile, settings, config, directory.FullName, logger, cancellationToken);
            }
            if (string.IsNullOrWhiteSpace(config.ExecutablePath))
                throw new InvalidDataException("Chưa cấu hình đường dẫn CuraEngine.");
            var split = request.StorageKey.IndexOf('/');
            if (split <= 0) throw new InvalidDataException("File model không hợp lệ.");
            await using var source = await files.DownloadAsync(request.StorageKey[..split], request.StorageKey[(split + 1)..], cancellationToken);
            using var content = new MemoryStream();
            var buffer = new byte[65536];
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                if (content.Length + read > uploadOptions.Value.MaxFileBytes)
                    throw new InvalidDataException($"File vượt giới hạn {uploadOptions.Value.MaxFileBytes / (1024m * 1024):0.##} MiB.");
                content.Write(buffer, 0, read);
            }
            content.Position = 0;
            // Surface paint is enough for preview, but not a set of closed material
            // volumes. Do not feed open colored surface patches to Cura as solids.
            IReadOnlyList<List<QuoteMesh.Triangle>> groups;
            IReadOnlyList<ColorGroupAssignment>? assignments = null;
            if (request.ColorAssignments is not null)
            {
                if (!request.FileFormat.TrimStart('.').Equals("3MF", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Ánh xạ nhóm màu hiện yêu cầu file 3MF.");
                var sourceGroups = ThreeMfPackage.ReadGroups(content, allowSurfacePainting: request.Colors.Count == 1);
                var mapped = ColorGroupMapping.Apply(sourceGroups, request.ColorAssignments, request.Colors);
                groups = mapped.Groups;
                assignments = mapped.Assignments;
            }
            else groups = QuoteMesh.Read(content, request.FileFormat, allowSurfacePainting: request.Colors.Count == 1);
            if (request.Colors.Count > 1 && groups.Count != request.Colors.Count)
                throw new InvalidDataException("File chưa có đủ vùng vật liệu cho các màu đã chọn. Hãy dùng 3MF với mỗi màu là một vùng vật liệu riêng; hệ thống không tự chia đều nhựa theo số màu.");
            // Do not allocate another six million vertices for a 100 MiB STL.
            var min = new Vector3(float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity);
            foreach (var group in groups)
            foreach (var triangle in group)
            {
                min = Vector3.Min(min, Vector3.Min(triangle.A, Vector3.Min(triangle.B, triangle.C)));
                max = Vector3.Max(max, Vector3.Max(triangle.A, Vector3.Max(triangle.B, triangle.C)));
            }
            var scaled = (max - min) * (float)request.Scale;
            decimal Setting(string name) => decimal.Parse(global[name]?.ToString() ?? throw new InvalidDataException($"Profile thiếu {name}."), CultureInfo.InvariantCulture);
            var bedWidth = Setting("machine_width"); var bedDepth = Setting("machine_depth");
            if (!FitsBuildPlate(global, (decimal)scaled.X, (decimal)scaled.Y, (decimal)scaled.Z))
                throw new InvalidDataException("Model theo hướng hiện tại vượt khổ máy. Hãy đổi hướng file hoặc chọn size nhỏ hơn.");
            var centered = global["machine_center_is_zero"]?.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) == true;
            var offset = new Vector3((centered ? 0 : (float)bedWidth / 2) - (min.X + max.X) * (float)request.Scale / 2,
                (centered ? 0 : (float)bedDepth / 2) - (min.Y + max.Y) * (float)request.Scale / 2, -min.Z * (float)request.Scale);
            ApplyQuoteSettings(global, request.LayerHeightMm, request.InfillPercent);
            global["machine_extruder_count"] = request.Colors.Count;
            foreach (var key in settings.Select(p => p.Key).Where(k => k.StartsWith("extruder.") &&
                int.TryParse(k[9..], out var number) && number >= request.Colors.Count).ToList())
            {
                settings.Remove(key);
            }

            for (var index = 0; index < request.Colors.Count; index++)
            {
                if (settings[$"extruder.{index}"] is not JsonObject extruder)
                    throw new InvalidDataException("Profile máy không hỗ trợ số màu yêu cầu.");
                ApplyQuoteSettings(extruder, request.LayerHeightMm, request.InfillPercent);
                extruder["extruder_nr"] = index;
                var mesh = request.Colors.Count == 1 ? groups.SelectMany(g => g).ToList() : groups[index];
                var name = $"part-{index}.stl";
                QuoteMesh.WriteStl(Path.Combine(directory.FullName, name), mesh, (float)request.Scale, offset);
                settings[name] = new JsonObject { ["extruder_nr"] = index, ["center_object"] = false,
                    ["mesh_position_x"] = 0, ["mesh_position_y"] = 0, ["mesh_position_z"] = 0 };
            }
            var output = Path.Combine(directory.FullName, "quote.gcode");
            var start = new ProcessStartInfo(config.ExecutablePath) { UseShellExecute = false, CreateNoWindow = true,
                WorkingDirectory = directory.FullName, RedirectStandardError = true, RedirectStandardOutput = true };

            foreach (var a in new[] { "slice", "-j", await Definition("global.def.json", global), "-o", output }) start.ArgumentList.Add(a);
            for (var i = 0; i < request.Colors.Count; i++)
            {
                foreach (var a in new[] { $"-e{i}", "-j", await Definition($"extruder-{i}.def.json", settings[$"extruder.{i}"]!.AsObject()),
                    "-l", Path.Combine(directory.FullName, $"part-{i}.stl"), "-s", "center_object=false",
                    "-s", "mesh_position_x=0", "-s", "mesh_position_y=0", "-s", "mesh_position_z=0" })
                {
                    start.ArgumentList.Add(a);
                }
            }
            using var process = new Process();
            process.StartInfo = start;
            var started = false;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds, 10, 600)));
            string summary;
            try
            {
                started = process.Start();
                if (!started) throw new InvalidOperationException("Không khởi động được CuraEngine.");
                var stdout = Drain(process.StandardOutput, timeout.Token);
                var stderr = Drain(process.StandardError, timeout.Token);
                await Task.WhenAll(process.WaitForExitAsync(timeout.Token), stdout, stderr);
                summary = stdout.Result + "\n" + stderr.Result;
                if (process.ExitCode != 0 || !File.Exists(output))
                { logger.LogWarning("CuraEngine failed for machine {Machine}, exit {Exit}", request.MachineId, process.ExitCode); throw new InvalidOperationException("CuraEngine không xử lý được file với profile máy hiện tại."); }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            { throw new InvalidOperationException("Slicer quá thời gian xử lý. Hãy thử file đơn giản hơn hoặc liên hệ PrintGrid."); }
            finally { if (started && !process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(CancellationToken.None); } }
            var diameters = Enumerable.Range(0, request.Colors.Count).Select(i => decimal.Parse(
                settings[$"extruder.{i}"]?["material_diameter"]?.ToString() ?? global["material_diameter"]?.ToString()
                ?? throw new InvalidDataException("Profile thiếu đường kính sợi nhựa material_diameter."), CultureInfo.InvariantCulture)).ToArray();
            var result = ParseEngineSummary(summary, request.Colors, request.Density, request.Quantity)
                ?? ParseGcodeHeader(File.ReadLines(output).Take(10000), request.Colors, request.Density, request.Quantity, diameters);
            // Conservative rectangular grid packing, with clearance between parts. Slicer time/material
            // are per-part estimates scaled by quantity; setup is charged once per occupied plate.
            var spacing = 5m;
            var across = Math.Max(1, (int)decimal.Floor((bedWidth + spacing) / ((decimal)scaled.X + spacing)));
            var along = Math.Max(1, (int)decimal.Floor((bedDepth + spacing) / ((decimal)scaled.Y + spacing)));
            var perPlate = global["machine_shape"]?.ToString() == "rectangular" ? across * along : 1;
            var plates = (int)decimal.Ceiling((decimal)request.Quantity / perPlate);
            return result with { PricePerHour = profile.PricePerHour, FailureRate = profile.FailureRate,
                PlateCount = plates, ProfileId = profile.ProfileId, ProfileVersion = profile.ProfileVersion,
                ColorAssignments = assignments, ModelScale = request.Scale, SlicingEngine = "CuraEngine 5.11.0" };

            // Turn resolved settings into definition JSONs with literal default_value fields.
            // Standard -j/-e/-l works with released CuraEngine versions; no GUI or -r required.
            async Task<string> Definition(string name, JsonObject values)
            {
                var literalSettings = new JsonObject();
                foreach (var pair in values)
                    literalSettings[pair.Key] = new JsonObject { ["default_value"] = pair.Value?.DeepClone() };
                var path = Path.Combine(directory.FullName, name);
                await File.WriteAllTextAsync(path, new JsonObject { ["settings"] = literalSettings }.ToJsonString(), cancellationToken);
                return path;
            }
        }
        catch (Exception e) when (e is IOException && e is not InvalidDataException or System.ComponentModel.Win32Exception or System.Text.Json.JsonException or FormatException or IndexOutOfRangeException)
        { logger.LogWarning(e, "Cura quote configuration/input failure"); throw new InvalidOperationException("Không thể chạy slicer. Kiểm tra file, đường dẫn CuraEngine và profile máy."); }
        finally { try { directory.Delete(recursive: true); } finally { _slots.Release(); } }
    }

    private static async Task<string> Drain(StreamReader reader, CancellationToken token)
    {
        var tail = new System.Text.StringBuilder(); var buffer = new char[4096]; int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) > 0)
        { tail.Append(buffer, 0, count); if (tail.Length > 65536) tail.Remove(0, tail.Length - 65536); }
        return tail.ToString();
    }

    public static bool FitsBuildPlate(JsonObject settings, decimal width, decimal depth, decimal height)
    {
        decimal Setting(string key) => decimal.Parse(settings[key]?.ToString()
            ?? throw new InvalidDataException($"Profile thiếu {key}."), CultureInfo.InvariantCulture);
        var bedWidth = Setting("machine_width");
        var bedDepth = Setting("machine_depth");
        var bedHeight = Setting("machine_height");
        if (bedWidth <= 0 || bedDepth <= 0 || bedHeight <= 0 || width <= 0 || depth <= 0 || height <= 0 ||
            width > bedWidth || depth > bedDepth || height > bedHeight) return false;
        return settings["machine_shape"]?.ToString() switch
        {
            "rectangular" => true,
            // The centered bounding rectangle must lie entirely inside the elliptical bed.
            "elliptic" => width / bedWidth * (width / bedWidth) + depth / bedDepth * (depth / bedDepth) <= 1,
            _ => throw new InvalidDataException("Loại bàn in trong profile không được hỗ trợ.")
        };
    }

    public static void ApplyQuoteSettings(JsonObject settings, decimal layerHeight, int infillPercent)
    {
        var previousLayer = Number("layer_height");
        var previousDensity = Number("infill_sparse_density");
        var previousDistance = Number("infill_line_distance");
        if (previousDensity > 0 && previousDistance.HasValue)
            settings["infill_line_distance"] = infillPercent == 0 ? 0 : previousDistance.Value * previousDensity.Value / infillPercent;
        if (previousLayer > 0 && Number("infill_sparse_thickness") is { } thickness)
            settings["infill_sparse_thickness"] = layerHeight * Math.Max(1, decimal.Round(thickness / previousLayer.Value));
        if (Number("top_thickness") is { } top) settings["top_layers"] = (int)decimal.Ceiling(top / layerHeight);
        if (Number("bottom_thickness") is { } bottom) settings["bottom_layers"] = (int)decimal.Ceiling(bottom / layerHeight);
        settings["layer_height"] = layerHeight;
        settings["layer_height_0"] = layerHeight;
        settings["infill_sparse_density"] = infillPercent;
        return;

        decimal? Number(string key) => decimal.TryParse(settings[key]?.ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    public static QuoteSliceResult? ParseEngineSummary(string summary, IReadOnlyList<string> colors, decimal density, int quantity)
    {
        var time = MyRegex().Matches(summary).LastOrDefault();
        var first = MyRegex1().Matches(summary).LastOrDefault();
        if (time is null || first is null) return null;
        var volumes = new decimal[colors.Count];
        volumes[0] = decimal.Parse(first.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(summary, @"Filament (\d+):\s*([\d.eE+\-]+)"))
        {
            var index = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) - 1;
            if (index >= 0 && index < volumes.Length) volumes[index] = decimal.Parse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        // Reuse strict validation; CLI summaries are always mm³, never filament length.
        return ParseGcodeHeader([
            ";TIME:" + time.Groups[1].Value,
            ";Filament used: " + string.Join(",", volumes.Select(v => v.ToString(CultureInfo.InvariantCulture) + "mm3"))
        ], colors, density, quantity);
    }

    public static QuoteSliceResult ParseGcodeHeader(IEnumerable<string> lines, IReadOnlyList<string> colors, decimal density, int quantity,
        IReadOnlyList<decimal>? filamentDiameters = null)
    {
        decimal? seconds = null;
        decimal[]? volumes = null;
        var griffin = new Dictionary<int, decimal>();
        foreach (var line in lines)
        {
            if (line.StartsWith(";TIME:", StringComparison.Ordinal)) seconds = decimal.Parse(line[6..].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
            if (line.StartsWith(";PRINT.TIME:", StringComparison.Ordinal)) seconds = decimal.Parse(line[12..].Trim(), CultureInfo.InvariantCulture);
            var match = MyRegex2().Match(line);
            if (match.Success) griffin[int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)] = decimal.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
            if (!line.StartsWith(";Filament used:", StringComparison.Ordinal)) continue;
            var raw = line[15..].Trim();
            volumes =
            [
                .. raw.Split(',').Select((v, i) =>
                {
                    v = v.Trim();
                    if (v.EndsWith("mm3", StringComparison.Ordinal))
                        return decimal.Parse(v[..^3], CultureInfo.InvariantCulture);
                    if (!v.EndsWith('m') || filamentDiameters is null || i >= filamentDiameters.Count ||
                        filamentDiameters[i] <= 0)
                    {
                        throw new InvalidDataException(
                            "Slicer trả đơn vị vật liệu không hỗ trợ hoặc profile thiếu đường kính sợi nhựa.");
                    }

                    var lengthMm = decimal.Parse(v[..^1], CultureInfo.InvariantCulture) * 1000m;
                    var diameter = filamentDiameters[i];
                    return lengthMm * (decimal)Math.PI * diameter * diameter / 4m;
                })
            ];
        }
        if (volumes is null && griffin.Count > 0)
        {
            volumes =
                [.. Enumerable.Range(0, griffin.Keys.Max() + 1).Select(i => griffin.GetValueOrDefault(i))];
        }

        if (density <= 0 || quantity <= 0 || seconds is null or <= 0 || volumes is null || volumes.Length < colors.Count ||
            volumes.Take(colors.Count).Any(v => v <= 0) || volumes.Skip(colors.Count).Any(v => v != 0))
        {
            throw new InvalidDataException("Slicer chưa trả đủ thời gian hoặc vật liệu từng màu.");
        }

        return new QuoteSliceResult(
            [.. colors.Select((c, i) => new QuoteColorUsage(c, volumes[i] / 1000m * density * quantity))],
            seconds.Value / 60m * quantity);
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"(?:Print time \(s\)|Total print time):\s*([\d.eE+\-]+)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex();
    [System.Text.RegularExpressions.GeneratedRegex(@"Filament \(mm\^3\):\s*([\d.eE+\-]+)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex1();
    [System.Text.RegularExpressions.GeneratedRegex(@"^;EXTRUDER_TRAIN\.(\d+)\.MATERIAL\.VOLUME_USED:([\d.]+)")]
    private static partial System.Text.RegularExpressions.Regex MyRegex2();
}
