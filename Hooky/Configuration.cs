using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace Hooky
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // Webhook Settings
        public string WebhookUrl { get; set; } = "";
        public string SecretKey { get; set; } = "";

        // Events
        public bool NotifyLoginQueue { get; set; } = true;
        public bool NotifyLoggedIn { get; set; } = true;
        public bool NotifyDutyPopped { get; set; } = true;

        internal Plugin Plugin;
        
        public void Initialize(Plugin plugin)
        {
            Plugin = plugin;
        }

        public void Save()
        {
            Plugin.PluginInterface!.SavePluginConfig(this);
        }
    }
}
