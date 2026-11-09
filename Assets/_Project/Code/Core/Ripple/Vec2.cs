using System;

namespace Collection.Core.Ripple
{
    /// <summary>
    /// 与引擎无关的二维向量。Game.Core 不允许引用 UnityEngine，故自带一份最小实现。
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0f, 0f);

        public float Length => MathF.Sqrt(X * X + Y * Y);

        public Vec2 Normalized
        {
            get
            {
                float length = Length;
                return length > 1e-6f ? new Vec2(X / length, Y / length) : Zero;
            }
        }

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;

        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);

        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);

        public static Vec2 operator *(Vec2 a, float scale) => new Vec2(a.X * scale, a.Y * scale);

        public static Vec2 operator *(float scale, Vec2 a) => a * scale;

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
