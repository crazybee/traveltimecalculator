using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using TraveltimeCalculator.Models;
using Microsoft.Extensions.Configuration;
using TraveltimeCalculator.Services;


namespace TraveltimeCalculator.Controllers
{
    /// <summary>
    /// a simple ui to initiate the travel time calculation
    /// </summary>
    public class HomeController : Controller
    {
     
        private readonly IConfiguration configuration;

        private IMessageSender msgSender;

        public HomeController(IConfiguration configuration, IMessageSender msgSender)
        {
            this.configuration = configuration;
            this.msgSender = msgSender;
        }

        public IActionResult Index()
        {
            var indexModel = new IndexModel();

            return View(indexModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(IndexModel model)
        {

            var request = new TravelTimeRequest
            {
                Email = this.configuration["TargetEmailAddress"] ?? "zheliu@outlook.com",
                FromHomeToWork = model.IsFromHome
            };
            model.IsSuccessful = await this.msgSender.SendTravelTimeRequestAsync(request);

            return View(model);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
