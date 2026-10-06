# 掉落物拾取成功与失败反馈

返回[地图](Map.md)、[掉落与拾取](MapDrops.md)、[G目标及寿命](MapPickupHud.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode；Forest/Grassland Json与BuiltIn当前schemaVersion=32/configRevision=35。本专题负责实际G请求结果的所属快照与原G面板显示。静态及用户人工GamePlayer通过；范围限v25/revision28十六项，详见下述人工边界；旧F完成/中断通过限v24/revision27十六项，其他旧通过保持原版本/清单。

## 【FACT】文件与接入

| 职责 | 文件 |
|---|---|
| 九字段DTO | [MapPickupFeedbackHudConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapPickupFeedbackHudConfig.cs) |
| 九字段固定Settings | [PickupFeedbackHudData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupFeedbackHudData.cs) |
| 枚举与四字段所属结果 | [PickupFeedback](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupFeedback.cs) |
| 原因映射及隔离写入 | [PickupFeedbackUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupFeedbackUtility.cs) |
| 普通客户端结果缓存 | [PickupFeedbackHudClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupFeedbackHudClient.cs) |
| 配置与根Baker | [MapDefinitionConfig](../../Assets/Scripts/CombatPrototype/Map/MapDefinitionConfig.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 原G请求及保存提交 | [DropPickupSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs) |
| 所属初值、绑定与原宿主 | [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs)、[BindingSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs)、[InteractionHud](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) |
| 原G绘制 | [PickupHudClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudClient.cs) |
| 显式配置 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

五个新脚本为普通代码，meta由Unity正常导入生成；没有新MonoBehaviour、Scene/SubScene/Prefab/Animator结构或资源绑定。原PickupHudStateSystem、DropTargetSelector、拾取资格、NetworkId升序、Main Camera注册、本地GhostOwnerIsLocal枚举与绑定退出条件保持。Player Ghost追加结果组件，各端须同版代码/配置并重新烘焙；没有新输入、RPC、事件队列、联网配置协议或存档字段。

## 【FACT】JSON与烘焙契约

必填pickupFeedbackHud恰九字段，两地图与BuiltIn一致：

| 字段 | 默认值 | 约束 |
|---|---|---|
| enabled | true | bool；仅控制新增显示，关闭仍完整校验 |
| feedbackSeconds | 2 | 有限正float，仅控制客户端显示寿命 |
| successColorHex | #6ED88A | 严格#RRGGBB |
| failureColorHex | #FF6B6B | 同上 |
| successLabel | Picked up | 非空白、无控制字符、最多61 UTF-8字节 |
| movingLabel | Stop moving first | 同上 |
| attackingLabel | Finish attack first | 同上 |
| noTargetLabel | No available drop | 同上 |
| failedLabel | Pickup failed | 同上 |

三物品名称与NoSpace直接复用pickupHud.appleLabel/woodLabel/stoneLabel/noSpaceLabel，没有第二份定义。原严格JsonReader沿DTO检查UTF-8、形状、缺失/null/未知/重复键及标量类型；语义复用Positive/HighlightColor/HudLabel。旧地图v1～v28明确拒绝，不迁移、不补默认段、不回退来源；正常导入/烘焙后生效，没有运行热重载。

原地图根Baker写九字段Settings：Enabled(byte)、FeedbackSeconds(float)、SuccessColor/FailureColor(float3 RGB)及五个FixedString64Bytes。它不是Ghost；绑定在地图源/本地玩家变化时传给原G客户端。普通缓存只保存配置、物品标签、观察序号、结果、文字、颜色与期限，不跨帧持有DynamicBuffer。英文为默认值，可配置中文，实际字体/字形与排版UNKNOWN。

## 【CURRENT STRATEGY】原请求的实际结果

仅处理原input.Pickup.IsSet请求。requested标记在读取并确认事件后设置；未能读取输入时沿原错误日志，不凭异常推断按过G。rewardSaved仅在原SavePrepared正常返回后置true，用来隔离保存后部分提交异常的反馈；原资格、目标、容量、PrepareReward、保存与提交顺序保持。

| 原处理入口 | 新结果 |
|---|---|
| 原SavePrepared正常返回、库存及地面余量或Consumed完整提交、原完成日志结束 | PickedUp；携带本次实际ItemId/Quantity，默认绿色 |
| PlayerMoving | PlayerMoving；movingLabel，红色 |
| AttackInProgress | AttackInProgress；attackingLabel，红色 |
| NoLandedTarget | NoLandedTarget；noTargetLabel，红色 |
| InventoryAlreadyOverCapacity / MaterialTotalCapacityExceeded / MaterialItemCapacityExceeded | NoSpace；复用原G容量文案，红色 |
| InvalidMoveInput，或已确认G请求的准备/保存前异常 | Failed；failedLabel，红色 |
| CommandTargetOwnerMismatch / PlayerDead，未进入原在线玩家批次 | 不向玩家写新结果，沿原日志/隐藏/Reset |
| SavePrepared正常返回后的部分提交异常 | 沿原错误日志，不发布PickedUp或宣称拾取失败/回滚成功；恢复仍UNKNOWN |

成功物品及数量在原target状态仍有效时取得，Quantity是本次增量，不是累计库存；后续目标切换、Consumed与销毁不会改变这份结果。无合格目标沿原选择器判定，不区分已被别人取走、到期、飞行中或超范围原因，统一No available drop。零余量、旧超限或关闭部分拾取后的容量拒绝不领取、不改选较远目标。失败不延长寿命、不自动重试，恢复后仍须新G；保存成功才提交库存及地面余量或Consumed，原同局DropId、生成/运动/到期/清理及世界快照保持。

共享写入辅助在单独try/catch内映射原原因、核实真实GhostOwner.NetworkId及存活状态并写入。缺少必需组件或未知原因明确记录stage=WriteFeedback、map/NetworkId/player/DropId/itemId/quantity/result/reason及原异常，不动态补齐。写入错误不传播进原拾取catch，不撤销已保存/提交业务，也不阻断后续玩家；不追加保存调用。

## 【FACT】四字段所属最新快照

CombatPrototypeMapPickupFeedback恰四GhostField，OwnerSendType=SendToOwner：

| 字段 | 类型与含义 |
|---|---|
| Sequence | uint；每次结果unchecked递增 |
| Result | byte枚举：None=0、PickedUp=1、PlayerMoving=2、AttackInProgress=3、NoLandedTarget=4、NoSpace=5、Failed=6 |
| ItemId | FixedString64Bytes；仅PickedUp携带原vitality_apple/wood/stone，其他结果为空 |
| Quantity | int；仅PickedUp为实际正增量，其他结果为0 |

原Player Baker初始0/None/空/0。只发最新快照，较新请求结果可以覆盖之前结果；没有结果历史、可靠事件队列或客户端目标。当前G目标七字段与Drop Ghost四字段保持独立；结果不用于选目标、入包、清理、资源工作或存档。

## 【CURRENT STRATEGY】原G面板与生命周期

客户端读取两份所属快照：七字段目标/可领量/寿命和原四字段结果。新缓存首次只观察Sequence，不重播现有结果；后续序号变化投影文案，None清消息。网络结果须合法枚举；PickedUp须已知ItemId及正Quantity，其他结果须空ItemId/0。非法结果只清本结果通道并记录stage=ReadSnapshot、map/sequence/result/itemId/quantity/异常，原有效目标及寿命沿原链；原目标快照错误仍收起G面板并记录原异常。

当前优先级为原NoSpace目标 > 拾取结果 > 原Ready目标/寿命。NoSpace保留原物品、实际数量与寿命；有效新结果也是NoSpace时，该目标第一行在窗口内使用失败色，其他旧结果不覆盖它。Ready或Hidden有有效结果时，复用原G面板居中单行显示结果，临时隐藏目标/寿命文字；成功例为Picked up  Wood ×3。到期重新显示当前目标及寿命，Hidden无结果时收起。G高亮始终按七字段目标快照的DropId解析实际目标，结果没有高亮目标或TTL。

默认2秒绿/红，显示期限使用Time.unscaledTimeAsDouble，服务端拾取/到期仍沿原模拟时间。每次Show/Clear恢复白色；Draw沿Repaint、原GUI矩阵/颜色finally恢复，并在finally恢复标签白色，避免新颜色残留。原400×84/底距168/字号20、缩放、字体/纹理与单行/双行几何保持；寿命关闭时允许原52高单行配置，不因结果额外增加行数。

pickupFeedbackHud.enabled=false仅隐藏新结果；pickupHud.enabled=false隐藏整个G文字，实际G与高亮仍沿原开关。新enabled不强制创建或维持HUD，全部原显示关闭时沿原绑定退出。F及B继续原反馈/优先级，资源状态和世界保存显示保持独立。Clear逐帧只收起可见状态，不清结果观察序号/期限；Reset/Configure、死亡、断线、无本地玩家/地图、源/玩家变化、World/Scene停止与释放清新增配置、标签、序号、观察标记、结果、期限、文字、颜色及显示选择，不保留上一局消息。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译0 Error，新九字段DTO/Settings与四字段所属反馈已加载；Console由编译前[0 Error,5 Warning,53 Log]变为[0,7,53]，不作为Console全部清空。18组合法读取通过，256份非法配置全部拒绝：新段形状、九字段缺失/null/错类型/重复、未知键、坏时间/颜色/五文案、关闭仍校验、旧v1～v24及重复根/额外内容。合法覆盖Json/BuiltIn一致、新显示/F/全部原显示/全部显示/原F失败/原F结果关闭、寿命关闭52高与自定义3.5秒/颜色及61字节边界。

两地图各22次，共44次隔离Editor Bake通过：保留原18类配置与关闭/自定义组合，追加新显示关闭、新自定义3.5秒/两颜色/五中文文案及原物品/容量文案、寿命关闭52高和全部显示关闭。九Settings与Player初值0/None/空/0正确，原配置、反馈、布局和资源签名保持；Forest89树/36采集点/20矿点/109阻挡，Grassland53/38/18/71。

实际生成Serializer为Sequence(uint)、Result(uint快照)、ItemId(FixedString64Bytes)、Quantity(int)，SendToOwner、4 mask bits、SnapshotSize=76。Editor World不初始化游戏网络函数指针，s_StateInitialized=false不作为实际收发验收。拾取反馈v25阶段核对输入19、Tools3、F4/G6/资源状态4/世界保存3/F失败2/F结果3，玩家v4根7/Tools项3与世界v2根9/掉落项8保持。

Bake前后Console均[0,7,53]，未新增烘焙错误/警告/日志；主场景干净、未Play，结束时一场景三根对象。临时TextAsset/克隆对象/Editor场景/World/BlobAssetStore释放，原SubScene只读使用后恢复。原339项人工内容/编号逐字保留，本阶段追加16项后共355项，归[运行入口](Runtime.md)。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v25/revision28及[运行入口](Runtime.md)十六项清单，结论来自用户反馈。清单涵盖实际拾取物品/增量及拒绝/失败反馈、写入隔离、G目标/寿命与NoSpace优先级、F/B独立显示、配置开关、所属同步和生命周期回归；原355项内容/编号逐字保留。未实际触发的独立成功/拒绝/准备与保存失败、反馈写入隔离、容量/优先级、显示/字形/排版、多人争抢/晚加入/重连、序号覆盖/回绕/预测回放及生命周期用例仍UNKNOWN。旧通过保持原版本/清单，本结论不扩展到性能、平台或线上联调。

AI未执行GamePlayer/PlayMode、游戏/显示系统或GUI回调、逻辑单元测试、真实存档业务I/O、命令行构建/发布、性能/带宽采样或图片检查，未创建子Agent/提交Git。保存后意外ECS恢复、玩家/世界跨文件事务、同槽并发及性能/平台/线上结论仍沿原UNKNOWN边界。

## 【FACT】地面合并与实际拾取结果

v26/revision29接入的[合并](MapDropMerge.md)仅改变原地面Quantity/ExpiresAt及来源Consumed，不写本组件或发布PickedUp；实际G仍在原保存/库存及地面余量或Consumed完整提交后才发成功，携带合并后的本次真实增量。本次领取量按[部分拾取](MapDropPartialPickup.md)开关计算，零余量仍NoSpace，移动/攻击/无目标/失败及面板优先级、开关/绑定保持。新合并静态及用户人工通过限v26/revision29十六项，未触发独立用例UNKNOWN；本专题原拾取反馈通过仍限v25/revision28十六项。
