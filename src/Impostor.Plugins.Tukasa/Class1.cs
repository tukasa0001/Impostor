using System;
using System.Threading.Tasks;
using Impostor.Api.Plugins;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Plugins.EBPlugin.Handlers;
using Microsoft.Extensions.Logging;

namespace Impostor.Plugins.EBPlugin
{
    [ImpostorPlugin(
        id: "local.EmptyBottle.au",
        name: "EBPlugin",
        author: "tukasa_01",
        version: "0.0.1")]
    public class EmptyBottlePlugin : PluginBase
    {
        private readonly ILogger<EmptyBottlePlugin> _logger;
        private readonly IEventManager _eventManager;
        private IDisposable _unregister;
        public EmptyBottlePlugin(ILogger<EmptyBottlePlugin> logger, IEventManager eventManager)
        {
            _logger = logger;
            _eventManager = eventManager;
        }
        public override ValueTask EnableAsync()
        {
            _logger.LogInformation("EmptyBottlePlugin is being enabled.");
            _unregister = _eventManager.RegisterListener(new GameEventListener(_logger));
            return default;
        }
        public override ValueTask DisableAsync()
        {
            _logger.LogInformation("EmptyBottlePlugin is being disabled.");
            _unregister.Dispose();
            return default;
        }
    }
}