using Microsoft.AspNetCore.Mvc;
using panelapp.Models;
using panelapp.Security;
using panelapp.Services;
using System.Diagnostics;

namespace panelapp.Controllers
{
    [SessionAuthorize]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public HomeController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardModel = await _dashboardService.GetDashboardAsync();

            ViewBag.ChartData = dashboardModel.ChartDataJson;

            return View(dashboardModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}