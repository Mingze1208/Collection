using Collection.Runtime.Flow;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Collection.EditorTools
{
    /// <summary>
    /// 生成 UI 窗口 prefab（不手写 prefab YAML）：黑色遮罩 + 标题 + 两个按钮。
    /// 菜单：Collection / UI / Rebuild UI Prefab
    /// </summary>
    public static class UiPrefabBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string WindowPath = PrefabFolder + "/P_UiWindow.prefab";
        private const string FontAssetPath = "Assets/_Project/Art/Font/font1716_TMP.asset";

        [MenuItem("Collection/UI/Rebuild UI Prefab", priority = 20)]
        public static void Rebuild()
        {
            EnsureFolder(PrefabFolder);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (font == null)
            {
                Debug.LogError($"[UiPrefabBuilder] 找不到 TMP 字体：{FontAssetPath}");
                return;
            }

            GameObject root = BuildWindow(font);
            PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UiPrefabBuilder] 已生成 {WindowPath}");
        }

        private static GameObject BuildWindow(TMP_FontAsset font)
        {
            var root = new GameObject("P_UiWindow", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(UiWindow));

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // 黑色遮罩
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root.transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 1f);

            // 标题
            TMP_Text title = CreateText(root.transform, "Title", font, 64f,
                new Vector2(0f, 120f), new Vector2(1200f, 120f));
            title.text = "章节结束";

            // 主按钮
            Button primary = CreateButton(root.transform, "Primary", font, "下一关", new Vector2(0f, -40f));
            TMP_Text primaryLabel = primary.GetComponentInChildren<TMP_Text>();

            // 次按钮
            Button secondary = CreateButton(root.transform, "Secondary", font, "返回主菜单", new Vector2(0f, -170f));
            TMP_Text secondaryLabel = secondary.GetComponentInChildren<TMP_Text>();

            var window = root.GetComponent<UiWindow>();
            window.Initialize(title, primary, primaryLabel, secondary, secondaryLabel);

            return root;
        }

        private static TMP_Text CreateText(Transform parent, string name, TMP_FontAsset font,
            float size, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.82f, 0.95f, 1f);
            text.raycastTarget = false;

            return text;
        }

        private static Button CreateButton(Transform parent, string name, TMP_FontAsset font,
            string label, Vector2 anchoredPosition)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(420f, 96f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.16f, 0.22f, 0.32f, 0.95f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = CreateText(go.transform, "Label", font, 36f, Vector2.zero, new Vector2(400f, 80f));
            text.text = label;

            return button;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
