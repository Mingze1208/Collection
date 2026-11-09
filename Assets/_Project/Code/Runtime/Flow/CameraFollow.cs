using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 相机跟随：X 始终跟；Y 可选跟随，带死区与夹取，避免步进式移动造成的纵向抖动。
    /// 死区 = 0 时为纯跟随；Y 夹取用来防止镜头越出走廊。
    /// </summary>
    public sealed class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private bool _followY = true;
        [SerializeField, Min(0f)] private float _smoothX = 0.12f;
        [SerializeField, Min(0f)] private float _smoothY = 0.25f;

        [Tooltip("Y 死区（m）：玩家偏离镜头中心超过该值才推动 Y。0 = 一直跟。")]
        [SerializeField, Min(0f)] private float _yDeadzone = 0.6f;

        [Tooltip("镜头 Y 的上下夹取，防止看到走廊外面。")]
        [SerializeField] private Vector2 _yClamp = new Vector2(-1.5f, 1.5f);

        private float _velocityX;
        private float _velocityY;
        private float _fixedY;

        public Transform Target => _target;
        public bool FollowY => _followY;
        public Vector2 YClamp => _yClamp;

        /// <summary>上一帧到这一帧的 Y 位移，用于验证“不抖”。</summary>
        public float LastYDelta { get; private set; }

        public void Initialize(Transform target, float fixedY, bool followY = true,
            float deadzone = 0.6f, Vector2? yClamp = null, float smoothX = 0.12f, float smoothY = 0.25f)
        {
            _target = target;
            _fixedY = fixedY;
            _followY = followY;
            _yDeadzone = deadzone;
            _smoothX = smoothX;
            _smoothY = smoothY;
            if (yClamp.HasValue) _yClamp = yClamp.Value;

            if (_target == null) return;

            transform.position = new Vector3(
                _target.position.x,
                _followY ? ClampY(_target.position.y) : _fixedY,
                transform.position.z);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            float previousY = transform.position.y;

            float x = _smoothX <= 0f
                ? _target.position.x
                : Mathf.SmoothDamp(transform.position.x, _target.position.x, ref _velocityX, _smoothX);

            float y = _followY ? ComputeY() : _fixedY;

            transform.position = new Vector3(x, y, transform.position.z);
            LastYDelta = y - previousY;
        }

        private float ComputeY()
        {
            float desired = ClampY(_target.position.y);
            float current = transform.position.y;
            float delta = desired - current;

            // 死区内不动，避免步进式移动让画面上下抖
            if (Mathf.Abs(delta) <= _yDeadzone) return current;

            float target = desired - Mathf.Sign(delta) * _yDeadzone;
            return _smoothY <= 0f
                ? target
                : Mathf.SmoothDamp(current, target, ref _velocityY, _smoothY);
        }

        private float ClampY(float y) => Mathf.Clamp(y, _yClamp.x, _yClamp.y);
    }
}
