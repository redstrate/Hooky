using System.Net.Http;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;

namespace Hooky
{
    public sealed class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface PluginInterface { get; init; }
        
        public Configuration Configuration { get; init; }
        
        [PluginService] private static IClientState ClientState { get; set; } = null!;
        
        [PluginService] private static IGameInteropProvider Hooking { get; set; } = null!;
        
        private unsafe delegate void OpenLoginWaitDialog(AgentLobby* agent, int position);

        private readonly Hook<OpenLoginWaitDialog>? openLoginWaitDialogHook;
        
        private ConfigWindow ConfigWindow { get; init; }
        private readonly WindowSystem WindowSystem = new("Hooky");
        
        public unsafe Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            PluginInterface = pluginInterface;
            Hooking.InitializeFromAttributes(this);
            
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            
            openLoginWaitDialogHook = Hooking.HookFromSignature<OpenLoginWaitDialog>(AgentLobby.Addresses.OpenLoginWaitDialog.String,
                OpenLoginWaitDialogDetour);
            openLoginWaitDialogHook?.Enable();
            
            ClientState.Login += OnLogin;
            ClientState.CfPop += OnContentPop;

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
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
            WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();

            ClientState.CfPop -= OnContentPop;
            ClientState.Login -= OnLogin;
            openLoginWaitDialogHook?.Dispose();
        }
        
        public void SendWebhook(string message)
        {
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
    }
}
