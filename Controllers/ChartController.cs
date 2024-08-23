using CustomerStatus.Model;
using CustomerStatus.Service;
using Microsoft.AspNetCore.Mvc;

namespace CustomerStatus.Controllers
{
    public class ChartController : Controller
    {
        private readonly ChartService _chartService;
        public ChartController(ChartService chartService)
        {
            _chartService = chartService;
        }
        public async Task<IActionResult> Index()
        {
            List<Customer> Data = await _chartService.getFirstandLastIDs();
            return View(Data);
        }
    }
}
