using System.Collections.Generic;
using Newtonsoft.Json;

namespace apiClickupDevops.Models.Clickup {
    public class ClickupUpdateWebhook {
        public string Event { get; set; } = null!;
        public string TaskId { get; set; } = null!;
        public string Username { get; set; } = null!;
        public int UserId { get; set; }
        public string? Comment { get; set; }
        public string ListId { get; set; } = null!;
    }
}