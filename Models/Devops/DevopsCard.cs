using apiClickupDevops.Models.Clickup;

namespace apiClickupDevops.Models.Devops {
    public class DevopsCard {
        public string AppId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Parent { get; set; }
        public string? AssignedEmail { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string ClickupUrl { get; set; } = null!;
        public ClickupConfiguration Configuration { get; set; } = null!;
        public ClickupUpdateWebhook UpdateWebwook { get; set; } = null!;
        public int Priority { get; set; }
        public string Status { get; set; } = null!;
        public bool DevopsSync { get; set; }
    }
}