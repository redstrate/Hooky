using System;
using System.Numerics;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Hooky
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;
        private Plugin Plugin;

        public ConfigWindow(Plugin plugin) : base(
            "FFXIV Webhook Settings",
            ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse)
        {
            Plugin = plugin;
            Size = new Vector2(500, 250);
            SizeCondition = ImGuiCond.Appearing;

            Configuration = plugin.Configuration;
        }

        public void Dispose()
        {
        }

        public override void Draw()
        {
            ImGui.TextDisabled("Webhook Settings");
            ImGui.Separator();
        
            var webhookUrl = Configuration.WebhookUrl;
            if (ImGui.InputText("Webhook URL", ref webhookUrl, 120))
            {
                Configuration.WebhookUrl = webhookUrl;
                Configuration.Save();
            }
            
            var secretKey = Configuration.SecretKey;
            if (ImGui.InputText("Secret Key", ref secretKey, 120))
            {
                Configuration.SecretKey = secretKey;
                Configuration.Save();
            }

            if (ImGui.Button("Send Test Message"))
            {
                Plugin.SendWebhook("Test message!");
            }

            ImGui.TextDisabled("Events");
            ImGui.Separator();
            
            var notifyLoginQueue = Configuration.NotifyLoginQueue;
            if (ImGui.Checkbox("In Login Queue", ref notifyLoginQueue))
            {
                Configuration.NotifyLoginQueue = notifyLoginQueue;
                Configuration.Save();
            }

            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Sends a message when you're in the login queue and any updates when you move through it.");
            
            var notifyLoggedIn = Configuration.NotifyLoggedIn;
            if (ImGui.Checkbox("Logged In", ref notifyLoggedIn))
            {
                Configuration.NotifyLoggedIn = notifyLoggedIn;
                Configuration.Save();
            }
            
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Sends a message when your character loaded into the game.");

            var notifyDutyPopped = Configuration.NotifyDutyPopped;
            if (ImGui.Checkbox("Duty Popped", ref notifyDutyPopped))
            {
                Configuration.NotifyDutyPopped = notifyDutyPopped;
                Configuration.Save();
            }
            
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("Sends a message when the duty has 'popped' and is awaiting to be commenced.");
        }
    }
}
