# unshaped-twitch-auth-mvc
A simple MVC application which shows how to use the Twitch oAuth flow

# The application needs some configuration

Create an appsettings.json containing a section like this:

`"Apps": {
    "ClientId" :"CLIENTID HERE",
    "ClientSecret": "CLIENT SECRET HERE",
    "CallbackURI": "https://localhost:5001/twitch/callback",
    "AppName": "APP NAME HERE"
  },`

  Note that you app configuration on Twitch must have the callback URI set to https://localhost:5001/twitch/callback

  # Using the Twitch Auth flow

  Start the app using dotnet run and browse to https://localhost:5001

  Click the Twitch link at the top of the page. The Twitch view will show the name of the app you configured in appsettings.json. Click the Authorise button and you will be taken through the Auth flow and the Code and Token will be displayed.

  # How it works

  The HomeController Twitch method grabs data from the configuration and generates the redirect Uri which is needed to start the auth flow to Twitch. It also provides the App Name in ViewData so we know what we're looking at.

  The Twitch view contains a Javascript snippet which provides a button that starts the Auth flow by redirecting you to Twitch. 

  The Twitch controller is where the callback from Twitch is handled. /twitch/callback and expects the code which is the initial part of the auth flow.

  The controller then runs some code to call Twitch again to exchange the code for the token.

  Finally the controller provides ViewData to the Callback view showing the code and the token.

  It should go without saying that displaying the token anywhere is super daft and in the normal scheme of things you would then use the Token when making calls to Twitch.

  
