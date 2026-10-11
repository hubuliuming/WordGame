# 制作配方收藏与置顶

返回[地图](Map.md)、[B面板](MapInventoryPanel.md)、[配方分类](MapInventoryRecipeFilter.md)、[配方搜索](MapInventoryRecipeSearch.md)、[本机偏好](MapInventoryPreferences.md)、[配方偏好](MapInventoryRecipePreferences.md)、[材料收藏](MapInventoryFavorites.md)和[运行入口](Runtime.md)。入口CombatPrototypeNetCode、原B面板；两地图Json/BuiltIn当前v42/revision45。本阶段实现与静态范围核对通过，人工GamePlayer待验收。原611项人工内容/编号保留，配方偏好用户通过仍限v41/revision44十六项，各旧通过保持原版本/清单，未实际触发独立用例仍UNKNOWN。

## 【FACT】文件与接入点

新增普通C#辅助类[RecipeFavorites](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryRecipeFavorites.cs)，GUID=607d3f39f419f634da3bffcc64ccbd0b，对应meta由正常Unity导入生成。没有新增MonoBehaviour/ECS组件或挂载。

| 现有文件 | 当前接入职责 |
|---|---|
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 配置/恢复、最新可见项之后应用待收藏、两区绘制/高度、请求取消与生命周期 |
| [Repair](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairPanel.cs)、[Capacity](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradePanel.cs)、[ToolUpgrade](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradePanel.cs) | Draw接收RecipeFavorites，在原标题加标记、原操作之后加收藏控制；原完整Capture与资格保持 |
| [Preferences](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferences.cs)、[Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesData.cs)、[Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs) | 配方收藏恢复/Revision观察/集合比较、v5十字段、严格v1～v4读取与原写入边界 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原配置尾部九字段、严格范围/文案校验、显式默认与原地图根Baker映射 |

当前收藏接入由十二个既有C#、两JSON及一个普通辅助类/正常meta承接。原RecipeFilter/RecipeSearch/Search、材料Favorites/ListView/Details/DropClient、Confirmation、HUD/Binding/PlayerInput及全部服务器源码保持；原输入19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3与反馈、玩家v4/世界v2保持。Scene/SubScene/Prefab/Animator、旧meta、资源/字体/包/构建配置结构保持。

## 【FACT】JSON契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与BuiltIn一致schemaVersion=42/configRevision=45。inventoryPanel原109字段/顺序/值保留，尾部追加九必填字段：

| 字段 | 默认 | 校验与Settings |
|---|---|---|
| recipeFavoritesEnabled | true | 严格bool→RecipeFavoritesEnabled byte |
| recipeFavoritesMaxCount | 7 | 严格int 1～7→RecipeFavoritesMaxCount int |
| recipeFavoriteButtonLabel | Favorite | 文案→RecipeFavoriteButtonLabel |
| recipeUnfavoriteButtonLabel | Unfavorite | 文案→RecipeUnfavoriteButtonLabel |
| recipeFavoriteTagLabel | Favorite | 文案→RecipeFavoriteTagLabel |
| recipeFavoritesFullLabel | Favorite limit | 文案→RecipeFavoritesFullLabel |
| favoriteRecipesLabel | Favorite recipes | 文案→FavoriteRecipesLabel |
| otherRecipesLabel | Other recipes | 文案→OtherRecipesLabel |
| preferencesSaveRecipeFavorites | true | 严格bool→PreferencesSaveRecipeFavorites byte |

六文案非空白、无控制、最多61 UTF-8字节，映射FixedString64Bytes。关闭收藏/保存/偏好/面板仍完整验证。DTO/Settings各118字段：DTO二十四bool、六float、五int及83string（三模式、79文案、一文件ID）；Settings二十四byte、六float、五int、三byte枚举及80 FixedString64Bytes。0 GhostField/无GhostComponent；旧地图v1～v41及未来版本拒绝，不补默认、不回退来源、无热重载，正常导入/烘焙生效，各端同版。

## 【FACT】七项稳定收藏标识

复用原Confirmation.Operation七项枚举，收藏记录与材料Name、显示文案、行号和快捷键编号分离：

| 文件ID | 原操作 |
|---|---|
| craft_axe | CraftAxe；斧头制作/重做共享 |
| craft_pickaxe | CraftPickaxe；镐子制作/重做共享 |
| repair_axe | RepairAxe |
| repair_pickaxe | RepairPickaxe |
| capacity_upgrade | CapacityUpgrade |
| upgrade_axe | UpgradeAxe |
| upgrade_pickaxe | UpgradePickaxe |

RecipeFavorites用七位byte集合及一个待操作保存本地选择，普通Section缓存可见收藏/其他两区。记录上限固定七个已知ID；降低当前配置上限不删已存收藏，达到或超过当前上限禁止新收藏，已收藏始终可取消。隐藏收藏仍计入上限，分类/查询变化不清集合。

## 【CURRENT STRATEGY】应用、分区与原业务

原有效Show先捕获完整Snapshot、应用Reset view/类别/配方查询，RecipeSearch按原类别与词取交集；RecipeFavorites.Capture只对最新仍可见的待操作应用一次，隐藏项的待请求丢弃。GUI只排操作，不在绘制期间改集合、重排或写盘；标题标记字符串在Title按原标题变化缓存，不修改业务标识。

每项可见配方加一行Favorite/Unfavorite，达到上限的非收藏项为禁用Favorite limit；业务按钮缺料、未持有、满耐久、满级或能力关闭不阻止收藏。标题标记为原标题加空格及[配置tag]。收藏开启时工具状态常驻摘要先显示，之后收藏区、其他区各按原容量升级→制作两项→修理两项→工具升级两项顺序绘制；每项只出现一次。仅有可见收藏时出现Favorite recipes标题，同时有可见其他项才出现Other recipes；无可见收藏不增加区标题。收藏关闭恢复原容量预览/工具摘要/制作/修理/升级顺序及原行高，不加控制和标记。

原三预览及全部七项候选Capture保持，不从分区推导资格。按Section计算原基础行数，增加每个可见收藏控制行与实际区标题；消耗提示及确认行仍按原可见操作计入。分区拆开时各自绘制原组标题和工具升级反馈行，内容高度相应计入。面板矩形、材料/容量摘要、双搜索字段命中位置、字体/缩放、固定页脚和GUI状态恢复保持。

实际收藏变化、Reset view清集合或原类别/查询变化，沿原ClearRecipeRequests取消七个未消费B请求、待收藏材料消耗确认及旧面板/行鼠标许可，滚动归零，即使总高度相同也取消。材料收藏/丢弃状态独立；不撤销已提交服务器事务。数字1～7仍绑定原操作，置顶不重新编号，原输入屏蔽/服务端SavePrepared与扣料流程保持。

## 【FACT】本机偏好v5

路径继续为Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json。本机偏好CurrentVersion=5，Data十实例字段；原v4九字段保留，尾部追加favoriteRecipeIds数组。数组必须最多七个唯一、严格string、逐字已知ID；大小写/空白变体、未知/null/错类型/重复项拒绝。任一字段、地图、版本、重复键或额外根内容非法整档失败，关闭收藏/保存仍完整校验。

严格兼容v1五/v2六/v3七/v4九字段：原材料排序/类别/词、收藏/筛选及配方字段沿原版本规则保留；v1～v3配方类别用当前配置默认、词为空，v4保留原类别和词；四种旧档的新配方收藏均为空，只内存迁移v5。加载/首次无文件本身不写盘，下一实际已应用偏好变化才保存v5，旧客户端不支持新v5。两个配方类别/查询读取条件为version>=4，避免升级CurrentVersion丢失旧v4值。

## 【CURRENT STRATEGY】独立保存、重置与失败

配方收藏只有RecipeFavoritesEnabled与PreferencesSaveRecipeFavorites同时开启时恢复/观察。两个开关关闭时其他偏好变化保存保留文件原配方收藏；原材料收藏与类别/关键词保存开关独立。全局PreferencesEnabled或面板关闭不读写；本绑定I/O暂停时本地收藏仍可临时使用。

Preferences按已应用配方收藏Revision复制稳定ID，按集合比较与上次成功写值是否不同，数组顺序不制造变化，返回已写集合取消待写。复用unscaledTime默认0.5秒合并及Close/绑定Reset提交；未应用GUI请求不保存。Reset view清待收藏，收藏开启时清已应用集合，再按保存开关参与原延迟保存，关闭保存保留文件原值。B/Close及无效ReadInput清待请求，同绑定关闭重开保留已应用集合；逐帧Clear只隐藏。绑定Reset/死亡断线/源、玩家、World或Scene失效清本地缓存，新有效绑定按开关/正式档案恢复。

UTF-8读可带BOM，写无BOM的.json.tmp/Flush(true)，再Replace原正式文件或首次Move；Load/Save失败沿原模块/阶段/地图/路径/异常日志，暂停本绑定I/O，不自动修复/重试或覆盖坏档。残留.tmp不恢复为正式档案。同机/fileId/地图共享，不按玩家ID分档或云同步；玩家v4/世界v2保存路径/事务保持。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译/重载、118字段类型/零GhostField、新普通类/正常meta/GUID、偏好v5十字段、三个Draw末参数与原Capture/输入/Ghost/存档协议元数据核对通过。4920份非法地图配置全部拒绝（每地图2460），612组合法读取通过（每地图306），覆盖新九字段/六文案/上限1～7、关闭仍验证和旧v1～v41/未来版本及原规则。没有执行配方收藏/分区/GUI或偏好Load/Save业务测试。

两地图各306次共612次隔离Editor Bake完成，保留原263变体并追加43个开关组合/全部七个上限/六文案ASCII61、UTF-8 61和中文/能力关闭/降低上限/四默认类别变体。完整118Settings映射与原Settings/零反馈/Prefab引用、布局/资源签名保持；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。临时克隆/TextAssets/Scene/World/BlobAssetStore释放，主场景干净、3根对象、单场景、源SubScene未加载，未Play/未编译。

Bake前后及结束Console均[0 Error,6 Warning,4 Log]；保留Package Manager/Input Manager、PEListener/DOTween及两MCP连接警告，未清Console。配置核对和Bake工具均返回空失败状态，已核实完整落盘4920拒绝/612合法及612次Bake结果与结束状态，未重跑业务。

原611项人工内容/编号逐字保留，新增十六项后共627项，归运行入口本阶段清单；主线程判定实现与静态范围核对通过，人工GamePlayer待用户验收。实际点击/分区重排/高度/裁切/字形/缩放/IME/鼠标许可、保存恢复/旧档迁移/坏档/权限/写失败/关闭重启及绑定生命周期、材料事务/多人/预测/延迟未实际触发仍UNKNOWN；并发进程、断电/平台Flush或Replace语义、性能与线上未验收。材料收藏专题当前协议为v5十字段，历史静态/人工证据保持原版本。

AI未执行真实偏好或玩家/世界存档I/O、面板/收藏/类别/搜索/协调/GUI业务、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片；无子Agent、暂存或Git提交。实现范围没有Scene/SubScene/Prefab/Animator或旧meta/资源结构改动。
