using Collection.Runtime.Enemy;
using Collection.Runtime.Feedback;
using Collection.Runtime.Flow;
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
        [SerializeField] private EnemyController[] _enemies = new EnemyController[0];
        [SerializeField] private ProximityPulse _pulse;
        [SerializeField] private SpikeFlow _flow;

        private float _smoothedFps = 60f;

        public void Initialize(RippleSystem ripple, PlayerController player, FootStepper stepper,
            EnemyController[] enemies, ProximityPulse pulse, SpikeFlow flow)
        {
            _ripple = ripple;
            _player = player;
            _stepper = stepper;
            _enemies = enemies ?? new EnemyController[0];
            _pulse = pulse;
            _flow = flow;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) _smoothedFps = Mathf.Lerp(_smoothedFps, 1f / dt, 0.1f);
        }

        private void OnGUI()
        {
#if !UNITY_EDITOR
            // 调试 HUD 只在编辑器里绘制；正式包不显示
            return;
#else
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
                $"唱歌: {settings.Voice.Speed:0.##} m/s × {settings.Voice.Lifetime:0.##} s ⇒ 半径 {settings.Voice.VisualRadius:0.##} m" +
                $"　冷却 {settings.Voice.Cooldown:0.00} s（剩 {(_player != null ? _player.VoiceCooldownRemaining : 0f):0.00}）",
                style);
            GUILayout.Label(
                $"落脚: {settings.Footstep.Speed:0.##} m/s × {settings.Footstep.Lifetime:0.##} s ⇒ 半径 {settings.Footstep.VisualRadius:0.##} m" +
                $"　冷却 {settings.Footstep.Cooldown:0.00} s",
                style);
            GUILayout.Label(
                $"唱 {settings.Voice.DirectionCount}×{settings.Voice.TrailDots}={settings.Voice.DotCount} 点/次" +
                $"　落脚 {settings.Footstep.DirectionCount}×{settings.Footstep.TrailDots}={settings.Footstep.DotCount} 点/次",
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
            GUILayout.Label(BreathLine(), style);
            GUILayout.Label(PulseLine(), style);
            GUILayout.Label(CheckpointLine(), style);
            GUILayout.Label(DropletLine(), style);
            GUILayout.Label("WASD 移动（落脚发声，绿点=有声落脚） · 按住 Shift 屏息静步（蓝点=静步） · 空格 唱歌", style);
            GUILayout.EndArea();
#endif
        }

        private string EnemyLine()
        {
            if (_enemies == null || _enemies.Length == 0) return "敌人: 未配置";

            var builder = new System.Text.StringBuilder("敌人: ");
            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyController enemy = _enemies[i];
                if (enemy == null) continue;

                if (i > 0) builder.Append("　|　");

                Vector3 position = enemy.transform.position;
                string seek = enemy.LastSeekSeconds >= 0f ? $"{enemy.LastSeekSeconds:0.00}" : "—";
                builder.Append($"[{i}] {enemy.State} ({position.x:0.0},{position.y:0.0})")
                       .Append($" 到达{seek}s　Seek/Camp {enemy.SeekCount}/{enemy.CampCount}");

                if (_player != null)
                {
                    float distance = Vector2.Distance(_player.transform.position, position);
                    builder.Append($"　距 {distance:0.00} m");
                }
            }

            return builder.ToString();
        }

        private string DropletLine()
        {
            var thrower = _player != null ? _player.GetComponent<DropletThrower>() : null;
            if (thrower == null) return "泪滴: 未配置";

            return $"泪滴: 冷却 {thrower.CooldownRemaining:0.00} s　投掷 {thrower.ThrowCount} 次" +
                   $"　落地 {thrower.LandingCount} 次　冷却挡下 {thrower.BlockedCount} 次" +
                   $"　落点 ({(thrower.LandingCount > 0 ? thrower.LastLanding.x : 0f):0.00}," +
                   $"{(thrower.LandingCount > 0 ? thrower.LastLanding.y : 0f):0.00})";
        }

        private string CheckpointLine()
        {
            if (_flow == null) return "复活点: 未配置";

            Vector2 respawn = _flow.RespawnPoint;
            string index = _flow.CheckpointIndex < 0 ? "章节起点" : $"检查点 #{_flow.CheckpointIndex}";
            return $"复活点 ({respawn.x:0.00},{respawn.y:0.00})　{index}　死亡/重试不重置检查点";
        }

        private string PulseLine()
        {
            if (_pulse == null) return "接近脉冲: 未配置";

            string catchTime = _pulse.TimeToCatch >= 0f ? $"{_pulse.TimeToCatch:0.00} s" : "—";
            return $"接近脉冲: {(_pulse.IsActive ? "开" : "关")}　强度 {_pulse.Intensity:0.00}　频率 {_pulse.Frequency:0.0} Hz" +
                   $"　距离 {_pulse.Distance:0.00} m　预计被抓 {catchTime}　闪红 {_pulse.FlashCount} 次";
        }

        private string BreathLine()
        {
            if (_player == null) return "屏息: —";

            string state = _player.IsHoldingBreath ? "屏息中" : "呼吸中";
            return $"屏息: {_player.BreathRemaining:0.00}/{_player.BreathDuration:0.00} s（{state}）" +
                   $"　静步 {(_stepper != null ? _stepper.SilentSteps : 0)} 次" +
                   $"　耗尽 {_player.BreathExhaustCount} 次";
        }

        private static float SinceLastEmit(RippleSystem ripple)
        {
            float since = Time.time - ripple.LastEmitTime;
            return since < 0f || since > 999f ? -1f : since;
        }
    }
}
