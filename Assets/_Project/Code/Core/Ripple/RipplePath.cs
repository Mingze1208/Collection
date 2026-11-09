using System;
using System.Collections.Generic;

namespace Collection.Core.Ripple
{
    /// <summary>
    /// 一条声音点的运动折线（含若干次镜面反射）。反射不损耗速度，
    /// 因此位置 = 沿折线的弧长，弧长 = speed * t。
    /// </summary>
    public sealed class RipplePath
    {
        private readonly Vec2[] _points;
        private readonly float[] _cumulative;
        private readonly int _bounceCount;

        public RipplePath(IReadOnlyList<Vec2> points, float speed, int bounceCount)
        {
            if (points == null || points.Count == 0) throw new ArgumentException("points 不能为空", nameof(points));

            _points = new Vec2[points.Count];
            _cumulative = new float[points.Count];
            float total = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                _points[i] = points[i];
                if (i > 0) total += Vec2.Distance(points[i - 1], points[i]);
                _cumulative[i] = total;
            }

            Speed = speed;
            _bounceCount = bounceCount;
        }

        public float Speed { get; }

        public Vec2 Origin => _points[0];

        public Vec2 End => _points[_points.Length - 1];

        /// <summary>折线总长度（m）。</summary>
        public float Length => _cumulative[_cumulative.Length - 1];

        /// <summary>反射次数（直线为 0）。不按顶点数推断：终点恰好落在墙上时会少算一次。</summary>
        public int BounceCount => _bounceCount;

        public IReadOnlyList<Vec2> Points => _points;

        /// <summary>按弧长取位置，超出范围时夹取到端点。</summary>
        public Vec2 PositionAt(float distance)
        {
            if (distance <= 0f) return _points[0];
            if (distance >= Length) return _points[_points.Length - 1];

            for (int i = 1; i < _points.Length; i++)
            {
                if (distance > _cumulative[i]) continue;

                float segmentStart = _cumulative[i - 1];
                float segmentLength = _cumulative[i] - segmentStart;
                if (segmentLength <= 1e-6f) return _points[i];

                float t = (distance - segmentStart) / segmentLength;
                return _points[i - 1] + (_points[i] - _points[i - 1]) * t;
            }

            return _points[_points.Length - 1];
        }
    }
}
