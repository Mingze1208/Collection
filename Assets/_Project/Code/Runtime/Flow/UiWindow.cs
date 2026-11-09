using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Collection.Runtime.Flow
{
    /// <summary>
    /// 通用弹出窗口（prefab 实例）：宽屏遮罩 + 标题 + 两个按钮。
    /// 关卡完成（下一关/主菜单）与死亡（重试/主菜单）共用同一个 prefab，内容在运行时配置。
    /// 键盘：Enter/N = 主动作，Esc = 次动作。
    /// </summary>
    public sealed class UiWindow : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private Button _primary;
        [SerializeField] private TMP_Text _primaryLabel;
        [SerializeField] private Button _secondary;
        [SerializeField] private TMP_Text _secondaryLabel;
        [SerializeField] private string _menuScene = "SC_MainMenu";

        private Action _primaryAction;
        private Action _secondaryAction;

        public bool IsVisible => gameObject.activeSelf;
        public int PrimaryClicks { get; private set; }
        public int SecondaryClicks { get; private set; }
        public string TitleText => _title != null ? _title.text : string.Empty;

        public void Initialize(TMP_Text title, Button primary, TMP_Text primaryLabel,
            Button secondary, TMP_Text secondaryLabel)
        {
            _title = title;
            _primary = primary;
            _primaryLabel = primaryLabel;
            _secondary = secondary;
            _secondaryLabel = secondaryLabel;

            if (_primary != null) _primary.onClick.AddListener(ClickPrimary);
            if (_secondary != null) _secondary.onClick.AddListener(ClickSecondary);

            gameObject.SetActive(false);
        }

        public void Show(string title, string primaryLabel, Action primary,
            string secondaryLabel, Action secondary)
        {
            _primaryAction = primary;
            _secondaryAction = secondary;

            if (_title != null) _title.text = title;
            if (_primaryLabel != null) _primaryLabel.text = primaryLabel;
            if (_secondaryLabel != null) _secondaryLabel.text = secondaryLabel;

            if (_primary != null) _primary.gameObject.SetActive(!string.IsNullOrEmpty(primaryLabel));
            if (_secondary != null) _secondary.gameObject.SetActive(!string.IsNullOrEmpty(secondaryLabel));

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!IsVisible) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.nKey.wasPressedThisFrame)
            {
                ClickPrimary();
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                ClickSecondary();
            }
        }

        public void ClickPrimary()
        {
            PrimaryClicks++;
            if (_primaryAction != null) _primaryAction();
        }

        public void ClickSecondary()
        {
            SecondaryClicks++;
            if (_secondaryAction != null) _secondaryAction();
        }

        /// <summary>返回主菜单（占位：没有主菜单场景时只打日志）。</summary>
        public void LoadMenu()
        {
            Debug.Log($"[UiWindow] 返回主菜单 → {_menuScene}");
            if (Application.CanStreamedLevelBeLoaded(_menuScene))
            {
                SceneManager.LoadScene(_menuScene);
            }
            else
            {
                Debug.LogWarning($"[UiWindow] 场景 {_menuScene} 不在 Build Settings 里");
            }
        }
    }
}
