namespace PrintGrid.Modules.Scheduling.Domain.ValueObjects;

public sealed record PrinterConfiguration(
    string Manufacturer,
    int ExtruderCount,
    int MaximumColorSlots,
    decimal NozzleDiameterMm,
    decimal FilamentDiameterMm,
    bool HeatedBed,
    decimal MaxNozzleTemperatureC,
    decimal MaxBedTemperatureC,
    decimal PricePerHour,
    decimal FailureRate)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Manufacturer) || Manufacturer.Length > 100)
            throw new ArgumentException("Hãng sản xuất là bắt buộc, tối đa 100 ký tự.");
        if (ExtruderCount is < 1 or > 8 || MaximumColorSlots is < 1 or > 8 || MaximumColorSlots < ExtruderCount)
            throw new ArgumentException("Số đầu đùn và số màu phải từ 1 đến 8.");
        if (NozzleDiameterMm is <= 0 or > 2 || FilamentDiameterMm is < 1 or > 4)
            throw new ArgumentException("Đường kính đầu phun hoặc sợi nhựa không hợp lệ.");
        if (MaxNozzleTemperatureC is <= 0 or > 500 || MaxBedTemperatureC is < 0 or > 200 ||
            (HeatedBed && MaxBedTemperatureC <= 0) || (!HeatedBed && MaxBedTemperatureC != 0))
            throw new ArgumentException("Giới hạn nhiệt độ đầu phun/bàn in không hợp lệ.");
        if (PricePerHour is <= 0 or > 10000000 || FailureRate is < 0 or >= 1)
            throw new ArgumentException("Đơn giá máy phải lớn hơn 0; tỷ lệ lỗi từ 0% đến dưới 100%.");
    }
}
