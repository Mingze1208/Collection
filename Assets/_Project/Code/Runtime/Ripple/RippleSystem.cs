using System.Collections.Generic;
using Collection.Core.Ripple;
using UnityEngine;

namespace Collection.Runtime.Ripple
{
    /// <summary>
    /// 涟漪点集：负责从声源发射声音点、按时间推进、渲染尾迹，并回答“某处是否被声音触及”。
    /// 传播数学在 Collection.Core.Ripple（可单测），这里只负责 Unity 侧的收集与表现。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RippleSystem : MonoBehaviour
    {
        [SerializeField] private RippleSettings _settings;
        [SerializeField] private Transform _wallRoot;
        [SerializeField] private Transform _dotRoot;
        [SerializeField] private Sprite _dotSprite;
        [SerializeField] private Material _dotMaterial;

        private sealed class Burst
        {
            public RipplePath[] Paths;
            public Vector2 Source;
            public float StartTime;
            /// <summary>0 表示用设置里的默认尾迹点数；声束取证时覆盖为整条路径长度。</summary>
            public int TrailDots;
            public float Speed = 3f;
            public float Lifetime = 1.2f;
            public Color Color = Color.white;
        }

        private readonly List<Aabb> _walls = new List<Aabb>();
        private readonly List<Burst> _bursts = new List<Burst>();
        private readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        private readonly List<Vector2> _dotPositions = new List<Vector2>();
        private readonly List<Vector2> _dotSources = new List<Vector2>();

        public RippleSettings Settings => _settings;
        public int ActiveBurstCount => _bursts.Count;
        public int ActiveDotCount => _dotPositions.Count;
        public int WallCount => _walls.Count;
        public IReadOnlyList<Vector2> DotPositions => _dotPositions;

        /// <summary>墙体 AABB 列表（只读），供覆盖率等离线计算使用。</summary>
        public IReadOnlyList<Aabb> Walls => _walls;

        /// <summary>最近一次发声的时刻（Time.time），供 HUD 显示“发声距今多久”。</summary>
        public float LastEmitTime { get; private set; } = float.NegativeInfinity;

        /// <summary>最近一次发声的原点，用于校验“落脚点 = 涟漪原点”。</summary>
        public Vector2 LastEmitOrigin { get; private set; }

        /// <summary>本次运行出现过的最大同屏点数，用于验证 512 上限。</summary>
        public int PeakDotCount { get; private set; }

        public void Initialize(RippleSettings settings, Transform wallRoot, Transform dotRoot,
            Sprite dotSprite, Material dotMaterial)
        {
            _settings = settings;
            _wallRoot = wallRoot;
            _dotRoot = dotRoot;
            _dotSprite = dotSprite;
            _dotMaterial = dotMaterial;
            CollectWalls();
        }

        /// <summary>切换参数档（A/B 对照用）。切档会清空在途声音，避免新旧参数混算。</summary>
        public void SetSettings(RippleSettings settings)
        {
            if (settings == null || settings == _settings) return;

            _settings = settings;
            _bursts.Clear();
            PeakDotCount = 0;
            RebuildDots();
        }

        private void Awake()
        {
            CollectWalls();
        }

        /// <summary>把墙体碰撞体转成 AABB 列表，供反射计算使用。</summary>
        public void CollectWalls()
        {
            _walls.Clear();
            if (_wallRoot == null) return;

            var colliders = _wallRoot.GetComponentsInChildren<BoxCollider2D>(true);
            foreach (var collider in colliders)
            {
                Bounds bounds = collider.bounds;
                _walls.Add(Aabb.FromMinMax(
                    new Vec2(bounds.min.x, bounds.min.y),
                    new Vec2(bounds.max.x, bounds.max.y)));
            }
        }

        /// <summary>在 origin 处发声。source 决定用哪一档密度（落脚为轻量档）。</summary>
        public void Emit(Vector2 origin, RippleSource source = RippleSource.Voice)
        {
            if (_settings == null) return;

            RippleProfile profile = source == RippleSource.Footstep ? _settings.Footstep : _settings.Voice;

            var paths = RippleBurstBuilder.BuildRadial(
                new Vec2(origin.x, origin.y),
                profile.DirectionCount,
                profile.Speed,
                profile.Lifetime,
                _walls,
                _settings.MaxBounces);

            _bursts.Add(new Burst
            {
                Paths = paths,
                Source = origin,
                StartTime = Time.time,
                TrailDots = profile.TrailDots,
                Speed = profile.Speed,
                Lifetime = profile.Lifetime,
                Color = profile.Color,
            });

            LastEmitTime = Time.time;
            LastEmitOrigin = origin;
            TrimToCapacity();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 调试用：只沿一个方向发射一条声音点路径，用于截图取证“镜面反射”。仅编辑器可用。
        /// 与径向发射同一套物理（同样的墙、速度、存活时间）。
        /// </summary>
        public void EmitBeam(Vector2 origin, Vector2 direction)
        {
            if (_settings == null) return;

            RippleProfile profile = _settings.Voice;

            var path = RippleBurstBuilder.BuildPath(
                new Vec2(origin.x, origin.y),
                new Vec2(direction.x, direction.y),
                profile.Speed * profile.Lifetime,
                profile.Speed,
                _walls,
                _settings.MaxBounces);

            _bursts.Add(new Burst
            {
                Paths = new[] { path },
                Source = origin,
                StartTime = Time.time,
                // 尾迹覆盖整条路径，便于截图看出折返
                TrailDots = Mathf.CeilToInt(path.Length / Mathf.Max(0.01f, _settings.TrailSpacing)) + 1,
                Speed = profile.Speed,
                Lifetime = profile.Lifetime,
                Color = profile.Color,
            });

            LastEmitTime = Time.time;
            TrimToCapacity();
        }
#endif

        public void Clear()
        {
            _bursts.Clear();
            RebuildDots();
        }

        /// <summary>声音点是否触及 point 附近 radius；命中时给出该点所属的声源坐标。</summary>
        public bool TrySense(Vector2 point, float radius, out Vector2 source)
        {
            source = Vector2.zero;
            float bestSqr = radius * radius;
            bool found = false;

            for (int i = 0; i < _dotPositions.Count; i++)
            {
                float sqr = (_dotPositions[i] - point).sqrMagnitude;
                if (sqr > bestSqr) continue;

                bestSqr = sqr;
                source = _dotSources[i];
                found = true;
            }

            return found;
        }

        private void TrimToCapacity()
        {
            if (_settings == null) return;

            int total = 0;
            foreach (Burst burst in _bursts) total += DotCost(burst);

            // 按真实点数削减：只丢最旧的一次，直到不超上限
            while (_bursts.Count > 1 && total > _settings.MaxDots)
            {
                total -= DotCost(_bursts[0]);
                _bursts.RemoveAt(0);
            }
        }

        private int DotCost(Burst burst)
        {
            return burst.Paths.Length * Mathf.Max(1, burst.TrailDots);
        }

        private void Update()
        {
            RebuildDots();
        }

        private void RebuildDots()
        {
            _dotPositions.Clear();
            _dotSources.Clear();
            if (_settings == null) return;

            float now = Time.time;

            for (int i = _bursts.Count - 1; i >= 0; i--)
            {
                if (now - _bursts[i].StartTime >= _bursts[i].Lifetime) _bursts.RemoveAt(i);
            }

            int used = 0;
            int cap = _settings.MaxDots;

            // 先画最新的一次发声，点数上限不够时牺牲旧尾迹
            for (int b = _bursts.Count - 1; b >= 0 && used < cap; b--)
            {
                Burst burst = _bursts[b];
                float age = now - burst.StartTime;
                float head = burst.Speed * age;

                // 整次发声共用同一个收缩/变暗系数：所有圆一起从原尺寸缩到 0
                float ageFade = 1f - Mathf.Clamp01(age / burst.Lifetime);
                float dotScale = _settings.DotRadius * 2f * ageFade;

                Color baseColor = burst.Color;
                int trailDots = Mathf.Max(1, burst.TrailDots);

                for (int p = 0; p < burst.Paths.Length && used < cap; p++)
                {
                    RipplePath path = burst.Paths[p];

                    for (int t = 0; t < trailDots && used < cap; t++)
                    {
                        float distance = head - t * _settings.TrailSpacing;
                        if (distance < 0f) break;

                        Vec2 position = path.PositionAt(distance);
                        float tailFade = 1f - (float)t / trailDots;

                        SpriteRenderer dot = GetDot(used);
                        dot.transform.position = new Vector3(position.X, position.Y, 0f);
                        dot.transform.localScale = Vector3.one * dotScale;

                        // 颜色向黑色收敛（尾迹越靠后越暗），同时淡出，避免黑圆挡住后面的点
                        float fade = ageFade * tailFade;
                        var color = Color.Lerp(Color.black, baseColor, fade);
                        color.a = baseColor.a * fade;
                        dot.color = color;
                        dot.enabled = true;

                        _dotPositions.Add(new Vector2(position.X, position.Y));
                        _dotSources.Add(burst.Source);
                        used++;
                    }
                }
            }

            if (_dotPositions.Count > PeakDotCount) PeakDotCount = _dotPositions.Count;

            for (int i = used; i < _pool.Count; i++) _pool[i].enabled = false;
        }

        private SpriteRenderer GetDot(int index)
        {
            while (_pool.Count <= index)
            {
                var go = new GameObject($"Dot_{_pool.Count:000}");
                go.transform.SetParent(_dotRoot != null ? _dotRoot : transform, false);

                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = _dotSprite;
                renderer.sharedMaterial = _dotMaterial;
                renderer.sortingOrder = 10;
                renderer.enabled = false;

                _pool.Add(renderer);
            }

            return _pool[index];
        }
    }
}
