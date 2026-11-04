# Collection — Unity 2D Game Jam 工程约定

> 本文件由人维护，AI 只读。需要变更时由人修改并提交。

## 1. 环境事实（不要凭记忆推断）

- Unity `6000.3.5f2`（Unity 6.3），URP 2D，Input System 1.17
- `activeInputHandler: 1` → 只有新 Input System，**没有 `UnityEngine.Input` 的旧 API**
- 色彩空间 Linear；API 兼容级别 .NET Standard 2.1
- 编辑器在 Windows：`C:\Program Files\Unity\Hub\Editor\6000.3.5f2\Editor\Unity.exe`
- 本机还装了 `6000.5.4f1`：**不要用它打开本工程**，会触发全量重导入
- 工程双路径：WSL `/mnt/d/Unity_Project/Collection` ↔ Windows `D:\Unity_Project\Collection`
- 编辑器在 Windows 侧运行，AI 在 WSL 侧运行；跨边界一律用 Windows 路径传给 Unity
- 任何传给 Unity 的路径必须是 Windows 形式；转换用 `wslpath -w <path>`

## 2. 目录契约

```
Assets/_Project/Art|Audio|Data|Prefabs|Scenes|UI     # 所有自有资产
Assets/_Project/Code/{Core,Runtime,Editor}           # Core 不引用 UnityEngine
Assets/_Project/Tests/{EditMode,PlayMode}
Assets/Scenes/                                       # 模板 + 当前入口场景（过渡期）
仓库根：只允许 工程文件 / .git* / .vscode / docs / .dsh
```

## 3. 铁律（违反即回滚，不接受“下次注意”）

1. **不创建、不修改、不删除任何 `*.meta`**。
2. **不手写 `*.unity` / `*.prefab` / `*.asset` 的 YAML**。场景与预制体只通过 Unity 工具或 Editor 脚本修改。
3. **不在 `Assets/` 之外创建 Unity 资产**。历史上已发生过一次 `Scenes/` 被移到仓库根、导致构建丢场景的事故。
4. 移动/重命名资产必须先说明“源路径 → 目标路径 + 是否保留 `.meta`”，并等我确认。
5. **一次只改一个可编译、可回滚的单元**；改完列出改动文件清单。
6. 不碰 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、`ProjectSettings/`（除非我明确要求）。
7. 不确定 Unity 6.3 API 签名时先查文档或问我，**禁止猜一个“能编译但行为不对”的写法**。
8. 声称“完成”必须附证据：编译输出 / 测试结果 / 截图 / 日志片段。缺哪项就明说缺哪项。

## 4. 验证顺序（缺一不可）

1. **编译**：Console 无 error（看 `Logs/` 或编辑器，不看 AI 自述）
2. **测试**：EditMode 优先（秒级）；只有改动涉及运行时行为才跑 PlayMode
3. **视觉**：Scene 或 Game 视图截图
4. **日志**：新增的 warning/error 逐条解释

## 5. 代码约定

- 命名空间 `Collection.<Domain>`；脚本放 `Code/Runtime/<Domain>/`
- `Game.Core` 不得引用 `UnityEngine`（保证可单测）
- 输入只用 `UnityEngine.InputSystem`；禁止 `Input.GetKey` / `Input.GetAxis`
- 玩法数值进 ScriptableObject（`Data/`），不硬编码
- 命名：场景 `SC_`、预制体 `P_`、SO `SO_`、脚本 PascalCase、私有字段 `_camelCase`
- 跨场景常驻逻辑走 `RuntimeInitializeOnLoadMethod` 或显式 Service，不靠静态字段（域重载会清空）

## 6. 资产约定

- 2D 精灵统一 PPU；先定义 Sorting Layer 再使用，禁止临时 `Order in Layer` 排序
- 2D 光照用 `Light2D`，依赖 URP 2D Renderer；Sprite 默认材质注意 Lit / Unlit 区别
- 美术进 `Art/`、音频进 `Audio/`、可配置数据进 `Data/`，不散落在 `Assets/` 根