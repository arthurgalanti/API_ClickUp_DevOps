using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using apiClickupDevops.Models.Clickup;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using apiClickupDevops.Exceptions;
using apiClickupDevops.Models.Devops;
using apiClickupDevops.Models;

namespace apiClickupDevops.Services {
    public class ClickupService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly string _clickupAPIKey;
        public ClickupService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IMapper mapper) {
            _httpClientFactory = httpClientFactory;
            _clickupAPIKey = configuration["TOKEN_CLICKUP"];
            _mapper = mapper;
        }

        public async Task UpdateTask(Relation relation, DevopsUpdateWebhook devopsWebhook) {
            var client = _httpClientFactory.CreateClient("UpdateTask");
            Dictionary<string, string> fieldsToUpdate = new() {
                { "name", devopsWebhook.Title },
                { "description", devopsWebhook.Description ?? " " },
                { "status", devopsWebhook.State }
            };
            if (devopsWebhook.Priority > 0)
                fieldsToUpdate.Add("priority", devopsWebhook.Priority.ToString());

            client.DefaultRequestHeaders.Add("Authorization", _clickupAPIKey);
            string fieldsContent = JsonConvert.SerializeObject(fieldsToUpdate);
            var postData = new StringContent(fieldsContent, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"https://api.clickup.com/api/v2/task/{relation.Clickup.AppId}", postData);
            if (response.IsSuccessStatusCode) {
                if (devopsWebhook.Comment != null)
                    await AddCommentTask(relation.Clickup.AppId, devopsWebhook.ChangedBy, devopsWebhook.Comment!);
                await UpdateCustomField(relation.Clickup.AppId, relation.Clickup.ListId, "Sync Status", "OK");
            }
            else
                throw new IntegrationException("ERRO #6: Falha ao atualizar!");
        }

        public async Task AddCommentTask(string taskId, string author, string comment) {
            string pattern = " <.*?>";
            string authorWithoutEmail = Regex.Replace(author, pattern, "");
            var client = _httpClientFactory.CreateClient("CommentTask");
            Dictionary<string, string> commentBody = new() {
                { "comment_text", $"{authorWithoutEmail} : {comment}" },
                { "notify_all", "false" }
            };

            client.DefaultRequestHeaders.Add("Authorization", _clickupAPIKey);
            string commentContent = JsonConvert.SerializeObject(commentBody);
            var postData = new StringContent(commentContent, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://api.clickup.com/api/v2/task/{taskId}/comment", postData);
            if (!response.IsSuccessStatusCode)
                throw new IntegrationException("ERRO #8: Falha ao comentar!");
        }
        public async Task<DevopsCard> GetDataToUpdate(string taskId) {
            var client = _httpClientFactory.CreateClient("GetDataToUpdate");
            client.DefaultRequestHeaders.Add("Authorization", _clickupAPIKey);
            var responseFields = await client.GetAsync($"https://api.clickup.com/api/v2/task/{taskId}");
            if (responseFields.IsSuccessStatusCode) {
                var responseContent = await responseFields.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<JObject>(responseContent);
                return _mapper.Map<DevopsCard>(data);
            }
            else
                throw new IntegrationException("ERRO #6: Dados não enviados!");
        }

        public async Task UpdateCustomField(string clickupId, string listId, string fieldName, string newValue) {
            string? fieldId = null;
            var client = _httpClientFactory.CreateClient("CustomFieldClickup");
            client.DefaultRequestHeaders.Add("Authorization", _clickupAPIKey);
            var responseFields = await client.GetAsync($"https://api.clickup.com/api/v2/list/{listId}/field");
            if (responseFields.IsSuccessStatusCode) {
                var responseObject = JsonConvert.DeserializeObject<dynamic>(await responseFields.Content.ReadAsStringAsync());
                JArray fieldsArray = (JArray)responseObject!["fields"];
                foreach (JObject field in fieldsArray.Cast<JObject>()) {
                    string? name = (string?)field["name"];
                    if (name == fieldName)
                        fieldId = (string?)field["id"];
                }
            }
            else
                throw new IntegrationException("ERRO #5: Campo inexistente!");

            if (fieldId != null) {
                var jsonBody = JsonConvert.SerializeObject(new Dictionary<string, string> { { "value", newValue } });
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var responseUpdate = await client.PostAsync($"https://api.clickup.com/api/v2/task/{clickupId}/field/{fieldId}", content);
                if (!responseUpdate.IsSuccessStatusCode)
                    throw new IntegrationException("ERRO #5: Campo inexistente!");
            }
        }
        public async Task<ClickupConfiguration> GetConfiguration(string listId, int taskLevel = 1) {
            ClickupConfiguration configuration = new();
            var client = _httpClientFactory.CreateClient("ListClickup");
            client.DefaultRequestHeaders.Add("Authorization", _clickupAPIKey);
            var response = await client.GetAsync($"https://api.clickup.com/api/v2/list/{listId}");

            if (response.IsSuccessStatusCode) {
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonConvert.DeserializeObject<JObject>(responseContent);
                string? contentValue = responseObject!["content"]!.Value<string?>();

                if (contentValue != null && contentValue != "") {
                    configuration.Project = Uri.EscapeDataString(ExtractValue(contentValue, "PROJECT"));
                    configuration.AreaPath = ExtractValue(contentValue, "AREAPATH");
                    configuration.WorkItemType = ExtractTaskLevel(contentValue, taskLevel);
                    return configuration;
                }
                else
                    throw new IntegrationException("ERRO #2: Configuração incorreta!");
            }
            else
                throw new Exception("ERRO #2: Configuração incorreta!");
        }

        private static string ExtractValue(string configString, string keyword) {
            int start = configString.IndexOf(keyword) + keyword.Length + 2;
            int end = configString.IndexOf("]", start);
            return configString[start..end];
        }

        private static string ExtractTaskLevel(string stringValue, int level) {
            var matches = Regex.Matches(stringValue, @"\[(.*?)\]");
            var totalTasks = matches.Count - 2;
            string[] contentArray = new string[totalTasks];
            for (int i = 0; i < totalTasks; i++)
                contentArray[i] = matches[i + 2].Groups[1].Value;
            if (level >= totalTasks)
                return contentArray[totalTasks - 1].Split(": ")[1];
            else
                return contentArray[level - 1].Split(": ")[1];
        }
    }
}