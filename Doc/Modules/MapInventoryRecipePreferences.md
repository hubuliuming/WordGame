# 制作配方显示偏好本地保存

返回[地图](Map.md)、[B面板](MapInventoryPanel.md)、[本机偏好](MapInventoryPreferences.md)、[配方分类](MapInventoryRecipeFilter.md)、[配方搜索](MapInventoryRecipeSearch.md)、[数据](DataResources.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode、原B面板；两地图Json/BuiltIn为v41/revision44。主线程按已确认方案完成实现及静态范围核对，人工GamePlayer待验收；搜索用户通过仍限v40/revision43十六项，分类仍限v39/revision42原清单，各旧通过保持原版本/清单。

## 【FACT】文件、配置与调用链

调整十个现有C#：[Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs)、[Preferences](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferences.cs)、[Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesData.cs)、[Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs)、[RecipeFilter](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryRecipeFilter.cs)、[DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs)。没有新增C#/组件或meta；原RecipeSearch/Search、ListView/Favorites/Details、Confirmation与三预览、HUD/Binding/PlayerInput及服务器源码保持。

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)及BuiltIn一致schemaVersion=41/configRevision=44；inventoryPanel原107字段/顺序/值保留，尾部追加两必填严格bool，关闭面板/偏好/对应能力或保存仍完整验证：

| JSON字段 | 默认 | Baker/Settings |
|---|---|---|
| preferencesSaveRecipeFilter | true | PreferencesSaveRecipeFilter byte |
| preferencesSaveRecipeSearch | true | PreferencesSaveRecipeSearch byte |

DTO/Settings各109字段：DTO二十二bool、六float、四int及77string（三模式、73文案、一文件ID）；Settings二十二byte、六float、四int、三byte枚举及74 FixedString64Bytes。0 GhostField/无GhostComponent；Reader原UTF-8/形状/完整字段/严格类型/重复键规则保持，旧地图v1～v40与未来版本拒绝，无补默认/来源回退/热重载，正常导入/烘焙及各端同版生效。

原有效Client Binding→HUD.Configure→Panel.Configure先Reset/提交旧绑定，配置两种搜索、RecipeFilter等后调用Preferences.Configure，新增传入类别实例与RecipeSearch.Editor。Store读正式偏好一次；有效Show在完整Snapshot之后应用配方切换/文本，再沿原ListView.Capture及Preferences.Capture观察两者已应用值。Close沿原Flush提交待保存值；逐帧Clear只隐藏，死亡/断线/源、玩家、World或Scene失效沿原Reset清缓存，新有效绑定重新读档。原十九输入、F/G/B请求与七项资格/Capture/服务器SavePrepared保持。

## 【FACT】本机偏好v4

路径仍为Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json，默认fileId=inventory_display。原v3七字段尾部追加两字段，Data共九实例字段，Store.CurrentVersion=4；完整原字段/收藏规则归[偏好](MapInventoryPreferences.md)：

| 新增字段 | 文件类型与规则 | Data类型 |
|---|---|---|
| recipeFilterMode | 严格string，逐字all/craft/repair/upgrade | 原四模式byte枚举 |
| recipeSearchText | 严格string，允许空白/空词，最多64 UTF-16单元，无控制或孤立代理项 | string |

Store复用原文本校验，诊断区分searchText/recipeSearchText；类别Resolver接受字段来源名，原配置来源仍inventoryPanel.defaultRecipeFilterMode，偏好来源为recipeFilterMode。v4缺失/null/错类型/非法模式/文本、未知或重复字段、错地图/版本/额外根内容整档拒绝，保存开关关闭仍完整校验；旧版客户端不支持新v4文件。

严格读v1五/v2六/v3七字段：原模式/材料查询保留，v1空收藏且favoritesOnly=false，v2保留收藏且favoritesOnly=false，v3保留收藏与favoritesOnly。三种旧档的新增类别采用当前Settings.DefaultRecipeFilterMode、配方词为空，仅内存迁移v4；读取不创建/覆盖文件、不因迁移排队保存，下一实际已应用偏好变化才写v4。首次无文件同样按配置默认/空词建立记录，显示未变化不创建目录/文件。

## 【CURRENT STRATEGY】恢复、独立开关与保留

全局PreferencesEnabled或面板关闭不读/写偏好。类别只在PreferencesSaveRecipeFilter与RecipeFilter.Enabled同时开启时恢复/观察；关键词只在PreferencesSaveRecipeSearch与RecipeSearch.Editor.Enabled同时开启时恢复/观察。原PreferencesSaveSearch只控制材料词，两词互不覆盖、开关互不替代；关闭对应能力/保存时仍允许原临时显示操作，其他偏好变化保存时保留读入的对应原值。

RecipeFilter.Restore只应用已验证的枚举、清未应用切换并更新缓存文本，不排GUI请求；分类能力关闭继续All。Search.Restore复用原CleanInput/当前长度限制，更新Revision、不聚焦或排队编辑。实际使用词可按配置recipeSearchMaxLength截短，读入原记录保留；仅恢复不制造dirty，之后实际已应用文本变化才更新文件值。类别与词恢复后沿原交集匹配，缺料/满级等资格文案与材料/工具状态保持。

## 【CURRENT STRATEGY】保存、重置与失败

Preferences.Capture新增观察Mode/AppliedText，并将其与上次已写值纳入原dirty判断；仅已应用值变化记录Time.unscaledTime，连续变化合并、默认0.5秒后写入。库存刷新或重复绘制不写，返回上次已写值取消待写；两种草稿/未应用GUI切换不保存。Close/绑定Reset只Flush当前已捕获的待写记录，再清未应用请求；不自动应用未消费草稿。

Reset view沿原有效Show恢复配置默认类别、清配方已应用词/草稿/待编辑；原实际类别/词改变取消七个未消费B请求、待收藏消耗确认和旧鼠标许可，滚动归零。随后按各保存开关进入原延迟保存；关闭能力/保存或I/O暂停时只临时生效，保留对应文件值。材料显示/收藏/详情重置和已提交服务器事务保持原链。

保存仍UTF-8无BOM的.json.tmp/Flush(true)，再File.Replace原正式文件或首次File.Move。只有FileNotFound/DirectoryNotFound为首次使用；其他Load或Save错误沿原协调边界记录模块、阶段、地图、路径和原异常，暂停本绑定I/O、不自动修复/重试或覆盖坏档；残留.tmp不读为正式文件，重建绑定可重新读取。按本机/fileId/地图隔离，不按玩家ID分档或云同步；原玩家v4/世界v2与路径/事务保持。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译/重载、109字段类型/零GhostField、偏好v4/九字段与接口及原GUID/协议元数据核对通过。4450份非法地图配置全部拒绝（每地图2225）、526组合法读取通过（每地图263）；覆盖两必填严格bool、所有原类型/形状/语义、关闭仍验证和旧v1～v40/未来版本。没有执行偏好Store.Load/Save或类别/搜索/协调/GUI业务测试。

两地图各263次，共526次隔离Editor Bake完成；原251变体加12个保存开关组合/材料保存独立关闭/类别或搜索能力、全局偏好或面板关闭/两能力关闭/配置默认Upgrade变体。109Settings映射与原Settings/零反馈/Prefab引用、布局和资源签名保持；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。临时克隆/TextAsset/Scene/World/BlobAssetStore释放，主场景干净、3根对象、单场景、源SubScene未加载、未Play/未编译。

Console执行前[0 Error,2 Warning,4 Log]、Bake前[0,4,4]、Bake后及结束[0,6,4]；零Error，保留Package Manager/Input Manager警告及日志，正常编译另有原PEListener/DOTween警告、Bake另有两MCP连接警告，未清Console。Bake工具返回空失败状态，已核实完整526条落盘结果与结束状态，未重跑业务。

原595项人工内容/编号逐字保留，新增十六项后共611项，归[运行入口](Runtime.md)本阶段清单；主线程判定实现与静态范围核对通过，人工GamePlayer待用户验收。独立保存/恢复/旧档迁移/坏档/权限或写入故障、关闭/重启/死亡断线/换图生命周期、焦点/IME/GUI事件/字形/缩放、材料事务/多人/预测/延迟未实际触发仍UNKNOWN；同机多进程并发、断电/平台Flush或Replace语义、性能与线上未验收。

AI未执行真实偏好或玩家/世界存档I/O、面板/类别/搜索/协调/GUI业务、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片；无子Agent、暂存或Git提交。Scene/SubScene/Prefab/Animator、全部meta/资源/字体/包/构建配置结构保持。
