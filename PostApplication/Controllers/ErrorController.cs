using Microsoft.AspNetCore.Mvc;

namespace PostApplication.Controllers;

public class ErrorController : Controller
{
    [HttpGet]
    [Route("Error/{statusCode:int}")]
    public IActionResult Show(int statusCode)
    {
        return statusCode switch
        {
            404 => View("NotFound"),
            401 => View("Unauthorized"),
            403 => View("Forbidden"),
            _ => View("Error"),
        };
    }
}