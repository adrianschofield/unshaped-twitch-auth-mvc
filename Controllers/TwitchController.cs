using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net.Http;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// This controller deals with the callback from Twitch

namespace twitch_auth_mvc.Controllers
{
    public class TwitchController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private string twitchClientId;
        private string twitchClientSecret;
        private string twitchRedirectUri;

        public TwitchController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Callback(string code)
        {
            // Set up the variables we need from Configuration
            // TODO work out what I was doing with this variable
            // string token = "No Token";
            twitchRedirectUri = _configuration.GetValue<string>("Apps:TwitchCallbackURI");
            twitchClientSecret = _configuration.GetValue<string>("Apps:TwitchClientSecret");
            twitchClientId = _configuration.GetValue<string>("Apps:TwitchClientId");
            // "https://id.twitch.tv/oauth2/authorize?response_type=code&client_id=5lc2pznnxzs8gijvw7qgaw8eoisj6nd&redirect_uri=https://localhost:5001/twitch/callback&scope=channel_read&state=123456"

            // DBG
            // _logger.LogDebug("In the Twitch callback");
            
            // Start the call to Twitch to exchange the code for the token
            var myResult = TwitchAuthorizationApi(code).Result;
           
            /* if (myResult.Count == 1) {
                token = myResult.First();
            }*/ 

            // Set up the view data
            ViewData["Code"] = code;
            // ViewData["Token"] = myResult.
            ViewData["Token"] = myResult.access_token;
            ViewData["Refresh"] = myResult.refresh_token;
            ViewData["Expires"] = myResult.expires_in;

            return View();
        }

        private async Task<TwitchAuthResponse> TwitchAuthorizationApi(string twitchcode)
        {
            using (HttpClient client = new())
            {
                List<string> result = new List<string>();
                string url = "https://id.twitch.tv/oauth2/token";
                var jsonData = new
                {
                    client_id = twitchClientId,
                    client_secret = twitchClientSecret,
                    grant_type = "authorization_code",
                    redirect_uri = twitchRedirectUri,
                    code = twitchcode
                };

                // Convert the JSON data to string content
                StringContent content = new(JsonSerializer.Serialize(jsonData), Encoding.UTF8, "application/x-www-form-urlencoded");

                // Send the POST
                HttpResponseMessage response = await client.PostAsync(url, content);

                // Get the response content
                string responseContent = await response.Content.ReadAsStringAsync();

                // We got the jsonResponse from Twitch let's Deserialize it,
                // I'm using Newtonsoft - Install-Package Newtonsoft.Json -Version 9.0.1
                // Class for deserializing is defined below

                TwitchAuthResponse myAuthResponse = null;

                try
                {
                    myAuthResponse = JsonSerializer.Deserialize<TwitchAuthResponse>(responseContent);
                }
                // TODO Handle this exception better
                catch(Exception ex)
                {
                    result.Add(string.Format("Ex: {0}", ex.Message));
                }
                
                // Update the MainWindow TextBox with the access_token
                // You never need to display the access_token in a real world situation, just grab it and use
                // it in your authenticated Twitch API requests

                // TODO I don't do anything with result so my exception handling never logs an error
                // result.Add(string.Format($"{myAuthResponse.access_token}"));

                return myAuthResponse;

            }
        }
    
    }

    public class TwitchAuthResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public string expires_in { get; set; }
        public List<string> scope { get; set; }
    }  
}