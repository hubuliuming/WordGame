# 战斗地图

## 【FACT】范围与真实入口

本模块属于独立 NetCode 战斗原型。地图入口是 [CombatPrototypeMapAuthoring.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs)，挂载于 [网络 SubScene](../../Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity) 原有唯一 CombatPrototypeNetworkRoot，与原 CombatPrototypePlayerSpawnerAuthoring 同对象；没有新增 Scene 根节点、地图 Ghost 或场景内玩家/敌人实例。

进入 PlayMode 前，在该组件的 Preset 中选择 Grassland 或 Forest，保存 SubScene。当前序列化选择为 Forest。两种模板共用格子、分块、资源绑定与生成流程；草地、林地、岩地是地表类型，grassland、forest、rocky 是生态类型，生态边界独立于区块边界。

| 模板 | mapDefinitionId | 默认生态 | 归一化生态区域，按顺序覆盖 |
|---|---|---|---|
| Grassland | battle_grassland_01 | grassland | forest：(0,0.82)～(1,1)；forest：(0,0)～(0.12,0.82)；rocky：(0.8,0)～(1,0.4) |
| Forest | battle_forest_01 | forest | grassland：(0.3,0.3)～(0.7,0.7)；rocky：(0.75,0)～(1,0.35) |

归一化坐标从地图最小 X/Z 角起算，范围为 0～1；区域使用最小边含、最大边不含的矩形规则。地图中心为世界 X/Z 原点，区块和格子从地图最小角沿正 X/Z 展开，物体使用连续坐标。

## 【FACT】当前默认配置

默认配置唯一来源为 [CombatPrototypeDefaultMapConfigSource.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)。参数是已确认第一阶段的实际默认值；树木、采集和阻挡相关字段的存在不表示对应行为已接入。

| 配置字段 | 当前值与用途 |
|---|---|
| schemaVersion / configRevision | 1 / 1 |
| defaultSeed | 12345，用于确定性布置 |
| geometry.cellSizeMeters | 2 米 |
| geometry.cellsPerChunk | 每块单边 16 格，即 32×32 米 |
| geometry.chunkCountX / chunkCountZ | 3 / 3，共 9 块、2304 格、96×96 米 |
| geometry.baseHeightMeters | 0；地图范围 X/Z 均为 -48～48 |
| layout.mainPathGroundId | grass |
| layout.mainPathWidthMeters / minimumPathWidthMeters | 4 / 2 米；当前生成两条贯穿出生中心的十字道路，最小宽度字段用于配置校验 |
| layout.edgeKeepoutMeters | 2 米，限制装饰布置与敌人初始位置 |
| layout.spawnSafeRadiusMeters / combatClearRadiusMeters | 6 / 8 米，以玩家出生区中心排除装饰；不提供无敌保护 |
| layout.enemySpawnMinDistanceMeters | 12 米，校验每个敌人初始点到玩家出生区中心的距离 |
| population.initialEnemyCount | 全图 32 个，由地图提供总量 |
| spawn.playerOriginX / playerOriginZ / playerSpacingMeters | 0 / 0 / 2 |
| spawn.actorHeightOffsetMeters | 1，角色实体 Y=地面基准高度+该偏移 |
| spawn.enemyOriginX / enemyOriginZ | 0 / 16；32 个敌人保留 8 列、间距 3 |

玩家加入和 R 复活均调用 [CombatPrototypeMapSpawnUtility.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapSpawnUtility.cs)：玩家出生区中心加 (NetworkId×playerSpacingMeters,0,0)。当前实际位置仍为 (NetworkId×2,1,0)。复活仍使用同一 Ghost；生命、体力、敌人旧锁定清理、身份与存档规则归 [玩家](Player.md)、[战斗](Combat.md)。

Spawner 的 EnemyColumns、EnemySpacing 保留现有序列化值并参与敌人网格校验。原 EnemyPosition、EnemyCount 字段名和序列化值兼容保留，当前 Baker 使用地图配置生成 ECS 中的 EnemyPosition、EnemyCount；这两个旧 Inspector 字段不再控制当前敌人原点与总量。首个敌人位置为 (0,1,16)，最后一个为 (21,1,25)。

| 生态 | 地表 | 草丛组 /100㎡ | 树木 /100㎡，仅预留 | 采集物 /100㎡，仅预留 | 静态碎石 /100㎡ |
|---|---|---:|---:|---:|---:|
| grassland | grass | 12 | 0.4 | 0.6 | 0.2 |
| forest | forest_floor | 8 | 1.5 | 0.5 | 0.2 |
| rocky | rock | 3 | 0.1 | 0.2 | 1 |

当前 objects 仅包含 decor_grass 和 decor_pebble。草丛占地半径为 0、同类最小间距 0.5 米；碎石占地半径为 0.3 米、同类最小间距 2.5 米。碎石是静态装饰，不是建议模板中的占地 0.75 米可采矿点。两种装饰均不阻挡、不产出、不再生；地表 walkable=true、movementMultiplier=1，尚未接入通行或速度计算。

## 【CURRENT STRATEGY】配置、烘焙与运行链

[ICombatMapConfigSource.cs](../../Assets/Scripts/CombatPrototype/Map/ICombatMapConfigSource.cs) 定义 LoadValidated(string mapDefinitionId)，返回 [CombatMapConfigSet.cs](../../Assets/Scripts/CombatPrototype/Map/CombatMapConfigSet.cs)。四类配置为 MapDefinitionConfig、BiomeDefinitionConfig、GroundDefinitionConfig、MapObjectDefinitionConfig，使用可序列化的普通字段、字符串、数值和数组，资源仅存稳定键。

MapAuthoring.LoadMapConfig 是当前配置来源接入点，使用默认来源并按 Preset 取得模板。默认来源在返回前通过 [配置校验器](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) 检查必填段、版本、数值范围、ID 唯一性、引用及区域范围。ID 使用小写 ASCII 字母、数字、下划线，最长 61 字符。LayoutBuilder 在烘焙边界结合原 Spawner 的列数/间距检查完整敌人网格和地图边界；缺失配置或绑定明确失败，没有其他模板或资源查找兜底。

当前没有读取实际地图 JSON 文件、运行热重载、地图状态存档或地图实例持久化。配置来源接口和数据结构已实装，JSON 读取来源、文件路径及平台读取方式仍未接入。能力字段包含 footprintRadiusMeters、minimumSameTypeSpacingMeters、blocksMovement、blocksMelee、blocksProjectile、gatherable、interactionDistanceMeters、gatherDurationSeconds、yieldItemId、yieldQuantity、regrowEnabled、regrowSeconds；这些字段不驱动本阶段采集、碰撞或再生行为。

烘焙得到 [CombatPrototypeMapData.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapData.cs) 中的地图单例和区块、格子、装饰、生态、地表材质、装饰 Prefab 缓冲。地图不是 Ghost；各 World 使用同一 SubScene 烘焙配置。原准入客户端/服务端、敌人生成与复活系统等待地图数据就绪。

[LayoutBuilder](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapLayoutBuilder.cs) 先确定生态、道路及禁止布置格，再按每块各生态的可布置格面积计算期望数量，进行确定性整数取样、候选格洗牌和格内抖动。候选点还须避开道路、安全区、战斗区、边缘及覆盖整个敌人初始网格的矩形；矩形在网格外再留半个敌人间距。物体占地参与避让，同类间距用共享空间桶检查。实际数量可少于期望值，每类装饰每格最多一个。

[ChunkMeshBuilder](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapChunkMeshBuilder.cs) 每块生成一个网格，地表类型对应子网格。[MapPresentationSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPresentationSystem.cs) 仅在客户端 PresentationSystemGroup、EntitiesGraphicsSystem 前运行；每个非空地表子网格创建渲染实体，两种装饰从显式烘焙 Prefab 实例化。服务端保留地图数据并沿原 Spawner/EnemySpawn 链生成敌人；静态装饰没有网络交互或权威状态。

显示系统在首次取得地图根实体时生成一次；结构变更前复制缓冲，装饰同时初始化 LocalTransform 与当前帧 LocalToWorld。区块、装饰逐项隔离生成失败并清理当前项，缺失整批必要依赖明确终止，日志包含阶段、条目标识和资源键。根实体失效/替换、系统停止及 World 销毁时释放本系统拥有的实体与生成网格；共享材质和 Prefab 不由该系统销毁。运行清理效果仍待人工验证。

## 【FACT】资源绑定与 Editor 入口

| 稳定键 | 资源路径 |
|---|---|
| ground_grass | Assets/Art/Map/CombatPrototype/Materials/GroundGrass.mat |
| ground_forest | Assets/Art/Map/CombatPrototype/Materials/GroundForest.mat |
| ground_rock | Assets/Art/Map/CombatPrototype/Materials/GroundRock.mat |
| decor_grass | Assets/Prefabs/CombatPrototype/Map/DecorationGrass.prefab |
| decor_pebble | Assets/Prefabs/CombatPrototype/Map/DecorationPebble.prefab |

地表使用 URP Lit 材质。装饰网格位于 Assets/Art/Map/CombatPrototype/Meshes/，草丛另用 DecorationGrass.mat，碎石复用 GroundRock.mat。两个新 Prefab 均只有一个根对象和 Transform、MeshFilter、MeshRenderer，无 Collider、Animator 或 Ghost；新 meta 由 Unity 导入生成。

[CombatPrototypeMapAssetBuilder.cs](../../Assets/Scripts/Editor/CombatPrototypeMapAssetBuilder.cs) 提供 Tools/CombatPrototype/地图 下的“生成第一阶段资源”和“绑定第一阶段地图”菜单。资源生成逐项处理明确的新路径，复用同路径同类型资源而不重写；绑定入口要求齐全资源及干净的既有 SubScene，只给原根节点首次添加地图组件，已有组件时明确拒绝重复绑定。资源生成使用临时 Editor 场景，绑定只保存该 SubScene。

原主场景、镜头、玩家/敌人 Ghost 及 PlayerView Prefab、Animator、旧 meta、包与构建设置保持。正式 QFramework Map 链、背包、采集物品与玩家存档未接入本模块。

## 【KNOWN ISSUES】当前验收边界

脚本编译、两种模板的隔离 Editor 烘焙、地表网格数据和显式资源引用已静态核对。人工 GamePlayer 的显示、跨端一致性、移动/攻击/死亡/复活、断线重连、模式切换及退出清理保持 UNKNOWN；静态核对不表示渲染运行或性能通过。核对记录只写入 [本月 ChangeLog](../ChangeLog/ChangeLog_2026-10.md)。

当前没有地图边界碰撞、地形高度、寻路避障、攻击遮挡、树木生成、采集、动态物体生命周期或再生。角色可沿原移动逻辑离开地表范围；旧 NetworkId 连续排列没有新增人数上限或回绕，较大 ID 的出生点可能越界。道路与安全区仅限制初始装饰布置，敌人仍可追踪进入，不提供持续安全区行为。

配置/种子没有新增联网校验或同步协议；不同端配置不一致的处理未接入。地图切换只支持 PlayMode 前保存 Preset，运行中切换未接入。未运行逻辑单元测试、GamePlayer/PlayMode、命令行构建、发布、性能采样或图片检查。

人工验收范围归 [运行入口](Runtime.md) 的“战斗地图第一阶段”。
