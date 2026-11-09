# 世界空间文本 · 使用指南

> 适用：`SC_Level01`（以及任何通过 `SpikeSceneBuilder` 生成的场景）。
> 内容以 **ScriptableObject 为准**，场景由菜单重建。

## 1. 数据在哪

`Assets/_Project/Data/SO_WorldTexts_Ch1.asset`

一个 SO = 一张场景的全部文本；每条 Entry = 一处文本 + 一个触发区。

| 字段 | 含义 |
|---|---|
| `Text Position` | 文本在**世界坐标**里的锚点（文本中心） |
| `Trigger Center` | 触发区中心（玩家走到这里才算数） |
| `Trigger Size` | 触发区宽 × 高（世界单位）。走廊净宽 3.5 m，建议 4 × 4 |
| `Width` | 排版宽度（世界单位），决定折几行 |
| `Text` | 文本内容，支持标签（见 §3） |

全局参数（同一个 SO，作用于所有条目）：

| 参数 | 现值 | 说明 |
|---|---|---|
| `Chars Per Second` | 12 | 逐字速度 |
| `Punctuation Pause` | 0.15 s | 标点后额外停顿 |
| `Wave Enabled` | 开 | 波浪总开关 |
| `Wave Amplitude Percent` | 0.14 | **振幅 = 字高 × 该比例** |
| `Wave Frequency` | 4 | 波动速度 |
| `Wave Phase Step` | 0.6 | 相邻字相位差（决定波浪推进感） |

> 触发方式：**玩家本体**进入 Trigger 才触发（涟漪不算）。每条**只触发一次**。

## 2. 加一处新文本（3 步）

1. 选中 `SO_WorldTexts_Ch1`，在 Inspector 的 `Entries` 点 `+`，填 5 个字段；
2. `Ctrl+S` 保存资源；
3. 菜单 **`Collection / Spike / Rebuild SC_Level01 Scene`** 重建场景。

重建后新文本会出现在场景里，名为 `WorldText_N`。

⚠️ **重建场景会按代码重新生成整个场景**：手动拖动的物体、临时改的参数都会丢。所有内容请改 SO（或在 `SpikeSceneBuilder` 里改布局），不要在场景里手改后指望保留。

## 3. 支持的标签

| 标签 | 效果 | 说明 |
|---|---|---|
| `<color=#7fd4ff>…</color>` | 颜色 | TMP 内置，十六进制 |
| `<size=120%>…</size>` | 字号 | TMP 内置 |
| `<wave>…</wave>` | 逐字波浪 | **自定义**，由 `WorldText` 解析后剥离 |
| `<b>…</b>` | 粗体 | **不支持**（字体只有单一字重，已放弃） |

- 标签**不占逐字计数**：`<color=…>` 里的字照常一个个浮现。
- `<wave>` **不要嵌套** `<wave>`；与其他标签可以混用：`<wave><color=#fff>字</color></wave>` 是可用的（颜色交给 TMP，波浪按可见字索引）。

## 4. 尺寸与可读性

- 文本挂在一个 World Space Canvas 上：缩放 **0.005**，字号 **56** ⇒ 字高约 **0.28 世界单位**。
- 相机 `orthographicSize = 3` 时约 **34 px** 高，走廊宽度内一行约 7–8 字。
- 想整体放大/缩小：改 `SpikeSceneBuilder.CreateWorldTexts()` 里的 `scale` 与 `tmp.fontSize`（两者一起改，保持比例）。

## 5. 常见问题

| 现象 | 排查 |
|---|---|
| 文本不出现 | ① `Trigger Size` 是否盖住玩家走的路线；② 是否已经触发过（一次性）；③ 改完 SO 是否重建了场景 |
| 波浪不动 | ① `<wave>` 是否成对闭合；② `Wave Enabled` 是否打开；③ `Wave Amplitude Percent` 是否为 0 |
| 字显示成方块/空白 | 字体资产缺该字的字形；Console 会有缺字告警，换字或换字体 |
| 文本位置不对 | `Text Position`（文本在哪）和 `Trigger Center`（在哪触发）是两个字段，可以分开 |
| 触发太早/太晚 | 调 `Trigger Center` 的 X 或 `Trigger Size` 的宽 |
| 逐字太快/太慢 | 改 SO 的 `Chars Per Second` |

## 6. 相关代码

- [WorldText.cs](../Assets/_Project/Code/Runtime/Flow/WorldText.cs)：标签解析、逐字、波浪
- [WorldTextSettings.cs](../Assets/_Project/Code/Runtime/Flow/WorldTextSettings.cs)：数据结构与参数
- [SpikeSceneBuilder.cs](../Assets/_Project/Code/Editor/SpikeSceneBuilder.cs)：`CreateWorldTexts()` 把 SO 条目实例化进场景
