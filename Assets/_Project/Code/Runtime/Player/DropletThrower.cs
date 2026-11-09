using Collection.Core.Ripple;
using Collection.Runtime.Ripple;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collection.Runtime.Player
{
    /// <summary>
    /// 泪滴投掷（T1）：
    /// 鼠标左键朝光标投掷，方向与移动方向无关；**投掷本身不发声**，只有落地时在落点触发一次“落脚档” burst。
    /// 投射物只是视觉：落点在投掷瞬间用“射线 vs 墙体 AABB”算出（撞墙或射程封顶），飞行时间 = 距离 / 速度。
    /// </summary>
    public sealed class DropletThrower : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private RippleSettings _settings;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _material;

        private GameObject _projectileGo;
        private SpriteRenderer _projectile;

        private Vector2 _from;
        private Vector2 _to;
        private float _elapsed;
        private float _duration;
        private bool _flying;
        private float _nextThrowTime;

        public bool IsFlying => _flying;
        public float CooldownRemaining => Mathf.Max(0f, _nextThrowTime - Time.time);
        public int ThrowCount { get; private set; }
        public int BlockedCount { get; private set; }
        public int LandingCount { get; private set; }
        public Vector2 LastLanding { get; private set; }
        public Vector2 LastTarget { get; private set; }

        public void Initialize(PlayerController player, RippleSystem ripple, RippleSettings settings,
            Sprite sprite, Material material)
        {
            _player = player;
            _ripple = ripple;
            _settings = settings;
            _sprite = sprite;
            _material = material;
        }

        private void Update()
        {
            if (_settings == null) return;

            HandleMouseInput();
            UpdateFlight();
        }

        private void HandleMouseInput()
        {
            if (_player != null && !_player.InputEnabled) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

            Camera camera = Camera.main;
            if (camera == null) return;

            Vector2 screen = mouse.position.ReadValue();
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            TryThrow(new Vector2(world.x, world.y));
        }

        /// <summary>朝目标点投掷（鼠标路径与调试路径共用）。冷却中或已有投射物时返回 false。</summary>
        public bool TryThrow(Vector2 targetWorld)
        {
            if (_settings == null || _ripple == null) return false;

            // 冷却中、或上一颗还在飞：都算“这次点击没生效”
            if (_flying || Time.time < _nextThrowTime)
            {
                BlockedCount++;
                return false;
            }

            Vector2 origin = _player != null ? (Vector2)_player.transform.position : (Vector2)transform.position;
            Vector2 delta = targetWorld - origin;
            if (delta.sqrMagnitude < 1e-6f) return false;

            Vector2 direction = delta.normalized;
            float wanted = Mathf.Min(_settings.DropletRange, delta.magnitude);
            float allowed = ComputeAllowedDistance(origin, direction, wanted);

            _from = origin;
            _to = origin + direction * allowed;
            _duration = Mathf.Max(0.01f, allowed / Mathf.Max(0.01f, _settings.DropletSpeed));
            _elapsed = 0f;
            _flying = true;

            LastTarget = targetWorld;
            ThrowCount++;
            _nextThrowTime = Time.time + _settings.DropletCooldown;

            ShowProjectileAt(_from);

            Debug.Log($"[Droplet] t={Time.time:0.00} 投掷：目标 ({targetWorld.x:0.00},{targetWorld.y:0.00})，" +
                      $"预计落点 ({_to.x:0.00},{_to.y:0.00})，距离 {allowed:0.00} m，飞行 {_duration:0.000} s（投掷本身不发声）");
            return true;
        }

        /// <summary>用墙体 AABB 做射线检测（与涟漪同一套墙），得到不超过射程的可达距离。</summary>
        private float ComputeAllowedDistance(Vector2 origin, Vector2 direction, float wanted)
        {
            float allowed = wanted;
            var rayOrigin = new Vec2(origin.x, origin.y);
            var rayDirection = new Vec2(direction.x, direction.y);

            foreach (Aabb wall in _ripple.Walls)
            {
                if (!wall.Raycast(rayOrigin, rayDirection, allowed, out float distance, out _)) continue;
                allowed = Mathf.Max(0.05f, distance - _settings.DropletRadius);
            }

            return allowed;
        }

        private void UpdateFlight()
        {
            if (!_flying) return;

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);
            if (_projectile != null) _projectile.transform.position = Vector2.Lerp(_from, _to, t);

            if (t < 1f) return;

            Land();
        }

        private void Land()
        {
            _flying = false;
            HideProjectile();

            LastLanding = _to;
            LandingCount++;

            // 只有落地发声：复用“落脚”档
            _ripple.Emit(_to, RippleSource.Footstep);

            Debug.Log($"[Droplet] t={Time.time:0.00} 落地 ({_to.x:0.00},{_to.y:0.00})，触发落脚档 burst");
        }

        private void ShowProjectileAt(Vector2 position)
        {
            EnsureProjectile();
            if (_projectile == null) return;

            _projectile.transform.position = new Vector3(position.x, position.y, 0f);
            _projectile.enabled = true;
        }

        private void HideProjectile()
        {
            if (_projectile != null) _projectile.enabled = false;
        }

        private void EnsureProjectile()
        {
            if (_projectileGo != null) return;

            _projectileGo = new GameObject("Droplet");
            _projectileGo.transform.position = transform.position;

            _projectile = _projectileGo.AddComponent<SpriteRenderer>();
            _projectile.sprite = _sprite;
            _projectile.sharedMaterial = _material;
            _projectile.color = new Color(0.55f, 0.85f, 1f, 1f);
            _projectile.sortingOrder = 12;
            _projectile.transform.localScale = Vector3.one * (_settings != null ? _settings.DropletRadius * 2f : 0.24f);
            _projectile.enabled = false;
        }

#if UNITY_EDITOR
        /// <summary>调试/取证：直接朝世界坐标投掷。仅编辑器可用。</summary>
        public bool ThrowAt(Vector2 worldPoint) => TryThrow(worldPoint);
#endif
    }
}
