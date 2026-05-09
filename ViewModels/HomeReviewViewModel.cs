using KIGHolding.Models.Entities;

namespace KIGHolding.ViewModels;

public class HomeReviewViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? BranchName { get; set; }

    public static HomeReviewViewModel FromReview(Review review)
    {
        return new HomeReviewViewModel
        {
            CustomerName = review.CustomerName,
            AvatarUrl = review.AvatarUrl,
            Rating = review.Rating,
            Content = review.Content,
            BranchName = review.Branch?.Name
        };
    }
}
