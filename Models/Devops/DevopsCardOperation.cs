using Newtonsoft.Json;

namespace apiClickupDevops.Models.Devops {
    public class DevopsCardOperation {
        public DevopsCardOperation(string op, string path, object value) {
            this.Op = op;
            this.Path = path;
            this.Value = value;
        }
        [JsonProperty("op")]
        public string Op { get; set; }
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}