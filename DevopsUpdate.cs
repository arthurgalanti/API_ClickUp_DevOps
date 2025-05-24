using System;
using System.IO;
using System.Threading.Tasks;
using apiClickupDevops.Exceptions;
using apiClickupDevops.Models.Devops;
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
    public class DevopsUpdate {
        private readonly ILogger<ClickupUpdate> _logger;
        private readonly RelationService _relationService;
        private readonly ClickupService _clickupService;
        private readonly IMapper _mapper;
        private readonly int _integradorClickupId;
        private readonly string _integradorDevopsId;

        public DevopsUpdate(
            ILogger<ClickupUpdate> logger,
            RelationService relationService,
            ClickupService clickupService,
            IMapper mapper,
            Microsoft.Extensions.Configuration.IConfiguration configuration) {
            _logger = logger;
            _relationService = relationService;
            _clickupService = clickupService;
            _mapper = mapper;
            _integradorClickupId = int.Parse(configuration["IntegradorClickupId"]!);
            _integradorDevopsId = configuration["IntegradorDevopsId"]!;
        }

        [FunctionName("DevopsUpdate")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "webhook/devops/update")] HttpRequest req) {
            var data = JsonConvert.DeserializeObject<JObject>(await new StreamReader(req.Body).ReadToEndAsync());
            var response = new OkResult();
            Task task = Task.Run(() => DevopsUpdateInBackground(data!));
            return response;
        }
        private async void DevopsUpdateInBackground(JObject data) {
            try {
                var devopsWebhook = _mapper.Map<DevopsUpdateWebhook>(data);
                if (devopsWebhook.ChangedBy == _integradorDevopsId)
                    return;

                var relation = await _relationService.GetByDevopsId(devopsWebhook.AppId);
                try {
                    if (relation != null) {
                        await _clickupService.UpdateTask(relation, devopsWebhook);
                    }
                }
                catch (IntegrationException ex) {
                    _logger.LogError($"IntegrationException: {ex.Message}");
                    await _clickupService.UpdateCustomField(relation!.Clickup.AppId, relation.Clickup.ListId, "Sync Status", $"{ex.Message}");
                }
                catch (Exception ex) {
                    _logger.LogError($"Exception: {ex.Message}");
                    await _clickupService.UpdateCustomField(relation!.Clickup.AppId, relation.Clickup.ListId, "Sync Status", $"ERRO #99: Exceção");
                }
            }
            catch (Exception ex) {
                _logger.LogError($"Exception: {ex.Message}");
            }
        }
    }
}