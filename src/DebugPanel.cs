using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SailingTogether
{
    /// <summary>
    /// Tool for testing alone: creates simulated rowers on the real benches of the ship the
    /// local player is on. Each bench has a -1 (S) / 0 / 1 (W) slider.
    ///
    /// Open/close with the configured key (F8). While open the cursor is free and game input is
    /// blocked; after closing, the values keep applying (steer from the helm).
    /// </summary>
    internal class DebugPanel : MonoBehaviour
    {
        internal class SimSeat
        {
            public string Name;
            public float LocalX;
            public float LocalZ;
            public float Row;
        }

        private static readonly List<SimSeat> s_seats = new List<SimSeat>();
        private static readonly SimSeat[] s_none = new SimSeat[0];
        private static Ship s_ship;
        private static bool s_simActive = true;

        internal static bool IsOpen { get; private set; }

        private Rect _window = new Rect(40f, 120f, 380f, 10f);

        internal static IEnumerable<SimSeat> GetSimulatedSeats(Ship ship)
        {
            if (!s_simActive || !SailingTogetherPlugin.DebugTool.Value || ship != s_ship || !s_ship)
                return s_none;
            return s_seats;
        }

        private void Update()
        {
            if (!SailingTogetherPlugin.DebugTool.Value)
            {
                IsOpen = false;
                return;
            }

            if (SailingTogetherPlugin.DebugHotkey.Value.IsDown() && Player.m_localPlayer
                && !Console.IsVisible() && !(Chat.instance && Chat.instance.HasFocus()))
            {
                IsOpen = !IsOpen;
            }

            Ship ship = Player.m_localPlayer ? Ship.GetLocalShip() : null;
            if (ship != s_ship)
                RebuildSeats(ship);
        }

        private static void RebuildSeats(Ship ship)
        {
            s_ship = ship;
            s_seats.Clear();
            if (!ship)
                return;

            foreach (ShipLayout.Seat seat in ShipLayout.Get(ship).Seats)
                s_seats.Add(new SimSeat { LocalX = seat.Local.x, LocalZ = seat.Local.z });

            // Ship without benches: two generic seats, one on each side.
            if (s_seats.Count == 0)
            {
                s_seats.Add(new SimSeat { LocalX = -1f, LocalZ = 0f });
                s_seats.Add(new SimSeat { LocalX = 1f, LocalZ = 0f });
            }

            for (int i = 0; i < s_seats.Count; i++)
                s_seats[i].Name = $"#{i + 1} {SideName(s_seats[i].LocalX)}";
        }

        private static string SideName(float localX)
        {
            float side = ShipAccess.SideOf(localX);
            return side == 0f ? "Center" : side < 0f ? "Left" : "Right";
        }

        private static void SetAll(float row, float side = 0f)
        {
            foreach (SimSeat seat in s_seats)
            {
                float seatSide = ShipAccess.SideOf(seat.LocalX);
                if (side == 0f || seatSide == side)
                    seat.Row = row;
                else if (side != 0f)
                    seat.Row = 0f;
            }
        }

        private void OnGUI()
        {
            if (!SailingTogetherPlugin.DebugTool.Value || !Player.m_localPlayer)
                return;

            float scale = Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (IsOpen)
            {
                _window = GUILayout.Window(0x5A17, _window, DrawWindow, "SailingTogether — rower simulator");
            }
            else if (s_simActive && s_ship && s_seats.Any(s => s.Row != 0f))
            {
                GUI.Label(new Rect(10f, 10f, 600f, 22f),
                    $"SailingTogether sim: {StatusLine()}   [{SailingTogetherPlugin.DebugHotkey.Value}] panel");
            }
        }

        private void DrawWindow(int id)
        {
            if (!s_ship)
            {
                GUILayout.Label("Board a ship to simulate its benches.");
                if (GUILayout.Button("Close"))
                    IsOpen = false;
                GUI.DragWindow();
                return;
            }

            s_simActive = GUILayout.Toggle(s_simActive, " Simulation active");
            GUILayout.Label($"Ship: {Utils.GetPrefabName(s_ship.gameObject)}   Speed: {s_ship.GetSpeedSetting()}");
            GUILayout.Label(StatusLine());

            RowingStats stats = RowingStats.Get(s_ship);
            if (stats != null && !stats.IsOwner)
                GUILayout.Label("<color=orange>You are not the ship owner: physics runs on another client.</color>");
            else if (stats != null && !stats.Allowed)
                GUILayout.Label("<color=orange>Rowing inactive at this speed (set speed 1 at the helm).</color>");

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All W")) SetAll(1f);
            if (GUILayout.Button("All S")) SetAll(-1f);
            if (GUILayout.Button("Reset")) SetAll(0f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Left W")) SetAll(1f, -1f);
            if (GUILayout.Button("Right W")) SetAll(1f, 1f);
            if (GUILayout.Button("Spin →")) MergeSpin(1f);
            if (GUILayout.Button("← Spin")) MergeSpin(-1f);
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            foreach (SimSeat seat in s_seats)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(seat.Name, GUILayout.Width(110f));
                GUILayout.Label("S", GUILayout.Width(12f));
                float value = GUILayout.HorizontalSlider(seat.Row, -1f, 1f, GUILayout.Width(150f));
                seat.Row = Mathf.Round(value);
                GUILayout.Label("W", GUILayout.Width(16f));
                GUILayout.Label(seat.Row > 0f ? "+1" : seat.Row < 0f ? "-1" : " 0", GUILayout.Width(24f));
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            GUILayout.Label($"[{SailingTogetherPlugin.DebugHotkey.Value}] closes the panel (values keep applying).");
            GUI.DragWindow();
        }

        // Spin in place: one side W and the other S. dir = +1 spins right.
        private static void MergeSpin(float dir)
        {
            foreach (SimSeat seat in s_seats)
            {
                float side = ShipAccess.SideOf(seat.LocalX);
                seat.Row = side == 0f ? 0f : -side * dir;
            }
        }

        private static string StatusLine()
        {
            RowingStats stats = RowingStats.Get(s_ship);
            if (stats == null)
                return "no data";
            return $"rowers {stats.Rowers}  thrust {stats.Thrust:+0.0;-0.0;0}  turn {stats.Turn:+0.0;-0.0;0}  " +
                   $"speed {s_ship.GetSpeed():0.0} m/s  yaw {stats.YawRate:+0;-0;0}°/s  {(stats.Applied ? "APPLYING" : "idle")}";
        }
    }

    // While the panel is open: free cursor and no character/camera input.
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    internal static class GameCamera_UpdateMouseCapture_Patch
    {
        private static void Postfix()
        {
            if (!DebugPanel.IsOpen)
                return;
            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }
    }

    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    internal static class PlayerController_TakeInput_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (DebugPanel.IsOpen)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    internal static class Player_TakeInput_Patch
    {
        private static void Postfix(ref bool __result)
        {
            if (DebugPanel.IsOpen)
                __result = false;
        }
    }
}
