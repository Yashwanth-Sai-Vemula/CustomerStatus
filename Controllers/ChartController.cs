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
            List<int> Ids = await _chartService.getFirstandLastIDs();
            List<int> data = await _chartService.getCustomerIds(Ids);
            List<Customer> ChartData = await _chartService.getData(data,Ids); 
            return View(ChartData);
        }
    }
}
