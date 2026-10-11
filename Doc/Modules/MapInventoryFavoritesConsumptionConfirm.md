# 收藏材料消耗确认

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[收藏](MapInventoryFavorites.md)、[消耗提示](MapInventoryFavoritesConsumptionHint.md)、[修理](MapToolRepair.md)、[容量升级](MapInventoryCapacityUpgrade.md)、[工具升级](MapGatherToolUpgrade.md)、[配置](DataResources.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode、原B面板；两地图Json/BuiltIn当前v41/revision44。主线程按已确认方案完成接入与静态范围核对；本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN。已验收提示仍限v37/revision40十六项，各旧阶段保持自己的原版本/清单，未实际触发的独立用例为UNKNOWN。

## 【FACT】文件与接入点

| 文件 | 当前职责 |
|---|---|
| [Confirmation](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryConsumptionConfirmation.cs) | 新普通C#辅助类；七操作标识、只读候选、单待确认与决定Revision、缓存提示、Confirm/Cancel绘制及一次请求选择 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 持有确认实例；Configure、Show两制作候选、原ReadInput七按钮请求、生命周期和内容高度/鼠标许可 |
| [RepairPanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairPanel.cs) | 原有效修理预览捕获两候选，配方内绘制确认内容 |
| [CapacityUpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradePanel.cs) | 原有效下一容量级预览捕获一候选，原按钮行绘制确认内容 |
| [ToolUpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradePanel.cs) | 原两ToolPreview捕获下一工具级候选，配方内绘制确认内容 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原结构尾部追加四必填配置字段 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | schema41/revision44、显式默认、原根四字段映射与文案校验 |

确认v38阶段九现有C#、两JSON、一个新普通C#及其正常Unity导入meta；确认GUID=498e9ba559536854b901419523142848，无新ECS/MonoBehaviour、挂载或Scene/SubScene/Prefab/Animator/旧meta/资源/字体/包/构建结构变更。Favorites、Snapshot/ListView/Search/Details、DropClient、Preferences/Data/Store、HUD/Binding、PlayerInput与服务端源码保持；Favorites/DropClient/Details原GUID保持。

## 【FACT】JSON契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与BuiltIn均为schemaVersion=41/configRevision=44，原inventoryPanel89字段及确认四字段保留，另有[配方筛选](MapInventoryRecipeFilter.md)七字段及[配方搜索](MapInventoryRecipeSearch.md)七字段；确认字段为：

| 字段 | 默认值 | 校验与映射 |
|---|---|---|
| favoritesConsumptionConfirmationEnabled | true | 必填严格bool→FavoritesConsumptionConfirmationEnabled byte |
| favoritesConsumptionConfirmationLabel | Use favorites? | 必填文案→FavoritesConsumptionConfirmationLabel FixedString64Bytes |
| favoritesConsumptionConfirmLabel | Confirm | 必填文案→FavoritesConsumptionConfirmLabel FixedString64Bytes |
| favoritesConsumptionCancelLabel | Cancel | 必填文案→FavoritesConsumptionCancelLabel FixedString64Bytes |

三文案非空白、无控制字符、最多61 UTF-8字节；关闭确认/收藏/面板/提示仍完整验证。DTO/Settings各109字段：DTO二十二bool、六float、四int、三模式string、73文案string及一偏好文件ID；Settings二十二byte、六float、四int、三byte枚举及74 FixedString64Bytes，零GhostField/无GhostComponent。原严格UTF-8、对象完整/类型、缺失/null/未知/重复键及语义校验保持；旧地图v1～v40/未来版本拒绝，不补默认或回退来源。正常导入/烘焙生效，无热重载，各端同版。

## 【CURRENT STRATEGY】首次按钮请求与实际配方

Panel.Configure配置独立确认实例；Show先捕获完整库存，沿ListView应用原待收藏/重置，再将同一Favorites和确认实例传到三预览。合法偏好已在原Configure恢复，使用的是已应用收藏集合，GUI尚未应用的收藏请求不作为真值。确认用原IsFavorite按Msg.ItemName.木材/石材稳定Name查询，分别要求woodCost>0/stoneCost>0；不调用ConsumptionHint决定是否确认，因此原提示开关关闭仍可确认。

| B按钮 | 沿原资格与实际成本 |
|---|---|
| 斧头/镐子制作或重做 | 原CanCraft，工具开启/库存合法/耐久小于Lv1消耗门槛/材料足够；读取原Lv1完整CraftWoodQuantity/CraftStoneQuantity |
| 斧头/镐子修理 | 原CanRepair，工具和修理开启/已持有/低于当前级上限/库存合法及材料足够；读取当前有效RepairWoodQuantity/RepairStoneQuantity |
| 容量升级 | 原CanUpgrade，容量与升级开启/未满级/库存合法及材料足够；读取当前容量级的下一定义WoodQuantity/StoneQuantity |
| 斧头/镐子升级 | 原ToolPreview.CanUpgrade，工具与升级开启/已持有/未满级/库存合法及材料足够；读取当前工具级下一定义WoodQuantity/StoneQuantity |

原GUI按钮仍只排原七个本地布尔请求；Binding复核World/地图/所属存活玩家与Connected/InGame后，沿HUD→Panel.ReadInput消费。启用确认且最新已捕获候选合格、存在正成本已收藏木石时，首次请求转为本地待确认，尚不输出原业务标志。不命中或关闭确认沿原按钮请求链；命中但资格已失效时清请求、无自动重试/新反馈。

最多一个待确认操作。点另一个有效配方按钮时替换旧待确认，旧决定Revision失效；没有收藏成本的有效操作沿原提交并取消旧待确认。首次转入时缓存提示，例如Use favorites? Wood 3, Stone 2，只列实际将消耗的收藏材料和该配方完整成本，木材在前。显示别名、排序/分类/搜索/仅看收藏不改变真实Name与成本，隐藏材料仍计入完整库存/资格。原消耗提示与确认提示可同时存在。

## 【CURRENT STRATEGY】确认、取消和生命周期

确认候选保存原Eligibility、木石成本/当前数、相关等级/耐久/上限、Favorites.Revision及两材料收藏命中。有效预览Capture更新对应候选，若与待确认不再相同即清待确认；Confirm只排一个带当前本地Revision的决定，ReadInput按最新已经捕获的候选再次核对操作、资格、收藏成本与Revision。先消费确认状态，再输出一个原请求标志一次；重复GUI/ReadInput不复用旧决定，不自动补发。

Cancel不提交；实际[配方筛选](MapInventoryRecipeFilter.md)类别变化清待确认和未消费决定；B关闭/Close、无效ReadInput、绑定Reset/死亡/断线/源或玩家变化/World或Scene失效清待确认和未消费决定。相关材料数、配方、等级、耐久、资格、已应用收藏状态变化后下一有效Show取消；Reset view实际应用后沿收藏Revision取消。合法新绑定沿原配置/偏好恢复，不保存旧待确认。逐帧Clear只隐藏HUD，不清该确认状态，下一有效Show继续；没有配置热重载或跨绑定确认。

确认复核只覆盖本机最新已捕获快照，不声称客户端至服务端之间状态不变。原服务器生命/库存/工具/等级/同tick/忙碌资格与SavePrepared顺序保持；服务端拒绝或保存失败沿原所属反馈和重新请求规则。确认不扣料、不返料、不写偏好/玩家/世界存档，不新增服务器收藏、RPC/输入/Ghost或文件I/O。

## 【CURRENT STRATEGY】行数、鼠标和输入范围

待确认配方内在原可选消耗提示之后、缺料行之前追加一条固定RowHeightPixels提示；该配方原全宽按钮行替换为同高Confirm/Cancel两按钮，其他配方保持原显示。单待确认RowCount为0或1，直接计入Panel滚动内容高度，原提示总行数仍0～7。GUI只读缓存候选/提示并排本地决定，不扫描库存或转换配置文案；既有GUI matrix/color/enabled恢复与固定页脚保持。

待确认Revision在首次进入、替换、取消或消费时变化，Panel在有效Show和ReadInput核对并清_mousePressAccepted/_rowMousePressAccepted；目标即使同高度替换也清旧许可。原详情/收藏/提示高度变化规则继续生效；搜索字段位于配方之前，其命中Y与焦点/IME/blocksKeyboard/鼠标隔离保持，无新Enter/Esc确认快捷键或游戏暂停。

仅B七按钮经过确认；数字1～7仍走原PlayerInput直达制作/修理/升级链，E/F/G/R/F5、移动/攻击/镜头沿原规则。确认独立于消耗提示、计数、收藏筛选、丢弃保护及排序/分类/搜索/详情/偏好/重置显示开关；收藏关闭时IsFavorite不命中，确认关闭恢复原按钮路径，两者关闭仍完整验证。

## 【CURRENT STRATEGY】配方分类、搜索与偏好关联

当前v41/revision44的[配方偏好](MapInventoryRecipePreferences.md)把配方类别与已应用关键词接入本机偏好v4九字段；独立保存开关、严格v1/v2/v3迁移、关闭项保留和重置/延迟/失败规则归专题。材料与工具状态、原输入/Ghost及服务器事务/玩家和世界档案保持；本阶段待人工。搜索用户通过仍限v40/revision43十六项，分类仍限v39/revision42原清单，其他旧通过保持原范围，未触发独立用例仍UNKNOWN。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译/重载完成，新meta由Unity正常生成。93字段及类型构成、零GhostField、普通类/枚举/候选/方法签名和GUID、三预览末两参Favorites/Confirmation核对通过；原ConsumptionHint、DropClient九参数DrawRow/单Favorites参数ReadRequest/请求两字段、偏好CurrentVersion3/Data七字段兼容v1/v2、输入19/Tools3/DropGhost4/F4/G7/资源状态4/世界保存3及全部所属反馈/玩家v4/世界v2元数据保持。

3492份非法配置全部拒绝（每地图1746），334组合法读取通过（每地图167），覆盖完整93字段/形状/类型/重复键、严格bool、新三文案非空/控制/UTF-8限制/关闭仍验证、旧v1～v37/未来版本及原规则。两地图各167次、共334次隔离Editor Bake完成，保留原138变体并追加确认/收藏/面板关闭、三文案各ASCII61/UTF-8 61/中文、十项独立显示开关、确认与收藏同时关闭、四原能力/修理关闭、确认与提示同时关闭共29变体。全部93Settings、原Settings/零反馈/Prefab引用、布局和兼容签名符合检查；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。源SubScene只读，临时克隆/TextAssets/Scene/World/BlobAssetStore释放，最终主场景干净、3根对象、单场景、未Play。

Console执行前[0 Error,7 Warning,53 Log]、正常编译后/Bake前[0,5,53]、Bake后/最终[0,7,53]。原三条NetCode Tick Batching与两条MCP连接警告保留，本次编译未重新报告原PEListener/DOTween两编译警告；Bake期间新增两MCP WebSocket连接工具警告。工具返回空失败状态，之后核实完整334条落盘结果、零Error及资源释放后确认静态完成；没有重跑业务或为工具状态修改生产代码，无本阶段新增项目编译错误/警告。未清Console，不用历史运行日志推导性能。

原547项人工内容/编号逐字保留，追加十六项后共563项，归[运行入口](Runtime.md)“收藏材料消耗确认”章节。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN。按钮/Confirm/Cancel/重复事件/目标替换/按下抬起、几何/滚动/裁切/真实字形/分辨率、数量/工具/收藏/绑定时序、真实偏好与游戏保存I/O/故障/材料事务、数字直达/多人/预测/延迟以及性能/平台/线上未实际触发为UNKNOWN；旧提示通过不覆盖新确认。

AI未调用确认/收藏/面板/预览/GUI业务，未执行真实偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片；未创建子Agent、暂存或提交Git。
