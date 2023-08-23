using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using apiClickupDevops.Exceptions;
using apiClickupDevops.Models;
using apiClickupDevops.Models.Devops;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace apiClickupDevops.Services {
    public class DevopsService {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ClickupService _clickupService;
        private readonly IMapper _mapper;
        private readonly string _devopsTOKEN;

        public DevopsService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ClickupService clickupService,
            IMapper mapper) {
            _clickupService = clickupService;
            _httpClientFactory = httpClientFactory;
            _devopsTOKEN = configuration["TOKEN_DEVOPS"];
            _mapper = mapper;
        }

        public async Task<DevopsCard> CreateCard(Relation relation, DevopsCard devopsCard) {
            var client = _httpClientFactory.CreateClient("CreateCard");
            var initialOperations = new List<DevopsCardOperation>
            {
                    new DevopsCardOperation("add","/fields/System.Tags","clickup"),
                    new DevopsCardOperation("add","/fields/System.AreaPath",devopsCard.Configuration.AreaPath),
                    new DevopsCardOperation("add","/fields/System.Title",devopsCard.Title),
                    new DevopsCardOperation("add","/fields/System.Description",devopsCard.Description),
                    new DevopsCardOperation("add","/fields/Custom.Clickup",devopsCard.ClickupUrl),
                };

            if (devopsCard.Priority > 0)
                initialOperations.Add(new DevopsCardOperation("add", "/fields/Microsoft.VSTS.Common.Priority", devopsCard.Priority.ToString()));
            if (devopsCard.Parent != null)
                initialOperations.Add(new DevopsCardOperation("add", "/relations/-", new { rel = "System.LinkTypes.Dependency-forward", url = $"https://dev.azure.com/SEB-CSC/{devopsCard.Configuration.Project}/_apis/wit/workItems/{devopsCard.Parent}" }));

            var jsonOperations = JsonConvert.SerializeObject(initialOperations);
            var content = new StringContent(jsonOperations, Encoding.UTF8, "application/json-patch+json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _devopsTOKEN);
            var response = await client.PostAsync($"https://dev.azure.com/SEB-CSC/{devopsCard.Configuration.Project}/_apis/wit/workitems/${devopsCard.Configuration.WorkItemType}?api-version=7.0", content);

            if (response.IsSuccessStatusCode) {
                var responseObject = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
                var appId = responseObject!["id"]!.Value<string>();
                var url = responseObject["_links"]!["html"]!["href"]!.Value<string>();
                devopsCard.AppId = appId!;
                devopsCard.Url = url!;

                await _clickupService.UpdateCustomField(relation.Clickup.AppId, relation.Clickup.ListId, "Sync Status", "OK");
                if (devopsCard.AssignedEmail != null)
                    await UpdateSingleField(relation, devopsCard, new DevopsCardOperation("add", "/fields/System.AssignedTo", devopsCard.AssignedEmail), "AVISO #1: E-mail inexistente", false);
                if (devopsCard.Status != null)
                    await UpdateSingleField(relation, devopsCard, new DevopsCardOperation("add", "/fields/System.State", devopsCard.Status), "AVISO #2: Status inexistente!", false);

                return devopsCard;
            }
            else {
                var responseObject = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                Console.WriteLine(responseObject?.ToString());
                throw new Exception($"{responseObject}");
            }

        }

        public async Task UpdateCard(Relation relation, DevopsCard devopsCard) {
            var client = _httpClientFactory.CreateClient("UpdateCard");
            var updateOperations = new List<DevopsCardOperation> {
                    new DevopsCardOperation("add","/fields/System.AreaPath",devopsCard.Configuration.AreaPath),
                    new DevopsCardOperation("add","/fields/System.Title",devopsCard.Title),
                    new DevopsCardOperation("add","/fields/System.Description",devopsCard.Description),
                };
            if (devopsCard.Priority > 0)
                updateOperations.Add(new DevopsCardOperation("add", "/fields/Microsoft.VSTS.Common.Priority", devopsCard.Priority.ToString()));
            if (devopsCard.UpdateWebwook.Event == "taskCommentPosted")
                updateOperations.Add(new DevopsCardOperation("add", "/fields/System.History", $"@&lt;{devopsCard.UpdateWebwook.Username}&gt; : {devopsCard.UpdateWebwook.Comment}"));
            var content = new StringContent(JsonConvert.SerializeObject(updateOperations), Encoding.UTF8, "application/json-patch+json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _devopsTOKEN);
            var response = await client.PatchAsync($"https://dev.azure.com/SEB-CSC/{devopsCard.Configuration.Project}/_apis/wit/workitems/{devopsCard.AppId}?api-version=7.0", content);

            await _clickupService.UpdateCustomField(relation.Clickup.AppId, relation.Clickup.ListId, "Sync Status", "OK");
            if (response.IsSuccessStatusCode) {
                if (devopsCard.AssignedEmail != null)
                    await UpdateSingleField(relation, devopsCard, new DevopsCardOperation("add", "/fields/System.AssignedTo", devopsCard.AssignedEmail), "AVISO #1: E-Mail inexistente!", false);
                if (devopsCard.Status != null)
                    await UpdateSingleField(relation, devopsCard, new DevopsCardOperation("add", "/fields/System.State", devopsCard.Status), "AVISO #2: Status inexistente!", false);
            }
            else
                throw new IntegrationException("ERRO #6: Falha ao atualizar!");
        }

        public async Task UpdateSingleField(Relation relation, DevopsCard devopsCard, DevopsCardOperation op, string caseError, bool stop = true) {

            var client = _httpClientFactory.CreateClient("UpdateSingleField");
            var jsonOperations = JsonConvert.SerializeObject(new List<DevopsCardOperation> { op });
            var content = new StringContent(jsonOperations, Encoding.UTF8, "application/json-patch+json");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", _devopsTOKEN);
            var response = await client.PatchAsync($"https://dev.azure.com/SEB-CSC/{devopsCard.Configuration.Project}/_apis/wit/workitems/{devopsCard.AppId}?api-version=7.0", content);
            if (!response.IsSuccessStatusCode) {
                if (stop == true)
                    throw new IntegrationException(caseError);
                else
                    await _clickupService.UpdateCustomField(relation.Clickup.AppId, relation.Clickup.ListId, "Sync Status", $"{caseError}");
            }



        }
    }
}