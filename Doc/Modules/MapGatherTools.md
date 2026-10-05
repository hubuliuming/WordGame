# 采集工具、制作与耐久

返回[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的两类工具、数字键制作、F 自动使用、耐久和所属玩家反馈；原目标选择、资源产出与再生分别归地图、[树木](MapTreeHarvest.md)、[采矿](MapMining.md)，完整存档格式归[资源与数据](DataResources.md)，人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [MapGatherToolsConfig](../../Assets/Scripts/CombatPrototype/Map/MapGatherToolsConfig.cs) | gatherTools 及工具定义 JSON DTO |
| [GatherToolData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolData.cs) | 地图配置缓冲、玩家唯一工具缓冲和制作反馈 |
| [GatherToolUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUtility.cs) | 两种稳定 ID、定义读取、开始时选择及成功扣耐久候选 |
| [GatherToolCraftSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolCraftSystem.cs) | 服务端资格、材料/工具候选、保存与制作提交 |
| [PlayerSaveTool](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveTool.cs) | 存档 Tools 的 ToolId/Durability DTO |
| [PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) | 1/2制作、3/4修理的单次InputEvent，修理归[专题](MapToolRepair.md) |
| [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 原玩家实体的空工具缓冲、零制作/修理反馈 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原地图根追加工具 Settings/Definitions |
| [准入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeNetCodeLifecycle.cs) / [SaveStore](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) | 全档校验后恢复工具，v1 读取迁移和 v2 保存候选 |
| [F 入口](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs) / TreeHarvest / MineHarvest | 沿原最近目标预约，锁定工具与实际耗时，完成保存耐久 |
| [HUD](MapInteractionHud.md) | 原 Main Camera 组件增加第二行，读取所属工具/制作反馈 |

新增五个脚本的 meta 由正常 Unity 导入生成；未修改 Scene、Prefab、Animator、旧 meta、图片、字体、包或构建设置。Player Baker 的 ECS 数据与两个新输入字段改变烘焙后的玩家 Ghost/命令布局，各端须使用同版代码、配置及重新烘焙的数据。

## 【FACT】当前 JSON 契约与数值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) 与 BuiltIn 一致为 schemaVersion=17/configRevision=20，必填 gatherTools及[面板配置](MapInventoryPanel.md)。沿原严格 UTF-8、完整字段、类型、未知/重复键校验；旧地图 v1～v16 明确失败，不补默认段或回退来源。配置仅在正常导入/烘焙后生效，无运行热重载。

gatherTools 的 enabled=true、craftFeedbackSeconds=2.0、tools 为恰好两条不重复定义；enabled=false 仍校验所有字段，停止制作、修理和工具加速，但保留已拥有工具/耐久，F 沿原徒手耗时。

| tools 字段 | 斧头 | 镐子 | 契约 |
|---|---|---|---|
| toolId | stone_axe | stone_pickaxe | 仅支持这两个稳定 ID，不重复 |
| displayName | Axe | Pickaxe | 非空白、无控制字符，最多 61 UTF-8 字节 |
| targetKind | tree | mine | 与对应 ID 固定匹配 |
| maxDurability | 60 | 40 | 正整数 |
| durabilityCostPerCompletion | 1 | 1 | 正整数，<= maxDurability |
| durationMultiplier | 0.75 | 0.75 | 有限且 0 < 倍率 <= 1，乘原基础耗时后仍有限正数 |
| craftWoodQuantity | 3 | 2 | 非负整数 |
| craftStoneQuantity | 2 | 3 | 非负整数，两类材料总成本必须 > 0 |

craftFeedbackSeconds 为有限正数。植物仍为徒手 1 秒；树木徒手 2 秒、有可用斧头 1.5 秒；矿点徒手 3 秒、有可用镐子 2.25 秒。工具不增加产出、范围或再生速度，普通攻击不消耗工具。每种工具一个逻辑槽，修理配置/结算归[工具修理](MapToolRepair.md)，无手动装备切换、品质、工具掉落、背包网格或工具使用动画。

## 【CURRENT STRATEGY】制作与材料事务

数字 1 请求斧头，数字 2 请求镐子；[面板](MapInventoryPanel.md)按钮合并到这两个原事件；只对本地启用 GhostOwnerIsLocal 写入一次 InputEvent，按住不连续制作。服务端 CraftSystem 在 PlayerDamage/DropCleanup 后、统一 F 入口与 PlayerRespawn 前执行。Connected、InGame、无断线请求、CommandTarget 指向启用 Simulate 的网络玩家才收集；复用 F 的归属、存活、有限零 Move、无 Attack 请求及近战 Ready 条件。任何 Collecting/Chopping/Mining 预约均拒绝制作。

同一输入 tick 有 F 和制作请求时 F 优先，制作不扣材料；同时有 1/2 时只处理斧头请求。工具不存在或当前耐久小于单次成本才允许制作；仍可用时拒绝，不覆盖/补满。非法归属连接不覆盖真正所属玩家反馈。

材料读取原 CombatPrototypeInventoryItem 的“木材”/“石材”数量，不创建第二份可变库存。先验证全部成本，准备扣料后完整 Items、满耐久工具和必要容量/引用，再调用 PrepareToolCraft → SavePrepared。保存成功后以非结构性写入同时提交材料与工具，数量归零的材料项移除，其余条目顺序保持；失败前旧材料、工具和正式档保持，没有部分扣料或自动重试。新玩家与 v1 迁移玩家不赠工具；制作通过后才获得满耐久。

## 【CURRENT STRATEGY】F 自动使用与成功扣除

F 沿原三类最近目标、各自距离、同距 PlacementIndex、NetworkId 请求排序与资源互斥规则。Tree/Mine TryBegin 按目标类型检查工具开关和对应工具耐久；不足成本或未拥有时直接使用原徒手路径。开始保存 Collector/HitSequence、ToolKind、ActualDuration 和 FinishAt=模拟时间+实际耗时；中途不换工具、目标或耗时。Progress 仍仅服务端，取消/再生清空锁定数据，原资源 Ghost 四字段和阻挡历史不变。

移动、攻击、受击、死亡、超距、断线/归属失效及预约争抢失败不扣耐久。完成时先生成并登记当前掉落，重新取得结构变更后的全部提交引用/缓冲，准备历史容量、阻挡/资源/再生状态；使用工具才投影当前完整金币/经验/库存及扣耐久后的 Tools，调用 PrepareToolUse → SavePrepared。徒手完成不新增工具保存。保存成功后才以预先取得的引用、无结构变更/新增分配提交耐久、资源完成和历史/阻挡；原 wood/stone ×3 地面掉落随后仍须 G 保存入包。

保存前生成、准备或保存失败，释放本次掉落，尝试恢复原障碍/历史长度并取消当前预约；不扣工具、不安排再生，错误保留阶段/地图/布置/玩家/DropId/资源与原异常，清理/回滚失败单独暴露，继续其他条目。SavePrepared 成功后的耐久成本已持久化，不用旧存档补偿，也不执行保存前的资源回滚；意外 ECS 提交异常明确暴露 durabilitySaved=true，跨文件系统与 ECS 的故障恢复保证仍为 UNKNOWN。

最后一次可用耐久允许正常完成，默认 1→0；保留损坏工具记录，下一次 F 恢复徒手；3/4可按[修理配方](MapToolRepair.md)恢复，重新制作仍按原资格覆盖该槽并扣完整制作配方。死亡、R、重连或服务端重启不补满工具。

## 【FACT】工具同步与持久化

CombatPrototypeMapGatherTool 是唯一可变工具状态，内部容量 2，每条 ToolId(FixedString64Bytes)/Durability(int) 以 SendToOwner 同步；制作反馈单独同步 Sequence(uint)、Kind(byte enum)、Result(byte enum)，不作为库存或耐久真值。地图 Settings/Definitions 由原地图根分别烘焙，不由 Ghost 同步。

玩家保存版本为 2，同一固定 ID 路径、UTF-8 无 BOM 写入、临时文件 Flush/正式文件原子替换保持。Tools 必填数组，最多两条，条目恰含 ToolId/Durability；ID 已知且不重复，耐久整数 0～当前配置最大值，损坏 0 合法。未知 ID、缺失/额外/重复字段、错误类型、负值或越界拒绝整个档案，不修正或忽略坏项。

旧 v1 仍严格读取原五字段；金币/经验/原库存保持，内存候选提升为 v2 且 Tools=[]，读取不写盘、不赠工具。下一次正常奖励、E 消耗、F 植物采集、G 拾取、制作、修理或工具完成保存时写 v2；没有批量重写。全部旧保存候选均携带当前 Tools，避免其他业务覆盖工具数据。工具只记录 ToolId/Durability；资源状态、地面掉落、预约/再生期限、HUD 和生命/体力仍不入玩家档案。

## 【CURRENT STRATEGY】HUD 读取与反馈

沿原 HUD 组件，面板当前 320×104、底距48、字号20、进度条10；高度须 >= 2×字号+条高+54。第一行继续显示原 F 文案或权威进度，第二行显示对应工具“名称 当前/最大耐久”；无工具显示 Hands 与 1/2 制作提示，损坏/不足成本显示对应制作提示，植物为 Hands。

制作结果按所属 Sequence 变化显示 2 秒，资源交互时只替换第二行；无 F 目标时可临时显示制作结果和工具状态。结果包含成功、关闭、仍可用、材料不足、资源忙、F 优先、玩家资格不符和失败。仅反馈显示使用客户端 unscaledTime；工作进度仍由服务器锁定的 ActualDuration/FinishAt 生成，不用客户端时间完成。初次绑定只观察当前 Sequence，不重播旧反馈；本地玩家/地图源变化、死亡、无本地 Ghost、停止及 World/Scene 释放清掉缓存和期限。关闭 HUD 不关闭工具玩法；关闭工具时普通工具行显示 Hands。

## 【KNOWN ISSUES】静态核对与人工边界

正常 Unity 编译无 C# Error；五个脚本导入、工具/反馈 SendToOwner Serializer/Snapshot、新输入字段、仅服务端锁定字段与系统声明顺序已静态核对。Forest/Grassland 各覆盖 Json 默认、BuiltIn 默认、Json 关闭采矿、关闭工具、关闭 HUD，共十次隔离 Editor 烘焙；v8/revision=11、全部默认工具值、玩家空工具/零反馈、资源初始零锁定及 HUD 320×104 一致，Json/BuiltIn 一致。全部初始布局位置/朝向和旧资源绑定保持，森林/草原树木89/53、采集点36/38、矿点20/18、阻挡109/71，空间违规为0；关闭采矿仍为原四类布局。烘焙 Console 前后均 [0 Error,2 Warning,0 Log]，无新增警告；原主场景干净，无临时烘焙 World 遗留。

用户已确认本阶段人工 GamePlayer 验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限 CombatPrototypeNetCode、v8/revision=11 和[运行入口](Runtime.md)工具十二项清单。人工结论来自用户反馈；未实际触发的精确边界、同 tick、延迟/预测回放、晚加入及独立配置/创建/准备/保存/提交/清理/回滚失败仍为 UNKNOWN。既有 HUD、矿点再生和其他用户通过仅限各自原版本/清单。同步保存新增频率与耗时、带宽/性能、中文字体/字形、平台构建和线上联调未验证；文件替换成功后的意外 ECS 故障不宣称可完全回滚。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交 Git。

v9/revision12阶段增加本地材料背包/配方与按钮，独立开关和人工边界归[制作面板](MapInventoryPanel.md)。工具资格/事务/反馈及v2保存链保持，旧工具十二项通过仍限v8/revision11。

当前地图v17/20必填[背包丢弃](MapInventoryDrop.md)，复用原掉落资源及保存链；本专题原交互/工具/产出/再生行为保持。新增丢弃静态及用户人工通过限[运行入口](Runtime.md)v10/13十二项，未触发用例UNKNOWN；旧通过仍限原版本/清单。

v11/14阶段的[G提示](MapPickupHud.md)只读共用掉落目标，与原F目标/工具进度独立；不修改本专题资源状态、产出、工具耐久、再生或保存。新显示编译/十次隔离烘焙静态通过；用户确认人工通过限v11/14十项，未触发用例UNKNOWN，旧用户通过保持各自版本/清单。

v12/15的[资源高亮](MapInteractionHighlight.md)复用原F四字段，Working绿色圆环跟随已锁定目标；文字关闭而F高亮开启仍采样。工具锁定耗时、耐久、制作及保存链保持。新显示编译/14次隔离烘焙静态通过，用户确认人工通过限v12/15十项，未触发独立用例UNKNOWN；旧工具/F/G用户通过不扩展。

v13/16的[资源状态](MapResourceStatusHud.md)显示资源阶段及服务端再生秒数；本人采集中优先原F锁定身份，不更改工具锁定耗时、耐久、制作或保存。移动/攻击时状态可显示，但F及制作资格保持。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧工具通过保持原范围。

当前v17/20的[工具修理](MapToolRepair.md)新增3/4和B按钮、所属三字段反馈；复用原唯一工具槽和候选保存，先保存再扣完整配方/提交封顶耐久，损坏0可修复。制作1/2资格与原F工具锁定/耗时/消耗保持。静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN；旧工具及资源状态通过保持原版本/清单。

## 【FACT】地图资源存档接入边界

使用工具的原耐久保存/资源完成顺序保持，新增世界保存位于已提交资源结果之后；世界写失败不回滚工具结算，不保证玩家/世界跨文件原子一致。修理仍限原v14/17通过，资源存档人工通过限v15/18十二项，未触发用例UNKNOWN，见[资源存档](MapResourcePersistence.md)。 掉落恢复及同文件快照归[掉落存档](MapDropPersistence.md)，人工通过限v16/19十二项，未触发用例UNKNOWN。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持，主线程静态通过、新人工UNKNOWN；旧用户通过仍限原版本/清单。
