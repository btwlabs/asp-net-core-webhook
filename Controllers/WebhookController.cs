using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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
        
        public WebhookController(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        [HttpPost]
        
        public async Task<IActionResult> Post([FromBody] WebhookData data)
        {
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
                    var fileBytes = await client.GetByteArrayAsync(url);
                    
                    // Figure the local filepath and create it if not existing.
                    string projectDirectory = Path.Combine( Directory.GetCurrentDirectory(), "wwwroot");
                    string fullPath = $"{projectDirectory}{relativePath}";
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
                    await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);
                    
                    // Overwrite the s3 domain with the app's domain if the site is live.
                    if (data.IsLive)
                    {
                        // Read the file content
                        string content = await System.IO.File.ReadAllTextAsync(filePath);

                        string find = $"{data.Name}.s3-website.us-east-2.amazonaws.com";

                        if (_httpContextAccessor.HttpContext != null)
                        {
                            string replace = $"{_httpContextAccessor.HttpContext.Request.Host}";
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