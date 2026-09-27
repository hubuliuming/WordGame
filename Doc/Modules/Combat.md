# 敌人、战斗与奖励

[返回总导航](../AI_Understanding.md)。本页记录当前命令式战斗链；玩家属性写入归[玩家](Player.md)，敌人配置数值和 Prefab 路径归[资源与数据](DataResources.md)。

## 入口文件

- [EnemyBase.cs](../../Assets/Scripts/Features/Enemy/Control/EnemyBase.cs)：加载敌人数据、注册 BtnAttack、重置 data。
- [EnemyData.cs](../../Assets/Scripts/Features/Enemy/EnemyData.cs)：独立 Serializable struct；Award 仍为嵌套 struct，字段形状保持不变。
- [AttackCommand.cs](../../Assets/Scripts/Command/AttackCommand.cs)：含成本的资格检查、同步循环、一次性提交玩家结算与成功后回收。
- [AttackMath.cs](../../Assets/Scripts/Utility/AttackMath.cs)：伤害计算。
- [DropSystem.cs](../../Assets/Scripts/System/DropSystem.cs)：创建物品掉落字典。
- [FactoryUISystem.cs](../../Assets/Scripts/Factory/FactoryUISystem.cs)：敌人池创建和取出时重置。

## 【FACT】对象与调用链

1. [运行入口](Runtime.md)的按键或按钮从 Game 已注册的 FactoryUISystem 实例借出野猪；该调用要求 MapCanvasControl 已绑定当前场景父节点，当前 Scene 的序列化绑定来源见运行入口。
2. 对象池首次创建实例时调用 `IBaseLife.Init(enemyName)`。EnemyBase 从敌人 JSON 读取同名条目，赋给 data 和 initData，并在直接子节点 BtnAttack 上注册点击。
3. 点击发送 `new AttackCommand(gameObject)`。构造函数从 EnemyBase.data 复制 EnemyData；EnemyData 是 struct。
4. QFramework 执行命令，命令获取 PlayerModel 与 PlayerEventSystem；EnableAttack(CostPower) 检查死亡状态、成本非负及当前体力是否足够。失败时结束，不扣除体力。
5. 通过检查后先用 AttackPlayer 计算局部结果，按 AttackResult 决定奖励，再将体力、Hp、经验、金币和掉落库存提交同一次 ChangeAll；提交成功后才记录结算结果与回收胜利敌人。

EnemyBase.InitData 将 initData 赋回 data，敌人池每次取出都调用它。AttackCommand 结算修改的是命令内部的结构体副本，没有将剩余 HP 写回 EnemyBase.data。

## 【CURRENT STRATEGY】结算顺序

`AttackPlayer()` 使用局部 playerHp，并在 `敌方 HP > 0 && playerHp > 0` 时循环：

- 玩家 Speed 大于等于敌人 Speed：先扣敌方 HP，再扣玩家 HP。
- 玩家 Speed 小于敌人 Speed：先扣玩家 HP，再扣敌方 HP。
- 两次扣血之间没有再次检查死亡；每个进入的循环体都执行双方扣血。
- `AttackMath.AttackValue(attack, defence)` 返回 attack-defence，结果不大于 0 时改为 1。
- 循环退出后，将 `Max(0, playerHp) - PlayerModel.Hp` 作为 Hp 增量，和体力成本、胜利奖励一起交给 ChangeAll；循环内不写玩家状态。
- `AttackResult()` 只判断命令中的敌方 HP 是否不大于 0，没有同时要求玩家存活。

这是单次命令内同步循环的当前实现；未接入逐帧攻击动画、攻击冷却或动画事件。

## 【FACT】奖励与回收

结果为胜利时，把 award.Exp、award.Coin 放入 ItemData 增量，并通过：

`DropSystem.GetRangeGoods(award.GoodsName, 1, 3) → PlayerEventSystem.ChangeAll(属性增量, 掉落字典)`。

GetRangeGoods 仍使用整数版 UnityEngine.Random.Range，实际掉落数量为 1 或 2，上界 3 不包含。奖励数量参数不变；掉落物品名为空时报错并拒绝本次结算。

ChangeAll 对完整操作先校验再应用，保存一次；失败不留下局部体力扣费或奖励，也不回收敌人。成功时更新玩家属性/金币事件，并在有普通库存变化时发送库存事件。金币当前通过专用入口的数值规则写入同一个 goodsDict。

胜利且提交成功后对当前敌人对象调用 Release；现有扩展转到 Game 已注册的 FactoryUISystem 实例归还。失败战斗仍提交本次体力与 Hp，但不回收敌人、不把命令副本中的敌人 HP 写回 EnemyBase.data。

## 【KNOWN ISSUES】静态边界

- 双方可能在同一个循环体中都扣到非正值，胜利判断仍只看敌方 HP。是否应允许同时死亡获胜为 `UNKNOWN`。
- 失败后再次点击会从 EnemyBase.data 重新复制敌人状态；当前命令内受伤不会成为组件的持久战斗状态。
- 第 2 阶段统一玩家结算改动已有用户实际行为和日志核对正确的反馈；该反馈不扩展为全部敌人 HP、伤害组合和后续战斗规则的业务验收。

## 未知项与验收状态

`UNKNOWN`：先手致死后的反击规则、同时死亡的正式胜负规则、战斗失败处置、数值平衡与奖励设计意图。体力成本不得超过当前体力已按用户规则接入，循环先后手与失败敌人状态语义保持现状。第 2 阶段经主线程结合静态检查与用户反馈已通过；第 3 阶段敌人借出、重置、归还与场景清理已有用户正常反馈，主线程已判定该阶段通过，验收边界见[运行入口](Runtime.md)。AI 未运行逻辑单元测试或人工 PlayMode。
