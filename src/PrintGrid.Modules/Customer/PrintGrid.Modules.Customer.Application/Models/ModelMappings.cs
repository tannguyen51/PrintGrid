using PrintGrid.Modules.Customer.Application.DTOs;

namespace PrintGrid.Modules.Customer.Application.Models;

public static class ModelMappings
{
    public static ModelDto ToDto(Domain.Entities.Model model) => new(
        model.Id,
        model.Name,
        model.Description,
        model.FileName,
        model.FileFormat,
        model.SizeBytes,
        model.Tags.ToList(),
        model.CreatedAt,
        model.UpdatedAt,
        model.GeometryStatus.ToString(),
        model.BoundingWidthMm,
        model.BoundingDepthMm,
        model.BoundingHeightMm,
        model.VolumeCm3,
        model.EstimatedPrintMinutes,
        model.GeometryMessage,
        model.StorageKey);
}