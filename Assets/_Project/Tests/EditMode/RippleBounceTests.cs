using System;
using System.Collections.Generic;
using Collection.Core.Ripple;
using NUnit.Framework;

namespace Collection.Tests.EditMode
{
    /// <summary>
    /// 涟漪点集的纯数学单测：不依赖场景与物理，秒级可跑。
    /// 参数取 one-pager v1 的 A 组基线：1 m/s × 2 s。
    /// </summary>
    public sealed class RippleBounceTests
    {
        private const float Speed = 1f;
        private const float Lifetime = 2f;
        private const float Tolerance = 1e-3f;

        // 一面竖墙：x = 2 处，向两侧延伸很远
        private static readonly Aabb VerticalWall =
            Aabb.FromMinMax(new Vec2(2f, -10f), new Vec2(2.5f, 10f));

        [Test]
        public void 空房间直线路径长度等于速度乘存活时间()
        {
            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, new Vec2(1f, 0f), Speed * Lifetime, Speed,
                Array.Empty<Aabb>(), 4);

            Assert.AreEqual(2f, path.Length, Tolerance, "A 组应为 1 m/s × 2 s = 2 m");
            Assert.AreEqual(0, path.BounceCount, "空房间不应有反射");
            Assert.AreEqual(2f, path.End.X, Tolerance);
            Assert.AreEqual(0f, path.End.Y, Tolerance);
        }

        [Test]
        public void 垂直入射一面竖墙后原路返回()
        {
            // 行程 4 m：走 2 m 撞到 x=2 的墙，反射后再走 2 m 回到 x=0
            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, new Vec2(1f, 0f), 4f, Speed,
                new[] { VerticalWall }, 4);

            Assert.AreEqual(1, path.BounceCount);
            Assert.AreEqual(2f, path.Points[1].X, Tolerance, "应在墙面 x=2 处反射");
            Assert.AreEqual(0f, path.End.X, Tolerance, "反射后回到 x=0");
            Assert.AreEqual(0f, path.End.Y, Tolerance);
            Assert.AreEqual(4f, path.Length, Tolerance, "反射不损耗行程");
        }

        [Test]
        public void 四十五度入射后水平分量保持竖直分量反向()
        {
            var floor = Aabb.FromMinMax(new Vec2(-10f, 1f), new Vec2(10f, 1.5f));
            var dir = new Vec2(1f, 1f).Normalized;

            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, dir, 4f, Speed, new[] { floor }, 4);

            // 从 (0,0) 以 45° 撞 y=1 的地板 → 反射点 (1,1)，之后方向 (1,-1)
            var hit = path.Points[1];
            Assert.AreEqual(1f, hit.X, Tolerance);
            Assert.AreEqual(1f, hit.Y, Tolerance);
            Assert.AreEqual(1, path.BounceCount);

            Assert.Greater(path.End.X, hit.X, "水平分量应保持");
            Assert.Less(path.End.Y, hit.Y, "竖直分量应反向");
            Assert.AreEqual(4f, path.Length, Tolerance);
        }

        [Test]
        public void 反射次数受上限约束且不会无限循环()
        {
            // 两条极近的平行墙，极易多次反射
            var left = Aabb.FromMinMax(new Vec2(-0.3f, -10f), new Vec2(-0.2f, 10f));
            var right = Aabb.FromMinMax(new Vec2(0.2f, -10f), new Vec2(0.3f, 10f));

            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, new Vec2(1f, 0f), 50f, Speed, new[] { left, right }, 6);

            Assert.LessOrEqual(path.BounceCount, 6, "反射次数不能超过 maxBounces");
            Assert.LessOrEqual(path.Length, 50f + Tolerance + 6 * RippleBurstBuilder.Nudge);
        }

        [Test]
        public void 采样点不会落在墙体内部()
        {
            var walls = new[]
            {
                VerticalWall,
                Aabb.FromMinMax(new Vec2(-10f, -10f), new Vec2(10f, -9.5f)),
            };

            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, new Vec2(1f, -1f).Normalized, 6f, Speed, walls, 8);

            for (float d = 0f; d <= path.Length; d += 0.01f)
            {
                var p = path.PositionAt(d);
                foreach (var wall in walls)
                {
                    Assert.IsFalse(wall.Contains(p), $"弧长 {d:0.00} 处的点 {p} 落在墙内");
                }
            }
        }

        [Test]
        public void 贴墙生成时不会卡在墙里()
        {
            // 起点就在墙体内：该墙被跳过，声音点应能正常飞出
            var startingInside = new Aabb(Vec2.Zero, new Vec2(0.05f, 0.05f));

            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, new Vec2(1f, 0f), 2f, Speed, new[] { startingInside }, 4);

            Assert.AreEqual(0, path.BounceCount);
            Assert.AreEqual(2f, path.End.X, Tolerance);
        }

        [Test]
        public void 径向发射方向数正确且每条行程一致()
        {
            var walls = new[] { VerticalWall };

            var paths = RippleBurstBuilder.BuildRadial(
                Vec2.Zero, 24, Speed, Lifetime, walls, 8);

            Assert.AreEqual(24, paths.Length);
            foreach (var path in paths)
            {
                // 撞墙的路径行程仍为 speed*lifetime，未撞墙的同样
                Assert.AreEqual(2f, path.Length, Tolerance, "A 组每条声音点总行程应为 2 m");
                Assert.AreEqual(Vec2.Zero, path.Origin);
            }
        }

        [Test]
        public void 弧长定位在端点外会被夹取()
        {
            var path = RippleBurstBuilder.BuildPath(
                Vec2.Zero, new Vec2(1f, 0f), 2f, Speed, Array.Empty<Aabb>(), 4);

            Assert.AreEqual(Vec2.Zero, path.PositionAt(-5f));
            Assert.AreEqual(path.End, path.PositionAt(99f));
            Assert.AreEqual(1f, path.PositionAt(1f).X, Tolerance);
        }
    }
}
