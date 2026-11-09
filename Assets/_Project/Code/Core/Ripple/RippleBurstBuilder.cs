using System;
using System.Collections.Generic;

namespace Collection.Core.Ripple
{
    /// <summary>
    /// 由声源向多个方向发射声音点，碰到墙做镜面反射，总行程 = speed * lifetime。
    /// 纯函数式，便于 EditMode 单测。
    /// </summary>
    public static class RippleBurstBuilder
    {
        /// <summary>反射后沿新方向推开的距离，防止下一帧又从同一墙面反弹（保持亚毫米级）。</summary>
        public const float Nudge = 1e-4f;

        private const float DistanceEpsilon = 1e-4f;

        /// <summary>均匀向 directionCount 个方向发射。</summary>
        public static RipplePath[] BuildRadial(Vec2 origin, int directionCount, float speed,
            float lifetime, IReadOnlyList<Aabb> walls, int maxBounces)
        {
            if (directionCount < 1) throw new ArgumentOutOfRangeException(nameof(directionCount));

            float totalDistance = MathF.Max(0f, speed * lifetime);
            var paths = new RipplePath[directionCount];

            for (int i = 0; i < directionCount; i++)
            {
                double angle = Math.PI * 2.0 * i / directionCount;
                var direction = new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle));
                paths[i] = BuildPath(origin, direction, totalDistance, speed, walls, maxBounces);
            }

            return paths;
        }

        /// <summary>单方向：走 totalDistance，遇墙镜面反射（强度/速度不变）。</summary>
        public static RipplePath BuildPath(Vec2 origin, Vec2 direction, float totalDistance,
            float speed, IReadOnlyList<Aabb> walls, int maxBounces)
        {
            var points = new List<Vec2>(Math.Max(2, maxBounces + 2)) { origin };

            var heading = direction.Normalized;
            if (heading.Length <= 1e-6f || totalDistance <= 0f)
            {
                points.Add(origin);
                return new RipplePath(points, speed, 0);
            }

            var position = origin;
            float remaining = totalDistance;
            int bounces = 0;

            // segment 0 是直线段，每反射一次多一段，总段数 = maxBounces + 1
            for (int segment = 0; segment <= maxBounces; segment++)
            {
                bool hit = false;
                float nearest = float.MaxValue;
                Vec2 normal = Vec2.Zero;

                if (walls != null)
                {
                    foreach (var wall in walls)
                    {
                        if (!wall.Raycast(position, heading, remaining, out float distance, out Vec2 wallNormal)) continue;
                        if (distance >= nearest) continue;

                        nearest = distance;
                        normal = wallNormal;
                        hit = true;
                    }
                }

                if (!hit)
                {
                    position += heading * remaining;
                    points.Add(position);
                    remaining = 0f;
                    break;
                }

                position += heading * nearest;
                points.Add(position);
                remaining -= nearest;

                if (remaining <= DistanceEpsilon)
                {
                    remaining = 0f;
                    break;
                }

                if (segment == maxBounces)
                {
                    // 反射次数已用尽：不再反射，按当前方向走完，避免无限循环
                    position += heading * remaining;
                    points.Add(position);
                    remaining = 0f;
                    break;
                }

                // 镜面反射：d - 2(d·n)n，速度不变（强度不变）
                heading = Reflect(heading, normal);
                position += heading * Nudge;
                bounces++;
            }

            if (remaining > DistanceEpsilon)
            {
                position += heading * remaining;
                points.Add(position);
            }

            return new RipplePath(points, speed, bounces);
        }

        public static Vec2 Reflect(Vec2 direction, Vec2 normal)
        {
            return direction - normal * (2f * Vec2.Dot(direction, normal));
        }
    }
}
