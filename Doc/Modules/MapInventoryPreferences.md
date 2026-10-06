# 背包显示偏好本地保存

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[排序筛选](MapInventoryListView.md)、[搜索](MapInventorySearch.md)、[配置与存档](DataResources.md)及[运行验收](Runtime.md)。入口CombatPrototypeNetCode，原B面板；Forest/Grassland Json/BuiltIn当前v30/revision33。主线程按确认方案完成代码、配置及静态核对，本阶段十六项人工GamePlayer待验收；搜索已验收仅限v29/revision32原十六项。

## 【FACT】文件与配置

八现有脚本修改：原Panel、ListView、Search、MapInventoryPanelConfig、PanelData、Validator、BuiltIn、MapAuthoring；两地图JSON追加四必填字段。三个新增普通C#类及其meta由正常Unity导入生成，没有新MonoBehaviour/组件类型或挂载：

| 文件 | 职责 |
|---|---|
| [Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesData.cs) | 五实例字段的本机显示记录 |
| [Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs) | 独立版本、严格读取、临时文件提交与固定路径 |
| [Preferences](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferences.cs) | 当前客户端绑定的读取、已应用值观察、延迟保存与失败隔离 |

| inventoryPanel字段 | 两地图Json/BuiltIn值 | 校验/Settings |
|---|---|---|
| preferencesEnabled | true | 必填严格bool→byte；关闭不访问偏好文件 |
| preferencesSaveSearch | true | 必填严格bool→byte；false允许临时搜索但不恢复/更新文件中的词 |
| preferencesFileId | inventory_display | 必填小写ASCII/数字/下划线，1～61字符→FixedString64Bytes |
| preferencesSaveDelaySeconds | 0.5 | 必填有限正float；客户端unscaledTime延迟 |

原51字段保留，当前DTO八bool、六float、两int、两模式string、36文案string及一文件ID，共55；Settings八byte、六float、两int、两byte枚举及37 FixedString64Bytes，共55，0 GhostField/无GhostComponent。关闭偏好/搜索/排序/筛选/面板仍完整校验。地图Reader只接受schema30和正revision/seed，旧v1～v29拒绝；无补默认、来源回退或热重载，正常导入/烘焙生效，各端同版。

## 【CURRENT STRATEGY】读取、应用与生命周期

原Client Binding验证所属Ghost/Connected/InGame后，沿HUD.Configure→Panel.Configure传入地图ID与Settings；Panel先Reset提交旧绑定，再配置ListView/Search，Preferences读正式文件一次。首次无正式文件按配置默认模式、空搜索初始化，不创建目录/文件；合法文件只恢复已启用能力的模式和允许保存的查询。Search.Restore复用CleanInput限制文本，更新本地Revision，不聚焦/排队编辑；初次Snapshot随后按当前模式/搜索建立可见行。

排序能力关闭仍显示original，筛选关闭仍all，搜索关闭仍无查询/键盘隔离；上述能力关闭时不应用或覆盖文件内对应值。preferencesSaveSearch=false同样保留原文件词；其他模式改变可保存，但对应关闭项保持已读记录。当前searchMaxLength较小时只截断用于显示/匹配，读取本身不写回；之后用户实际改变并应用搜索才更新记录。全局偏好关闭不读、不写，重建绑定按原配置初始化。

Panel.Show在Snapshot之后由ListView.Capture应用待处理模式/搜索，Preferences.Capture随后只观察已应用值；GUI draft/尚未应用切换不保存。变化更新时间，连续变化合并，0.5秒到期写入；返回上次已写值取消待写，库存刷新而偏好不变不重写。B/关闭按钮与绑定Reset经Close提交已应用待写值，然后清未应用请求；逐帧Clear只隐藏，不重置协调状态。

死亡/断线、玩家或地图源变化及World/Scene停止释放沿原Reset清本地缓存，新有效绑定重新读取。面板开关仍按initiallyOpen，滚动归零、焦点释放；这些状态不保存。偏好按本机/地图ID隔离，同一设备同一fileId/地图共享显示值，不按玩家ID分档，也不复制至其他设备或服务器。

## 【FACT】文件协议与失败边界

路径：Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json。正式文件版本1，恰五个JSON字段：version（整数1）、mapDefinitionId（当前地图ID）、sortMode（original/type/quantity）、filterMode（all/resources/supplies/other）、searchText（字符串）。UTF-8严格解码，可读BOM；类型/字段、重复键、额外根内容、地图/版本及模式检查，文本最多64 UTF-16单元，不接收控制或孤立代理项。

只FileNotFound/DirectoryNotFound视为首次使用；其他读取错误由协调边界记录模块/Load/地图/路径/原异常，本绑定偏好I/O暂停，不覆盖文件，临时模式/搜索仍可使用。写入UTF-8无BOM的.json.tmp并Flush(true)，再File.Replace正式文件或File.Move首次文件；Save失败同样暂停本绑定，不自动重试。可能残留.tmp不作为正式数据读入；重建绑定可重新读取/尝试后续保存。没有云同步、游戏库存写入或玩家/世界事务调用。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译/重载完成，55配置/Settings、三个普通类及五字段协议元数据已核对；三个脚本均被AssetDatabase注册并加载。1612份非法地图配置全部拒绝（每地图806），96组合法读取通过（每地图48），覆盖全部55字段/原规则、新开关、ID/延迟、关闭仍验证及旧/未来地图版本；只配置读取/校验，不调用偏好读写或界面业务。

两地图各49次，共98次隔离Editor Bake通过，原41变体保留，增加偏好关闭、不保存搜索、自定义/1/61字符ID、0.1/2秒延迟、面板/全部展示关闭组合。全部55Settings和原Settings/零反馈/Prefab引用、完整布置/资源签名匹配；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。只读源SubScene、临时克隆/TextAsset/Scene/World/BlobAssetStore释放，主场景干净、3根对象、未Play。

更新过程中旧导入Worker以旧程序集读取新JSON，留下两条preferencesEnabled未知字段错误；当前程序集读取/Bake均通过，重新正常导入SubScene没有新增错误。隔离Bake及重新导入前后Console均[2 Error,7 Warning,47 Log]，未清空，也未宣称Console为0 Error。

原Input19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3及全部反馈、玩家v4根7/工具项3、世界v2根9/掉落项8保持。Scene/SubScene/Prefab/Animator/旧meta/资源/字体/包/构建配置、HUD/Binding、PlayerInput/服务器采集/拾取/丢弃/制作/修理/升级/游戏保存未修改。

原419项人工内容/编号保留，追加十六项后435项，清单归[运行入口](Runtime.md)。本阶段GamePlayer/偏好实际文件I/O、重启/生命周期、坏档/权限/替换失败、GUI/焦点/字形/联网/预测、同机多进程并发、断电和平台Flush/Replace语义及性能均UNKNOWN；旧阶段用户通过保持原版本/清单。AI未执行Preference Configure/Load/Save/Capture/Flush、搜索/排序/库存/GUI业务、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、真实游戏存档I/O、采样或图片，未创建子Agent或提交Git。
