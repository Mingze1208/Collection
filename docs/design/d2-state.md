# 工程状态快照（D2 进行中）

> 用途：新会话接手时先读本文件 + `d2-plan.md` + `spike-d1-conclusion.md`，即可恢复全部上下文。
> 最后更新：2026-11-09

## 1. 环境

- Unity `6000.3.5f2`（URP 2D / 新 Input System / Linear）
- 编辑器 Windows `D:\Unity_Project\Collection`；AI 在 WSL `/mnt/d/Unity_Project/Collection`
- CLI：`/mnt/c/Users/Chang/AppData/Local/Unity/bin/unity.exe`，项目路径必须用 Windows 形式
- **跑构建菜单前必须确认 `playMode: stopped`**（否则 `EditorSceneManager.NewScene` 抛异常）
- 编辑器失焦时播放循环会停：`unity command set_run_in_background --enabled true`（用完还原 false）
- 键盘注入 `runtime simulate_key` 在本工程**无效**；自动化靠脚本调用组件上的调试入口

## 2. 场景（全部由编辑器脚本生成，不手写 YAML）

| 场景 | 菜单 | 用途 |
|---|---|---|
| `Assets/_Project/Scenes/SC_Spike.unity` | Collection/Spike/Rebuild SC_Spike Scene | D1 用房（10×10、L 形内墙、门洞） |
| `Assets/_Project/Scenes/SC_Corridor.unity` | Collection/Spike/Rebuild SC_Corridor Scene | 3 m 宽 21 m 长直廊，测屏息/驻留 |
| `Assets/_Project/Scenes/SC_Level01.unity` | Collection/Spike/Rebuild SC_Level01 Scene | 第一章 124 m 连续通路 + 终点门 + 3 检查点 + 叙事 |

场景内固定结构：`Main Camera`(CameraFollow) / 墙体根 / `RippleSystem`(RippleSystem+ProximityPulse+SpikeHud) / `Player`(Rigidbody2D 冻结+CircleCollider2D+PlayerController+FootStepper) / `Feet`(Foot_Left/Right) / `Enemy` / `GameFlow`(SpikeFlow) / `FinishDoor` / `Checkpoint_n` / `Narrative`。

## 3. 代码地图

```
Assets/_Project/Code/
  Core/Ripple/            Vec2, Aabb, RipplePath, RippleBurstBuilder   （纯 C#，可单测）
  Runtime/Ripple/         RippleSettings(SO), RippleSystem
  Runtime/Player/         PlayerController, FootStepper（一步一印 + 屏息静步）
  Runtime/Enemy/          EnemySettings(SO), EnemyState, EnemyController
  Runtime/Flow/           SpikeFlow, ChapterGoal, Checkpoint, CameraFollow, NarrativeSettings(SO), NarrativeIntro
  Runtime/Feedback/       ProximityPulse（接近脉冲 + 抓前闪红）
  Runtime/Diagnostics/    SpikeHud（IMGUI 调试面板）
  Editor/                 SpikeSceneBuilder（生成场景/资源）
  Tests/EditMode/         RippleBounceTests（8 条）
```

## 4. 关键参数（都在 SO，改数值不用改代码）

`Data/SO_Ripple_V2.asset`（场景实际使用）—— 两档独立参数（Inspector 分「唱歌」「落脚」两节）
- **唱歌**：3 m/s × 1.2 s（半径 3.6 m），24 方向 × 8 尾迹 = 192 点/次，**冷却 0.5 s**
- **落脚**：3 m/s × 1.2 s，12 方向 × 4 尾迹 = 48 点/次，冷却 0
- 两组共用：尾迹间距 0.12、同屏上限 512、点半径 0.05、最大反射 8
- 步态：步长 0.8 m、步间隔 0.35 s（≈2.29 m/s）、残影灰化 1.2 s、脚方块 0.3 m
- 屏息：3.0 s 时长、1.5× 恢复；耗尽在脚下发满档声

`Data/SO_Enemy.asset`
- 速度 2.5、到达 0.15、感知 0.45、改向阈值 0.3、击杀 0.55（实际取 max(0.55, 双方半径和+0.02)）
- 接近脉冲：6 m 内、0.8→3.0 Hz、抓前 0.3 s 闪红

`Data/SO_Narrative_Ch1.asset`：6 行 / 84 字，1 s 延迟 + 3 s/行，Enter/Esc 跳过

`Data/SO_Ripple_A.asset`、`SO_Ripple_B.asset`：D1 A/B 对照用，保留

## 5. 已验证的事实（含证据目录）

- `docs/evidence/d1/`：D1 全部（涟漪、A/B、敌人、失败循环、性能）
- `docs/evidence/d2/`
  - `A1–A4`：124 m 通路从头走到门，用时 54.1 s
  - `B1–B4`：检查点激活/复活、终点门 Trigger
  - `C1–C2`：多检查点顺序覆盖、死亡后保持激活
  - `D1–D4`：叙事字幕时机与跳过
  - `U/V/W/X/Y/Z`：驻留、屏息静步、接近脉冲

## 6. 已知缺口 / 待办

- 任务 5：**真实键鼠试玩**（需要人做）
- 任务 6 已完成（首段叙事）
- 新需求：世界空间富文本（逐字 + 粗体/颜色/波浪），见 `d2-text-effect-report.md`，**待拍板**
- 设计遗留：
  - 3.5 m 宽 + ±1.5 m 折线 ⇒ 中线全程直通，没有真正的\"看不到前面\"
  - 124 m 与计划\"3–4 分钟\"有差距（连续走仅 54 s）
  - 检查点/门的判定都用 Box Trigger + 冻结刚体，实测正常
  - 玩家调试入口 `SetDebugDrive / SetDebugBreath / StepOnce / EmitOnce` 仍在，正式版应删除或加编译开关

## 7. 常用命令

```bash
UNITY="/mnt/c/Users/Chang/AppData/Local/Unity/bin/unity.exe"; PROJ='D:\Unity_Project\Collection'
"$UNITY" command editor status --project-path "$PROJ" --result-only
"$UNITY" command script recompile --project-path "$PROJ" --result-only
"$UNITY" command editor invoke_menu --path "Collection/Spike/Rebuild SC_Level01 Scene" --project-path "$PROJ"
"$UNITY" command editor.playmode start --project-path "$PROJ" --result-only
"$UNITY" command capture game_view --output-path 'D:\Unity_Project\Collection\docs\evidence\d2\x.png' --project-path "$PROJ"
"$UNITY" command test run --mode EditMode --project-path "$PROJ" --result-only
```
