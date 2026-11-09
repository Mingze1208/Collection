using UnityEngine;
using UnityEngine.InputSystem;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 首段叙事：底部字幕，按固定节奏逐行淡入淡出。
    /// 可跳过（Enter / Esc），不冻结玩家操作——对应 one-pager“叙述背景的同时玩家做出操作”。
    /// </summary>
    public sealed class NarrativeIntro : MonoBehaviour
    {
        [SerializeField] private NarrativeSettings _settings;

        private float _lineStart;
        private int _index;
        private bool _finished;

        /// <summary>当前行号，-1 表示已结束。</summary>
        public int CurrentLine => _finished ? -1 : _index;

        public bool Finished => _finished;
        public int SkipCount { get; private set; }

        public void Initialize(NarrativeSettings settings)
        {
            _settings = settings;
        }

        private void Start()
        {
            _lineStart = Time.time + (_settings != null ? _settings.StartDelay : 1f);
        }

        private void Update()
        {
            if (_finished || _settings == null || _settings.Lines.Length == 0) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
            {
                Skip();
                return;
            }

            if (Time.time < _lineStart) return;

            if (Time.time - _lineStart < _settings.LineDuration) return;

            _index++;
            _lineStart += _settings.LineDuration;

            if (_index >= _settings.Lines.Length) Finish();
        }

        public void Skip()
        {
            if (_finished) return;
            SkipCount++;
            Finish();
        }

        private void Finish()
        {
            _finished = true;
            Debug.Log($"[Narrative] t={Time.time:0.00} 首段叙事结束（播到第 {_index + 1} 行，跳过 {SkipCount} 次）");
        }

        private void OnGUI()
        {
            if (_finished || _settings == null || _index >= _settings.Lines.Length) return;
            if (Time.time < _lineStart) return;

            float elapsed = Time.time - _lineStart;
            float fade = _settings.FadeTime <= 0f
                ? 1f
                : Mathf.Clamp01(Mathf.Min(elapsed, _settings.LineDuration - elapsed) / _settings.FadeTime);
            if (fade <= 0f) return;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.93f, 0.93f, 0.96f, fade) },
            };
            var hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.75f, 0.75f, 0.8f, fade * 0.7f) },
            };

            float width = Mathf.Min(Screen.width * 0.8f, 900f);
            float x = (Screen.width - width) * 0.5f;
            GUI.Label(new Rect(x, Screen.height - 130f, width, 70f), _settings.Lines[_index], style);
            GUI.Label(new Rect(x, Screen.height - 52f, width - 20f, 24f), "Enter / Esc 跳过", hint);
        }
    }
}
