using System;
using System.IO;
using System.Threading.Tasks;
using apiClickupDevops.Exceptions;
using apiClickupDevops.Models.Clickup;
using apiClickupDevops.Services;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace apiClickupDevops {
    public class ClickupUpdate {
        private readonly ILogger<ClickupUpdate> _logger;
        private readonly RelationService _relationService;
        private readonly ClickupService _clickupService;
        private readonly DevopsService _devopsService;
        private readonly IMapper _mapper;
        private readonly int _integradorClickupId;
        private readonly string _integradorDevopsId;

        public ClickupUpdate(
            ILogger<ClickupUpdate> logger,
            RelationService relationService,
            ClickupService clickupService,
            DevopsService devopsService,
            IMapper mapper,
            Microsoft.Extensions.Configuration.IConfiguration configuration) {
            _logger = logger;
            _relationService = relationService;
            _clickupService = clickupService;
            _devopsService = devopsService;
            _mapper = mapper;
            _integradorClickupId = int.Parse(configuration["IntegradorClickupId"]!);
            _integradorDevopsId = configuration["IntegradorDevopsId"]!;
        }

        [FunctionName("ClickupUpdate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook/clickup/update")] HttpRequest req) {
            var data = JsonConvert.DeserializeObject<JObject>(await new StreamReader(req.Body).ReadToEndAsync());
            var response = new OkResult();
            Task task = Task.Run(() => ClickupUpdateInBackground(data!));
            return response;
        }

        private async void ClickupUpdateInBackground(JObject data) {
            try {
                var clickupWebhook = _mapper.Map<ClickupUpdateWebhook>(data);
                if (clickupWebhook.UserId == _integradorClickupId)
                    return;

                try {
                    var relation = await _relationService.GetByClickupId(clickupWebhook.TaskId);

                    if (relation != null) {
                        var devopsCard = await _clickupService.GetDataToUpdate(clickupWebhook.TaskId);
                        devopsCard.AppId = relation.Devops.AppId;
                        devopsCard.UpdateWebwook = clickupWebhook;
                        devopsCard.Configuration = await _clickupService.GetConfiguration(clickupWebhook.ListId, relation.Clickup.TaskLevel);
                        await _devopsService.UpdateCard(relation, devopsCard);
                    }
                }
                catch (IntegrationException ex) {
                    _logger.LogError($"IntegrationException: {ex.Message}");
                    await _clickupService.UpdateCustomField(clickupWebhook.TaskId, clickupWebhook.ListId, "Sync Status", $"{ex.Message}");
                }
                catch (Exception ex) {
                    _logger.LogError($"Exception: {ex.Message}");
                    await _clickupService.UpdateCustomField(clickupWebhook.TaskId, clickupWebhook.ListId, "Sync Status", $"ERRO #99: Exceção");
                }
            }
            catch (Exception ex) {
                _logger.LogError($"Exception: {ex.Message}");
            }
        }
    }
}