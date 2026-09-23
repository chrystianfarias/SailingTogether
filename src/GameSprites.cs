using UnityEngine;
using UnityEngine.UI;

namespace SailingTogether
{
    /// <summary>
    /// Ícones do próprio jogo usados na HUD: por padrão "ship_top" (barco visto de cima, o mesmo
    /// do indicador de vento) e "winddirection" (seta do vento). Ambos são desenhados na
    /// orientação nativa (proa/ponta para cima). Os nomes vêm da config [Hud] ShipSprite / ArrowSprite.
    /// </summary>
    internal static class GameSprites
    {
        private static readonly Color DefaultShipColor = new Color(1f, 0.62f, 0.15f, 1f);

        private static bool s_loaded;

        internal static Sprite Ship;
        internal static Color ShipColor = DefaultShipColor;
        /// <summary>Área da forma visível dentro do sprite do barco (0..1, origem no topo-esquerdo).</summary>
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
            // "ship_top" tem sombra/margem em volta: medido no PNG exportado (171x256).
            ShipVisible = Ship && Ship.name == "ship_top"
                ? new Rect(61f / 171f, 29f / 256f, 49f / 171f, 173f / 256f)
                : new Rect(0f, 0f, 1f, 1f);

            Arrow = Find(SailingTogetherPlugin.HudArrowSprite.Value, out _);

            Oar = Find(SailingTogetherPlugin.HudOarSprite.Value, out _);

            SailingTogetherPlugin.Log.LogInfo($"HUD: barco = '{(Ship ? Ship.name : "desenho próprio")}', seta = '{(Arrow ? Arrow.name : "desenho próprio")}', remo = '{(Oar ? Oar.name : "nenhum")}'");
        }

        private static Sprite Find(string spriteName, out Image hudImage)
        {
            hudImage = null;
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;
            spriteName = spriteName.Trim();

            // Preferência: a instância usada no HUD de navegação (traz a cor do jogo).
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

            SailingTogetherPlugin.Log.LogWarning($"HUD: sprite '{spriteName}' não encontrado.");
            return null;
        }

        /// <summary>
        /// Desenha um sprite (de atlas) no IMGUI, girado em volta do centro.
        /// <paramref name="angle"/> em graus, sentido horário na tela; <paramref name="guiScale"/>
        /// é a escala aplicada em GUI.matrix.
        /// </summary>
        internal static void Draw(Rect rect, Sprite sprite, float angle, Color color, float guiScale)
        {
            Draw(rect, sprite, angle, rect.center, color, guiScale);
        }

        /// <summary>Igual a <see cref="Draw(Rect, Sprite, float, Color, float)"/>, girando em volta de <paramref name="pivotPoint"/>.</summary>
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
