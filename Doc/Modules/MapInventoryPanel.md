# 材料背包与制作面板

返回[背包与道具](Inventory.md)和[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的本地库存列表、工具耐久、配方预览、制作按钮及客户端输入隔离。制作资格、材料/工具事务和成功反馈仍归[采集工具](MapGatherTools.md)，存档归[资源与数据](DataResources.md)，人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [MapInventoryPanelConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs) | inventoryPanel 的严格 JSON DTO |
| [InventoryPanelData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原地图根上的固定显示 Settings；无 GhostField/新玩家数据 |
| [InventoryPanelSnapshot](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSnapshot.cs) | 原库存缓冲的只读展示投影及材料计数 |
| [InventoryPanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | GUI 列表/滚动/按钮、本地开关和待提交按钮请求 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原根追加 InventoryPanelSettings 烘焙数据 |
| [HUD 绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 所属玩家/连接检查、配置、投影与输入交接 |
| [HUD 宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) | 复用 Main Camera 原组件，委托绘制并共享原制作反馈 |
| [PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) | B 本地开关、按钮与原1/2合并、面板指针隔离 |

新 Snapshot/Panel 是普通 C# 类，没有新 MonoBehaviour 挂载。v9面板阶段四个脚本及对应meta由正常Unity导入生成。主场景、SubScene、Prefab、Animator、旧 meta、资源绑定、图片/字体、包和构建设置保持。v9面板接入只增加显示烘焙数据；v10丢弃另有输入与玩家所属反馈布局变化，归[丢弃](MapInventoryDrop.md)；v11新增所属G显示数据，归[拾取提示](MapPickupHud.md)，各端须同版代码/配置及重新烘焙。正式 Map 的 QFramework 背包/99拆格与道具详情保持原边界。

## 【FACT】JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) 与 [BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) 当前一致为 schemaVersion=19/configRevision=22。必填 inventoryPanel 共30字段，沿原严格UTF-8、完整字段/类型及未知/重复键检查；即使关闭面板也校验全部字段。旧地图v1～v18明确失败，不补段/默认值或回退来源；正常导入/烘焙后生效，无运行热重载。原工具配方/耐久仍从 gatherTools 唯一读取，面板不复制配置真值。

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

全部22文案须非空白、无控制字符且最多61个UTF-8字节，烘焙为FixedString64Bytes。内置GUI字体和白色纹理复用；当前英文文案，中文接口保留，字体/字形覆盖为UNKNOWN，没有读取图片或界面截图。

## 【CURRENT STRATEGY】库存、工具与配方显示

客户端仅枚举启用GhostOwnerIsLocal的一个玩家，并核对其GhostOwner对应Connected/InGame且无断线请求的连接；没有有效本地Ghost/连接时清空显示。展示原CombatPrototypeInventoryItem的正数量条目及原顺序，不设第二份可变库存，不按99拆格、不排序；零数量不列出。材料标题后增加一行总量/上限，受管材料行显示数量/单种上限，关闭限制显示Unlimited，规则归[容量](MapInventoryCapacity.md)。木材/石材/活力苹果/小块肉映射上述Wood/Stone/Apple/Meat文案，其余合法名称直接按原ItemName显示。ToolId/Durability仍来自原所属工具缓冲，工具独立于库存，两槽显示当前/最大耐久、未持有或损坏/功能关闭状态。

新Snapshot只缓存显示文本/数量；条目名称空白/控制字符、负数量或重复名称按索引/地图/玩家记录错误并跳过该项，其余独立条目继续，当前快照有错误时禁用制作、修理和丢弃按钮，不用展示数据修正库存。原工具网络数据仍由原HUD统一校验。必需地图配置、客户端绑定/宿主缺失明确暴露错误，不查找或创建组件兜底。

两条配方直接读取原definitions：斧头木3/石2、镐子木2/石3；分别列出现有/需要与max(需要-现有,0)缺少数量。只有工具开关启用、库存展示快照合法、材料充足且工具未持有或耐久小于单次成本时按钮可用。Ready仅是当前客户端材料/耐久预览，连接、生命、静止、无攻击、近战Ready、资源预约互斥及同tick优先仍由原服务端制作系统决定；网络快照滞后时服务器可拒绝，不按客户端预览扣料/发工具。工具功能关闭保留已有耐久并禁用按钮。

面板右上，按min(屏幕宽/1920,屏幕高/1080)等比缩放。标题和共享反馈/关闭按钮固定，中间列表滚动；背景alpha=0.85。缓存条目/配方/按钮文本和GUI样式；GUI绘制后恢复matrix/color/enabled。新的按钮/滚动参与正常GUI事件，原F提示/进度仍仅Repaint且保持底部320×104。

## 【CURRENT STRATEGY】输入、请求与生命周期

B单次按下在GhostInputSystemGroup内切换本地面板，同一渲染帧不反复切换；底部B: Close也关闭。开关不入输入命令/存档、不修改Time.timeScale，打开后WASD、空格、F/G/E/R和镜头Z/X仍沿原规则。指针落在当前可见面板的同一缩放矩形内，左键不写Attack，摄像机ReadMove只接收原keyboard与null mouse，使面板滚轮不缩放镜头；矩形外原鼠标攻击/镜头滚轮保持。B关闭与鼠标同帧时仍屏蔽原面板内该次按下。

制作按钮只缓存本地Axe/Pickaxe布尔请求，要求鼠标按下来自本次有效绑定的可见面板；MouseUp结束后清理按下标记。输入系统下一次读取时重新核对地图、所属玩家、连接及生命，消费并清空按钮请求，再与数字1/2按下合并到原CraftAxe/CraftPickaxe InputEvent；原制作按钮不增加RPC、命令字段或保存入口；丢弃请求归[丢弃](MapInventoryDrop.md)。原服务端F优先、两类同时斧头优先、忙碌拒绝、仍可用拒绝及SavePrepared先于扣料/工具提交保持。一个按钮请求消费一次，没有自动重试；再次主动点击仍按当前服务器资格处理。

制作成功/拒绝/失败共享原所属Sequence/Kind/Result，当前2秒展示期限由原宿主处理，只用于显示；新绑定只观察现有Sequence，不重播旧反馈。interactionHud.enabled只关闭F提示/进度，inventoryPanel.enabled独立；关闭F HUD仍可B打开本面板/查看制作反馈，关闭本面板仍可用原1/2。F/G文字、面板、高亮及资源状态全关时宿主清空显示，工具玩法由原开关控制。

死亡、断线/无本地Ghost、玩家或地图源变化、关闭面板、World/Scene停止或释放清掉未提交按钮请求；绑定释放同时清库存投影、滚动位置、鼠标按下标记及旧反馈。重建有效绑定按initiallyOpen应用初始状态，默认保持关闭。已经消费进原输入命令的请求仍由原服务器链处理，不通过关闭面板撤销已提交制作。面板不保存/修改库存、耐久、世界资源、再生期限、金币/经验或玩家档案，沿原v2写入/v1读取迁移。

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

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：当前16输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。
