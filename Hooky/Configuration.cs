using Dalamud.Configuration;
using System;

namespace Hooky
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        
        // General Settings
        public bool OnlyWhenInactive { get; set; } = true;

        // Webhook Settings
        public string WebhookUrl { get; set; } = "";
        public string SecretKey { get; set; } = "";

        // Events
        public bool NotifyLoginQueue { get; set; } = true;
        public bool NotifyLoggedIn { get; set; } = true;
        public bool NotifyDutyPopped { get; set; } = true;
        public bool NotifyGate { get; set; } = true;

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
