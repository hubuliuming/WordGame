# 地图存档状态提示与手动保存

返回[地图](Map.md)、[资源存档](MapResourcePersistence.md)、[掉落存档](MapDropPersistence.md)和[运行入口](Runtime.md)。入口为CombatPrototypeNetCode；本专题负责F5请求、世界保存结果投影及只读HUD，世界文件契约仍归原资源/掉落存档，玩家文件仍沿原链路。

## 【FACT】接入点与职责

| 文件 | 当前职责 |
|---|---|
| [输入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) | 原所属输入增加SaveWorld，F5单次按下；当前共17字段 |
| [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 追加世界保存所属显示状态，Hidden/0/None初值 |
| [手动请求](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapWorldSaveManualRequests.cs) | 资格、全局冷却、同tick合并、本人结果序号与连接缓存释放 |
| [原保存服务](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceSaveSystem.cs) | 当前tick完整捕获、手动/原自动触发合并、真实写盘结果及会话状态 |
| [服务端投影](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapWorldSaveHudStateSystem.cs) | 原保存/复活之后，只读保存结果并提交所属3字段 |
| [显示数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapWorldSaveHudData.cs) | 16字段Settings、Mode/ManualSequence/ManualResult所属Ghost |
| [显示配置](../../Assets/Scripts/CombatPrototype/Map/MapWorldSaveHudConfig.cs) | worldSaveHud段16个必填公共字段 |
| [客户端显示](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapWorldSaveHudClient.cs) | 缓存文案与本人结果期限，原Repaint宿主委托绘制 |
| [原宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | Main Camera现有组件委托Configure/Show/Clear/Reset/Draw，独立显示开关 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [校验器](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) | 固定配置写入原地图根，严格检查新字段和布局 |

五个新C#脚本均不新增MonoBehaviour挂载，meta由Unity正常导入生成。Scene/SubScene/Prefab/Animator、旧meta、资源绑定、图片/字体/材质、包及构建配置保持。输入由15变16，玩家增加所属3字段；各端须同版代码/配置并重新烘焙，没有新增RPC、联网版本协商或运行热重载。

## 【FACT】配置与建议值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为schemaVersion=23/configRevision=26。resourcePersistence由4变6字段，Settings由5变7，新增ManualSaveEnabled/ManualSaveCooldownSeconds；worldSaveHud段及16字段必填。原严格UTF-8、形状、缺失/未知/重复字段、标量类型和语义检查保持；旧地图v1～v22明确拒绝，不补默认或回退来源，关闭仍校验。正常导入/烘焙后生效。

| 字段 | 默认值 | 校验/行为 |
|---|---|---|
| resourcePersistence.manualSaveEnabled | true | 必填布尔；只控制F5，原自动保存保持；世界总开关优先 |
| resourcePersistence.manualSaveCooldownSeconds | 5 | 有限正数秒；服务端模拟时间、全局冷却，接受失败尝试也占用 |
| worldSaveHud.enabled | true | 必填布尔；显示独立于手动保存和其他HUD开关 |
| panelWidthPixels | 400 | 有限，(32,1920] |
| panelHeightPixels | 84 | 有限正数，至少2×fontSize+40 |
| bottomMarginPixels | 336 | 有限非负；至少resourceStatusHud底距+高度+16，底距+高度≤1080 |
| fontSize | 20 | 正int，须满足双行高度 |
| feedbackSeconds | 3 | 有限正数秒，只决定本人结果文案期限 |
| disabledLabel | World saving disabled | 以下9个文案均非空白、无控制字符、最多61个UTF-8字节 |
| notSavedLabel | No world checkpoint yet | 当前会话尚无SaveSystem成功检查点，含手动写入 |
| savedLabel | World saved | 原SavePrepared实际写入成功返回后才使用 |
| captureFailedLabel | Snapshot failed | 当前完整捕获失败 |
| saveFailedLabel | World save failed | 写盘失败；手动失败反馈共用此文案 |
| manualSaveLabel | F5 Save world | 本人反馈到期后的快捷键提示 |
| manualDisabledLabel | Manual save disabled | 世界/手动开关关闭；本人Disabled结果也用此文案 |
| cooldownLabel | Save cooldown | 全局冷却内请求的本人结果 |
| unavailableLabel | Save unavailable | 本人资格/恢复就绪检查未满足时的结果 |
| errorColorHex | #FF6B6B | #RRGGBB六位十六进制，沿原方法烘焙float3 RGB，显示alpha=1 |

默认布局：F320×104/底48、G400×84/底168、资源状态400×52/底268、存档400×84/底336，相邻文字面板间隔16。按1920×1080基准等比缩放；两行各fontSize+4高、间隔8、居中，背景黑色alpha0.7，正常白字，捕获/写盘及本人Failed结果用配置错误色。旧单行G52/资源状态底236组合可显式把存档底距设304；关闭面板仍检查布局约束。

## 【CURRENT STRATEGY】F5与原保存服务

本地启用GhostOwnerIsLocal的玩家沿原输入系统写SaveWorld事件，按住不连续触发。服务器沿原Connected/InGame连接和CommandTarget，校验玩家/所有权匹配、启用Simulate、存活、世界与手动开关及恢复Ready；移动、攻击或活动采集预约不阻止F5。无有效所属目标不写本人结果；合法所属但死亡/未Ready为Unavailable，开关关闭为Disabled，冷却内为Cooldown。

每tick先收集全部合格请求，再预占一次全局冷却；同tick多个Client请求共用一个完整捕获和一次世界写入，每个接受者获得本人Success或Failed。不同tick请求遵守同一个冷却；失败不自动重发F5。自动保存不占手动冷却。F5与原资源变化/10秒检查点同时满足时只调用一次TrySave，reason=ManualRequest；不修改原F/G、制作、修理或丢弃的资格/优先规则。

接受F5时跳过原捕获失败后的等待，仍在当前tick完整读取全部资源及开启时的已提交未到期地面物/编号上限；两类均成功才交换缓存并保存。绑定或捕获失败设置CaptureFailed、记录stage/map/slot/placement/原异常，接受者为Failed；该手动尝试不能用旧缓存写成成功。写盘继续调用原SavePrepared与.tmp/Flush(true)/原子替换，成功返回才设置Saved/Success，异常为SaveFailed/Failed并保留待保存状态和原正式档。下一原状态变化/检查点或冷却后的新F5可再次尝试。

原SaveSystem仍位于服务端预测组的资源再生、DropCleanup/InventoryDrop之后、PlayerRespawn之前。原自动保存、10秒检查点及源变化/OnStopRunning/OnDestroy的最后完整缓存保存保持；关闭保存只用已持有缓存，不读取已释放实体。源更换/停止/销毁清手动连接结果、序号缓存、全局冷却与显示状态，未创建客户端保存入口。

F5保存原世界v2资源/Drops/LastDropId；saveGroundDrops=false仍只存资源，世界总开关关闭不读写世界文件。世界路径、九字段根、四字段耗尽条目、八字段掉落条目、布局签名及合法v1读取保持；新HUD/输入/冷却不进入世界或玩家文件，也不参与资源签名。玩家库存/Tools继续原v2保存链。两种文件分别替换，跨文件原子一致、防重复和同槽多服务端写权协调仍未接入。

## 【CURRENT STRATEGY】只读所属显示

Mode为Hidden/Disabled/NotSaved/Saved/CaptureFailed/SaveFailed。第一行展示世界服务当前会话的最近保存结果；Saved不表示之后永无未保存变化，也不表示玩家文件已保存。RestoreSystem首次建档/加载仍按原链完成，本HUD不以文件存在或恢复成功推断SaveSystem已成功写检查点；首次Ready绑定先建立NotSaved，若该tick已有变化会沿原规则写入并转Saved，原首次建档成功与本会话检查点是不同事实。开启但恢复未Ready时Hidden，关闭世界存档时Disabled。

Player Baker初值Hidden/ManualSequence=0/ManualResult=None，仅SendToOwner。服务器在PlayerRespawn之后给Connected/InGame、所有权匹配、启用Simulate且存活玩家提交Mode及本人ManualSequence/ManualResult；世界Mode相同，手动结果按所属玩家隔离。ManualResult为None/Success/Disabled/Cooldown/Unavailable/Failed，结果序号每次本人拒绝或接受完成时递增；只在字段变化时写入。

客户端仅沿原本地玩家和HUD绑定读取这3字段，不读服务器快照缓存或世界文件。第二行默认显示F5提示或Manual save disabled，本人序号变化时显示结果3秒；使用unscaledTime只决定文案到期。初次绑定/晚加入只观察当前序号，不重播旧结果。非法枚举、None配非零序号或非零Hidden快照记录stage/map/mode/sequence/result/原异常并隐藏本频道。

worldSaveHud.enabled=false只关闭本显示，F5继续按服务端开关处理；manualSaveEnabled=false只关闭F5，世界自动结果仍显示。原F/G、B、高亮与资源状态全关但本HUD开启时绑定保留；所有显示均关闭才收起绑定，键盘F5不依赖HUD。死亡/断线、源/玩家变化、World/Scene释放沿原Reset清文案/序号/期限，Repaint恢复GUI矩阵及颜色，不消费GUI输入或执行保存。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译无C# Error，实际Assembly与生成Serializer/Snapshot核对所属3字段/SendToOwner、输入16、配置/Settings各16、resourcePersistence配置6/Settings7；原F4/G6/资源状态4、Drop Ghost4、世界根9/掉落条目8保持。Forest/Grassland各13种隔离Editor烘焙共26次通过：Json/BuiltIn默认、世界关闭、手动关闭、HUD关闭、世界/HUD同时关闭、自定义2.5秒冷却/1.5秒反馈/9文案/RGB/尺寸、地面存档关闭、仅存档HUD、全部显示关闭、永久Lifetime=0、G单行52/状态236/存档304及手动/HUD同时关闭。两来源等价，全16Settings及Hidden/0/None、原工具/反馈/F/G/高亮/资源状态、三掉落Prefab/Prepared和完整布局保持；资源签名与原v17/20烘焙结果相同。森林/草地树89/53、采集36/38、矿20/18、阻挡109/71保持。

烘焙Console前后均[2 Error,1 Warning,0 Log]，没有新增错误；2条为接入中提前刷新旧Forest配置缺manualSaveEnabled的历史烘焙错误，JSON补齐并升级后上述26次重新烘焙全部成功；1条为MCP WebSocket未初始化警告。AI未清Console，主场景干净，临时Scene/World/TextAsset释放。

主线程代码/配置静态验收通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有正常Unity编译、实际字段/序列化、26次隔离Editor烘焙与用户反馈判定通过，限CombatPrototypeNetCode、schemaVersion=18/configRevision=21及[运行入口](Runtime.md)十二项清单，人工结论来自用户反馈。原237项编号/内容及v17/20寿命等旧用户通过保持原版本/清单。未实际触发的独立资格/同tick/多Client/延迟/晚加入、捕获/保存/关闭失败、原子替换及恢复、显示/字形/布局/颜色和生命周期仍UNKNOWN；跨文件原子一致/防重复、意外ECS故障恢复、同槽并发与性能/带宽/平台/线上仍UNKNOWN。AI未运行真实玩家/世界存档读写、游戏/显示系统/GUI回调、GamePlayer/PlayMode、逻辑单元测试、命令行构建、发布、采样或图片检查，未创建子Agent或提交Git。

## 【FACT】容量等级接入边界

[升级](MapInventoryCapacityUpgrade.md)：当前19输入含数字5升级与F5世界保存，各入口沿自身资格；世界格式/原F5冷却保持，玩家v4工具/容量等级保存由升级及原九入口处理。新链静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧通过保持原版本/清单。
