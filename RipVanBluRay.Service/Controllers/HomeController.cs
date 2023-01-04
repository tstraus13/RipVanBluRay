using Microsoft.AspNetCore.Mvc;
using RipVanBluRay.Models;

namespace RipVanBluRay;

public class HomeController : Controller
{
    private SharedState _sharedState;

    public HomeController(SharedState sharedState)
    {
        _sharedState = sharedState;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "RipVanBluRay";

        return View(_sharedState);
    }
}