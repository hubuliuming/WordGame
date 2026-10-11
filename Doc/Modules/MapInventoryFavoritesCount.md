# 背包收藏数量与上限提示

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[收藏](MapInventoryFavorites.md)、[仅看收藏](MapInventoryFavoritesFilter.md)、[排序筛选](MapInventoryListView.md)、[搜索](MapInventorySearch.md)、[详情](MapInventoryDetails.md)、[本机偏好](MapInventoryPreferences.md)、[配置](DataResources.md)与[运行验收](Runtime.md)。入口CombatPrototypeNetCode、原B面板；两地图Json/BuiltIn当前v41/revision44。主线程按已确认方案完成代码/配置及静态范围核对，本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；筛选旧通过限v34/revision37十六项，收藏限v33/revision36及其他旧阶段原版本/清单。

## 【FACT】入口与文件

| 文件 | 当前职责 |
|---|---|
| [Favorites](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelFavorites.cs) | 原Name集合/容量规则；本地计数开关/文案、CountText缓存、CountRowCount0～1 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 容量后绘制计数，统一内容高度与SearchField命中 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 两必填字段及原地图根本地Settings |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 完整校验、默认值、原根组件映射 |
| [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) | 显式Json与BuiltIn同值 |

仅七现有C#脚本与两JSON接入，没有新helper/脚本/meta/组件/挂载。原HUD/Binding、ListView/Search/Details、Preferences/Data/Store、PlayerInput及服务器/玩家世界保存链代码保持；Scene/SubScene/Prefab/Animator/资源/字体/包/构建结构保持。

## 【FACT】配置与建议值

| inventoryPanel字段 | 两地图Json/BuiltIn值 | 校验与用途 |
|---|---|---|
| favoritesCountEnabled | true | 新必填严格bool→byte，控制计数行 |
| favoritesCountLabel | Favorites | 新必填文案→FixedString64Bytes |
| favoritesMaxCount | 64 | 原字段/规则保持，严格int1～256；达到/超过只限制新增 |
| favoritesFullLabel | Favorite limit | 原文案保持，满额/超限后缀与原按钮共用 |

原计数阶段83→85字段保留，另有[收藏保护](MapInventoryFavoritesDropProtection.md)与[收藏提示](MapInventoryFavoritesConsumptionHint.md)各两字段，另有[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)四字段及[配方筛选](MapInventoryRecipeFilter.md)七字段及[配方搜索](MapInventoryRecipeSearch.md)七字段及[配方偏好](MapInventoryRecipePreferences.md)两字段，当前109：DTO二十二bool、六float、四int、三模式string、73文案string及一文件ID；Settings二十二byte、六float、四int、三byte枚举及74 FixedString64Bytes，零GhostField/无GhostComponent。计数文案非空白、无控制字符、最多61 UTF-8字节，关闭计数/收藏/面板仍完整验证。仅地图schema41、正revision/seed；默认revision43，旧v1～v40、缺失/null/错类型/未知或重复键拒绝，无补默认、来源回退或热重载，正常导入/烘焙生效，各端同版。

## 【CURRENT STRATEGY】计数与满额

沿原JSON→Reader/Validator→Baker根Settings→Binding/HUD→Panel.Configure→Favorites.Configure设置本绑定。只有FavoritesEnabled与FavoritesCountEnabled同时开启，CountRowCount=1，否则0/隐藏；独立于FavoritesFilterEnabled、原sort/filter/search/details/preferences/preferencesReset。Panel在材料标题、容量之后、模式控件之前绘制只读缓存文字，默认Favorites: 0/64；库存空或交集空仍显示计数行，原Empty/No matching favorites/搜索分类空态规则保持。

计数使用原_names.Count，对已应用的不同FixedString64Bytes Name统计一次，不用显示名、可见行数、库存数量或单位容量。分类/搜索/仅看收藏隐藏的名称、暂不在库存或数量归零的收藏仍占名额；库存重新获得同Name不会重复增加。格式为FavoritesCountLabel + ": " + Count + "/" + FavoritesMaxCount；Count>=Max追加" - "+FavoritesFullLabel，例如Favorites: 64/64 - Favorite limit。合法旧记录256、当前上限64时显示256/64及满额后缀，不截断/删旧收藏；沿原禁止新增、已收藏始终可取消的规则，低于上限后撤销后缀并开放原新增按钮。

## 【CURRENT STRATEGY】缓存、布局与生命周期

Configure先Reset，配置原上限/满额文案和新开关/标题后缓存0/Max；原Preferences合法Restore后按实际集合刷新。GUI仍只排队原收藏Name，下一有效Show由原ListView.Capture调用Favorites.ApplyPending，在最新可见行确认并消费实际变化，Revision变化后更新CountText；拒绝/隐藏/消失请求不改变计数。ResetDisplay清启用非空集合后更新0/Max，原空集合无需重建；Reset清新开关/文案/文字。没有每次GUI拼接计数或新增集合遍历，未通过性能采样证明具体开销。

CountRowCount共用于DrawBody显示、滚动内容总行数和ContainsSearchField的(3+CountRowCount+ControlRowCount)*RowHeightPixels。原三类模式ControlRowCount保持0～3；计数开启时额外下移搜索框一行。计数始终固定单行，不因满额或数值变化增减高度；原标题/面板矩形/页脚、详情实际高度及原焦点/键盘/指针代码保持；原操作行固定高度，保护时提示内容归[收藏保护](MapInventoryFavoritesDropProtection.md)。未实际触发的长标题/后缀、最小几何的字体、裁切与分辨率仍UNKNOWN，未自动换行或引入资源。

Close/B关闭、逐帧Clear、无效ReadInput沿原隐藏/清未应用请求规则，同一绑定已应用集合和计数关闭重开保留；绑定Reset/死亡/断线/地图源或玩家变化、World/Scene释放沿原提交已观察偏好再清缓存。新绑定按配置和合法偏好恢复；收藏关闭不应用文件数组，计数关闭仍保留原收藏行为/记录。

## 【FACT】保存与业务边界

计数由集合计算，不增加本机偏好字段或写入入口；Preferences/Data/Store保持独立v4九字段，严格v1五字段/v2六字段兼容和原路径、0.5秒unscaledTime、.tmp/Flush(true)/Replace或Move、失败暂停当前绑定、不自动重试/修坏档规则。载入/库存刷新/计数显示本身不写盘，实际收藏变化仍经原Revision观察保存。全局偏好关闭或本绑定I/O暂停时，按当前临时已应用集合显示。

不新增快捷键/PlayerInput/RPC/Ghost、库存/工具/反馈或玩家v4/世界v2字段；完整容量、木石配方、制作/修理/升级资格、服务端丢弃资格、已提交请求及SavePrepared事务沿原链；本机丢弃行与未消费请求保护归[收藏保护](MapInventoryFavoritesDropProtection.md)。计数关闭只关闭显示，不能增加收藏名额或改变物品数量。

## 【CURRENT STRATEGY】配方分类、搜索与偏好关联

当前v41/revision44的[配方偏好](MapInventoryRecipePreferences.md)把配方类别与已应用关键词接入本机偏好v4九字段；独立保存开关、严格v1/v2/v3迁移、关闭项保留和重置/延迟/失败规则归专题。材料与工具状态、原输入/Ghost及服务器事务/玩家和世界档案保持；本阶段待人工。搜索用户通过仍限v40/revision43十六项，分类仍限v39/revision42原清单，其他旧通过保持原范围，未触发独立用例仍UNKNOWN。

## 【KNOWN ISSUES】静态核对与人工边界

以下计数静态证据及人工通过均限v35/revision38阶段。该阶段正常Unity编译/重载及元数据核对通过：DTO十四bool、六float、三int、两模式string、59文案string及一文件ID，共85；Settings十四byte、六float、三int、两byte枚举及60 FixedString64Bytes，共85、零GhostField。原Favorites普通类/GUID、CountText字符串和CountRowCount整数元数据、偏好CurrentVersion=3/Data七字段及原协议保持。3084份非法配置全部拒绝（每地图1542），202组合法读取（每地图101）通过，包含85字段完整/形状/类型/重复键、计数严格bool、文案空白/控制/61字节与关闭仍校验、旧v1～v34/未来版本及全部原规则；只配置读取和元数据，不调用计数/GUI/偏好业务。

两地图各101次、共202次隔离Editor Bake通过；保留原87变体，新增计数关闭、收藏/面板关闭、计数/满额文案ASCII61/UTF-8 61/中文、收藏筛选/排序/分类/搜索/详情/偏好/重置关闭仍配置计数、计数与收藏同时关闭。全部85Settings、原Settings/零反馈/Prefab引用、资源布置/兼容签名匹配：Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。源SubScene只读，临时克隆/TextAsset/Scene/World/BlobAssetStore释放；主场景干净、3根对象、单场景，未Play。

执行前Console[0 Error,3 Warning,27 Log]；正常编译后/Bake前后/最终均[0 Error,5 Warning,27 Log]，新增两条既有PEListener UAC1001与DOTween编辑器CS0618警告，原三条NetCode Tick Batching警告保留；未清空Console，不把旧运行日志视为当前性能结论。七现有C#脚本、两JSON接入，无新脚本/meta/组件/挂载或Scene/SubScene/Prefab/Animator/资源/字体/包/构建结构变更。原输入19/DropGhost4/Tools3/F4/G7/资源状态4/世界保存3及全部反馈、玩家v4根7/工具项3、世界v2根9/掉落项8和本机偏好v3七字段/业务事务保持。

原499项人工内容/编号逐字保留，追加十六项后515项，归[运行入口](Runtime.md)。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；筛选旧通过限v34/revision37清单，其他旧阶段保持原范围。未实际触发的独立计数/满额/超限/缓存时序、关闭/重绑、偏好实际读写/故障、GUI搜索命中/滚动/字形/裁切/分辨率/多人/预测，以及性能/平台/线上仍UNKNOWN。

AI未执行计数/Favorites/ListView/Panel/Preferences/GUI业务、真实偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、采样或图片，未创建子Agent、暂存或提交Git。

## 【FACT】当前收藏保护边界

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：计数缓存、CountRowCount/布局与Favorites代码保持；保护按实际Name查询，不读取CountText或名额。favoritesCountEnabled=false只隐藏计数，仍可按启用收藏保护；原计数人工通过仍限v35/revision38十六项。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

v37/revision40提示阶段的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。

## 【CURRENT STRATEGY】收藏材料消耗确认

当前v41/revision44的[收藏材料消耗确认](MapInventoryFavoritesConsumptionConfirm.md)仅处理原B七个制作/修理/容量与工具升级按钮。首次有效按钮请求命中已应用收藏木石的正成本时暂存一个操作，在配方内显示数量提示，以Confirm/Cancel替换原按钮行；确认按最新已捕获候选和本地Revision复核后沿原请求提交一次，取消/关闭B/相关数量、等级、耐久、配方或已应用收藏变化/绑定失效清待确认。逐帧Clear仅隐藏，确认目标或高度变化清旧面板与行鼠标许可。确认独立于原消耗提示开关；数字1～7保持原直达链，偏好v4九字段、输入19、Ghost/服务器与保存入口保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，已验收提示仍限v37/revision40，全部旧通过保持原版本/清单。
