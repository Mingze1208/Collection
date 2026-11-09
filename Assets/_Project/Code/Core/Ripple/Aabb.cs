using System;

namespace Collection.Core.Ripple
{
    /// <summary>
    /// 轴对齐包围盒（墙）。用于声音点的镜面反射求交。
    /// </summary>
    public readonly struct Aabb
    {
        private const float Epsilon = 1e-6f;

        public readonly Vec2 Center;
        public readonly Vec2 HalfSize;

        public Aabb(Vec2 center, Vec2 halfSize)
        {
            Center = center;
            HalfSize = new Vec2(MathF.Abs(halfSize.X), MathF.Abs(halfSize.Y));
        }

        public static Aabb FromMinMax(Vec2 min, Vec2 max)
        {
            var center = new Vec2((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f);
            var half = new Vec2((max.X - min.X) * 0.5f, (max.Y - min.Y) * 0.5f);
            return new Aabb(center, half);
        }

        public float MinX => Center.X - HalfSize.X;
        public float MaxX => Center.X + HalfSize.X;
        public float MinY => Center.Y - HalfSize.Y;
        public float MaxY => Center.Y + HalfSize.Y;

        public bool Contains(Vec2 point) =>
            point.X >= MinX && point.X <= MaxX && point.Y >= MinY && point.Y <= MaxY;

        /// <summary>
        /// 射线与该盒的首次相交（slab 法）。起点在盒内时返回 false，
        /// 避免声音点贴墙生成后被困在墙里反复反弹。
        /// </summary>
        public bool Raycast(Vec2 origin, Vec2 direction, float maxDistance,
            out float distance, out Vec2 normal)
        {
            distance = 0f;
            normal = Vec2.Zero;

            if (maxDistance <= 0f) return false;
            if (Contains(origin)) return false;

            float tMin = 0f;
            float tMax = maxDistance;
            Vec2 hitNormal = Vec2.Zero;

            if (!ClipSlab(origin.X, direction.X, MinX, MaxX,
                    new Vec2(-1f, 0f), new Vec2(1f, 0f), ref tMin, ref tMax, ref hitNormal))
            {
                return false;
            }

            if (!ClipSlab(origin.Y, direction.Y, MinY, MaxY,
                    new Vec2(0f, -1f), new Vec2(0f, 1f), ref tMin, ref tMax, ref hitNormal))
            {
                return false;
            }

            if (tMin <= 1e-5f) return false;
            if (tMin > maxDistance) return false;

            distance = tMin;
            normal = hitNormal;
            return true;
        }

        private static bool ClipSlab(float origin, float direction, float min, float max,
            Vec2 negativeNormal, Vec2 positiveNormal,
            ref float tMin, ref float tMax, ref Vec2 hitNormal)
        {
            if (MathF.Abs(direction) < Epsilon)
            {
                return origin >= min && origin <= max;
            }

            float inv = 1f / direction;
            float t1 = (min - origin) * inv;
            float t2 = (max - origin) * inv;
            Vec2 n1 = negativeNormal;
            Vec2 n2 = positiveNormal;

            if (t1 > t2)
            {
                (t1, t2) = (t2, t1);
                (n1, n2) = (n2, n1);
            }

            if (t1 > tMin)
            {
                tMin = t1;
                hitNormal = n1;
            }

            if (t2 < tMax)
            {
                tMax = t2;
            }

            return tMin <= tMax;
        }
    }
}
