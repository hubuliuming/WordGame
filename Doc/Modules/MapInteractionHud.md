# 资源交互提示与进度显示

返回[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的 F 资源提示、权威进度快照和屏幕 HUD；采集/砍伐/采矿、产出、G 拾取、存档及再生仍归原模块，人工清单归[运行入口](Runtime.md)。

## 【FACT】真实入口与职责

| 文件 | 职责 |
|---|---|
| [HUD 配置](../../Assets/Scripts/CombatPrototype/Map/MapInteractionHudConfig.cs) | interactionHud 的十个 JSON 字段 |
| [HUD 数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudData.cs) | 地图烘焙显示配置与玩家所属 Ghost 快照 |
| [服务端状态](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudStateSystem.cs) | 在原资源维护/再生与 PlayerRespawn 后读取目标和真实进度，只写 HUD |
| [客户端绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | Presentation 读取本地所属玩家快照并交给显示组件 |
| [HUD 显示](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) | Client World 注册/解绑，缓存文案与样式，OnGUI 绘制固定面板 |
| [F 入口](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs) | GetInteractionHintRejection 只读复用原 RejectPlayer；原 F 执行流程保持 |
| [目标选择](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionTargetSelector.cs) | F 与 HUD 共用同一 Select |
| [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 在原玩家实体追加初始 Hidden HUD 组件 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 在原地图根烘焙 HUD Settings |

[主场景](../../Assets/Scenes/CombatPrototypeNetCode.unity) 的 Main Camera 原四个组件保留，只追加 CombatPrototypeMapInteractionHud，序列化组件 fileID=329441056，脚本 GUID=55daeba957df67347a415a043d00bd70。主场景仍为三根对象；没有新 Canvas、EventSystem、场景根、UI Prefab、字体或图片资源。原网络 Player Prefab 文本/层级保持，Baker 新增 ECS 数据改变玩家烘焙后的 Ghost 布局，各端须使用同版代码、配置与重新烘焙的数据。

## 【FACT】当前 JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) 与 [BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) 为 schemaVersion=37/configRevision=40，interactionHud、[高亮](MapInteractionHighlight.md)、[G提示](MapPickupHud.md)、[工具配置](MapGatherTools.md)与[面板配置](MapInventoryPanel.md)均必填；interactionHud全部十个字段必填，沿原严格 UTF-8/字段/类型/重复键检查。旧 v1～v36 明确失败，不补默认段或回退来源；JSON 只在正常导入和烘焙后生效，无运行热重载。

| 字段 | 默认值 | 契约 |
|---|---|---|
| enabled | true | 仅控制 F 文字/进度；F文字/F高亮/资源状态均关闭时快照Hidden，原 F/G 继续 |
| panelWidthPixels | 320 | 有限，32 < 宽度 <= 1920 |
| panelHeightPixels | 104 | 有限正数；高度 >= 2×fontSize + progressBarHeightPixels + 54 |
| bottomMarginPixels | 48 | 有限非负；高度 + 底距 <= 1080 |
| fontSize | 20 | 正整数 |
| progressBarHeightPixels | 10 | 有限正数 |
| gatherLabel | Gather Apple | 非空白、无控制字符、至多 61 UTF-8 字节 |
| treeLabel | Chop Tree | 同上 |
| mineLabel | Mine Rock | 同上 |
| noSpaceLabel | Not enough space | 同上；[容量](MapInventoryCapacity.md)拒绝时使用 |

即使 enabled=false，其余字段仍按上述契约校验。显示按 1920×1080 参考像素，以 min(屏幕宽/1920,屏幕高/1080) 等比缩放，底部居中；面板黑色背景 alpha=0.7，第一行/原制作修理反馈白色，原[F启动失败](MapInteractionFailureHud.md)用配置红色，[完成/中断](MapGatherOutcomeHud.md)用配置绿/橙/红色，第二行工具状态颜色归[耐久预警](MapToolDurabilityHud.md)，进度背景白色 alpha=0.2，填充 RGB=(0.85,0.65,0.3)。使用内置 GUI 样式字体与 Texture2D.whiteTexture，不导入新的字体/纹理；当前英文文案，中文文案字段可配置，但中文字体与实际字形覆盖未确认。

## 【CURRENT STRATEGY】权威采样与本地显示

服务端 HUDStateSystem 在 PredictedSimulation、PlayerRespawn 后执行；原采集/砍伐/采矿及其再生均在复活前完成。每次读取原地图和三类资源，仅为 Connected、InGame、未请求断线、CommandTarget 指向当前启用 Simulate 玩家的连接采样。可交互条件调用原 RejectPlayer：所有权匹配、存活、有限零 Move、无攻击请求且近战 Ready。非法归属连接不覆盖真实所属玩家快照。

本人正在 Collecting/Chopping/Mining 时固定使用原 Collector 实体对应目标，不因其他资源更近而切换。进度由服务端模拟时间和原 FinishAt、本次锁定的 ActualDuration（植物仍为原 GatherDuration） 计算 round(clamp(1-(FinishAt-now)/duration,0,1)×1000)。默认徒手耗时1/2/3秒，斧头/镐子为1.5/2.25秒；工具完成保存归[采集工具](MapGatherTools.md)，HUD不另起工作计时器或提前完成。

空闲植物目标另按[容量](MapInventoryCapacity.md)检查，拒绝时NoSpace而非Working；保留最近目标身份。空闲时复用同一Select：各类型原交互距离筛选，只选Available/Standing/Available，按X/Z中心最近、同距较小PlacementIndex；关闭砍伐/采矿不选相应类型。提示不预约，按F仍由当tick选择/预约。不可交互时原F目标/进度隐藏，制作/修理、启动失败与[采集结果](MapGatherOutcomeHud.md)仍按各自条件临时显示；死亡时整个HUD收起。收到取消/完成状态后清进度，可显示下一有效目标或隐藏；显示代表最近权威快照，网络延迟可滞后，Working百分比不是发奖/保存成功标志。

CombatPrototypeMapInteractionHudState 用 OwnerSendType=SendToOwner 同步给所属玩家：

| 字段 | 声明与含义 |
|---|---|
| Mode | byte 枚举：0 Hidden、1 Ready、2 Working、3 NoSpace（仅Gather） |
| Kind | byte：0 None、1 Gather、2 Tree、3 Mine |
| PlacementIndex | int；Hidden 为 -1，其余是原布置索引 |
| ProgressPermille | ushort，0～1000；非 Working 为 0 |

状态仅含显示数据；FinishAt、RegrowAt、Collector实体引用和原资源计时配置仍仅服务端，工具Definitions由各端烘焙供所属工具行读取。F提示/进度不写资源Phase/Collector/历史/障碍、玩家输入/属性/库存、掉落或存档；同一宿主的背包面板只缓存按钮请求，由原输入系统消费。原资源Ghost字段与F/G/E/R效果保持，数字1/2制作归工具专题。服务器每tick重建缓存，字段不变不重复写；F文字、F高亮与资源状态均关闭，或连接无效、未采样、地图停止时清Hidden；采样条件为interactionHud.enabled、启用的F高亮通道或resourceStatusHud.enabled，后者保留原F身份供状态提示优先使用。

客户端枚举启用的 GhostOwnerIsLocal，不在含该可启用组件的查询上调用单例 API；只显示本地所属玩家且死亡时收起，不显示远端玩家进度。F文字路径不读取PlayerView或世界位置；[高亮](MapInteractionHighlight.md)额外读取所选资源的客户端LocalToWorld，不替代官方Transform显示桥接。Main Camera 组件随 Client World 变更注册/解绑；无客户端、准入未完成、没有本地 Ghost 或地图停止时隐藏。World/Scene 释放清掉显示和引用，不保留上一局目标、进度或任一反馈；本地玩家或地图源变化也重置反馈序号观察。

NoSpace第一行显示noSpaceLabel、第二行显示F目标，不画进度；Ready 显示“F  文案”；Working 显示“文案  百分比%”和按千分比填充的进度条，整数百分比为 ProgressPermille/10。第二行读取所属工具缓冲显示对应名称/Level/本级耐久上限、Hands、3/4修理或Lv1重做提示（依耐久预警/修理开关），制作/修理反馈默认各2秒；F显示优先级为NoSpace>启动失败>完成/中断>修理>制作>工具状态；新F失败有Ready/Working目标时只替换第二行，Hidden时临时第一行显示且第二行空，成功启动清旧失败、默认2秒后恢复原工具颜色。原制作/修理无F目标时仍临时显示结果及对应工具；B页脚继续接收原修理/制作反馈。初次绑定只观察现有Sequence，不重播旧结果；只有反馈显示使用客户端unscaledTime。F面板仍只在Repaint绘制，库存列表/按钮委托[制作面板](MapInventoryPanel.md)处理GUI事件；B由客户端输入系统读取。缓存文案与样式，绘制后恢复GUI.matrix/color/enabled。F文字面板不显示掉落拾取提示、世界标记或再生倒计时；独立G面板归[拾取提示](MapPickupHud.md)，宿主先绘制[投影圆环](MapInteractionHighlight.md)再绘制原面板。

资源采样错误按单项暴露地图、类型、布置索引、实体和原异常并继续；玩家帧错误按连接隔离，未生成有效快照者清为 Hidden。必需服务、地图 Settings、HUD 挂载或本地 HUD 数据缺失明确报错，不查找节点、不创建替代组件或默认配置；非法非隐藏网络快照明确报错并保持隐藏。

## 【KNOWN ISSUES】静态核对与人工边界

下述静态与用户通过保留HUD v7/revision=10范围。正常Unity编译无C# Error；生成的 HUD Serializer/Snapshot 四字段、SendToOwner、系统顺序及五个脚本导入已静态核对。Forest/Grassland 各覆盖 Json 默认、BuiltIn 默认、Json 关闭采矿、Json 关闭矿点再生（仍 600 秒）、Json 关闭 HUD，共十次隔离 Editor 烘焙；v7/revision=10、HUD 参数、玩家 Prefab 初始 Hidden/Kind=0/PlacementIndex=-1/进度=0 均一致。默认森林/草原仍为矿点 20/18、阻挡 109/71、树木 89/53、采集点 36/38；HUD 关闭不改布局，采矿关闭恢复原四类布局。全部初始布置位置/朝向、资源引用、保护区/占地/间距及敌人初始重叠均通过。烘焙 Console 前后 [0 Error,9 Warning,53 Log]，无新增烘焙警告；主场景已保存，临时烘焙 World/Scene 已释放。

主线程静态验收通过；用户已确认本阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定通过，范围限 CombatPrototypeNetCode、v7/revision=10 和[运行入口](Runtime.md)HUD 八项清单。人工结论来自用户反馈；未实际触发的精确距离/同距、同 tick、延迟/预测回放、晚加入和独立保存/创建/提交/清理/回滚失败仍为 UNKNOWN，中文字体/字形覆盖未确认。既有矿点再生、统一 F 与各旧阶段通过保持原版本/清单；第九阶段原 J、第二阶段独立 JSON 人工 UNKNOWN 保持。性能/带宽开销、平台构建和线上联调未测量。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交 Git。

工具v8/revision=11接入第二行和制作反馈，实际锁定耗时用于服务器进度；正常编译和十次工具隔离烘焙已核对，用户确认工具显示人工通过限v8/revision=11十二项清单，未实际触发的独立用例仍为UNKNOWN，完整静态与人工边界见[采集工具](MapGatherTools.md)/[运行入口](Runtime.md)。原HUD八项用户通过不覆盖本次新行为。

v9/revision12阶段复用原Main Camera宿主接入[材料背包与制作面板](MapInventoryPanel.md)，两个显示开关独立。关闭F HUD仍保留本地面板/制作反馈，关闭面板仍保留F HUD；绑定先核对所属Connected/InGame连接，v9时两者关闭或生命周期失效时清空；当前显示开关见末段。原服务端四字段不变，本次编译/十四次隔离烘焙静态通过；用户确认面板人工通过限v9/revision12及[运行入口](Runtime.md)十二项，未触发的独立失败/时序用例仍UNKNOWN。

当前地图v26/29必填[背包丢弃](MapInventoryDrop.md)，复用原掉落资源及保存链；本专题原交互/工具/产出/再生行为保持。新增丢弃静态及用户人工通过限[运行入口](Runtime.md)v10/13十二项，未触发用例UNKNOWN；旧通过仍限原版本/清单。

v11/14阶段复用原宿主/绑定接入[G提示](MapPickupHud.md)：pickupHud、interactionHud及inventoryPanel独立，三者全关闭或生命周期失效才收起整体绑定；F/G可同时显示，F四字段和进度/工具/反馈规则保持。G新增所属四字段、五职责脚本/meta及显示Settings，未改Scene/Prefab/Animator结构。编译/十次隔离烘焙静态通过，用户确认G显示人工通过，限v11/14十项，未触发用例UNKNOWN，旧通过保持原范围。

v12/15高亮复用原F四字段与Working锁定目标接入[资源交互高亮](MapInteractionHighlight.md)，不增加输入/Ghost字段。F/G文字与两类高亮独立，三文字全关、没有启用高亮通道且资源状态/存档HUD均关闭时才收起绑定。正常编译/14次隔离Editor烘焙静态通过；用户确认高亮人工通过限v12/15十项，未触发独立用例UNKNOWN，旧F/G及工具/面板/丢弃用户通过仍限各自版本/清单。

v13/16的[资源状态](MapResourceStatusHud.md)复用此宿主，独立显示三类资源状态与服务端剩余秒数，F文字面板本身保持。F采样增加状态开关作为身份消费者；仅状态开启时仍保留原F目标优先级。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN；旧通过仍限各自版本/清单。

当前v25/28的[修理反馈](MapToolRepair.md)由新普通缓存类读取所属Sequence/Kind/Result并委托原F第二行/B页脚显示，初次绑定不重播；无F目标显示结果及对应工具。非法修理反馈只清本通道并记录原异常，Configure/Reset清旧序号、期限/身份。原F进度、G、高亮/资源状态开关及计时保持；静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN。

## 【FACT】地图资源存档接入边界

新增资源恢复沿原Ghost阶段/进度读取，预约/工作进度不恢复，不增加F显示字段、按钮或本地倒计时；原修理反馈与优先保持。资源存档人工通过限v15/18十二项，未触发用例UNKNOWN，归[资源存档](MapResourcePersistence.md)。 掉落恢复及同文件快照归[掉落存档](MapDropPersistence.md)，人工通过限v16/19十二项，未触发用例UNKNOWN。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)由原Main Camera宿主/绑定委托新普通助手绘制；只关闭worldSaveHud不关闭F5，所有原显示关闭但本HUD开启仍保留绑定。原F进度/工具反馈优先与G/高亮/资源状态保持；静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN。

## 【FACT】容量等级接入边界

[升级](MapInventoryCapacityUpgrade.md)：F采样按实际Level选择容量；新升级反馈只在B显示，原F进度/工具反馈和目标身份保持。新链静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧通过保持原版本/清单。

## 【FACT】工具等级显示

[升级](MapGatherToolUpgrade.md)增加所属工具Level和B专属结果；原F四字段、工作进度/目标、高亮/G/资源状态与制作/修理反馈优先保持。绑定读取四条固定升级定义及树/矿基础耗时，HUD校验本级上限、按等级变化刷新原第二行和B/修理预览。新结果仅在B工具升级通道显示，输入19，原生命周期Reset同步释放。编译/22次隔离Bake静态通过；用户确认工具升级人工通过限v21/revision24二十二项，未触发用例UNKNOWN；旧HUD用户通过保持原版本/清单。

## 【FACT】耐久预警显示接入

地图schema22/revision25新增必填[gatherToolDurabilityHud](MapToolDurabilityHud.md)11字段/固定Settings；原绑定传给HUD普通只读助手。RefreshToolStatus沿原ID/唯一性/等级/本级上限校验，再按Durability与Level缓存计算颜色；等级变化且耐久未变仍刷新。F第一行、NoSpace、反馈优先及进度保持；只在对应工具状态第二行使用预警颜色，损坏且修理开启提示3/4，关闭修理仍1/2重做。无目标仍仅原反馈临时显示，没有独立耐久弹窗/计时器；B委托原面板显示两条新增详情。每帧Clear只收起，绑定失效等原Reset清新增状态/颜色。原F四字段/输入19、资源/存档/Camera组件与结构保持。静态及用户人工通过，限v22/revision25十六项；未触发的新增显示/排版/字形/生命周期和联网分支UNKNOWN，见[运行入口](Runtime.md)。

## 【FACT】统一F失败所属反馈

当前v25/revision28必填[interactionFailureHud](MapInteractionFailureHud.md)九字段/固定Settings；原Player Baker初始化Sequence0/ResultNone二字段所属Ghost，统一F只投影原请求启动的实际拒绝原因。原F四字段与HUDStateSystem、目标/进度/选择器及三类采集原玩法保持；旧NoSpace优先、新失败红色且仅F、B原反馈保持。新普通缓存初次不重播，Clear隐藏/Reset清序号与期限/文字/颜色，原绑定全部显示关闭时仍退出。此前v23/revision26编译/实际Serializer、224份非法配置拒绝与32次隔离Bake静态通过；用户确认本阶段人工通过限v23/revision26十六项，未触发的显示/错误隔离/联网/生命周期用例UNKNOWN，原耐久预警通过仍限v22/revision25十六项。

## 【FACT】采集结果所属显示

v24/revision27新增[完成/中断](MapGatherOutcomeHud.md)十三字段固定Settings与Sequence/Kind/Result三字段所属反馈。服务端实际完成提交或Cancel成功才写结果，原F四字段与进度采样保持。F按NoSpace>启动失败>完成/中断>修理>制作>工具显示，B仍接原displayFeedback；Ready/Working只换第二行，Hidden第一行结果/第二行对应工具或Hands。原Clear隐藏/Reset清新增缓存，首次不回放，原开关/生命周期及绘制结构保持。静态及用户人工通过，限v24/revision27十六项，未触发独立用例UNKNOWN；旧通过仍限原版本/清单。

## 【FACT】原G面板的实际结果显示

v25/revision28的[拾取反馈](MapPickupFeedbackHud.md)经原绑定/宿主传九Settings及四字段所属结果给PickupHudClient，G缓存与F/B反馈独立。原NoSpace目标优先保留物品/数量/寿命，有效新NoSpace窗口内第一行标红；其他结果居中单行暂替原G目标/寿命，到期恢复，布局/高亮目标保持。Clear逐帧仅隐藏、Reset清新增缓存；G关闭或新结果关闭不显示消息，原全部显示关闭仍退出绑定。F完成/失败优先级、B原displayFeedback及原OnGUI委托顺序保持；新显示静态及用户人工通过限v25/revision28十六项，未触发独立用例UNKNOWN，F完成/中断通过仍限v24/revision27十六项。

## 【FACT】合并与原F/G显示边界

v26/revision29的[地面合并](MapDropMerge.md)通过原掉落Quantity/Phase及服务端期限改变G真实目标/数量/余时，不接入本宿主、绑定或新增显示通道。原G目标/NoSpace/高亮及实际拾取结果读取原快照，F完成/失败优先级、B反馈/输入、布局和原全部显示关闭条件保持；合并开关不会强制HUD。新链静态及用户人工通过限v26/revision29十六项，未触发独立用例UNKNOWN，旧显示通过保持各自原版本/清单。

## 【FACT】G可领取量投影

当前v37/revision40的[部分拾取](MapDropPartialPickup.md)在原G所属快照增加PickupQuantity；原绑定整体传递该struct，宿主/高亮/面板布局与F/B优先级保持。G显示本次可领量/地面量或零余量NoSpace，实际结果仍暂时覆盖Ready目标；全部显示关闭只关闭展示。输入19、F4/资源状态4及原结果字段保持，静态及用户人工通过限v27/revision30十六项，未触发独立用例UNKNOWN。

## 【FACT】B列表本地排序筛选

当前v37/revision40的[排序筛选](MapInventoryListView.md)沿原绑定整体传递InventoryPanelSettings到宿主/Panel；v28排序筛选阶段原绑定、Main Camera组件和输入系统代码保持。Panel.Show在完整库存Capture后应用本地模式并缓存可见行，原GUI滚动区新增0～2控制行；F/G提示、高亮、资源/世界保存状态和原所属反馈布局保持。关闭保留该绑定已应用选择，死亡/断线/源或玩家变化及World/Scene释放沿原Reset清缓存；新绑定先配置默认，再由[本机偏好](MapInventoryPreferences.md)恢复启用能力的合法选择。静态及用户人工通过限v28/revision31十六项，人工结论来自用户反馈；未实际触发的独立GUI事件/字体/排版/联网时序仍UNKNOWN。

## 【FACT】B搜索输入隔离

当前v37/revision40的[搜索](MapInventorySearch.md)在原Binding.ReadPanelInput→HUD.ReadPanelInput→Panel.ReadInput传递本地blocksKeyboard；原有效地图、存活所属Ghost/Connected/InGame检查保持。PlayerInput先采样面板再按焦点决定是否采样键盘，十九字段及原Ghost/反馈布局保持。Show在完整Snapshot之后应用待处理搜索、分类交集及排序；逐帧Clear只隐藏展示，不清编辑状态，原Reset绑定失效时清搜索。文本焦点只在原OnGUI读取/设置/释放，无新宿主或组件。搜索静态及用户人工GamePlayer通过，限v29/revision32及运行入口十六项，结论来自用户反馈；未实际触发的独立GUI/焦点/联网用例UNKNOWN，v28排序筛选通过不覆盖搜索。

v30/revision33保存阶段的[本机偏好](MapInventoryPreferences.md)复用原Configure传递的mapDefinitionId与完整Settings，原Binding/HUD脚本不变。Panel在ListView应用选择/搜索之后观察已应用状态，Close/Reset提交待保存值，逐帧Clear仍只隐藏；输入、F/G目标、反馈和游戏存档布局保持。静态核对通过，新增十六项人工已获用户通过反馈，范围见[运行入口](Runtime.md)，未实际触发的独立用例仍UNKNOWN。

v31/revision34重置阶段显示重置由原Panel的GUI排队，在有效Show的Snapshot之后应用，原HUD/Binding代码与入口不变；57Settings沿原Configure整体传递。焦点释放与面板指针隔离沿原链路，F/G目标、反馈和服务器存档保持；静态通过，重置人工十六项已获用户通过反馈，限本阶段版本/清单，未触发独立用例仍UNKNOWN，归[偏好](MapInventoryPreferences.md)。

## 【FACT】材料详情传递

v32/revision35详情阶段的[详情](MapInventoryDetails.md)由原Panel持有普通辅助类，71Settings及工具/容量配方沿原Configure/Show传入；HUD/Binding入口与代码保持。原搜索焦点、指针隔离、F/G显示与所属反馈不变，无新挂载或服务器调用。详情人工十六项已获用户通过反馈，限v32/revision35及运行入口清单，未触发独立用例仍UNKNOWN，原通过保持各自清单。

## 【FACT】收藏显示边界

v33/revision36收藏阶段的[收藏](MapInventoryFavorites.md)：复用原宿主/绑定的完整Settings传递；收藏仅在原B面板内绘制、应用与保存本机显示偏好，F提示/进度、输入链与服务器资格保持。人工收藏十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN，旧通过仍限各自版本/清单。

## 【FACT】仅看收藏边界

v34/revision37筛选阶段的[收藏筛选](MapInventoryFavoritesFilter.md)：沿原完整Settings到Panel传递，不改HUD/Binding代码、F/G目标和进度、所属反馈或新输入；新增控制只在原B面板滚动区。本阶段十六项人工GamePlayer已获用户通过反馈，限上述版本及运行入口清单；未触发独立用例仍UNKNOWN，旧通过范围保持。

## 【FACT】收藏计数边界

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)：沿原整体Settings传递到Panel，HUD/Binding、F/G目标/进度和所属反馈代码保持；新增计数只在B滚动区。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；筛选旧通过限v34/revision37及其他阶段原版本/清单。

## 【FACT】B面板收藏丢弃保护

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：原HUD/Binding代码、Settings整体传递、本地玩家/连接检查和F/G目标/进度/所属反馈保持。保护仅在Panel/DropClient读取同一Favorites；没有新宿主/挂载或场景结构，原Clear隐藏和Reset绑定失效边界保持。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

当前v37/revision40的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段人工GamePlayer待验收，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。
