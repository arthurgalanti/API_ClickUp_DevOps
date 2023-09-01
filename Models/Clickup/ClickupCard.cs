namespace apiClickupDevops.Models.Clickup {
    public class ClickupCard {
        public string AppId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string? Parent { get; set; }
        public string TeamId { get; set; } = null!;
        public string Url { get; set; } = null!;
        public string? AssignedEmail { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; } = null!;
        public string ListId { get; set; } = null!;
    }
}