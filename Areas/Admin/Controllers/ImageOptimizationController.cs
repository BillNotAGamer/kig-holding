using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Areas.Admin.Controllers;

public class ImageOptimizationController : AdminBaseController
{
    private const int BatchSize = 15;

    private readonly IImageWebpMigrationService _migrationService;

    public ImageOptimizationController(IImageWebpMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var preview = await _migrationService.PreviewAsync(cancellationToken);
        return View(preview);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        var result = await _migrationService.RunBatchAsync(BatchSize, cancellationToken);

        if (result.Processed == 0 && result.Errors.Count > 0)
        {
            SetErrorMessage(result.Errors[0]);
            return RedirectToAction(nameof(Index));
        }

        if (result.Processed == 0)
        {
            SetSuccessMessage("Không còn ảnh nào cần chuyển sang WebP.");
            return RedirectToAction(nameof(Index));
        }

        var savedPercent = result.BytesBefore > 0
            ? (int)Math.Round(100d * (result.BytesBefore - result.BytesAfter) / result.BytesBefore)
            : 0;

        var message =
            $"Đã xử lý {result.Processed} ảnh: {result.ConvertedToWebp} chuyển sang WebP, " +
            $"{result.KeptOriginalFormat} giữ định dạng cũ, {result.Failed} lỗi. " +
            $"Dung lượng {FormatBytes(result.BytesBefore)} → {FormatBytes(result.BytesAfter)} (giảm {savedPercent}%). " +
            $"Cập nhật {result.UpdatedRecords} bản ghi. Còn lại {result.Remaining} ảnh.";

        if (result.Failed > 0)
        {
            message += " Lỗi: " + string.Join("; ", result.Errors.Take(3));
        }

        SetSuccessMessage(message);
        return RedirectToAction(nameof(Index));
    }

    private static string FormatBytes(long bytes)
    {
        return bytes >= 1024 * 1024
            ? $"{bytes / 1024d / 1024d:0.0}MB"
            : $"{bytes / 1024d:0}KB";
    }
}
