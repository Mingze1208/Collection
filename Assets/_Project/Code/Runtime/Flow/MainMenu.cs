using UnityEngine;
using UnityEngine.SceneManagement;

namespace Collection.Runtime.Flow
{
    /// <summary>主菜单：复用同一个 UI 窗口 prefab（标题 + 开始游戏 / 退出）。</summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private UiWindow _window;
        [SerializeField] private string _firstScene = "SC_Level01";

        public void Initialize(UiWindow window, string firstScene)
        {
            _window = window;
            if (!string.IsNullOrEmpty(firstScene)) _firstScene = firstScene;
        }

        private void Start()
        {
            if (_window == null) return;

            _window.Show("涟漪", "开始游戏（Enter）", StartGame, "退出", Quit);
            Debug.Log($"[Menu] 主菜单已显示，开始按钮 → {_firstScene}");
        }

        private void StartGame()
        {
            if (Application.CanStreamedLevelBeLoaded(_firstScene))
            {
                SceneManager.LoadScene(_firstScene);
            }
            else
            {
                Debug.LogWarning($"[Menu] 场景 {_firstScene} 不在 Build Settings 里");
            }
        }

        private void Quit()
        {
            Debug.Log("[Menu] 退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
