using System.Globalization;
using apiClickupDevops.Models.Clickup;
using apiClickupDevops.Models.Devops;
using AutoMapper;
using Newtonsoft.Json.Linq;

namespace apiClickupDevops.Models.Mapper {
    public class CardMapper : Profile {
        public CardMapper() {

            CreateMap<ClickupCard, DevopsCard>()
                .ForMember(dst => dst.AppId, map => map.Ignore())
                .ForMember(dst => dst.Parent, map => map.Ignore())
                .ForMember(dst => dst.Url, map => map.Ignore())
                .ForMember(dst => dst.ClickupUrl, map => map.MapFrom(src => src.Url))
                .ForMember(dst => dst.Priority, map => map.MapFrom(src => ConvertPriority(src.Priority)));

            CreateMap<JObject, ClickupCard>()
                .ForMember(dest => dest.AppId, map => map.MapFrom(src => src["payload"]!["id"]!.Value<string>()))
                .ForMember(dest => dest.Title, map => map.MapFrom(src => src["payload"]!["name"]!.Value<string>()))
                .ForMember(dest => dest.Description, map => map.MapFrom(src => src["payload"]!["description"]!.Value<string>()))
                .ForMember(dest => dest.Parent, map => map.MapFrom(src => src["payload"]!["parent"]!.Value<string?>()))
                .ForMember(dest => dest.TeamId, map => map.MapFrom(src => src["payload"]!["team_id"]!.Value<string>()))
                .ForMember(dest => dest.Url, map => map.MapFrom(src => src["payload"]!["url"]!.Value<string>()))
                .ForMember(dest => dest.AssignedEmail, map => map.MapFrom(src => GetFirstAssigneeEmail(src["payload"]!["assignees"]!.ToObject<JArray>())))
                .ForMember(dest => dest.Priority, map => map.MapFrom(src => src["payload"]!["priority"]!.HasValues ? src["payload"]!["priority"]!["orderindex"]!.Value<int>() : 0))
                .ForMember(dest => dest.Status, map => map.MapFrom(src => CapitalizeWords(src["payload"]!["status"]!["status"]!.Value<string>())))
                .ForMember(dest => dest.ListId, map => map.MapFrom(src => src["payload"]!["list"]!["id"]!.Value<string>()));

            CreateMap<JObject, DevopsCard>()
                .ForMember(dest => dest.Title, map => map.MapFrom(src => src["name"]!.Value<string>()))
                .ForMember(dest => dest.Description, map => map.MapFrom(src => src["description"]!.Value<string>()))
                .ForMember(dest => dest.AssignedEmail, map => map.MapFrom(src => GetFirstAssigneeEmail(src["assignees"]!.ToObject<JArray>())))
                .ForMember(dest => dest.Priority, map => map.MapFrom(src => src["priority"]!.HasValues ? ConvertPriority(src["priority"]!["orderindex"]!.Value<int>()) : 0))
                .ForMember(dest => dest.Status, map => map.MapFrom(src => CapitalizeWords(src["status"]!["status"]!.Value<string>())))
                .ForMember(dest => dest.DevopsSync, map => map.MapFrom(src => GetSyncFlag(src["custom_fields"]!.ToObject<JArray>())));


            CreateMap<JObject, DevopsUpdateWebhook>()
                .ForMember(dest => dest.AppId, map => map.MapFrom(src => src["resource"]!["workItemId"]!.Value<string>()))
                .ForMember(dest => dest.Title, map => map.MapFrom(src => src["resource"]!["revision"]!["fields"]!["System.Title"]!.Value<string>()))
                .ForMember(dest => dest.Description, map => map.MapFrom(src => DescriptionCleaner(src["resource"]!["revision"]!["fields"]!["System.Description"]!.Value<string>())))
                .ForMember(dest => dest.State, map => map.MapFrom(src => src["resource"]!["revision"]!["fields"]!["System.State"]!.Value<string>()))
                .ForMember(dest => dest.Priority, map => map.MapFrom(src => ConvertPriority(src["resource"]!["revision"]!["fields"]!["Microsoft.VSTS.Common.Priority"]!.Value<int>())))
                .ForMember(dest => dest.Comment, map => map.MapFrom(src => src["resource"]!["revision"]!["fields"]!["System.History"]!.Value<string>()))
                .ForMember(dest => dest.ChangedBy, map => map.MapFrom(src => src["resource"]!["revision"]!["fields"]!["System.ChangedBy"]!.Value<string>()));

            CreateMap<JObject, ClickupUpdateWebhook>()
                .ForMember(dest => dest.Event, map => map.MapFrom(src => src["event"]!.Value<string>()))
                .ForMember(dest => dest.TaskId, map => map.MapFrom(src => src["task_id"]!.Value<string>()))
                .ForMember(dest => dest.UserId, map => map.MapFrom(src => src["history_items"]![0]!["user"]!["id"]!.Value<int>()))
                .ForMember(dest => dest.Username, map => map.MapFrom(src => src["history_items"]![0]!["user"]!["username"]!.Value<string>()))
                .ForMember(dest => dest.Comment, map => map.MapFrom(src => src["history_items"]![0]!["comment"]!.HasValues ? src["history_items"]![0]!["comment"]!["comment"]![0]!["text"]!.Value<string?>() : null))
                .ForMember(dest => dest.ListId, map => map.MapFrom(src => src["history_items"]![0]!["parent_id"]!.Value<string>()));
        }

        static string CapitalizeWords(string? input) {
            TextInfo textInfo = new CultureInfo("pt-br", false).TextInfo;
            return textInfo.ToTitleCase(input!);
        }

        static string? DescriptionCleaner(string? description) {
            description?.Replace("<div>", "");
            description?.Replace("</div>", "");
            return description;
        }
        static bool? GetSyncFlag(JArray? customFields) {

            foreach (var field in customFields!) {
                if (field["name"]?.ToString() == "DevOps Sync") {
                    return field["value"]!.Value<bool>();
                }
            }
            return null;
        }

        static string? GetFirstAssigneeEmail(JArray? assignees) {
            if (assignees != null && assignees.Count > 0) {
                var firstAssignee = assignees[0];
                return firstAssignee["email"]?.Value<string>();
            }
            return null;
        }
        public static int ConvertPriority(int priority) {
            int[] priorityMapping = { 4, 3, 2, 1 };

            if (priority >= 1 && priority <= 4) {
                return priorityMapping[priority - 1];
            }

            return priority;
        }
    }

}