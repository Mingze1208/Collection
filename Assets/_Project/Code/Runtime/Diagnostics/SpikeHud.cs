using Collection.Runtime.Enemy;
using Collection.Runtime.Player;
using Collection.Runtime.Ripple;
using UnityEngine;

namespace Collection.Runtime.Diagnostics
{
    /// <summary>
    /// Spike 用的极简 HUD（IMGUI），只服务证据截图：参数、点数、落脚校验、帧率。
    /// 正式 UI 不在 D1 范围内。
    /// </summary>
    public sealed class SpikeHud : MonoBehaviour
    {
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private PlayerController _player;
        [SerializeField] private FootStepper _stepper;
        [SerializeField] private EnemyController _enemy;

        private float _smoothedFps = 60f;

        public void Initialize(RippleSystem ripple, PlayerController player, FootStepper stepper, EnemyController enemy)
        {
            _ripple = ripple;
            _player = player;
            _stepper = stepper;
            _enemy = enemy;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / dt, 0.1f);
        }

        private void OnGUI()
        {
            if (_ripple == null || _ripple.Settings == null) return;

            RippleSettings settings = _ripple.Settings;
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
            };

            GUILayout.BeginArea(new Rect(12f, 8f, 620f, 200f));
            GUILayout.Label("SC_Spike — D1 涟漪 spike", style);
            GUILayout.Label(
                $"参数: {settings.Speed:0.##} m/s × {settings.Lifetime:0.##} s ⇒ 半径 ≈ {settings.VisualRadius:0.##} m",
                style);
            GUILayout.Label(
                $"唱歌 {settings.DirectionCount} 方向 × {settings.TrailDots} 尾迹 = {settings.DirectionCount * settings.TrailDots} 点/次" +
                $"　|　落脚 {settings.FootstepDirectionCount} × {settings.FootstepTrailDots} = {settings.FootstepDirectionCount * settings.FootstepTrailDots} 点/次",
                style);
            GUILayout.Label(
                $"活跃: {_ripple.ActiveBurstCount} 次发声 / {_ripple.ActiveDotCount} 点" +
                $"（峰值 {_ripple.PeakDotCount}，上限 {settings.MaxDots}） / 墙 {_ripple.WallCount}",
                style);
            GUILayout.Label(
                $"唱歌 {(_player != null ? _player.EmitCount : 0)} 次　" +
                $"迈步 {(_stepper != null ? _stepper.StepCount : 0)} 次" +
                $"（左{(_stepper != null ? _stepper.LeftSteps : 0)}/右{(_stepper != null ? _stepper.RightSteps : 0)}）" +
                $"　当前本体 [0=左,1=右]={(_stepper != null ? _stepper.CurrentFoot : -1)}" +
                $"　最近发声 {SinceLastEmit(_ripple):0.00} s 前",
                style);
            GUILayout.Label(
                $"上一步: 距离 {(_stepper != null ? _stepper.LastStepDistance : 0f):0.00} m" +
                $"　方向 ({(_stepper != null ? _stepper.LastStepDirection.x : 0f):0.00},{(_stepper != null ? _stepper.LastStepDirection.y : 0f):0.00})" +
                $"　残影灰化 {(_stepper != null ? _stepper.GhostProgress * 100f : 0f):0}%" +
                $"　落点-声源误差 {(_stepper != null ? _stepper.LastOriginError * 100f : 0f):0.0} cm" +
                $"　被挡 {(_stepper != null ? _stepper.BlockedCount : 0)} 次　FPS {_smoothedFps:0}",
                style);
            GUILayout.Label(EnemyLine(), style);
            GUILayout.Label("WASD 移动（落脚发声，粉色方块=落脚点标记） · 空格 唱歌", style);
            GUILayout.EndArea();
        }

        private string EnemyLine()
        {
            if (_enemy == null) return "敌人: 未配置";

            float distance = Vector2.Distance(_enemy.transform.position, _enemy.Target);
            string seek = _enemy.LastSeekSeconds >= 0f ? $"{_enemy.LastSeekSeconds:0.00} s" : "—";

            return $"敌人: {_enemy.State}　目标 ({_enemy.Target.x:0.00},{_enemy.Target.y:0.00})　距离 {distance:0.00} m" +
                   $"　上次/最长到达 {seek}/{_enemy.MaxSeekSeconds:0.00} s" +
                   $"　Seek/Camp {_enemy.SeekCount}/{_enemy.CampCount}　卡墙 {_enemy.StuckCount} 次";
        }

        private static float SinceLastEmit(RippleSystem ripple)
        {
            float since = Time.time - ripple.LastEmitTime;
            return since < 0f || since > 999f ? -1f : since;
        }
    }
}
