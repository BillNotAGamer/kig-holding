using System.Text;
using KIGHolding.Data;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Areas.Admin.Controllers;

/// <summary>
/// Downloads the customer list in Meta's value-based custom audience CSV format (for Lookalike).
/// </summary>
public class AudienceExportController : AdminBaseController
{
    private readonly AppDbContext _dbContext;

    public AudienceExportController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var rows = await MetaAudienceExport.BuildRowsAsync(_dbContext, cancellationToken);
        var csv = MetaAudienceExport.ToCsv(rows);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var fileName = $"meta-audience-{DateTime.UtcNow.AddHours(7):yyyyMMdd}.csv";

        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}
