using System;
using UnityEngine;

namespace Collection.Runtime.Ripple
{
    /// <summary>
    /// 一档发声参数。唱歌与落脚各持一份，互不影响：
    /// 方向数 / 尾迹点数 / 速度 / 存活 / 颜色 / 冷却。
    /// </summary>
    [Serializable]
    public sealed class RippleProfile
    {
        [Tooltip("同时发射的方向数（点集密度）。")]
        [SerializeField, Min(1)] private int _directionCount = 24;

        [Tooltip("每条路径的尾迹点数（决定尾迹长度：点数 × 全局间距）。")]
        [SerializeField, Min(1)] private int _trailDots = 8;

        [Tooltip("声音点速度（m/s）。")]
        [SerializeField, Min(0.01f)] private float _speed = 3f;

        [Tooltip("声音点存活时间（s）。半径 ≈ 速度 × 存活。")]
        [SerializeField, Min(0.05f)] private float _lifetime = 1.2f;

        [Tooltip("这一档声音点的颜色。")]
        [SerializeField] private Color _color = new Color(0.72f, 0.95f, 1f, 1f);

        [Tooltip("两次发声的最小间隔（s）。0 = 无冷却。")]
        [SerializeField, Min(0f)] private float _cooldown = 0f;

        public RippleProfile()
        {
        }

        public RippleProfile(int directionCount, int trailDots, float speed, float lifetime,
            float cooldown, Color color)
        {
            _directionCount = directionCount;
            _trailDots = trailDots;
            _speed = speed;
            _lifetime = lifetime;
            _cooldown = cooldown;
            _color = color;
        }

        public int DirectionCount => _directionCount;
        public int TrailDots => _trailDots;
        public float Speed => _speed;
        public float Lifetime => _lifetime;
        public Color Color => _color;
        public float Cooldown => _cooldown;

        /// <summary>近似可视半径 = 速度 × 存活（直线，未计反射）。</summary>
        public float VisualRadius => _speed * _lifetime;

        /// <summary>一次发声消耗的点数。</summary>
        public int DotCount => _directionCount * _trailDots;
    }
}
