using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ShipTogether
{
    /// <summary>
    /// Lado do dono do barco (quem simula a física): soma o input de cada remador,
    /// separado por lado do barco, e aplica empurrão para frente/trás e giro (yaw).
    ///
    /// Cada remador contribui com:
    ///   empurrão = input                (W = +1, S = -1)
    ///   giro     = input * lado         (bombordo/esquerda = -1, boreste/direita = +1, centro = 0)
    ///
    /// Então: todos com W -> vai reto; só a esquerda com W -> vira para a direita;
    /// esquerda W + direita S -> gira no próprio eixo.
    /// </summary>
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    internal static class Ship_CustomFixedUpdate_Patch
    {
        private static readonly AccessTools.FieldRef<Ship, ZNetView> s_nview =
            AccessTools.FieldRefAccess<Ship, ZNetView>("m_nview");
        private static readonly AccessTools.FieldRef<Ship, Rigidbody> s_body =
            AccessTools.FieldRefAccess<Ship, Rigidbody>("m_body");
        private static readonly AccessTools.FieldRef<Ship, WaterVolume> s_previousCenter =
            AccessTools.FieldRefAccess<Ship, WaterVolume>("m_previousCenter");

        private static void Postfix(Ship __instance, float fixedDeltaTime)
        {
            if (!ShipTogetherPlugin.Enabled.Value)
                return;

            ZNetView nview = s_nview(__instance);
            if (!nview || !nview.IsValid())
                return;

            RowingStats stats = RowingStats.For(__instance);
            stats.Reset();
            stats.IsOwner = nview.IsOwner();
            if (!stats.IsOwner)
                return;

            stats.Allowed = ShipAccess.RowingAllowed(__instance.GetSpeedSetting());

            Transform shipTransform = __instance.transform;
            float thrust = 0f;
            float turn = 0f;

            void AddRower(float localX, float row)
            {
                row = Mathf.Clamp(row, -1f, 1f);
                if (Mathf.Abs(row) < 0.05f)
                    return;
                float side = ShipAccess.SideOf(localX);
                thrust += row;
                turn += row * side;
                stats.Rowers++;
            }

            List<Player> players = ShipAccess.Players(__instance);
            if (players != null)
            {
                long helmUser = __instance.m_shipControlls ? __instance.m_shipControlls.GetUser() : 0L;
                foreach (Player player in players)
                {
                    if (!player || player.GetPlayerID() == helmUser || !RowerInput.TryGetRower(player, out float row))
                        continue;
                    AddRower(shipTransform.InverseTransformPoint(player.transform.position).x, row);
                }
            }

            foreach (DebugPanel.SimSeat seat in DebugPanel.GetSimulatedSeats(__instance))
                AddRower(seat.LocalX, seat.Row);

            stats.Thrust = thrust;
            stats.Turn = turn;

            Rigidbody body = s_body(__instance);
            if (!body)
                return;
            stats.YawRate = Vector3.Dot(body.angularVelocity, shipTransform.up) * Mathf.Rad2Deg;
            stats.Floating = IsFloating(__instance, body);

            if (!stats.Allowed || !stats.Floating || (thrust == 0f && turn == 0f))
                return;
            stats.Applied = true;

            // Empurrão: fração da força do remo vanilla (m_backwardForce é por barco).
            float power = __instance.m_backwardForce * ShipTogetherPlugin.PowerPerRower.Value;
            body.AddForce(shipTransform.forward * (thrust * power * fixedDeltaTime), ForceMode.VelocityChange);

            // Giro: remar para frente do lado direito empurra a proa para a esquerda (yaw negativo no Unity).
            Vector3 up = shipTransform.up;
            float yawRate = stats.YawRate;
            float delta = -turn * ShipTogetherPlugin.TurnAccelPerRower.Value * fixedDeltaTime;
            float max = ShipTogetherPlugin.MaxTurnRate.Value;
            if (delta > 0f)
                delta = Mathf.Min(delta, Mathf.Max(0f, max - yawRate));
            else if (delta < 0f)
                delta = Mathf.Max(delta, Mathf.Min(0f, -max - yawRate));
            if (delta != 0f)
                body.AddTorque(up * (delta * Mathf.Deg2Rad), ForceMode.VelocityChange);
        }

        // Mesmo critério do vanilla para aplicar forças: centro de massa perto da linha d'água.
        private static bool IsFloating(Ship ship, Rigidbody body)
        {
            Vector3 com = body.worldCenterOfMass;
            float waterLevel = Floating.GetWaterLevel(com, ref s_previousCenter(ship));
            float depth = com.y - waterLevel - ship.m_waterLevelOffset;
            return depth <= ship.m_disableLevel;
        }
    }

    /// <summary>Último estado calculado da física do remo, para o painel de debug.</summary>
    internal class RowingStats
    {
        private static readonly RowingStats s_empty = new RowingStats();
        private static Ship s_ship;
        private static readonly RowingStats s_current = new RowingStats();

        public bool IsOwner, Allowed, Floating, Applied;
        public int Rowers;
        public float Thrust, Turn, YawRate;

        // Só guarda o barco em que o jogador local está (o único mostrado no painel).
        internal static RowingStats For(Ship ship)
        {
            if (ship != Ship.GetLocalShip())
                return s_empty;
            s_ship = ship;
            return s_current;
        }

        internal static RowingStats Get(Ship ship) => ship && ship == s_ship ? s_current : null;

        internal void Reset()
        {
            IsOwner = Allowed = Floating = Applied = false;
            Rowers = 0;
            Thrust = Turn = YawRate = 0f;
        }
    }
}
