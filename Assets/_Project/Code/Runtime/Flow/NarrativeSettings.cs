using UnityEngine;

namespace Collection.Runtime.Flow
{
    /// <summary>首段叙事文本数据（放 Data/，改文案不用改代码）。</summary>
    [CreateAssetMenu(menuName = "Collection/Narrative", fileName = "SO_Narrative")]
    public sealed class NarrativeSettings : ScriptableObject
    {
        [SerializeField, TextArea(2, 4)] private string[] _lines = new string[0];

        [Tooltip("每行停留时间（s）。")]
        [SerializeField, Min(0.2f)] private float _lineDuration = 3f;

        [Tooltip("淡入淡出时间（s）。")]
        [SerializeField, Min(0f)] private float _fadeTime = 0.4f;

        [Tooltip("进场后延迟多久开始（s）。")]
        [SerializeField, Min(0f)] private float _startDelay = 1f;

        public string[] Lines => _lines;
        public float LineDuration => _lineDuration;
        public float FadeTime => _fadeTime;
        public float StartDelay => _startDelay;

        /// <summary>总字数（用于验收“≤150 字”）。</summary>
        public int TotalCharacters
        {
            get
            {
                int total = 0;
                foreach (string line in _lines) total += line?.Length ?? 0;
                return total;
            }
        }
    }
}
