using HarmonyLib;
using UnityEngine;

namespace SailingTogether
{
    /// <summary>
    /// Lado do cliente: quando o jogador local está sentado num banco do barco,
    /// W/S deixam de levantá-lo e passam a ser o input do remo. O valor é escrito
    /// no ZDO do próprio Player (que o cliente já é dono), e assim chega ao dono do barco.
    /// </summary>
    internal static class RowerInput
    {
        internal static readonly int RowHash = "SailingTogether_Row".GetStableHashCode();
        internal static readonly int SeatedHash = "SailingTogether_Seated".GetStableHashCode();

        private static float s_lastSent;
        private static bool s_lastSeated;
        private static Ship s_seatedShip;

        /// <summary>Barco em cujo banco o jogador está sentado (exclui o leme).</summary>
        internal static Ship GetSeatedShip(Player player)
        {
            if (!player.IsAttachedToShip() || player.GetDoodadController() != null)
                return null;
            Transform attachPoint = player.GetAttachPoint();
            return attachPoint ? attachPoint.GetComponentInParent<Ship>() : null;
        }

        internal static void Send(Player player, float row, bool seated)
        {
            row = Mathf.Round(Mathf.Clamp(row, -1f, 1f) * 20f) / 20f;
            if (Mathf.Approximately(row, s_lastSent) && seated == s_lastSeated)
                return;

            ZNetView nview = player.GetComponent<ZNetView>();
            if (!nview || !nview.IsValid() || !nview.IsOwner())
                return;

            nview.GetZDO().Set(RowHash, row);
            nview.GetZDO().Set(SeatedHash, seated ? 1 : 0);
            s_lastSent = row;
            s_lastSeated = seated;
        }

        /// <summary>Lê o estado de remo de qualquer jogador (sincronizado via ZDO).</summary>
        internal static bool TryGetRower(Player player, out float row)
        {
            row = 0f;
            ZNetView nview = player ? player.GetComponent<ZNetView>() : null;
            ZDO zdo = nview ? nview.GetZDO() : null;
            if (zdo == null || zdo.GetInt(SeatedHash) == 0)
                return false;
            row = Mathf.Clamp(zdo.GetFloat(RowHash), -1f, 1f);
            return true;
        }

        internal static void OnSeatChanged(Player player, Ship ship)
        {
            if (ship == s_seatedShip)
                return;
            s_seatedShip = ship;
            if (ship && SailingTogetherPlugin.ShowHint.Value)
                player.Message(MessageHud.MessageType.TopLeft, "Remo: [W] frente  [S] trás  —  [Pular] levantar");
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    internal static class Player_SetControls_Patch
    {
        private static void Prefix(Player __instance, ref Vector3 movedir)
        {
            if (__instance != Player.m_localPlayer)
                return;

            Ship ship = SailingTogetherPlugin.Enabled.Value ? RowerInput.GetSeatedShip(__instance) : null;
            RowerInput.OnSeatChanged(__instance, ship);

            float row = 0f;
            if (ship)
            {
                row = movedir.z;
                // Zera o movimento para o vanilla não chamar AttachStop() (pular continua levantando).
                movedir = Vector3.zero;
            }
            RowerInput.Send(__instance, row, ship);
        }
    }
}
