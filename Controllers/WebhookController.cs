using System.IO.Compression;
using asp_net_core_storycanvas_webhook.Models;
using Microsoft.AspNetCore.Mvc;

namespace asp_net_core_storycanvas_webhook.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly string _siteDirectory = Path.Combine( AppContext.BaseDirectory, "wwwroot");
        private readonly string _backupDirectory = Path.Combine( AppContext.BaseDirectory, "Backups");
        
        private void DeleteSite()
        {
            DirectoryInfo directory = new DirectoryInfo(this._siteDirectory);

            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete(); 
            }
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }
        }
        
        private void BackupSite()
        {
            // Specify backup directory in the root of your solution
            Directory.CreateDirectory(_backupDirectory);
    
            // Generate a backup file name
            string backupFileName = $"backup_{DateTime.Now:yyyyMMddHHmmss}.zip";
            string backupFilePath = Path.Combine(_backupDirectory, backupFileName);
    
            // Create the zip file
            ZipFile.CreateFromDirectory(_siteDirectory, backupFilePath,
                CompressionLevel.Optimal, false);
        }
        
        public WebhookController(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        [HttpPost]
        
        public async Task<IActionResult> Post([FromBody] WebhookData data)
        {
            // DO AUTHENTICATION.
            // Get the API key from the headers
            const string apikeyHeaderName = "X-API-KEY";
            if (!Request.Headers.TryGetValue(apikeyHeaderName, out var receivedApikey)) {
                return Unauthorized();
            }

            var expectedApikey = _configuration["ApiKey"];
            
            // Compare the received API key with the expected one
            if (!string.Equals(receivedApikey, expectedApikey, StringComparison.OrdinalIgnoreCase)) {
                return Unauthorized();
            }
            
            // CLEAN THE SITE DIRECTORY.
            // Create a backup of current site if it is live.
            if (data.IsLive)
            {
                BackupSite();
            }
            // Delete contents before re-deployment.
            DeleteSite();
            
            // GET ALL OF THE SITE'S FILES.
            // Copy from all of the Urls.
            foreach (var url in data.Urls)
            {
                // Download the file
                // You should handle any exceptions that might occur during download.
                using (HttpClient client = new HttpClient())
                {
                    // Get the file Name and desired path from the URL
                    Uri uri = new Uri(url);
                    string fileName = Path.GetFileName(uri.LocalPath);
                    string relativePath = string.Join("", uri.Segments.Take(uri.Segments.Length - 1));

                    // Get the data.
                    byte[]? fileBytes;
                    try
                    {
                        fileBytes = await client.GetByteArrayAsync(url);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }

                    if (fileBytes != null) {
                        // Figure the local filepath and create it if not existing.
                        string fullPath = $"{_siteDirectory}{relativePath}";
                        #pragma warning disable CS8600
                        string directory = Path.GetDirectoryName(fullPath);
                        if (string.IsNullOrEmpty(directory))
                        {
                            // Handle the error appropriately, could be throwing an exception, logging it, etc.
                            throw new Exception("The directory path is null or empty.");
                        }
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        
                        // Write the new file.
                        string filePath = $"{directory}/{fileName}";
                        try
                        {
                            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                        
                        // Overwrite the s3 domain with the app's domain.
                        string content = await System.IO.File.ReadAllTextAsync(filePath);

                        string find = $"{data.Name}.s3-website.us-east-2.amazonaws.com";

                        // Only proceed if this is and expected http request.
                        if (_httpContextAccessor.HttpContext != null)
                        {
                            string replace = data.Name; 
                            if (!data.IsLive)
                            { 
                                replace = $"{_httpContextAccessor.HttpContext.Request.Host}";
                            }
                            // Replace the string you need with data.Domain
                            string newContent = content.Replace(find, replace);
                            // Write the modified content back to the file
                            await System.IO.File.WriteAllTextAsync(filePath, newContent);
                        }
                        else
                        {
                            // Handle the error appropriately
                            throw new Exception("HttpContext is null.");
                        }
                    }
                    
                }
            }

            return Ok();
        }
    }
}