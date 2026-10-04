# 战斗地图动态掉落与拾取

返回[战斗地图](Map.md)。本专题负责独立 NetCode 原型的额外敌人掉落、木材和石材共用飞行/落地、G 拾取、保存及清理；砍伐来源归[树木砍伐](MapTreeHarvest.md)，采矿来源归[采矿](MapMining.md)，种植采集和原点再生仍由地图/背包原链维护。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [MapDropConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapDropConfig.cs) | 地图定义的必填 drops DTO |
| [MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 校验显式掉落绑定并烘焙 DropSettings |
| [DropData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropData.cs) | 状态、地图配置与服务端运动/清理进度 |
| [DropAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropAuthoring.cs) | 新掉落 Prefab 的状态/进度烘焙 |
| [DropSpawnSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnSystem.cs) | 首次死亡调用、苹果/木材/石材统一编号与实例所有权 |
| [DropMotionSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropMotionSystem.cs) | 服务端弧线位置与 Landed |
| [DropPickupSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs) | 在线请求、资格/最近目标、存档和入包提交 |
| [DropCleanupSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropCleanupSystem.cs) | 消耗/到期后的延迟实体销毁 |
| [DropRenderSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropRenderSystem.cs) | 客户端按状态控制显示 |
| [PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) | G 单次按下写 Pickup 事件 |
| [DropAssetBuilder](../../Assets/Scripts/Editor/CombatPrototypeMapDropAssetBuilder.cs) | 创建明确的新 Prefab 路径，复用既有网格/材质 |
| [DropBinding](../../Assets/Scripts/Editor/CombatPrototypeMapDropBinding.cs) | 原干净 SubScene 仅追加一个 drop_apple 引用 |

## 【FACT】JSON 契约与当前默认值

[battle_forest_01.json](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[battle_grassland_01.json](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn来源](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)均为schemaVersion=8/configRevision=11，drops必填，工具配置归[采集工具](MapGatherTools.md)。原空间、移动、出生及drops/grounds值保持；mining与生态矿点字段归采矿专题，tree_normal/gather_apple/mine_rock均默认600秒再生。配置只在烘焙时读取，不支持热重载；各端须相同版本、输入布局与资源，未新增一致性协议。

| 字段 | 默认值 | 校验/行为 |
|---|---|---|
| enabled | true | false 禁止敌人新掉落；树木/矿点开关独立，字段/绑定仍必填 |
| itemId | vitality_apple | 稳定 ID；白名单 vitality_apple/wood/stone，分别映射活力苹果/木材/石材 |
| quantity | 1 | 正整数；一份 Ghost 承载配置数量 |
| visualResourceKey | drop_apple | 稳定键，须显式绑定独立掉落 Prefab |
| pickupDistanceMeters | 2 | 有限正数，X/Z 中心距离 |
| flightDurationSeconds | 0.4 | 有限正数，服务端飞行时长 |
| scatterRadiusMeters | 0.6 | 有限非负数，落点 X/Z 散落半径 |
| arcHeightMeters | 0.6 | 有限非负数，相对起终点线性轨迹的弧高 |
| groundOffsetMeters | 0.05 | 有限非负数，终点 Y=地图 BaseHeight+偏移 |
| visualScale | 0.5 | 有限正数，实例 LocalTransform 的统一缩放 |
| lifetimeSeconds | 600 | 有限非负数；从生成时刻计时，0 关闭自动到期 |

全部字段显式填写。字符串遵循原小写ASCII/数字/下划线及最长61字符；缺失/未知/重复字段、错类型或非法值沿严格UTF-8 JSON边界报错。旧地图v1～v7不迁移或补字段，Json失败不回退BuiltIn。资源键由原MapAuthoring.DecorationPrefabs解析，不查找或临时创建兜底。

## 【CURRENT STRATEGY】死亡、生成与飞行

DropSpawn 在服务端预测组的原 Reward 之后、EnemyAttack 之前读取 EnemyState.IsDead。同一地图源内每个敌人首次观测死亡只尝试生成一份额外掉落，DropId 从 1 顺序分配，与树木木材、矿点石材共用且不复用，失败可留空号；原伤害系统和金币/经验/小块肉统一击杀奖励保持。额外苹果独立于原奖励是否成功、攻击者是否在线，且无击杀者所有权；只生成地面物体，不直接入包。

实例初始位置为敌人死亡位置，服务端用地图种子与 DropId 生成圆内落点，终点高度为地图基准加贴地偏移。初始化 Transform、DropState 和 DropProgress 全部成功后才登记有效所有权；失败记录阶段、地图/敌人/DropId/物品/资源及原异常，清理当前半成品，其他死亡条目继续；不自动重放该敌人的生成尝试。没有 Collider、Rigidbody、树木/边界碰撞或落点避让，掉落不参加原移动阻挡和伤害链。

状态为 Airborne → Landed → Consumed。DropMotion 在玩家伤害之后、拾取之前，根据服务端模拟时间计算 t=clamp((now-StartedAt)/FlightDuration,0,1)，位置为起终点线性插值加 Y 方向的 4×t×(1-t)×ArcHeight；t 达到 1 时进入 Landed。Airborne 显示且不可拾取，Landed 显示且可请求；客户端不自行生成、运动结算、入包或控制到期。

## 【CURRENT STRATEGY】G 请求与保存提交

Pickup 为原 IInputComponentData 的新增 InputEvent，客户端只对 GhostOwnerIsLocal 在 G 单次按下时置位，不提交客户端目标。服务端从 Connected、NetworkStreamInGame、未请求断线的连接 CommandTarget 取当前玩家，要求启用 Simulate，GhostOwner 与 NetworkId 一致、存活、有限零 Move、没有攻击请求且近战 Ready。同次更新请求按 NetworkId 升序处理；玩家伤害先执行，拾取先于统一 F 资源交互及 R 复活。

目标仅为本人 X/Z 距离内的 Landed、尚未到期掉落。选最近一个，精确同距取较小 DropId；一次请求只提交一份，任何合格玩家均可拾取。没有合格目标时日志 NoLandedTarget，持续按住 G 不持续发放。即使移动被树木阻挡，非零移动输入仍拒绝。

提交前按目标DropState.ItemId解析实际物品名，取得库存与状态可写引用，checked合并同名数量，新条目先预留容量。PrepareReward投影保持当前金币/经验/Tools的完整库存v2候选，SavePrepared成功才提交库存及Consumed；提交不做结构变更、分配或第二次查找。同次后续请求读取Consumed，不能重复入包；读取v1迁移，临时文件与正式档替换规则保持。

准备/保存失败记录 NetworkId、DropId、物品、阶段及原异常，不提交库存数量或未到期掉落消耗，继续其他玩家请求；恢复后须新 G，不自动重试。到期规则仍生效，不为失败拾取延长寿命。背包沿原 Ghost 缓冲及每 2 秒日志同步观察；苹果/木材/石材没有新增使用效果或正式 UI，E 仍只用小块肉。F 预约、取消、保存后耗尽及 600 秒原点再生保持。

## 【CURRENT STRATEGY】Ghost、到期与释放

Map Baker 在原地图根实体写入 DropSettings，配置标注 Server 且仅由服务端系统消费。独立掉落 Ghost 同步 DropId、ItemId、Quantity、Phase 四字段及原 LocalTransform 位置/旋转/缩放；DropProgress 标注仅服务端，保存起终位置、StartedAt、ExpiresAt 和 CleanupQueued，不向客户端同步期限或运动结算。客户端在 Presentation、EntitiesGraphics 前按 Consumed 禁用 MaterialMeshInfo，Airborne/Landed 保持显示；后续实体销毁沿 Ghost 的移除链处理。

lifetimeSeconds>0 时 ExpiresAt=生成时的 Server World 模拟时间+寿命；为 0 时期限为 0。到期目标在拾取选择中直接排除。DropCleanup 在拾取之后、统一 F 入口之前把已消耗/到期实体置 Consumed，并排队至 EndSimulation ECB 销毁；CleanupQueued 防止多次模拟 tick 重复排队。地图源更换/失效、系统停止及 World 销毁时，DropSpawn 清理其拥有的实体及死亡记录；已排队项保留给 ECB 执行，避免重复销毁。共享 Prefab、网格和材质不随实例销毁。

地面掉落的位置、DropId、飞行/落地/消耗及期限只保留本局，不写玩家或地图存档；重启服务端清空未拾取掉落，新局重新分配 ID。成功入包的苹果/木材/石材沿原固定ID的v2库存保存/恢复，候选保留Tools并兼容读取v1。计时使用 SystemAPI.Time.ElapsedTime，不使用客户端时间或系统墙钟；到期和释放不发奖励、不写盘。运行中地图切换仍未接入，模板/来源切换须 PlayMode 前保存并重新进入。

## 【FACT】资源与 Editor 边界

[DroppedApple.prefab](../../Assets/Prefabs/CombatPrototype/Map/DroppedApple.prefab) 的稳定键为 drop_apple，新单根含 Transform/MeshFilter/MeshRenderer/GhostAuthoringComponent/DropAuthoring 及 Ghost 必需的 LinkedEntityGroupAuthoring；无子节点、Collider、Animator、Owner 或 AutoCommandTarget，只支持 Interpolated，优化模式为 Dynamic。复用原 GatherApple.asset 和 GatherApple.mat，未新建或改写网格/材质，种植 GatherApple.prefab 保持。

Tools/CombatPrototype/地图 下的“生成第六阶段掉落资源”只创建明确的新 Prefab 路径，资源已存在时拒绝覆盖；“绑定第六阶段掉落物”要求原唯一 CombatPrototypeNetworkRoot、MapAuthoring/Spawner、完整资源及干净 SubScene，先检查两种配置与布局，再仅追加一个 drop_apple 引用，已绑定时拒绝重复执行。资源创建使用临时 Editor 场景，绑定只保存原网络 SubScene。新脚本/Prefab 的 meta 由 Unity 正常导入生成，原主场景、玩家/敌人及采集 Prefab、Animator、旧 meta、包与构建设置保持。

## 【KNOWN ISSUES】验收与未接入边界

正常 Unity 编译、生成输入/状态类型、系统顺序、两种模板隔离 Editor 烘焙、drops 参数与资源绑定已静态核对，主线程静态验收通过。用户已明确确认第六阶段人工 GamePlayer 验收通过，主线程结合既有静态核对与用户反馈判定该阶段通过。范围限[运行入口](Runtime.md)第六阶段九项清单的一次性掉落、飞行/落地、G 资格与最近选择、多人争抢、保存失败、晚加入/重启、配置/到期/释放和原玩法回归；人工结论来自用户反馈，未实际触发的临界距离、精确同距与同 tick 用例仍为 UNKNOWN。原第一/第三/第四/第五阶段通过范围保持，第二阶段独立 JSON 人工清单仍为 UNKNOWN。

没有物理碰撞、拾取动画/UI、客户端自动拾取、苹果/木材/石材使用效果、通用动态对象框架、世界掉落持久化、运行热重载或新增联网配置校验协议。同步写盘继续占用服务端线程，运行性能、平台构建与线上联调未验收。AI 未运行游戏模拟/显示系统、GamePlayer/PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

相关规则：[背包与道具](Inventory.md)、[玩家](Player.md)、[战斗](Combat.md)、[资源与数据](DataResources.md)。

## 【FACT】树木木材的共用掉落接入

DropSpawnSystem.SpawnOwnedDrop 接收明确来源、Prefab、资源键、ItemId/数量及起点，通过[DropSpawnUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnUtility.cs)初始化后登记统一所有权；原敌人首次死亡仍只尝试一次。树木在完成时调用同一入口，若树木提交失败则 ReleaseDrop 释放当前木材，不能复用旧 DropId。配置、生成时序及回滚归[树木砍伐](MapTreeHarvest.md)。

木材使用独立 DroppedWood Ghost，四字段/运动/到期/清理与苹果共用；G 按实际 ItemId 解析并保存，不把木材结算为苹果。第六阶段通过仅限当时 v4/苹果版本；用户已确认第七阶段人工 GamePlayer 通过，主线程结合静态核对判定该阶段通过，范围限[运行入口](Runtime.md)第七阶段十项清单中的多物品、木材及失败处理。人工结论来自用户反馈，不能实际触发的创建/清理/回滚及其他独立用例仍为 UNKNOWN。

## 【FACT】树木再生与重复砍伐来源

树木成功砍倒后默认模拟计时 600 秒，原点空闲才恢复；再生本身不创建掉落、不修改库存或保存。恢复后新 F 选中树木并完成再次调用原 SpawnOwnedDrop，继续同局共享且不复用的 DropId；苹果/木材的 G、保存、飞行、到期和释放职责保持。树木再生/占位及阻挡历史归[树木砍伐](MapTreeHarvest.md)，用户已确认第八阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定该阶段通过；掉落范围限重复砍伐新木材与 G/保存/到期/释放回归，完整边界归[运行入口](Runtime.md)第八阶段八项清单。人工结论来自用户反馈，未触发的独立失败用例仍为 UNKNOWN，第六/第七阶段原通过范围保持。

## 【FACT】采矿石材的共用掉落接入

MineHarvest 调用原 SpawnOwnedDrop 生成 DroppedStone，stone 显式映射 Msg.ItemName.石材。石材、木材、敌人苹果共享同局 DropId、运动/落地、G 实际 ItemId 解析、SavePrepared 先于库存/Consumed 提交、到期和实例释放；drops.enabled 只控制敌人额外掉落，采矿由 mining.enabled 控制。矿点提交失败只释放当前石材并尝试恢复原阻挡/历史长度/Available 并清空再生期限，原异常及清理/回滚错误保留，需新 F；完整事务边界归[采矿](MapMining.md)。第九阶段资源/烘焙已静态核对，采矿/G/保存失败及跨端的人工 GamePlayer 为 UNKNOWN，归[运行入口](Runtime.md)；第六至第八阶段通过仍限各自旧版本和清单。

矿点默认成功耗尽后 600 秒原点空闲才恢复，再生本身不创建石材、不修改库存或保存；恢复后新 F 完成再次走原 SpawnOwnedDrop 与同局共享 DropId、G/保存/到期/释放链。规则归[采矿](MapMining.md)，用户已确认再生阶段人工 GamePlayer 通过；掉落范围限新 F 再次产出、G/保存/到期/释放回归，完整边界归[运行入口](Runtime.md)矿点再生八项清单及 v6/revision=9，未触发独立失败分支仍为 UNKNOWN。

砍树/采矿现由统一 F 选目标并启动，产出的木材/石材仍走原地面掉落和 G 保存链；跨类型交互及产出回归已获用户人工通过反馈，结论限[运行入口](Runtime.md)统一 F 八项清单，未实际触发的独立用例仍为 UNKNOWN。旧阶段人工通过范围保持。

## 【FACT】F 交互显示边界

v7/revision=10 接入 F 三类资源的目标提示与原服务端采集进度，规则归[交互显示](MapInteractionHud.md)。HUD 不显示 G 拾取提示，不创建掉落、提交库存/Consumed 或调用保存；原 DropId、飞行/落地、G、到期及释放链保持。本阶段掉落回归已获用户人工通过反馈，限[运行入口](Runtime.md)HUD 八项清单及 v7/revision=10；未实际触发的独立创建/提交/清理/回滚失败仍为 UNKNOWN，既有通过保持各自原范围。

## 【FACT】工具完成与掉落边界

[采集工具](MapGatherTools.md)不改变DropId、产出数量、飞行、G或释放链；使用斧头/镐子完成先准备并登记当前掉落、保存耐久，再提交资源完成。保存前失败只清理本次掉落并取消预约；保存成功后的意外ECS故障不执行旧资源回滚，恢复保证为UNKNOWN。G保存候选携带当前Tools，防止拾取覆盖耐久。本阶段编译/隔离烘焙已静态核对，人工事务与回归均为UNKNOWN，旧掉落用户通过范围保持。
