using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace SailingTogether
{
    /// <summary>
    /// Utilitário para testar sozinho: cria remadores simulados nos assentos reais do barco
    /// em que o jogador local está. Cada assento tem um slider -1 (S) / 0 / 1 (W).
    ///
    /// Abrir/fechar com a tecla configurada (F8). Com o painel aberto o cursor fica livre e o
    /// input do jogo é bloqueado; ao fechar, os valores continuam valendo (pilote pelo leme).
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

            // Barco sem bancos: dois assentos genéricos, um de cada lado.
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
            return side == 0f ? "Centro" : side < 0f ? "Esquerda" : "Direita";
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
                _window = GUILayout.Window(0x5A17, _window, DrawWindow, "SailingTogether — simulador de remadores");
            }
            else if (s_simActive && s_ship && s_seats.Any(s => s.Row != 0f))
            {
                GUI.Label(new Rect(10f, 10f, 600f, 22f),
                    $"SailingTogether sim: {StatusLine()}   [{SailingTogetherPlugin.DebugHotkey.Value}] painel");
            }
        }

        private void DrawWindow(int id)
        {
            if (!s_ship)
            {
                GUILayout.Label("Suba num barco para simular os assentos.");
                if (GUILayout.Button("Fechar"))
                    IsOpen = false;
                GUI.DragWindow();
                return;
            }

            s_simActive = GUILayout.Toggle(s_simActive, " Simulação ativa");
            GUILayout.Label($"Barco: {Utils.GetPrefabName(s_ship.gameObject)}   Velocidade: {s_ship.GetSpeedSetting()}");
            GUILayout.Label(StatusLine());

            RowingStats stats = RowingStats.Get(s_ship);
            if (stats != null && !stats.IsOwner)
                GUILayout.Label("<color=orange>Você não é o dono do barco: a física roda em outro cliente.</color>");
            else if (stats != null && !stats.Allowed)
                GUILayout.Label("<color=orange>Remo inativo nesta velocidade (use a velocidade 1 no leme).</color>");

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Todos W")) SetAll(1f);
            if (GUILayout.Button("Todos S")) SetAll(-1f);
            if (GUILayout.Button("Zerar")) SetAll(0f);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Esq. W")) SetAll(1f, -1f);
            if (GUILayout.Button("Dir. W")) SetAll(1f, 1f);
            if (GUILayout.Button("Girar →")) MergeSpin(1f);
            if (GUILayout.Button("← Girar")) MergeSpin(-1f);
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
            GUILayout.Label($"[{SailingTogetherPlugin.DebugHotkey.Value}] fecha o painel (os valores continuam valendo).");
            GUI.DragWindow();
        }

        // Girar no próprio eixo: um lado W e o outro S. dir = +1 gira para a direita.
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
                return "sem dados";
            return $"remadores {stats.Rowers}  empurrão {stats.Thrust:+0.0;-0.0;0}  giro {stats.Turn:+0.0;-0.0;0}  " +
                   $"vel {s_ship.GetSpeed():0.0} m/s  yaw {stats.YawRate:+0;-0;0}°/s  {(stats.Applied ? "APLICANDO" : "parado")}";
        }
    }

    // Com o painel aberto: cursor livre e sem input no personagem/câmera.
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
