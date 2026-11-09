using Collection.Runtime.Ripple;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Collection.Runtime.Player
{
    /// <summary>
    /// 玩家输入：方向键驱动“一步一印”的迈步（交给 FootStepper），空格唱歌。
    /// 移动不再是连续速度：单击走一步，按住按 stepInterval 连续迈步，改方向立即迈步。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private RippleSystem _ripple;
        [SerializeField] private FootStepper _stepper;

        private Vector2 _moveInput;
        private Vector2 _lastStepDirection;
        private float _stepTimer;
        private bool _debugDrive;
        private Vector2 _debugMove;
        private bool _inputEnabled = true;

        public bool IsMoving => _moveInput.sqrMagnitude > 0.0001f;
        public Vector2 MoveInput => _moveInput;

        /// <summary>唱歌（空格）次数。</summary>
        public int EmitCount { get; private set; }

        public void Initialize(RippleSystem ripple, FootStepper stepper)
        {
            _ripple = ripple;
            _stepper = stepper;
        }

        /// <summary>调试/取证：代替键盘持续给出移动方向（不改变正常输入路径）。</summary>
        public void SetDebugDrive(Vector2 direction)
        {
            _debugDrive = true;
            _debugMove = direction;
        }

        public void ClearDebugDrive()
        {
            _debugDrive = false;
            _debugMove = Vector2.zero;
        }

        /// <summary>调试/取证入口：等价于按一次发声键（脚本或外部工具可调用）。</summary>
        public void EmitOnce() => Emit();

        /// <summary>调试/取证入口：立即朝给定方向迈一步。</summary>
        public bool StepOnce(Vector2 direction) => _stepper != null && _stepper.TryStep(direction);

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

            if (_stepper.TryStep(direction))
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

        private void SampleInput()
        {
            if (_debugDrive)
            {
                _moveInput = _debugMove.sqrMagnitude > 0.0001f ? _debugMove.normalized : Vector2.zero;
                return;
            }

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
            _ripple.Emit(transform.position, RippleSource.Voice);
            EmitCount++;
        }
    }
}
