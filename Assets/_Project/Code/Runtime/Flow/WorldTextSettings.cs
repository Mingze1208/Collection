using System;
using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>世界空间文本配置：每条 = 一处文本 + 一个触发区域。</summary>
    [CreateAssetMenu(menuName = "Collection/World Text Set", fileName = "SO_WorldTexts")]
    public sealed class WorldTextSettings : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("文本在世界中的位置（显示锚点）。")]
            public Vector2 TextPosition;

            [Tooltip("触发区域中心。")]
            public Vector2 TriggerCenter;

            [Tooltip("触发区域大小（宽 × 高，世界单位）。")]
            public Vector2 TriggerSize;

            [Tooltip("文本排版宽度（世界单位）。")]
            public float Width;

            [TextArea(2, 5)]
            public string Text;
        }

        [SerializeField] private Entry[] _entries = new Entry[0];

        [Tooltip("逐字浮现速度（字/秒）。")]
        [SerializeField, Min(1f)] private float _charsPerSecond = 12f;

        [Tooltip("标点后的额外停顿（s）。")]
        [SerializeField, Min(0f)] private float _punctuationPause = 0.15f;

        [Header("波浪动效（<wave>…</wave>）")]
        [Tooltip("是否启用波浪。")]
        [SerializeField] private bool _waveEnabled = true;

        [Tooltip("振幅 = 字高 × 该比例（0.14 ≈ 字高的 14%）。")]
        [SerializeField, Range(0f, 0.5f)] private float _waveAmplitudePercent = 0.14f;

        [Tooltip("波动频率（Hz 的角速度倍数）。")]
        [SerializeField, Min(0f)] private float _waveFrequency = 4f;

        [Tooltip("相邻字的相位差（弧度），决定“波浪”的推进感。")]
        [SerializeField, Min(0f)] private float _wavePhaseStep = 0.6f;

        public Entry[] Entries => _entries;
        public float CharsPerSecond => _charsPerSecond;
        public float PunctuationPause => _punctuationPause;
        public bool WaveEnabled => _waveEnabled;
        public float WaveAmplitudePercent => _waveAmplitudePercent;
        public float WaveFrequency => _waveFrequency;
        public float WavePhaseStep => _wavePhaseStep;

        public void SetEntries(Entry[] entries) => _entries = entries;
    }
}
