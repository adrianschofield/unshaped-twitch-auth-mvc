using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using twitch_auth_mvc.Models;

namespace twitch_auth_mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Twitch()
        {
            // DBG
            //_logger.LogDebug("HomeController.cs Twitch Action");

            // Get the values we need for the initial Twitch POST
            // These come from the config in appsettings.json
            string clientId = _configuration.GetValue<string>("Apps:ClientId");
            string redirectUri = _configuration.GetValue<string>("Apps:CallbackURI");

            // Add them to the ViewData so we can display them
            ViewData["ClientId"] = clientId;
            ViewData["AppName"] = _configuration.GetValue<string>("Apps:AppName");
            
            // Finally add the Twitch URL
            // This is the url that Twitch auth needs
            ViewData["AuthURL"] = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope=channel_read&state=123456";

            // Return the view
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public ActionResult MyAction(string button)
        {
            return View("TestView");
        }
    }
}
