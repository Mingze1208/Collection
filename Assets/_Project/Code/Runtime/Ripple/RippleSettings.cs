using UnityEngine;

namespace Collection.Runtime.Ripple
{
    /// <summary>
    /// 涟漪参数。唱歌与落脚各有一档完整参数（RippleProfile），另有少量两组共用的项。
    /// 玩法数值不硬编码，全部在这里调。
    /// </summary>
    [CreateAssetMenu(menuName = "Collection/Ripple Settings", fileName = "SO_Ripple")]
    public sealed class RippleSettings : ScriptableObject
    {
        [Header("唱歌（满档，空格）")]
        [Tooltip("唱歌的涟漪：方向数、尾迹、速度、存活、颜色、冷却。")]
        [SerializeField]
        private RippleProfile _voice = new RippleProfile(
            24, 8, 3f, 1.2f, 0.5f, new Color(0.72f, 0.95f, 1f, 1f));

        [Header("落脚（轻量档，走路自动）")]
        [Tooltip("走路的涟漪：点更少、更淡；冷却通常为 0（由步频控制）。")]
        [SerializeField]
        private RippleProfile _footstep = new RippleProfile(
            12, 4, 3f, 1.2f, 0f, new Color(0.62f, 0.86f, 0.98f, 0.9f));

        [Header("两组共用")]
        [Tooltip("相邻尾迹点的弧长间隔（m）。")]
        [SerializeField, Min(0.01f)] private float _trailSpacing = 0.12f;

        [Tooltip("同屏点数上限，超出后丢弃最旧的一次发声。")]
        [SerializeField, Min(1)] private int _maxDots = 512;

        [Tooltip("单个声音点的显示半径（世界单位）。")]
        [SerializeField, Min(0.001f)] private float _dotRadius = 0.05f;

        [Tooltip("单条路径最多反射次数，防止贴墙时无限递归。")]
        [SerializeField, Min(0)] private int _maxBounces = 8;

        [Header("步态（一步一印，脚用方块）")]
        [Tooltip("一步的距离（m）。")]
        [SerializeField, Min(0.1f)] private float _stepLength = 0.8f;

        [Tooltip("按住方向键时每步的间隔（s）。")]
        [SerializeField, Min(0.05f)] private float _stepInterval = 0.35f;

        [Tooltip("上一步的残影从本体色渐变到灰色所需时间（s）。")]
        [SerializeField, Min(0.1f)] private float _ghostFadeTime = 1.2f;

        [Tooltip("脚方块边长（m）。")]
        [SerializeField, Min(0.02f)] private float _footSize = 0.3f;

        [Header("屏息静步")]
        [Tooltip("一次屏息可持续多久（s）。")]
        [SerializeField, Min(0.5f)] private float _breathDuration = 3f;

        [Tooltip("不屏息时呼吸恢复速度（每秒恢复多少秒的屏息）。")]
        [SerializeField, Min(0.1f)] private float _breathRecovery = 1.5f;

        public RippleProfile Voice => _voice;
        public RippleProfile Footstep => _footstep;

        public float TrailSpacing => _trailSpacing;
        public int MaxDots => _maxDots;
        public float DotRadius => _dotRadius;
        public int MaxBounces => _maxBounces;

        public float StepLength => _stepLength;
        public float StepInterval => _stepInterval;
        public float GhostFadeTime => _ghostFadeTime;
        public float FootSize => _footSize;
        public float BreathDuration => _breathDuration;
        public float BreathRecovery => _breathRecovery;
    }
}
