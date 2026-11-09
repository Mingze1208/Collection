using Collection.Runtime.Ripple;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collection.Runtime.Player
{
    /// <summary>
    /// 玩家输入：方向键驱动“一步一印”的迈步（交给 FootStepper），空格唱歌，
    /// 按住 Shift 屏息静步（移动不发声，有时间上限，耗尽会发出大声响）。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private FootStepper _stepper;

        private Vector2 _moveInput;
        private Vector2 _lastStepDirection;
        private float _stepTimer;
        private bool _inputEnabled = true;
#if UNITY_EDITOR
        private bool _debugDrive;
        private Vector2 _debugMove;
        private bool _debugBreath;
#endif
        private float _breath = -1f;
        private bool _breathLocked;
        private float _nextVoiceTime;

        public bool IsMoving => _moveInput.sqrMagnitude > 0.0001f;
        public Vector2 MoveInput => _moveInput;

        /// <summary>唱歌（空格）次数。</summary>
        public int EmitCount { get; private set; }

        /// <summary>当前是否处于屏息静步状态。</summary>
        public bool IsHoldingBreath { get; private set; }

        /// <summary>剩余屏息时间（s）。</summary>
        public float BreathRemaining => Mathf.Max(0f, _breath);

        public float BreathDuration => _ripple != null && _ripple.Settings != null ? _ripple.Settings.BreathDuration : 3f;

        public float BreathNormalized => BreathDuration > 0f ? Mathf.Clamp01(BreathRemaining / BreathDuration) : 0f;

        /// <summary>屏息耗尽的次数（每次会发出一次大声响）。</summary>
        public int BreathExhaustCount { get; private set; }

        /// <summary>唱歌剩余冷却（s），0 = 可唱。</summary>
        public float VoiceCooldownRemaining => Mathf.Max(0f, _nextVoiceTime - Time.time);

        public bool CanEmitVoice => VoiceCooldownRemaining <= 0f;

        /// <summary>被冷却挡下的按键次数（用来验证连点不会叠出很多次发声）。</summary>
        public int VoiceBlockedCount { get; private set; }

        public void Initialize(RippleSystem ripple, FootStepper stepper)
        {
            _ripple = ripple;
            _stepper = stepper;
        }

#if UNITY_EDITOR
        /// <summary>调试/取证：代替键盘持续给出移动方向（不改变正常输入路径）。仅编辑器可用。</summary>
        public void SetDebugDrive(Vector2 direction)
        {
            _debugDrive = true;
            _debugMove = direction;
        }

        /// <summary>调试/取证：停止代替键盘。仅编辑器可用。</summary>
        public void ClearDebugDrive()
        {
            _debugDrive = false;
            _debugMove = Vector2.zero;
        }

        /// <summary>调试/取证入口：等价于按一次发声键。仅编辑器可用。</summary>
        public void EmitOnce() => Emit();

        /// <summary>调试/取证入口：立即朝给定方向迈一步。仅编辑器可用。</summary>
        public bool StepOnce(Vector2 direction, bool silent = false) =>
            _stepper != null && _stepper.TryStep(direction, silent);

        /// <summary>调试/取证：代替 Shift 键强制屏息。仅编辑器可用。</summary>
        public void SetDebugBreath(bool hold) => _debugBreath = hold;
#endif

        /// <summary>GameOver 期间关掉移动与发声输入。</summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (enabled) return;

            _moveInput = Vector2.zero;
            _stepTimer = 0f;
        }

        /// <summary>瞬移（复活用）：同步 transform 与刚体位置。</summary>
        public void Teleport(Vector2 position)
        {
            transform.position = position;
            if (TryGetComponent(out Rigidbody2D body)) body.position = position;
        }

        private void Update()
        {
            if (!_inputEnabled)
            {
                _moveInput = Vector2.zero;
                return;
            }

            SampleInput();
            UpdateBreath();

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Emit();
            }

            UpdateStepping();
        }

        private void UpdateStepping()
        {
            if (_stepper == null) return;

            if (!IsMoving)
            {
                _stepTimer = 0f;
                return;
            }

            Vector2 direction = _moveInput.normalized;
            bool directionChanged = Vector2.Dot(direction, _lastStepDirection) < 0.9f;
            _stepTimer -= Time.deltaTime;

            if (!directionChanged && _stepTimer > 0f) return;

            if (_stepper.TryStep(direction, IsHoldingBreath))
            {
                _lastStepDirection = direction;
                _stepTimer = _stepper.StepInterval;
            }
            else
            {
                // 被墙挡住：按步间隔重试，避免每帧刷计数
                _stepTimer = _stepper.StepInterval;
            }
        }

        /// <summary>屏息：按住 Shift 进入静步；耗尽后必须松手才能恢复。</summary>
        private void UpdateBreath()
        {
            float max = BreathDuration;
            if (_breath < 0f) _breath = max;

            Keyboard keyboard = Keyboard.current;
#if UNITY_EDITOR
            bool holdKey = _debugBreath ||
                           (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed));
#else
            bool holdKey = keyboard != null &&
                           (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#endif

            if (!holdKey) _breathLocked = false;

            if (holdKey && !_breathLocked && _breath > 0f)
            {
                IsHoldingBreath = true;
                _breath -= Time.deltaTime;

                if (_breath <= 0f)
                {
                    _breath = 0f;
                    _breathLocked = true;
                    IsHoldingBreath = false;
                    ExhaustBreath();
                }

                return;
            }

            IsHoldingBreath = false;
            if (!holdKey) _breath = Mathf.Min(max, _breath + Time.deltaTime * BreathRecovery);
        }

        /// <summary>屏息耗尽：发出一次大声响（对应 one-pager“时间到而未停止屏息会发出较大声响”）。</summary>
        private void ExhaustBreath()
        {
            BreathExhaustCount++;
            if (_ripple != null) _ripple.Emit(transform.position, RippleSource.Voice);
            Debug.Log($"[Player] t={Time.time:0.00} 屏息耗尽：在 ({transform.position.x:0.00},{transform.position.y:0.00}) 发出大声响");
        }

        private float BreathRecovery =>
            _ripple != null && _ripple.Settings != null ? _ripple.Settings.BreathRecovery : 1.5f;

        private void SampleInput()
        {
#if UNITY_EDITOR
            if (_debugDrive)
            {
                _moveInput = _debugMove.sqrMagnitude > 0.0001f ? _debugMove.normalized : Vector2.zero;
                return;
            }
#endif

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                _moveInput = Vector2.zero;
                return;
            }

            float x = 0f;
            float y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            _moveInput = new Vector2(x, y).normalized;
        }

        private void Emit()
        {
            if (_ripple == null) return;

            // 冷却：连点空格不会叠出多次发声
            if (Time.time < _nextVoiceTime)
            {
                VoiceBlockedCount++;
                return;
            }

            _ripple.Emit(transform.position, RippleSource.Voice);
            EmitCount++;

            float cooldown = _ripple.Settings != null ? _ripple.Settings.Voice.Cooldown : 0f;
            _nextVoiceTime = Time.time + cooldown;
        }
    }
}
