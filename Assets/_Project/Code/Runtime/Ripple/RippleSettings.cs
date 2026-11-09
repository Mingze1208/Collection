using UnityEngine;

namespace Collection.Runtime.Ripple
{
    /// <summary>
    /// 涟漪参数。玩法数值不硬编码：A 组基线 = 1 m/s × 2 s（one-pager v1）。
    /// 换 B 组只需换一份 SO 资源，不改代码。
    /// </summary>
    [CreateAssetMenu(menuName = "Collection/Ripple Settings", fileName = "SO_Ripple")]
    public sealed class RippleSettings : ScriptableObject
    {
        [Header("传播")]
        [Tooltip("声音点速度（m/s）。A 组基线 1；B 组候选 3。")]
        [SerializeField, Min(0.01f)] private float _speed = 1f;

        [Tooltip("声音点存活时间（s）。A 组基线 2；B 组候选 2.5。")]
        [SerializeField, Min(0.05f)] private float _lifetime = 2f;

        [Tooltip("每帧发射的方向数（点集密度）。")]
        [SerializeField, Min(1)] private int _directionCount = 24;

        [Header("尾迹")]
        [Tooltip("每条路径上同时显示的点数（造成尾迹）。")]
        [SerializeField, Min(1)] private int _trailDots = 8;

        [Tooltip("相邻尾迹点的弧长间隔（m）。")]
        [SerializeField, Min(0.01f)] private float _trailSpacing = 0.12f;

        [Tooltip("同屏点数上限，超出后丢弃最旧的一次发声。")]
        [SerializeField, Min(1)] private int _maxDots = 512;

        [Tooltip("单个声音点的显示半径（世界单位）。")]
        [SerializeField, Min(0.001f)] private float _dotRadius = 0.05f;

        [Tooltip("单条路径最多反射次数，防止贴墙时无限递归。")]
        [SerializeField, Min(0)] private int _maxBounces = 8;

        [Header("脚步发声（轻量档）")]
        [Tooltip("落脚时发射的方向数。比唱歌少，控制同屏点数。")]
        [SerializeField, Min(1)] private int _footstepDirectionCount = 12;

        [Tooltip("落脚时每条路径的尾迹点数。")]
        [SerializeField, Min(1)] private int _footstepTrailDots = 4;

        [Header("步态（一步一印，脚用方块）")]
        [Tooltip("一步的距离（m）。")]
        [SerializeField, Min(0.1f)] private float _stepLength = 0.8f;

        [Tooltip("按住方向键时每步的间隔（s）。")]
        [SerializeField, Min(0.05f)] private float _stepInterval = 0.35f;

        [Tooltip("上一步的残影从本体色渐变到灰色所需时间（s）。")]
        [SerializeField, Min(0.1f)] private float _ghostFadeTime = 1.2f;

        [Tooltip("脚方块边长（m）。")]
        [SerializeField, Min(0.02f)] private float _footSize = 0.3f;

        [Header("表现")]
        [SerializeField] private Color _dotColor = new Color(0.72f, 0.95f, 1f, 1f);

        public float Speed => _speed;
        public float Lifetime => _lifetime;
        public int DirectionCount => _directionCount;
        public int TrailDots => _trailDots;
        public float TrailSpacing => _trailSpacing;
        public int MaxDots => _maxDots;
        public float DotRadius => _dotRadius;
        public int MaxBounces => _maxBounces;
        public Color DotColor => _dotColor;

        public int FootstepDirectionCount => _footstepDirectionCount;
        public int FootstepTrailDots => _footstepTrailDots;
        public float StepLength => _stepLength;
        public float StepInterval => _stepInterval;
        public float GhostFadeTime => _ghostFadeTime;
        public float FootSize => _footSize;

        /// <summary>近似可视半径 = speed × lifetime（直线，未计反射）。</summary>
        public float VisualRadius => _speed * _lifetime;
    }
}
