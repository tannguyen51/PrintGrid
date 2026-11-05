using System.Globalization;
using System.Text.Json.Nodes;
using PrintGrid.Modules.Scheduling.Domain.Entities;

namespace PrintGrid.Modules.Scheduling.Infrastructure.Slicing;

public static class CuraProfileImporter
{
    public static CuraProfile Parse(string name, string manufacturer, string model, string material,
        string version, string engineVersion, int extruderCount, string json)
    {
        if (new[] { name, manufacturer, model, material, version, engineVersion }.Any(string.IsNullOrWhiteSpace) ||
            new[] { name, manufacturer, model, material, version, engineVersion }.Any(v => v.Length > 100))
        {
            throw new ArgumentException("Tên profile, hãng, model, vật liệu, phiên bản là bắt buộc (tối đa 100 ký tự).");
        }

        if (material.Trim().Equals("RESIN", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("CuraEngine chỉ hỗ trợ profile FDM/FFF.");
        if (extruderCount is < 1 or > 8 || json.Length > 2 * 1024 * 1024)
            throw new ArgumentException("Số đầu đùn phải từ 1 đến 8; file profile tối đa 2 MB.");
        var root = JsonNode.Parse(json)?.AsObject() ?? throw new ArgumentException("File profile không hợp lệ.");
        if (root.Any(p => p.Key != "global" && p.Key != "limit_to_extruder" && !p.Key.StartsWith("extruder.")))
            throw new ArgumentException("Cần profile resolved JSON gồm global và extruder.N; file .curaprofile/.def.json chưa resolve không được hỗ trợ.");
        var global = root["global"] as JsonObject ?? throw new ArgumentException("Profile thiếu global.");
        var count = Number(global, "machine_extruder_count");
        if (count is < 1 or > 8 || count != decimal.Truncate(count)) throw new ArgumentException("Số tool màu trong profile không hợp lệ.");
        var slots = (int)count;
        if (slots < extruderCount) throw new ArgumentException("Số tool màu phải lớn hơn hoặc bằng số đầu đùn.");
        if (root.Count(p => p.Key.StartsWith("extruder.")) != slots)
            throw new ArgumentException("Profile phải có đủ extruder.0 đến extruder.N tương ứng số tool màu.");
        var width = Number(global, "machine_width"); var depth = Number(global, "machine_depth"); var height = Number(global, "machine_height");
        if (new[] { width, depth, height }.Any(n => n is <= 0 or > 1000)) throw new ArgumentException("Khổ in profile không hợp lệ.");
        if (global["machine_shape"]?.ToString() != "rectangular")
            throw new ArgumentException("Báo giá hiện hỗ trợ bàn in chữ nhật; bàn tròn cần cấu hình kiểm tra vùng in riêng.");
        if (!bool.TryParse(global["machine_heated_bed"]?.ToString(), out var heated) ||
            !bool.TryParse(global["machine_center_is_zero"]?.ToString(), out _))
        {
            throw new ArgumentException("Profile thiếu loại bàn nhiệt hoặc gốc tọa độ.");
        }

        foreach (var key in new[] { "machine_max_feedrate_x", "machine_max_feedrate_y", "machine_acceleration", "layer_height", "speed_print" })
            if (Number(global, key) <= 0) throw new ArgumentException($"Thông số {key} phải lớn hơn 0.");
        var nozzle = Number(global, "machine_nozzle_size"); var filament = Number(global, "material_diameter");
        decimal nozzleTemp = 0, bedTemp = 0;
        for (var i = 0; i < slots; i++)
        {
            var tool = root[$"extruder.{i}"] as JsonObject ?? throw new ArgumentException($"Thiếu extruder.{i}.");
            if (Number(tool, "machine_nozzle_size") != nozzle || Number(tool, "material_diameter") != filament)
                throw new ArgumentException("Các tool phải cùng đường kính đầu phun và sợi nhựa với cấu hình máy.");
            foreach (var key in new[] { "speed_print", "speed_wall", "speed_travel", "layer_height" })
                if (Number(tool, key) <= 0) throw new ArgumentException($"Tool {i}: {key} phải lớn hơn 0.");
            nozzleTemp = Math.Max(nozzleTemp, Math.Max(Number(tool, "material_print_temperature"), Number(tool, "material_print_temperature_layer_0")));
            bedTemp = Math.Max(bedTemp, Math.Max(Number(tool, "material_bed_temperature"), Number(tool, "material_bed_temperature_layer_0")));
        }
        new Domain.ValueObjects.PrinterConfiguration(manufacturer, extruderCount, slots, nozzle, filament,
            heated, nozzleTemp, bedTemp, 1, 0).Validate();
        return CuraProfile.Import(name, manufacturer, model, material, version, engineVersion, nozzle, filament,
            extruderCount, slots, width, depth, height, heated, nozzleTemp, bedTemp, root.ToJsonString());

        static decimal Number(JsonObject obj, string key) => decimal.TryParse(obj[key]?.ToString(), NumberStyles.Float,
            CultureInfo.InvariantCulture, out var n) ? n : throw new ArgumentException($"Profile thiếu/sai thông số {key}.");
    }
}
