using KIGHolding.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("error")]
public class ErrorController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

        return View("Error", new ErrorViewModel
        {
            RequestId = exceptionFeature?.Error?.GetHashCode().ToString() ?? HttpContext.TraceIdentifier
        });
    }

    [HttpGet("{statusCode:int}")]
    public IActionResult StatusCodePage(int statusCode)
    {
        Response.StatusCode = statusCode;

        if (statusCode == StatusCodes.Status404NotFound)
        {
            return View("NotFound");
        }

        return View("Error", new ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
