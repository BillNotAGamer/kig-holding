using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;
using Xunit;

namespace KIGHolding.Tests;

public sealed class BranchCardViewModelTests
{
    [Fact]
    public void FromBranch_WhenLunchBreakAppliesOnWeekendIsTrue_SetsPropertyAndLabelWithoutWeekendExclusion()
    {
        // Arrange
        var branch = new Branch
        {
            Name = "Truyền Thuyết Champong Tân Phú",
            Slug = "truyen-thuyet-champong-tan-phu",
            Address = "Số 1 Đường Tân Phú",
            District = "Tân Phú",
            City = "TP. Hồ Chí Minh",
            Hotline = "028 1234 5678",
            OpeningTime = new TimeOnly(11, 0),
            ClosingTime = new TimeOnly(22, 0),
            LunchBreakStart = new TimeOnly(14, 30),
            LunchBreakEnd = new TimeOnly(16, 30),
            LunchBreakAppliesOnWeekend = true
        };

        // Act
        var viewModel = BranchCardViewModel.FromBranch(branch);

        // Assert
        Assert.True(viewModel.LunchBreakAppliesOnWeekend);
        Assert.Equal("14:30 - 16:30", viewModel.LunchBreakHours);
        Assert.Equal("GIỜ NGHỈ TRƯA", viewModel.LunchBreakLabel);
    }

    [Fact]
    public void FromBranch_WhenLunchBreakAppliesOnWeekendIsFalse_SetsPropertyAndLabelWithWeekendExclusion()
    {
        // Arrange
        var branch = new Branch
        {
            Name = "Truyền Thuyết Champong Quận 1",
            Slug = "truyen-thuyet-champong-quan-1",
            Address = "Số 1 Đường Lê Lợi",
            District = "Quận 1",
            City = "TP. Hồ Chí Minh",
            Hotline = "028 8765 4321",
            OpeningTime = new TimeOnly(11, 0),
            ClosingTime = new TimeOnly(22, 0),
            LunchBreakStart = new TimeOnly(14, 30),
            LunchBreakEnd = new TimeOnly(16, 30),
            LunchBreakAppliesOnWeekend = false
        };

        // Act
        var viewModel = BranchCardViewModel.FromBranch(branch);

        // Assert
        Assert.False(viewModel.LunchBreakAppliesOnWeekend);
        Assert.Equal("14:30 - 16:30", viewModel.LunchBreakHours);
        Assert.Equal("GIỜ NGHỈ TRƯA (TRỪ THỨ BẢY, CHỦ NHẬT)", viewModel.LunchBreakLabel);
    }

    [Theory]
    [InlineData(true, "GIỜ NGHỈ TRƯA")]
    [InlineData(false, "GIỜ NGHỈ TRƯA (TRỪ THỨ BẢY, CHỦ NHẬT)")]
    public void LunchBreakLabel_ReturnsExpectedTextBasedOnLunchBreakAppliesOnWeekend(bool appliesOnWeekend, string expectedLabel)
    {
        // Arrange & Act
        var viewModel = new BranchCardViewModel
        {
            LunchBreakAppliesOnWeekend = appliesOnWeekend
        };

        // Assert
        Assert.Equal(expectedLabel, viewModel.LunchBreakLabel);
    }
}
