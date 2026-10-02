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

攻击阶段与攻击序号、敌人 HP/受击序号/死亡标记使用 `GhostField`；客户端不自行扣血。群体复用原敌人 Interpolated Ghost，客户端实体表现的边界见第 3B-2 节，不调用正式 `AttackCommand`、PlayerModel、掉落或存档，不含寻路避障、伤害预测或敌人复活；独立奖励接在首次死亡之后，见第 4B 节，敌人反击与玩家受伤见第 6A 节。

## 【KNOWN ISSUES】第 2B 阶段战斗验收

Ghost 字段、Baker 和资源引用已形成实际烘焙数据；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。战斗验收仅覆盖当前独立网络原型的基础近战、单敌人生命/受击/死亡同步及 Mono 表现，不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收。AI 未执行逻辑单元测试、PlayMode 或构建。

## 【CURRENT STRATEGY】第 3A 阶段目标、空间查询与事件结算

- CombatPrototypeEnemyMovementSystem 每个服务端模拟 tick 从同时具备 NetworkStreamInGame、NetworkId、CommandTarget 且连接状态为 Connected 的连接读取有效存活玩家目标；在 X/Z 平面选择最近者，同距离时选择较小 NetworkId。以速度 2 直线追踪，到距离 1.5 停止并限制当次步长，保持敌人 Y 不变；无目标、敌人死亡或处于第 6A 前摇/后摇时停止移动和旋转。
- CombatPrototypeEnemySpatialSystem 在敌人移动后重建格宽 2 的 X/Z 空间索引，只加入存活敌人，每个敌人恰好属于一个格子；索引使用可复用的 NativeParallelMultiHashMap，随 World 销毁释放。
- CombatPrototypeMeleeServerSystem 保留 Ready → Startup → Active → Recovery → Ready。Startup 结束只查询一次与攻击范围相交的格子，再执行距离 2 与扇形 100° 过滤，为每个命中目标添加一条包含攻击者 NetworkId、攻击序号及伤害 25 的事件。每个格子只遍历一次，配合每个敌人只入一格，保证一次攻击对同一敌人最多产生一次事件；Active 阶段不重复查询。
- CombatPrototypeDamageSystem 在近战系统之后消费各敌人的事件缓冲，统一写入 HP、HitSequence 和 IsDead；已死亡目标不再结算后续事件，缓冲在同 tick 清空，下一 tick 不重放。客户端从 Ghost 读取状态，不参与权威扣血。

## 【KNOWN ISSUES】第 3A 阶段群体验收

编译及 Editor 烘焙静态核对已完成；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。战斗验收仅覆盖当前独立网络原型的 32 敌人追踪、最近在线玩家切换、停止距离与无在线玩家时停止追踪、一次攻击多目标且每目标仅结算一次伤害 25、死亡后停止移动和受击，以及双端 HP、死亡状态与存活/死亡统计一致性。本阶段未进行性能测量，不扩展为正式 Map、平台构建、大规模性能或线上联调验收。直线追踪仍未接入导航或碰撞避让；第 3A 人工通过结论不包含第 3B-2 Entities Graphics 群体表现及第 6A 敌人反击/玩家受伤。AI 未执行逻辑单元测试、PlayMode 或构建。

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
- 存活玩家在 Startup、Active、Recovery 收到 Attack 时记录 `AttackInProgress`，不额外扣费或增加序号，并继续原 PhaseTimer 和阶段推进。没有攻击排队；死亡时的阶段取消由第 6A 死亡门槛处理。
- Ready → Startup → Active → Recovery → Ready 的时序、伤害 25、范围 2、角度 100°、前摇 0.18s/命中 0.08s/后摇 0.3s、32 敌人的空间筛选与每目标去重均保持原值。客户端不参与权威体力扣费或伤害结算；本阶段没有体力恢复、玩家受击、奖励或存档链。

## 【KNOWN ISSUES】第 4A 阶段战斗验收

服务端分支、编译和实际烘焙配置已完成静态核对；用户已确认第 4A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4A 阶段通过。战斗验收仅覆盖当前独立网络原型的成功启动攻击一次扣 10（含空挥）、忙碌输入拒绝且原阶段继续推进、10 次成功启动后体力为 0 并拒绝后续攻击且序号不增加，以及原群体伤害和死亡显示回归；双玩家体力独立性及同步、无自动恢复、重新加入恢复 100 和原移动回归范围见[玩家](Player.md)。第 2B、第 3A、第 3B 的既有通过范围保持原状，不扩展为规模性能、平台构建或线上联调验收。AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4B 阶段击杀归属与奖励事件

敌人既有 Authoring 烘焙 `CombatPrototypeKillRewardConfig` 与空 `CombatPrototypeKillRewardEvent` 缓冲，两者均限定为 Server Prefab 组件；Coin=1、Experience=10，当前配置与事件还包含第 4C 的 ItemName/ItemQuantity，事件保留 AttackerNetworkId 和 AttackSequence，存放在当前敌人实体上。Baker 检查金币/经验非负、物品名非空且可转为 FixedString64Bytes、物品数量为正。

`CombatPrototypeDamageSystem` 保持原伤害事件处理顺序。只有当前事件首次使敌人从存活进入 IsDead=1 时，才为该事件的攻击者生成一次奖励事件；后续伤害遇到 IsDead 即停止并清空伤害缓冲，不再次产奖。归属采用致命一击，不按累计伤害或额外排序判定；没有新增去重账本或复活规则。

## 【CURRENT STRATEGY】第 4B 阶段统一奖励结算

调用链为：`致命伤害 → 敌人击杀奖励事件 → CombatPrototypeRewardSystem 确认在线接收玩家 → 完整候选奖励 → 第 4D 服务端存档写入成功 → 同次更新金币、经验与第 4C 物品 → Ghost 同步和原日志`。奖励系统仅在 ServerSimulation 的 PredictedSimulationSystemGroup、伤害系统之后执行，从有效在线连接的 CommandTarget 找到玩家；残留 GhostOwner 不作为在线证明。

每个独立奖励统一准备金币、经验与目标物品结果。整数加法溢出、必需奖励/背包/身份组件缺失、准备或第 4D 保存失败时，记录敌人、攻击者、攻击序号及原因，保存异常另含 PlayerId 与文件路径；跳过整个事件继续后续事件，不留下部分奖励提交。攻击者已离线时记录 AttackerOffline，不补发；本次奖励缓冲处理后统一清空，失败和离线事件也不重放。奖励事件生成时的必需数据缺失或异常同样记录并隔离，不阻断其他敌人。

每敌人奖励 1 金币、10 经验和第 4C 小块肉 1；原体力 100/100、攻击成本 10 且无恢复、伤害 25、范围 2、角度 100° 和 0.18/0.08/0.3 秒时序保持。32 敌人的追踪、伤害、死亡状态和客户端隐藏规则保持；网络原型存档见[资源与数据](DataResources.md)，正式玩家结算、正式背包、实体掉落和奖励 UI 保持原边界，本局背包规则见[背包与道具](Inventory.md)。

## 【KNOWN ISSUES】第 4B 阶段战斗验收

代码编译和实际奖励配置/空事件缓冲已完成静态核对；用户已确认第 4B 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4B 阶段通过。战斗验收仅覆盖当前独立网络原型的非致命伤害无奖励、致命一击归属并获得金币 1/经验 10、每敌人只奖励一次、一次攻击多目标分别结算、死亡后不重复奖励，以及原体力/伤害/死亡显示回归；双玩家累计独立与双端同步、离线不补发和重新加入归零范围见[玩家](Player.md)。第 4A 及更早阶段通过范围保持，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未运行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【CURRENT STRATEGY】第 4C 阶段三项奖励统一提交

敌人 RewardItemName 复用 `Code_01.Msg.ItemName.小块肉`，RewardItemQuantity=1；伤害系统只在原首次死亡产奖点把配置中的物品名与数量复制进同一事件，不改变伤害顺序或致命一击归属。

服务端奖励边界先检查奖励数据和必需组件，取得金币/经验组件的可写引用，再用 checked 计算金币、经验及同名物品的最终数量；新物品先 EnsureCapacity 准备所需容量，不改变背包条目或数量。第 4D 将该完整候选投影为存档，并在临时文件写完、正式文件替换成功后写入目标条目，再通过已取得的组件引用写回金币/经验；这两次 ECS 提交之间不做分配、结构修改或第二次组件查找。单次事件的三项奖励整体成功或整体拒绝；不存在仅发金币/经验或仅发物品的分支，没有第二份可变 ECS 背包或新增事务系统。

## 【KNOWN ISSUES】第 4C 阶段战斗验收

代码编译、实际小块肉 1/金币 1/经验 10 配置及空奖励缓冲已静态核对；用户已确认第 4C 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4C 阶段通过。战斗验收仅覆盖当前独立网络原型的非致命无奖、致命一击三项奖励一致、每敌人一次、多目标分别结算、同名合并、离线不补发和原体力、伤害、死亡显示回归；双玩家空初始背包、独立累计与双端同步、重新加入清空和原移动回归范围见[玩家](Player.md)。第 4B 及更早通过结论保持原范围，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【KNOWN ISSUES】第 4D 阶段奖励保存验收

奖励候选准备、写盘先于三项 ECS 提交、离线与失败事件消费的顺序已静态核对，奖励系统已由 Unity 编译并加载；用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。战斗范围覆盖保存成功后三项到账并同步、保存失败三项均不发且旧档保留、后续独立奖励继续及原战斗回归，完整边界见[运行入口](Runtime.md)。既有第 4C 通过范围保持；人工结论来自本阶段用户反馈，AI 未运行逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 6A 阶段敌人反击数据

[CombatPrototypeEnemyAttack.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyAttack.cs) 定义仅服务端保留的攻击配置与状态：Damage=10、Range=1.75、StartupSeconds=0.5、RecoverySeconds=1；状态含 Ready/Startup/Recovery、计时、攻击序号及锁定玩家实体/NetworkId。敌人原 Baker 添加配置和 Ready、计时/序号 0、空目标状态，检查各攻击参数为有限正值且 `0 <= StopDistance <= AttackRange`。

## 【CURRENT STRATEGY】第 6A 阶段反击与玩家伤害顺序

现有服务端预测模拟组的完整顺序为：`玩家移动 → 敌人目标与移动 → 敌人空间索引 → 玩家近战事件 → 敌人伤害 → 击杀奖励/存档 → CombatPrototypeEnemyAttackSystem → CombatPrototypePlayerDamageSystem → CombatPrototypePlayerRespawnSystem`。前八项沿用第 6A 顺序及职责，第 6B 在末尾接入独立玩家复活系统。

- [敌人攻击系统](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyAttackSystem.cs) 复用当前最近在线存活目标。Ready 仅在 X/Z 距离不超过 1.75 时锁定该实体及 NetworkId，递增一次攻击序号并进入 0.5 秒前摇；前摇与后摇中敌人停止移动和旋转。
- 前摇结束先进入 1 秒后摇，再对锁定目标重新确认 Connected、NetworkStreamInGame、CommandTarget、玩家生命与 X/Z 距离。目标离线、死亡或出范围则空击，同次挥击不换目标；命中只添加一条伤害 10 的玩家事件，后摇不再产生命中。后摇结束清空锁定目标并回 Ready。
- 反击系统先检查敌人死亡标记并取消其阶段/锁定目标，因此同 tick 被玩家击杀的敌人不再反击。原敌人目标字段可持续选择最近存活玩家，进行中的攻击使用独立锁定目标，二者不相互覆盖。
- [玩家伤害系统](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerDamageSystem.cs) 在反击之后依缓冲顺序结算，把玩家 HP 截到不小于 0，每次有效受击递增 HitSequence，归零设置 IsDead。多个敌人的伤害各自叠加；玩家死亡后不处理其后续伤害，全部缓冲在同 tick 消费清空，不在后续 tick 或重连重放。
- 在线目标缺失必需生命组件、命中目标缺失必需玩家伤害缓冲或事件写入异常会记录阶段、玩家/敌人/序号及原因（异常包含原始异常），隔离当前条目并继续后续目标或敌人。不存在默认生命、动态补组件或失败重试挥击。

玩家生命、死亡输入门槛、局内复活和重入初始化归[玩家](Player.md)。敌人反击仅由服务端执行，客户端只接收玩家生命快照；没有玩家伤害预测、无敌时间、防御减伤、击退、动画或治疗链。

## 【KNOWN ISSUES】第 6A 阶段战斗验收

两个系统及数据已由 Unity 编译并加载，实际攻击配置与 Ready 初态、玩家空事件缓冲和 Ghost 生命字段已静态核对；用户已确认第 6A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。战斗范围覆盖前摇单次命中/后摇停动、离线/死亡/出范围空击、同 tick 击杀取消反击、事件不重复扣血、死亡输入门槛及原敌人伤害/显示/奖励存档回归，完整边界见[运行入口](Runtime.md)。32 敌人伤害允许叠加，玩家站立可能很快死亡；规模性能仍为 UNKNOWN，版本与负载限制见[性能基线](Performance.md)。人工结论来自用户反馈，AI 未运行游戏系统、PlayMode、逻辑单元测试或构建。

## 【FACT】第 6B 阶段旧攻击与锁定清理

复活系统在玩家伤害后清除匹配玩家实体/NetworkId 的 CombatPrototypeEnemyTarget 和 CombatPrototypeEnemyAttackState 锁定。旧 Startup 取消并进入配置的完整 Recovery；旧 Recovery 保留剩余计时。序号及其他玩家锁定保持，旧挥击不命中新生命；后摇结束后沿原规则重新攻击。玩家状态收尾归[玩家](Player.md)。

## 【KNOWN ISSUES】第 6B 阶段战斗验收

用户已确认第 6B 人工 GamePlayer 验收通过，主线程结合既有静态核对与反馈判定该阶段通过。战斗范围覆盖旧前摇取消、旧后摇计时保持、双玩家锁定隔离及原战斗/奖励回归，完整边界归[运行入口](Runtime.md)。不扩展为规模性能或平台验收；AI 未运行游戏系统或逻辑单元测试。
