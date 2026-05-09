namespace KIGHolding.Areas.Admin.ViewModels;

public class AdminDashboardViewModel
{
    public string AdminUsername { get; set; } = string.Empty;
    public int TotalBranches { get; set; }
    public int ActiveBranches { get; set; }
    public int TotalPosts { get; set; }
    public int PendingReservations { get; set; }
    public int TotalMessages { get; set; }
    public bool DatabaseConnected { get; set; }
}
