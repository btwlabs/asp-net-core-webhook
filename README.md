This app can be put on any server to supply a StoryCanvas webhook for offsite website hosting.

Windows Installation Instructions:

1) Publish the app to a folder as a stand alone app with R2R compilation, for the correct system
2) Upload the published app to the server
3) Open Windows Powershell as an administrator and execute sc.exe create: sc.exe create "MyAppService" binPath= "\"<path to your app>.exe\" --service" DisplaName="My Name"
4) In powershell set the service description with: sc.exe description MyAppService "This is the description of the service.."
5) Test the service by starting it in the services manager. Try one of the default urls: localhost:5000 or localhost:5001 to see the app. You may need to specify a url in the appsettings.
6) If the site app is working, set up a reverse proxy in IIS manager.
    a) make sure the URL Rewrite and Application Request Routing (ARR) modules are installed. Get them from microsoft
    b) set up a reverse proxy rule with in bound to the app service url matching '(.*)' requests. No need for outbound rules.
7) Create and add an ApiKey env var to the system settings for the server. Write it down to use in configuring the Storycanvas site webhook.
