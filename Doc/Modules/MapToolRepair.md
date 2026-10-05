# 采集工具修理与耐久恢复

返回[战斗地图](Map.md)、[采集工具](MapGatherTools.md)与[材料面板](MapInventoryPanel.md)。本专题负责CombatPrototypeNetCode的两种工具修理、3/4输入、配方预览及所属结果；人工清单归[运行入口](Runtime.md)，工具槽与v2存档仍归原模块。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [修理数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairData.cs) | Result枚举及所属Sequence/Kind/Result三字段 |
| [服务端结算](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairSystem.cs) | 原资格/预约检查、材料与耐久候选、保存后提交 |
| [反馈缓存](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairFeedbackClient.cs) | 所属结果文案、显示期限及非法快照隔离 |
| [面板修理](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairPanel.cs) | 两配方/缺口/恢复预览、按钮与一次性待提交请求 |
| [配置](../../Assets/Scripts/CombatPrototype/Map/MapGatherToolsConfig.cs) / [工具烘焙数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolData.cs) | 原gatherTools及Settings/Definitions追加修理字段 |
| [面板配置](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs) / [面板数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 三个新文案字段 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 原地图根新增字段值，原玩家实体追加零修理反馈 |
| [输入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) / [宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) / [原面板](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 3/4与按钮合并、所属显示及生命周期释放 |
| [原丢弃](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryDropSystem.cs) | HasPriorOperation追加两个修理请求 |
| [原SaveStore](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) | 复用PrepareToolCraft候选投影及SavePrepared；该文件未修改 |

四个新脚本的meta由Unity正常导入生成。两个客户端类均为普通C#类，委托原Main Camera HUD/滚动面板；本阶段不修改Scene/SubScene/Prefab/Animator结构、旧meta、资源引用、字体/图片/材质、包或构建配置。执行期间工作区另有敌人动画脚本和Prefab等并行差异，保留且不纳入修理阶段验收。

## 【FACT】当前配置契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为schemaVersion=19/configRevision=22。resourcePersistence/geometry/layout/movement/drops/treeHarvest/mining/gatherTools/interactionHud/pickupHud/interactionHighlight/resourceStatusHud/worldSaveHud/inventoryPanel/inventoryDrop/population/spawn均必填；新增字段沿原严格UTF-8、对象形状、缺失/未知/重复字段与标量类型校验。旧v1～v18明确拒绝，不迁移、补默认或回退来源；正常导入/烘焙后生效，无热重载。

| 配置位置/字段 | 默认值 | 校验或行为 |
|---|---|---|
| gatherTools.repairEnabled | true | 布尔值；修理同时要求原enabled=true |
| gatherTools.repairFeedbackSeconds | 2.0 | 有限正数，只控制反馈显示期限 |
| tools[stone_axe].repairDurability | 20 | 正整数，<=本工具maxDurability |
| tools[stone_pickaxe].repairDurability | 15 | 同上 |
| 两工具repairWoodQuantity | 1 | 非负整数 |
| 两工具repairStoneQuantity | 1 | 非负整数，两类材料总成本>0 |
| inventoryPanel.repairLabel | Repair | 非空白、无控制字符、最多61 UTF-8字节 |
| inventoryPanel.repairButtonLabel | Repair | 同上 |
| inventoryPanel.fullDurabilityLabel | Full durability | 同上 |

gatherTools根当前5字段，tools每条11字段；共新增2个根字段及每工具3字段。inventoryPanel由25字段增至28字段，20个文案均必填。关闭修理、工具或面板仍校验全部字段；不以开关补参数。0木材或0石材成本合法，但两者不可同时为0。工具ID、槽数、最大耐久60/40、制作配方木3石2/木2石3、成功消耗1/工具倍率0.75、原F/B/高亮默认值、三类600秒再生及地图空间/种子/32敌人/出生保持。

## 【CURRENT STRATEGY】服务端资格与事务

RepairSystem在原CraftSystem之后、InventoryDrop/统一F入口/PlayerRespawn之前执行；因此继承原制作位于PlayerDamage/DropCleanup之后的顺序。只收集Connected、InGame、无断线请求、CommandTarget指向网络玩家且启用Simulate的请求。3/4同时设置时只处理斧头。复用原F服务的所有权、存活、有限且为零的Move、无Attack输入及近战Ready资格；CommandTarget所有权不符不写真正所属玩家反馈。

同tick有F/G/E/R/CraftAxe/CraftPickaxe任一请求时返回ExistingOperationHasPriority；此检查不表示这些原业务一定成功，也不改变原业务之间的优先关系。存在任意植物Collecting、树Chopping或矿Mining预约时返回Busy；工具总开关或修理开关关闭返回Disabled。Drop沿原HasPriorOperation检测两个修理请求并拒绝，即使修理最终也被拒绝，同tick不改为执行Drop。

使用原工具定义与玩家唯一工具缓冲，未拥有返回NotOwned；当前等于maxDurability返回AlreadyFull；木材或石材不足返回InsufficientMaterials。耐久0的已持有工具允许修复，修理不创建新工具、不覆盖其他槽。

实际恢复量=min(repairDurability,maxDurability-当前耐久)，恢复后<=最大值；近满耐久仍扣完整配方。默认斧头40→60、59→60、0→20，镐子25→40、39→40、0→15；每次木1石1。恢复立即发生，没有修理工作阶段、进度或工具使用计时变化。

先读取所有必需引用、材料索引/数量、工具槽、当前身份/金币/经验；生成扣料与新耐久候选，复用原PrepareToolCraft投影能力保存完整Items/Tools，调用SavePrepared。该方法名称仍为ToolCraft，但本次只使用它替换既有工具槽和两类材料数量的能力；不调用原CraftSystem或赠送新槽。保存成功后才无结构变更/新增分配提交材料和工具，零材料按高索引到低索引移除，最后写Success反馈。其他库存、工具、金币/经验保持。

读取、准备或保存失败按当前连接隔离，记录stage/map/NetworkId/player/tool/原异常，写Failed且继续其他请求；保存前不修改原库存/耐久，不自动重试。保存成功后的意外ECS提交异常记录saved=true；不以旧档补偿已成功保存的候选，文件系统与ECS之间的意外故障恢复保证仍UNKNOWN。原SaveStore、制作/工具使用结算、玩家准入与保存格式均未修改。

## 【FACT】输入、同步与持久化

CombatPrototypePlayerInput新增RepairAxe/RepairPickaxe两个InputEvent，共15实例字段；数字3/4单次按下与B按钮布尔请求合并，按住不连续修理。原13字段及F/G/1/2语义保持。修理反馈CombatPrototypeMapToolRepairFeedback仅SendToOwner同步Sequence(uint)、Kind(byte工具枚举)、Result(byte结果枚举)，Baker初始0/None/None；不是库存或耐久真值。

Result依次为None、Success、Disabled、NotOwned、AlreadyFull、InsufficientMaterials、Busy、ExistingOperationHasPriority、PlayerUnavailable、Failed。原工具缓冲仍唯一同步ToolId/Durability，原制作/丢弃反馈、F/资源状态各四字段及G六字段保持；无新RPC或协商协议。输入和玩家Ghost烘焙布局变化，各端必须同版代码/配置并重新烘焙。

玩家存档仍写v2、读v1迁移，路径、UTF-8、原子文件替换与Tools[ToolId/Durability]契约保持；修理只保存原材料/工具字段，不保存反馈、面板或输入。重连/重启沿原加载恢复修理后的Tools，死亡/R不补满。

## 【CURRENT STRATEGY】面板、反馈与生命周期

原面板在制作区之后新增11滚动行：一个Repair标题，每工具五行（名称/状态、当前→恢复后与实际增量、材料现有/需要、材料缺口、3/4按钮）。面板尺寸380×640、字号18/行高32及鼠标隔离保持；按原库存投影与工具快照变化缓存文本，不创建另一份可变库存。满耐久、未持有、材料不足、修理/工具关闭或原库存展示快照非法时按钮不可用；Ready与恢复量仅为客户端预览，最终资格/扣料由服务器判断。

按钮沿原有效绑定/可见面板鼠标按下标记，缓存请求由原输入系统再次核对地图、所属连接与存活玩家后消费一次。关闭面板、未就绪、死亡/断线、玩家/地图源变化、World/Scene停止与释放清未提交修理请求；已提交命令由服务端决定，不以关闭面板撤销。关闭inventoryPanel只关闭B/按钮，3/4仍可使用；gatherTools或repairEnabled关闭由服务端拒绝。

反馈缓存只在所属Sequence变化时显示默认2秒，初次绑定只观察当前Sequence，不重播旧结果；使用客户端unscaledTime只决定文案期限，不恢复耐久或结算修理。无F目标时沿原F面板显示结果/对应工具，交互中保留原F第一行并在第二行显示结果；当前未到期修理结果优先于制作结果，B页脚仍先显示未到期丢弃结果。F文字关闭而B开启仍可看到修理反馈，全部显示关闭不关闭快捷键玩法。

非法修理网络反馈在新缓存边界记录地图/Sequence/Kind/Result与原异常，只清修理反馈，原制作/F/G/B/圆环/资源状态保持；Configure/Reset清文案、观察序号、期限与工具身份。没有本地补耐久、按键自动重试、客户端保存或新的GameObject组件挂载。

## 【KNOWN ISSUES】静态证据与人工边界

修理v14/17阶段正常Unity编译无C# Error，新所属Ghost Serializer已生成；3反馈字段/SendToOwner、15输入、F/G/资源状态各4、工具定义11、工具Settings4、面板配置28经静态反射核对。Forest/Grassland各9次隔离Editor烘焙共18次：Json默认、BuiltIn默认、修理关闭、工具关闭、面板关闭、F文字关闭、全部显示关闭、采矿关闭及自定义修理值/文案（单类材料为0）。两来源等价，4个工具Settings/两条11字段定义/全部28面板设置、玩家零修理/制作反馈和空工具、原F/G/资源状态初值、掉落Prepared/Prefab与完整配置对应布局核对通过。默认树89/53、采集36/38、矿20/18、阻挡109/71保持；烘焙Console前后均[0 Error,8 Warning,47 Log]，主场景干净，临时World/Scene/TextAsset释放。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定阶段通过；范围限CombatPrototypeNetCode、schemaVersion=14/configRevision=17及[运行入口](Runtime.md)十二项清单。人工结论来自用户反馈；旧资源状态v13/16十项、高亮v12/15十项、G文字v11/14十项、丢弃v10/13十二项、面板v9/12十二项、工具v8/11十二项及更早用户通过保持原版本/清单。未实际触发的独立资格/同tick/延迟/预测、配置/快照/保存/提交/恢复、多玩家/晚加入/生命周期分支，以及未覆盖字形/排版、性能/带宽、平台/线上与文件替换后意外ECS故障恢复仍UNKNOWN。AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 【FACT】地图资源存档接入边界

当前地图另接[资源存档](MapResourcePersistence.md)，原修理先保存后扣料/耐久及所属结果保持，当前16输入的SaveWorld归[F5](MapWorldSaveHud.md)；世界档不保存Tools或修理反馈。修理用户通过限v14/17十二项，资源存档人工通过限v15/18十二项，未触发用例UNKNOWN。 掉落恢复及同文件快照归[掉落存档](MapDropPersistence.md)，人工通过限v16/19十二项，未触发用例UNKNOWN。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：当前16输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。
