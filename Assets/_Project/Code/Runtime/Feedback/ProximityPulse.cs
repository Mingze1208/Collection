using Collection.Runtime.Enemy;
using Collection.Runtime.Player;
using UnityEngine;

namespace Collection.Runtime.Feedback
{
    /// <summary>
    /// 接近脉冲（D-3）：敌人处于 Seek/Camp 且距离 ≤ 6 m 时，屏幕边缘出现低频脉冲，
    /// 越近越快、越亮；预计 0.3 s 内被抓时闪红一次。
    /// 只提示“危险程度”，不给方向——它是压迫感提示，不是雷达。
    /// </summary>
    public sealed class ProximityPulse : MonoBehaviour
    {
        [SerializeField] private EnemyController _enemy;
        [SerializeField] private PlayerController _player;

        private Texture2D _vignette;
        private float _flashUntil;
        private bool _flashArmed = true;

        public bool IsActive { get; private set; }

        /// <summary>0 = 范围边缘，1 = 贴身。</summary>
        public float Intensity { get; private set; }

        /// <summary>当前脉冲频率（Hz）。</summary>
        public float Frequency { get; private set; }

        public float Distance { get; private set; }

        /// <summary>预计还有多久被抓（s），无法计算时为 -1。</summary>
        public float TimeToCatch { get; private set; } = -1f;

        public bool IsFlashing => Time.time < _flashUntil;
        public int FlashCount { get; private set; }

        public void Initialize(EnemyController enemy, PlayerController player)
        {
            _enemy = enemy;
            _player = player;
        }

        private void Awake()
        {
            _vignette = BuildVignette(96);
        }

        private void OnDestroy()
        {
            if (_vignette != null) Destroy(_vignette);
        }

        private void Update()
        {
            if (_enemy == null || _player == null || !_enemy.isActiveAndEnabled)
            {
                IsActive = false;
                Intensity = 0f;
                return;
            }

            EnemySettings settings = _enemy.Settings;
            if (settings == null)
            {
                IsActive = false;
                return;
            }

            Distance = Vector2.Distance(_player.transform.position, _enemy.transform.position);
            float range = Mathf.Max(0.5f, settings.PulseRange);

            bool chasing = _enemy.State == EnemyState.Seek || _enemy.State == EnemyState.Camp;
            IsActive = chasing && Distance <= range;

            Intensity = IsActive ? 1f - Mathf.Clamp01(Distance / range) : 0f;
            Frequency = Mathf.Lerp(settings.PulseSlowHz, settings.PulseFastHz, Intensity);

            UpdateFlash(settings);
        }

        private void UpdateFlash(EnemySettings settings)
        {
            if (!IsActive)
            {
                TimeToCatch = -1f;
                _flashArmed = true;
                return;
            }

            float travel = Distance - _enemy.KillDistance;
            TimeToCatch = travel <= 0f ? 0f : travel / Mathf.Max(0.1f, _enemy.MoveSpeed);

            // 只有“正在追过来”才预警：驻留中的敌人不会突然抓人
            bool approaching = _enemy.State == EnemyState.Seek;
            if (approaching && TimeToCatch <= settings.FlashLeadTime && _flashArmed)
            {
                _flashUntil = Time.time + 0.35f;
                _flashArmed = false;
                FlashCount++;
                Debug.Log($"[Pulse] 闪红 #{FlashCount}：预计 {TimeToCatch:0.00} s 后被抓（距离 {Distance:0.00} m）");
            }
            else if (TimeToCatch > settings.FlashLeadTime * 2f)
            {
                _flashArmed = true;
            }
        }

        private void OnGUI()
        {
            if (!IsActive || _vignette == null) return;

            // 低频脉冲：alpha 随相位起伏
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Frequency * Mathf.PI * 2f);
            float alpha = Mathf.Lerp(0.05f, 0.32f, Intensity) * Mathf.Lerp(0.35f, 1f, pulse);
            Color tint = Color.Lerp(new Color(1f, 0.93f, 0.75f), new Color(1f, 0.35f, 0.28f), Intensity);
            tint.a = alpha;

            GUI.color = tint;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _vignette);
            GUI.color = Color.white;

            if (!IsFlashing) return;

            // 抓前闪红：一次 0.35 s 的强红，随剩余时间衰减
            float remain = Mathf.Clamp01((_flashUntil - Time.time) / 0.35f);
            Color flash = new Color(1f, 0.15f, 0.12f, 0.45f * remain);
            GUI.color = flash;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _vignette);
            GUI.color = Color.white;
        }

        /// <summary>生成一张“中心透明、边缘不透明”的暗角贴图。</summary>
        private static Texture2D BuildVignette(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (size - 1f) * 2f - 1f;
                    float ny = y / (size - 1f) * 2f - 1f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny) / Mathf.Sqrt(2f);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, distance));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
