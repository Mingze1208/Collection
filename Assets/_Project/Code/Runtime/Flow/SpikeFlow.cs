using Collection.Runtime.Enemy;
using Collection.Runtime.Player;
using Collection.Runtime.Ripple;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collection.Runtime.Flow
{
    public enum SpikeState
    {
        Playing = 0,
        GameOver = 1,
    }

    /// <summary>
    /// 失败循环：敌人接触玩家 → GameOver 覆盖层 → 重试（回到房间入口）或返回主菜单（D1 占位）。
    /// 重试是就地复位，不做场景重载，所以“覆盖层出现到可再次移动”耗时在毫秒级。
    /// </summary>
    public sealed class SpikeFlow : MonoBehaviour
    {
        [SerializeField] private PlayerController _player;
        [SerializeField] private FootStepper _stepper;
        [SerializeField] private EnemyController[] _enemies = new EnemyController[0];
        [SerializeField] private RippleSystem _ripple;

        [Header("复活点")]
        [SerializeField] private Vector2 _playerSpawn = new Vector2(-2.5f, -2.5f);
        [SerializeField] private Vector2[] _enemySpawns = new Vector2[0];

        public SpikeState State { get; private set; } = SpikeState.Playing;
        public int DeathCount { get; private set; }
        public int RetryCount { get; private set; }

        /// <summary>从按下重试到玩家重新可移动的耗时（ms）。</summary>
        public float RetryCostMs { get; private set; } = -1f;

        /// <summary>覆盖层出现到按下重试之间停留了多久（ms，含玩家反应时间）。</summary>
        public float OverlayDwellMs { get; private set; } = -1f;

        public string MenuNotice { get; private set; } = string.Empty;

        /// <summary>当前复活点（默认章节起点，经过检查点后更新）。</summary>
        public Vector2 RespawnPoint => _respawnPoint;

        /// <summary>当前生效的检查点序号（-1 = 章节起点）。</summary>
        public int CheckpointIndex => _checkpointIndex;

        /// <summary>
        /// 设置复活点：只允许“更靠后的检查点”覆盖，避免回头踩旧检查点把进度退回去。
        /// 死亡与重试不会重置检查点。
        /// </summary>
        public bool SetCheckpoint(Vector2 position, int index)
        {
            if (index <= _checkpointIndex) return false;

            _respawnPoint = position;
            _checkpointIndex = index;
            return true;
        }

        private float _deathRealtime;
        private Vector2 _respawnPoint;
        private int _checkpointIndex = -1;
        private float _playerRadius = 0.3f;
        private float _enemyRadius = 0.3f;

        /// <summary>敌人数量（HUD / 取证用）。</summary>
        public int EnemyCount => _enemies != null ? _enemies.Length : 0;

        /// <summary>实际使用的致命距离：参数值，但绝不会小于两者碰撞体接触距离。</summary>
        public float EffectiveKillDistance =>
            Mathf.Max(_enemies != null && _enemies.Length > 0 && _enemies[0] != null
                ? _enemies[0].KillDistance
                : 0.55f,
                _playerRadius + _enemyRadius + 0.02f);

        public void Initialize(PlayerController player, FootStepper stepper, EnemyController[] enemies,
            RippleSystem ripple, Vector2 playerSpawn, Vector2[] enemySpawns)
        {
            _player = player;
            _stepper = stepper;
            _enemies = enemies ?? new EnemyController[0];
            _ripple = ripple;
            _playerSpawn = playerSpawn;
            _enemySpawns = enemySpawns ?? new Vector2[0];
            _respawnPoint = playerSpawn;

            _playerRadius = RadiusOf(player != null ? player.GetComponent<Collider2D>() : null);
            _enemyRadius = RadiusOf(_enemies.Length > 0 && _enemies[0] != null
                ? _enemies[0].GetComponent<Collider2D>()
                : null);
        }

        private static float RadiusOf(Collider2D collider)
        {
            if (collider == null) return 0.3f;
            return Mathf.Max(0.05f, collider.bounds.extents.x);
        }

        private void Awake()
        {
            // _respawnPoint 不参与序列化，进入运行时必须从起点初始化
            _respawnPoint = _playerSpawn;
        }

        private void Update()
        {
            if (State == SpikeState.Playing)
            {
                CheckCaught();
                return;
            }

            HandleGameOverInput();
        }

        private void CheckCaught()
        {
            if (_player == null) return;

            Vector2 playerPosition = _player.transform.position;
            for (int i = 0; i < _enemies.Length; i++)
            {
                EnemyController enemy = _enemies[i];
                if (enemy == null) continue;

                float distance = Vector2.Distance(playerPosition, enemy.transform.position);
                if (distance > EffectiveKillDistance) continue;

                Die(distance);
                return;
            }
        }

        public void Die() => Die(-1f);

        public void Die(float distance, string reason = null)
        {
            if (State == SpikeState.GameOver) return;

            State = SpikeState.GameOver;
            DeathCount++;
            _deathRealtime = Time.realtimeSinceStartup;
            MenuNotice = string.Empty;

            if (_player != null) _player.SetInputEnabled(false);
            StopEnemies();
            if (_ripple != null) _ripple.Clear();

            string cause = string.IsNullOrEmpty(reason) ? "敌人接触到玩家" : reason;
            Debug.Log(
                $"[Flow] GameOver #{DeathCount}：{cause}" +
                (distance >= 0f ? $"（距离 {distance:0.00} m，判定阈值 {EffectiveKillDistance:0.00} m）" : string.Empty));
        }

        private void HandleGameOverInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.rKey.wasPressedThisFrame)
            {
                Retry();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                MenuNotice = "主菜单尚未实现（D1 范围外）";
                Debug.Log($"[Flow] 返回主菜单：{MenuNotice}");
            }
        }

        /// <summary>重试：玩家回到房间入口，敌人回到出生点。</summary>
        public void Retry()
        {
            float start = Time.realtimeSinceStartup;
            OverlayDwellMs = (start - _deathRealtime) * 1000f;

            if (_player != null)
            {
                _player.Teleport(_respawnPoint);
                _player.SetInputEnabled(true);
            }

            if (_stepper != null) _stepper.ResetFeet();
            if (_ripple != null) _ripple.Clear();
            ResetEnemies();

            State = SpikeState.Playing;
            RetryCount++;
            RetryCostMs = (Time.realtimeSinceStartup - start) * 1000f;

            Debug.Log(
                $"[Flow] Retry #{RetryCount}：玩家 → ({_respawnPoint.x:0.00},{_respawnPoint.y:0.00})，" +
                $"敌人 {EnemyCount} 个回到出生点；" +
                $"覆盖层停留 {OverlayDwellMs:0.0} ms，重试生效 {RetryCostMs:0.00} ms");
        }

        private void StopEnemies()
        {
            foreach (EnemyController enemy in _enemies)
            {
                if (enemy != null) enemy.Stop();
            }
        }

        private void ResetEnemies()
        {
            for (int i = 0; i < _enemies.Length; i++)
            {
                if (_enemies[i] == null) continue;

                Vector2 spawn = i < _enemySpawns.Length ? _enemySpawns[i] : _enemies[i].transform.position;
                _enemies[i].ResetTo(spawn);
            }
        }

        private void OnGUI()
        {
            if (State != SpikeState.GameOver) return;

            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 44,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.45f, 0.45f) },
            };
            var body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            var hint = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.85f) },
            };

            float centerY = Screen.height * 0.5f - 90f;
            GUI.Label(new Rect(0f, centerY, Screen.width, 60f), "GAME OVER", title);
            GUI.Label(new Rect(0f, centerY + 70f, Screen.width, 30f),
                $"按 R 重试（回到房间入口）　·　按 Esc 返回主菜单", body);
            GUI.Label(new Rect(0f, centerY + 110f, Screen.width, 24f),
                $"已死亡 {DeathCount} 次" +
                (RetryCostMs >= 0f ? $"　上次重试生效 {RetryCostMs:0.00} ms" : string.Empty),
                hint);

            if (!string.IsNullOrEmpty(MenuNotice))
            {
                GUI.Label(new Rect(0f, centerY + 140f, Screen.width, 24f), MenuNotice, hint);
            }
        }
    }
}
