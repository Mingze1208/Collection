using UnityEngine;

namespace Collection.Runtime.Enemy
{
    /// <summary>敌人参数。玩法数值进 SO，不硬编码。</summary>
    [CreateAssetMenu(menuName = "Collection/Enemy Settings", fileName = "SO_Enemy")]
    public sealed class EnemySettings : ScriptableObject
    {
        [Header("移动")]
        [Tooltip("直线追击速度（m/s）。")]
        [SerializeField, Min(0.1f)] private float _moveSpeed = 2.5f;

        [Tooltip("到达判定距离（m）。")]
        [SerializeField, Min(0.01f)] private float _arriveDistance = 0.15f;

        [Header("感知")]
        [Tooltip("感知半径 = 敌人半径 + 声音点半径；有点进入该距离即视为“接触到”。")]
        [SerializeField, Min(0.05f)] private float _senseRadius = 0.45f;

        [Tooltip("新声源与当前目标相距超过该值才改向，避免同一个声音反复重置。")]
        [SerializeField, Min(0f)] private float _retargetDistance = 0.3f;

        [Header("接近提示（D-3）")]
        [Tooltip("敌人处于 Seek/Camp 且距离 ≤ 该值时，屏幕边缘出现脉冲（m）。")]
        [SerializeField, Min(0.5f)] private float _pulseRange = 6f;

        [Tooltip("脉冲最慢频率（Hz，在范围边缘）。")]
        [SerializeField, Min(0.1f)] private float _pulseSlowHz = 0.8f;

        [Tooltip("脉冲最快频率（Hz，贴身时）。")]
        [SerializeField, Min(0.1f)] private float _pulseFastHz = 3f;

        [Tooltip("预计被抓前多少秒闪红一次（s）。")]
        [SerializeField, Min(0.05f)] private float _flashLeadTime = 0.3f;

        [Header("致命接触")]
        [Tooltip("敌人与玩家中心距离小于该值即判定 GameOver（约等于两者半径之和）。")]
        [SerializeField, Min(0.05f)] private float _killDistance = 0.55f;

        [Header("卡墙记录")]
        [Tooltip("每隔多久检查一次是否没动（s）。")]
        [SerializeField, Min(0.1f)] private float _stuckCheckInterval = 0.5f;

        [Tooltip("一个检查周期内位移小于该值即记一次卡墙（m）。")]
        [SerializeField, Min(0.001f)] private float _stuckMoveThreshold = 0.1f;

        public float MoveSpeed => _moveSpeed;
        public float ArriveDistance => _arriveDistance;
        public float SenseRadius => _senseRadius;
        public float RetargetDistance => _retargetDistance;
        public float KillDistance => _killDistance;
        public float PulseRange => _pulseRange;
        public float PulseSlowHz => _pulseSlowHz;
        public float PulseFastHz => _pulseFastHz;
        public float FlashLeadTime => _flashLeadTime;
        public float StuckCheckInterval => _stuckCheckInterval;
        public float StuckMoveThreshold => _stuckMoveThreshold;
    }
}
