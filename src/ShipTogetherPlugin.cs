using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace ShipTogether
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class ShipTogetherPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "farias.ShipTogether";
        public const string ModName = "ShipTogether";
        public const string ModVersion = "0.1.0";

        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> PowerPerRower;
        internal static ConfigEntry<float> TurnAccelPerRower;
        internal static ConfigEntry<float> MaxTurnRate;
        internal static ConfigEntry<float> CenterDeadZone;
        internal static ConfigEntry<bool> RowInStop;
        internal static ConfigEntry<bool> RowInBack;
        internal static ConfigEntry<bool> ShowHint;
        internal static ConfigEntry<bool> ShowHud;
        internal static ConfigEntry<float> HudScale;
        internal static ConfigEntry<float> HudOffsetX;
        internal static ConfigEntry<float> HudOffsetY;
        internal static ConfigEntry<string> HudShipSprite;
        internal static ConfigEntry<string> HudArrowSprite;
        internal static ConfigEntry<string> HudOarSprite;
        internal static ConfigEntry<bool> DebugTool;
        internal static ConfigEntry<KeyboardShortcut> DebugHotkey;

        internal static BepInEx.Logging.ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Liga/desliga o remo cooperativo.");

            PowerPerRower = Config.Bind("Rowing", "PowerPerRower", 0.35f,
                "Força de cada remador, como fração da força do remo do leme (velocidade 1) do próprio barco.");
            TurnAccelPerRower = Config.Bind("Rowing", "TurnAccelPerRower", 10f,
                "Aceleração de giro (graus/s²) gerada por cada remador desbalanceado (ex.: 1 lado remando e o outro não).");
            MaxTurnRate = Config.Bind("Rowing", "MaxTurnRate", 25f,
                "Velocidade máxima de giro (graus/s) que os remadores conseguem impor ao barco.");
            CenterDeadZone = Config.Bind("Rowing", "CenterDeadZone", 0.4f,
                "Distância lateral (metros) do eixo central do barco dentro da qual o assento conta como 'centro' (só empurra, não gira).");
            RowInStop = Config.Bind("Rowing", "RowInStop", false,
                "Permite remar com o barco parado (velocidade 0), inclusive sem ninguém no leme.");
            RowInBack = Config.Bind("Rowing", "RowInBack", true,
                "Permite remar com o barco em ré.");
            ShowHint = Config.Bind("General", "ShowHint", true,
                "Mostra a dica de controles ao sentar num banco do barco.");
            ShowHud = Config.Bind("Hud", "ShowShipHud", true,
                "Mostra o barco visto de cima com os bancos (piloto: todos; remador: só o seu).");
            HudScale = Config.Bind("Hud", "Scale", 1f,
                "Escala do mini-mapa do barco.");
            HudOffsetX = Config.Bind("Hud", "OffsetX", 30f,
                "Distância (px em 1080p) da borda direita da tela.");
            HudOffsetY = Config.Bind("Hud", "OffsetY", 230f,
                "Distância (px em 1080p) da borda inferior da tela.");
            HudShipSprite = Config.Bind("Hud", "ShipSprite", "ship_top",
                "Nome do sprite do jogo usado como barco (proa para cima). Vazio = desenho próprio.");
            HudArrowSprite = Config.Bind("Hud", "ArrowSprite", "winddirection",
                "Nome do sprite do jogo usado como seta (ponta para cima). Vazio = desenho próprio.");
            HudOarSprite = Config.Bind("Hud", "OarSprite", "ship_rudder_icon",
                "Nome do sprite do jogo usado como remo animado em cada banco ocupado. Vazio = sem remo.");
            HudOarSprite.SettingChanged += (_, __) => GameSprites.Reset();
            HudShipSprite.SettingChanged += (_, __) => GameSprites.Reset();
            HudArrowSprite.SettingChanged += (_, __) => GameSprites.Reset();
            DebugTool = Config.Bind("Debug", "SimulatorPanel", false,
                "Habilita o painel de remadores simulados (F8), para testar sozinho. Padrão: desabilitado.");
            DebugHotkey = Config.Bind("Debug", "SimulatorHotkey", new KeyboardShortcut(UnityEngine.KeyCode.F8),
                "Tecla que abre/fecha o painel de remadores simulados.");

            gameObject.AddComponent<DebugPanel>();
            gameObject.AddComponent<RowingHud>();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(ShipTogetherPlugin).Assembly);
            Logger.LogInfo($"{ModName} {ModVersion} carregado.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
