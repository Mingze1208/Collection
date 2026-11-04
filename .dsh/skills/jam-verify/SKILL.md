---
name: jam-verify
description: 在声称“完成/修好了”之前执行的四件套验证（编译→测试→截图→日志），并输出可核对的证据块与回滚建议。
whenToUse: 任何实现类任务结束前；用户说“验证一下”“能不能跑”“好了吗”时
---

# Jam 验证四件套

## 判定原则

没有证据 = 没完成。四项缺任何一项，必须明确写“缺失 + 原因”，不许用“应该没问题”替代。

## 1. 编译

- 触发一次编译（保存脚本 / 让编辑器 Refresh）。
- 判定：**0 error**。warning 要列出并分类（新引入 / 既有）。
- 证据：贴关键输出行，或指出日志文件路径（`Logs/`、Editor.log）。

## 2. 测试

- 优先 EditMode（秒级、无需进 Play）。
- 涉及运行时行为（输入、物理、协程、场景加载）才跑 PlayMode。
- 判定：全绿；或明确说明“本改动无对应测试，建议补 X”。

## 3. 视觉

- 截 Scene 或 Game 视图，说明看的是哪个。
- 判定：画面里能观察到本改动带来的差异。
- 截图不能证明“手感”，只能证明“存在且没崩”——手感必须由人试玩。

## 4. 日志

- 读 Console / 日志新增内容，逐条说明是否与本次改动相关。
- 出现 `NullReferenceException`、`MissingReferenceException`、`Missing (Mono Script)` → **直接判失败**，不允许“先记着”。

## 输出模板

```
## 验证结果
- 编译：PASS（0 error / 2 warning，均为既有）
- 测试：PASS（EditMode 3/3）｜或 N/A（本改动无测试，理由：…）
- 视觉：PASS（Game 视图，附截图）
- 日志：PASS（无新增异常）

## 改动文件
- Assets/_Project/Code/Runtime/Player/PlayerController.cs（新增）
- Assets/_Project/Scenes/SC_Main.unity（场景保存）

## 风险与未覆盖
- 未验证：手柄输入、低帧率表现
- 建议下一步：人工试玩 60 秒，重点看 X

## 回滚
git restore <文件>  /  git reset --hard <上一个绿提交>
```

## 红线

- 不看截图就说“视觉没问题”。
- 用“应该”“理论上”描述验证结果。
- 发现异常后继续往下做，而不是先报告。
