using Collection.Runtime.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 结局画面（T4）：到达终点门后，黑场逐行浮现终章文本；R 可重看。
    /// </summary>
    public sealed class EndingScreen : MonoBehaviour
    {
        [SerializeField] private ChapterGoal _goal;
        [SerializeField] private string[] _lines = new string[0];

        [SerializeField] private float _startDelay = 1.2f;
        [SerializeField] private float _lineInterval = 1.8f;
        [SerializeField] private float _fadeTime = 0.6f;

        private float _startedAt = -1f;

        public bool Showing => _goal != null && _goal.Completed;

        /// <summary>已经完整播放过几次（含重看）。</summary>
        public int RevealCount { get; private set; }

        /// <summary>当前可见行数（逐行浮现用）。</summary>
        public int VisibleLines { get; private set; }

        public int LineCount => _lines.Length;

        /// <summary>所有行都浮现完并停留 1.5 s（用于衔接“章节完成”窗口）。</summary>
        public bool Finished
        {
            get
            {
                if (!Showing || _startedAt < 0f) return false;
                float elapsed = Time.time - _startedAt - _startDelay;
                return elapsed > _lines.Length * _lineInterval + 1.5f;
            }
        }

        public void Initialize(ChapterGoal goal, string[] lines)
        {
            _goal = goal;
            _lines = lines ?? new string[0];
        }

        private void Update()
        {
            if (!Showing)
            {
                _startedAt = -1f;
                VisibleLines = 0;
                return;
            }

            bool replay = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

            if (_startedAt < 0f)
            {
                Restart("结局开始播放");
            }
            else if (replay)
            {
                Restart("重看结局");
            }

            float elapsed = Time.time - _startedAt - _startDelay;
            VisibleLines = Mathf.Clamp(Mathf.FloorToInt(elapsed / Mathf.Max(0.05f, _lineInterval)) + 1,
                0, _lines.Length);
        }

        /// <summary>重播结局（R 键与调试入口共用）。</summary>
        public void Restart(string reason = "重看结局")
        {
            _startedAt = Time.time;
            RevealCount++;
            Debug.Log($"[Ending] t={Time.time:0.00} {reason}（第 {RevealCount} 遍，共 {_lines.Length} 行）");
        }

        private void OnGUI()
        {
            if (!Showing || _lines.Length == 0) return;

            GUI.color = new Color(0f, 0f, 0f, 0.94f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float lineHeight = Mathf.Min(56f, Screen.height / (float)(_lines.Length + 3));
            float totalHeight = lineHeight * _lines.Length;
            float top = Screen.height * 0.5f - totalHeight * 0.5f;

            for (int i = 0; i < VisibleLines; i++)
            {
                float shownFor = Time.time - _startedAt - _startDelay - i * _lineInterval;
                float alpha = Mathf.Clamp01(shownFor / Mathf.Max(0.05f, _fadeTime));
                bool isTitle = i == 0;

                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(isTitle ? 52f : 28f),
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.82f, 0.95f, 1f, alpha) },
                };

                GUI.Label(new Rect(0f, top + i * lineHeight, Screen.width, lineHeight), _lines[i], style);
            }

            if (VisibleLines >= _lines.Length)
            {
                float hintAlpha = Mathf.Clamp01((Time.time - _startedAt - _startDelay - _lines.Length * _lineInterval) / 1f);
                var hint = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.75f, 0.75f, 0.8f, hintAlpha * 0.8f) },
                };
                GUI.Label(new Rect(0f, Screen.height - 80f, Screen.width, 30f),
                    "R 重看结局　·　Esc 退出", hint);
            }
        }
    }
}
