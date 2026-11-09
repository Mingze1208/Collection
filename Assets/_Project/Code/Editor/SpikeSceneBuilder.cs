using System.IO;
using Collection.Runtime.Diagnostics;
using Collection.Runtime.Enemy;
using Collection.Runtime.Feedback;
using Collection.Runtime.Flow;
using Collection.Runtime.Player;
using Collection.Runtime.Ripple;
using TMPro;
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
        private const string CorridorPath = "Assets/_Project/Scenes/SC_Corridor.unity";
        private const string Level01Path = "Assets/_Project/Scenes/SC_Level01.unity";
        private const string ArtFolder = "Assets/_Project/Art";
        private const string DataFolder = "Assets/_Project/Data";
        private const string SpritePath = ArtFolder + "/Sprite_White.png";
        private const string CircleSpritePath = ArtFolder + "/Sprite_Circle.png";
        private const string MaterialPath = ArtFolder + "/Mat_SpikeSprite.mat";
        private const string SettingsPath = DataFolder + "/SO_Ripple_A.asset";
        private const string SettingsBPath = DataFolder + "/SO_Ripple_B.asset";
        private const string SettingsV2Path = DataFolder + "/SO_Ripple_V2.asset";
        private const string NarrativePath = DataFolder + "/SO_Narrative_Ch1.asset";
        private const string WorldTextsPath = DataFolder + "/SO_WorldTexts_Ch1.asset";
        private const string WorldTextsPath2 = DataFolder + "/SO_WorldTexts_Ch2.asset";
        private const string NarrativePath2 = DataFolder + "/SO_Narrative_Ch2.asset";
        private const string Level02Path = "Assets/_Project/Scenes/SC_Level02.unity";
        private const string Level03Path = "Assets/_Project/Scenes/SC_Level03.unity";
        private const string WorldTextsPath3 = DataFolder + "/SO_WorldTexts_Ch3.asset";
        private const string NarrativePath3 = DataFolder + "/SO_Narrative_Ch3.asset";
        private const string FontAssetPath = "Assets/_Project/Art/Font/font1716_TMP.asset";

        // 首段叙事（hook）：黑暗 → 出声显形 → 唱歌的来处 → “但是”
        private static readonly string[] Chapter1Lines =
        {
            "睁开眼，四周是纯粹的黑暗。",
            "她试着出声——声音划开黑暗，露出一小圈地面。",
            "从前，唱歌是她唯一确定自己存在的方式。",
            "父母说：唱吧，你喜欢就好。",
            "于是她一路唱进了艺术学校。",
            "但是……",
        };
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
            BuildScene(ScenePath, new Vector2(0f, 0f), 6.5f, Walls,
                new Vector2(-2.5f, -2.5f), new[] { new Vector2(3f, -2f) }, "Room");
        }

        // D2 验收用：3 m 宽、20 m 长的直廊（净宽 3 m，墙厚 0.5 m）
        private static readonly (string Name, Vector2 Center, Vector2 Size)[] CorridorWalls =
        {
            ("Wall_Bottom", new Vector2(0f, -1.75f), new Vector2(21f, 0.5f)),
            ("Wall_Top", new Vector2(0f, 1.75f), new Vector2(21f, 0.5f)),
            ("Wall_Left", new Vector2(-10.25f, 0f), new Vector2(0.5f, 4f)),
            ("Wall_Right", new Vector2(10.25f, 0f), new Vector2(0.5f, 4f)),
        };

        [MenuItem("Collection/Spike/Rebuild SC_Corridor Scene", priority = 2)]
        public static void RebuildCorridorScene()
        {
            BuildScene(CorridorPath, new Vector2(-2f, 0f), 3f, CorridorWalls,
                new Vector2(-4f, 0f), new[] { new Vector2(0f, 0f) }, "Corridor");
        }

        // D-4/D-5：第一章连续通路（净宽 3.5 m，纵向起伏 ±1.5 m，全程约 133 m）
        // rects 为“可走区域”，墙体由网格自动生成，避免手写几十面墙
        private static readonly (Vector2 Min, Vector2 Max)[] Level01Rects =
        {
            (new Vector2(0f, -1.75f), new Vector2(27.75f, 1.75f)),
            (new Vector2(24.25f, -1.75f), new Vector2(27.75f, 3.25f)),
            (new Vector2(24.25f, -0.6f), new Vector2(53.75f, 3.1f)),
            (new Vector2(50.25f, -3.25f), new Vector2(53.75f, 3.25f)),
            (new Vector2(50.25f, -3.1f), new Vector2(79.75f, 0.6f)),
            (new Vector2(76.25f, -3.25f), new Vector2(79.75f, 2.75f)),
            (new Vector2(76.25f, -0.75f), new Vector2(106.75f, 2.75f)),
            (new Vector2(103.25f, -1.75f), new Vector2(106.75f, 2.75f)),
            (new Vector2(103.25f, -1.75f), new Vector2(128f, 1.75f)),
        };

        [MenuItem("Collection/Spike/Rebuild SC_Level01 Scene", priority = 3)]
        public static void RebuildLevel01Scene()
        {
            (string, Vector2, Vector2)[] walls = BuildWallsFromRects(Level01Rects, out bool[,] walkable,
                out float originX, out float originY, out int nx, out int ny);

            // 起点 (2,0) → 终点门 (126,0)：网格洪泛验证连通性
            int startX = Mathf.FloorToInt((2f - originX) / GridCell);
            int startY = Mathf.FloorToInt((0f - originY) / GridCell);
            int goalX = Mathf.FloorToInt((126f - originX) / GridCell);
            int goalY = Mathf.FloorToInt((0f - originY) / GridCell);
            int distance = FloodFill(walkable, nx, ny, startX, startY, goalX, goalY);
            float pathMeters = distance * GridCell;

            Debug.Log(distance < 0
                ? "[Level01] 连通性检查失败：起点到终点不可达"
                : $"[Level01] 连通性检查通过：起点→终点 {distance} 格 ≈ {pathMeters:0.0} m，生成 {walls.Length} 面墙");

            BuildScene(Level01Path, new Vector2(2f, 0f), 3f, walls,
                new Vector2(2f, 0f), new[] { new Vector2(60f, -1.5f), new Vector2(88f, 1.2f) }, "Level01",
                followCamera: true, goalPosition: new Vector2(126f, 0f),
                checkpointPositions: new[]
                {
                    new Vector2(30f, 1.25f),
                    new Vector2(55f, -1.25f),
                    new Vector2(95f, 1.0f),
                },
                narrativePath: NarrativePath,
                worldTextsPath: WorldTextsPath,
                trapPosition: new Vector2(74f, -1.25f));
        }

        // 第二章：200 m 直廊 + 1.5 m 缺口（敌人堵在缺口里，必须用泪滴引开）
        private static readonly (string Name, Vector2 Center, Vector2 Size)[] Level02Walls =
        {
            ("Wall_Top", new Vector2(100f, 2.0f), new Vector2(200.5f, 0.5f)),
            ("Wall_Bottom", new Vector2(100f, -2.0f), new Vector2(200.5f, 0.5f)),
            ("Wall_Left", new Vector2(-0.25f, 0f), new Vector2(0.5f, 4.5f)),
            ("Wall_Right", new Vector2(200.5f, 0f), new Vector2(0.5f, 4.5f)),
            // 缺口：x∈[96,98]，只留 y∈[-0.75,0.75] 的 1.5 m
            ("Gate_Top", new Vector2(97f, 1.25f), new Vector2(2f, 1.0f)),
            ("Gate_Bottom", new Vector2(97f, -1.25f), new Vector2(2f, 1.0f)),
        };

        [MenuItem("Collection/Spike/Rebuild SC_Level02 Scene", priority = 4)]
        public static void RebuildLevel02Scene()
        {
            EnsureWorldTextsChapter2();
            EnsureNarrativeChapter2();

            BuildScene(Level02Path, new Vector2(2f, 0f), 3f, Level02Walls,
                new Vector2(2f, 0f), new[]
                {
                    new Vector2(50f, 0f),   // 中段敌人
                    new Vector2(97.5f, 0f), // 缺口守卫：堵在 1.5 m 缺口里（离缺口口 1.3 m，玩家可贴边投掷）
                }, "Level02",
                followCamera: true, goalPosition: new Vector2(198f, 0f),
                checkpointPositions: new[]
                {
                    new Vector2(30f, 1.2f),
                    new Vector2(120f, -1.2f),
                },
                narrativePath: NarrativePath2,
                worldTextsPath: WorldTextsPath2,
                trapPosition: new Vector2(140f, 0.9f));

            Debug.Log("[Level02] 已生成：200 m 直廊，缺口 x∈[96,98] 仅 1.5 m 高，敌人堵在 (97,0)");
        }

        // 第三章：200 m 直廊，3 个敌人 + 结局
        private static readonly (string Name, Vector2 Center, Vector2 Size)[] Level03Walls =
        {
            ("Wall_Top", new Vector2(100f, 2.0f), new Vector2(200.5f, 0.5f)),
            ("Wall_Bottom", new Vector2(100f, -2.0f), new Vector2(200.5f, 0.5f)),
            ("Wall_Left", new Vector2(-0.25f, 0f), new Vector2(0.5f, 4.5f)),
            ("Wall_Right", new Vector2(200.5f, 0f), new Vector2(0.5f, 4.5f)),
        };

        private static readonly string[] Level03EndingLines =
        {
            "《涟漪》",
            "她终于明白：唱歌本身就有意义。",
            "不需要听众，也不需要回声。",
            "黑暗还在，但她不再是那个怕黑的人。",
            "",
            "按 R 重看结局",
        };

        [MenuItem("Collection/Spike/Rebuild SC_Level03 Scene", priority = 5)]
        public static void RebuildLevel03Scene()
        {
            EnsureWorldTextsChapter3();
            EnsureNarrativeChapter3();

            BuildScene(Level03Path, new Vector2(2f, 0f), 3f, Level03Walls,
                new Vector2(2f, 0f), new[]
                {
                    new Vector2(45f, 0f),
                    new Vector2(110f, 0f),
                    new Vector2(160f, 0f),
                }, "Level03",
                followCamera: true, goalPosition: new Vector2(198f, 0f),
                checkpointPositions: new[]
                {
                    new Vector2(30f, 1.2f),
                    new Vector2(100f, -1.2f),
                    new Vector2(150f, 1.2f),
                },
                narrativePath: NarrativePath3,
                worldTextsPath: WorldTextsPath3,
                trapPosition: new Vector2(80f, 0.9f),
                chapterTitle: "第三章",
                endingLines: Level03EndingLines);

            Debug.Log("[Level03] 已生成：200 m 直廊，3 个敌人，结局画面 + R 重看");
        }

        private const float GridCell = 0.5f;
        private const float WallThickness = 0.5f;

        /// <summary>把“可走矩形”的并集边界生成为墙体（外侧 0.5 m 厚），并返回可走网格。</summary>
        private static (string Name, Vector2 Center, Vector2 Size)[] BuildWallsFromRects(
            (Vector2 Min, Vector2 Max)[] rects, out bool[,] walkable,
            out float originX, out float originY, out int nx, out int ny)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var rect in rects)
            {
                minX = Mathf.Min(minX, rect.Min.x);
                minY = Mathf.Min(minY, rect.Min.y);
                maxX = Mathf.Max(maxX, rect.Max.x);
                maxY = Mathf.Max(maxY, rect.Max.y);
            }

            originX = minX - GridCell;
            originY = minY - GridCell;
            nx = Mathf.CeilToInt((maxX - originX) / GridCell) + 1;
            ny = Mathf.CeilToInt((maxY - originY) / GridCell) + 1;

            walkable = new bool[nx, ny];
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    var center = new Vector2(originX + (i + 0.5f) * GridCell, originY + (j + 0.5f) * GridCell);
                    foreach (var rect in rects)
                    {
                        if (center.x < rect.Min.x || center.x > rect.Max.x) continue;
                        if (center.y < rect.Min.y || center.y > rect.Max.y) continue;
                        walkable[i, j] = true;
                        break;
                    }
                }
            }

            var walls = new System.Collections.Generic.List<(string, Vector2, Vector2)>();

            // 水平边（沿 X 的墙）
            for (int j = 0; j <= ny; j++)
            {
                int i = 0;
                while (i < nx)
                {
                    bool below = j > 0 && walkable[i, j - 1];
                    bool above = j < ny && walkable[i, j];
                    if (below == above) { i++; continue; }

                    int start = i;
                    while (i < nx && ((j > 0 && walkable[i, j - 1]) != (j < ny && walkable[i, j]))) i++;

                    float x0 = originX + start * GridCell;
                    float x1 = originX + i * GridCell;
                    float y = originY + j * GridCell + (below ? WallThickness * 0.5f : -WallThickness * 0.5f);

                    walls.Add(($"Wall_H_{walls.Count:00}", new Vector2((x0 + x1) * 0.5f, y),
                        new Vector2(x1 - x0, WallThickness)));
                }
            }

            // 垂直边（沿 Y 的墙）
            for (int i = 0; i <= nx; i++)
            {
                int j = 0;
                while (j < ny)
                {
                    bool left = i > 0 && walkable[i - 1, j];
                    bool right = i < nx && walkable[i, j];
                    if (left == right) { j++; continue; }

                    int start = j;
                    while (j < ny && ((i > 0 && walkable[i - 1, j]) != (i < nx && walkable[i, j]))) j++;

                    float y0 = originY + start * GridCell;
                    float y1 = originY + j * GridCell;
                    float x = originX + i * GridCell + (left ? WallThickness * 0.5f : -WallThickness * 0.5f);

                    walls.Add(($"Wall_V_{walls.Count:00}", new Vector2(x, (y0 + y1) * 0.5f),
                        new Vector2(WallThickness, y1 - y0)));
                }
            }

            // 柱子：0.5×0.5，每 20 m 不超过 2 根（D-5），同时充当反射点
            Vector2[] pillars =
            {
                new Vector2(36f, 2.2f), new Vector2(46f, 2.2f),
                new Vector2(64f, -2.2f), new Vector2(66f, -2.2f),
                new Vector2(92f, 1.9f),
            };
            foreach (Vector2 pillar in pillars)
            {
                walls.Add(($"Pillar_{walls.Count:00}", pillar, new Vector2(0.5f, 0.5f)));
            }

            return walls.ToArray();
        }

        private static int FloodFill(bool[,] walkable, int nx, int ny, int sx, int sy, int gx, int gy)
        {
            if (sx < 0 || sy < 0 || gx < 0 || gy < 0 || sx >= nx || sy >= ny || gx >= nx || gy >= ny) return -1;
            if (!walkable[sx, sy] || !walkable[gx, gy]) return -1;

            var distance = new int[nx, ny];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < ny; j++)
                    distance[i, j] = -1;

            var queue = new System.Collections.Generic.Queue<(int, int)>();
            distance[sx, sy] = 0;
            queue.Enqueue((sx, sy));

            while (queue.Count > 0)
            {
                (int x, int y) = queue.Dequeue();
                if (x == gx && y == gy) return distance[x, y];

                for (int d = 0; d < 4; d++)
                {
                    int nxx = x + (d == 0 ? 1 : d == 1 ? -1 : 0);
                    int nyy = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                    if (nxx < 0 || nyy < 0 || nxx >= nx || nyy >= ny) continue;
                    if (!walkable[nxx, nyy] || distance[nxx, nyy] >= 0) continue;

                    distance[nxx, nyy] = distance[x, y] + 1;
                    queue.Enqueue((nxx, nyy));
                }
            }

            return -1;
        }

        private static void CreateGoal(Vector2 position, PlayerController player, Sprite sprite, Material material,
            string chapterTitle = null, string[] endingLines = null)
        {
            var go = new GameObject("FinishDoor");
            go.transform.position = new Vector3(position.x, position.y, 0f);

            // 视觉：门体 0.6 × 2.4
            var visualGo = new GameObject("DoorVisual");
            visualGo.transform.SetParent(go.transform, false);
            visualGo.transform.localScale = new Vector3(0.6f, 2.4f, 1f);
            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = new Color(0.62f, 0.9f, 1f, 1f);
            renderer.sortingOrder = 5;

            // 判定：矩形 Trigger 1.6 × 2.4
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.6f, 2.4f);

            var goal = go.AddComponent<ChapterGoal>();
            goal.Initialize(player, chapterTitle);

            // 第三章：结局画面（黑场逐行 + R 重看）
            if (endingLines != null && endingLines.Length > 0)
            {
                var ending = go.AddComponent<EndingScreen>();
                ending.Initialize(goal, endingLines);
            }
        }

        private static void CreateCheckpoint(Vector2 position, int index, PlayerController player, SpikeFlow flow,
            Sprite sprite, Material material)
        {
            var go = new GameObject($"Checkpoint_{index}");
            go.transform.position = new Vector3(position.x, position.y, 0f);

            // 视觉：0.6 × 0.6 黄色方块
            var visualGo = new GameObject("CheckpointVisual");
            visualGo.transform.SetParent(go.transform, false);
            visualGo.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = new Color(0.85f, 0.75f, 0.25f, 0.55f);
            renderer.sortingOrder = 4;

            // 判定：覆盖整个走廊断面的矩形 Trigger
            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(3.2f, 5f);

            var checkpoint = go.AddComponent<Checkpoint>();
            checkpoint.Initialize(player, flow, renderer, index);
        }

        private static void BuildScene(string scenePath, Vector2 cameraPosition, float cameraSize,
            (string Name, Vector2 Center, Vector2 Size)[] walls, Vector2 playerSpawn, Vector2[] enemySpawns,
            string worldName, bool followCamera = false, Vector2? goalPosition = null,
            Vector2[] checkpointPositions = null, string narrativePath = null,
            string worldTextsPath = null, Vector2? trapPosition = null,
            string chapterTitle = null, string[] endingLines = null)
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
            RippleSettings settings = EnsureRippleSettingsV2();
            EnsureRippleSettings();
            EnsureRippleSettingsB();

            if (sprite == null || circle == null || material == null || settings == null)
            {
                Debug.LogError("[SpikeSceneBuilder] 依赖资源创建失败（sprite / circle / material / settings），已中止。");
                return;
            }

            // Scene.name 只读，场景名由保存路径决定
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera(cameraPosition, cameraSize);

            var worldGo = new GameObject(worldName);
            foreach (var wall in walls)
            {
                CreateWall(worldGo.transform, wall.Name, wall.Center, wall.Size, sprite, material);
            }

            var rippleGo = new GameObject("RippleSystem");
            var dotRoot = new GameObject("Dots");
            dotRoot.transform.SetParent(rippleGo.transform, false);
            var ripple = rippleGo.AddComponent<RippleSystem>();
            ripple.Initialize(settings, worldGo.transform, dotRoot.transform, circle, material);

            var playerGo = new GameObject("Player");
            playerGo.transform.position = playerSpawn;
            var player = CreatePlayer(playerGo, sprite, material);

            // 一步一印：本体始终是双脚中的一只，另一只是上一步的残影。
            // 脚挂在独立的 Feet 容器下，不随玩家根节点移动，否则残影会被一起拖走。
            var feetRoot = new GameObject("Feet");
            Transform leftFoot = CreateFoot(feetRoot.transform, "Foot_Left", sprite, material, settings);
            Transform rightFoot = CreateFoot(feetRoot.transform, "Foot_Right", sprite, material, settings);
            var stepper = playerGo.AddComponent<FootStepper>();
            stepper.Initialize(ripple, settings, worldGo.transform, leftFoot, rightFoot, sprite, material);
            player.Initialize(ripple, stepper);

            // T1 泪滴投掷：鼠标瞄准，投掷不发声，落地触发落脚档 burst
            var thrower = playerGo.AddComponent<DropletThrower>();
            thrower.Initialize(player, ripple, settings, circle, material);

            // 只有显式传入路径的场景才会有世界文本（避免所有场景共用同一份文本）
            if (!string.IsNullOrEmpty(worldTextsPath))
            {
                WorldTextSettings worldTexts = AssetDatabase.LoadAssetAtPath<WorldTextSettings>(worldTextsPath);
                if (worldTexts != null) CreateWorldTexts(worldTexts, player, camera);
                else Debug.LogError($"[SpikeSceneBuilder] 找不到世界文本资源：{worldTextsPath}");
            }

            if (followCamera)
            {
                var follow = camera.gameObject.AddComponent<CameraFollow>();
                follow.Initialize(playerGo.transform, cameraPosition.y,
                    followY: true, deadzone: 0.6f, yClamp: new Vector2(-1.5f, 1.5f));
            }

            // 敌人：只靠听，碰到涟漪点就奔向声源
            EnemySettings enemySettings = EnsureEnemySettings();
            if (enemySettings == null)
            {
                Debug.LogError("[SpikeSceneBuilder] 敌人设置创建失败，已中止。");
                return;
            }

            var enemies = new EnemyController[enemySpawns.Length];
            for (int i = 0; i < enemySpawns.Length; i++)
            {
                var enemyGo = new GameObject(enemySpawns.Length > 1 ? $"Enemy_{i}" : "Enemy");
                enemyGo.transform.position = enemySpawns[i];
                enemies[i] = CreateEnemy(enemyGo, sprite, material);
                enemies[i].Initialize(ripple, enemySettings);
            }

            // 失败循环：接触 → GameOver 覆盖层 → 重试回到入口
            var flowGo = new GameObject("GameFlow");
            var flow = flowGo.AddComponent<SpikeFlow>();
            flow.Initialize(player, stepper, enemies, ripple, playerSpawn, enemySpawns);

            if (goalPosition.HasValue)
            {
                CreateGoal(goalPosition.Value, player, sprite, material, chapterTitle, endingLines);
            }

            // 同理：字幕也只给显式指定的章节
            if (!string.IsNullOrEmpty(narrativePath))
            {
                NarrativeSettings narrative = AssetDatabase.LoadAssetAtPath<NarrativeSettings>(narrativePath);
                if (narrative != null)
                {
                    var narrativeGo = new GameObject("Narrative");
                    var intro = narrativeGo.AddComponent<NarrativeIntro>();
                    intro.Initialize(narrative);
                }
                else Debug.LogError($"[SpikeSceneBuilder] 找不到叙事资源：{narrativePath}");
            }

            if (checkpointPositions != null)
            {
                for (int i = 0; i < checkpointPositions.Length; i++)
                {
                    CreateCheckpoint(checkpointPositions[i], i, player, flow, sprite, material);
                }
            }

            if (trapPosition.HasValue)
            {
                CreateVoidTrap(trapPosition.Value, player, flow, ripple, sprite, material);
            }

            // D-3 接近脉冲：屏幕边缘提示，不给方向
            var pulse = rippleGo.AddComponent<ProximityPulse>();
            pulse.Initialize(enemies, player);

            var hud = rippleGo.AddComponent<SpikeHud>();
            hud.Initialize(ripple, player, stepper, enemies, pulse, flow);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                Debug.LogError($"[SpikeSceneBuilder] 保存场景失败：{scenePath}");
                return;
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[SpikeSceneBuilder] 已生成 {scenePath}：{walls.Length} 面墙，" +
                $"唱歌 {settings.Voice.Speed} m/s × {settings.Voice.Lifetime} s（半径≈{settings.Voice.VisualRadius} m，" +
                $"{settings.Voice.DirectionCount} 方向 × {settings.Voice.TrailDots} 尾迹，冷却 {settings.Voice.Cooldown} s）；" +
                $"落脚 {settings.Footstep.DirectionCount} 方向 × {settings.Footstep.TrailDots} 尾迹，上限 {settings.MaxDots}。" +
                $"墙体 sprite={sprite.name}，尾迹 sprite={circle.name}（ppu={circle.pixelsPerUnit}）。");
        }

        private static Camera CreateCamera(Vector2 position, float orthographicSize)
        {
            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(position.x, position.y, -10f);

            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;

            // URP 下相机需要额外的相机数据组件，缺失时由该扩展方法补上
            camera.GetUniversalAdditionalCameraData();
            return camera;
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

        /// <summary>v2（D-1 决定）：3 m/s × 1.2 s（≈3.6 m）。</summary>
        private static RippleSettings EnsureRippleSettingsV2()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RippleSettings>(SettingsV2Path);
            if (existing != null) return existing;

            return CreateRippleProfile(SettingsV2Path, 3f, 1.2f);
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

        /// <summary>章节世界文本（V3+V6 验证用：玩家经过 x=8 时浮现一句）.</summary>
        private static WorldTextSettings EnsureWorldTextsChapter1()
        {
            // 内容以 SO 为准：只在资源不存在时写入默认条目，之后由策划在 Inspector 里编辑
            var existing = AssetDatabase.LoadAssetAtPath<WorldTextSettings>(WorldTextsPath);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<WorldTextSettings>();
            settings.SetEntries(new[]
            {
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(8f, 1.6f),
                    TriggerCenter = new Vector2(8f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "声音是<wave>唯一</wave>能<color=#7fd4ff>照亮</color>这里的东西。",
                },
            });

            AssetDatabase.CreateAsset(settings, WorldTextsPath);
            return settings;
        }

        /// <summary>虚无陷阱：踩到脚下发满档声响。</summary>
        private static void CreateVoidTrap(Vector2 position, PlayerController player, SpikeFlow flow,
            RippleSystem ripple, Sprite sprite, Material material)
        {
            var go = new GameObject("VoidTrap_0");
            go.transform.position = position;

            var trigger = go.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.2f, 1.2f);

            var visualGo = new GameObject("TrapVisual");
            visualGo.transform.SetParent(go.transform, false);
            var visual = visualGo.AddComponent<SpriteRenderer>();
            visual.sprite = sprite;
            visual.sharedMaterial = material;
            visual.color = new Color(0.85f, 0.15f, 0.25f, 0f);
            visual.sortingOrder = 1;
            visualGo.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

            var trap = go.AddComponent<VoidTrap>();
            trap.Initialize(player, flow, ripple, visual);
        }

        /// <summary>第二章世界文本（5 条，约 104 字）。</summary>
        private static WorldTextSettings EnsureWorldTextsChapter2()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WorldTextSettings>(WorldTextsPath2);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<WorldTextSettings>();
            settings.SetEntries(new[]
            {
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(20f, 1.4f),
                    TriggerCenter = new Vector2(20f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "第二段记忆比第一段<wave>更窄</wave>，也更安静。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(55f, 1.4f),
                    TriggerCenter = new Vector2(55f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "你唱得越响，它来得越快；你屏住呼吸，它就找不到你。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(90f, 1.4f),
                    TriggerCenter = new Vector2(90f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "它挡在唯一的缺口里——这一次，<color=#7fd4ff>把声音扔到别处</color>。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(130f, 1.4f),
                    TriggerCenter = new Vector2(130f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "别回头。它记得你每一次<wave>发声的位置</wave>。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(175f, 1.4f),
                    TriggerCenter = new Vector2(175f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "前面那点亮光，是你自己的声音照出来的。",
                },
            });
            AssetDatabase.CreateAsset(settings, WorldTextsPath2);
            return settings;
        }

        /// <summary>第二章叙事（5 行，约 60 字）。</summary>
        private static NarrativeSettings EnsureNarrativeChapter2()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NarrativeSettings>(NarrativePath2);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<NarrativeSettings>();
            AssetDatabase.CreateAsset(settings, NarrativePath2);

            string[] lines =
            {
                "这一次，记忆更窄了。",
                "她听见自己的回声，被折叠成一条走廊。",
                "有什么东西，一直挡在缺口里。",
                "她把一滴眼泪，轻轻抛向远处。",
                "声音落下的地方，不是她站的地方。",
            };

            var serialized = new SerializedObject(settings);
            SerializedProperty linesProperty = serialized.FindProperty("_lines");
            linesProperty.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                linesProperty.GetArrayElementAtIndex(i).stringValue = lines[i];
            }

            serialized.FindProperty("_lineDuration").floatValue = 3f;
            serialized.FindProperty("_fadeTime").floatValue = 0.4f;
            serialized.FindProperty("_startDelay").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>第三章世界文本（6 条，约 102 字）。</summary>
        private static WorldTextSettings EnsureWorldTextsChapter3()
        {
            var existing = AssetDatabase.LoadAssetAtPath<WorldTextSettings>(WorldTextsPath3);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<WorldTextSettings>();
            settings.SetEntries(new[]
            {
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(25f, 1.4f),
                    TriggerCenter = new Vector2(25f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "第三段记忆里，连<wave>回声</wave>都被吞掉了。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(60f, 1.4f),
                    TriggerCenter = new Vector2(60f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "她开始怀疑，唱歌到底还有没有意义。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(95f, 1.4f),
                    TriggerCenter = new Vector2(95f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "恐惧不是声音，是声音落下之后的<color=#c792ea>沉默</color>。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(130f, 1.4f),
                    TriggerCenter = new Vector2(130f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "她想起来，小时候唱歌，从来不是为了谁。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(160f, 1.4f),
                    TriggerCenter = new Vector2(160f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "可她还是唱了，哪怕黑暗里没有人听。",
                },
                new WorldTextSettings.Entry
                {
                    TextPosition = new Vector2(185f, 1.4f),
                    TriggerCenter = new Vector2(185f, 0f),
                    TriggerSize = new Vector2(4f, 4f),
                    Width = 7f,
                    Text = "这一次，她只想<wave>听清</wave>自己的声音。",
                },
            });
            AssetDatabase.CreateAsset(settings, WorldTextsPath3);
            return settings;
        }

        /// <summary>第三章叙事（5 行，约 55 字）。</summary>
        private static NarrativeSettings EnsureNarrativeChapter3()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NarrativeSettings>(NarrativePath3);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<NarrativeSettings>();
            AssetDatabase.CreateAsset(settings, NarrativePath3);

            string[] lines =
            {
                "最后一层，安静得可怕。",
                "她怕的不是被听见，是没人再听。",
                "可她还是会唱。",
                "因为唱歌这件事本身，就是她的光。",
                "——她睁开眼睛。",
            };

            var serialized = new SerializedObject(settings);
            SerializedProperty linesProperty = serialized.FindProperty("_lines");
            linesProperty.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                linesProperty.GetArrayElementAtIndex(i).stringValue = lines[i];
            }

            serialized.FindProperty("_lineDuration").floatValue = 3f;
            serialized.FindProperty("_fadeTime").floatValue = 0.4f;
            serialized.FindProperty("_startDelay").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        /// <summary>把 SO 里的每条文本生成为世界空间 TMP + 触发区。</summary>
        private static void CreateWorldTexts(WorldTextSettings settings, PlayerController player, Camera camera)
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                Debug.LogError($"[SpikeSceneBuilder] 找不到 TMP 字体资产：{FontAssetPath}");
                return;
            }

            const float scale = 0.005f;

            for (int i = 0; i < settings.Entries.Length; i++)
            {
                WorldTextSettings.Entry entry = settings.Entries[i];

                var root = new GameObject($"WorldText_{i}");
                root.transform.position = new Vector3(entry.TriggerCenter.x, entry.TriggerCenter.y, 0f);

                var trigger = root.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = entry.TriggerSize;

                var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasGroup));
                canvasGo.transform.SetParent(root.transform, false);
                canvasGo.transform.position = new Vector3(entry.TextPosition.x, entry.TextPosition.y, -1f);
                canvasGo.transform.localScale = Vector3.one * scale;

                var canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;

                var rectTransform = canvasGo.GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(entry.Width / scale, 320f);

                var textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(canvasGo.transform, false);

                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.font = fontAsset;
                tmp.fontSize = 56f;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.text = entry.Text;

                var textRect = textGo.GetComponent<RectTransform>();
                textRect.sizeDelta = rectTransform.sizeDelta;
                textRect.anchoredPosition = Vector2.zero;

                var group = canvasGo.GetComponent<CanvasGroup>();
                var worldText = root.AddComponent<WorldText>();
                worldText.Initialize(player, tmp, group, entry.Text,
                    settings.CharsPerSecond, settings.PunctuationPause,
                    settings.WaveEnabled, settings.WaveAmplitudePercent,
                    settings.WaveFrequency, settings.WavePhaseStep);
            }
        }

        /// <summary>首段叙事文本资源（≤150 字）。</summary>
        private static NarrativeSettings EnsureNarrativeChapter1()
        {
            var existing = AssetDatabase.LoadAssetAtPath<NarrativeSettings>(NarrativePath);
            if (existing != null) return existing;

            var settings = ScriptableObject.CreateInstance<NarrativeSettings>();
            AssetDatabase.CreateAsset(settings, NarrativePath);

            var serialized = new SerializedObject(settings);
            SerializedProperty lines = serialized.FindProperty("_lines");
            lines.arraySize = Chapter1Lines.Length;
            for (int i = 0; i < Chapter1Lines.Length; i++)
            {
                lines.GetArrayElementAtIndex(i).stringValue = Chapter1Lines[i];
            }

            serialized.FindProperty("_lineDuration").floatValue = 3f;
            serialized.FindProperty("_fadeTime").floatValue = 0.4f;
            serialized.FindProperty("_startDelay").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            int total = 0;
            foreach (string line in Chapter1Lines) total += line.Length;
            Debug.Log($"[Narrative] 生成 {NarrativePath}：{Chapter1Lines.Length} 行，共 {total} 字（上限 150）");

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
