namespace asp_net_core_storycanvas_webhook.Models;

public class WebhookData
{
    public string Domain { get; set; } = null!;
    public bool IsLive { get; set; }
    public int SiteId { get; set; }
    public string? Name { get; set; }
    public IEnumerable<string> Urls { get; set; } = new List<string>();
}