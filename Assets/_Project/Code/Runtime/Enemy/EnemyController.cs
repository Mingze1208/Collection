using Collection.Runtime.Ripple;
using UnityEngine;

namespace Collection.Runtime.Enemy
{
    /// <summary>
    /// 敌人：只靠听。碰到任一涟漪点就记下该点所属的声源坐标，直奔过去，到达后驻留，
    /// 直到下一次听到声音。移动用直线 + 物理滑动（简单避障），并记录是否卡墙。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyController : MonoBehaviour
    {
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private EnemySettings _settings;

        private Rigidbody2D _body;
        private Vector2 _target;
        private float _seekStartTime;
        private Vector2 _stuckCheckPosition;
        private float _stuckTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;
        public Vector2 Target => _target;
        public Vector2 LastSensedSource { get; private set; }

        /// <summary>最近一次 Seek 到 Camp 的耗时（s）。</summary>
        public float LastSeekSeconds { get; private set; } = -1f;

        /// <summary>所有追击里最长的一次耗时（s），用于验收“&lt; 3 s 到达”。</summary>
        public float MaxSeekSeconds { get; private set; }

        /// <summary>累计被判定的“卡住”次数（0.5 s 内位移不足阈值）。</summary>
        public int StuckCount { get; private set; }

        public int SeekCount { get; private set; }
        public int CampCount { get; private set; }

        /// <summary>致命接触距离（m）。</summary>
        public float KillDistance => _settings != null ? _settings.KillDistance : 0.55f;

        /// <summary>GameOver 时停住：清速度并停掉状态机。</summary>
        public void Stop()
        {
            if (_body != null)
            {
                _body.linearVelocity = Vector2.zero;
                _target = _body.position;
            }

            enabled = false;
        }

        /// <summary>重试时回到出生点并复位状态。</summary>
        public void ResetTo(Vector2 position)
        {
            transform.position = position;
            if (_body != null)
            {
                _body.position = position;
                _body.linearVelocity = Vector2.zero;
            }

            _target = position;
            State = EnemyState.Idle;
            _stuckTimer = 0f;
            _stuckCheckPosition = position;
            enabled = true;
        }

        public void Initialize(RippleSystem ripple, EnemySettings settings)
        {
            _ripple = ripple;
            _settings = settings;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (_ripple == null || _settings == null) return;

            if (!_ripple.TrySense(transform.position, _settings.SenseRadius, out Vector2 source)) return;

            LastSensedSource = source;

            // 已经在前往/驻留在同一个声源上就不再重置；只有明显不同的新声音才改向
            if (State != EnemyState.Idle && Vector2.Distance(source, _target) <= _settings.RetargetDistance) return;

            BeginSeek(source);
        }

        private void FixedUpdate()
        {
            if (_body == null || _settings == null) return;

            switch (State)
            {
                case EnemyState.Seek:
                    SeekStep();
                    break;

                default:
                    _body.linearVelocity = Vector2.zero;
                    break;
            }
        }

        private void SeekStep()
        {
            Vector2 position = _body.position;
            float distance = Vector2.Distance(position, _target);

            if (distance <= _settings.ArriveDistance)
            {
                Arrive();
                return;
            }

            Vector2 direction = (_target - position).normalized;
            _body.linearVelocity = direction * _settings.MoveSpeed;
            TrackStuck(position);
        }

        private void TrackStuck(Vector2 position)
        {
            _stuckTimer += Time.fixedDeltaTime;
            if (_stuckTimer < _settings.StuckCheckInterval) return;

            float moved = Vector2.Distance(position, _stuckCheckPosition);
            if (moved < _settings.StuckMoveThreshold)
            {
                StuckCount++;
                Debug.Log(
                    $"[Enemy] 卡住 #{StuckCount}：{_settings.StuckCheckInterval:0.0} s 内只移动了 {moved:0.00} m，" +
                    $"位置 ({position.x:0.00},{position.y:0.00})，目标 ({_target.x:0.00},{_target.y:0.00})");
            }

            _stuckCheckPosition = position;
            _stuckTimer = 0f;
        }

        private void BeginSeek(Vector2 source)
        {
            State = EnemyState.Seek;
            _target = source;
            _seekStartTime = Time.time;
            _stuckCheckPosition = _body != null ? _body.position : (Vector2)transform.position;
            _stuckTimer = 0f;
            SeekCount++;

            Debug.Log($"[Enemy] →Seek 声源 ({source.x:0.00},{source.y:0.00})");
        }

        private void Arrive()
        {
            State = EnemyState.Camp;
            _body.linearVelocity = Vector2.zero;
            LastSeekSeconds = Time.time - _seekStartTime;
            if (LastSeekSeconds > MaxSeekSeconds) MaxSeekSeconds = LastSeekSeconds;
            CampCount++;

            Debug.Log($"[Enemy] Seek→Camp 到达 ({_target.x:0.00},{_target.y:0.00})，用时 {LastSeekSeconds:0.00} s");
        }
    }
}
