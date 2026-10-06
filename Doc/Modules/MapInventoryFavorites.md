# 背包材料收藏与置顶

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[排序筛选](MapInventoryListView.md)、[搜索](MapInventorySearch.md)、[材料详情](MapInventoryDetails.md)、[本机偏好](MapInventoryPreferences.md)、[配置与存档](DataResources.md)及[运行验收](Runtime.md)。入口CombatPrototypeNetCode，原B面板；Forest/Grassland Json/BuiltIn当前v36/revision39。主线程按用户已确认方案完成代码、配置和静态核对；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v33/revision36及运行入口十六项清单；人工结论来自用户反馈。详情用户通过仍限v32/revision35十六项，其他旧通过保持各自版本/清单。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [Favorites](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelFavorites.cs) | 普通客户端C#类，真实Name集合/待请求、容量、稳定分组、标记缓存及控制行 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 原Configure/Show/GUI/重置/Close/Reset与布局接入 |
| [ListView](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelListView.cs) | 最新过滤行后应用收藏、原排序后置顶、收藏Revision缓存 |
| [Details](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelDetails.cs) | 名称标签接收收藏显示文字；选择仍按真实Name |
| [Preferences](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferences.cs) | 恢复/观察已应用收藏、集合比较与原延迟/提交/失败边界 |
| [Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesData.cs)/[Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs) | 当前七实例字段，严格v3七字段及v1五字段/v2六字段兼容读取 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)/[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原inventoryPanel增加六字段 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)/[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)/[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 全量校验、默认值、原地图根Settings映射 |
| [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)/[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) | 显式Json来源，与BuiltIn一致 |

v33/revision36收藏阶段十一现有脚本、一新普通Favorites类及正常Unity生成meta、两地图JSON接入。Favorites GUID=fce8d766a3a6f334286842eb7f4d86a8已注册/加载，无新MonoBehaviour/ECS组件或挂载。原Details GUID保持；Scene/SubScene/Prefab/Animator/旧meta/资源/图片/字体/包/构建配置、HUD/Binding/PlayerInput及服务器/玩家世界保存代码保持。

## 【FACT】配置与建议值

| inventoryPanel字段 | Json/BuiltIn建议值 | 校验/Settings |
|---|---|---|
| favoritesEnabled | true | 必填严格bool→byte |
| favoritesMaxCount | 64 | 必填严格int，1～256；达到上限禁新增 |
| favoriteButtonLabel | Favorite | 必填文案→FixedString64Bytes |
| unfavoriteButtonLabel | Unfavorite | 必填文案→FixedString64Bytes |
| favoriteTagLabel | Favorite | 必填文案→FixedString64Bytes |
| favoritesFullLabel | Favorite limit | 必填文案→FixedString64Bytes |

原收藏阶段六字段保留，追加[收藏筛选](MapInventoryFavoritesFilter.md)六字段、[收藏计数](MapInventoryFavoritesCount.md)两字段与[收藏保护](MapInventoryFavoritesDropProtection.md)两字段，当前共87：DTO十五bool、六float、三int、两模式string、60文案string及一文件ID；Settings十五byte、六float、三int、两byte枚举及61 FixedString64Bytes。Settings零GhostField/无GhostComponent。四文案非空白、无控制字符、最多61 UTF-8字节；关闭收藏/面板/原能力仍全量验证。地图schema36、revision39，各端同版；旧地图v1～v35或缺失/null/错类型/未知/重复键明确拒绝，无补默认、来源回退或热重载，正常导入/烘焙后生效。

## 【CURRENT STRATEGY】可见行、置顶与点击

原HUD.Configure→Panel.Configure先配置ListView/Search/Favorites，再Preferences读取/恢复一次；原有效Show先由Snapshot捕获完整库存，在显示重置处理之后调用ListView.Capture。ListView先应用模式/搜索并生成最新合法正数量可见行，再消费收藏待请求；只有当前仍可见的同一真实FixedString64Bytes Name可切换。隐藏/消失的待Name丢弃，不用显示名或行索引绑定。每次只保留一个待请求，消费一次；GUI只排队，不修改库存、写盘或重新排序。

分类、搜索与启用的收藏筛选条件取交集后按原original/type/quantity排序；Favorites随后稳定分为收藏/普通两组，收藏在前，各组内部保持该模式的顺序。关闭原排序仍original组内顺序；隐藏收藏不强制显示，清词或切换分类不清收藏。Snapshot继续验证完整库存；容量统计、材料总数、木/石配方与业务资格不从可见行推导。收藏数量归零/物品消失仍保留Name，重新获得同名材料时参与置顶。收藏总数包含暂不存在的名称。

每个可见材料保留原名称与Drop/All操作行，之后追加一行全宽Favorite/Unfavorite；未收藏且达到上限时显示禁用的Favorite limit。已收藏行名称文字为原Text加空格及[配置tag]，真实Name不改，详情选择保持。启用的详情展开在收藏控制行之后；收藏关闭恢复原两行材料布局，不追加标记/按钮。面板矩形、固定标题/页脚、Drop/All几何及指针隔离保持；当前SearchField共享ControlRowCount与CountRowCount定位，收藏筛选/计数各自开启时各下移一行。材料基础滚动行数为可见数×3（收藏关闭×2），详情实际测量高度另计。

实际收藏变化归零滚动并取消旧面板/行鼠标按下许可，防止重排后沿旧位置完成点击；可见行身份检查和详情高度变化仍沿原许可规则，原排序/搜索输入行为保持。收藏切换不直接清已排队业务；未消费丢弃请求另按[收藏保护](MapInventoryFavoritesDropProtection.md)复核，制作/修理/升级及已提交服务器动作保持；无新快捷键、RPC/输入或Ghost字段。

## 【CURRENT STRATEGY】容量与生命周期

收藏上限默认64、合法1～256；已收藏始终可取消并腾出名额。偏好读取保留最多256个合法记录，降低当前上限不删已存记录；达到或超过当前上限只限制新收藏。未在当前库存的记录不自动清理；恢复超过当前上限时仍显示/置顶所有已存的当前可见收藏。

显示重置在原Snapshot之后清未应用收藏，收藏开启时同时清已应用集合；关闭收藏时文件原记录保留。随后按配置默认模式/空搜索重建列表并释放焦点。B/关闭按钮、无效ReadInput清待收藏，同一绑定已应用收藏关闭重开保留；逐帧Clear仍只隐藏。绑定Reset先沿Close提交已观察待写偏好，再清Favorites集合/缓存；死亡/断线、地图源/玩家或World/Scene变化沿原绑定释放，新有效绑定按开关/偏好读档规则初始化，不恢复开关/滚动/焦点或详情选择。

preferencesEnabled=false或本绑定偏好I/O暂停时，收藏仅在当前绑定内临时生效；inventoryPanel.enabled=false不提供收藏输入，也不访问偏好文件；收藏关闭时不应用/更新文件收藏数组，其他显示偏好变化保存时仍保留已读记录。恢复坏档/缺文件不会自造收藏，读失败不覆盖原文件、不自动重试。

## 【FACT】本机偏好v3与兼容

路径继续为Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json；本机/地图隔离。当前v3恰七字段：version=3、mapDefinitionId、sortMode、filterMode、searchText、favoriteItemNames及严格bool favoritesOnly。模式/搜索原校验保持；收藏必须数组、最多256项，元素均为非空白/无控制或孤立代理项/最多61 UTF-8字节的字符串，Ordinal唯一，不按显示名校验、不要求当前库存存在。任一非法项、字段/类型/重复键/额外内容/未知版本错误整档失败，沿原Load边界日志并暂停本绑定I/O。

严格兼容v1五字段与v2六字段：v1保留模式/搜索、空收藏、favoritesOnly=false；v2保留模式/搜索/收藏、favoritesOnly=false，均内存迁移v3。加载或首次缺文件本身不写盘、不创建目录；下一实际已应用偏好变化才保存v3。v3布尔值缺失/错类型、旧档额外字段或未知版本整档失败，不批量改档或修复坏档。偏好版本独立，玩家v4根七字段/工具项三字段和世界v2根九字段/掉落项八字段及其事务保持。

Preferences只在已应用收藏Revision变化时将名称按Ordinal复制到Data；与上次成功写入的名称集合比较，数组顺序不制造变化，返回已写集合取消待写，库存刷新不写档。连续偏好变化复用unscaledTime默认0.5秒合并，Close/Reset提交已应用待写，未应用GUI请求不保存。UTF-8严格读可带BOM，写无BOM的.json.tmp、Flush(true)、File.Replace或首次File.Move沿原；Save失败暂停本绑定I/O，不自动重试，可能残留.tmp不作为正式数据读取。

## 【KNOWN ISSUES】静态核对与人工边界

以下证据及收藏人工通过均限v33/revision36阶段。该阶段正常Unity编译/重载完成，77配置/Settings、零GhostField、新普通类注册/加载、偏好v2/六实例字段及原协议元数据核对通过。2692份非法配置全部拒绝（每地图1346），146组合法读取通过（每地图73）：保留此前71字段规则，追加六字段的缺失/null/错形状/错误标量/重复键、开关严格性、整数1～256边界、四文案空白/控制/61 UTF-8字节及关闭仍校验，旧v1～v32/未来版本及原规则。只配置读取/语义验证，不执行收藏/GUI/偏好业务或I/O。

两地图各73次，共146次隔离Editor Bake通过。保留原64变体，增加收藏关闭、收藏/面板关闭、上限1/256、四文案ASCII61/UTF-8 61/中文、偏好及显示控件关闭但收藏开启、详情关闭。全部77Settings、原Settings/零反馈/Prefab引用、布置与兼容签名匹配；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。只读源SubScene，临时克隆/TextAsset/Scene/World/BlobAssetStore全部释放；主场景干净、3根对象、单场景、未Play。烘焙后仅将收藏变化的按下许可取消限定为收藏Revision实际变化，保留原排序/搜索行为；正常编译/77字段再次核对，配置/烘焙契约未改。

执行前Console[0 Error,5 Warning,106 Log]，五条为已有NetCode Server Tick Batching警告；隔离Bake前后及最终编译核对均[0 Error,7 Warning,106 Log]，新增两条为MCP WebSocket未初始化与既有PEListener源码序列化Warning。未清空Console；不从这些旧运行日志推导本阶段性能/运行结论。

原467项人工内容/编号逐字保留，追加十六项后共483项，清单归[运行入口](Runtime.md)。用户已确认本阶段人工十六项通过，主线程结合既有静态核对与用户反馈判定通过，范围限CombatPrototypeNetCode、v33/revision36及运行入口清单；人工结论来自用户反馈。旧详情用户通过限v32/revision35十六项及其他旧阶段原范围。未实际触发的独立按钮/置顶/数量变化、容量限制、重置/关闭/重绑、偏好v1迁移/v2读写/失败、焦点/字形/分辨率、多人/预测/时序用例仍UNKNOWN；同机并发/断电/平台Flush/Replace语义、性能/带宽/平台构建/线上联调未单独验证，仍UNKNOWN。AI未执行Favorites/ListView/Panel/Preferences/GUI业务、真实偏好/游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、采样或图片，未创建子Agent、暂存或提交Git。

## 【CURRENT STRATEGY】仅看收藏与当次取消

v34/revision37筛选阶段新增[收藏筛选](MapInventoryFavoritesFilter.md)，Favorites仅新增共享IsFavorite查询，不接管模式或偏好协调。ListView先按最新分类/搜索/收藏条件生成可见行，再应用待Name收藏；仅看收藏时原地移除取消收藏的行，同次刷新按Name关闭隐藏详情，排序/置顶/数量统计及原收藏容量规则保持。Close/无效输入清待新模式，显示重置恢复DefaultFavoritesOnly并清启用收藏；本机v3保存新布尔值，关闭能力保留原值。当前筛选十六项已获用户通过反馈，限v34/revision37及运行入口清单；未触发独立用例仍UNKNOWN，既有收藏通过仍限v33/revision36清单。

## 【CURRENT STRATEGY】收藏数量与上限

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)复用原_names.Count/_maximum与FavoritesFullLabel，新增本地_countEnabled/_countLabel、缓存CountText和CountRowCount；Configure、合法Restore、实际ApplyPending变化和非空ResetDisplay后更新文字，Reset清缓存。统计包含隐藏/暂不存在的Name，满额/超限旧记录保留且可取消；控制按钮、容量门槛、Revision/置顶与原偏好保存行为保持。计数配置及用户确认人工十六项通过边界归计数专题，限CombatPrototypeNetCode、v35/revision38及运行入口清单，未实际触发的独立用例仍UNKNOWN；原收藏通过仍限v33/revision36，筛选通过限v34/revision37清单。

## 【CURRENT STRATEGY】收藏材料丢弃保护

当前v36/revision39的[收藏保护](MapInventoryFavoritesDropProtection.md)：Favorites代码、Name集合/上限/置顶/Revision/计数与保存保持，复用原IsFavorite；favoritesDropProtectionEnabled与favoritesEnabled共同决定实际保护，与计数/筛选/排序/搜索/详情/偏好/重置开关独立。收藏材料仍计完整库存，可供原制作/修理/升级与E使用；只限制本机面板丢弃和未消费丢弃请求。 本阶段十六项人工GamePlayer待验收，完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。
