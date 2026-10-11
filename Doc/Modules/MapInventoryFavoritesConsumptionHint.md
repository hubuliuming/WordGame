# 收藏材料消耗提示

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[收藏](MapInventoryFavorites.md)、[收藏保护](MapInventoryFavoritesDropProtection.md)、[修理](MapToolRepair.md)、[容量升级](MapInventoryCapacityUpgrade.md)、[工具升级](MapGatherToolUpgrade.md)、[配置](DataResources.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode、原B面板；两地图Json/BuiltIn当前v42/revision45。主线程按用户已确认方案完成代码、配置及静态范围核对，本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN。保护用户通过仍限v36/revision39十六项，所有旧阶段保持各自原版本/清单；未实际触发的独立用例为UNKNOWN。

## 【FACT】文件与调用关系

| 现有文件 | 当前改动 |
|---|---|
| [Favorites](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelFavorites.cs) | 缓存提示开关/文案及木石显示标签；ConsumptionHint按真实Name与正成本格式化 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | Show缓存两制作提示、传同一Favorites到三预览类，累计可选行与鼠标许可；GUI绘制缓存 |
| [RepairPanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairPanel.cs) | 当前级修理成本/上限、收藏Revision及两提示缓存/可选行 |
| [CapacityUpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradePanel.cs) | 原实际下一容量级配方、收藏Revision及一提示缓存/可选行 |
| [ToolUpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradePanel.cs) | 原两ToolPreview下一工具级配方、收藏Revision及两提示缓存/可选行 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原结构尾部追加两必填显示字段，无新组件 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | schema42/revision45、显式默认值、严格文案及原根映射 |

v37提示阶段共十现有C#、两JSON，未新增脚本/类/meta/组件或资源结构；当前[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)另接一个普通C#辅助类及正常导入meta。原Snapshot/ListView/Search/Details、DropClient、Preferences/Data/Store、HUD/Binding、PlayerInput与服务端代码保持。Favorites仍普通C#类、GUID=fce8d766a3a6f334286842eb7f4d86a8；DropClient和Details原GUID保持。

## 【FACT】JSON契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)及BuiltIn一致为schemaVersion=42/configRevision=45，原87字段与下列提示两字段保留，另有[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)四字段及[配方筛选](MapInventoryRecipeFilter.md)七字段及[配方搜索](MapInventoryRecipeSearch.md)七字段及[配方偏好](MapInventoryRecipePreferences.md)两字段及[配方收藏](MapInventoryRecipeFavorites.md)九字段，当前共118字段：

| 字段 | 建议值 | 校验/映射 |
|---|---|---|
| favoritesConsumptionHintEnabled | true | 严格bool→原Settings的FavoritesConsumptionHintEnabled byte |
| favoritesConsumptionHintLabel | Uses favorites | 非空白、无控制字符、最多61 UTF-8字节→FavoritesConsumptionHintLabel FixedString64Bytes |

DTO/Settings各118字段：DTO二十四bool、六float、五int、三模式string、79文案string及一偏好文件ID；Settings二十四byte、六float、五int、三byte枚举及80 FixedString64Bytes，零GhostField/无GhostComponent。沿原严格UTF-8、对象完整/类型及缺失/null/未知/重复键校验，关闭提示/收藏/面板或原能力仍完整验证。旧地图v1～v41、未来版本拒绝，不补默认或回退来源，正常导入/烘焙后生效、无热重载，各端同版。

## 【CURRENT STRATEGY】真实材料与七份配方

提示有效条件为favoritesConsumptionHintEnabled与favoritesEnabled同时开启。Favorites.ConsumptionHint复用原IsFavorite，按Msg.ItemName.木材/石材的稳定FixedString64Bytes Name查询已应用集合，分别只接纳woodCost>0/stoneCost>0；标题与Wood/Stone显示标签在Configure转换并缓存。木材在前、石材在后，仅木材为Uses favorites: Wood，仅石材为Uses favorites: Stone，两者为Uses favorites: Wood, Stone，没有命中返回空。显示别名/排序/搜索/仅看收藏/数量不足或归零不改变真实匹配；不支持苹果、小块肉、工具或其他材料提示。

| 配方 | 读取原成本 | 可显示状态 |
|---|---|---|
| 斧头/镐子制作或重做 | 原Lv1定义CraftWoodQuantity/CraftStoneQuantity | gatherTools开启且耐久小于原DurabilityCostPerCompletion，包含未持有；已有可用工具隐藏 |
| 斧头/镐子修理 | 当前级有效定义RepairWoodQuantity/RepairStoneQuantity | 工具/修理开启、已持有且耐久低于当前级MaxDurability；未持有/满耐久隐藏 |
| 背包容量升级 | 实际当前级下一定义WoodQuantity/StoneQuantity | 容量/升级均开启且未满级 |
| 斧头/镐子升级 | 原ToolPreview实际当前级下一定义WoodQuantity/StoneQuantity | 工具/升级均开启、已持有且未满级 |

每份配方最多一行，放在原配方行之后、缺料行之前，固定原RowHeightPixels；无有效配方、关闭能力或无正成本收藏材料则隐藏。缺材料仍提示，原InventoryValid与CanCraft/CanRepair/CanUpgrade按钮资格保持。已持有损坏工具重做仍使用原满Lv1配方，升级不借用修理成本，修理按实际级上限；不在显示路径决定或扣减材料。

## 【CURRENT STRATEGY】缓存、滚动与生命周期

原JSON→Reader/Validator→Baker根Settings→Binding/HUD整体传递→Panel.Configure链保持。Show先捕获完整库存，沿ListView应用原待收藏/筛选请求，再将同一Favorites传至三预览Capture；合法偏好恢复已在原Configure中应用。四处预览按原数量/耐久/等级/合法性和Favorites.Revision刷新提示缓存，修理另核对当前实际成本及上限；配置/定义仍在原绑定Configure固定，无热重载。

Panel按当前配方类别累计可见两制作、两修理、一容量及两工具升级的ConsumptionHintRowCount；All最多7、Craft最多2、Repair最多2、Upgrade最多3行，与同一缓存文字对应，计入原内容高度。GUI仅读缓存、按非空文字绘制一行，没有逐次扫描收藏/库存、转换配置文案或改状态。总提示高度变化清_mousePressAccepted/_rowMousePressAccepted，防止旧按下在新位置触发按钮；不清已排队制作/修理/升级/丢弃请求。收藏Revision改变仍沿原清点击许可，搜索字段在配方之前，其原命中Y不因新提示变化。

取消收藏或Reset view实际应用后下一有效Show刷新；GUI仅排原Name/重置请求，未应用前读取旧集合。B关闭/逐帧Clear/死亡断线/所属玩家或源变化及World/Scene失效沿原生命周期处理；Reset清新文案、Revision缓存和总行数，合法新绑定恢复配置/偏好。提示独立于收藏计数/筛选/排序/分类/搜索/详情/偏好/重置按钮与favoritesDropProtectionEnabled；关闭这些能力不关闭启用提示，关闭收藏或提示隐藏全部新增行。

## 【CURRENT STRATEGY】业务边界

提示仍捕获原B面板七份候选，仅绘制[配方筛选](MapInventoryRecipeFilter.md)当前可见组，数字1～7/E/F/G沿原输入；v38的[B按钮消耗确认](MapInventoryFavoritesConsumptionConfirm.md)独立读取已应用收藏及正成本，关闭提示仍可确认。提示本身只读，不决定请求或失败/成功反馈。收藏丢弃保护仍按其原行与消费前检查独立生效；提示不解除保护，也不阻止原制作、修理或两升级消耗收藏木石，数量归零仍保留原收藏Name。

原输入19、DropGhost4/请求2、Tools3、F4/G7/资源状态4/世界保存3及全部反馈保持；玩家v4根7/工具项3、世界v2根9/掉落项8、本机偏好CurrentVersion=5/Data十字段兼容v1～v4，规则归[偏好](MapInventoryPreferences.md)。没有服务器收藏同步/新RPC/新输入/Ghost字段，原SavePrepared、库存候选/提交/失败行为及本机偏好提交路径保持，无新文件I/O入口。

## 【CURRENT STRATEGY】配方分类、搜索、偏好与收藏关联

当前v42/revision45的[配方收藏](MapInventoryRecipeFavorites.md)在原七项操作接入本机收藏/分区置顶，偏好v5十字段严格兼容v1～v4；独立保存、关闭项保留、上限/重置/延迟/失败规则归专题。材料收藏/工具状态、原输入/Ghost及服务器事务/玩家和世界档案保持；本阶段待人工。配方偏好用户通过仍限v41/revision44十六项，搜索仍限v40/revision43十六项，分类仍限v39/revision42原清单，各旧通过保持原范围，未触发独立用例仍UNKNOWN。

## 【KNOWN ISSUES】静态证据与人工边界

以下静态与人工通过证据限v37/revision40提示阶段。该阶段正常Unity编译/重载完成，89字段/类型构成、零GhostField、普通类/GUID及ConsumptionHint返回string/两int参数、三预览Capture末参同一Favorites和可选行int属性核对通过。DropClient仍DrawRow九参数/ReadRequest一个Favorites参数/请求两字段，原偏好/输入/保存元数据保持。3280份非法配置全部拒绝（每地图1640），276组合法读取通过（每地图138），覆盖完整89字段/形状/类型/重复键、严格bool、文案UTF-8 61字节/关闭仍验证、旧v1～v36/未来版本与全部原规则；只验证配置和反射元数据，没有调用格式化/收藏/面板/GUI业务。

两地图各138次、共276次隔离Editor Bake完成；保留原117变体，追加提示/收藏/面板关闭、文案ASCII61/UTF-8 61/中文、计数/收藏筛选/丢弃保护/排序/分类/搜索/详情/偏好/重置关闭、提示与收藏同时关闭、工具/容量/容量升级/工具升级关闭及修理关闭共21变体。全部89Settings、原Settings/零反馈/Prefab引用、布置与兼容签名符合检查；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。源SubScene只读，临时克隆/TextAssets/Scene/World/BlobAssetStore释放，最终主场景干净、3根对象、单场景、未Play。

Console执行前[0 Error,3 Warning,53 Log]、正常编译后/Bake前[0 Error,5 Warning,53 Log]、Bake后/最终[0 Error,7 Warning,53 Log]。编译重新报告原PEListener UAC1001/DOTween CS0618，原三条NetCode Tick Batching保留；Bake期间另有MCP WebSocket keep-alive/connection closed两工具警告。工具返回空失败状态，之后核实完整276条落盘结果、零Error及资源释放后确认静态完成；没有重跑业务或更改生产代码补工具状态。没有新增本阶段项目编译错误/警告，未清Console，不用既有运行日志推导性能。

提示阶段原531项人工内容/编号逐字保留，追加十六项后当时共547项，归[运行入口](Runtime.md)“收藏材料消耗提示”章节。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；未实际触发的独立GUI命中/滚动/裁切/中文字形/分辨率、收藏应用/取消/重置/预览状态时序、真实偏好I/O/故障、材料消耗/事务、多人与预测、性能/平台/线上未实际触发为UNKNOWN，旧阶段通过不覆盖新提示。

AI未执行Favorites/Panel/各预览/GUI业务、实际偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片读取；未创建子Agent、暂存或提交Git。

## 【CURRENT STRATEGY】收藏材料消耗确认

当前v42/revision45的[收藏材料消耗确认](MapInventoryFavoritesConsumptionConfirm.md)仅处理原B七个制作/修理/容量与工具升级按钮。首次有效按钮请求命中已应用收藏木石的正成本时暂存一个操作，在配方内显示数量提示，以Confirm/Cancel替换原按钮行；确认按最新已捕获候选和本地Revision复核后沿原请求提交一次，取消/关闭B/相关数量、等级、耐久、配方或已应用收藏变化/绑定失效清待确认。逐帧Clear仅隐藏，确认目标或高度变化清旧面板与行鼠标许可。确认独立于原消耗提示开关；数字1～7保持原直达链，偏好v5十字段、输入19、Ghost/服务器与保存入口保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，已验收提示仍限v37/revision40，全部旧通过保持原版本/清单。
