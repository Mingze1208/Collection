using System.IO;
using Collection.Runtime.Diagnostics;
using Collection.Runtime.Enemy;
using Collection.Runtime.Flow;
using Collection.Runtime.Player;
using Collection.Runtime.Ripple;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Collection.EditorTools
{
    /// <summary>
    /// 生成 D1 spike 场景（SC_Spike）。场景由脚本生成、不用手写 YAML，
    /// 改布局就改这里再重新生成，保证可复现。
    /// </summary>
    public static class SpikeSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/SC_Spike.unity";
        private const string ArtFolder = "Assets/_Project/Art";
        private const string DataFolder = "Assets/_Project/Data";
        private const string SpritePath = ArtFolder + "/Sprite_White.png";
        private const string CircleSpritePath = ArtFolder + "/Sprite_Circle.png";
        private const string MaterialPath = ArtFolder + "/Mat_SpikeSprite.mat";
        private const string SettingsPath = DataFolder + "/SO_Ripple_A.asset";
        private const string SettingsBPath = DataFolder + "/SO_Ripple_B.asset";
        private const string EnemySettingsPath = DataFolder + "/SO_Enemy.asset";

        private const int SortingWalls = 0;
        private const int SortingDots = 10;
        private const int SortingEnemy = 15;
        private const int SortingPlayer = 20;

        private static readonly Color WallColor = new Color(0.16f, 0.17f, 0.22f, 1f);
        private static readonly Color PlayerColor = new Color(1f, 0.84f, 0.55f, 1f);
        private static readonly Color EnemyColor = new Color(0.95f, 0.32f, 0.36f, 1f);

        // 房间内净空：x,y ∈ (-5, 5)，墙厚 0.5。
        // 东北角（x>1.5, y>2）是一间只有门洞可进的房间：用来测“堵门”。
        private static readonly (string Name, Vector2 Center, Vector2 Size)[] Walls =
        {
            ("Wall_Bottom", new Vector2(0f, -5.25f), new Vector2(11f, 0.5f)),
            ("Wall_Top", new Vector2(0f, 5.25f), new Vector2(11f, 0.5f)),
            ("Wall_Left", new Vector2(-5.25f, 0f), new Vector2(0.5f, 10f)),
            ("Wall_Right", new Vector2(5.25f, 0f), new Vector2(0.5f, 10f)),
            // L 形内墙：竖段 + 横段，横段中间留 0.8 m 门洞（x ∈ [3.2, 4.0]）
            ("Wall_Divider", new Vector2(1.5f, 3.5f), new Vector2(0.3f, 3f)),
            ("Wall_RoomA", new Vector2(2.35f, 2f), new Vector2(1.7f, 0.3f)),
            ("Wall_RoomB", new Vector2(4.5f, 2f), new Vector2(1f, 0.3f)),
        };

        [MenuItem("Collection/Spike/Rebuild SC_Spike Scene", priority = 1)]
        public static void RebuildScene()
        {
            Scene active = EditorSceneManager.GetActiveScene();
            if (active.isDirty)
            {
                Debug.LogError(
                    $"[SpikeSceneBuilder] 当前场景 {active.path} 有未保存改动，已中止生成。" +
                    "请先保存或丢弃，避免覆盖你的工作。");
                return;
            }

            EnsureFolder(ArtFolder);
            EnsureFolder(DataFolder);
            EnsureFolder("Assets/_Project/Scenes");

            Sprite sprite = EnsureWhiteSprite();
            Sprite circle = EnsureCircleSprite();
            Material material = EnsureSpriteMaterial();
            RippleSettings settings = EnsureRippleSettings();
            EnsureRippleSettingsB();

            if (sprite == null || circle == null || material == null || settings == null)
            {
                Debug.LogError("[SpikeSceneBuilder] 依赖资源创建失败（sprite / circle / material / settings），已中止。");
                return;
            }

            // Scene.name 只读，场景名由保存路径决定
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera();

            var roomGo = new GameObject("Room");
            foreach (var wall in Walls)
            {
                CreateWall(roomGo.transform, wall.Name, wall.Center, wall.Size, sprite, material);
            }

            var rippleGo = new GameObject("RippleSystem");
            var dotRoot = new GameObject("Dots");
            dotRoot.transform.SetParent(rippleGo.transform, false);
            var ripple = rippleGo.AddComponent<RippleSystem>();
            ripple.Initialize(settings, roomGo.transform, dotRoot.transform, circle, material);

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(-2.5f, -2.5f, 0f);
            var player = CreatePlayer(playerGo, sprite, material);

            // 一步一印：本体始终是双脚中的一只，另一只是上一步的残影。
            // 脚挂在独立的 Feet 容器下，不随玩家根节点移动，否则残影会被一起拖走。
            var feetRoot = new GameObject("Feet");
            Transform leftFoot = CreateFoot(feetRoot.transform, "Foot_Left", sprite, material, settings);
            Transform rightFoot = CreateFoot(feetRoot.transform, "Foot_Right", sprite, material, settings);
            var stepper = playerGo.AddComponent<FootStepper>();
            stepper.Initialize(ripple, settings, roomGo.transform, leftFoot, rightFoot, sprite, material);
            player.Initialize(ripple, stepper);

            // 敌人：只靠听，碰到涟漪点就奔向声源
            EnemySettings enemySettings = EnsureEnemySettings();
            if (enemySettings == null)
            {
                Debug.LogError("[SpikeSceneBuilder] 敌人设置创建失败，已中止。");
                return;
            }

            var enemyGo = new GameObject("Enemy");
            enemyGo.transform.position = new Vector3(3f, -2f, 0f);
            var enemy = CreateEnemy(enemyGo, sprite, material);
            enemy.Initialize(ripple, enemySettings);

            // 失败循环：接触 → GameOver 覆盖层 → 重试回到房间入口
            var flowGo = new GameObject("GameFlow");
            var flow = flowGo.AddComponent<SpikeFlow>();
            flow.Initialize(player, stepper, enemy, ripple,
                new Vector2(-2.5f, -2.5f), new Vector2(3f, -2f));

            var hud = rippleGo.AddComponent<SpikeHud>();
            hud.Initialize(ripple, player, stepper, enemy);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[SpikeSceneBuilder] 保存场景失败：{ScenePath}");
                return;
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[SpikeSceneBuilder] 已生成 {ScenePath}：{Walls.Length} 面墙，" +
                $"参数 {settings.Speed} m/s × {settings.Lifetime} s（半径≈{settings.VisualRadius} m），" +
                $"{settings.DirectionCount} 方向 × {settings.TrailDots} 尾迹点，上限 {settings.MaxDots}。" +
                $"墙体 sprite={sprite.name}，尾迹 sprite={circle.name}（ppu={circle.pixelsPerUnit}）。");
        }

        private static void CreateCamera()
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            // URP 下相机需要额外的相机数据组件，缺失时由该扩展方法补上
            camera.GetUniversalAdditionalCameraData();
        }

        private static void CreateWall(Transform parent, string name, Vector2 center, Vector2 size,
            Sprite sprite, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(center.x, center.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = WallColor;
            renderer.sortingOrder = SortingWalls;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
        }

        private static PlayerController CreatePlayer(GameObject go, Sprite sprite, Material material)
        {
            // 视觉交给两只脚；本体位置由 FootStepper 离散步进设置
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.3f;

            return go.AddComponent<PlayerController>();
        }

        private static Transform CreateFoot(Transform parent, string name, Sprite sprite, Material material,
            RippleSettings settings)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one * settings.FootSize;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = PlayerColor;
            renderer.sortingOrder = SortingPlayer;

            return go.transform;
        }

        private static EnemyController CreateEnemy(GameObject go, Sprite sprite, Material material)
        {
            go.transform.localScale = Vector3.one * 0.6f;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = EnemyColor;
            renderer.sortingOrder = SortingEnemy;

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f; // 缩放 0.6 ⇒ 世界半径 0.3 m

            return go.AddComponent<EnemyController>();
        }

        private static Sprite EnsureWhiteSprite()
        {
            return EnsureSprite(SpritePath, 16, false);
        }

        /// <summary>尾迹元素：实心圆形（抗锯齿边缘），1 世界单位直径。</summary>
        private static Sprite EnsureCircleSprite()
        {
            return EnsureSprite(CircleSpritePath, 64, true);
        }

        private static Sprite EnsureSprite(string spritePath, int size, bool circle)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing != null) return existing;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float radius = size * 0.5f - 1f;
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    byte alpha = 255;
                    if (circle)
                    {
                        float dx = x - center;
                        float dy = y - center;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = (byte)(Mathf.Clamp01((radius - distance) / 1.5f) * 255f);
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(ToAbsolutePath(spritePath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(spritePath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
            if (importer == null)
            {
                Debug.LogError($"[SpikeSceneBuilder] 无法读取导入设置：{spritePath}");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size; // size px / size ppu = 1 世界单位
            importer.filterMode = circle ? FilterMode.Bilinear : FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static Material EnsureSpriteMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                Debug.LogError("[SpikeSceneBuilder] 找不到可用的 Sprite 着色器。");
                return null;
            }

            var material = new Material(shader) { name = "Mat_SpikeSprite" };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static RippleSettings EnsureRippleSettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RippleSettings>(SettingsPath);
            if (existing != null) return existing;

            return CreateRippleProfile(SettingsPath, 1f, 2f);
        }

        /// <summary>B 组候选：3 m/s × 2.5 s（≈7.5 m），只改这两个数，其余与 A 组一致。</summary>
        private static RippleSettings EnsureRippleSettingsB()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RippleSettings>(SettingsBPath);
            if (existing != null) return existing;

            return CreateRippleProfile(SettingsBPath, 3f, 2.5f);
        }

        private static RippleSettings CreateRippleProfile(string path, float speed, float lifetime)
        {
            var settings = ScriptableObject.CreateInstance<RippleSettings>();
            AssetDatabase.CreateAsset(settings, path);

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("_speed").floatValue = speed;
            serialized.FindProperty("_lifetime").floatValue = lifetime;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return settings;
        }

        private static EnemySettings EnsureEnemySettings()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EnemySettings>(EnemySettingsPath);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<EnemySettings>();
            AssetDatabase.CreateAsset(settings, EnemySettingsPath);
            return settings;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
