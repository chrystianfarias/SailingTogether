using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace SailingTogether
{
    internal static class ShipAccess
    {
        internal static readonly AccessTools.FieldRef<Ship, List<Player>> Players =
            AccessTools.FieldRefAccess<Ship, List<Player>>("m_players");

        internal static float SideOf(float localX)
        {
            return Mathf.Abs(localX) < SailingTogetherPlugin.CenterDeadZone.Value ? 0f : Mathf.Sign(localX);
        }

        internal static bool RowingAllowed(Ship.Speed speed)
        {
            switch (speed)
            {
                case Ship.Speed.Slow:
                    return true;
                case Ship.Speed.Back:
                    return SailingTogetherPlugin.RowInBack.Value;
                case Ship.Speed.Stop:
                    return SailingTogetherPlugin.RowInStop.Value;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Geometry of a ship type in the Ship's local space: benches, helm and hull outline.
    /// Cached per prefab (every instance of the same ship is identical).
    /// </summary>
    internal class ShipLayout
    {
        internal class Seat
        {
            public Vector3 Local;
        }

        private static readonly Dictionary<string, ShipLayout> s_cache = new Dictionary<string, ShipLayout>();

        public readonly List<Seat> Seats = new List<Seat>();
        public bool HasHelm;
        public Vector3 HelmLocal;
        public float MinX, MaxX, MinZ, MaxZ;

        public float Length => MaxZ - MinZ;
        public float Width => MaxX - MinX;

        internal static ShipLayout Get(Ship ship)
        {
            string key = Utils.GetPrefabName(ship.gameObject);
            if (!s_cache.TryGetValue(key, out ShipLayout layout))
            {
                layout = Build(ship);
                s_cache[key] = layout;
            }
            return layout;
        }

        private static ShipLayout Build(Ship ship)
        {
            Transform t = ship.transform;
            ShipLayout layout = new ShipLayout();

            foreach (Chair chair in ship.GetComponentsInChildren<Chair>())
            {
                Transform point = chair.m_attachPoint ? chair.m_attachPoint : chair.transform;
                layout.Seats.Add(new Seat { Local = t.InverseTransformPoint(point.position) });
            }
            // Bow to stern, left before right.
            layout.Seats.Sort((a, b) =>
                Mathf.Abs(a.Local.z - b.Local.z) > 0.05f ? b.Local.z.CompareTo(a.Local.z) : a.Local.x.CompareTo(b.Local.x));

            if (ship.m_shipControlls)
            {
                Transform helm = ship.m_shipControlls.m_attachPoint ? ship.m_shipControlls.m_attachPoint : ship.m_shipControlls.transform;
                layout.HasHelm = true;
                layout.HelmLocal = t.InverseTransformPoint(helm.position);
            }

            // Outline: the float collider box, expanded to contain the benches and the helm.
            layout.MinX = layout.MinZ = float.MaxValue;
            layout.MaxX = layout.MaxZ = float.MinValue;
            if (ship.m_floatCollider)
            {
                BoxCollider box = ship.m_floatCollider;
                Vector3 half = box.size * 0.5f;
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        layout.Encapsulate(t.InverseTransformPoint(box.transform.TransformPoint(box.center + new Vector3(half.x * sx, 0f, half.z * sz))), 0f);
            }
            foreach (Seat seat in layout.Seats)
                layout.Encapsulate(seat.Local, 0.6f);
            if (layout.HasHelm)
                layout.Encapsulate(layout.HelmLocal, 0.6f);
            if (layout.MinX > layout.MaxX)
            {
                layout.MinX = -1.5f; layout.MaxX = 1.5f; layout.MinZ = -4f; layout.MaxZ = 4f;
            }
            return layout;
        }

        private void Encapsulate(Vector3 p, float margin)
        {
            MinX = Mathf.Min(MinX, p.x - margin);
            MaxX = Mathf.Max(MaxX, p.x + margin);
            MinZ = Mathf.Min(MinZ, p.z - margin);
            MaxZ = Mathf.Max(MaxZ, p.z + margin);
        }

        internal int NearestSeat(Vector3 local, float maxDistance)
        {
            int best = -1;
            float bestDist = maxDistance * maxDistance;
            for (int i = 0; i < Seats.Count; i++)
            {
                Vector3 d = Seats[i].Local - local;
                d.y *= 0.5f; // the sitting animation offsets more vertically
                float dist = d.sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = i;
                }
            }
            return best;
        }
    }
}
