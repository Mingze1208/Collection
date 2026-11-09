using Collection.Core.Ripple;
using Collection.Runtime.Player;
using Collection.Runtime.Ripple;
using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 虚无陷阱（T4）：玩家触之即死。
    /// 平时不可见；当涟漪尾迹点进入陷阱范围时，区域内的点变红（RippleSystem 负责染色），
    /// 陷阱本体的轮廓也随之显形——玩家靠“听”发现它，而不是靠看。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class VoidTrap : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private SpikeFlow _flow;
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private SpriteRenderer _visual;

        [SerializeField] private Color _hiddenColor = new Color(0.85f, 0.15f, 0.25f, 0f);
        [Tooltip("显形后的本体透明度。0 = 完全不可见，只靠红点提示（推荐）。")]
        [SerializeField] private Color _revealedColor = new Color(0.85f, 0.15f, 0.25f, 0f);

        private Aabb _zone;
        private bool _revealed;
        private float _revealAt = -1f;

        public int TriggerCount { get; private set; }
        public bool Revealed => _revealed;
        public float RevealedAt => _revealAt;

        /// <summary>陷阱矩形（世界坐标），供 HUD / 取证读取。</summary>
        public Aabb Zone => _zone;

        public void Initialize(PlayerController player, SpikeFlow flow, RippleSystem ripple, SpriteRenderer visual)
        {
            _player = player;
            _flow = flow;
            _ripple = ripple;
            _visual = visual;

            var collider = GetComponent<BoxCollider2D>();
            Vector3 position = transform.position;
            Vector2 size = collider != null ? collider.size : new Vector2(1.2f, 1.2f);
            Vector3 scale = transform.lossyScale;

            _zone = Aabb.FromMinMax(
                new Vec2(position.x - size.x * scale.x * 0.5f, position.y - size.y * scale.y * 0.5f),
                new Vec2(position.x + size.x * scale.x * 0.5f, position.y + size.y * scale.y * 0.5f));

            if (visual != null) visual.color = _hiddenColor;
        }

        private void Awake()
        {
            // 运行时把区域重新登记给涟漪系统（编辑期调用 Initialize 时登记的是编辑器实例）
            RebuildZone();
            if (_ripple != null) _ripple.RegisterTrapZone(_zone);
            if (_visual != null) _visual.color = _revealed ? _revealedColor : _hiddenColor;
        }

        private void RebuildZone()
        {
            var collider = GetComponent<BoxCollider2D>();
            Vector3 position = transform.position;
            Vector2 size = collider != null ? collider.size : new Vector2(1.2f, 1.2f);
            Vector3 scale = transform.lossyScale;

            _zone = Aabb.FromMinMax(
                new Vec2(position.x - size.x * scale.x * 0.5f, position.y - size.y * scale.y * 0.5f),
                new Vec2(position.x + size.x * scale.x * 0.5f, position.y + size.y * scale.y * 0.5f));
        }

        private void Update()
        {
            if (!_revealed) CheckReveal();
        }

        /// <summary>只要有任何一个声音点落进范围，陷阱就显形。</summary>
        private void CheckReveal()
        {
            if (_ripple == null) return;

            var dots = _ripple.DotPositions;
            for (int i = 0; i < dots.Count; i++)
            {
                var point = new Vec2(dots[i].x, dots[i].y);
                if (!_zone.Contains(point)) continue;

                _revealed = true;
                _revealAt = Time.time;
                if (_visual != null) _visual.color = _revealedColor;

                Debug.Log($"[Trap] t={Time.time:0.00} 涟漪触及陷阱范围（{_zone.Center.X:0.00},{_zone.Center.Y:0.00}），陷阱显形");
                return;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_player == null || _flow == null) return;
            if (other.GetComponentInParent<PlayerController>() != _player) return;
            if (_flow.State != SpikeState.Playing) return;

            TriggerCount++;
            Debug.Log($"[Trap] t={Time.time:0.00} 玩家踩中虚无（{transform.position.x:0.00},{transform.position.y:0.00}），立即死亡");
            _flow.Die(-1f, "踩中虚无陷阱");
        }
    }
}
