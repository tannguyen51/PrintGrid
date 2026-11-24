using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PrintGrid.Infrastructure.Shared.Slicing;
using PrintGrid.Modules.Customer.Application.Quotes;
using PrintGrid.SharedKernel.Interfaces;

namespace PrintGrid.Modules.Customer.Infrastructure.Slicing;

/// <summary>Native MMU slicing of closed meshes with painted facets. No surface-area cost approximation.</summary>
public static partial class PrusaQuoteSlicer
{
    public sealed record PlateResult(decimal[] Grams, decimal Minutes, int ToolChanges, string Engine);

    public static async Task<QuoteSliceResult> SliceAsync(ThreeMfPackage.NativeDocument document,
        QuoteSliceRequest request, RegisteredCuraSettings reference, JsonObject settings, CuraQuoteOptions options,
        string directory, ILogger logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(options.PaintSlicerExecutablePath))
            throw new InvalidDataException("Slicer in nhiều màu chưa được cấu hình.");
        var global = settings["global"]!.AsObject();
        decimal Required(string key) => decimal.Parse(global[key]?.ToString() ?? throw new InvalidDataException($"Profile thiếu {key}."), CultureInfo.InvariantCulture);
        var width = Required("machine_width"); var depth = Required("machine_depth"); var height = Required("machine_height");
        var project = PrusaPaintProject.Write(document, request.Colors, request.ColorAssignments, directory, request.Scale,
            width, depth, height, options.PaintWipeTowerWidthMm,
            (w, d, h) => CuraQuoteSlicer.FitsBuildPlate(global, w, d, h));
        var grams = new decimal[request.Colors.Count];
        decimal minutes = 0; var changes = 0; string? engine = null;
        var fingerprints = new List<string>();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.PaintTimeoutSeconds, 10, 600)));
        try
        {
            foreach (var plate in project.Plates)
            {
                var ini = BuildProfile(settings, request, width, depth, height, plate.Tools, options);
                fingerprints.Add(ini);
                var configPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(plate.Path)}.ini");
                await File.WriteAllTextAsync(configPath, ini, new UTF8Encoding(false), timeout.Token);
                var output = Path.ChangeExtension(plate.Path, ".gcode");
                var start = new ProcessStartInfo(options.PaintSlicerExecutablePath)
                { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (var arg in new[] { "--loglevel", "1", "--datadir", Path.Combine(directory, "preferences"),
                    "--load", configPath, "--dont-arrange", "--export-gcode", "--output", output, plate.Path }) start.ArgumentList.Add(arg);
                using var process = new Process { StartInfo = start };
                var started = false;
                try
                {
                    started = process.Start();
                    if (!started) throw new InvalidOperationException("Không khởi động được slicer in nhiều màu.");
                    var stdout = Drain(process.StandardOutput, timeout.Token);
                    var stderr = Drain(process.StandardError, timeout.Token);
                    await Task.WhenAll(process.WaitForExitAsync(timeout.Token), stdout, stderr);
                    if (process.ExitCode != 0 || !File.Exists(output))
                    {
                        logger.LogWarning("Native color slicer failed, plate {Plate}, exit {Exit}: {Output}", plate.Id, process.ExitCode, stdout.Result + stderr.Result);
                        throw new InvalidDataException($"Không cắt lớp được bàn in {plate.Id} với cấu hình đã chọn. Hãy kiểm tra hình học, kích thước hoặc chọn lớp in khác.");
                    }
                }
                finally
                {
                    if (started && !process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(CancellationToken.None); }
                }
                var sliced = ParseResult(File.ReadLines(output), plate.Tools.Length);
                for (var i = 0; i < plate.Tools.Length; i++) grams[plate.Tools[i] - 1] += sliced.Grams[i];
                minutes += sliced.Minutes; changes += sliced.ToolChanges;
                engine = sliced.Engine;
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new InvalidDataException("Slicer nhiều màu quá thời gian xử lý. Hãy thử lớp in dày hơn hoặc kích thước nhỏ hơn."); }
        if (grams.Any(g => g <= 0) || minutes <= 0)
            throw new InvalidDataException("Một nhóm màu không tạo được đường in. Hãy kiểm tra chi tiết quá mỏng hoặc gộp nhóm đó vào màu khác.");
        return new QuoteSliceResult(request.Colors.Select((color, i) => new QuoteColorUsage(color, grams[i] * request.Quantity)).ToList(),
            minutes * request.Quantity, reference.PricePerHour, reference.FailureRate, project.Plates.Count * request.Quantity,
            reference.ProfileId, reference.ProfileVersion, project.Assignments, engine,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", fingerprints)))).ToLowerInvariant(),
            request.Scale, document.PlateCount, changes * request.Quantity, project.Plates.Max(plate => plate.RequiredBedWidthMm));
    }

    /// <summary>Whitelisted conversion of server-approved dimensions, material and motion settings.</summary>
    public static string BuildProfile(JsonObject settings, QuoteSliceRequest request, decimal width, decimal depth, decimal height,
        IReadOnlyList<int> activeTools, CuraQuoteOptions options)
    {
        var g = settings["global"]!.AsObject();
        decimal Number(JsonObject obj, string key, decimal fallback) => decimal.TryParse(obj[key]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
        decimal N(string key, decimal fallback) => Number(g, key, fallback);
        // Cura's inherited "unlimited" feedrate is 299792458000 mm/s. Prusa 2.5
        // validates these options against [0, INT_MAX], so do not copy the sentinel.
        // Keep real approved limits; use the adapter defaults only outside that range.
        decimal MotionLimit(string key, decimal fallback)
        {
            var value = N(key, fallback);
            return value is >= 0 and <= int.MaxValue ? value : fallback;
        }
        string F(decimal n) => n.ToString("0.######", CultureInfo.InvariantCulture);
        string Tools(Func<int, decimal> value) => string.Join(',', activeTools.Select(tool => F(value(tool - 1))));
        decimal E(int i, string key, decimal fallback) => settings[$"extruder.{i}"] is JsonObject e ? Number(e, key, N(key, fallback)) : N(key, fallback);
        var bed = g["machine_shape"]?.ToString() == "elliptic"
            ? string.Join(',', Enumerable.Range(0, 64).Select(i => $"{F(width / 2 + width / 2 * (decimal)Math.Cos(i * Math.PI / 32))}x{F(depth / 2 + depth / 2 * (decimal)Math.Sin(i * Math.PI / 32))}"))
            : $"0x0,{F(width)}x0,{F(width)}x{F(depth)},0x{F(depth)}";
        var values = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["threads"] = "2", ["printer_technology"] = "FFF", ["gcode_flavor"] = "marlin",
            ["bed_shape"] = bed, ["max_print_height"] = F(height),
            ["layer_height"] = F(request.LayerHeightMm), ["first_layer_height"] = F(request.LayerHeightMm),
            ["fill_density"] = request.InfillPercent + "%", ["fill_pattern"] = "grid",
            ["perimeters"] = F(Math.Clamp(N("wall_line_count", 3), 1, 20)),
            ["top_solid_layers"] = F(Math.Max(1, decimal.Ceiling(N("top_thickness", .8m) / request.LayerHeightMm))),
            ["bottom_solid_layers"] = F(Math.Max(1, decimal.Ceiling(N("bottom_thickness", .8m) / request.LayerHeightMm))),
            ["nozzle_diameter"] = Tools(i => E(i, "machine_nozzle_size", .4m)),
            ["filament_diameter"] = Tools(i => E(i, "material_diameter", 1.75m)),
            ["filament_density"] = Tools(_ => request.Density),
            ["temperature"] = Tools(i => E(i, "material_print_temperature", 210)),
            ["first_layer_temperature"] = Tools(i => E(i, "material_print_temperature_layer_0", 210)),
            ["bed_temperature"] = Tools(i => E(i, "material_bed_temperature", 60)),
            ["first_layer_bed_temperature"] = Tools(i => E(i, "material_bed_temperature_layer_0", 60)),
            ["filament_max_volumetric_speed"] = Tools(_ => options.PaintMaxVolumetricSpeed),
            ["perimeter_speed"] = F(N("speed_wall", 50)), ["external_perimeter_speed"] = F(N("speed_wall_0", 30)),
            ["infill_speed"] = F(N("speed_infill", 60)), ["solid_infill_speed"] = F(N("speed_skin", 40)),
            ["top_solid_infill_speed"] = F(N("speed_topbottom", 30)), ["travel_speed"] = F(N("speed_travel", 150)),
            ["first_layer_speed"] = F(N("speed_print_layer_0", 20)),
            ["default_acceleration"] = F(N("acceleration_print", 1000)),
            ["perimeter_acceleration"] = F(N("acceleration_wall", 1000)), ["infill_acceleration"] = F(N("acceleration_infill", 1000)),
            ["machine_max_acceleration_extruding"] = F(N("acceleration_print", 1000)),
            ["machine_max_acceleration_travel"] = F(N("acceleration_travel", 1000)),
            ["machine_limits_usage"] = "time_estimate_only",
            ["single_extruder_multi_material"] = "1", ["use_relative_e_distances"] = "1",
            ["layer_gcode"] = "G92 E0", ["start_gcode"] = "G28\\nG92 E0", ["end_gcode"] = "M104 S0\\nM140 S0",
            ["toolchange_gcode"] = "G4 S" + F(options.PaintColorChangeSeconds),
            ["support_material"] = g["support_enable"]?.ToString().Equals("true", StringComparison.OrdinalIgnoreCase) == true ? "1" : "0",
            ["support_material_extruder"] = "0", ["support_material_interface_extruder"] = "0",
            ["retract_length"] = Tools(i => E(i, "retraction_amount", .8m)),
            ["retract_speed"] = Tools(i => E(i, "retraction_retract_speed", 35)),
            ["skirts"] = "1", ["skirt_distance"] = "2", ["complete_objects"] = "0",
            ["wipe_tower"] = activeTools.Count > 1 ? "1" : "0", ["wipe_tower_x"] = "3", ["wipe_tower_y"] = "5",
            ["wipe_tower_width"] = F(options.PaintWipeTowerWidthMm),
            ["wiping_volumes_matrix"] = string.Join(',', Enumerable.Range(0, activeTools.Count * activeTools.Count)
                .Select(i => i / activeTools.Count == i % activeTools.Count ? "0" : F(options.PaintPurgeVolumeMm3))),
            ["post_process"] = "", ["before_layer_gcode"] = "", ["filament_start_gcode"] = "", ["filament_end_gcode"] = ""
        };
        foreach (var axis in new[] { "x", "y", "z", "e" })
        {
            values["machine_max_feedrate_" + axis] = F(MotionLimit("machine_max_feedrate_" + axis, axis == "z" ? 10 : 150));
            values["machine_max_acceleration_" + axis] = F(MotionLimit("machine_max_acceleration_" + axis, axis == "z" ? 100 : 1000));
        }
        return "; PrintGrid approved-profile adapter prusa-paint-v2\n" + string.Join('\n', values.Select(v => v.Key + " = " + v.Value)) + "\n";
    }

    public static PlateResult ParseResult(IEnumerable<string> lines, int colors)
    {
        decimal[]? grams = null; decimal? minutes = null; var changes = 0; string? engine = null;
        foreach (var line in lines)
        {
            if (line.StartsWith("; generated by PrusaSlicer ", StringComparison.Ordinal))
                engine = "PrusaSlicer " + line[27..].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
            if (line.StartsWith("; filament used [g] = ", StringComparison.Ordinal))
                grams = line[22..].Split(',').Select(v => decimal.Parse(v.Trim(), CultureInfo.InvariantCulture)).ToArray();
            if (line.StartsWith("; total toolchanges = ", StringComparison.Ordinal)) changes = int.Parse(line[22..].Trim(), CultureInfo.InvariantCulture);
            if (line.StartsWith("; estimated printing time (normal mode) = ", StringComparison.Ordinal))
            {
                decimal seconds = 0;
                foreach (Match match in Duration().Matches(line[(line.IndexOf('=') + 1)..]))
                    seconds += decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) * (match.Groups[2].Value switch { "d" => 86400, "h" => 3600, "m" => 60, _ => 1 });
                minutes = seconds / 60m;
            }
        }
        if (grams is null || grams.Length != colors || grams.Any(g => g < 0) || grams.Sum() <= 0 || minutes is null or <= 0 || engine is null)
            throw new InvalidDataException("Slicer nhiều màu chưa trả đủ khối lượng từng màu và thời gian in.");
        return new(grams, minutes.Value, changes, engine);
    }
    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*([dhms])")]
    private static partial Regex Duration();
    private static async Task<string> Drain(StreamReader reader, CancellationToken ct)
    {
        var tail = new StringBuilder(); var buffer = new char[8192]; int read;
        while ((read = await reader.ReadAsync(buffer, ct)) > 0) { tail.Append(buffer, 0, read); if (tail.Length > 32768) tail.Remove(0, tail.Length - 32768); }
        return tail.ToString();
    }
}
