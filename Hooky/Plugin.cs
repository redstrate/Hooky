using System.Net.Http;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;

namespace Hooky
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public IDalamudPluginInterface PluginInterface { get; set; }
        
        public Configuration Configuration { get; init; }
        
        [PluginService] private static IClientState ClientState { get; set; } = null!;
        
        [PluginService] private static IGameInteropProvider Hooking { get; set; } = null!;
        
        [PluginService] private static IPluginLog Log { get; set; } = null!;
        
        private readonly Hook<AgentLobby.Delegates.OpenLoginWaitDialog>? openLoginWaitDialogHook;
        
        private ConfigWindow ConfigWindow { get; init; }
        private readonly WindowSystem WindowSystem = new("Hooky");
        
        [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
        
        public unsafe Plugin()
        {
            Hooking.InitializeFromAttributes(this);
            
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(this);

            ConfigWindow = new ConfigWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            
            openLoginWaitDialogHook = Hooking.HookFromAddress<AgentLobby.Delegates.OpenLoginWaitDialog>(AgentLobby.MemberFunctionPointers.OpenLoginWaitDialog,
                OpenLoginWaitDialogDetour);
            openLoginWaitDialogHook?.Enable();
            
            ClientState.Login += OnLogin;
            ClientState.CfPop += OnContentPop;

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
            
            Plugin.ChatGui.ChatMessage += OnChatMessage;
        }
        
        private unsafe void OpenLoginWaitDialogDetour(AgentLobby* agent, int position)
        {
            openLoginWaitDialogHook?.OriginalDisposeSafe(agent, position);

            if (Configuration.NotifyLoginQueue)
            {
                SendWebhook("Position in queue:" + position);
            }
        }

        private void OnLogin()
        {
            if (Configuration.NotifyLoggedIn)
            {
                SendWebhook("Logged in!");
            }
        }

        private void OnContentPop(ContentFinderCondition condition)
        {
            if (Configuration.NotifyDutyPopped)
            {
                SendWebhook("Content popped!");
            }
        }
        
        public void Dispose()
        {
            Plugin.ChatGui.ChatMessage -= OnChatMessage;
            
            WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();

            ClientState.CfPop -= OnContentPop;
            ClientState.Login -= OnLogin;
            openLoginWaitDialogHook?.Dispose();
        }

        private bool IsAfk()
        {
            unsafe
            {
                bool isAfk = false;
                if (ClientState.LocalPlayer != null && ClientState.LocalPlayer.OnlineStatus.ValueNullable != null)
                {
                    // TODO: Really? 17?
                    isAfk = ClientState.LocalPlayer.OnlineStatus.RowId == 17;
                }
                isAfk |= !UIInputData.Instance()->CursorInputs.IsGameWindowFocused;

                return isAfk;
            }
        }
        
        public void SendWebhook(string message, bool checkAfk = true)
        {
            if (checkAfk && Configuration.OnlyWhenInactive && !IsAfk())
            {
                return;
            }
            
            var client = new HttpClient();
            client.PostAsync(Configuration.WebhookUrl, new StringContent("{\"body\":\"" + message + "\", \"key\": \"" + Configuration.SecretKey + "\"}"));
        }
        
        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
        
        private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message,
            ref bool ishandled)
        {
            // TODO: Support other languages
            if (Configuration.NotifyGate && sender.ToString() == "Gold Saucer Attendant")
            {
                Regex rg = new Regex("limited-time event “(.*)”.*in.([^.]*)");
                var match = rg.Match(message.ToString());
                if (match.Groups.Count == 3)
                {
                    SendWebhook($"GATE {match.Groups[1]} started in {match.Groups[2]}!");
                }
            }
        }
    }
}
