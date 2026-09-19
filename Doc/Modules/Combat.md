# 敌人、战斗与奖励

[返回总导航](../AI_Understanding.md)。本页记录当前命令式战斗链；玩家属性写入归[玩家](Player.md)，敌人配置数值和 Prefab 路径归[资源与数据](DataResources.md)。

## 入口文件

- [EnemyBase.cs](../../Assets/Scripts/Features/Enemy/Control/EnemyBase.cs)：加载敌人数据、注册 BtnAttack、重置 data。
- [AttackCommand.cs](../../Assets/Scripts/Command/AttackCommand.cs)：攻击资格检查、体力变更、循环结算与奖励。
- [AttackMath.cs](../../Assets/Scripts/Utility/AttackMath.cs)：伤害计算。
- [DropSystem.cs](../../Assets/Scripts/System/DropSystem.cs)：创建物品掉落字典。
- [FactoryUISystem.cs](../../Assets/Scripts/Factory/FactoryUISystem.cs)：敌人池创建和取出时重置。

## 【FACT】对象与调用链

1. [运行入口](Runtime.md)的按键或按钮通过 FactoryUISystem.Get 获取野猪。
2. 对象池首次创建实例时调用 `IBaseLife.Init(enemyName)`。EnemyBase 从敌人 JSON 读取同名条目，赋给 data 和 initData，并在直接子节点 BtnAttack 上注册点击。
3. 点击发送 `new AttackCommand(gameObject)`。构造函数从 EnemyBase.data 复制 EnemyData；EnemyData 是 struct。
4. QFramework 执行命令，命令获取 PlayerModel 与 PlayerEventSystem；资格检查失败时打印“玩家已经死亡或者体力不足”并结束。
5. 通过检查后调用 ChangePower(-CostPower)，执行 AttackPlayer，再按 AttackResult 判断奖励与对象回收。

EnemyBase.InitData 将 initData 赋回 data，敌人池每次取出都调用它。AttackCommand 结算修改的是命令内部的结构体副本，没有将剩余 HP 写回 EnemyBase.data。

## 【CURRENT STRATEGY】结算顺序

`AttackPlayer()` 使用局部 playerHp，并在 `敌方 HP > 0 && playerHp > 0` 时循环：

- 玩家 Speed 大于等于敌人 Speed：先扣敌方 HP，再扣玩家 HP。
- 玩家 Speed 小于敌人 Speed：先扣玩家 HP，再扣敌方 HP。
- 两次扣血之间没有再次检查死亡；每个进入的循环体都执行双方扣血。
- `AttackMath.AttackValue(attack, defence)` 返回 attack-defence，结果不大于 0 时改为 1。
- 循环退出后，通过 `ChangeHp(-(PlayerModel.Hp - playerHp))` 回写玩家生命；其最终行为受 PlayerModel 的 Hp setter 影响。
- `AttackResult()` 只判断命令中的敌方 HP 是否不大于 0，没有同时要求玩家存活。

这是单次命令内同步循环的当前实现；未接入逐帧攻击动画、攻击冷却或动画事件。

## 【FACT】奖励与回收

结果为胜利时，WinAward 依次调用 ChangeExp、ChangeCoin，然后执行：

`DropSystem.GetRangeGoods(award.GoodsName, 1, 3) → ChangeGoodsDic(物品名, 数量)`。

GetRangeGoods 将整数版 `UnityEngine.Random.Range(min, max)` 的结果放入只有一个物品键的字典；此处实际数量范围为 1 或 2，上界 3 不包含。奖励数量的 1、3 是调用点硬编码参数。

命令最后对当前敌人对象调用 Release，回收到对象池。失败分支没有回收敌人，也没有将命令副本中的敌人 HP 保存回组件。金币 setter 的限制见[玩家的已知问题](Player.md)，不能仅因存在 ChangeCoin 调用就写成金币奖励已正确到账。

## 【KNOWN ISSUES】静态边界

- EnableAttack 只检查状态标记；扣除 CostPower 后，命令没有再次检查体力变更是否实际成功。
- 双方可能在同一个循环体中都扣到非正值，胜利判断仍只看敌方 HP。是否应允许同时死亡获胜为 `UNKNOWN`。
- 失败后再次点击会从 EnemyBase.data 重新复制敌人状态；当前命令内受伤不会成为组件的持久战斗状态。
- 敌人 HP、伤害与玩家 Hp setter 的组合尚未经过 GamePlayer 验证；不能将同步循环完成等同于战斗业务验收。

## 未知项与验收状态

`UNKNOWN`：先手致死后的反击规则、战斗失败处置、体力成本规则、数值平衡与奖励设计意图。未运行逻辑单元测试或人工 PlayMode；本页不记录为已通过。
