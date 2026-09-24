using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SailingTogether
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class SailingTogetherPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "farias.SailingTogether";
        public const string ModName = "SailingTogether";
        public const string ModVersion = "1.0.0";

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
                "Enables/disables cooperative rowing.");

            PowerPerRower = Config.Bind("Rowing", "PowerPerRower", 0.35f,
                "Force of each rower, as a fraction of the ship's own helm-oar force (speed 1).");
            TurnAccelPerRower = Config.Bind("Rowing", "TurnAccelPerRower", 10f,
                "Turn acceleration (degrees/s²) produced by each unbalanced rower (e.g. one side rowing and the other not).");
            MaxTurnRate = Config.Bind("Rowing", "MaxTurnRate", 25f,
                "Maximum turn rate (degrees/s) the rowers can apply to the ship.");
            CenterDeadZone = Config.Bind("Rowing", "CenterDeadZone", 0.4f,
                "Lateral distance (meters) from the ship's centerline within which a bench counts as 'center' (pushes only, does not turn).");
            RowInStop = Config.Bind("Rowing", "RowInStop", false,
                "Allows rowing while the ship is stopped (speed 0), even with nobody at the helm.");
            RowInBack = Config.Bind("Rowing", "RowInBack", true,
                "Allows rowing while the ship is in reverse.");
            ShowHint = Config.Bind("General", "ShowHint", true,
                "Shows the controls hint when sitting on a ship bench.");
            ShowHud = Config.Bind("Hud", "ShowShipHud", true,
                "Shows the top-down ship HUD with the benches (helmsman: all benches, rower: only their own).");
            HudScale = Config.Bind("Hud", "Scale", 1f,
                "Scale of the ship HUD.");
            HudOffsetX = Config.Bind("Hud", "OffsetX", 30f,
                "Distance (px at 1080p) from the right edge of the screen.");
            HudOffsetY = Config.Bind("Hud", "OffsetY", 230f,
                "Distance (px at 1080p) from the bottom edge of the screen.");
            HudShipSprite = Config.Bind("Hud", "ShipSprite", "ship_top",
                "Name of the game sprite used for the ship (bow up). Empty = built-in drawing.");
            HudArrowSprite = Config.Bind("Hud", "ArrowSprite", "winddirection",
                "Name of the game sprite used for the arrows (tip up). Empty = built-in drawing.");
            HudOarSprite = Config.Bind("Hud", "OarSprite", "ship_rudder_icon",
                "Name of the game sprite used for the animated oar on each occupied bench. Empty = no oar.");
            HudOarSprite.SettingChanged += (_, __) => GameSprites.Reset();
            HudShipSprite.SettingChanged += (_, __) => GameSprites.Reset();
            HudArrowSprite.SettingChanged += (_, __) => GameSprites.Reset();
            DebugTool = Config.Bind("Debug", "SimulatorPanel", false,
                "Enables the simulated rowers panel (F8), for testing alone. Default: disabled.");
            DebugHotkey = Config.Bind("Debug", "SimulatorHotkey", new KeyboardShortcut(UnityEngine.KeyCode.F8),
                "Key that opens/closes the simulated rowers panel.");

            gameObject.AddComponent<DebugPanel>();
            gameObject.AddComponent<RowingHud>();

            _harmony = new Harmony(ModGuid);
            _harmony.PatchAll(typeof(SailingTogetherPlugin).Assembly);
            Logger.LogInfo($"{ModName} {ModVersion} loaded.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
