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

## 【FACT】第 1 阶段实时战斗切片

`CombatPrototypeMeleeAttack` 已落地最小实时动作链：输入后进入 Startup，计时结束执行一次 `Physics.OverlapSphereNonAlloc` 扇形过滤并调用 `CombatPrototypeHealth.ApplyDamage`，随后进入 Active/Recovery，完成后回到 Ready。`CombatPrototypeHealth` 负责当前生命、死亡判定和对象停用；该切片不修改 `AttackCommand` 的既有同步结算。

## 【CURRENT STRATEGY】第 1 阶段边界

先以独立 GameObject 组件验证单玩家、单敌人的近战手感；攻击数值由组件序列化字段提供，未接入 PlayerModel、奖励、掉落、ECS 或多人权威结算。

`UNKNOWN`：先手致死后的反击规则、同时死亡的正式胜负规则、战斗失败处置、数值平衡与奖励设计意图。体力成本不得超过当前体力已按用户规则接入，循环先后手与失败敌人状态语义保持现状。第 1 阶段切片已获用户人工 GamePlayer 验收确认，结论仅覆盖独立测试场景。第 2 阶段经主线程结合静态检查与用户反馈已通过；第 3 阶段敌人借出、重置、归还与场景清理已有用户正常反馈，主线程已判定该阶段通过，验收边界见[运行入口](Runtime.md)。AI 未运行逻辑单元测试或人工 PlayMode。

## 【FACT】第 3A 阶段服务端群体战斗链

`CombatPrototypePlayerNetCodeAuthoring` 烘焙近战参数与 `CombatPrototypeMeleeState`；`CombatPrototypeEnemyNetCodeAuthoring` 为每个敌人烘焙 `CombatPrototypeEnemyState`，并添加仅服务端保留的移动参数、目标状态与 `CombatPrototypeDamageEvent` 缓冲。输入事件通过 NetCode 发送后，服务端在预测模拟组中依次执行玩家移动、敌人目标选择与移动、空间索引重建、近战事件生成、统一伤害结算。

参数沿用第 1 阶段：伤害 `25`、距离 `2`、角度 `100°`、前摇 `0.18s`、命中阶段 `0.08s`、后摇 `0.3s`，敌人初始 HP `100`。命中将 HP 降至不小于 0、递增 `HitSequence`，HP 为 0 时设置 `IsDead=1`。死亡敌人保留同步状态，不再承受伤害；客户端实体显示系统据同步死亡标记禁用根实体的 MaterialMeshInfo。

攻击阶段与攻击序号、敌人 HP/受击序号/死亡标记使用 `GhostField`；客户端不自行扣血。群体复用原敌人 Interpolated Ghost，客户端实体表现的边界见第 3B-2 节，不调用正式 `AttackCommand`、PlayerModel、奖励、掉落或存档，不含敌人反击、寻路避障、伤害预测或复活。

## 【KNOWN ISSUES】第 2B 阶段战斗验收

Ghost 字段、Baker 和资源引用已形成实际烘焙数据；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。战斗验收仅覆盖当前独立网络原型的基础近战、单敌人生命/受击/死亡同步及 Mono 表现，不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收。AI 未执行逻辑单元测试、PlayMode 或构建。

## 【CURRENT STRATEGY】第 3A 阶段目标、空间查询与事件结算

- CombatPrototypeEnemyMovementSystem 每个服务端模拟 tick 从同时具备 NetworkStreamInGame、NetworkId、CommandTarget 且连接状态为 Connected 的连接读取有效玩家目标；在 X/Z 平面选择最近者，同距离时选择较小 NetworkId。以速度 2 直线追踪，到距离 1.5 停止并限制当次步长，保持敌人 Y 不变；无目标或敌人死亡时停止。
- CombatPrototypeEnemySpatialSystem 在敌人移动后重建格宽 2 的 X/Z 空间索引，只加入存活敌人，每个敌人恰好属于一个格子；索引使用可复用的 NativeParallelMultiHashMap，随 World 销毁释放。
- CombatPrototypeMeleeServerSystem 保留 Ready → Startup → Active → Recovery → Ready。Startup 结束只查询一次与攻击范围相交的格子，再执行距离 2 与扇形 100° 过滤，为每个命中目标添加一条包含攻击者 NetworkId、攻击序号及伤害 25 的事件。每个格子只遍历一次，配合每个敌人只入一格，保证一次攻击对同一敌人最多产生一次事件；Active 阶段不重复查询。
- CombatPrototypeDamageSystem 在近战系统之后消费各敌人的事件缓冲，统一写入 HP、HitSequence 和 IsDead；已死亡目标不再结算后续事件，缓冲在同 tick 清空，下一 tick 不重放。客户端从 Ghost 读取状态，不参与权威扣血。

## 【KNOWN ISSUES】第 3A 阶段群体验收

编译及 Editor 烘焙静态核对已完成；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。战斗验收仅覆盖当前独立网络原型的 32 敌人追踪、最近在线玩家切换、停止距离与无在线玩家时停止追踪、一次攻击多目标且每目标仅结算一次伤害 25、死亡后停止移动和受击，以及双端 HP、死亡状态与存活/死亡统计一致性。本阶段未进行性能测量，不扩展为正式 Map、平台构建、大规模性能或线上联调验收。直线追踪未接入导航、碰撞避让或敌人攻击；第 3A 人工通过结论不包含第 3B-2 Entities Graphics 群体表现。AI 未执行逻辑单元测试、PlayMode 或构建。

## 【FACT】第 3B-2 阶段死亡显示接入

服务端 `CombatPrototypeDamageSystem → CombatPrototypeEnemyState.IsDead → Ghost 同步` 的权威链保持不变；客户端由 `CombatPrototypeEnemyRenderSystem → MaterialMeshInfo 启用状态 → Entities Graphics` 控制敌人显示。IsDead 为 0 时启用渲染，否则禁用；系统仅在客户端 Presentation 阶段、EntitiesGraphicsSystem 之前执行，并持续查询已经禁用渲染的敌人。

## 【CURRENT STRATEGY】第 3B-2 阶段战斗边界

死亡隐藏只改变客户端根实体的渲染启用状态，保留死亡实体、HP/受击序号/死亡标记及原统计规则。群体生成配置、服务端追踪与停止规则、每次攻击每目标仅结算一次伤害 25 的规则未改变；本阶段没有新增复活、敌人攻击、奖励或存档链。

## 【KNOWN ISSUES】第 3B-2 阶段战斗验收

代码编译、敌人根实体渲染组件和资源引用已静态核对；用户已确认第 3B-2 人工 GamePlayer 验证通过，主线程结合既有静态检查与用户反馈判定第 3B-2 阶段通过。验收仅覆盖当前独立网络原型的双端敌人显示与移动、死亡隐藏无重复显示或残影、重新加入后的死亡状态，以及 HP/存活/死亡统计一致性。第 2B、第 3A 与第 3B-1 的既有通过范围保持原状，不扩展为规模性能、平台构建或线上联调验收。AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查，也未进行规模性能测量。

## 【FACT】第 4A 阶段服务端攻击体力门槛

`CombatPrototypeMeleeConfig.AttackPowerCost` 为整数，当前玩家 Prefab 配置为 `10`；玩家体力组件和初值由[玩家](Player.md)维护。`CombatPrototypeMeleeServerSystem` 仍仅在 ServerSimulation 的预测模拟组处理玩家攻击，新增同一玩家的体力读写，不改敌人伤害事件链。

## 【CURRENT STRATEGY】第 4A 阶段攻击接受与拒绝

- Ready 收到 Attack 且当前体力不少于成本时，服务端先扣一次成本，再进入 Startup、设置原前摇计时并递增 AttackSequence；日志记录 `accepted`、`ReadyAndPowerAvailable`、玩家 NetworkId、剩余体力/上限、成本及序号。扣费发生在命中查询前，因此空挥同样消耗 10。
- Ready 收到 Attack 但体力不足时，记录 `rejected` 与 `InsufficientPower`，不扣费、不改变攻击阶段或序号。体力恰好为 10 时可扣至 0；此后无法支付成本 10 的攻击。
- Startup、Active、Recovery 收到 Attack 时记录 `AttackInProgress`，不额外扣费或增加序号，并继续原 PhaseTimer 和阶段推进。没有新增攻击排队或取消逻辑。
- Ready → Startup → Active → Recovery → Ready 的时序、伤害 25、范围 2、角度 100°、前摇 0.18s/命中 0.08s/后摇 0.3s、32 敌人的空间筛选与每目标去重均保持原值。客户端不参与权威体力扣费或伤害结算；本阶段没有体力恢复、玩家受击、奖励或存档链。

## 【KNOWN ISSUES】第 4A 阶段战斗验收

服务端分支、编译和实际烘焙配置已静态核对；人工 GamePlayer 的成功攻击一次扣费、空挥扣费、忙碌拒绝且阶段持续推进、10 次成功启动后体力为 0 且后续输入不递增序号，以及原群体伤害和死亡表现回归仍为 `UNKNOWN`。既有第 2B、第 3A、第 3B 人工通过范围不扩展为第 4A。AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。
