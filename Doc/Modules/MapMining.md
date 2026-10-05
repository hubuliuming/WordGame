# 战斗地图矿点采集、石材掉落与原点再生

返回[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的 F 采矿、矿点原点再生及状态/阻挡；石材飞行、G 拾取、保存和到期由[掉落与拾取](MapDrops.md)维护。人工清单归[运行入口](Runtime.md)，库存归[背包](Inventory.md)。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [MapMiningConfig](../../Assets/Scripts/CombatPrototype/Map/MapMiningConfig.cs) | 必填 mining DTO |
| [BiomeDefinitionConfig](../../Assets/Scripts/CombatPrototype/Map/BiomeDefinitionConfig.cs) | mineObjectId/mineDensityPer100m2 |
| [MapConfigValidator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) | 字段、数值、引用、独立矿点与再生配置约束 |
| [MapLayoutBuilder](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapLayoutBuilder.cs) | 原树木/采集物后、草丛/碎石前布置矿点 |
| [MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原地图根写入 MineSettings/Mineable 与显式 Ghost 引用 |
| [PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) | F 单次 Gather 事件，仅写本地玩家；Mine 字段保留 |
| [MineData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineData.cs) | 状态、阻挡历史及服务端配置/进度 |
| [MineAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineAuthoring.cs) | 矿点 Ghost 初态/进度与空历史烘焙 |
| [MineSpawn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineSpawnSystem.cs) | 服务端实例生成与所有权清理 |
| [MineHarvest](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineHarvestSystem.cs) | 接收统一 F 指定目标、预约、中断和完成/回滚 |
| [MineRegrow](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineRegrowSystem.cs) | 服务端到期、原点占位检查与原实例恢复 |
| [MineObstacle](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineObstacleSystem.cs) | Client/Server 每个预测 tick 重建矿点阻挡 |
| [MineRender](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineRenderSystem.cs) | 客户端耗尽状态隐藏 |
| [YieldItemResolver](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapYieldItemResolver.cs) / [Msg](../../Assets/Scripts/Msg/Msg.cs) | stone → Msg.ItemName.石材 |
| [MineAssetBuilder](../../Assets/Scripts/Editor/CombatPrototypeMapMineAssetBuilder.cs) | 四个明确新资源路径的创建 |
| [MineBinding](../../Assets/Scripts/Editor/CombatPrototypeMapMineBinding.cs) | 原 SubScene 仅追加两个 Prefab 引用 |

## 【FACT】JSON 契约与默认值

[battle_forest_01.json](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[battle_grassland_01.json](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) 与 [BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) 为 schemaVersion=17/configRevision=20。mining 及全部字段必填，interactionHud归[交互显示](MapInteractionHud.md)，gatherTools归[采集工具](MapGatherTools.md)；旧v1～v16、缺失/未知/重复字段、错误类型或无效引用明确失败，不补字段、不回退来源。配置只在烘焙时读取，无运行热重载或新联网配置校验协议。各端须使用相同代码、输入布局、Ghost、配置及资源。

```json
"mining": {
  "enabled": true,
  "mineObjectId": "mine_rock",
  "visualResourceKey": "mine_rock",
  "harvestDurationSeconds": 3.0,
  "dropItemId": "stone",
  "dropQuantity": 3,
  "dropVisualResourceKey": "drop_stone"
}
```

| 字段 | 默认值 | 约束/用途 |
|---|---|---|
| enabled | true | false 时不布置/实例化矿点，不产采矿掉落；配置与绑定仍必填 |
| mineObjectId | mine_rock | 所有生态引用同一配置矿点，与树木/采集/装饰角色分开 |
| visualResourceKey | mine_rock | 显式单根插值 Mine Ghost |
| harvestDurationSeconds | 3 | 有限正数；服务端模拟计时 |
| dropItemId | stone | 现有白名单 vitality_apple/wood/stone，分别映射活力苹果/木材/石材 |
| dropQuantity | 3 | 正整数；一次完成生成一堆 |
| dropVisualResourceKey | drop_stone | 显式 Drop Ghost |

[biomes.json](../../Assets/Config/CombatPrototype/Map/biomes.json) 新增必填 mineObjectId、mineDensityPer100m2，密度须有限非负。grassland/forest/rocky 分别为 0.1/0.2/1 个每 100㎡可布置面积，均引用 mine_rock；rockObjectId/rockDensityPer100m2 继续控制不阻挡的装饰碎石。

[objects.json](../../Assets/Config/CombatPrototype/Map/objects.json) 在原四项后追加 mine_rock，旧索引保持：visualResourceKey=mine_rock、footprintRadiusMeters=0.75、minimumSameTypeSpacingMeters=2.5、interactionDistanceMeters=2、blocksMovement=true、blocksMelee/blocksProjectile=false、gatherable=false、gatherDurationSeconds=0、yieldItemId=null、yieldQuantity=0、regrowEnabled=true、regrowSeconds=600。再生复用原物体字段，间隔须有限非负、开启时须大于 0；关闭时不安排期限。Map Baker 将开关/间隔复制到仅服务端 MineSettings，v6 旧配置中的 false/0 仍保持耗尽。矿点必须为正占地/交互距离的移动阻挡物、非 gatherable；采矿时长/产出由 mining 控制。普通攻击不破坏矿点，保留能力字段不驱动攻击遮挡。

LayoutBuilder 使用原 seed/随机流，先跨全图完成树木，再采集植物，再启用的矿点，最后草丛/碎石；原树木/采集点位置及朝向保持。矿点沿原占地互斥、同类间距、道路/安全区/战斗区/边缘/敌人初始矩形避让；阻挡物额外计入最大角色半径及留缝。实际数量可少于目标，矿点会改变后续装饰的位置/数量。关闭 mining 后跳过这一随机/占地步骤，原四类布局保持。

## 【CURRENT STRATEGY】生成、状态与显示

MineSpawn 在服务端 Simulation、NetworkReceive 后、GoInGameServer 前取得原地图单例；只生成 Mineable 定义的已布置矿点，settings.Enabled=0 时直接结束。Instantiate 前复制对象/布置缓冲；保留原 Position/Yaw/PlacementIndex，写入 Available、CollectorNetworkId=0、MinedTick=0 和空进度/RegrowAt=0、空 MineBlockingEvent 历史后才登记有效所有权。逐项生成失败记录地图/布置/对象/资源/阶段及原异常，清理当前半成品，继续其他条目，不自动重放整批。

MineState 同步 PlacementIndex、Phase、CollectorNetworkId、MinedTick 四字段，阶段为 Available → Mining → Depleted → Available；MineSettings/Progress 标注 Server，再生配置及 Collector/StartHitSequence/FinishAt/RegrowAt 只由服务端消费。MineBlockingEvent 是独立 Ghost 缓冲，内部容量 4、同步 TransitionTick/Disabled，保留本局全部耗尽/再生转换。耗尽矿点保留原实体，供晚加入接收；再生复用同一 Position/Yaw/PlacementIndex/Ghost，不补生成新实例。[资源存档](MapResourcePersistence.md)开启时恢复耗尽/剩余秒数并重建本局阻挡基态，关闭时从原布局Available、零期限及空历史重置；未拾取石材按[掉落存档](MapDropPersistence.md)开关恢复。

MapPresentation 在校验和实例化两处均跳过 Mineable，不创建静态矿点副本；标记按矿点对象角色写入，关闭时因无布置记录也不会显示矿点。MineRender 在客户端 Presentation、EntitiesGraphics 前按 Depleted 禁用 MaterialMeshInfo，Available/Mining 显示；不提交玩法或存档状态。

## 【CURRENT STRATEGY】F 采矿预约与取消

客户端 F 单次按下只给 GhostOwnerIsLocal 写入已有 Gather；Mine 字段保留，J 停止触发与消费，不提交客户端目标。[统一交互](Map.md)在伤害/G 清理后跨三类选目标，立即调用 MineHarvest.TryBegin。MineHarvest 在 TreeHarvest 后、PlayerRespawn 前维护计时/取消/完成。

- 从 Connected、NetworkStreamInGame、没有 RequestDisconnect 的连接读取 CommandTarget/NetworkId，要求网络玩家、启用 Simulate 及 GhostOwner 归属匹配。
- 玩家存活、Move 有限且为零、无 Attack 请求且近战 Ready 才合格；即使位移被障碍挡住，非零 Move 仍拒绝/取消。
- 玩家同时只持有一种资源预约，已有交互期间 F 不重置、不切换；旧 F/H 优先判断移除。G/E/R 仍独立，R 不补发死亡时的 F。
- 统一入口按各类型配置的 X/Z 交互距离筛选三类最近可用目标，矿点须 Available；精确同距取小 PlacementIndex，请求按 NetworkId 升序且立即预约，已占用点不覆盖，下一玩家可取其他有效目标。当前矿点距离 2 米。
- 预约保存Collector、当前HitSequence、ToolKind、ActualDuration及FinishAt=模拟时间+锁定耗时（徒手3秒，镐子默认2.25秒），状态 Mining、CollectorNetworkId=该玩家。重复 F 不重置期限；持续按住不自动连续采矿。
- 每次更新核对在线/归属、生命、Move/攻击、HitSequence 和距离。移动/攻击/受击/死亡/超距/在线或归属失效均取消，清空进度/再生期限、恢复 Available/CollectorNetworkId=0/MinedTick=0，不创建石材、不解除阻挡；已有轮次历史保持，重新 F 也不清空历史。
- 统一入口在资源系统更新前记录已有交互，完成/取消玩家本 tick 不再预约，后续须新 F。启动失败只清理该玩家部分预约，不自动转选另一目标；日志保留阶段/地图/类型/布置/玩家及原异常，清理错误单独记录，继续其他请求。

## 【CURRENT STRATEGY】完成、掉落与回滚

到期仍合格时，MineHarvest 保存原障碍值与历史长度，调用原 DropSpawnSystem.SpawnOwnedDrop，传入明确 map source、石材 Prefab/资源键、stone/3 与矿点起点。DropSpawnUtility 完整初始化 Transform/DropState/DropProgress，并登记共享掉落所有权后才返回；沿原统一 DropId 递增、不复用，可有失败空号。Instantiate 后重新获取所需组件/缓冲，先准备历史容量及全部提交引用；使用镐子时先保存扣耐久候选，成功后再提交耐久并结束预约并写入 RegrowAt=启用时的当次 Server World 模拟时间+间隔（关闭为 0）、追加当前权威 tick 的 Disabled=1 历史、设置对应障碍 Disabled=1，提交 Depleted/CollectorNetworkId=0/MinedTick=当前权威 tick。只有完整掉落与矿点提交成功才保留期限；采矿完成不直接改库存/金币/经验；徒手不新增保存，使用镐子时保存耐久，规则归工具专题。

保存前生成/准备/保存失败只释放本次石材，尝试恢复原障碍与历史长度并取消到 Available、清空期限；既往轮次历史保留，不安排失败项再生。记录原异常、当前 DropId/物品/资源及独立清理/回滚错误。若回滚失败，日志暴露实际错误，不宣称恢复成功；耐久保存成功后不执行此回滚，意外ECS提交故障暴露durabilitySaved=true，恢复保证为UNKNOWN；其他矿点/玩家继续，不自动补发或重试。无法实际触发的运行失败分支为 UNKNOWN。

石材复用 drops 的 0.4 秒飞行、0.6 米散落/弧高、0.05 米贴地、0.5 缩放、600 秒寿命和 2 米 G 拾取距离。drops.enabled 只关闭敌人额外掉落，mining.enabled 独立；没有落点物理碰撞或避障。DropPickup 按实际 ItemId 解析石材并合并同名库存，PrepareReward → SavePrepared 成功才提交库存及 Consumed；失败保留旧正式档、库存数量和未到期掉落，恢复后须新 G。G候选保留当前Tools、沿玩家v2保存并兼容读取v1，已入包石材随固定ID恢复，矿点耗尽/进度及地面掉落只保留本局。

## 【CURRENT STRATEGY】原点再生与占位等待

MineRegrow 在仅服务端的 PredictedSimulation、MineHarvest 之后和 PlayerRespawn 之前运行；mining.enabled 与所选矿点 regrowEnabled 均开启才处理。只有 Depleted 且 RegrowAt>0、当前模拟时间达到期限的点参与；不使用客户端时间或系统墙钟，G 拾取石材不启动、重置或推迟期限。

有到期点时收集全部存活 PlayerNetCode/PlayerHealth 和 EnemyState 的 X/Z 位置，包含未启用 Simulate 的存活玩家；IsDead!=0 不占位。角色位置非有限时记录具体错误并停止该次占位批次。原障碍圆中心到角色中心的距离小于等于“矿点 footprintRadius+角色半径+collisionSkin”即占位；当前玩家为 0.75+0.4+0.01=1.16 米，敌人为 0.75+0.45+0.01=1.21 米。占位时保持 Depleted、隐藏和解除阻挡，保留原到期时间，后续更新继续检查；不推开角色、不移动矿点或重置倒计时。

原点空闲时先准备历史容量，再追加当前权威 tick 的 Disabled=0，清空 MineProgress（含 RegrowAt）、恢复 Available/CollectorNetworkId=0/MinedTick=0 及对应障碍。原 Ghost/布置/位置/朝向不变，显示由原 MineRender 恢复。再生本身不生成石材、不改库存或写盘；之后必须新 F 预约，继续基础徒手3秒、可用镐子2.25秒、stone×3及原G保存链。

各到期矿点独立隔离错误；恢复失败尝试还原耗尽状态、原期限、障碍与历史长度，保留原异常，回滚失败另记具体错误，其他条目继续。成功回滚后可在之后更新重新检查，不宣称回滚失败已恢复。取消/失败采矿没有再生期限。再生开关关闭时矿点保持本局耗尽，mining.enabled=false 时无矿点；开关和参数须 PlayMode 前配置并完成烘焙。

## 【CURRENT STRATEGY】预测阻挡与清理

MineObstacle 在 Client/Server 的 PredictedSimulation、TreeObstacle 之前执行，后者再先于玩家/敌人移动。按地图源建立 Mineable 的 PlacementIndex→障碍索引；每个预测 tick 先恢复这些记录的初始 Disabled=0。对 Available/Mining/Depleted 均从历史末尾查找当前 ServerTick 严格晚于的最近有效转换，再应用该条 Disabled；没有匹配转换或历史为空时保持初始阻挡。耗尽/再生在移动后提交，从下一模拟 tick 生效；回放到转换前或两次转换间按历史恢复，不能只用最新 Phase/MinedTick。仅处理 Mineable 索引；缺索引、缺历史、无效 tick/Disabled 记录具体错误并隔离当前项。每完整轮次增加两条，本局全部历史不截断；取消/新 F 不清空，随矿点实例释放。系统停止恢复本系统索引的初始阻挡，MineRegrow 只清理自己的索引缓存。

矿点由 MineSpawn 在地图根更换/失效、停止及 World 销毁时清理本系统拥有的实体；石材由原 DropSpawn/DropCleanup 清理。共享网格/材质/Prefab 不随实例销毁。世界资源档归独立服务；没有运行中地图切换、手动工具装备、采矿动画、物理碰撞或石材使用效果；F 目标提示/进度归[交互显示](MapInteractionHud.md)；E 仍只使用小块肉。

## 【FACT】资源与 Editor 边界

| 键/类型 | 路径 | Unity GUID |
|---|---|---|
| 网格 | [MineableRock.asset](../../Assets/Art/Map/CombatPrototype/Meshes/MineableRock.asset) | 5f86aae15e7fddf4c958787664bfb898 |
| 灰色材质 | [MineableRock.mat](../../Assets/Art/Map/CombatPrototype/Materials/MineableRock.mat) | 9414f076780a3f549ab7ce373530381c |
| mine_rock | [MineableRock.prefab](../../Assets/Prefabs/CombatPrototype/Map/MineableRock.prefab) | 6a4118ae4b4c6334983e718cf8c791e5 |
| drop_stone | [DroppedStone.prefab](../../Assets/Prefabs/CombatPrototype/Map/DroppedStone.prefab) | 5c046f7757439c444b83563690bb15b4 |

程序占位网格高 1.2 米、底部 Y=0、最大半径 0.75 米，38 顶点/72 三角形，数据检查非退化且朝外。两个 Prefab 共用新网格/材质，均为单根 Transform/MeshFilter/MeshRenderer/LinkedEntityGroupAuthoring/GhostAuthoringComponent/对应 Authoring，无子节点、Owner、AutoCommandTarget、Collider 或 Animator，Ghost 仅插值、Dynamic。

Tools/CombatPrototype/地图/生成第九阶段采矿资源 要求空闲 EditMode、干净场景、原目录及 GroundRock 材质的 Shader，只创建四个明确新路径，已有资源或 meta 时拒绝覆盖。逐项记录创建/清理失败；使用临时场景并恢复原活动场景。绑定第九阶段采矿资源要求干净的原[网络 SubScene](../../Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity)、唯一 CombatPrototypeNetworkRoot、原 MapAuthoring/Spawner；检查两种模板和完整资源键后仅追加 mine_rock/drop_stone 两项，拒绝重复绑定。全部新 meta 由 Unity 导入生成；旧资源/旧 meta、主 Scene、根/挂载关系、玩家/敌人 Prefab、Animator、包与构建设置保持。

## 【KNOWN ISSUES】验收边界

第九阶段静态核对时正常 Unity 编译无 C# Error，MineState Serializer/Snapshot、原 J 命令类型和系统顺序已核对。Forest/Grassland 的 Json/BuiltIn 各完成一次隔离 Editor 烘焙，另各完成 mining.enabled=false 的 Json 烘焙，共六次；原树木/采集点位置与朝向、保护区域、占地/间距和显式资源引用通过。默认森林/草原为树木 89/53、采集点 36/38、矿点 20/18、阻挡 109/71；关闭采矿后旧四类数量分别为 [598,17,89,36] / [746,22,53,38]，与第八阶段一致。默认启用时草丛/碎石分别为 601/19 与 744/19，空间违规为 0。烘焙前后 Console 均为 [0 Error,2 Warning,7 Log]，未清空日志、未新增烘焙警告；未进入 PlayMode，主场景干净，无临时 World 遗留。

主线程代码/资源静态验收通过；第九阶段 J 实际计时/取消/F-H 优先/争抢、跨端显示与阻挡预测/晚加入、石材 G/保存失败/恢复、运行创建/清理/回滚及开关回归的人工 GamePlayer 为 UNKNOWN，完整清单归[运行入口](Runtime.md)。第八阶段用户通过仍限 v5/revision=7 八项清单，第七阶段仍限 v5/revision=6 十项清单，其余既有通过与第二阶段独立 JSON UNKNOWN 保持。同步写盘耗时、规模性能、平台构建与线上联调未验收；AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

统一 F 的正常 Unity 编译/启动入口已核对；用户已确认人工 GamePlayer 通过，结论限[运行入口](Runtime.md)统一 F 八项清单及 v6/revision=8，未实际触发的边界/同 tick/失败/预测用例仍为 UNKNOWN。旧 J 清单保留第九阶段原口径，不作为当前按键说明；该通过不覆盖矿点再生阶段。

矿点原点再生的正常 Unity 编译无 C# Error，MineState 与 MineBlockingEvent Ghost Serializer/Snapshot、仅服务端再生字段及系统顺序已静态核对。Forest/Grassland 的 Json/BuiltIn 各一次，另各一次 Json mining=false 和 regrowEnabled=false（间隔仍为 600），共八次隔离 Editor 烘焙；schemaVersion=6/configRevision=9、默认 true/600、Prefab 空历史/零期限、资源键与全部初始布置位置/朝向一致。默认森林/草原仍为矿点 20/18、阻挡 109/71、树木 89/53、采集点 36/38；关闭采矿为 0 矿点、89/53 阻挡，关闭再生保持启用矿点布局。占地/间距/保护区/敌人初始重叠违规为 0，Console 前后均 [0 Error,3 Warning,7 Log]，无新增烘焙警告，原场景干净，无临时 World 遗留。

用户已确认矿点原点再生人工 GamePlayer 验收通过，主线程结合既有代码/烘焙静态核对与用户反馈判定该阶段通过；范围限 schemaVersion=6/configRevision=9 及[运行入口](Runtime.md)矿点再生八项清单。人工结论来自用户反馈，未实际触发的精确距离/特殊 Simulate、同 tick/延迟/预测回放、晚加入及独立失败分支仍为 UNKNOWN。已有统一 F 及第七/第八阶段用户通过仅限各自原版本/清单，不扩展到新行为。矿点历史随本局轮次增长，其内存/网络开销及规模性能、平台构建与线上联调未测量；AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

当前自动镐子/耐久归[采集工具](MapGatherTools.md)，v8/revision=11锁定字段/保存链已编译和静态烘焙核对，用户确认工具人工通过限v8/revision=11及[运行入口](Runtime.md)十二项清单，未实际触发的独立失败/边界/预测用例仍为UNKNOWN；旧采矿/再生用户通过仅限各自原版本与清单。

v9/revision12阶段的[材料面板](MapInventoryPanel.md)只展示石材库存/镐子耐久和原配方，按钮沿原制作链；采矿/石材/阻挡/再生逻辑保持，用户确认面板人工通过限[运行入口](Runtime.md)v9/revision12十二项，未触发的独立显示/输入失败仍UNKNOWN。

当前地图v17/20必填[背包丢弃](MapInventoryDrop.md)，复用原掉落资源及保存链；本专题原交互/工具/产出/再生行为保持。新增丢弃静态及用户人工通过限[运行入口](Runtime.md)v10/13十二项，未触发用例UNKNOWN；旧通过仍限原版本/清单。

v11/14阶段的[G提示](MapPickupHud.md)只读共用掉落目标，与原F目标/工具进度独立；不修改本专题资源状态、产出、工具耐久、再生或保存。新显示编译/十次隔离烘焙静态通过；用户确认人工通过限v11/14十项，未触发用例UNKNOWN，旧用户通过保持各自版本/清单。

v12/15的[高亮](MapInteractionHighlight.md)按原Kind/PlacementIndex解析矿点客户端Ghost，Ready黄圈、Working绿圈，默认半径0.9米；读取原Available/Mining/Depleted及CollectorNetworkId，不新增资源状态或倒计时。采矿/掉落/阻挡/再生及工具保存链保持，高亮静态及用户人工通过限v12/15十项，未触发独立用例UNKNOWN。

v13/16的[资源状态](MapResourceStatusHud.md)只读Available/Mining/Depleted与原服务端RegrowAt；到期仍Depleted显示等待再生，实际恢复后才显示可用；mining关闭时不选择矿点。原采矿/掉落/占位/阻挡及保存行为保持。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧通过保持原范围。

当前v17/20的[工具修理](MapToolRepair.md)：Busy期间拒绝修理；原已锁定镐子、ActualDuration/FinishAt、成功扣耐久、地面stone×3、600秒再生/占位/阻挡与保存链保持。新链静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN，旧矿点用户通过保持原版本/清单。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持，主线程静态通过、新人工UNKNOWN；旧用户通过仍限原版本/清单。
