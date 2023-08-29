using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace apiClickupDevops.Models {
    [BsonIgnoreExtraElements]
    public class Relation {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonElement("clickup")]
        [JsonPropertyName("clickup")]
        public ClickupDetails Clickup { get; set; } = new();

        [BsonElement("devops")]
        [JsonPropertyName("devops")]
        public DevopsDetails Devops { get; set; } = new();

    }

    public class App {
        [BsonElement("appid")]
        [JsonPropertyName("appid")]
        public string AppId { get; set; } = string.Empty;

        [BsonElement("parent")]
        [JsonPropertyName("parent")]
        public string? Parent { get; set; } = null;
    }
    public class ClickupDetails : App {
        [BsonElement("tasklevel")]
        [JsonPropertyName("tasklevel")]
        public int TaskLevel { get; set; }
        [BsonElement("listid")]
        [JsonPropertyName("listid")]
        public string ListId { get; set; } = null!;
    }
    public class DevopsDetails : App {
        [BsonElement("workitemtype")]
        [JsonPropertyName("workitemtype")]
        public string WorkItemType { get; set; } = null!;
    }

}