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
        [SerializeField] private string _chapterTitle = "第一章";
        [SerializeField] private string _nextScene = string.Empty;
        [SerializeField] private UiWindow _window;
        [SerializeField] private EndingScreen _ending;

        public bool Completed { get; private set; }
        public string NextScene => _nextScene;
        public bool WindowShown { get; private set; }
        public int TriggerCount { get; private set; }
        public float ReachedAt { get; private set; } = -1f;

        public void Initialize(PlayerController player, string chapterTitle = null,
            string nextScene = null, UiWindow window = null, EndingScreen ending = null)
        {
            _player = player;
            if (!string.IsNullOrEmpty(chapterTitle)) _chapterTitle = chapterTitle;
            if (!string.IsNullOrEmpty(nextScene)) _nextScene = nextScene;
            _window = window;
            _ending = ending;
        }

        private void Update()
        {
            if (!Completed || WindowShown || _window == null) return;

            // 有结局动画时，等它播完再弹窗
            if (_ending != null && !_ending.Finished) return;

            WindowShown = true;

            bool hasNext = !string.IsNullOrEmpty(_nextScene);
            _window.Show(
                $"{_chapterTitle} · 结束",
                hasNext ? "下一关（Enter）" : string.Empty,
                hasNext ? (System.Action)LoadNext : null,
                "返回主菜单",
                _window.LoadMenu);

            Debug.Log($"[Goal] 完成窗口已弹出：{_chapterTitle}，下一关={(hasNext ? _nextScene : "无")}");
        }

        private void LoadNext()
        {
            if (string.IsNullOrEmpty(_nextScene)) return;

            Debug.Log($"[Goal] 载入下一关：{_nextScene}");
            UnityEngine.SceneManagement.SceneManager.LoadScene(_nextScene);
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

    }
}
