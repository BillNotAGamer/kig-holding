using System.Globalization;
using KIGHolding.Models.Entities;

namespace KIGHolding.ViewModels.Shared;

public class BranchCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
    public string ImageUrl { get; set; } = "/images/placeholders/branch-card.webp";
    public string Address { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Hotline { get; set; } = string.Empty;
    public string OpeningHours { get; set; } = string.Empty;
    public string? LunchBreakHours { get; set; }
    public string GoogleMapUrl { get; set; } = "#";
    public int Capacity { get; set; }
    public decimal? AreaSquareMeters { get; set; }
    public int? NumberOfFloors { get; set; }
    public string Description { get; set; } = string.Empty;

    public static BranchCardViewModel FromBranch(Branch branch)
    {
        return new BranchCardViewModel
        {
            Name = branch.Name,
            Slug = branch.Slug,
            Url = $"/chi-nhanh#{branch.Slug}",
            ImageUrl = string.IsNullOrWhiteSpace(branch.ThumbnailUrl) ? "/images/placeholders/branch-card.webp" : branch.ThumbnailUrl,
            Address = branch.Address,
            District = branch.District,
            City = branch.City,
            Hotline = branch.Hotline,
            OpeningHours = FormatTimeRange(branch.OpeningTime, branch.ClosingTime),
            LunchBreakHours = FormatLunchBreakHours(branch),
            GoogleMapUrl = string.IsNullOrWhiteSpace(branch.GoogleMapUrl) ? "#" : branch.GoogleMapUrl,
            Capacity = branch.Capacity,
            AreaSquareMeters = branch.AreaSquareMeters,
            NumberOfFloors = branch.NumberOfFloors,
            Description = branch.Description
        };
    }

    private static string? FormatLunchBreakHours(Branch branch)
    {
        return branch.LunchBreakStart.HasValue && branch.LunchBreakEnd.HasValue
            ? FormatTimeRange(branch.LunchBreakStart.Value, branch.LunchBreakEnd.Value)
            : null;
    }

    private static string FormatTimeRange(TimeOnly start, TimeOnly end)
    {
        return $"{start.ToString("HH:mm", CultureInfo.InvariantCulture)} - {end.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    }
}
