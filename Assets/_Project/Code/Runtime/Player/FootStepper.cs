using System.Collections.Generic;
using Collection.Runtime.Ripple;
using UnityEngine;

namespace Collection.Runtime.Player
{
    /// <summary>
    /// 一步一印的走路：本体始终是双脚中的一只。
    /// 迈步时另一只脚落在方向键指向的 stepLength 处、在该点发声；
    /// 旧脚留在原地并逐渐变灰，作为上一步的痕迹；该脚下次落步时残影自然被取走。
    /// 离散步进会跨过薄墙，所以每次迈步前做圆形扫描，只在可达范围内落脚。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class FootStepper : MonoBehaviour
    {
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private RippleSettings _settings;
        [SerializeField] private Transform _wallRoot;
        [SerializeField] private Transform _leftFoot;
        [SerializeField] private Transform _rightFoot;
        [SerializeField] private Sprite _markerSprite;
        [SerializeField] private Material _markerMaterial;
        [SerializeField] private Color _bodyColor = new Color(1f, 0.84f, 0.55f, 1f);
        [SerializeField] private Color _ghostColor = new Color(0.5f, 0.5f, 0.56f, 0.55f);

        private const int MaxMarkers = 6;
        private const float MarkerLifetime = 0.6f;
        private const float MinStep = 0.15f;
        private const float WallClearance = 0.02f;

        private readonly List<SpriteRenderer> _markers = new List<SpriteRenderer>();
        private readonly List<float> _markerTimes = new List<float>();

        private Rigidbody2D _body;
        private Collider2D _collider;
        private int _bodySide; // 0 = 左, 1 = 右
        private float _ghostTime = float.NegativeInfinity;

        public int StepCount { get; private set; }
        public int LeftSteps { get; private set; }
        public int RightSteps { get; private set; }

        /// <summary>静步次数（屏息状态下迈出、且不发声音的步数）。</summary>
        public int SilentSteps { get; private set; }

        /// <summary>被墙挡住、没能迈出去的次数。</summary>
        public int BlockedCount { get; private set; }

        /// <summary>当前本体是哪只脚：0 = 左，1 = 右。</summary>
        public int CurrentFoot => _bodySide;

        public Vector2 LastStepDirection { get; private set; } = Vector2.down;
        public Vector2 LastStepPosition { get; private set; }

        /// <summary>上一步实际迈出的距离（m）。</summary>
        public float LastStepDistance { get; private set; }

        /// <summary>落点与涟漪原点的偏差（m），应恒为 0。</summary>
        public float LastOriginError { get; private set; }

        public Color BodyColor => _bodyColor;
        public Color GhostColor => _ghostColor;

        /// <summary>按住方向键时的步间隔（s）。</summary>
        public float StepInterval => _settings != null ? _settings.StepInterval : 0.35f;

        /// <summary>屏息静步的步间隔（s）：更慢但完全无声。</summary>
        public float SilentStepInterval => _settings != null ? _settings.SilentStepInterval : 0.63f;

        /// <summary>当前步速（m/s）：走路 vs 静步。</summary>
        public float SpeedFor(bool silent)
        {
            float interval = silent ? SilentStepInterval : StepInterval;
            float stepLength = _settings != null ? _settings.StepLength : 0.8f;
            return stepLength / Mathf.Max(0.01f, interval);
        }

        /// <summary>上一步那只脚当前的颜色（用于验证“变灰”）。</summary>
        public Color GhostFootColor
        {
            get
            {
                SpriteRenderer renderer = GhostRenderer();
                return renderer != null ? renderer.color : Color.clear;
            }
        }

        public float GhostProgress
        {
            get
            {
                if (_settings == null) return 0f;
                return Mathf.Clamp01((Time.time - _ghostTime) / _settings.GhostFadeTime);
            }
        }

        public void Initialize(RippleSystem ripple, RippleSettings settings, Transform wallRoot,
            Transform leftFoot, Transform rightFoot, Sprite markerSprite, Material markerMaterial)
        {
            _ripple = ripple;
            _settings = settings;
            _wallRoot = wallRoot;
            _leftFoot = leftFoot;
            _rightFoot = rightFoot;
            _markerSprite = markerSprite;
            _markerMaterial = markerMaterial;

            ResetFeet();
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();
            ResetFeet();
        }

        /// <summary>回到“只有左脚”的初始状态。</summary>
        public void ResetFeet()
        {
            Vector2 origin = transform.position;

            if (_leftFoot != null)
            {
                _leftFoot.position = origin;
                _leftFoot.gameObject.SetActive(true);
            }

            if (_rightFoot != null)
            {
                _rightFoot.position = origin;
                _rightFoot.gameObject.SetActive(false);
            }

            _bodySide = 0;
            _ghostTime = float.NegativeInfinity;

            SpriteRenderer left = RendererOf(_leftFoot);
            if (left != null) left.color = _bodyColor;
        }

        /// <summary>朝 direction 迈一步。silent = 屏息静步：不发声。被挡住或距离不足时返回 false。</summary>
        public bool TryStep(Vector2 direction, bool silent = false)
        {
            if (_settings == null) return false;
            if (direction.sqrMagnitude < 0.01f) return false;

            direction = direction.normalized;

            Vector2 origin = transform.position;
            float allowed = AllowedDistance(origin, direction);

            if (allowed < MinStep)
            {
                BlockedCount++;
                return false;
            }

            Vector2 destination = origin + direction * allowed;

            int nextSide = 1 - _bodySide;
            Transform nextFoot = nextSide == 0 ? _leftFoot : _rightFoot;
            if (nextFoot == null) return false;

            // 新脚落到目标点，成为本体；旧脚原地不动，由 Update 渐变成残影
            nextFoot.gameObject.SetActive(true);
            nextFoot.position = destination;

            _bodySide = nextSide;
            _ghostTime = Time.time;

            transform.position = destination;
            if (_body != null) _body.position = destination;

            LastStepDirection = direction;
            LastStepPosition = destination;
            LastStepDistance = allowed;
            StepCount++;
            if (nextSide == 0) LeftSteps++;
            else RightSteps++;

            if (silent)
            {
                SilentSteps++;
            }
            else if (_ripple != null)
            {
                _ripple.Emit(destination, RippleSource.Footstep);
                LastOriginError = Vector2.Distance(destination, _ripple.LastEmitOrigin);
            }

            SpawnMarker(destination, silent);
            return true;
        }

        /// <summary>沿 direction 能走多远才碰到墙（最多 stepLength）。</summary>
        private float AllowedDistance(Vector2 origin, Vector2 direction)
        {
            if (_settings == null) return 0f;

            float allowed = _settings.StepLength;
            float radius = _collider != null ? Mathf.Max(0.05f, _collider.bounds.extents.x) : 0.3f;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, direction, _settings.StepLength);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider == null) continue;
                if (_collider != null && hit.collider == _collider) continue;
                if (_wallRoot != null && !hit.collider.transform.IsChildOf(_wallRoot)) continue;

                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - WallClearance));
            }

            return allowed;
        }

        private void Update()
        {
            UpdateGhostFade();
            UpdateMarkers();
        }

        private void UpdateGhostFade()
        {
            SpriteRenderer ghost = GhostRenderer();
            if (ghost == null || _settings == null) return;

            ghost.color = Color.Lerp(_bodyColor, _ghostColor, GhostProgress);

            SpriteRenderer body = RendererOf(_bodySide == 0 ? _leftFoot : _rightFoot);
            if (body != null && body != ghost) body.color = _bodyColor;
        }

        private SpriteRenderer GhostRenderer()
        {
            Transform ghost = _bodySide == 0 ? _rightFoot : _leftFoot;
            if (ghost == null || !ghost.gameObject.activeSelf) return null;
            return RendererOf(ghost);
        }

        private static SpriteRenderer RendererOf(Transform foot)
        {
            return foot != null ? foot.GetComponent<SpriteRenderer>() : null;
        }

        private void SpawnMarker(Vector2 position, bool silent)
        {
            SpriteRenderer marker = GetMarker();

            marker.transform.position = new Vector3(position.x, position.y, 0f);
            marker.transform.localScale = Vector3.one * 0.08f;
            // 绿 = 有声音的落脚；蓝 = 屏息静步（无声音）
            marker.color = silent ? new Color(0.35f, 0.65f, 1f, 1f) : new Color(0.2f, 1f, 0.6f, 1f);
            marker.enabled = true;
        }

        private SpriteRenderer GetMarker()
        {
            while (_markers.Count < MaxMarkers)
            {
                var go = new GameObject($"StepMarker_{_markers.Count:00}");
                go.transform.SetParent(transform.parent, false);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = _markerSprite;
                renderer.sharedMaterial = _markerMaterial;
                renderer.sortingOrder = 30;
                renderer.enabled = false;

                _markers.Add(renderer);
                _markerTimes.Add(float.NegativeInfinity);
            }

            int oldest = 0;
            for (int i = 1; i < _markerTimes.Count; i++)
            {
                if (_markerTimes[i] < _markerTimes[oldest]) oldest = i;
            }

            _markerTimes[oldest] = Time.time;
            return _markers[oldest];
        }

        private void UpdateMarkers()
        {
            float now = Time.time;
            for (int i = 0; i < _markers.Count; i++)
            {
                if (!_markers[i].enabled) continue;

                float age = now - _markerTimes[i];
                if (age >= MarkerLifetime)
                {
                    _markers[i].enabled = false;
                    continue;
                }

                var color = _markers[i].color;
                color.a = 1f - Mathf.Clamp01(age / MarkerLifetime);
                _markers[i].color = color;
            }
        }
    }
}
