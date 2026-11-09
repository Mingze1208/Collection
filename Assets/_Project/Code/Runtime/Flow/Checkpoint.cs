using Collection.Runtime.Player;
using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 检查点：BoxCollider2D（Trigger）矩形判定，玩家经过即激活，并把复活点设为自身位置。
    /// 一次性激活；激活后由暗黄变亮黄。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private SpikeFlow _flow;
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Color _inactiveColor = new Color(0.85f, 0.75f, 0.25f, 0.55f);
        [SerializeField] private Color _activeColor = new Color(1f, 0.95f, 0.15f, 1f);
        [SerializeField] private int _index;

        public bool Activated { get; private set; }

        /// <summary>当前颜色，供验证“激活后变亮”。</summary>
        public Color CurrentColor => _renderer != null ? _renderer.color : Color.clear;
        public int TriggerCount { get; private set; }

        /// <summary>复活点 = 检查点中心。</summary>
        public Vector2 RespawnPoint => transform.position;

        /// <summary>章节内顺序（0 起），用于“只允许向前覆盖复活点”。</summary>
        public int Index => _index;

        public void Initialize(PlayerController player, SpikeFlow flow, SpriteRenderer renderer, int index)
        {
            _player = player;
            _flow = flow;
            _renderer = renderer;
            _index = index;

            if (_renderer != null) _renderer.color = _inactiveColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Activated || _player == null) return;
            if (other.GetComponentInParent<PlayerController>() != _player) return;

            Activate();
        }

        private void Activate()
        {
            Activated = true;
            TriggerCount++;

            if (_renderer != null) _renderer.color = _activeColor;
            bool took = _flow != null && _flow.SetCheckpoint(RespawnPoint, _index);

            Debug.Log(
                $"[Checkpoint] t={Time.time:0.00} 激活检查点 #{_index} ({RespawnPoint.x:0.00},{RespawnPoint.y:0.00})，" +
                (took ? "复活点已更新" : "已被更靠后的检查点覆盖，忽略"));
        }
    }
}
