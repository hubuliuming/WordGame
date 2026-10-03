# 战斗地图树木砍伐

返回[战斗地图](Map.md)。本专题负责独立 NetCode 原型的 H 砍伐、树木状态及移动阻挡同步；木材飞行、G 拾取、保存和到期清理由[掉落与拾取](MapDrops.md)维护。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [MapTreeHarvestConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapTreeHarvestConfig.cs) | 地图定义的必填 treeHarvest DTO |
| [MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 校验树木/木材显式绑定、烘焙配置与 Harvestable 路由 |
| [TreeData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeData.cs) | Ghost 状态、地图砍伐配置和仅服务端计时进度 |
| [TreeAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeAuthoring.cs) | 新树木 Prefab 的状态/进度烘焙 |
| [TreeSpawnSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeSpawnSystem.cs) | 服务端按原布置生成树木 Ghost，拥有并清理实例 |
| [TreeHarvestSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeHarvestSystem.cs) | H 资格、最近目标、预约/中断、计时与完成提交 |
| [TreeObstacleSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeObstacleSystem.cs) | 移动前按权威砍倒时刻重建本次预测 tick 的阻挡开关 |
| [TreeRenderSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeRenderSystem.cs) | 客户端按 Felled 控制显示 |
| [DropSpawnUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnUtility.cs) | 苹果/木材共用的掉落实例初始化与半成品清理 |
| [TreeHarvestAssetBuilder](../../Assets/Scripts/Editor/CombatPrototypeMapTreeHarvestAssetBuilder.cs) | 创建四项明确的新资源，不覆盖已有资源 |
| [TreeHarvestBinding](../../Assets/Scripts/Editor/CombatPrototypeMapTreeHarvestBinding.cs) | 原干净 SubScene 仅追加 tree_harvest/drop_wood 引用 |

[PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 在原输入增加 InputEvent HarvestTree，H 单次按下仅写入本地 GhostOwnerIsLocal 的事件；客户端不提交目标、不生成木材或结算库存。所有端须使用同一输入布局、配置和 Ghost 资源。

## 【FACT】JSON 契约与当前默认值

两份地图定义与 BuiltIn 均为 schemaVersion=5、configRevision=6，treeHarvest 段及全部字段必填。原空间、种子、生态密度、出生、movement/drops 和三份共享 JSON 保持。配置仍在 SubScene 烘焙时读取，Json 失败不回退 BuiltIn，不支持热重载或新的联网配置校验协议。

| 字段 | 当前默认值 | 校验与作用 |
|---|---|---|
| enabled | true | 独立控制树木砍伐；false 时按原静态树显示与阻挡 |
| treeObjectId | tree_normal | 引用已有生态树木，要求非 gatherable、blocksMovement=true、正交互距离 |
| visualResourceKey | tree_harvest | 显式绑定单根插值树木 Ghost |
| harvestDurationSeconds | 2 | 有限正数，服务端模拟时间计时 |
| dropItemId | wood | 稳定物品 ID；映射 Msg.ItemName.木材 |
| dropQuantity | 3 | 正整数，一份掉落 Ghost 承载该数量 |
| dropVisualResourceKey | drop_wood | 显式绑定单根插值掉落 Ghost |

交互距离复用 objects 中所选树木的 interactionDistanceMeters，当前为 2 米，不增加第二份距离配置。默认 tree_normal 的占地 0.5 米、同类间距 3 米及原布置保持。

字符串沿既有小写 ASCII/数字/下划线、最长 61 字符约束。物品 ID 白名单为 vitality_apple/wood；未知 ID 明确失败。旧 v1/v2/v3/v4、缺段/字段、未知/重复字段或类型错误沿原严格 UTF-8 JSON 边界报错，不补默认字段。enabled=false 时其余字段与资源绑定仍必须有效。

木材复用 drops 的 pickupDistanceMeters=2、flightDurationSeconds=0.4、scatterRadiusMeters=0.6、arcHeightMeters=0.6、groundOffsetMeters=0.05、visualScale=0.5、lifetimeSeconds=600；寿命 0 关闭自动到期。drops.enabled 只控制敌人额外掉落，treeHarvest.enabled 控制树木产出，二者独立；数值、运动、G 和清理规则归掉落专题。

## 【CURRENT STRATEGY】生成、状态与预约

Map Baker 保留原 LayoutBuilder 的物体/阻挡布置。砍伐开启时，仅所选 treeObjectId 的 MapObject.Harvestable=1、ResourceKey/Prefab 路由到新树木 Ghost；客户端 MapPresentation 跳过 Harvestable 与 Gatherable，避免再生成静态副本。关闭砍伐时使用原 TreeNormal.prefab，不生成树木 Ghost，原静态阻挡仍存在。

TreeSpawn 在服务端地图就绪后、准入前复制物体/布置缓冲，逐项实例化并写入原位置、Yaw、PlacementIndex 和 Standing；只有初始化完整的实体登记有效所有权。单项失败记录地图、阶段、布置索引、物体/资源/Prefab 与原异常，清理当前半成品并继续；源更换/失效、停止及 World 销毁时只清理自身实例。共享资源保持。

状态为 Standing → Chopping → Felled。服务端从 Connected、NetworkStreamInGame、无断线请求的连接 CommandTarget 取当前启用 Simulate 的玩家；要求 GhostOwner 与 NetworkId 一致、存活、有限零 Move、没有攻击请求且近战 Ready。非零移动输入即使被树挡住仍拒绝。

同次请求按 NetworkId 升序处理，H 取 X/Z 交互距离内最近 Standing 树木，精确同距取较小 PlacementIndex；没有目标日志 NoStandingTarget。预约后保存 Collector、StartHitSequence 和 FinishAt=当前模拟时间+时长。重复 H 不重置期限、不预约第二棵，持续按住 H 不自动连续砍伐。

TreeHarvest 在原玩家伤害和 F Gather 之后、R 复活之前处理。移动、攻击、受击序号变化、死亡、超距、归属变化或断线取消预约，树木恢复 Standing，不产木材。F 请求或正在 Collecting 的玩家优先：拒绝新的 H，取消既有砍伐；同次 F/H 不同时进行。原 Gather 系统未修改；H 不改变 E/R/G 的既有资格或使用效果，G 仍按原规则独立请求。

## 【CURRENT STRATEGY】完成与失败

到时后先核对树木对应的原阻挡记录，再调用原 DropSpawnSystem.SpawnOwnedDrop 创建一份木材堆。苹果和木材共用同一地图源的 DropId 顺序、有效所有权与释放；ID 不复用，生成失败可能留空号。共用 DropSpawnUtility 根据地图种子、DropId 和 drops 数值初始化位置、进度及 Airborne 状态。木材初始位置为树根位置，生成在本次运动/拾取系统之后，后续模拟 tick 进入原飞行/落地链。

木材完全初始化并登记后，重新获取结构变更失效的组件/缓冲访问，清空砍伐进度、设置阻挡 Disabled=1、树木 Felled/CollectorNetworkId=0 并记录 FelledTick。Felled 保留为本局 Ghost，不再预约或重复掉落。砍伐只生成地面资源，不直接入包，不改变金币、经验或战斗奖励。

创建/提交失败时释放当前新掉落、恢复原阻挡记录并取消预约；错误保留阶段、地图/布置/玩家、DropId、物品/资源和原异常，其他条目继续。清理/回滚失败另记错误，不伪装成功。恢复条件后必须新 H，不自动补发。运行失败和多人争抢结果仍待人工 GamePlayer 核对。

G 按目标 DropState.ItemId 解析实际物品名称，苹果和木材分别同名合并；沿 PrepareReward → SavePrepared 成功后才提交库存及 Consumed。木材不新增使用效果、正式背包 UI 或存档字段；原 v1 固定玩家 ID 库存可保存“木材”。拾取失败与到期边界归掉落专题。

## 【CURRENT STRATEGY】同步、阻挡与生命周期

TreeState 的 PlacementIndex、Phase、CollectorNetworkId、FelledTick 四字段通过 Ghost 同步。FelledTick 保存 NetworkTime.ServerTick.SerializedData，包含有效性位；TreeProgress 标注仅服务端，不同步 Collector 实体、受击基准或完成期限。地图根 TreeSettings 标注 Server，仅由服务端生成/砍伐消费；地图根本身仍不是 Ghost。

TreeObstacle 在 Client/Server 预测组、玩家与敌人移动之前执行，只处理 Harvestable 对应记录。每次先恢复原启用状态，再按权威 FelledTick 判断：当前模拟 tick 严格晚于砍倒 tick 才禁用阻挡，因为完成提交发生在该 tick 的移动之后。客户端预测回放至此前 tick 时恢复阻挡；不由客户端显示系统推断或修改服务端状态。没有树木快照时保留原阻挡。实际跨端修正、回放抖动与晚加入时序仍为 UNKNOWN。

MapMovementUtility 跳过 Disabled 记录，玩家预测与服务端敌人继续复用原扫掠/滑动算法。TreeRender 在客户端 Presentation、EntitiesGraphics 前按 Felled 禁用 MaterialMeshInfo；树木没有物理倒伏、Collider、Rigidbody、树桩、Animator 或砍伐动画。

树木状态与地面木材只保留当前 Server World，不写世界存档、不安排树木再生。重启后按原布局重新生成 Standing 树木，未拾取掉落清空；已成功入包的木材沿原玩家 v1 存档恢复。原 F 采集物 600 秒原点再生、原奖励与存储类保持。

## 【FACT】资源与 Editor 边界

- [HarvestableTree.prefab](../../Assets/Prefabs/CombatPrototype/Map/HarvestableTree.prefab)：单根插值 Dynamic Ghost、TreeAuthoring 与必需的 LinkedEntityGroupAuthoring，复用原 TreeNormal.asset/.mat，无 Owner/AutoCommandTarget。
- [DroppedWood.prefab](../../Assets/Prefabs/CombatPrototype/Map/DroppedWood.prefab)：单根插值 Dynamic Drop Ghost，复用原 DropAuthoring，绑定新木材网格/材质。
- [DroppedWood.asset](../../Assets/Art/Map/CombatPrototype/Meshes/DroppedWood.asset)：程序生成八边木材占位网格，18 顶点/32 三角形，尺寸 1.2×0.44×0.44 米，底部 Y=0。
- [DroppedWood.mat](../../Assets/Art/Map/CombatPrototype/Materials/DroppedWood.mat)：复用原树木 shader，_BaseColor=(0.40,0.24,0.11)、Smoothness=0、Back cull，启用 GPU Instancing。

Tools/CombatPrototype/地图 下“生成第七阶段砍伐资源”预检原目录和依赖，只创建上述四个新路径，存在资源或 meta 时拒绝覆盖；资源与 Prefab 创建逐项隔离失败，临时 Editor 场景在结束时释放。“绑定第七阶段砍伐资源”要求原唯一根节点、MapAuthoring/Spawner 和干净 SubScene，检查两种模板后仅追加 tree_harvest/drop_wood 引用，拒绝重复绑定。

新资源和脚本 meta 由 Unity 导入生成。原主场景、旧 Prefab/网格/材质、Animator、旧 meta、包与构建配置保持，原 SubScene 只有两个新增引用。

## 【KNOWN ISSUES】验收状态与边界

正常 Unity 编译、HarvestTree 输入和 TreeState Ghost Serializer、系统顺序/仅服务端 Progress、两种模板隔离 Editor 烘焙及显式资源已静态核对，主线程静态验收通过。Json 与 BuiltIn 完整值一致，为 v5/revision=6；森林/草原仍为 89/53 树木与初始启用阻挡、36/38 采集点、9 块/2304 格/96×96 米，占地/间距/保护区/敌人出生重叠违规为 0。树木、木材、苹果及采集物的 GhostType 互不重复；木材网格有限、三角形无退化且朝外。检查没有运行砍伐、移动、生成、拾取等游戏系统。

第七阶段人工 GamePlayer 为 UNKNOWN，清单归[运行入口](Runtime.md)。第一/第三/第四/第五/第六阶段通过仅限各自版本与原清单，第二阶段独立 JSON 人工清单仍为 UNKNOWN。跨端预测、取消/争抢、失败回滚、木材保存/恢复及原玩法回归不能以静态编译/烘焙代替。

未接斧头装备、工具耐久、砍伐动作、树桩、树木再生、世界状态持久化、攻击遮挡、寻路、通用动态对象框架、物品使用效果或新 UI。同步写盘耗时、规模性能、平台构建和线上联调未验收。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交 Git。

相关规则：[掉落与拾取](MapDrops.md)、[背包与道具](Inventory.md)、[玩家](Player.md)、[战斗](Combat.md)、[资源与数据](DataResources.md)。
