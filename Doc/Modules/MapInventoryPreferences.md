# 背包显示偏好本地保存

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[排序筛选](MapInventoryListView.md)、[搜索](MapInventorySearch.md)、[配置与存档](DataResources.md)及[运行验收](Runtime.md)。入口CombatPrototypeNetCode，原B面板；Forest/Grassland Json/BuiltIn当前v35/revision38，[材料详情](MapInventoryDetails.md)人工已获用户通过反馈，限v32/revision35十六项，未触发独立用例仍UNKNOWN；用户已确认重置阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v31/revision34及运行入口十六项清单，人工结论来自用户反馈。主线程按确认方案完成代码、配置及静态核对；用户已确认v30保存阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v30/revision33及运行入口十六项清单。人工结论来自用户反馈；搜索已验收仅限v29/revision32原十六项。

## 【FACT】文件与配置

v31/revision34重置阶段修改原Panel、ListView、Search、MapInventoryPanelConfig、PanelData、Validator、BuiltIn、MapAuthoring八个脚本；两地图JSON在原四个保存字段之外增加两个必填重置字段。三个普通C#类及其meta已由正常Unity导入生成，没有新MonoBehaviour/组件类型或挂载：

| 文件 | 职责 |
|---|---|
| [Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesData.cs) | 七实例字段的本机显示记录（含FavoriteItemNames列表及FavoritesOnly布尔值） |
| [Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs) | 独立版本、严格读取、临时文件提交与固定路径 |
| [Preferences](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferences.cs) | 当前客户端绑定的读取、已应用值观察、延迟保存与失败隔离 |

| inventoryPanel字段 | 两地图Json/BuiltIn值 | 校验/Settings |
|---|---|---|
| preferencesEnabled | true | 必填严格bool→byte；关闭不访问偏好文件 |
| preferencesSaveSearch | true | 必填严格bool→byte；false允许临时搜索但不恢复/更新文件中的词 |
| preferencesFileId | inventory_display | 必填小写ASCII/数字/下划线，1～61字符→FixedString64Bytes |
| preferencesSaveDelaySeconds | 0.5 | 必填有限正float；客户端unscaledTime延迟 |
| preferencesResetEnabled | true | 必填严格bool→byte；原面板中的重置按钮开关 |
| preferencesResetLabel | Reset view | 必填非空白、无控制字符、最多61 UTF-8字节→FixedString64Bytes |

原83字段保留，追加[收藏计数](MapInventoryFavoritesCount.md)两字段，当前DTO十四bool、六float、三int、两模式string、59文案string及一文件ID，共85；Settings十四byte、六float、三int、两byte枚举及60 FixedString64Bytes，共85，0 GhostField/无GhostComponent。关闭详情/重置/偏好/搜索/排序/筛选/面板仍完整校验。地图Reader只接受schema35和正revision/seed，旧v1～v34拒绝；无补默认、来源回退或热重载，正常导入/烘焙生效，各端同版。

## 【CURRENT STRATEGY】读取、应用与生命周期

原Client Binding验证所属Ghost/Connected/InGame后，沿HUD.Configure→Panel.Configure传入地图ID与Settings；Panel先Reset提交旧绑定，再配置ListView/Search/Favorites，Preferences读正式文件一次。首次无正式文件按配置默认模式、DefaultFavoritesOnly及空搜索初始化，不创建目录/文件；合法文件只恢复已启用能力的模式、允许保存的查询、启用的收藏及收藏筛选值。Search.Restore复用CleanInput限制文本，更新本地Revision，不聚焦/排队编辑；初次Snapshot随后按当前分类/搜索/收藏条件建立可见行，再将其中收藏稳定置顶；不按当前库存删除文件中暂不存在的收藏。

排序能力关闭仍显示original，分类筛选关闭仍all，搜索关闭仍无查询/键盘隔离；上述能力、收藏或收藏筛选关闭时不应用或覆盖文件内对应值；收藏关闭不删除已读取收藏记录，新筛选关闭时其他偏好保存保留已读favoritesOnly。preferencesSaveSearch=false同样保留原文件词；其他模式改变可保存，但对应关闭项保持已读记录。当前searchMaxLength较小时只截断用于显示/匹配，读取本身不写回；之后用户实际改变并应用搜索才更新记录。全局偏好关闭不读、不写，重建绑定按原配置初始化。

Panel.Show在Snapshot之后由ListView.Capture应用待处理模式/搜索/收藏条件与当前可见Name收藏切换，Preferences.Capture随后只观察已应用值；GUI draft/尚未应用切换不保存。变化更新时间，连续变化合并，0.5秒到期写入；返回上次已写值取消待写，收藏以名称集合比较、不受数组顺序影响；库存刷新而偏好不变不重写。B/关闭按钮与绑定Reset经Close提交已应用待写值，然后清未应用请求；逐帧Clear只隐藏，不重置协调状态。

死亡/断线、玩家或地图源变化及World/Scene停止释放沿原Reset清本地缓存，新有效绑定重新读取。面板开关仍按initiallyOpen，滚动归零、焦点释放；这些状态不保存；已应用收藏同绑定关闭重开保留，新绑定按开关/偏好读取规则恢复。偏好按本机/地图ID隔离，同一设备同一fileId/地图共享显示值，不按玩家ID分档，也不复制至其他设备或服务器。

## 【FACT】文件协议与失败边界

路径：Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json。正式文件版本3，恰七个JSON字段：version（整数3）、mapDefinitionId（当前地图ID）、sortMode（original/type/quantity）、filterMode（all/resources/supplies/other）、searchText（字符串）、favoriteItemNames（数组）及favoritesOnly（严格bool）。收藏数组最多256项，按Ordinal名称唯一，每项字符串非空白、无控制/孤立代理项、最多61 UTF-8字节；暂不存在于库存的合法名称仍保留，不按当前favoritesMaxCount删除。任一非法项整档拒绝。UTF-8严格解码，可读BOM；类型/完整字段、重复键、额外根内容、地图/版本及模式检查，搜索最多64 UTF-16单元、不接收控制或孤立代理项。

严格兼容v1五字段与v2六字段：v1保留模式/搜索、空收藏、favoritesOnly=false；v2保留模式/搜索/收藏、favoritesOnly=false，均内存迁移v3。读取不自动保存或创建文件，下一实际已应用偏好变化才写v3。v3缺失/null/非bool favoritesOnly、旧档额外字段、未知版本及其他非法记录整档失败，不修复坏档；原玩家v4/世界v2及其路径保持。

只FileNotFound/DirectoryNotFound视为首次使用；其他读取错误由协调边界记录模块/Load/地图/路径/原异常，本绑定偏好I/O暂停，不覆盖文件，临时模式/搜索/收藏仍可使用。写入UTF-8无BOM的.json.tmp并Flush(true)，再File.Replace正式文件或File.Move首次文件；Save失败同样暂停本绑定，不自动重试。可能残留.tmp不作为正式数据读入；重建绑定可重新读取/尝试后续保存。没有云同步、游戏库存写入或玩家/世界事务调用。

## 【CURRENT STRATEGY】背包显示偏好重置

原Panel在排序/分类/搜索区之后、材料可见行之前绘制一行全宽preferencesResetLabel按钮；preferencesResetEnabled=false隐藏该行，面板关闭时沿原隐藏。按钮沿原_mousePressAccepted校验只排队本地请求，不在GUI内改列表或写文件；不依赖preferencesEnabled，所有显示能力关闭时也只按原关闭能力规则恢复临时显示。

下一次原有效Panel.Show先捕获完整Snapshot，再消费重置：ListView.ResetDisplay清未应用模式请求，按当前Settings.DefaultSortMode/DefaultFilterMode/DefaultFavoritesOnly恢复，关闭能力保持original/all/FavoritesOnly=false，并使缓存重新建立；Search.ResetDisplay清已应用词/草稿/待编辑请求，仅原已应用文本非空时增加Revision，沿原ReleaseFocus/FlushGUIFocus释放焦点。Favorites.ResetDisplay同时清启用的已应用收藏与待请求，收藏关闭保留文件原记录；之后原ListView.Capture按当前完整库存重新生成可见行，滚动归零、旧行按下许可取消，面板保持打开；SearchField使用ControlRowCount与CountRowCount共享几何，收藏筛选与计数各自启用时分别下移一行，重置仍在搜索之后。

Preferences.Capture仍在可见行刷新后观察已应用值，默认0.5秒unscaledTime保存；Close/Reset提交已应用待写值。重复默认值不制造新的待写或重置计时，回到上次已写值取消待写；首次无文件且显示值未变化不创建目录/文件。preferencesEnabled=false或本绑定I/O暂停时仅临时重置；关闭排序/筛选/搜索能力或preferencesSaveSearch=false仍保留文件中对应原值，收藏开启时清当前收藏，关闭时保留文件中的收藏；不删除偏好文件、不重建保存协调类、不自动修复坏档或重试失败。

Close/Reset及原无效ReadInput分支清未应用重置，逐帧Clear仍只隐藏，已应用值沿原保存边界处理。业务库存/统计、容量/配方/工具、已排队的真实Name丢弃与制作/修理/升级请求及已提交服务器动作保持；只取消未应用显示切换/编辑和旧行按下许可。无新输入/RPC/Ghost字段，Store/Data/Preferences沿原路径/提交边界使用v3七字段并兼容严格v1五字段/v2六字段，完整筛选与保存规则归[收藏筛选](MapInventoryFavoritesFilter.md)。

## 【KNOWN ISSUES】v30保存阶段静态证据与人工边界

以下静态证据和十六项人工通过仅限v30/revision33保存阶段；该阶段正常Unity编译/重载完成，55配置/Settings、三个普通类及五字段协议元数据已核对；三个脚本均被AssetDatabase注册并加载。1612份非法地图配置全部拒绝（每地图806），96组合法读取通过（每地图48），覆盖全部55字段/原规则、新开关、ID/延迟、关闭仍验证及旧/未来地图版本；只配置读取/校验，不调用偏好读写或界面业务。

两地图各49次，共98次隔离Editor Bake通过，原41变体保留，增加偏好关闭、不保存搜索、自定义/1/61字符ID、0.1/2秒延迟、面板/全部展示关闭组合。全部55Settings和原Settings/零反馈/Prefab引用、完整布置/资源签名匹配；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。只读源SubScene、临时克隆/TextAsset/Scene/World/BlobAssetStore释放，主场景干净、3根对象、未Play。

更新过程中旧导入Worker以旧程序集读取新JSON，留下两条preferencesEnabled未知字段错误；当前程序集读取/Bake均通过，重新正常导入SubScene没有新增错误。隔离Bake及重新导入前后Console均[2 Error,7 Warning,47 Log]，未清空，也未宣称Console为0 Error。

原Input19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3及全部反馈、玩家v4根7/工具项3、世界v2根9/掉落项8保持。Scene/SubScene/Prefab/Animator/旧meta/资源/字体/包/构建配置、HUD/Binding、PlayerInput/服务器采集/拾取/丢弃/制作/修理/升级/游戏保存未修改。

原419项人工内容/编号保留，追加十六项后435项，清单归[运行入口](Runtime.md)。本阶段人工通过范围限上述版本和十六项清单，未实际触发的独立文件I/O、重启/生命周期、坏档/权限/替换失败、GUI/焦点/字形/联网/预测场景仍UNKNOWN；同机多进程并发、断电和平台Flush/Replace语义及性能未获单独验收结论，仍UNKNOWN。旧阶段用户通过保持原版本/清单。AI未执行Preference Configure/Load/Save/Capture/Flush、搜索/排序/库存/GUI业务、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、真实游戏存档I/O、采样或图片，未创建子Agent或提交Git。

## 【KNOWN ISSUES】v31重置阶段验收边界

v31/revision34重置阶段的57配置/Settings及0 GhostField已正常Unity编译/重载并核对；1716份非法地图配置拒绝（每地图858），112组合法读取通过（每地图56），包括新开关标量/完整57字段/新文案和关闭仍验证、旧v1～v30/未来版本及原规则。

两地图各56次，共112次隔离Editor Bake通过：保留原49变体，增加重置关闭、ASCII/中文61字节/中文文案、显示与重置关闭、面板与重置关闭及自定义默认模式/初始打开/保存关闭组合。全部57Settings与原Settings/零反馈/Prefab引用、完整布置/资源签名匹配；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71保持。Bake Console前后均[0 Error,2 Warning,0 Log]，两条PEListener/DOTween源码警告，未清空Console；临时资源释放，主场景干净、3根对象、未Play。

原输入19/DropGhost4/Tools3和全部所属反馈、玩家v4根7/工具项3、世界v2根9/掉落项8及本机偏好v1五字段保持。重置阶段仅八现有脚本、两JSON与文档修改，Scene/SubScene/Prefab/Animator/全部meta/资源/字体/包/构建配置、HUD/Binding/PlayerInput及服务器业务保持。原435项人工内容/编号保留，新增十六项后451项，清单归[运行入口](Runtime.md)；本阶段人工十六项已获用户通过反馈，限CombatPrototypeNetCode、v31/revision34及运行入口清单，主线程结合既有静态核对判定通过，结论来自用户反馈；既有用户通过仍限各阶段原版本/清单。

本阶段人工通过范围限上述版本和十六项清单；未实际触发的独立点击/刷新/焦点输入、关闭/重绑/重启、文件I/O/失败、多人/时序/字形/分辨率仍UNKNOWN，同机并发/断电/平台语义及性能未获单独验收结论，仍UNKNOWN。AI未执行ResetDisplay/面板/偏好/GUI业务、实际偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、采样或图片，未创建子Agent或提交Git。

## 【CURRENT STRATEGY】材料详情与偏好边界

v32/revision35详情阶段的[详情](MapInventoryDetails.md)选择只属当前客户端绑定，不观察为排序/分类/搜索变化，也不写入当前v3七字段文件（兼容读取v1五字段/v2六字段）。原显示重置、Close/Reset及无效ReadInput清详情选择/未应用请求；展开高度变化取消旧面板/行按下许可，已排队业务保持。Store/Data/Preferences三个保存类、默认0.5秒延迟及关闭能力/失败隔离规则保持，详情人工已获用户通过反馈，限v32/revision35十六项，未触发独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏观察与容量边界

v33/revision36收藏阶段新增收藏记录；开启收藏时Favorites.Configure在Preferences.Configure之前，加载v2后恢复全部合法名称。显示上限默认64、配置1～256；降低上限保留已存记录，达到或超出上限只禁新增，原收藏仍能取消。Preferences只在收藏Revision实际变化时复制名称并按Ordinal排序，再与上次成功写入集合比较，复用0.5秒延迟及Close/Reset提交；恢复/库存刷新本身不写盘。偏好关闭或本绑定I/O暂停只保持当前绑定的临时收藏，收藏关闭时其他偏好保存保留原收藏数组。人工十六项及未运行的文件/迁移/故障用例归[收藏](MapInventoryFavorites.md)。

## 【CURRENT STRATEGY】收藏筛选偏好

当前v35/revision38仅在新筛选能力开启时恢复并观察FavoritesOnly，已应用模式变化更新Data，并与上次成功写入布尔值比较，复用原0.5秒unscaledTime延迟及Close/Reset提交。返回已写值取消该变化，加载/库存刷新/未应用GUI请求不写盘；显示重置恢复DefaultFavoritesOnly，同时沿原清启用收藏。偏好关闭或本绑定I/O暂停时临时模式可用，筛选能力关闭时其他显示变化保存保留文件原布尔值。文件路径、严格UTF-8、.tmp/Flush(true)/Replace或Move及失败暂停当前绑定规则保持；迁移与实际I/O人工边界归[收藏筛选](MapInventoryFavoritesFilter.md)。

## 【FACT】收藏计数与偏好边界

当前v35/revision38的[收藏计数](MapInventoryFavoritesCount.md)在原Favorites.Configure/Restore时缓存数量文字，Preferences/Data/Store代码与独立v3七字段、严格v1/v2兼容、路径/延迟/.tmp/Flush/Replace或Move及失败边界保持。未增加计数字段，不按库存或当前配置上限删旧收藏；显示关闭不影响原收藏恢复/观察和保存，I/O关闭或暂停仍按当前临时已应用集合计数。十六项人工待验收归计数专题，旧偏好/筛选通过范围保持。
