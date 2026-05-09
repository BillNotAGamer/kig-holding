using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class ReviewController : AdminBaseController
{
    private readonly AppDbContext _dbContext;

    public ReviewController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var reviews = await _dbContext.Reviews
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ReviewListItemViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                Rating = x.Rating,
                IsVisible = x.IsVisible,
                DisplayOrder = x.DisplayOrder
            })
            .ToListAsync();

        return View(new ReviewIndexViewModel { Reviews = reviews });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View(await BuildCreateModelAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.BranchOptions = await BuildBranchOptionsAsync();
            return View(model);
        }

        var review = new Review
        {
            CustomerName = model.CustomerName.Trim(),
            Rating = model.Rating,
            Content = model.Content.Trim(),
            BranchId = model.BranchId,
            IsVisible = model.IsVisible,
            DisplayOrder = model.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Reviews.Add(review);
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var review = await _dbContext.Reviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (review is null)
        {
            return NotFound();
        }

        return View(new ReviewEditViewModel
        {
            Id = review.Id,
            CustomerName = review.CustomerName,
            Rating = review.Rating,
            Content = review.Content,
            BranchId = review.BranchId,
            IsVisible = review.IsVisible,
            DisplayOrder = review.DisplayOrder,
            BranchOptions = await BuildBranchOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ReviewEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var review = await _dbContext.Reviews.FirstOrDefaultAsync(x => x.Id == id);
        if (review is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.BranchOptions = await BuildBranchOptionsAsync();
            return View(model);
        }

        review.CustomerName = model.CustomerName.Trim();
        review.Rating = model.Rating;
        review.Content = model.Content.Trim();
        review.BranchId = model.BranchId;
        review.IsVisible = model.IsVisible;
        review.DisplayOrder = model.DisplayOrder;

        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await _dbContext.Reviews.FirstOrDefaultAsync(x => x.Id == id);
        if (review is null)
        {
            return NotFound();
        }

        review.IsVisible = false;
        await _dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<ReviewCreateViewModel> BuildCreateModelAsync()
    {
        return new ReviewCreateViewModel
        {
            BranchOptions = await BuildBranchOptionsAsync(),
            IsVisible = true
        };
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildBranchOptionsAsync()
    {
        var items = await _dbContext.Branches
            .AsNoTracking()
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Name} - {x.City}"
            })
            .ToListAsync();

        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "Không gắn chi nhánh" }
        };

        options.AddRange(items);
        return options;
    }
}
