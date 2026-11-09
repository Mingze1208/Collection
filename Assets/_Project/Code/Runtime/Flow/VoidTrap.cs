using Collection.Runtime.Player;
using Collection.Runtime.Ripple;
using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 虚无陷阱（T2）：玩家踩进矩形区域时，在脚下发出一次**满档**声响（唱歌档），
    /// 相当于自己暴露了一次位置。区域内可重复触发，但有 3 s 冷却。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class VoidTrap : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private SpriteRenderer _visual;

        [SerializeField] private float _cooldown = 3f;
        [SerializeField] private Color _idleColor = new Color(0.16f, 0.10f, 0.24f, 0.85f);
        [SerializeField] private Color _fireColor = new Color(0.85f, 0.30f, 0.55f, 1f);

        private float _nextTriggerTime;
        private float _flashUntil;

        public int TriggerCount { get; private set; }
        public bool IsArmed => Time.time >= _nextTriggerTime;
        public float CooldownRemaining => Mathf.Max(0f, _nextTriggerTime - Time.time);

        public void Initialize(PlayerController player, RippleSystem ripple, SpriteRenderer visual)
        {
            _player = player;
            _ripple = ripple;
            _visual = visual;

            if (_visual != null) _visual.color = _idleColor;
        }

        private void Update()
        {
            if (_visual == null) return;

            _visual.color = Time.time < _flashUntil
                ? Color.Lerp(_idleColor, _fireColor, Mathf.Clamp01((_flashUntil - Time.time) / 0.45f))
                : _idleColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_player == null || _ripple == null) return;
            if (other.GetComponentInParent<PlayerController>() != _player) return;
            if (Time.time < _nextTriggerTime) return;

            _nextTriggerTime = Time.time + _cooldown;
            _flashUntil = Time.time + 0.45f;
            TriggerCount++;

            Vector2 position = _player.transform.position;
            _ripple.Emit(position, RippleSource.Voice);

            Debug.Log($"[Trap] 触发 #{TriggerCount}：玩家踩中陷阱 ({position.x:0.00},{position.y:0.00})，发出满档声响");
        }
    }
}
