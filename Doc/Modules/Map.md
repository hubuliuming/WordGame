# 战斗地图

## 【FACT】范围与真实入口

本模块属于独立 NetCode 战斗原型。地图入口是 [CombatPrototypeMapAuthoring.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs)，挂载于 [网络 SubScene](../../Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity) 原有唯一 CombatPrototypeNetworkRoot，与原 CombatPrototypePlayerSpawnerAuthoring 同对象；没有新增 Scene 根节点、地图 Ghost 或场景内玩家/敌人实例。

进入 PlayMode 前，在该组件的 Preset 中选择 Grassland 或 Forest，保存 SubScene。当前序列化选择为 Forest、SourceMode=Json，五份 JSON 已显式绑定。两种模板共用格子、分块、资源绑定与生成流程；草地、林地、岩地是地表类型，grassland、forest、rocky 是生态类型，生态边界独立于区块边界。

| 模板 | mapDefinitionId | 默认生态 | 归一化生态区域，按顺序覆盖 |
|---|---|---|---|
| Grassland | battle_grassland_01 | grassland | forest：(0,0.82)～(1,1)；forest：(0,0)～(0.12,0.82)；rocky：(0.8,0)～(1,0.4) |
| Forest | battle_forest_01 | forest | grassland：(0.3,0.3)～(0.7,0.7)；rocky：(0.75,0)～(1,0.35) |

归一化坐标从地图最小 X/Z 角起算，范围为 0～1；区域使用最小边含、最大边不含的矩形规则。地图中心为世界 X/Z 原点，区块和格子从地图最小角沿正 X/Z 展开，物体使用连续坐标。

## 【FACT】当前默认配置

v37/40；[收藏提示](MapInventoryFavoritesConsumptionHint.md)待验收；旧通过限原版本/清单。

| 配置字段 | 当前值与用途 |
|---|---|
| schemaVersion / configRevision | 37 / 40 |
| inventoryCapacity | 默认总量300、单种200，完整契约归[容量](MapInventoryCapacity.md) |
| defaultSeed | 12345，用于确定性布置 |
| geometry.cellSizeMeters | 2 米 |
| geometry.cellsPerChunk | 每块单边 16 格，即 32×32 米 |
| geometry.chunkCountX / chunkCountZ | 3 / 3，共 9 块、2304 格、96×96 米 |
| geometry.baseHeightMeters | 0；地图范围 X/Z 均为 -48～48 |
| layout.mainPathGroundId | grass |
| layout.mainPathWidthMeters / minimumPathWidthMeters | 4 / 2 米；当前生成两条贯穿出生中心的十字道路，最小宽度还须容纳最大角色直径及两侧碰撞留缝 |
| layout.edgeKeepoutMeters | 2 米，限制物体初始布置与敌人初始位置 |
| layout.spawnSafeRadiusMeters / combatClearRadiusMeters | 6 / 8 米，以玩家出生区中心排除物体布置；不提供无敌保护 |
| layout.enemySpawnMinDistanceMeters | 12 米，校验每个敌人初始点到玩家出生区中心的距离 |
| population.initialEnemyCount | 全图 32 个，由地图提供总量 |
| spawn.playerOriginX / playerOriginZ / playerSpacingMeters | 0 / 0 / 2 |
| spawn.actorHeightOffsetMeters | 1，角色实体 Y=地面基准高度+该偏移 |
| spawn.enemyOriginX / enemyOriginZ | 0 / 16；32 个敌人保留 8 列、间距 3 |

玩家加入和 R 复活均调用 [CombatPrototypeMapSpawnUtility.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapSpawnUtility.cs)：玩家出生区中心加 (NetworkId×playerSpacingMeters,0,0)。当前实际位置仍为 (NetworkId×2,1,0)。复活仍使用同一 Ghost；生命、体力、敌人旧锁定清理、身份与存档规则归 [玩家](Player.md)、[战斗](Combat.md)。

Spawner 的 EnemyColumns、EnemySpacing 保留现有序列化值并参与敌人网格校验。原 EnemyPosition、EnemyCount 字段名和序列化值兼容保留，当前 Baker 使用地图配置生成 ECS 中的 EnemyPosition、EnemyCount；这两个旧 Inspector 字段不再控制当前敌人原点与总量。首个敌人位置为 (0,1,16)，最后一个为 (21,1,25)。

| 生态 | 地表 | 草丛组 /100㎡ | 树木 /100㎡ | 采集物 /100㎡ | 静态碎石 /100㎡ | 矿点 /100㎡ |
|---|---|---:|---:|---:|---:|---:|
| grassland | grass | 12 | 0.4 | 0.6 | 0.2 | 0.1 |
| forest | forest_floor | 8 | 1.5 | 0.5 | 0.2 | 0.2 |
| rocky | rock | 3 | 0.1 | 0.2 | 1 | 1 |

当前 objects 包含 decor_grass、decor_pebble、tree_normal、gather_apple、mine_rock。草丛占地 0、同类间距 0.5 米；装饰碎石占地 0.3、间距 2.5 米，均不阻挡。mine_rock 独立阻挡，占地 0.75、间距 2.5、交互 2 米，F 采矿 3 秒产出 stone ×3，默认 600 秒原点再生，占位等待。

普通树木的 footprintRadiusMeters=0.5、minimumSameTypeSpacingMeters=3、blocksMovement=true，三种生态通过 treeObjectId=tree_normal 引用。F 使用 interactionDistanceMeters=2，按 treeHarvest 的 2 秒/wood ×3 砍伐；regrowEnabled=true、regrowSeconds=600。普通攻击不破坏树木；gatherDurationSeconds=1 未用于砍伐，gatherable=false、yieldItemId=null、yieldQuantity=0。地表 walkable=true、movementMultiplier=1，尚未接入地表通行或速度计算。

gather_apple 的 footprintRadiusMeters=0.3、minimumSameTypeSpacingMeters=1.5、interactionDistanceMeters=2、gatherDurationSeconds=1、yieldItemId=vitality_apple、yieldQuantity=1，gatherable=true。三种生态通过 gatherObjectId=gather_apple 引用；移动/近战/投射物阻挡关闭，regrowEnabled=true、regrowSeconds=600。当前采集产出 vitality_apple，显式映射既有 Msg.ItemName.活力苹果；当前苹果只入包和保存，E 键仍仅使用小块肉。

## 【FACT】JSON 文件与配置入口

五份UTF-8无BOM JSON，地图v29/32；[存档](MapResourcePersistence.md)含[掉落](MapDropPersistence.md)；v15/18、v16/19各通过十二项。生态含treeObjectId/gatherObjectId/mineObjectId、mineDensityPer100m2，物体含mine_rock；原地表、空间、种子和出生保持。

| MapAuthoring 字段 | 显式绑定文件 | JSON 根类型 |
|---|---|---|
| GrasslandJson | [battle_grassland_01.json](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) | MapDefinitionConfig 对象 |
| ForestJson | [battle_forest_01.json](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json) | MapDefinitionConfig 对象 |
| BiomesJson | [biomes.json](../../Assets/Config/CombatPrototype/Map/biomes.json) | BiomeDefinitionConfig 数组 |
| GroundsJson | [grounds.json](../../Assets/Config/CombatPrototype/Map/grounds.json) | GroundDefinitionConfig 数组 |
| ObjectsJson | [objects.json](../../Assets/Config/CombatPrototype/Map/objects.json) | MapObjectDefinitionConfig 数组 |

SourceMode=BuiltIn 时明确使用内置来源，SourceMode=Json 时使用选中地图 JSON 和三份共享 JSON；Json 来源失败不自动回退 BuiltIn。Preset=Grassland 要求地图 ID 为 battle_grassland_01，Preset=Forest 要求 battle_forest_01。schemaVersion=37为契约版本，configRevision须为正整数；旧v1～v36或缺少必填字段明确失败，不补字段或回退来源。

[MapMovementConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapMovementConfig.cs) 是地图定义的必填 movement 段：

| JSON 字段 | 当前默认值 | 校验约束 |
|---|---:|---|
| movement.playerRadiusMeters | 0.4 米 | 有限正数 |
| movement.enemyRadiusMeters | 0.45 米 | 有限正数 |
| movement.collisionSkinMeters | 0.01 米 | 有限正数，须小于两个角色半径 |
| movement.maxSlideIterations | 3 | 1～8 的整数 |

layout.minimumPathWidthMeters 须不小于 2×(最大角色半径+collisionSkinMeters)。treeObjectId 必须引用已有物体，gatherObjectId 必须引用 gatherable=true 的物体；blocksMovement=true 要求正占地半径。采集物要求正交互距离、时长和产出数量，关闭三类阻挡，并通过产出 ID 白名单。regrowSeconds 须为有限非负数；regrowEnabled=true 时还须大于 0。字段沿用统一形状与语义校验。

[JSON 读取器](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapJsonReader.cs) 使用 TextAsset.bytes 严格 UTF-8 解码，支持 UTF-8 BOM；按现有 DTO 公共字段检查缺失/未知/重复字段、对象/数组形状和标量类型。全部字段必填，布尔与数值保留字段也须显式写出；仅对象 yieldItemId 可为 null，gatherable=true 时仍必须通过产出 ID 校验。整数必须为 32 位整数，数字字符串不转换；浮点字段接受整数或有限浮点数，不接受超出 float 范围的数值。解析及形状错误包含文件路径和 JSON 字段路径。

[JSON 来源](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeJsonMapConfigSource.cs) 组装 CombatMapConfigSet，检查地图 ID 与选中模板匹配，并复用原配置校验器检查版本、数值范围、ID/引用和区域；语义错误保留四份输入文件路径及原校验原因。资源键仍须对应既有 GroundMaterials/DecorationPrefabs 的显式绑定，不从 JSON 查找或创建 Unity 资源；未绑定键的错误包含对应配置文件、条目 ID 与 visualResourceKey 字段。

修改 JSON 后须等 Unity 完成导入及正常烘焙再进入 PlayMode；切换 Preset 或 SourceMode 时须先保存 SubScene。Map Baker 与 Spawner Baker 都在读取 JSON 前登记选中地图文件和三份共享文件的 DependsOn，确保文件内容变化参与两条烘焙依赖。运行期间使用固定的烘焙数据快照。

## 【CURRENT STRATEGY】配置、烘焙与运行链

[ICombatMapConfigSource.cs](../../Assets/Scripts/CombatPrototype/Map/ICombatMapConfigSource.cs) 定义 LoadValidated(string mapDefinitionId)，返回 [CombatMapConfigSet.cs](../../Assets/Scripts/CombatPrototype/Map/CombatMapConfigSet.cs)。四类配置为 MapDefinitionConfig、BiomeDefinitionConfig、GroundDefinitionConfig、MapObjectDefinitionConfig，使用可序列化的普通字段、字符串、数值和数组，资源仅存稳定键。

MapAuthoring.LoadMapConfig 是当前配置来源接入点，按 SourceMode 和 Preset 取得模板；内置与 JSON 来源都在返回前通过 [配置校验器](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) 检查必填段、版本、数值范围、ID 唯一性、引用及区域范围。ID 使用小写 ASCII 字母、数字、下划线，最长 61 字符。LayoutBuilder 在烘焙边界结合原 Spawner 的列数/间距检查完整敌人网格和地图边界；缺失配置或绑定明确失败，没有其他模板或资源查找兜底。

地图配置通过TextAsset在烘焙时读取，无外部配置热重载；资源状态存档归独立服务端系统。能力字段包含 footprintRadiusMeters、minimumSameTypeSpacingMeters、blocksMovement、blocksMelee、blocksProjectile、gatherable、interactionDistanceMeters、gatherDurationSeconds、yieldItemId、yieldQuantity、regrowEnabled、regrowSeconds；footprintRadiusMeters 参与布置占地及移动阻挡半径，minimumSameTypeSpacingMeters 控制同类间距，blocksMovement 决定阻挡记录；采集能力、距离、时长、产出及再生字段进入烘焙物体定义，供服务端生成、结算与再生使用；攻击遮挡字段未驱动运行行为。

烘焙得到 [CombatPrototypeMapData.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapData.cs) 中的地图单例和区块、格子、静态物体布置、阻挡、生态、地表材质、Prefab 缓冲。CombatPrototypeMapData 保存两类角色半径、碰撞留缝与滑动次数；CombatPrototypeMapObstacle 保存布置索引、物体索引、X/Z 位置、占地半径和 Disabled 开关。地图不是 Ghost；各 World 使用同一 SubScene 烘焙配置。原准入客户端/服务端、敌人生成与复活系统等待地图数据就绪。

[LayoutBuilder](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapLayoutBuilder.cs) 先生成全图生态、道路及禁止布置格，再跨所有区块依次完成树木、采集物、启用的矿点布置，最后补草丛和碎石。每块各生态按可布置格面积计算期望数量，进行确定性整数取样、候选格洗牌和格内抖动。候选点避开道路、安全区、战斗区、边缘及整个敌人初始网格的矩形；矩形在网格外留半个敌人间距。阻挡物的避让半径额外计入最大角色半径和碰撞留缝。共享空间桶检查同类间距与所有非零占地物体之间的重叠；零占地草丛不参与占地互斥。实际数量可少于期望值，每次类别布置每格最多一个候选。相同配置/种子决定布局；树木/采集物的位置与朝向保持，矿点优先于草丛/碎石，后两类可变化。

[ChunkMeshBuilder](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapChunkMeshBuilder.cs) 每块生成一个网格，地表类型对应子网格。[MapPresentationSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPresentationSystem.cs) 仅在客户端 PresentationSystemGroup、EntitiesGraphicsSystem 前创建地表和静态装饰，跳过 Gatherable/Harvestable/Mineable。砍伐开启时树木由服务端 TreeSpawn 生成插值 Ghost，关闭时仍显示原静态树；草丛/碎石保持静态。采集物由 [GatherSpawnSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherSpawnSystem.cs) 在服务端地图就绪后、准入前实例化为插值 Ghost，根实体更换/失效或系统停止时清理其拥有的实体。服务端原敌人生成链保持。

显示系统在首次取得地图根实体时生成一次；结构变更前复制缓冲，装饰同时初始化 LocalTransform 与当前帧 LocalToWorld。区块、装饰逐项隔离生成失败并清理当前项，缺失整批必要依赖明确终止，日志包含阶段、条目标识和资源键。根实体失效/替换、系统停止及 World 销毁时释放本系统拥有的实体与生成网格；共享材质和 Prefab 不由该系统销毁。第一阶段清理相关人工验收已获用户确认通过，范围见 [运行入口](Runtime.md)。

[采集状态](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherData.cs) 同步 PlacementIndex、Available/Collecting/Depleted 和 CollectorNetworkId，配置与计时组件仅服务端保留。[GatherRenderSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherRenderSystem.cs) 按耗尽状态禁用客户端 MaterialMeshInfo，回到 Available 时恢复；Ghost 保留至本局地图释放，供晚加入接收。采集计时/中断及保存后入包归[背包](Inventory.md)，F 统一选目标见下节，人工范围归[运行入口](Runtime.md)。

Map Baker 将再生开关/间隔写入物体定义，GatherSpawnSystem 复制到服务端采集配置。采集仅在 SavePrepared 成功、提交库存并耗尽时写入 RegrowAt=当次服务端模拟时间+间隔；取消/保存失败清空进度且不安排再生。独立 [GatherRegrowSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherRegrowSystem.cs) 在服务端预测组的采集之后、复活之前检查 Depleted、启用再生且期限已到的点，清空进度/采集者并恢复 Available。位置、布置索引及原 Ghost 保持，不实例化新点、不发物品；原再生系统不写盘，独立资源保存系统记录最终状态。再次采集须有新 F 请求。关闭再生时点保持本局耗尽。期限仅属于当前 Server World，不使用客户端或系统墙钟。

## 【CURRENT STRATEGY】统一 F 资源交互

[统一入口](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs)在服务端预测组的伤害/G 清理后、Gather 前执行；[选择器](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionTargetSelector.cs)跨三类比较。F 单次按下只写已有 Gather；HarvestTree/Mine 保留，H/J 停用，输入布局与 G/E/R 保持。

玩家须在线、归属匹配、启用 Simulate、存活、有限零 Move、无攻击且近战 Ready。按各类型配置距离筛选 Available 采集点、Standing 树和 Available 矿点，关闭功能/占用/耗尽目标排除；比较 X/Z 中心距离平方，精确同距取小 PlacementIndex，无类型优先。请求按 NetworkId 升序，立即预约后再处理下一玩家。

记录本 tick 已交互玩家，重复 F 不重置/切换，完成/取消本 tick 不再启动，须新 F，按住不连续。原系统计时/中断/产出；启动失败仅清理当前预约，记录地图/类型/布置/玩家/阶段与原异常，不转选，其他请求继续。统一 F 用户人工通过，范围见[运行入口](Runtime.md)，未触发用例 UNKNOWN。

## 【CURRENT STRATEGY】静态物体移动阻挡

[CombatPrototypeMapMovementUtility.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMovementUtility.cs) 在 X/Z 平面对完整位移进行圆形扫掠；检测半径为物体占地半径+角色半径+collisionSkinMeters。每次取最早接触点，同时间命中使用稳定的烘焙顺序；把剩余位移的向内分量移除后继续滑动。达到 maxSlideIterations 时舍弃未解决位移。初始已重叠时允许向外或切向移动，阻止继续深入；不主动挤出或传送角色。

[玩家移动](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerMovementSystem.cs) 在 Client/Server 的原预测模拟组中共用该工具，只处理 Simulate 且存活的玩家，保留非有限输入拒绝、长度限制、速度及朝向规则。[敌人移动](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyMovementSystem.cs) 只在服务端调用，最近在线存活目标、前后摇停动及距离 1.5 停止规则保持；碰到障碍可滑动或停止，没有全局路径规划。

两个移动系统都等待地图单例，直接读取现有地图根实体上的只读阻挡缓冲，不实例化 Collider；该移动阻挡链不增加玩家/敌人组件或输入/Ghost 字段。角色 Y 保持；树木/矿点阻挡历史按权威 tick 重建 Disabled，移动工具只处理启用记录。地表 walkable、movementMultiplier 与地图边界尚不参与移动阻挡。不同端仍须使用相同地图配置，未新增联网配置校验协议。

## 【FACT】资源绑定与 Editor 入口

| 稳定键 | 资源路径 |
|---|---|
| ground_grass | Assets/Art/Map/CombatPrototype/Materials/GroundGrass.mat |
| ground_forest | Assets/Art/Map/CombatPrototype/Materials/GroundForest.mat |
| ground_rock | Assets/Art/Map/CombatPrototype/Materials/GroundRock.mat |
| decor_grass | Assets/Prefabs/CombatPrototype/Map/DecorationGrass.prefab |
| decor_pebble | Assets/Prefabs/CombatPrototype/Map/DecorationPebble.prefab |
| tree_normal | Assets/Prefabs/CombatPrototype/Map/TreeNormal.prefab |
| gather_apple | Assets/Prefabs/CombatPrototype/Map/GatherApple.prefab |
| drop_apple | Assets/Prefabs/CombatPrototype/Map/DroppedApple.prefab |
| tree_harvest | Assets/Prefabs/CombatPrototype/Map/HarvestableTree.prefab |
| drop_wood | Assets/Prefabs/CombatPrototype/Map/DroppedWood.prefab |

地表使用 URP Lit 材质。静态物体网格位于 Assets/Art/Map/CombatPrototype/Meshes/，草丛另用 DecorationGrass.mat，碎石复用 GroundRock.mat，普通树木使用 TreeNormal.asset 与 TreeNormal.mat。占位树为单网格、单材质，模型高 4 米、最大视觉半径 1.25 米，视觉尺寸与 0.5 米逻辑占地分开。砍伐关闭时保留原三个静态 Prefab，均为单根 Transform/MeshFilter/MeshRenderer，无 Collider、Animator 或 Ghost。采集物使用 GatherApple.asset/.mat/.prefab，网格高 0.9 米、最大视觉半径 0.45 米、36 顶点/48 三角形；单根含 Transform、MeshFilter、MeshRenderer、GhostAuthoringComponent、GatherAuthoring 和 Ghost 必需的 LinkedEntityGroupAuthoring，无子节点、Collider 或 Animator。Ghost 无 Owner/AutoCommandTarget，仅插值；新增 meta 由 Unity 导入生成。

[CombatPrototypeMapAssetBuilder.cs](../../Assets/Scripts/Editor/CombatPrototypeMapAssetBuilder.cs) 提供 Tools/CombatPrototype/地图 下的“生成第一阶段资源”和“绑定第一阶段地图”菜单。资源生成逐项处理明确的新路径，复用同路径同类型资源而不重写；绑定入口要求齐全资源及干净的既有 SubScene，只给原根节点首次添加地图组件，已有组件时明确拒绝重复绑定。资源生成使用临时 Editor 场景，绑定只保存该 SubScene。

[CombatPrototypeMapJsonBinding.cs](../../Assets/Scripts/Editor/CombatPrototypeMapJsonBinding.cs) 提供 Tools/CombatPrototype/地图/绑定第二阶段 JSON 菜单；要求五份已导入的文件及干净的既有 SubScene，先校验两种配置与敌人网格，再修改原 MapAuthoring 的五个引用及 SourceMode=Json。Preset、原材质/装饰绑定、根节点及组件挂载关系保持；只保存该 SubScene，没有创建资源兜底。

[CombatPrototypeMapTreeAssetBuilder.cs](../../Assets/Scripts/Editor/CombatPrototypeMapTreeAssetBuilder.cs) 提供 Tools/CombatPrototype/地图/生成第三阶段树木资源，只处理 TreeNormal.mat、TreeNormal.asset、TreeNormal.prefab 三个明确路径，要求原资源目录齐全；逐项隔离失败，已有同类型资源不重写。[CombatPrototypeMapTreeBinding.cs](../../Assets/Scripts/Editor/CombatPrototypeMapTreeBinding.cs) 提供绑定第三阶段树木入口，要求原根节点、两个组件和干净的 SubScene，校验两种配置与网格后只追加 tree_normal 引用；已绑定时拒绝重复执行。资源生成使用临时 Editor 场景，绑定仅保存原 SubScene。

[CombatPrototypeMapGatherAssetBuilder.cs](../../Assets/Scripts/Editor/CombatPrototypeMapGatherAssetBuilder.cs) 提供“生成第四阶段采集资源”，只处理 GatherApple 的三个新资源路径；[CombatPrototypeMapGatherBinding.cs](../../Assets/Scripts/Editor/CombatPrototypeMapGatherBinding.cs) 提供“绑定第四阶段采集物”，校验干净 SubScene、两个模板及完整网格后仅追加 gather_apple Prefab 引用，拒绝重复绑定。菜单均位于 Tools/CombatPrototype/地图。

原主场景、镜头、玩家/敌人 Ghost 及 PlayerView Prefab、Animator、旧 meta、包与构建设置保持。采集复用网络背包及玩家v2存档（读取v1迁移），不接正式 QFramework Map/UI 链；资源存档开启时恢复耗尽与剩余期限，关闭时按原布局重置；玩家库存仍按固定ID恢复。

## 【KNOWN ISSUES】当前验收边界

脚本、模板隔离烘焙、网格及绑定已有静态核对。用户已确认第一/第三/第四/第五/第六/第七/第八阶段人工通过，仍限[运行入口](Runtime.md)各自原版本/清单；不覆盖统一 F，性能/平台/线上为 UNKNOWN。记录归[本月 ChangeLog](../ChangeLog/ChangeLog_2026-10.md)。

当前没有地图边界碰撞、地形高度、路径规划、攻击遮挡、超出当前掉落范围的动态物体生命周期。角色仍可离开地表范围；旧 NetworkId 连续排列没有新增人数上限或回绕，较大 ID 的出生点可能越界。道路与安全区仅限制初始物体布置，敌人仍可追踪进入，不提供持续安全区行为。敌人可能在树木前停止，当前滑动不保证绕过成片障碍或全图可达。普通树木的显示、阻挡和滑动已获第三阶段人工通过反馈，结论限第三阶段版本与对应清单。

配置/种子没有新增联网校验或同步协议；不同端配置不一致的处理未接入。地图切换只支持 PlayMode 前保存 Preset，运行中切换未接入。AI 未运行逻辑单元测试、GamePlayer/PlayMode、命令行构建、发布、性能采样或图片检查。

第二阶段独立 JSON 人工仍为 UNKNOWN。第七阶段编译/Ghost/资源/隔离烘焙已核对，用户人工通过限 v5/revision=6 十项清单；未触发用例 UNKNOWN，规则归[树木](MapTreeHarvest.md)。

树木成功砍倒后默认 600 秒原点再生，占位则等待，原 Ghost 恢复 Standing/显示/阻挡，新 F 才再次产出。第八阶段 v5/revision=7 编译/历史序列化/烘焙已核对且布局保持；用户通过限八项原清单，未触发用例 UNKNOWN；旧 H 通过不覆盖统一 F。

森林/草原：20/18 矿点、109/71 阻挡，关闭采矿后原布局保持；矿点再生编译/八次隔离烘焙已核对，用户确认人工通过，范围见[运行入口](Runtime.md)/[采矿](MapMining.md)。

HUD通过限v7/10，工具限v8/11、[面板](MapInventoryPanel.md)限v9/12原清单。[丢弃](MapInventoryDrop.md)v10/13静态及用户人工通过限十二项，未触发用例UNKNOWN。

[G提示](MapPickupHud.md)寿命已验收；[F5](MapWorldSaveHud.md)已验收。
