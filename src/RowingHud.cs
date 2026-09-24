using System.Collections.Generic;
using UnityEngine;

namespace SailingTogether
{
    /// <summary>
    /// Top-down ship HUD (bow up).
    ///   Helmsman: every bench, who is sitting and each rower's direction.
    ///   Rower: only their own bench and their rowing direction.
    /// </summary>
    internal class RowingHud : MonoBehaviour
    {
        private enum SeatState { Free, Player, Simulated, Local }

        private struct SeatView
        {
            public SeatState State;
            public float Row;
        }

        private static readonly Color HullFill = new Color(0.42f, 0.28f, 0.15f, 0.85f);
        private static readonly Color HullEdge = new Color(0.16f, 0.09f, 0.04f, 1f);
        private static readonly Color FreeColor = new Color(0.85f, 0.85f, 0.85f, 0.55f);
        private static readonly Color PlayerColor = new Color(0.35f, 0.7f, 1f, 1f);
        private static readonly Color SimColor = new Color(0.75f, 0.5f, 1f, 1f);
        private static readonly Color LocalColor = new Color(1f, 0.82f, 0.3f, 1f);
        private static readonly Color HelmColor = new Color(1f, 0.95f, 0.6f, 1f);
        private static readonly Color ForwardColor = new Color(0.45f, 1f, 0.45f, 1f);
        private static readonly Color BackColor = new Color(1f, 0.45f, 0.35f, 1f);
        private static readonly Color InactiveColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

        private const float MaxHullHeight = 140f;
        private const float MaxHullWidth = 52f;
        private const float SeatSize = 9f;
        private const float ArrowSize = 17f;
        private const float ArrowGap = 4f;
        private const float OarHeight = 20f;

        private readonly Dictionary<ShipLayout, Texture2D> _hullTextures = new Dictionary<ShipLayout, Texture2D>();
        private readonly List<SeatView> _views = new List<SeatView>();
        private Texture2D _circle, _ring, _arrowUp, _arrowDown;

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint)
                return;
            if (!SailingTogetherPlugin.Enabled.Value || !SailingTogetherPlugin.ShowHud.Value)
                return;

            Player player = Player.m_localPlayer;
            if (!player || !Hud.instance || Hud.IsUserHidden() || Menu.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen())
                return;

            bool pilot = false;
            Ship ship = null;
            if (player.GetDoodadController() is ShipControlls controls && controls.m_ship)
            {
                ship = controls.m_ship;
                pilot = true;
            }
            else
            {
                ship = RowerInput.GetSeatedShip(player);
            }
            if (!ship)
                return;

            ShipLayout layout = ShipLayout.Get(ship);
            if (pilot && layout.Seats.Count == 0)
                return;

            EnsureResources();
            GameSprites.TryLoad();
            float scale = Mathf.Max(1f, Screen.height / 1080f) * SailingTogetherPlugin.HudScale.Value;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            BuildViews(ship, layout, player, pilot);
            Draw(ship, layout, pilot, scale);
        }

        private void BuildViews(Ship ship, ShipLayout layout, Player local, bool pilot)
        {
            _views.Clear();
            for (int i = 0; i < layout.Seats.Count; i++)
                _views.Add(new SeatView { State = SeatState.Free });

            Transform t = ship.transform;
            long helmUser = ship.m_shipControlls ? ship.m_shipControlls.GetUser() : 0L;

            List<Player> players = ShipAccess.Players(ship);
            if (players != null)
            {
                foreach (Player p in players)
                {
                    if (!p || p.GetPlayerID() == helmUser)
                        continue;
                    bool isLocal = p == local;
                    if (!pilot && !isLocal)
                        continue;
                    if (!RowerInput.TryGetRower(p, out float row))
                        continue;
                    int index = layout.NearestSeat(t.InverseTransformPoint(p.transform.position), 1f);
                    if (index < 0)
                        continue;
                    _views[index] = new SeatView
                    {
                        State = isLocal ? SeatState.Local : SeatState.Player,
                        Row = row,
                    };
                }
            }

            if (pilot)
            {
                foreach (DebugPanel.SimSeat sim in DebugPanel.GetSimulatedSeats(ship))
                {
                    if (sim.Row == 0f)
                        continue;
                    int index = layout.NearestSeat(new Vector3(sim.LocalX, layout.Seats.Count > 0 ? layout.Seats[0].Local.y : 0f, sim.LocalZ), 1f);
                    if (index < 0 || _views[index].State != SeatState.Free)
                        continue;
                    _views[index] = new SeatView { State = SeatState.Simulated, Row = sim.Row };
                }
            }
        }

        private void Draw(Ship ship, ShipLayout layout, bool pilot, float scale)
        {
            // Fixed height; width follows the icon's aspect ratio (never stretch the game sprite).
            float hullH = MaxHullHeight;
            float hullW;
            if (GameSprites.Ship)
            {
                Rect v = GameSprites.ShipVisible;
                Rect r = GameSprites.Ship.rect;
                hullW = hullH * (v.width * r.width) / (v.height * r.height);
            }
            else
            {
                hullW = Mathf.Min(hullH * layout.Width / layout.Length, MaxHullWidth);
            }
            float ppmX = hullW / layout.Width;
            float ppmZ = hullH / layout.Length;

            float arrowW = ArrowSize;
            if (GameSprites.Arrow)
                arrowW = ArrowSize * GameSprites.Arrow.rect.width / Mathf.Max(1f, GameSprites.Arrow.rect.height);
            float areaW = hullW + (arrowW + ArrowGap) * 2f;

            float screenW = Screen.width / scale;
            float screenH = Screen.height / scale;
            Rect hull = new Rect(
                screenW - SailingTogetherPlugin.HudOffsetX.Value - (areaW + hullW) * 0.5f,
                screenH - SailingTogetherPlugin.HudOffsetY.Value - hullH,
                hullW, hullH);

            if (GameSprites.Ship)
            {
                // Fit the icon's visible shape (without the shadow) to the hull outline.
                Rect visible = GameSprites.ShipVisible;
                float iconW = hullW / visible.width;
                float iconH = hullH / visible.height;
                Rect icon = new Rect(hull.x - visible.x * iconW, hull.y - visible.y * iconH, iconW, iconH);
                GameSprites.Draw(icon, GameSprites.Ship, 0f, GameSprites.ShipColor, scale);
            }
            else
            {
                DrawTinted(hull, GetHullTexture(layout, hullW, hullH), Color.white);
            }

            Vector2 ToGui(Vector3 local) => new Vector2(
                hull.x + (local.x - layout.MinX) * ppmX,
                hull.y + (layout.MaxZ - local.z) * ppmZ);

            bool allowed = ShipAccess.RowingAllowed(ship.GetSpeedSetting());

            if (layout.HasHelm)
            {
                Vector2 h = ToGui(layout.HelmLocal);
                bool manned = ship.m_shipControlls && ship.m_shipControlls.HaveValidUser();
                DrawTinted(Centered(h, SeatSize), manned ? _circle : _ring, HelmColor);
            }

            for (int i = 0; i < layout.Seats.Count; i++)
            {
                SeatView view = _views[i];
                if (!pilot && view.State != SeatState.Local)
                    continue;

                Vector2 c = ToGui(layout.Seats[i].Local);
                Rect seatRect = Centered(c, SeatSize);
                switch (view.State)
                {
                    case SeatState.Free:
                        DrawTinted(seatRect, _ring, FreeColor);
                        continue;
                    case SeatState.Player:
                        DrawTinted(seatRect, _circle, PlayerColor);
                        break;
                    case SeatState.Simulated:
                        DrawTinted(seatRect, _circle, SimColor);
                        break;
                    case SeatState.Local:
                        DrawTinted(Centered(c, SeatSize + 4f), _ring, Color.white);
                        DrawTinted(seatRect, _circle, LocalColor);
                        break;
                }

                bool rowing = Mathf.Abs(view.Row) >= 0.05f;
                DrawOar(c, rowing ? view.Row : 0f, allowed, scale);

                if (!rowing)
                    continue;

                // Arrow outside the hull, level with the bench (center bench: above it).
                float side = ShipAccess.SideOf(layout.Seats[i].Local.x);
                Vector2 a = side > 0f ? new Vector2(hull.xMax + ArrowGap + arrowW * 0.5f, c.y)
                    : side < 0f ? new Vector2(hull.x - ArrowGap - arrowW * 0.5f, c.y)
                    : new Vector2(c.x, c.y - (OarHeight + ArrowSize) * 0.5f);
                Color color = !allowed ? InactiveColor : view.Row > 0f ? ForwardColor : BackColor;
                color.a *= Mathf.Lerp(0.5f, 1f, Mathf.Abs(view.Row));
                if (GameSprites.Arrow)
                {
                    Rect arrowRect = new Rect(a.x - arrowW * 0.5f, a.y - ArrowSize * 0.5f, arrowW, ArrowSize);
                    GameSprites.Draw(arrowRect, GameSprites.Arrow, view.Row > 0f ? 0f : 180f, color, scale);
                }
                else
                {
                    DrawTinted(Centered(a, ArrowSize), view.Row > 0f ? _arrowUp : _arrowDown, color);
                }
            }
        }

        /// <summary>
        /// Upright oar on the bench, swaying like the vanilla helm oar (Ship.UpdateRudder):
        /// forward sin(t*6)*20°, backward sin(-t*3)*40°. Idle: at rest.
        /// </summary>
        private static void DrawOar(Vector2 seat, float row, bool allowed, float scale)
        {
            Sprite oar = GameSprites.Oar;
            if (!oar)
                return;

            float angle = 0f;
            if (row != 0f && allowed)
                angle = row > 0f ? Mathf.Sin(Time.time * 6f) * 20f : Mathf.Sin(Time.time * -3f) * 40f;

            float oarW = OarHeight * oar.rect.width / oar.rect.height;
            Rect rect = new Rect(seat.x - oarW * 0.5f, seat.y - OarHeight * 0.5f, oarW, OarHeight);
            GameSprites.Draw(rect, oar, angle, allowed ? Color.white : InactiveColor, scale);
        }

        private static Rect Centered(Vector2 c, float size) => new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size);

        private static void DrawTinted(Rect rect, Texture2D texture, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        private void EnsureResources()
        {
            if (_circle)
                return;
            _circle = Procedural(32, (u, v) => Coverage(0.46f - Mathf.Sqrt(u * u + v * v), 32));
            _ring = Procedural(32, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                return Mathf.Min(Coverage(0.46f - d, 32), Coverage(d - 0.32f, 32));
            });
            // Triangle: (u, v) in [-0.5, 0.5], v up.
            _arrowUp = Procedural(32, (u, v) => Coverage(Mathf.Min(v + 0.4f, (0.4f - v) * 0.5f - Mathf.Abs(u) * 1.0f), 32));
            _arrowDown = Procedural(32, (u, v) => Coverage(Mathf.Min(-v + 0.4f, (0.4f + v) * 0.5f - Mathf.Abs(u) * 1.0f), 32));

        }

        private Texture2D GetHullTexture(ShipLayout layout, float width, float height)
        {
            if (_hullTextures.TryGetValue(layout, out Texture2D texture) && texture)
                return texture;

            int w = Mathf.Clamp(Mathf.RoundToInt(width * 2f), 32, 256);
            int h = Mathf.Clamp(Mathf.RoundToInt(height * 2f), 64, 512);
            texture = NewTexture(w, h);
            Color32[] pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float t = (y + 0.5f) / h * 2f - 1f; // -1 stern, +1 bow
                float half = t >= 0f
                    ? Mathf.Pow(Mathf.Max(0f, 1f - t * t), 0.6f)
                    : Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(-t, 2.5f)), 0.55f);
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                    float edgePx = (half - u) * w * 0.5f; // distance to the edge, in pixels
                    Color c;
                    if (edgePx <= -1f)
                        c = Color.clear;
                    else if (edgePx < 3f)
                        c = new Color(HullEdge.r, HullEdge.g, HullEdge.b, HullEdge.a * Mathf.Clamp01(edgePx + 1f));
                    else
                    {
                        c = HullFill;
                        // Deck planks and keel.
                        if (Mathf.Repeat(y, 10f) < 1f)
                            c *= 0.85f;
                        if (u * w * 0.5f < 1f)
                            c = Color.Lerp(c, HullEdge, 0.5f);
                        c.a = HullFill.a;
                    }
                    pixels[y * w + x] = c;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            _hullTextures[layout] = texture;
            return texture;
        }

        private static float Coverage(float signedDistance, int size) => Mathf.Clamp01(signedDistance * size + 0.5f);

        private static Texture2D Procedural(int size, System.Func<float, float, float> alpha)
        {
            Texture2D texture = NewTexture(size, size);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size - 0.5f;
                    float v = (y + 0.5f) / size - 0.5f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha(u, v));
                }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D NewTexture(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }
}
