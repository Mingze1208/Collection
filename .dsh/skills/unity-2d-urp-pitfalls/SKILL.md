---
name: unity-2d-urp-pitfalls
description: 写 Unity 2D + URP + 新 Input System 相关代码前查这份清单，避免踩渲染、输入、物理、域重载的经典坑。
whenToUse: 涉及 Sprite/Light2D/相机/输入/物理2D/UI/域重载时；写新玩法脚本前
---

# Unity 2D + URP 常见坑

本工程：Unity 6000.3.5f2 / URP 2D / Input System only / Linear 色彩空间 / .NET Standard 2.1。

## 1. 渲染

- **URP 2D Renderer 必须被赋值**，否则 `Light2D` 完全不生效。检查 Quality 设置里的 Render Pipeline Asset（本工程已指向 `UniversalRP.asset` → `Renderer2D.asset`）。
- Sprite 默认材质有 **Lit 与 Unlit** 两种：用 `Sprite-Lit-Default` 才受 2D 光照影响；想要纯色不受光就用 Unlit。
- 排序用 **Sorting Layer + Order in Layer**，不要靠 Z 轴“碰运气”。新 Layer 要先在 Tag & Layers 里定义。
- 相机是 Orthographic：`orthographicSize` = **屏幕高度的一半（世界单位）**。像素风要和 PPU、分辨率一起算，别单独调。
- 色彩空间是 **Linear**：贴图的 sRGB 勾选会影响亮度，导入设置不要乱改。
- UI 走 Canvas（Screen Space - Overlay 最省事），**不要**把 UI 塞进 2D 光照排序里。

## 2. 输入（最容易写错）

- 本工程 `activeInputHandler: 1` → **只有新 Input System**。`Input.GetKey` 会直接抛异常。
- 用 `PlayerInput` 组件 + `InputAction`，或直接用 `InputAction` 代码绑定；回调参数是 `InputAction.CallbackContext`。
- 已有 `Assets/InputSystem_Actions.inputactions`：先读它，复用已有 Action，不要重新发明一套。
- 输入读取放 `Update`；物理相关放 `FixedUpdate`。

## 3. 物理 2D

- 俯视角通常 `Rigidbody2D.gravityScale = 0`；平台跳跃才用重力。
- 移动用 `Rigidbody2D.linearVelocity`（Unity 6 已把 `velocity` 改名），不要直接改 `transform.position`。
- 碰撞层先在 `Physics2DSettings` 的 Layer Collision Matrix 里配好，代码里用 LayerMask 而不是硬编码数字。
- 连续碰撞检测只给快速移动对象开，全开有性能代价。

## 4. 生命周期与域重载

- 进入 Play Mode 会触发域重载：**静态字段会被重置**。跨场景状态用 ScriptableObject 或显式 Service。
- `[RuntimeInitializeOnLoadMethod]` 适合做启动引导，不要在里面做重资源加载。
- 协程在对象销毁后不会自动停；用 `CancellationToken` 或显式停止。

## 5. 本工程特有的约束

- 无 asmdef 时，任何脚本改动都会触发全量编译；等 asmdef 建好后按程序集局部编译。
- 不要把运行时逻辑写进 `Code/Editor/`（不会进构建）。
- TextMeshPro 资源若未导入，UI 文本会显示为方块；先确认 TMP Essentials 是否已导入【待确认】。
