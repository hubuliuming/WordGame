# 背包仅看收藏筛选

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[列表](MapInventoryListView.md)、[收藏](MapInventoryFavorites.md)、[搜索](MapInventorySearch.md)、[详情](MapInventoryDetails.md)、[本机偏好](MapInventoryPreferences.md)、[配置](DataResources.md)与[运行验收](Runtime.md)。入口CombatPrototypeNetCode、原B面板；两地图Json/BuiltIn当前v38/revision41。主线程按已确认方案完成接入及静态范围核对；用户已确认本阶段十六项人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v34/revision37及运行入口清单，人工结论来自用户反馈；收藏旧通过限v33/revision36十六项，其他旧阶段保持各自版本/清单。

## 【FACT】入口与文件

| 文件 | 当前职责 |
|---|---|
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 新控制行、空态优先级、Reset/无效输入和鼠标许可接入 |
| [ListView](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelListView.cs) | 新模式/待请求、可见交集、当次取消后移除及共享布局行数 |
| [Favorites](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelFavorites.cs) | 原Name集合、置顶/容量/标记及共享IsFavorite查询 |
| [Preferences](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferences.cs)、[Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesData.cs)、[Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs) | 已应用新模式观察、v3严格协议/v1与v2兼容及原提交/失败边界 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 六项必填接口与本地固定设置 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 完整校验、建议值和原地图根Settings映射 |
| [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) | 显式Json配置，与BuiltIn一致 |

沿十一现有C#脚本及两地图JSON接入，没有新脚本/helper/meta/组件或挂载。原Details、HUD/Binding/PlayerInput和服务器/玩家世界保存链代码保持；Scene/SubScene/Prefab/Animator/资源/字体/包/构建结构保持。

## 【FACT】配置与建议值

| inventoryPanel字段 | 两地图Json/BuiltIn值 | 校验及Settings |
|---|---|---|
| favoritesFilterEnabled | true | 必填严格bool→byte |
| defaultFavoritesOnly | false | 必填严格bool→byte；新能力关闭时显示强制false |
| favoritesFilterLabel | Favorites | 必填文案→FixedString64Bytes |
| favoritesFilterAllLabel | All items | 必填文案→FixedString64Bytes |
| favoritesFilterOnlyLabel | Favorites only | 必填文案→FixedString64Bytes |
| noMatchingFavoritesLabel | No matching favorites | 必填文案→FixedString64Bytes |

原83字段保留，[收藏计数](MapInventoryFavoritesCount.md)、[收藏保护](MapInventoryFavoritesDropProtection.md)与[收藏提示](MapInventoryFavoritesConsumptionHint.md)各追加两字段，另有[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)四字段，当前93：DTO十七bool、六float、三int、两模式string、64文案string及一文件ID；Settings十七byte、六float、三int、两byte枚举及65 FixedString64Bytes，零GhostField/无GhostComponent。四文案非空白、无控制字符、最多61 UTF-8字节；关闭收藏/新筛选/面板仍全量验证。仅地图schema38、正revision/seed；默认revision41，旧地图v1～v37、缺失/null/错类型/未知或重复键拒绝，无补默认/来源回退/热重载，正常导入/烘焙生效，各端同版。

## 【CURRENT STRATEGY】显示交集与点击

材料标题/容量之后，沿原Sort/Filter控件再绘制全宽Favorites控制行，随后搜索和原Reset view。默认All items，点击只QueueFavoritesFilter；下一有效Panel.Show捕获完整Snapshot后，由ListView.Capture先应用搜索/原模式/新待切换一次，再建立分类×搜索×收藏条件交集。只用真实FixedString64Bytes Name判断收藏，不用显示名/行索引；原排序与稳定收藏置顶规则保持。

可见行建立后消费原待收藏Name；不在最新交集/消失的请求丢弃。仅看收藏中取消当前Name，随后原地移除不再收藏的行，在同一次有效刷新关闭被隐藏的详情，再观察已应用偏好。完整库存空沿原Empty；库存非空但仅看收藏交集为空优先No matching favorites；全部模式保留原搜索/分类无匹配文案。隐藏材料仍计容量、木石配方与原业务资格，数量归零的收藏记录保留，再获同名按当前交集显示。

新模式实际变化归零滚动并取消旧面板/行鼠标按下许可，原排序/搜索许可规则保持；收藏实际变化、行身份及详情高度检查继续沿原。筛选切换不清已排队业务；未消费丢弃请求另按[收藏保护](MapInventoryFavoritesDropProtection.md)复核，制作/修理/两升级及已提交服务器动作保持；不增加快捷键、RPC/Ghost或服务端条件。ControlRowCount纳入筛选行（总0～3）；绘制内容高度与ContainsSearchField还共用计数CountRowCount0～1，收藏筛选和计数各自开启时分别使搜索文本框下移一行。面板矩形、固定标题/页脚、原操作行高度与焦点/键盘隔离代码保持，收藏保护行内容及消费前复核归保护专题。

## 【CURRENT STRATEGY】开关、重置与生命周期

FavoritesEnabled与FavoritesFilterEnabled须同时开启才显示/应用新筛选；任一关闭时隐藏新行、FavoritesOnly=false，仍保留原分类/搜索。新模式独立于分类FilterEnabled，分类关闭仍可仅看收藏。显示Reset在原Snapshot之后清未应用请求、恢复DefaultFavoritesOnly（能力关闭强制false），并沿原恢复默认排序/分类、清查询/焦点/详情和启用收藏；默认true配合清收藏可显示无匹配收藏。

Close/原无效ReadInput额外清未应用新模式，已应用模式同一绑定关闭重开保留；逐帧Clear只隐藏。绑定Reset先沿Close提交已观察待写，再清全部本地缓存；死亡/断线、地图源/玩家或World/Scene变化沿原绑定释放，新有效绑定按配置/合法偏好恢复，不保存滚动/焦点/详情或面板开关。

## 【FACT】本机偏好v3与兼容

沿Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json，本机/地图隔离。v3恰七字段：整数version=3、mapDefinitionId、sortMode、filterMode、searchText、favoriteItemNames及严格bool favoritesOnly；原模式/搜索/收藏完整校验保持，v3布尔缺失/null/错类型、未知/重复字段、额外内容、非法收藏或地图/版本错误整档拒绝。

严格兼容v1五字段：保留模式/搜索、空收藏、favoritesOnly=false；严格兼容v2六字段：保留模式/搜索/收藏、favoritesOnly=false；二者仅内存迁移v3。加载或首次缺文件不创建目录/写盘，下一实际已应用偏好变化才写v3。首次缺文件按当前DefaultFavoritesOnly初始化；合法旧文件按迁移false恢复，而不是用新默认覆盖历史记录。

协调类只在有效新能力开启时恢复/观察已应用布尔值，与上次成功写入值比较，连续变化复用0.5秒unscaledTime合并，Close/Reset提交。返回已写值取消对应待写；未应用GUI请求、库存刷新或载入不写盘。新功能关闭时其他偏好保存保留已读flag；preferencesEnabled=false或本绑定I/O暂停仅临时生效。UTF-8严格读可带BOM、写无BOM的.json.tmp、Flush(true)、Replace或首次Move及失败暂停本绑定I/O保持；不自动重试/修坏档，不改玩家v4/世界v2路径、字段或事务。

## 【KNOWN ISSUES】静态核对与人工边界

以下静态证据及筛选人工通过均限v34/revision37阶段。该阶段正常Unity编译/重载及元数据核对通过：DTO十三bool、六float、三int、两模式string、58文案string与一文件ID，共83；Settings十三byte、六float、三int、两byte枚举、59 FixedString64Bytes，共83，零GhostField。偏好CurrentVersion=3/Data七实例字段、严格FavoritesOnly布尔类型已反射核对；原普通类身份和GUID保持。2986份非法配置全部拒绝（每地图1493），174组合法读取（每地图87）通过，含两地图Json/BuiltIn等价、83字段完整/类型/重复键、两新严格bool、四文案边界及关闭仍校验、旧v1～v33/未来版本与全部原规则。仅配置Reader/Validator及元数据检查，不执行列表/GUI/偏好业务或I/O。

两地图各87次、共174次隔离Editor Bake通过；保留原73变体，新增筛选关闭/默认仅看收藏/收藏或面板关闭且默认true、四文案ASCII61/UTF-8 61/中文、原分类/搜索/偏好关闭仍开启筛选、全部原控件关闭、详情关闭及自定义默认模式组合。全部83Settings和原Settings/零反馈/Prefab引用、布置及兼容签名匹配：Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。源SubScene只读，临时克隆/TextAsset/Scene/World/BlobAssetStore释放；主场景干净、3根对象、单场景、未Play。

执行前Console[0 Error,7 Warning,106 Log]；正常编译后及Bake前后均[0 Error,8 Warning,106 Log]，新增一条既有DOTween编辑器代码的FindObjectsOfType弃用CS0618警告，原五条NetCode Tick Batching、MCP WebSocket与PEListener UAC1001警告保留。未清空Console，不将旧运行日志作为当前性能结论。原输入19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3和全部反馈、玩家v4根7/工具项3、世界v2根9/掉落项8及业务事务保持。

原483项人工内容/编号逐字保留，追加十六项后499项，归[运行入口](Runtime.md)。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v34/revision37及运行入口本阶段清单；人工结论来自用户反馈。此前收藏通过限v33/revision36清单，其他旧范围保持。未实际触发的独立GUI/交集/当次取消/鼠标时序、偏好v1/v2迁移/v3真实I/O/故障、关闭/重绑、字形/分辨率/多人/预测用例，以及性能/平台/线上仍UNKNOWN。

AI未执行ListView/Favorites/Panel/Preferences/GUI业务、真实偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、采样或图片，未创建子Agent、暂存或提交Git。

## 【FACT】当前收藏计数边界

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)独立于FavoritesOnly/FavoritesFilterEnabled；切换分类/搜索/仅看收藏不修改全量_names计数。仅看收藏中取消收藏，在原ApplyPending实际变化后更新计数，同次Capture仍移除行并关闭隐藏详情。ListView及Preferences代码/协议保持，本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；本专题筛选通过仍限v34/revision37清单。

## 【FACT】收藏保护与仅看收藏

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：收藏筛选所用ListView与本机偏好代码保持；仅看收藏不自动解锁Drop/All。过滤/隐藏不删Name，当前已应用集合由DropClient共享查询；取消收藏仍按原同次移除行与关闭隐藏详情，之后消费未提交丢弃按更新后的集合复核。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

v37/revision40提示阶段的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。

## 【CURRENT STRATEGY】收藏材料消耗确认

当前v38/revision41的[收藏材料消耗确认](MapInventoryFavoritesConsumptionConfirm.md)仅处理原B七个制作/修理/容量与工具升级按钮。首次有效按钮请求命中已应用收藏木石的正成本时暂存一个操作，在配方内显示数量提示，以Confirm/Cancel替换原按钮行；确认按最新已捕获候选和本地Revision复核后沿原请求提交一次，取消/关闭B/相关数量、等级、耐久、配方或已应用收藏变化/绑定失效清待确认。逐帧Clear仅隐藏，确认目标或高度变化清旧面板与行鼠标许可。确认独立于原消耗提示开关；数字1～7保持原直达链，偏好v3七字段、输入19、Ghost/服务器与保存入口保持。本阶段十六项人工GamePlayer待验收，已验收提示仍限v37/revision40，全部旧通过保持原版本/清单。
