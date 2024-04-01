using System.Collections.Generic;
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
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WebhookData data)
        {

            // validate the Urls here
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
                    #pragma warning restore CS8600
                    
                    if (!Directory.Exists(directory))
                    {
                        #pragma warning disable CS8604
                        Directory.CreateDirectory(directory);
                        #pragma warning restore CS8604
                    }
                    
                    await System.IO.File.WriteAllBytesAsync($"{directory}/{fileName}", fileBytes);
                }
            }

            return Ok();
        }
    }
}