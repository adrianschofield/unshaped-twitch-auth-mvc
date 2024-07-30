using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net; //required for HttpListenerRequest
using System.IO; //required for Streaming requests and responses
using System.Web; //required for HttpUtility - don't forget to add a Reference

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

// This controller deals with the callback from Twitch

namespace twitch_auth_mvc.Controllers
{
    public class MiroController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private string miroClientId;
        private string miroClientSecret;
        private string miroRedirectUri;

        public MiroController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Callback(string code)
        {
            // Set up the variables we need from Configuration
            string token = "No Token";
            miroRedirectUri = _configuration.GetValue<string>("Apps:MiroCallbackURI");
            miroClientSecret = _configuration.GetValue<string>("Apps:MiroClientSecret");
            miroClientId = _configuration.GetValue<string>("Apps:MiroClientId");
            // "https://id.twitch.tv/oauth2/authorize?response_type=code&client_id=5lc2pznnxzs8gijvw7qgaw8eoisj6nd&redirect_uri=https://localhost:5001/twitch/callback&scope=channel_read&state=123456"

            // DBG
            // _logger.LogDebug("In the Miro callback");
            
            // Start the call to Miro to exchange the code for the token
            var myResult = MiroAuthorizationApi(code);
           
            /* if (myResult.Count == 1) {
                token = myResult.First();
            }*/ 

            // Set up the view data
            ViewData["Code"] = code;
            ViewData["Token"] = myResult.access_token;
            // ViewData["Refresh"] = myResult.refresh_token;
            // ViewData["Expires"] = myResult.expires_in;

            return View();
        }

    

        private MiroAuthResponse MiroAuthorizationApi(string code)
        {
            HttpWebRequest myWebRequest = null;
            ASCIIEncoding encoding = new ASCIIEncoding();
            Dictionary<string, string> postDataDictionary = new Dictionary<string, string>();
            List<string> result = new List<string>();

            // We need to prepare the POST data ahead of time, Add each entry required by the Twitch Authorization Code Flow
            // Then spin through URLEncoding the keys and values and joining them into one string using & and =

            postDataDictionary.Add("client_id", miroClientId);
            postDataDictionary.Add("client_secret", miroClientSecret);
            postDataDictionary.Add("grant_type", "authorization_code");
            postDataDictionary.Add("redirect_uri", miroRedirectUri);
            //postDataDictionary.Add("state", "123456");
            postDataDictionary.Add("code", code);

            string postData = "";

            foreach (KeyValuePair<string, string> kvp in postDataDictionary)
            {
                postData += HttpUtility.UrlEncode(kvp.Key) + "=" + HttpUtility.UrlEncode(kvp.Value) + "&";
            }
            
            //We need the POST data as a byte array, using ASCII encoding to keep things simple

            byte[] byte1 = encoding.GetBytes(postData);

            // OK set up our request for the final step in the Authorization Code Flow
            // This is the destination URI as described in https://dev.twitch.tv/docs/v5/guides/authentication/

            myWebRequest = WebRequest.CreateHttp("https://api.miro.com/v1/oauth/token");

            // This request is a POST with the required content type

            myWebRequest.Method = "POST";
            myWebRequest.ContentType = "application/x-www-form-urlencoded";

            // Set the request length based on our byte array

            myWebRequest.ContentLength = byte1.Length;

            // Things can go wrong here so let's do some sensible exception handling, this sample is
            // short lived but best practice is to manage the POST and response

            // POST

            Stream postStream = null;

            try
            {
                //Set up the request and write the data this should complete the POST
                postStream = myWebRequest.GetRequestStream();
                postStream.Write(byte1, 0, byte1.Length);
            }
            catch (Exception ex)
            {
                //We should log any exception here but I am just going to supress them for this sample
                result.Add(string.Format("Ex: {0}", ex.Message));
                throw ex;
            }
            finally
            {
                postStream.Close();
            }

            //response to POST

            Stream responseStream = null;
            StreamReader responseStreamReader = null;
            WebResponse response = null;
            string jsonResponse = null;

            try
            {
                // Wait for the response from the POST above and get a stream with the data

                response = myWebRequest.GetResponse();
                responseStream = response.GetResponseStream();

                // Read the response, if everything worked we'll have our JSON encoded oauth token
                responseStreamReader = new StreamReader(responseStream);
                jsonResponse = responseStreamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                // We should log any exception here but I am just going to supress them for this sample
                result.Add(string.Format("Ex: {0}", ex.Message));
                throw ex;
            }
            finally
            {
                responseStreamReader.Close();
                responseStream.Close();
                response.Close();
            }

            // We got the jsonResponse from Twitch let's Deserialize it,
            // I'm using Newtonsoft - Install-Package Newtonsoft.Json -Version 9.0.1
            // Class for deserializing is defined below

            MiroAuthResponse myAuthResponse = null;

            try
            {
                myAuthResponse = JsonSerializer.Deserialize<MiroAuthResponse>(jsonResponse);
            }
            catch(Exception ex)
            {
                result.Add(string.Format("Ex: {0}", ex.Message));
                throw ex;
            }
            
            //Update the MainWindow TextBox with the access_token
            //You never need to display the access_token in a real world situation, just grab it and use
            //it in your authenticated Twitch API requests

            // result.Add(string.Format($"{myAuthResponse.access_token}"));

            return myAuthResponse;
        }
    }

    public class MiroAuthResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        //public string expires_in { get; set; }
        //public List<string> scope { get; set; }
    }  
}