using Collection.Runtime.Player;
using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 章末“镜子”门：BoxCollider2D（Trigger）矩形判定，走进门即通关。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ChapterGoal : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;

        public bool Completed { get; private set; }
        public int TriggerCount { get; private set; }
        public float ReachedAt { get; private set; } = -1f;

        public void Initialize(PlayerController player)
        {
            _player = player;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Completed || _player == null) return;
            if (other.GetComponentInParent<PlayerController>() != _player) return;

            Completed = true;
            TriggerCount++;
            ReachedAt = Time.time;

            _player.SetInputEnabled(false);
            Debug.Log($"[Goal] t={Time.time:0.00} 进入终点门触发区 ({transform.position.x:0.00},{transform.position.y:0.00})");
        }

        private void OnGUI()
        {
            if (!Completed) return;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.95f, 1f) },
            };
            var hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.9f) },
            };

            float y = Screen.height * 0.5f - 40f;
            GUI.Label(new Rect(0f, y, Screen.width, 60f), "第一章 · 结束", title);
            GUI.Label(new Rect(0f, y + 60f, Screen.width, 30f),
                $"到达时刻 {ReachedAt:0.0} s（场景内计时）　·　下一章尚未实现", hint);
        }
    }
}
