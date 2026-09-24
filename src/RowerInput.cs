using HarmonyLib;
using UnityEngine;

namespace SailingTogether
{
    /// <summary>
    /// Client side: while the local player is sitting on a ship bench, W/S no longer
    /// stand them up and become the rowing input instead. The value is written to the
    /// player's own ZDO (already owned by this client), which syncs it to the ship owner.
    /// </summary>
    internal static class RowerInput
    {
        internal static readonly int RowHash = "SailingTogether_Row".GetStableHashCode();
        internal static readonly int SeatedHash = "SailingTogether_Seated".GetStableHashCode();

        private static float s_lastSent;
        private static bool s_lastSeated;
        private static Ship s_seatedShip;

        /// <summary>Ship whose bench the player is sitting on (excludes the helm).</summary>
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

        /// <summary>Reads the rowing state of any player (synced through their ZDO).</summary>
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
                player.Message(MessageHud.MessageType.TopLeft, "Rowing: [W] forward  [S] backward  —  [Jump] stand up");
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
                // Clear the movement so vanilla does not call AttachStop() (jumping still stands up).
                movedir = Vector3.zero;
            }
            RowerInput.Send(__instance, row, ship);
        }
    }
}
