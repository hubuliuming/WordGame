# 战斗地图树木砍伐与原点再生

返回[战斗地图](Map.md)。本专题负责独立 NetCode 原型的 F 砍伐、原点再生、树木状态及移动阻挡同步；木材飞行、G 拾取、保存和到期清理由[掉落与拾取](MapDrops.md)维护。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [MapTreeHarvestConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapTreeHarvestConfig.cs) | 地图定义的必填 treeHarvest DTO |
| [MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 校验树木/木材显式绑定、烘焙配置与 Harvestable 路由 |
| [TreeData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeData.cs) | Ghost 状态/阻挡历史、地图配置与仅服务端砍伐/再生计时 |
| [TreeAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeAuthoring.cs) | 原树木 Prefab 的状态/进度及空阻挡历史烘焙 |
| [TreeSpawnSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeSpawnSystem.cs) | 服务端按原布置生成树木 Ghost，拥有并清理实例 |
| [TreeHarvestSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeHarvestSystem.cs) | 接收统一 F 指定目标、预约/中断、计时与完成提交 |
| [TreeRegrowSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeRegrowSystem.cs) | 服务端到期与存活角色占位检查、原实体恢复及失败回滚 |
| [TreeObstacleSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeObstacleSystem.cs) | 移动前按权威砍倒/再生历史重建本次预测 tick 的阻挡开关 |
| [TreeRenderSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeRenderSystem.cs) | 客户端按 Felled 控制显示 |
| [DropSpawnUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnUtility.cs) | 苹果/木材共用的掉落实例初始化与半成品清理 |
| [TreeHarvestAssetBuilder](../../Assets/Scripts/Editor/CombatPrototypeMapTreeHarvestAssetBuilder.cs) | 创建四项明确的新资源，不覆盖已有资源 |
| [TreeHarvestBinding](../../Assets/Scripts/Editor/CombatPrototypeMapTreeHarvestBinding.cs) | 原干净 SubScene 仅追加 tree_harvest/drop_wood 引用 |

[PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 的 F 单次按下只给本地 GhostOwnerIsLocal 写入已有 Gather，树木由[统一交互](Map.md)选择并调用 TreeHarvest.TryBegin；HarvestTree 字段保留，H 不再触发或消费。客户端不提交目标/产出，各端仍须使用相同代码、输入布局、配置与 Ghost 资源。

## 【FACT】JSON 契约与当前默认值

当前两份地图定义与 BuiltIn 均为 schemaVersion=13、configRevision=16，treeHarvest 段及全部字段必填。树木基础配置仍为2米/徒手2秒/wood×3；[斧头](MapGatherTools.md)可用时1.5秒，再生复用 objects.tree_normal 的 true/600 秒；原空间、种子、树木/采集物密度、出生及 movement/drops/grounds 数值保持。新增 mining 段与生态矿点字段归[采矿](MapMining.md)，树木规则独立。配置在 SubScene 烘焙时读取，Json 失败不回退 BuiltIn，不支持热重载或新的联网配置校验协议。

| 字段 | 当前默认值 | 校验与作用 |
|---|---|---|
| enabled | true | 独立控制树木砍伐；false 时按原静态树显示与阻挡 |
| treeObjectId | tree_normal | 引用已有生态树木，要求非 gatherable、blocksMovement=true、正交互距离 |
| visualResourceKey | tree_harvest | 显式绑定单根插值树木 Ghost |
| harvestDurationSeconds | 2 | 有限正数，服务端模拟时间计时 |
| dropItemId | wood | 稳定物品 ID；映射 Msg.ItemName.木材 |
| dropQuantity | 3 | 正整数，一份掉落 Ghost 承载该数量 |
| dropVisualResourceKey | drop_wood | 显式绑定单根插值掉落 Ghost |

交互距离复用 objects 中所选树木的 interactionDistanceMeters，当前为 2 米，不增加第二份距离配置。默认 tree_normal 的占地 0.5 米、同类间距 3 米及原布置保持；gatherDurationSeconds=1 不用于砍伐，砍伐时长来自 treeHarvest。

再生复用 objects 中所选树木的 regrowEnabled=true、regrowSeconds=600，Map Baker 复制到地图根的服务端 TreeSettings。间隔必须有限且非负，启用时须大于 0；关闭时允许 0，沿原校验器检查，不新增 DTO 或默认补字段。开关只控制成功砍倒后的本局再生，砍伐开关关闭时仍是原静态树。gather_apple 的独立再生配置保持。

字符串沿原小写ASCII/数字/下划线、最长61字符约束；产出ID由原Resolver校验vitality_apple/wood/stone，砍伐默认wood，未知ID失败。旧地图v1～v12、缺段/字段、未知/重复字段或错类型沿严格UTF-8边界报错，不补默认字段；enabled=false仍校验其余字段与资源绑定。工具定义归[采集工具](MapGatherTools.md)。

木材复用 drops 的 pickupDistanceMeters=2、flightDurationSeconds=0.4、scatterRadiusMeters=0.6、arcHeightMeters=0.6、groundOffsetMeters=0.05、visualScale=0.5、lifetimeSeconds=600；寿命 0 关闭自动到期。drops.enabled 只控制敌人额外掉落，treeHarvest.enabled 控制树木产出，二者独立；数值、运动、G 和清理规则归掉落专题。

## 【CURRENT STRATEGY】生成、状态与预约

Map Baker 保留原 LayoutBuilder 的物体/阻挡布置。砍伐开启时，仅所选 treeObjectId 的 MapObject.Harvestable=1、ResourceKey/Prefab 路由到新树木 Ghost；客户端 MapPresentation 跳过 Harvestable 与 Gatherable，避免再生成静态副本。关闭砍伐时使用原 TreeNormal.prefab，不生成树木 Ghost，原静态阻挡仍存在。

TreeSpawn 在服务端地图就绪后、准入前复制物体/布置缓冲，逐项实例化并写入原位置、Yaw、PlacementIndex 和 Standing，清空进度/阻挡历史；只有初始化完整的实体登记有效所有权。单项失败记录地图、阶段、布置索引、物体/资源/Prefab 与原异常，清理当前半成品并继续；源更换/失效、停止及 World 销毁时只清理自身实例。共享资源保持。

启用再生时状态为 Standing → Chopping → Felled → Standing；关闭再生时 Felled 保持至当前 World 释放。服务端从 Connected、NetworkStreamInGame、无断线请求的连接 CommandTarget 取当前启用 Simulate 的玩家；要求 GhostOwner 与 NetworkId 一致、存活、有限零 Move、没有攻击请求且近战 Ready。非零移动输入即使被树挡住仍拒绝。

统一入口按 NetworkId 升序跨采集点/树木/矿点选最近有效目标，并立即调用指定树木的 TryBegin；只接受 Standing。保存Collector、StartHitSequence、ToolKind、ActualDuration和FinishAt=当前模拟时间+锁定时长，进入 Chopping。重复 F 不重置、不换目标，按住不自动连续砍伐；精确同距、每类型范围和互斥规则归[战斗地图](Map.md)。

TreeHarvest 在统一入口/Gather 之后、R 复活之前只维护预约。移动、攻击、受击序号变化、死亡、超距、归属变化或断线取消，恢复 Standing，不产木材。旧 F 采集优先判断已移除；入口记录本 tick 已交互玩家，完成/取消后须新 F，不能同 tick 启动另一资源交互。G/E/R 资格与效果保持。

## 【CURRENT STRATEGY】完成与失败

到时后先核对树木对应的原阻挡记录，再调用原 DropSpawnSystem.SpawnOwnedDrop 创建一份木材堆。苹果和木材共用同一地图源的 DropId 顺序、有效所有权与释放；ID 不复用，生成失败可能留空号。共用 DropSpawnUtility 根据地图种子、DropId 和 drops 数值初始化位置、进度及 Airborne 状态。木材初始位置为树根位置，生成在本次运动/拾取系统之后，后续模拟 tick 进入原飞行/落地链。

木材完全初始化并登记后，重新获取结构变更失效的组件/缓冲访问，先为新增阻挡记录预留容量并取得全部提交引用；使用斧头时先保存扣耐久候选，成功后再提交耐久及Felled/CollectorNetworkId=0、FelledTick 和阻挡 Disabled=1，并追加该权威 tick 的 Disabled=1 历史。清空砍伐进度，启用再生时 RegrowAt=本次 Server World 模拟时间+间隔，否则为 0。Felled 保留为本局 Ghost，恢复 Standing 前不能预约或重复掉落；成功再生后须新 F。砍伐只生成地面资源，不直接入包，不改变金币、经验或战斗奖励。

保存前创建/准备/保存失败时释放当前新掉落、恢复原阻挡记录和历史长度并取消预约；不安排再生，既往轮次历史仍保留。错误保留阶段、地图/布置/玩家、DropId、物品/资源和原异常，其他条目继续。清理/回滚失败另记错误，不伪装成功；耐久保存成功后不执行此回滚，意外ECS提交故障暴露durabilitySaved=true，其恢复保证为UNKNOWN。恢复条件后必须新 F，不自动补发。运行失败与多人争抢的人工通过边界归运行入口；不能实际触发的失败分支仍为 UNKNOWN。

G 按目标 DropState.ItemId 解析实际物品名称，苹果和木材分别同名合并；沿 PrepareReward → SavePrepared 成功后才提交库存及 Consumed。木材不新增使用效果或正式背包UI；G候选保留当前Tools，玩家v2固定ID库存可保存“木材”，v1读取迁移。拾取失败与到期边界归掉落专题。

## 【CURRENT STRATEGY】原点再生与占位

独立 TreeRegrowSystem 仅在服务端预测组、TreeHarvest 之后及 PlayerRespawn 之前执行。砍伐和再生均开启时，只检查 Felled、RegrowAt>0 且服务端模拟时间到期的树；没有到期项时不收集角色。计时从成功生成并登记木材且提交砍倒时开始，取消/失败不安排，拾取木材不改变期限。使用本局模拟时间，不使用客户端时间或墙钟。

恢复前检查原烘焙阻挡圆的位置。服务端全部存活 PlayerNetCode+PlayerHealth+LocalTransform 玩家及存活 EnemyState+LocalTransform 敌人参与占位，不要求玩家当前启用 Simulate；死亡角色不占位。X/Z 中心距离小于等于树木占地半径+对应角色半径+collisionSkin 即等待，当前玩家/敌人阈值分别为 0.91/0.96 米。原点占位时保留 Felled、原期限及历史，后续模拟 tick 在清空后恢复；不推开角色、不换位置、不重置期限。角色位置非有限时记录公共前置错误并停止本批恢复。

到期且原点空闲时先预留历史容量，再追加权威 tick 的 Disabled=0，清空进度，原实体恢复 Standing/CollectorNetworkId=0/FelledTick=0，并启用原阻挡。位置、Yaw、PlacementIndex 和原 Ghost 保持；客户端原显示链随 Standing 恢复。不实例化新树、不发物品、不写盘、不自动再次砍伐。单树提交失败恢复原状态、进度、历史长度及阻挡，记录地图/布置/树木/物体/资源/阶段与原异常，继续其他树；回滚失败另记错误。保留原期限，后续检查继续尝试恢复。系统停止清理自身索引缓存，实例和历史释放仍由原 TreeSpawn/World 生命周期负责。

## 【CURRENT STRATEGY】同步、阻挡与生命周期

TreeState 的 PlacementIndex、Phase、CollectorNetworkId、FelledTick 四字段通过 Ghost 同步。另有 CombatPrototypeMapTreeBlockingEvent 动态缓冲，每条同步 TransitionTick(uint) 与 Disabled(byte，0 启用/1 禁用)；tick 保存 NetworkTime.ServerTick.SerializedData，包含有效性位。TreeProgress 标注仅服务端，含Collector、StartHitSequence、FinishAt、ToolKind、ActualDuration、RegrowAt，不同步预约实体或期限。地图根 TreeSettings 标注 Server，由服务端生成/砍伐/再生消费；地图根本身仍不是 Ghost。

TreeObstacle 在 Client/Server 预测组、玩家与敌人移动之前执行，只处理 Harvestable 对应记录。每次先恢复原启用状态，对 Standing/Chopping/Felled 均从历史末尾查找当前模拟 tick 严格晚于的最近转换，再写入其 Disabled。砍倒和再生均在该 tick 的移动后提交，阻挡转换从下一模拟 tick 生效；回放到转换之前或两次转换之间按历史恢复对应状态，不能只用最新 Phase/FelledTick。无快照/空历史时保留原阻挡；缺历史或无效记录明确报错。显示系统不推断或修改服务端状态。阻挡历史内部容量为 4，每完整砍倒/再生轮次增加两条；保留当前 World 全部轮次，不截断，随树木实体释放。取消预约及重新 F 不清空已有历史。第八阶段多轮跨端/晚加入的人工通过边界归[运行入口](Runtime.md)；未实际触发的延迟/回放时序仍为 UNKNOWN。

MapMovementUtility 跳过 Disabled 记录，玩家预测与服务端敌人继续复用原扫掠/滑动算法。TreeRender 在客户端 Presentation、EntitiesGraphics 前按 Felled 禁用 MaterialMeshInfo；树木没有物理倒伏、Collider、Rigidbody、树桩、Animator 或砍伐动画。

树木状态、再生期限、阻挡历史与地面木材只保留当前 Server World，不写世界存档。重启后按原布局重新生成 Standing 树木，未拾取掉落清空；已入包木材沿玩家v2存档恢复，读取v1迁移；原F采集物600秒再生保持，全部保存候选保留Tools。

## 【FACT】资源与 Editor 边界

- [HarvestableTree.prefab](../../Assets/Prefabs/CombatPrototype/Map/HarvestableTree.prefab)：单根插值 Dynamic Ghost、TreeAuthoring 与必需的 LinkedEntityGroupAuthoring，复用原 TreeNormal.asset/.mat，无 Owner/AutoCommandTarget。
- [DroppedWood.prefab](../../Assets/Prefabs/CombatPrototype/Map/DroppedWood.prefab)：单根插值 Dynamic Drop Ghost，复用原 DropAuthoring，绑定新木材网格/材质。
- [DroppedWood.asset](../../Assets/Art/Map/CombatPrototype/Meshes/DroppedWood.asset)：程序生成八边木材占位网格，18 顶点/32 三角形，尺寸 1.2×0.44×0.44 米，底部 Y=0。
- [DroppedWood.mat](../../Assets/Art/Map/CombatPrototype/Materials/DroppedWood.mat)：复用原树木 shader，_BaseColor=(0.40,0.24,0.11)、Smoothness=0、Back cull，启用 GPU Instancing。

Tools/CombatPrototype/地图 下“生成第七阶段砍伐资源”预检原目录和依赖，只创建上述四个新路径，存在资源或 meta 时拒绝覆盖；资源与 Prefab 创建逐项隔离失败，临时 Editor 场景在结束时释放。“绑定第七阶段砍伐资源”要求原唯一根节点、MapAuthoring/Spawner 和干净 SubScene，检查两种模板后仅追加 tree_harvest/drop_wood 引用，拒绝重复绑定。

新资源和脚本 meta 由 Unity 导入生成。原主场景、旧 Prefab/网格/材质、Animator、旧 meta、包与构建配置保持，原 SubScene 只有两个新增引用。

## 【KNOWN ISSUES】验收状态与边界

第八阶段的正常 Unity 编译、原 H 输入和 TreeState Ghost Serializer、系统顺序/仅服务端 Progress、两种模板隔离 Editor 烘焙及显式资源已静态核对，主线程静态验收通过。该阶段 Json 与 BuiltIn 完整值一致，为 v5/revision=7，tree_normal 再生 true/600 秒、TreeSettings/物体定义一致；阻挡历史 Ghost Serializer/Snapshot 已生成，原 Prefab 烘焙历史为空且 RegrowAt=0；森林/草原仍为 89/53 树木与初始启用阻挡、36/38 采集点、9 块/2304 格/96×96 米，占地/间距/保护区/敌人出生重叠违规为 0。树木、木材、苹果及采集物的 GhostType 互不重复；木材网格有限、三角形无退化且朝外。检查没有运行砍伐、移动、生成、拾取等游戏系统。

用户已确认第七阶段人工 GamePlayer 验收通过，主线程结合既有静态核对与用户反馈判定该阶段通过，范围限[运行入口](Runtime.md)第七阶段十项清单及 v5/revision=6 版本。人工结论来自用户反馈，未实际触发的临界距离、精确同距、同 tick、延迟/预测回放及创建/清理/回滚失败分支仍为 UNKNOWN。第一/第三/第四/第五/第六阶段通过仅限各自版本与原清单，第二阶段独立 JSON 人工清单仍为 UNKNOWN；规模性能、平台构建和线上联调未验收。

第八阶段正常编译、仅服务端 RegrowAt/系统顺序、两种模板隔离烘焙及原资源不变已静态核对，主线程静态验收通过；用户已确认第八阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定该阶段通过，范围限[运行入口](Runtime.md)第八阶段八项清单及 v5/revision=7。人工结论来自用户反馈；未实际触发的精确边界、同 tick、延迟/预测回放和创建/清理/回滚失败仍为 UNKNOWN。第七阶段通过仍限原版本/清单。阻挡历史随轮次增长，其内存/网络开销未测量。

斧头自动使用/耐久归[采集工具](MapGatherTools.md)，未接手动装备、砍伐动作、树桩、世界状态持久化、攻击遮挡、寻路、通用动态对象框架或物品使用效果；F 目标提示/进度归[交互显示](MapInteractionHud.md)。同步写盘耗时、规模性能、平台构建和线上联调未验收。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交 Git。

相关规则：[掉落与拾取](MapDrops.md)、[背包与道具](Inventory.md)、[玩家](Player.md)、[战斗](Combat.md)、[资源与数据](DataResources.md)。

采集点、树木和矿点现共用 F、跨类型最近选择，无 F/H/J 类型优先；旧 H 第七/第八阶段通过仍限原版本，统一 F 已获用户人工通过反馈，范围与未触发用例归[运行入口](Runtime.md)。

工具v8/revision=11完成保存与锁定字段已编译/静态烘焙核对，用户确认工具人工通过限v8/revision=11十二项清单，未实际触发的独立失败/边界/预测用例仍为UNKNOWN；旧树木用户通过只覆盖原版本/清单，见[工具](MapGatherTools.md)/[运行入口](Runtime.md)。

v9/revision12阶段的[材料面板](MapInventoryPanel.md)只展示木材库存/斧头耐久和原配方，按钮沿原制作链；砍伐/木材/阻挡/再生逻辑保持，用户确认面板人工通过限[运行入口](Runtime.md)v9/revision12十二项，未触发的独立显示/输入失败仍UNKNOWN。

当前地图v13/16必填[背包丢弃](MapInventoryDrop.md)，复用原掉落资源及保存链；本专题原交互/工具/产出/再生行为保持。新增丢弃静态及用户人工通过限[运行入口](Runtime.md)v10/13十二项，未触发用例UNKNOWN；旧通过仍限原版本/清单。

v11/14阶段的[G提示](MapPickupHud.md)只读共用掉落目标，与原F目标/工具进度独立；不修改本专题资源状态、产出、工具耐久、再生或保存。新显示编译/十次隔离烘焙静态通过；用户确认人工通过限v11/14十项，未触发用例UNKNOWN，旧用户通过保持各自版本/清单。

v12/15的[高亮](MapInteractionHighlight.md)按原Kind/PlacementIndex解析树木客户端Ghost，Ready黄圈、Working绿圈，默认半径0.9米；读取原Standing/Chopping/Felled及CollectorNetworkId，不新增资源状态或倒计时。树木砍伐/掉落/阻挡/再生及工具保存链保持，高亮静态及用户人工通过限v12/15十项，未触发独立用例UNKNOWN。

当前v13/16的[资源状态](MapResourceStatusHud.md)只读Standing/Chopping/Felled与原服务端RegrowAt；到期仍Felled显示等待再生，不由倒计时恢复树木或推断占位原因。无新树桩/世界标记、期限写盘或资源Ghost字段；原砍伐/掉落/占位/阻挡历史保持。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧通过保持原范围。
