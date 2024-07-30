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

// This controller deals with the callback from Github

namespace twitch_auth_mvc.Controllers
{
    public class GithubController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private string githubClientId;
        private string githubClientSecret;
        private string githubRedirectUri;

        public GithubController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public IActionResult Callback(string code)
        {
            // Set up the variables we need from Configuration
            string token = "No Token";
            githubRedirectUri = _configuration.GetValue<string>("Apps:GithubCallbackURI");
            githubClientSecret = _configuration.GetValue<string>("Apps:GithubClientSecret");
            githubClientId = _configuration.GetValue<string>("Apps:GithubClientId");
            
            // DBG
            // _logger.LogDebug("In the Github callback");
            
            // Start the call to Github to exchange the code for the token
            List<string> myResult = GithubAuthorizationApi(code);
           
            if (myResult.Count == 1) {
                token = myResult.First();
            }

            // Set up the view data
            ViewData["Code"] = code;
            ViewData["Token"] = token;

            return View();
        }

    

        private List<string>GithubAuthorizationApi(string code)
        {
            HttpWebRequest myWebRequest = null;
            ASCIIEncoding encoding = new ASCIIEncoding();
            Dictionary<string, string> postDataDictionary = new Dictionary<string, string>();
            List<string> result = new List<string>();

            // We need to prepare the POST data ahead of time, Add each entry required by the Github Authorization Code Flow
            // Then spin through URLEncoding the keys and values and joining them into one string using & and =

            postDataDictionary.Add("client_id", githubClientId);
            postDataDictionary.Add("client_secret", githubClientSecret);
            postDataDictionary.Add("redirect_uri", githubRedirectUri);
            postDataDictionary.Add("code", code);

            string postData = "";

            foreach (KeyValuePair<string, string> kvp in postDataDictionary)
            {
                postData += HttpUtility.UrlEncode(kvp.Key) + "=" + HttpUtility.UrlEncode(kvp.Value) + "&";
            }
            
            //We need the POST data as a byte array, using ASCII encoding to keep things simple

            byte[] byte1 = encoding.GetBytes(postData);

            // OK set up our request for the final step in the Authorization Code Flow
            // This is the destination URI as described in https://docs.github.com/en/developers/apps/building-oauth-apps/authorizing-oauth-apps

            myWebRequest = WebRequest.CreateHttp("https://github.com/login/oauth/access_token");

            // This request is a POST with the required content type

            myWebRequest.Method = "POST";
            // myWebRequest.ContentType = "application/x-www-form-urlencoded";
            myWebRequest.Accept = "application/json";

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

            // We got the jsonResponse from Github let's Deserialize it,
            // I'm using Newtonsoft - Install-Package Newtonsoft.Json -Version 9.0.1
            // Class for deserializing is defined below

            GithubAuthResponse myAuthResponse = null;

            try
            {
                //myAuthResponse = JsonConvert.DeserializeObject<GithubAuthResponse>(jsonResponse);
                myAuthResponse = JsonSerializer.Deserialize<GithubAuthResponse>(jsonResponse);
            }
            catch(Exception ex)
            {
                result.Add(string.Format("Ex: {0}", ex.Message));
                throw ex;
            }
            
            // Update the MainWindow TextBox with the access_token
            // You never need to display the access_token in a real world situation, just grab it and use
            // it in your authenticated Github API requests

            result.Add(string.Format($"{myAuthResponse.access_token}"));

            return result;
        }
    }

    public class GithubAuthResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
    }  
}