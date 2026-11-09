using System.Collections.Generic;
using System.Text;
using Collection.Runtime.Player;
using TMPro;
using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 世界空间文本（V3+V4+V5）：
    /// 玩家本体进入矩形触发区后逐字浮现；文本锚定世界坐标；
    /// 支持 TMP 内置富文本（color/size 等，标签不计入逐字），以及自定义 &lt;wave&gt;…&lt;/wave&gt; 逐字波浪。
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class WorldText : MonoBehaviour
    {
        public enum WorldTextState
        {
            Hidden = 0,
            Revealing = 1,
            Complete = 2,
        }

        [SerializeField] private PlayerController _player;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private string _fullText = string.Empty;
        [SerializeField] private float _charsPerSecond = 12f;
        [SerializeField] private float _punctuationPause = 0.15f;

        [Header("波浪")]
        [SerializeField] private bool _waveEnabled = true;
        [Tooltip("振幅 = fontSize × 该比例（按字高百分比）。")]
        [SerializeField] private float _waveAmplitudePercent = 0.14f;
        [SerializeField] private float _waveFrequency = 4f;
        [SerializeField] private float _wavePhaseStep = 0.6f;

        private bool[] _waveMask = new bool[0];
        private float _progress;
        private float _pauseTimer;

        public WorldTextState State { get; private set; } = WorldTextState.Hidden;
        public int TotalChars { get; private set; }
        public int VisibleChars => _text != null ? _text.maxVisibleCharacters : 0;
        public int TriggerCount { get; private set; }
        public int WaveCharCount { get; private set; }
        public string RenderedText => _text != null ? _text.text : string.Empty;

        /// <summary>按字高换算后的实际振幅（TMP 本地单位），供调试/取证。</summary>
        public float WaveAmplitudeUnits => _text != null ? _text.fontSize * _waveAmplitudePercent : 0f;

        /// <summary>当前浮现进度（0..1）。</summary>
        public float Progress => TotalChars <= 0 ? 0f : Mathf.Clamp01(_progress / TotalChars);

        public void Initialize(PlayerController player, TMP_Text text, CanvasGroup group,
            string content, float charsPerSecond, float punctuationPause,
            bool waveEnabled, float waveAmplitudePercent, float waveFrequency, float wavePhaseStep)
        {
            _player = player;
            _text = text;
            _group = group;
            _fullText = content;
            _charsPerSecond = charsPerSecond;
            _punctuationPause = punctuationPause;
            _waveEnabled = waveEnabled;
            _waveAmplitudePercent = waveAmplitudePercent;
            _waveFrequency = waveFrequency;
            _wavePhaseStep = wavePhaseStep;

            Prepare();
        }

        private void Awake()
        {
            Prepare();
        }

        /// <summary>解析标签、把文本交给 TMP、置为不可见。</summary>
        private void Prepare()
        {
            if (_text == null) return;

            // 构建期赋值依赖序列化；若为空则回退到 TMP 上已有的文本
            if (string.IsNullOrEmpty(_fullText)) _fullText = _text.text;

            string prepared = StripWaveTags(_fullText, out _waveMask);
            _text.text = prepared;
            _text.ForceMeshUpdate();

            TotalChars = _text.textInfo.characterCount;
            _text.maxVisibleCharacters = 0;

            WaveCharCount = 0;
            foreach (bool flag in _waveMask)
            {
                if (flag) WaveCharCount++;
            }

            if (_waveMask.Length != TotalChars)
            {
                Debug.LogWarning(
                    $"[WorldText] 字符映射不一致：mask={_waveMask.Length}，TMP={TotalChars}（波浪可能错位）");
            }

            if (_group != null) _group.alpha = 0f;

            State = WorldTextState.Hidden;
            _progress = 0f;
            _pauseTimer = 0f;
        }

        /// <summary>
        /// 剥离 &lt;wave&gt; / &lt;/wave&gt;，其余标签原样保留给 TMP；
        /// 同时产出“第 n 个可见字符是否属于波浪区间”的掩码。
        /// </summary>
        private static string StripWaveTags(string raw, out bool[] mask)
        {
            var builder = new StringBuilder(raw.Length);
            var flags = new List<bool>(raw.Length);
            bool inWave = false;

            int i = 0;
            while (i < raw.Length)
            {
                if (raw[i] == '<')
                {
                    int close = raw.IndexOf('>', i);
                    if (close > i)
                    {
                        string inner = raw.Substring(i + 1, close - i - 1).Trim().ToLowerInvariant();

                        if (inner == "wave")
                        {
                            inWave = true;
                            i = close + 1;
                            continue;
                        }

                        if (inner == "/wave")
                        {
                            inWave = false;
                            i = close + 1;
                            continue;
                        }

                        // 其他标签（color/size/…）交给 TMP，不计入可见字
                        builder.Append(raw, i, close - i + 1);
                        i = close + 1;
                        continue;
                    }
                }

                builder.Append(raw[i]);
                flags.Add(inWave);
                i++;
            }

            mask = flags.ToArray();
            return builder.ToString();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (State != WorldTextState.Hidden || _player == null) return;
            if (other.GetComponentInParent<PlayerController>() != _player) return;

            TriggerCount++;
            State = WorldTextState.Revealing;
            if (_group != null) _group.alpha = 1f;

            Debug.Log($"[WorldText] t={Time.time:0.00} 触发 ({transform.position.x:0.00},{transform.position.y:0.00})，" +
                      $"共 {TotalChars} 字（波浪 {WaveCharCount} 字），{_charsPerSecond:0.#} 字/秒");
        }

        private void Update()
        {
            if (State == WorldTextState.Revealing) UpdateReveal();
            if (State != WorldTextState.Hidden) ApplyWave();
        }

        private void UpdateReveal()
        {
            if (_text == null) return;

            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            int previous = Mathf.FloorToInt(_progress);
            _progress += _charsPerSecond * Time.deltaTime;
            int current = Mathf.Min(TotalChars, Mathf.FloorToInt(_progress));

            if (current > previous)
            {
                _text.maxVisibleCharacters = current;

                // 刚浮现的字若是标点，停顿一下
                if (_punctuationPause > 0f && IsPunctuation(current - 1))
                {
                    _pauseTimer = _punctuationPause;
                }
            }

            if (current >= TotalChars) State = WorldTextState.Complete;
        }

        /// <summary>逐字波浪：只移动可见且在 &lt;wave&gt; 区间内的字符顶点。</summary>
        private void ApplyWave()
        {
            if (!_waveEnabled || _text == null) return;

            float amplitude = WaveAmplitudeUnits;
            if (amplitude <= 0f) return;

            _text.ForceMeshUpdate();
            TMP_TextInfo info = _text.textInfo;
            float time = Time.time;
            bool changed = false;

            int count = Mathf.Min(info.characterCount, _waveMask.Length);
            for (int i = 0; i < count; i++)
            {
                if (!_waveMask[i]) continue;

                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible) continue;

                float offset = amplitude * Mathf.Sin(time * _waveFrequency + i * _wavePhaseStep);
                int vertexIndex = character.vertexIndex;
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;

                for (int k = 0; k < 4 && vertexIndex + k < vertices.Length; k++)
                {
                    vertices[vertexIndex + k].y += offset;
                }

                changed = true;
            }

            if (changed) _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        private bool IsPunctuation(int index)
        {
            if (_text == null || index < 0) return false;
            if (index >= _text.textInfo.characterCount) return false;

            char c = _text.textInfo.characterInfo[index].character;
            return c == '，' || c == '。' || c == '、' || c == '！' || c == '？' ||
                   c == '；' || c == '：' || c == '…' || c == '—';
        }

        /// <summary>调试/取证：第 index 个字符左下顶点的 Y（TMP 本地坐标）。</summary>
        public float GetCharacterVertexY(int index)
        {
            if (_text == null) return 0f;

            TMP_TextInfo info = _text.textInfo;
            if (index < 0 || index >= info.characterCount) return 0f;

            TMP_CharacterInfo character = info.characterInfo[index];
            Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            return character.vertexIndex < vertices.Length ? vertices[character.vertexIndex].y : 0f;
        }

#if UNITY_EDITOR
        /// <summary>调试/取证：立即播完。仅编辑器可用。</summary>
        public void CompleteInstantly()
        {
            if (_text == null) return;

            _progress = TotalChars;
            _text.maxVisibleCharacters = TotalChars;
            State = WorldTextState.Complete;
            if (_group != null) _group.alpha = 1f;
        }
#endif
    }
}
