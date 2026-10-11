# 材料背包与制作面板

返回[背包与道具](Inventory.md)和[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的本地库存列表、工具耐久、配方预览、制作按钮及客户端输入隔离。制作资格、材料/工具事务和成功反馈仍归[采集工具](MapGatherTools.md)，存档归[资源与数据](DataResources.md)，人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [MapInventoryPanelConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs) | inventoryPanel 的严格 JSON DTO |
| [InventoryPanelData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原地图根上的固定显示 Settings；无 GhostField/新玩家数据 |
| [InventoryPanelSnapshot](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSnapshot.cs) | 完整库存原顺序缓存、材料计数及本地行Revision |
| [ListView](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelListView.cs) | [排序筛选](MapInventoryListView.md)的可见行、顺序及本地选择缓存 |
| [InventoryPanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | GUI 列表/滚动/按钮、本地开关、待提交按钮请求与偏好协调入口 |
| [配方筛选](MapInventoryRecipeFilter.md) | Panel持有普通分类辅助类；七配方显隐、默认值与待切换 |
| [本机偏好](MapInventoryPreferences.md) | 原Panel持有普通协调类，按地图读取/保存已应用展示选择 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原根追加 InventoryPanelSettings 烘焙数据 |
| [HUD 绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 所属玩家/连接检查、配置、投影与输入交接 |
| [HUD 宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) | 复用 Main Camera 原组件，委托绘制并共享原制作反馈 |
| [PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) | B 本地开关、按钮与原1/2合并、面板指针隔离 |

新 Snapshot/Panel 是普通 C# 类，没有新 MonoBehaviour 挂载。v9面板阶段四个脚本及对应meta由正常Unity导入生成。主场景、SubScene、Prefab、Animator、旧 meta、资源绑定、图片/字体、包和构建设置保持。v9面板接入只增加显示烘焙数据；v10丢弃另有输入与玩家所属反馈布局变化，归[丢弃](MapInventoryDrop.md)；v11新增所属G显示数据，归[拾取提示](MapPickupHud.md)，各端须同版代码/配置及重新烘焙。正式 Map 的 QFramework 背包/99拆格与道具详情保持原边界。

## 【FACT】JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) 与 [BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) 当前一致为 schemaVersion=42/configRevision=45。必填 inventoryPanel 共118字段，原30字段加[排序筛选](MapInventoryListView.md)十四、[搜索](MapInventorySearch.md)七、[本机偏好与重置](MapInventoryPreferences.md)六、[材料详情](MapInventoryDetails.md)十四、[收藏](MapInventoryFavorites.md)六、[收藏筛选](MapInventoryFavoritesFilter.md)六、[收藏计数](MapInventoryFavoritesCount.md)两、[收藏保护](MapInventoryFavoritesDropProtection.md)两、[收藏提示](MapInventoryFavoritesConsumptionHint.md)两及[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)四字段及[配方筛选](MapInventoryRecipeFilter.md)七字段及[配方搜索](MapInventoryRecipeSearch.md)七字段及[配方偏好](MapInventoryRecipePreferences.md)两字段及[配方收藏](MapInventoryRecipeFavorites.md)九字段，沿原严格UTF-8、完整字段/类型及未知/重复键检查；即使关闭面板也校验全部字段。旧地图v1～v41明确失败，不补段/默认值或回退来源；正常导入/烘焙后生效，无运行热重载。Lv1配方/定义仍从gatherTools读取，当前级上限/倍率归[gatherToolUpgrade](MapGatherToolUpgrade.md)，面板不复制配置真值。

| 字段 | 默认值 | 契约 |
|---|---|---|
| enabled | true | 仅控制本面板及B/按钮输入；关闭不关闭原1/2制作 |
| initiallyOpen | false | 建立有效地图/本地玩家绑定时的初始开关 |
| panelWidthPixels / panelHeightPixels | 380 / 640 | 有限正数，宽>=12×字号+48，高>=8×行高+48 |
| rightMarginPixels / topMarginPixels | 24 / 64 | 有限非负，宽+右距<=1920、高+上距<=1080 |
| fontSize | 18 | 正整数 |
| rowHeightPixels | 32 | 有限正数，行高>=字号+8 |

| 文案字段 | 默认值 |
|---|---|
| panelTitle | Inventory |
| materialsLabel | Materials |
| capacityLabel / unlimitedLabel | Capacity / Unlimited |
| toolsLabel | Tools |
| craftLabel | Crafting |
| craftButtonLabel | Craft |
| emptyInventoryLabel | Empty |
| woodLabel | Wood |
| stoneLabel | Stone |
| appleLabel | Apple |
| meatLabel | Meat |
| closeLabel | B: Close |
| missingLabel | Missing |
| usableLabel | Still usable |
| brokenLabel | Broken |
| notOwnedLabel | Not owned |
| disabledLabel | Disabled |
| readyLabel | Ready |
| repairLabel | Repair |
| repairButtonLabel | Repair |
| fullDurabilityLabel | Full durability |

全部79文案（上表原22项、排序筛选十项、搜索四项、重置一项、[材料详情](MapInventoryDetails.md)十三项、[收藏](MapInventoryFavorites.md)四项、[收藏筛选](MapInventoryFavoritesFilter.md)四项、[收藏计数](MapInventoryFavoritesCount.md)一项、[收藏保护](MapInventoryFavoritesDropProtection.md)一项、[收藏提示](MapInventoryFavoritesConsumptionHint.md)一项及[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)三项及[配方筛选](MapInventoryRecipeFilter.md)五项及[配方搜索](MapInventoryRecipeSearch.md)四项及[配方收藏](MapInventoryRecipeFavorites.md)六项）须非空白、无控制字符且最多61个UTF-8字节，烘焙为FixedString64Bytes。内置GUI字体和白色纹理复用；当前英文文案，中文接口保留，字体/字形覆盖为UNKNOWN，没有读取图片或界面截图。

## 【CURRENT STRATEGY】库存、工具与配方显示

客户端仅枚举启用GhostOwnerIsLocal的一个玩家，并核对其GhostOwner对应Connected/InGame且无断线请求的连接；没有有效本地Ghost/连接时清空显示。Snapshot按原CombatPrototypeInventoryItem顺序缓存全部正数量条目，[排序筛选](MapInventoryListView.md)与[搜索](MapInventorySearch.md)只生成材料列表的可见行；不设第二份可变库存或99拆格，零数量不列出。材料标题后增加一行总量/上限，受管材料行显示数量/单种上限，关闭限制显示Unlimited，规则归[容量](MapInventoryCapacity.md)。木材/石材/活力苹果/小块肉映射上述Wood/Stone/Apple/Meat文案，其余合法名称直接按原ItemName显示。ToolId/Durability/Level来自原所属工具缓冲，工具独立于库存，两槽显示当前等级/本级最大耐久、未持有或功能关闭状态；预警开启时状态文字与颜色归[耐久预警](MapToolDurabilityHud.md)，关闭时沿原损坏显示。

Snapshot缓存完整显示文本/数量，行内容、数量/顺序或容量等级变化时更新本地Revision；条目名称空白/控制字符、负数量或重复名称按索引/地图/玩家记录错误并跳过该项，其余独立条目继续，当前快照有错误时禁用制作、修理和丢弃按钮，不用展示数据修正库存。原工具网络数据仍由原HUD统一校验。必需地图配置、客户端绑定/宿主缺失明确暴露错误，不查找或创建组件兜底。

两条配方直接读取原definitions：斧头木3/石2、镐子木2/石3；分别列出现有/需要与max(需要-现有,0)缺少数量。只有工具开关启用、库存展示快照合法、材料充足且工具未持有或耐久小于单次成本时按钮可用。Ready仅是当前客户端材料/耐久预览，连接、生命、静止、无攻击、近战Ready、资源预约互斥及同tick优先仍由原服务端制作系统决定；网络快照滞后时服务器可拒绝，不按客户端预览扣料/发工具。工具功能关闭保留已有耐久/等级并禁用按钮；制作/重做满耐久Lv1，损坏高等级工具的按钮明确显示Recraft at Lv1。

面板右上，按min(屏幕宽/1920,屏幕高/1080)等比缩放。标题和共享反馈/关闭按钮固定，中间列表滚动；背景alpha=0.85。缓存条目/配方/按钮文本和GUI样式；GUI绘制后恢复matrix/color/enabled。按钮/滚动参与正常GUI事件，原F提示/进度仍仅Repaint且保持底部320×104。排序、分类及启用的收藏筛选共0～3个完整宽度控制行位于材料标题/容量及启用收藏计数行之后，默认Sort: Type、Filter: All、Favorites: All items；材料搜索开启追加标题及文本框/清空按钮两行，[配方搜索](MapInventoryRecipeSearch.md)独立开启再追加两行，同处原滚动区；其后有可配置重置按钮一行，规则归[偏好](MapInventoryPreferences.md)。内容高度计入可见行、收藏计数行与全部启用控制行。完整库存为空沿原Empty；库存有条目但仅看收藏交集为空显示No matching favorites；全部材料模式中有效搜索无可见行显示No search results，仅分类无匹配显示No matching items。完整容量统计、工具及制作/修理/升级资格不受材料筛选或搜索影响；配方显隐归[配方筛选](MapInventoryRecipeFilter.md)。

排序/筛选按钮仅记录客户端布尔切换，在Show捕获库存后应用一次并滚动归零；无输入/RPC或存档新增字段。可见行身份/顺序/数目变化只清本次行丢弃的鼠标按下许可，防止按下至抬起间重排丢错行；显示切换不清已排队Kind/Mode；消费前另按[收藏保护](MapInventoryFavoritesDropProtection.md)复核，制作/修理/升级沿原完整Snapshot资格。详细规则与v28静态/人工边界归[排序筛选](MapInventoryListView.md)。

## 【CURRENT STRATEGY】输入、请求与生命周期

B单次按下在GhostInputSystemGroup内切换本地面板，同一渲染帧不反复切换；底部B: Close也关闭。开关不入输入命令/存档、不修改Time.timeScale，打开且两个搜索框均未编辑时，WASD、空格、F/G/E/R和镜头Z/X沿原规则；编辑及获得/释放焦点同帧屏蔽WASD、空格、B、E/R/F/G/F5、1～7和镜头Z/X。Enter/Esc结束编辑，之后B可关闭；文本焦点、IME及外部点击规则归[搜索](MapInventorySearch.md)。指针落在当前可见面板的同一缩放矩形内，左键不写Attack，摄像机ReadMove接收经搜索焦点隔离的keyboard与null mouse，使面板滚轮不缩放镜头；矩形外原鼠标攻击/镜头滚轮保持；搜索编辑时用于点击面板外释放焦点的当次左键也被消费，不写Attack。B关闭与鼠标同帧时仍屏蔽原面板内该次按下。

制作按钮只缓存本地Axe/Pickaxe布尔请求，要求鼠标按下来自本次有效绑定的可见面板；MouseUp结束后清理按下标记。输入系统下一次读取时重新核对地图、所属玩家、连接及生命，消费并清空按钮请求，再与数字1/2按下合并到原CraftAxe/CraftPickaxe InputEvent；原制作按钮不增加RPC、命令字段或保存入口；丢弃请求归[丢弃](MapInventoryDrop.md)。原服务端F优先、两类同时斧头优先、忙碌拒绝、仍可用拒绝及SavePrepared先于扣料/工具提交保持。一个按钮请求消费一次，没有自动重试；再次主动点击仍按当前服务器资格处理。

制作成功/拒绝/失败共享原所属Sequence/Kind/Result，当前2秒展示期限由原宿主处理，只用于显示；新绑定只观察现有Sequence，不重播旧反馈。interactionHud.enabled只关闭F提示/进度，inventoryPanel.enabled独立；关闭F HUD仍可B打开本面板/查看制作反馈，关闭本面板仍可用原1/2。F/G文字、面板、高亮及资源状态全关时宿主清空显示，工具玩法由原开关控制。

死亡、断线/无本地Ghost、玩家或地图源变化、关闭面板、World/Scene停止或释放清掉未提交按钮请求；绑定释放同时清库存投影、滚动位置、鼠标按下标记及旧反馈。同一绑定内B或关闭按钮保留已应用的排序/筛选选择与搜索词，清本地未应用编辑/切换并释放文本焦点；重建有效绑定先按initiallyOpen及两默认模式初始化，默认关闭/type/all，原滚动归零；偏好启用且正式文件合法时再恢复已启用能力的选择/搜索词，不恢复开关、滚动或焦点。Close及Reset提交未保存的已应用偏好，未应用编辑/切换不提交。已经消费进原输入命令的请求仍由原服务器链处理，不通过关闭面板撤销已提交制作。面板不保存/修改库存、耐久、世界资源、再生期限、金币/经验或玩家档案，当前玩家v4与合法旧档迁移归[资源与数据](DataResources.md)。

## 【CURRENT STRATEGY】配方分类、搜索、偏好与收藏关联

当前v42/revision45的[配方收藏](MapInventoryRecipeFavorites.md)在原七项操作接入本机收藏/分区置顶，偏好v5十字段严格兼容v1～v4；独立保存、关闭项保留、上限/重置/延迟/失败规则归专题。材料收藏/工具状态、原输入/Ghost及服务器事务/玩家和世界档案保持；本阶段待人工。配方偏好用户通过仍限v41/revision44十六项，搜索仍限v40/revision43十六项，分类仍限v39/revision42原清单，各旧通过保持原范围，未触发独立用例仍UNKNOWN。

## 【KNOWN ISSUES】静态核对与人工边界

v9面板阶段正常Unity编译无C# Error，四个脚本导入、配置/烘焙字段、原输入生成类型与两个普通展示类已核对。Forest/Grassland各覆盖Json默认、BuiltIn默认、Json关闭采矿、工具、F HUD、背包面板、两种显示，共十四次隔离Editor烘焙；schema9/revision12、25个面板值、原工具/HUD/玩家初值均符合契约，Json/BuiltIn一致。所有原布置位置/朝向、资源引用、占地/间距/保护区/敌人初始重叠保持，空间违规0；默认森林/草原矿点20/18、阻挡109/71、树木89/53、采集点36/38，关闭采矿恢复原布局。烘焙Console前后均[0 Error,7 Warning,67 Log]，无新增烘焙警告；主场景干净，临时烘焙World/Scene已释放。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限CombatPrototypeNetCode、schemaVersion=9/configRevision=12及[运行入口](Runtime.md)材料面板十二项清单。人工结论来自用户反馈；未实际触发的精确排版/命中边界、事件顺序、同tick、延迟/预测回放、晚加入和独立配置/网络条目/创建/准备/保存/提交/清理失败仍为UNKNOWN。旧工具v8/revision11十二项用户通过、HUD v7/revision10八项及其他通过保持原版本/清单，不覆盖本阶段新显示/按钮行为。字体、运行性能/带宽、平台构建和线上联调未验证；保存成功后意外ECS故障恢复仍沿工具原未知边界。AI未执行GamePlayer/PlayMode、游戏模拟/GUI显示回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

v10/13在材料行下接入[Drop/All](MapInventoryDrop.md)，当时页脚先丢弃再制作反馈。普通客户端帮助类缓存一个未提交请求；关闭和原绑定失效均清空。原面板尺寸/滚动/鼠标隔离、配方与F显示开关保持；本阶段编译/16次隔离烘焙静态通过，用户确认丢弃人工通过限[运行入口](Runtime.md)v10/13十二项，未触发的独立用例UNKNOWN；不扩展上述v9面板用户通过。

v11/14阶段的[G提示](MapPickupHud.md)与F HUD/本面板三开关独立，共用原宿主/连接及生命周期绑定；关闭本面板仍可显示G，v11时三者全关闭收起绑定。未修改B、列表/按钮、鼠标隔离或制作/丢弃事务，原13输入保持；新显示编译/十次隔离烘焙静态通过；用户确认人工通过限v11/14十项，未触发用例UNKNOWN。

v12/15的[高亮](MapInteractionHighlight.md)复用同一宿主，圆环先于原面板绘制；文字/面板关闭而高亮开启仍绑定本地玩家。原B、鼠标隔离、按钮、输入与反馈保持，新增圆环不消费GUI事件。正常编译/14次隔离烘焙静态通过，用户确认高亮人工通过限v12/15十项，未触发独立用例UNKNOWN；旧面板/G提示用户通过仍限原版本/清单。

v13/16的[资源状态](MapResourceStatusHud.md)与B面板独立，仍复用原宿主/绑定；状态开启时即使其余显示全关仍绑定。新状态不消费GUI事件或制作请求，B按钮与鼠标隔离保持。状态静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN；旧面板/G/高亮用户通过保持原范围。

v14/17修理阶段的[工具修理](MapToolRepair.md)在制作区后追加11滚动行，预览封顶恢复值、材料现有/需要及缺口，3/4按钮沿原鼠标按下标记和输入绑定消费一次。关闭面板或生命周期失效清未提交修理，关闭面板仍可键盘3/4；库存展示错误禁用修理按钮。原380×640/字号18/行32、制作、Drop/All及鼠标隔离保持。配置28字段/20文案、工具根/定义和新反馈经编译/18次隔离烘焙静态通过；用户确认修理人工通过，限v14/17十二项，未触发用例UNKNOWN。

## 【FACT】地图资源存档接入边界

资源存档不增加B页、按钮或本地可变库存；原制作/修理/Drop/All与输入保持。地图恢复完成才接纳玩家，世界文件职责归[资源存档](MapResourcePersistence.md)，资源存档人工通过限v15/18十二项，未触发用例UNKNOWN。 掉落恢复及同文件快照归[掉落存档](MapDropPersistence.md)，人工通过限v16/19十二项，未触发用例UNKNOWN。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：该阶段17输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。

## 【FACT】个人容量等级与升级区

当前[升级](MapInventoryCapacityUpgrade.md)在材料后/工具前增加10滚动行，等级、当前→下一上限、完整配方/缺口、5按钮及本人反馈；Snapshot按实际Level刷新上限，数量未变也更新。数字5/按钮合并为同一一次事件，关闭或原绑定失效清未提交请求；满级/开关关闭/材料不足/投影无效禁用按钮，服务器资格最终判定。原尺寸/滚动/鼠标隔离和其他按钮保持；当前19输入、玩家v3保存等级，新增所属Level及Sequence/Result。22次Bake静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧面板通过限原版本/清单。

## 【FACT】工具升级面板与有效上限

[工具升级](MapGatherToolUpgrade.md)在修理之后追加14滚动行，显示两工具当前/下级、保留耐久与新上限、当前→下一秒数、材料/缺口及6/7按钮、本人2秒反馈。HUD先校验ToolId/Durability/Level，原工具状态/修理预览以有效定义和Level变化刷新，耐久未变也更新最大值；缓存只读且不保存DynamicBuffer跨帧。输入19，B关闭/绑定失效清新增请求，全部显示关闭仍可6/7。工具升级v21/24编译/22次Bake静态通过；用户确认工具升级人工通过限v21/revision24二十二项，未触发用例UNKNOWN；旧面板与背包升级用户通过保持各自原范围。

## 【FACT】耐久预警与剩余次数

沿[gatherToolDurabilityHud](MapToolDurabilityHud.md)的只读两工具帧，状态行显示Low/Critical/Broken并单行着色；各工具下一行显示Uses及整除后的剩余次数，预警/损坏且修理开启提示3/4，损坏且关闭修理提示原1/2 Recraft at Lv1。缺少工具仍Not owned；新开关与gatherTools均开启才追加两滚动行，关闭新开关恢复旧损坏文字/白色且不追加行，工具关闭沿原Disabled。原380×640、字号18/行32、配方/修理/升级/丢弃/反馈及按钮资格保持；ToolLabel绘制后恢复GUI.color，详情行沿原白色。提示仅说明入口，不保证材料或服务器资格。Configure/Reset释放新帧；Durability/Level变化刷新且无跨帧DynamicBuffer。编译/配置/隔离Bake静态通过；用户确认预警人工通过限v22/revision25十六项，未触发的独立排版/字形/命中及生命周期分支仍UNKNOWN。

## 【FACT】G拾取结果与B面板边界

v25/revision28阶段的[拾取反馈](MapPickupFeedbackHud.md)独立显示在原G面板，不进入B页脚或原displayFeedback；本面板列表、按钮、制作/修理/丢弃/升级请求及鼠标隔离保持。关闭本面板仍可显示G结果，G文字或新结果关闭隐藏新消息；原绑定全部显示关闭时仍退出，新开关不强制维持HUD。新四字段所属结果不修改本面板快照或输入19字段；新显示静态及用户人工通过限v25/revision28十六项，未触发独立用例UNKNOWN，旧B通过仍限原版本/清单。

## 【FACT】丢弃后合并与面板边界

v26/revision29的[地面合并](MapDropMerge.md)在已保存激活的丢弃物落地后执行，不改变B列表、Drop/All按钮、原请求/数量或丢弃反馈；原All可产生超99大堆，新链不拆分或钳制它。合并本身不修改库存/工具/等级或追加玩家保存；之后G按[部分拾取](MapDropPartialPickup.md)开关计算本次可接收量，库存投影显示实际入包数量。原面板尺寸/滚动/鼠标隔离及制作/修理/升级保持；合并链静态及用户人工通过限v26/revision29十六项，部分拾取用户人工通过限v27/revision30十六项；未触发独立用例UNKNOWN，旧B通过保持原版本/清单。

## 【KNOWN ISSUES】排序筛选边界

v28/revision31排序筛选已落地，正常Unity编译、1078份非法配置拒绝/62组合法读取、64次隔离Editor Bake及44配置/Settings、0 GhostField静态核对通过。原输入19/G7/其他Ghost字段、玩家v4/世界v2与资源布局签名保持；用户已确认人工GamePlayer通过，范围限v28/revision31及[运行入口](Runtime.md)十六项，主线程结合既有静态核对判定通过；人工结论来自用户反馈，完整边界归[排序筛选](MapInventoryListView.md)。旧面板及部分拾取通过仍限各原版本/清单；AI未执行排序/筛选/库存Capture、面板/HUD/GUI、GamePlayer/PlayMode、逻辑单元测试、构建/发布、真实存档业务I/O、采样/图片或子Agent/Git提交，未实际触发的独立交互/时序、字体/排版/缩放、联网/生命周期及性能仍UNKNOWN。

本阶段搜索配置、输入调用链与静态证据归[搜索](MapInventorySearch.md)：v29搜索阶段51配置/Settings；用户已确认人工GamePlayer通过，限v29/revision32及运行入口十六项，结论来自用户反馈；未实际触发的独立用例UNKNOWN，旧面板与排序筛选通过仍限各自原版本/清单。

v30/revision33保存阶段的[本机偏好](MapInventoryPreferences.md)新增四必填字段，配置/Settings各55；八现有脚本与三普通类、两JSON接入，原HUD/Binding与十九输入不变。正常编译、1612份非法配置拒绝/96组合法读取及98次隔离Editor Bake静态通过，原库存/工具/服务器保存保持；人工十六项已获用户通过反馈，范围见[运行入口](Runtime.md)，未实际触发的独立用例仍UNKNOWN。

v31/revision34重置阶段的显示重置在原GUI排队，在有效Show的Snapshot之后、ListView刷新及Preferences观察之前恢复配置默认模式/空查询，归零滚动、释放焦点并取消旧行按下许可。Close/Reset及无效ReadInput清未应用重置；不调用保存类重建或服务器业务。57字段、严格配置读取和112组隔离Bake静态通过，新增十六项人工已获用户通过反馈，限v31/revision34本阶段清单，未触发独立用例仍UNKNOWN，范围见[运行入口](Runtime.md)。

## 【CURRENT STRATEGY】材料详情

v32/revision35详情阶段在原材料名称行右侧绘制Details按钮；GUI排队真实Name，Show在完整Snapshot/可见行刷新后应用详情，随后原Preferences只观察显示偏好。选择行的原Drop/All行之后展开只读详情；当前收藏开启时先追加收藏控制行，再展开详情，标题与正文自动换行、Close details复用全宽行，滚动高度计入实际测量结果；材料重排保留同一Name，隐藏/消失、显示重置、关闭/无效输入与释放绑定清选择。原每帧Clear仍只隐藏，详情开关关闭恢复整宽材料名称及原行高。完整字段、调用链/点击许可、配方及静态/人工边界归[材料详情](MapInventoryDetails.md)，人工十六项已获用户通过反馈，限v32/revision35及运行入口清单，未触发独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏与置顶

v33/revision36收藏阶段在每个可见材料的Drop/All之后增加一行全宽Favorite/Unfavorite；详情在该行之后展开，收藏关闭则沿原两行布局。GUI按真实Name排队，下一有效Show先捕获完整库存及分类/搜索可见行，再应用收藏，原排序后稳定分为收藏/普通两组，组内保持原顺序；名称仅追加配置标记。实际收藏变化归零滚动并取消旧面板/行按下许可；原排序/搜索许可规则及已排队业务保持。内容基础材料行数为可见数×3（关闭收藏×2），实际详情高度另计。

Panel先配置Favorites再恢复Preferences，ListView使用收藏Revision/待请求刷新缓存，Preferences仅观察已应用值。重置清启用的收藏；Close/无效输入清待请求，同一绑定已应用收藏保留，绑定Reset先提交已观察值再清缓存。收藏阶段77字段及当时本机偏好v2兼容v1归[收藏](MapInventoryFavorites.md)，人工十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN；原详情通过仍限v32/revision35十六项。

## 【CURRENT STRATEGY】仅看收藏筛选

当前v42/revision45在原排序/分类之后、搜索之前接入全宽Favorites控制行，由ListView管理已应用布尔模式与待切换；分类×搜索×收藏条件取交集后沿原排序。仅看收藏取消当前Name的收藏，在同一有效刷新中移除该行并关闭被隐藏详情；完整Snapshot及业务请求仍沿原链。实际新模式变化归零滚动并取消旧面板/行按下许可，原排序/搜索行为保持。Close/无效输入只清待切换，同一绑定已应用模式保留；Reset view恢复DefaultFavoritesOnly并沿原清启用收藏。

FavoritesEnabled和FavoritesFilterEnabled须同时开启，新控制独立于原FilterEnabled；关闭新能力时收藏条件退回全部，分类/搜索仍有效。ControlRowCount及SearchField共享几何纳入新增控制行；偏好v5十字段严格兼容v1/v2/v3，只观察已应用值，关闭功能保留已读favoritesOnly。完整六字段、配置及用户确认人工十六项通过边界归[收藏筛选](MapInventoryFavoritesFilter.md)，旧收藏用户通过仍限v33/revision36清单。

## 【CURRENT STRATEGY】收藏计数行

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)：容量之后、模式控件之前绘制原Favorites缓存CountText；CountRowCount固定为本绑定启用时1、关闭时0，共用于DrawBody、内容高度和ContainsSearchField。启用条件为FavoritesEnabled且FavoritesCountEnabled，独立于收藏筛选；数量按已应用Name集合，达到/超过原上限追加FavoritesFullLabel。计数不增加输入/业务请求或保存字段，配置/生命周期与用户确认人工十六项通过边界归计数专题，限CombatPrototypeNetCode、v35/revision38及运行入口清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料丢弃保护

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：Panel.DrawBody将原Favorites传给DropClient.DrawRow，Panel.ReadInput将同一实例传给ReadRequest；行绘制按真实Row.Name，消费按稳定Kind映射Definition.ItemName读取当前已应用收藏。只替换原操作行内容，不增加行高/输入字段；ClearPending、Close及Reset沿原链，Reset同时清保护开关与文案缓存。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

v37/revision40提示阶段的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。

## 【CURRENT STRATEGY】收藏材料消耗确认

当前v42/revision45的[收藏材料消耗确认](MapInventoryFavoritesConsumptionConfirm.md)仅处理原B七个制作/修理/容量与工具升级按钮。首次有效按钮请求命中已应用收藏木石的正成本时暂存一个操作，在配方内显示数量提示，以Confirm/Cancel替换原按钮行；确认按最新已捕获候选和本地Revision复核后沿原请求提交一次，取消/关闭B/相关数量、等级、耐久、配方或已应用收藏变化/绑定失效清待确认。逐帧Clear仅隐藏，确认目标或高度变化清旧面板与行鼠标许可。确认独立于原消耗提示开关；数字1～7保持原直达链，偏好v5十字段、输入19、Ghost/服务器与保存入口保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，已验收提示仍限v37/revision40，全部旧通过保持原版本/清单。
