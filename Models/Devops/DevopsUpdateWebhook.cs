
namespace apiClickupDevops.Models.Devops {
    public class DevopsUpdateWebhook {
        public string AppId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string? Description { get; set; } = null!;
        public string State { get; set; } = null!;
        public int Priority { get; set; }
        public string? Comment { get; set; }
        public string ChangedBy { get; set; } = null!;
    }
}