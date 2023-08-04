using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using apiClickupDevops.Services;
using apiClickupDevops.Models.Clickup;
using apiClickupDevops.Models.Devops;
using apiClickupDevops.Models;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Threading;
using Newtonsoft.Json.Linq;
using apiClickupDevops.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace apiClickupDevops {
    public class ClickupCreate {
        private readonly ILogger<ClickupCreate> _logger;
        private readonly RelationService _relationService;
        private readonly ClickupService _clickupService;
        private readonly DevopsService _devopsService;
        private readonly IMapper _mapper;

        public ClickupCreate(
            ILogger<ClickupCreate> logger,
            RelationService relationService,
            ClickupService clickupService,
            DevopsService devopsService,
            IMapper mapper) {
            _logger = logger;
            _relationService = relationService;
            _clickupService = clickupService;
            _devopsService = devopsService;
            _mapper = mapper;
        }

        [FunctionName("ClickupCreate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook/clickup/create")] HttpRequest req) {
            var data = JsonConvert.DeserializeObject<JObject>(await new StreamReader(req.Body).ReadToEndAsync());
            var response = new OkResult();
            Task task = Task.Run(() => ClickupCreateInBackground(data!));
            return response;
        }

        private async void ClickupCreateInBackground(JObject data) {
            try {
                var clickupCard = _mapper.Map<ClickupCard>(data);
                try {
                    var relation = await _relationService.GetByClickupId(clickupCard.AppId);
                    if (relation == null) {
                        relation = new Relation();
                        relation.Clickup.AppId = clickupCard.AppId;
                        relation.Clickup.ListId = clickupCard.ListId;
                        var devopsCard = _mapper.Map<DevopsCard>(clickupCard);
                        if (clickupCard.Parent == null)
                            relation.Clickup.TaskLevel = 1;
                        else {
                            int checkTimes = 0;
                            var parent = await _relationService.GetByClickupId(clickupCard.Parent);
                            do {
                                if (parent == null) {
                                    Thread.Sleep(Configuration.TimeToWaitParent);
                                    parent = await _relationService.GetByClickupId(clickupCard.Parent);
                                    checkTimes++;
                                }
                                else
                                    break;
                            } while (checkTimes < Configuration.MaxTimesToCheckParent);

                            if (parent != null) {
                                relation.Clickup.TaskLevel = Math.Min(parent!.Clickup.TaskLevel + 1, Configuration.MaxNestedTask);
                                relation.Clickup.Parent = parent.Clickup.AppId;
                                relation.Devops.Parent = parent.Devops.AppId;
                                devopsCard.Parent = relation.Devops.Parent;
                            }
                            else {
                                await _clickupService.UpdateCustomField(clickupCard.AppId, clickupCard.ListId, "Sync Status", $"ERRO #1: {clickupCard.Parent}");
                                return;
                            }
                        }
                        devopsCard.Configuration = await _clickupService.GetConfiguration(clickupCard.ListId, relation.Clickup.TaskLevel);
                        devopsCard = await _devopsService.CreateCard(relation, devopsCard);
                        relation.Devops.WorkItemType = devopsCard.Configuration.WorkItemType;
                        relation.Devops.AppId = devopsCard.AppId;
                        await _relationService.Create(relation);
                        await _clickupService.UpdateCustomField(clickupCard.AppId, clickupCard.ListId, "Devops", devopsCard.Url);
                    }
                }
                catch (IntegrationException ex) {
                    _logger.LogError($"IntegrationException: {ex.Message}");
                    await _clickupService.UpdateCustomField(clickupCard.AppId, clickupCard.ListId, "Sync Status", $"{ex.Message}");
                }
                catch (Exception ex) {
                    _logger.LogError($"Exception: {ex.Message}");
                    await _clickupService.UpdateCustomField(clickupCard.AppId, clickupCard.ListId, "Sync Status", $"ERRO #99: Exceção");
                }
            }
            catch (Exception ex) {
                _logger.LogError($"Exception: {ex.Message}");
            }

        }
    }
}
