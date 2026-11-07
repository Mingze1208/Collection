---
name: unity-scene-ops
description: 创建或修改 Unity 场景、GameObject、组件、预制体时使用；通过 unity CLI 驱动编辑器，强制“先探状态、再改、再保存、再验证”的顺序，禁止直接编辑场景 YAML。
whenToUse: 用户说“搭个场景”“加个对象”“挂个脚本”“做个预制体”“看看场景结构”时
---

# Unity 场景操作流程（CLI 驱动）

本工程用 **Unity CLI** 驱动编辑器，不依赖 MCP 工具。固定变量：

```bash
UNITY="/mnt/c/Users/Chang/AppData/Local/Unity/bin/unity.exe"
PROJ='D:\Unity_Project\Collection'      # 必须 Windows 路径；从 WSL 用 wslpath -w 转换
```

## 0. 前置：确认链路

```bash
"$UNITY" pipeline list                        # 期望：运行中 true / 服务器可达 true / 端口 7800
"$UNITY" command list_open --project-path "$PROJ" --result-only
```

- 编辑器离线 → **停止**，报告“Unity 编辑器未运行”，不要用文件操作“模拟”场景修改。
- 记录当前场景路径与 `isDirty`；有未保存改动时先问。

## 1. 侦察（只读）

```bash
"$UNITY" command scene get_hierarchy --project-path "$PROJ" --result-only
"$UNITY" command --query <关键字> --detail full        # 不确定命令名时先查，禁止猜
```

确认：目标对象是否存在、父节点、组件列表；要挂的脚本是否已编译（`... console get`）。

## 2. 变更（一次一个原子操作）

命令形如 `unity command <分组> <命令名> [参数]`（例：`scene get_hierarchy`、`gameobject create`）。
参数结构先用 `unity command --query <名字>` 查描述，再执行。

规则：
- 一次最多 3 个原子操作，然后停下报告。
- **绝不**直接编辑 `.unity` / `.prefab` 的 YAML。
- **绝不**创建或删除 `.meta`。
- 修改预制体实例前，先说明“实例覆盖还是改源预制体”。

## 3. 保存与验证

```bash
"$UNITY" command scene save --project-path "$PROJ"
"$UNITY" command capture game_view --project-path "$PROJ"     # 截图（参数待确认）
"$UNITY" command console get --project-path "$PROJ"           # 新增日志
```

输出格式：

```
改动：
- 新增 Player（SpriteRenderer + Rigidbody2D + PlayerController）
- 挂载 Assets/_Project/Code/Runtime/Player/PlayerController.cs
证据：
- 场景已保存 ✓（isDirty=false）
- 截图 ✓（路径）
- Console：0 error / 1 warning（内容）
风险：
- Rigidbody2D.gravityScale 仍为 1，俯视角可能需 0 —— 待确认
```

## 4. 失败处理

- 命令报 `Command Not Found` → 用 `unity command --query` 找正确名称，不要重试同一个错名。
- 命令报 `names several commands` → 按提示补分组名（如 `gameobject find`）。
- 任一命令失败 → 停下报告原文；半成品状态建议 `git restore`，不要“再改回去”。
