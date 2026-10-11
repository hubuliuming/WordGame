# 背包材料排序与筛选

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[容量](MapInventoryCapacity.md)、[丢弃](MapInventoryDrop.md)、[配置](DataResources.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode，Forest/Grassland Json/BuiltIn当前schemaVersion=42/configRevision=45；下述排序筛选静态及人工结论来自v28/revision31阶段。用户已确认该阶段人工GamePlayer通过，主线程结合既有静态核对与用户反馈判定通过，限v28/revision31及运行入口十六项，人工结论来自用户反馈。旧部分拾取用户通过限v27/revision30十六项，旧面板/丢弃/容量/工具通过保持原版本/清单。

## 【FACT】文件与配置

| 职责 | 实际文件 |
|---|---|
| 严格JSON字段、原显示Settings | [MapInventoryPanelConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[PanelData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) |
| 版本/语义、默认值及原Baker | [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 完整库存与本地行Revision | [Snapshot](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSnapshot.cs) |
| 本地排序/筛选/可见行与选择 | [ListView](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelListView.cs) |
| 现有GUI、Show与行点击许可 | [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) |
| 显式来源 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

v28排序筛选阶段七个现有脚本接入，新ListView是普通客户端C#类，同文件含两个byte枚举；其meta由Unity正常导入生成，没有新MonoBehaviour/组件类型或挂载。主场景/SubScene/Prefab/Animator、旧meta/资源/字体、包/构建配置保持；v28时原HUD宿主/绑定整体传递Settings，代码不变；当前[搜索](MapInventorySearch.md)沿原绑定新增本地blocksKeyboard输出。正式Map的QFramework背包/99拆格与网络原型独立。

以下为inventoryPanel新增字段摘录，原三十字段仍必填：

```json
{
  "sortEnabled": true,
  "filterEnabled": true,
  "defaultSortMode": "type",
  "defaultFilterMode": "all",
  "sortLabel": "Sort",
  "originalOrderLabel": "Original",
  "typeOrderLabel": "Type",
  "quantityOrderLabel": "Quantity",
  "filterLabel": "Filter",
  "allFilterLabel": "All",
  "resourcesFilterLabel": "Resources",
  "suppliesFilterLabel": "Supplies",
  "otherFilterLabel": "Other",
  "noMatchingItemsLabel": "No matching items"
}
```

当前配置与原Settings各118字段：DTO二十四bool、六float、五int、三模式string、79文案string及一文件ID；Settings二十四byte、六float、五int、三byte枚举及80 FixedString64Bytes；搜索七字段归[搜索](MapInventorySearch.md)，偏好四字段及重置两字段归[本机偏好](MapInventoryPreferences.md)，0 GhostField且无GhostComponent。sortEnabled/filterEnabled只控制两种展示能力；关闭排序强制original并隐藏排序按钮，关闭筛选强制all并隐藏筛选按钮，两者关闭时分类为all、排序为original，仍应用搜索和启用的独立收藏条件。地图Settings仍保存已配置默认模式，强制显示模式在客户端Configure及显式ResetDisplay应用。

defaultSortMode仅original/type/quantity，defaultFilterMode仅all/resources/supplies/other，大小写与空格严格匹配，不修剪/转换未知值。两模式ID通过共用Resolver校验/烘焙；十新文案沿原非空白、无控制字符且最多61 UTF-8字节。关闭任一能力、面板或全部显示仍检查全部字段与语义。原Reader保留UTF-8/完整对象、缺失/null/错误类型/未知或重复键检查；当前仅schema42，revision/seed须正数，旧v1～v41拒绝，无补默认、来源回退或运行热重载。正常导入/烘焙后生效，各端同版代码/配置并重新烘焙。

## 【CURRENT STRATEGY】完整库存与显示投影

原绑定枚举启用GhostOwnerIsLocal且Connected/InGame的有效所属玩家，Panel.Show调用Snapshot.Capture校验完整原库存。Snapshot继续按原顺序缓存正数量Name/OriginalName/DisplayName/Quantity/Text；零条目不显示，木/石材料数量及受管三种材料总量/本级上限仍基于全部库存。名称空白/控制字符、负数量或重复名称沿原索引/地图/玩家日志跳过独立条目，InventoryValid=false继续限制所有原业务按钮；筛选隐藏该条目不能恢复合法性。

Snapshot的uint Revision仅为本地展示缓存：行名/数量/顺序/行数或容量等级改变时在Capture更新一次；等级变化仍沿原清行及文本重建，Reset归零。ListView消费该Revision、搜索Revision、收藏Revision/待请求、本地选择变化或显式显示重置才重建可见行，复用列表、比较委托及文字缓存；没有复制可变游戏库存、LINQ集合转换、服务器库存写入、奖励或保存调用。未变化时原可见行继续使用，GUI不执行排序。

类型顺序固定为木材、石材、活力苹果、小块肉、其他。依据原ItemName比较，不按可配置显示文案或本地语言排序；其他合法名称按OriginalName的StringComparer.Ordinal升序。quantity为正数量降序，用CompareTo避免相减溢出；相同数量按类型，再按原名确定顺序。original保留Snapshot原行顺序，各模式只作用于客户端副本，原缓冲顺序不改。分类、[搜索](MapInventorySearch.md)与启用的[收藏筛选](MapInventoryFavoritesFilter.md)条件取交集后才按原模式排序；随后[收藏](MapInventoryFavorites.md)稳定分组置顶，两组内部顺序保持。清空搜索不清当前模式或收藏，隐藏的收藏不强制显示。

| 筛选ID | 实际条目 |
|---|---|
| all | 所有合法正数量行 |
| resources | 原ItemName木材、石材 |
| supplies | 原ItemName活力苹果、小块肉 |
| other | 上述四名称以外的合法正数量行，保持原名 |

筛选只影响材料列表，工具不是库存材料行，不加入筛选。完整容量行、工具/耐久、配方/缺料、背包升级、工具修理/升级继续读取原Snapshot；例如只看资源时苹果仍占容量，只看补给时隐藏的木/石仍可供原制作与修理。新规则不改变服务器资格、库存/耐久/等级、F/G拾取与SavePrepared事务。

## 【CURRENT STRATEGY】控件、点击与生命周期

复用原380×640面板、边距右24/顶64、字号18/行高32、1920×1080比例、背景0.85、原标题/页脚/滚动区及鼠标隔离。材料标题、完整容量及启用收藏计数行后有0～3个全宽控制行，分别默认Sort: Type、Filter: All与Favorites: All items；滚动内容基础高度按max(可见数×3,1)计算材料行（收藏关闭时×2），加启用收藏计数行、模式/搜索/重置控制行及原固定区域，详情实际展开高度另计。每个可见行保留原操作行高度；支持且已应用收藏的Name在[收藏保护](MapInventoryFavoritesDropProtection.md)启用时改为只读提示，其余按原Drop/All或Unavailable规则。不改面板矩形、宿主或任何场景资源结构。

GUI按钮仅QueueSort/QueueFilter/QueueFavoritesFilter，保持原有效鼠标按下来源检查，每种本地请求在下一Show/Capture最多应用一次；原列表在同次绘制中保持稳定。排序循环type→quantity→original→type，分类循环all→resources→supplies→other→all，收藏条件在All items/Favorites only间切换；三个本地控件可同次应用，变化后更新缓存标题并滚动归零。不写输入事件/RPC、玩家Ghost或游戏存档；已应用模式经[本机偏好](MapInventoryPreferences.md)协调类保存。完整库存为空沿原Empty；库存非空且仅看收藏交集为空优先No matching favorites；全部模式没有有效搜索而分类无可见行使用No matching items，有有效搜索而交集无可见行使用No search results，不更改容量或配方显示。

原丢弃DrawRow依照传入的真实Row.Name解析原稳定Kind/Mode，不把可见行索引当库存索引。鼠标按下时记录独立行许可；原排序/分类导致可见行名/顺序/数目改变时取消尚未抬起的行许可；收藏条件实际变化额外取消面板许可，MouseUp清许可，新主动点击按当前行处理。相同身份/顺序下纯文本或数量更新沿原资格。已排队Kind/Mode保持，排序/筛选不撤回已经消费或提交的原请求；原制作/修理/升级鼠标许可与服务器资格保持。

B/关闭按钮沿原Close清未提交业务请求及本地未应用模式请求、滚动归零；同一绑定已应用选择及搜索词保留，未应用搜索编辑回退并释放焦点。死亡/断线、无有效所属玩家/连接、源或玩家变化及World/Scene停止释放沿原Reset清行/模式/请求/文字与按下许可。有效绑定重建先按配置defaultSortMode/defaultFilterMode/defaultFavoritesOnly及initiallyOpen初始化；偏好启用且正式文件合法时，只恢复已启用能力的模式，面板开关仍按配置。缺少必需依赖沿原明确错误边界，不查找或创建组件/默认配置兜底。选择为本机显示状态；已应用选择可按地图保存并在新绑定读取，失败与关闭能力边界归[偏好](MapInventoryPreferences.md)。

## 【CURRENT STRATEGY】配方分类、搜索、偏好与收藏关联

当前v42/revision45的[配方收藏](MapInventoryRecipeFavorites.md)在原七项操作接入本机收藏/分区置顶，偏好v5十字段严格兼容v1～v4；独立保存、关闭项保留、上限/重置/延迟/失败规则归专题。材料收藏/工具状态、原输入/Ghost及服务器事务/玩家和世界档案保持；本阶段待人工。配方偏好用户通过仍限v41/revision44十六项，搜索仍限v40/revision43十六项，分类仍限v39/revision42原清单，各旧通过保持原范围，未触发独立用例仍UNKNOWN。

## 【KNOWN ISSUES】静态与人工边界

以下静态核对仅记录v28/revision31排序筛选阶段，当前搜索证据归[搜索](MapInventorySearch.md)。该阶段正常Unity脚本编译完成，无Error；初始Console[0 Error,5 Warning,3 Log]，两次正常刷新后Bake前后同[0,4,3]。未主动清空Console，当前四条为Package/Input Manager、PEListener及MCP WebSocket警告；新JSON导入没有Error记录。ListView与byte枚举已在当前程序集加载，44配置/Settings、0 GhostField与非MonoBehaviour属性通过反射核对。

1078份非法配置全部拒绝（每地图539），62组合法读取通过（每地图31）：完整44字段缺失/null/数组/对象/错误标量/重复键、根错误/未知键/额外内容、十四新字段非有限字面量、布尔数字或字符串、未知/大小写/空格模式、空白/控制字符/超61字节文案、关闭仍必填/验证、旧v1～v27与原布局/部分拾取/合并约束。合法含默认Json/BuiltIn等价、三排序×四筛选、单独/同时关闭、面板/全部显示关闭、初始打开、61字节ASCII/中文边界、原最小几何、其他功能关闭及定义/等级换序。仅配置读取/语义校验，不调用排序/筛选/Capture或GUI作为单元测试。

两地图各32次，共64次隔离Editor Bake通过：默认Json/BuiltIn、两能力开关/组合与十二模式组合、面板/全部显示/初始打开、61字节和中文文案、关闭能力仍配置其他模式、容量/部分拾取/合并/产出丢弃/世界与地面保存/G展示关闭及容量定义/等级换序。全部44字段、原所有Settings/反馈/Prefab引用、完整布置与资源签名匹配。Forest仍89树/36采集/20矿/109阻挡，Grassland53/38/18/71；只读源SubScene、临时TextAsset/克隆/Editor场景/World/BlobAssetStore释放，原主场景干净、未Play、一场景三根对象。

输入19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3及原结果字段，玩家v4根7/工具项3与世界v2根9/掉落项8保持。原服务器采集/拾取/丢弃/制作/修理/升级/保存、Ghost Serializer、源绑定和资源结构代码保持；没有新增网络选择/存档布局或业务测试。

原387项人工清单内容/编号逐字保留，追加本阶段十六项后共403项，归[运行入口](Runtime.md)。用户已确认本阶段人工GamePlayer通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v28/revision31及十六项清单，人工结论来自用户反馈；未实际触发的独立排序/分类/循环、完整统计/按钮、重排按下抬起、快照/GUI/网络时序、字体/排版/缩放、关闭重开/重绑生命周期及性能/平台/线上仍UNKNOWN。旧部分拾取用户通过限v27/revision30十六项，其他旧通过保持原范围。AI未执行ListView排序/筛选/Capture、库存Capture、面板/HUD/GUI、GamePlayer/PlayMode、逻辑单元测试、真实存档业务I/O、命令行构建/发布、采样或图片，未创建子Agent/提交Git。

v29/revision32搜索阶段只进一步筛选材料可见行并控制本地文本输入焦点；v30/revision33保存阶段的[偏好保存](MapInventoryPreferences.md)人工已获用户通过反馈，限运行入口本阶段十六项，未实际触发的独立用例仍UNKNOWN。v28/revision31排序筛选用户通过不覆盖新搜索；用户已确认搜索人工GamePlayer通过，限v29/revision32及[运行入口](Runtime.md)十六项，结论来自用户反馈；未实际触发的独立用例UNKNOWN。

v31/revision34重置阶段显式重置在原Panel.Show中调用ResetDisplay：清未应用模式请求，恢复当前配置默认排序/分类，关闭能力仍original/all，令_hasSnapshot=false并更新文案；随后原Capture重建可见行，不修改原库存。搜索词/焦点及保存边界归[偏好](MapInventoryPreferences.md)，重置阶段静态通过、人工十六项已获用户通过反馈，限本阶段版本/清单，未触发独立用例仍UNKNOWN。

## 【FACT】材料详情的只读类别

v32/revision35详情阶段新增CategoryLabel，只读复用原TypeRank及配置Resources/Supplies/Other文案；排序/筛选规则、缓存和库存保持。详情按真实Name匹配当前可见行，排序保持同一材料，分类/搜索隐藏时清选择；数量与配方不从可见行总量推导。本阶段十六项人工已获用户通过反馈，限v32/revision35及运行入口清单；未触发独立用例仍UNKNOWN，完整规则与验收边界归[详情](MapInventoryDetails.md)。

## 【CURRENT STRATEGY】收藏分组与行身份

v33/revision36收藏阶段在最新分类/搜索可见行建立后消费收藏请求，只有当前仍可见的合法正数量Name可切换。原type/quantity排序或original顺序完成后，Favorites用复用列表稳定分组，并缓存可见收藏名称的标记文本；未变化快照继续复用，不在GUI排序。收藏隐藏/归零/消失仍保留Name，再次出现参与同一规则。收藏切换实际改变时归零滚动并取消旧面板/行按下许可；列表身份变化仍沿原行许可检查，已排队业务不撤回。完整保存/上限与人工边界归[收藏](MapInventoryFavorites.md)，人工十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏条件与刷新

当前v42/revision45增加独立FavoritesOnly布尔值；GUI QueueFavoritesFilter只排队，下一有效Capture在搜索/原模式应用后切换一次。先建立最新分类×搜索×收藏条件可见行，再消费同Name收藏请求；仅看收藏时再原地移除取消收藏的行，随后原排序、稳定收藏分组及标记缓存照常运行。待Name不在最新交集则丢弃，身份比较及Details.Capture沿原真实Name规则；收藏Revision或模式实际变化使缓存重建。

控制开启条件为FavoritesEnabled且FavoritesFilterEnabled，与原分类开关无依赖；关闭强制FavoritesOnly=false。模式实际变化归零滚动并取消两种旧鼠标许可，原排序/搜索许可保持；ClearPending同时清新待切换，原无效ReadInput只额外清新请求。Close保留已应用值，ResetDisplay恢复配置默认值；绑定Reset清缓存。完整本机保存与人工范围归[收藏筛选](MapInventoryFavoritesFilter.md)，当前阶段十六项已获用户通过反馈，限v34/revision37及运行入口清单；未触发独立用例仍UNKNOWN。

## 【FACT】收藏计数与可见集合

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)由原Favorites集合提供，ListView代码保持；分类/搜索/仅看收藏只改变可见行，不减少全量收藏名额。ControlRowCount仍仅三类模式控件0～3，Panel额外加CountRowCount0～1用于绘制高度与搜索命中。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；筛选旧通过限v34/revision37及运行入口清单。

## 【FACT】收藏保护与可见身份

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：ListView代码保持，分类/搜索/仅看收藏交集与稳定置顶仍只决定可见行。DropClient以可见Row.Name检查原IsFavorite，操作行固定高度；排序/行身份变化不会把保护绑定到显示名或索引。显示方法不清已排队业务，消费前复核归DropClient。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

v37/revision40提示阶段的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。

## 【CURRENT STRATEGY】收藏材料消耗确认

当前v42/revision45的[收藏材料消耗确认](MapInventoryFavoritesConsumptionConfirm.md)仅处理原B七个制作/修理/容量与工具升级按钮。首次有效按钮请求命中已应用收藏木石的正成本时暂存一个操作，在配方内显示数量提示，以Confirm/Cancel替换原按钮行；确认按最新已捕获候选和本地Revision复核后沿原请求提交一次，取消/关闭B/相关数量、等级、耐久、配方或已应用收藏变化/绑定失效清待确认。逐帧Clear仅隐藏，确认目标或高度变化清旧面板与行鼠标许可。确认独立于原消耗提示开关；数字1～7保持原直达链，偏好v5十字段、输入19、Ghost/服务器与保存入口保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，已验收提示仍限v37/revision40，全部旧通过保持原版本/清单。
