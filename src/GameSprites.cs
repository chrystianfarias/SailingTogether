using UnityEngine;
using UnityEngine.UI;

namespace SailingTogether
{
    /// <summary>
    /// The game's own icons used by the HUD: by default "ship_top" (top-down ship, the same one
    /// used by the wind indicator), "winddirection" (wind arrow) and "ship_rudder_icon" (oar).
    /// All are drawn in their native orientation (bow/tip up). Names come from the [Hud] config.
    /// </summary>
    internal static class GameSprites
    {
        private static readonly Color DefaultShipColor = new Color(1f, 0.62f, 0.15f, 1f);

        private static bool s_loaded;

        internal static Sprite Ship;
        internal static Color ShipColor = DefaultShipColor;
        /// <summary>Area of the visible shape inside the ship sprite (0..1, top-left origin).</summary>
        internal static Rect ShipVisible = new Rect(0f, 0f, 1f, 1f);

        internal static Sprite Arrow;

        internal static Sprite Oar;

        internal static void Reset() => s_loaded = false;

        internal static void TryLoad()
        {
            if (s_loaded || !Hud.instance)
                return;
            s_loaded = true;

            Ship = Find(SailingTogetherPlugin.HudShipSprite.Value, out Image shipImage);
            ShipColor = shipImage ? shipImage.color : DefaultShipColor;
            // "ship_top" has a shadow/margin around it: measured on the exported PNG (171x256).
            ShipVisible = Ship && Ship.name == "ship_top"
                ? new Rect(61f / 171f, 29f / 256f, 49f / 171f, 173f / 256f)
                : new Rect(0f, 0f, 1f, 1f);

            Arrow = Find(SailingTogetherPlugin.HudArrowSprite.Value, out _);

            Oar = Find(SailingTogetherPlugin.HudOarSprite.Value, out _);

            SailingTogetherPlugin.Log.LogInfo($"HUD: ship = '{(Ship ? Ship.name : "built-in")}', arrow = '{(Arrow ? Arrow.name : "built-in")}', oar = '{(Oar ? Oar.name : "none")}'");
        }

        private static Sprite Find(string spriteName, out Image hudImage)
        {
            hudImage = null;
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;
            spriteName = spriteName.Trim();

            // Prefer the instance used by the sailing HUD (it carries the game's color).
            Hud hud = Hud.instance;
            if (hud && hud.m_shipHudRoot)
            {
                foreach (Image image in hud.m_shipHudRoot.GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite && image.sprite.name == spriteName)
                    {
                        hudImage = image;
                        return image.sprite;
                    }
                }
            }

            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite && sprite.name == spriteName)
                    return sprite;
            }

            SailingTogetherPlugin.Log.LogWarning($"HUD: sprite '{spriteName}' not found.");
            return null;
        }

        /// <summary>
        /// Draws an (atlas) sprite with IMGUI, rotated around its center.
        /// <paramref name="angle"/> is in degrees, clockwise on screen; <paramref name="guiScale"/>
        /// is the scale applied to GUI.matrix.
        /// </summary>
        internal static void Draw(Rect rect, Sprite sprite, float angle, Color color, float guiScale)
        {
            Draw(rect, sprite, angle, rect.center, color, guiScale);
        }

        /// <summary>Same as <see cref="Draw(Rect, Sprite, float, Color, float)"/>, rotating around <paramref name="pivotPoint"/>.</summary>
        internal static void Draw(Rect rect, Sprite sprite, float angle, Vector2 pivotPoint, Color color, float guiScale)
        {
            Texture texture = sprite.texture;
            Rect tr = sprite.textureRect;
            Rect uv = new Rect(tr.x / texture.width, tr.y / texture.height, tr.width / texture.width, tr.height / texture.height);

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            if (angle != 0f)
            {
                Vector3 pivot = new Vector3(pivotPoint.x * guiScale, pivotPoint.y * guiScale, 0f);
                GUI.matrix = Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, angle), Vector3.one)
                             * Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one)
                             * previousMatrix;
            }
            GUI.color = color;
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }
    }
}
