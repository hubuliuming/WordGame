# 资源交互失败原因提示

返回[地图](Map.md)、[统一F显示](MapInteractionHud.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode，当前Forest/Grassland Json/BuiltIn为schemaVersion=25/configRevision=28。本专题负责单次F请求未能开始资源交互时的所属反馈；选目标、[完成/中断](MapGatherOutcomeHud.md)、工具使用、G拾取与存储归各自专题。静态及用户人工通过，限v23/revision26十六项，未触发独立用例UNKNOWN；耐久预警用户通过仍限v22/revision25十六项，旧通过保持原版本和清单。

## 【FACT】入口与文件

| 职责 | 文件 |
|---|---|
| 新九字段DTO | [MapInteractionFailureHudConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapInteractionFailureHudConfig.cs) |
| 新九字段固定Settings | [CombatPrototypeMapInteractionFailureHudData.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionFailureHudData.cs) |
| 新结果枚举及二字段所属反馈 | [CombatPrototypeMapInteractionFailureFeedback.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionFailureFeedback.cs) |
| 新普通客户端缓存 | [CombatPrototypeMapInteractionFailureHudClient.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionFailureHudClient.cs) |
| 原地图DTO/验证/BuiltIn | [MapDefinitionConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapDefinitionConfig.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) |
| 原地图根烘焙 | [MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 原Player Baker初值 | [PlayerNetCodeAuthoring](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) |
| 原服务端统一F请求 | [MapInteractionSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs) |
| 原客户端绑定/宿主 | [BindingSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs)、[InteractionHud](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) |
| 显式地图配置 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

四个新脚本为普通代码，meta由Unity正常导入生成；没有新增MonoBehaviour或Scene/SubScene/Prefab/Animator结构、资源绑定、旧meta、包或构建改动。原Camera/HUD注册及本地GhostOwnerIsLocal绑定保持。

## 【FACT】JSON与固定配置

必填interactionFailureHud恰九字段，两地图与BuiltIn默认一致：

| 字段 | 默认值 | 约束 |
|---|---|---|
| enabled | true | bool；关闭仍完整校验 |
| feedbackSeconds | 2 | 有限正float，仅客户端提示寿命 |
| errorColorHex | #FF6B6B | 严格#RRGGBB |
| alreadyInteractingLabel | Already interacting | 非空白、无控制字符、最多61 UTF-8字节 |
| movingLabel | Stop moving first | 同上 |
| attackingLabel | Finish attack first | 同上 |
| noTargetLabel | No available resource | 同上 |
| targetUnavailableLabel | Target unavailable | 同上 |
| failedLabel | Interaction failed | 同上 |

NoSpace直接复用interactionHud.noSpaceLabel，没有第二份容量文案。原严格JsonReader通过DTO字段完整检查对象形状、缺失/null/未知/重复键/标量类型及UTF-8；语义校验复用Positive、HighlightColor、HudLabel。旧地图v1～v24明确拒绝，不迁移、不补默认段、不回退来源；正常导入/烘焙后生效，无运行热重载。英文为默认文案；可配置中文，实际字形与排版仍UNKNOWN。

原Map Baker在唯一地图根写九字段Settings：Enabled(byte)、FeedbackSeconds(float)、ErrorColor(float3 RGB)及六个FixedString64Bytes。它不是Ghost；绑定在地图源/玩家变化时传给原HUD，普通缓存保存固定配置、文字、观察序号、期限和颜色，不跨帧持有DynamicBuffer。

## 【CURRENT STRATEGY】真实原因与所属反馈

新CombatPrototypeMapInteractionFailureFeedback含Sequence(uint)与Result(byte枚举)两个GhostField，SendToOwner。原Player Baker初始化0/None；各端须同版重新烘焙。枚举顺序为None=0、AlreadyInteracting=1、AttackInProgress=2、PlayerMoving=3、NoAvailableTarget=4、NoSpace=5、TargetUnavailable=6、Failed=7。

| 现有服务端判定 | 结果/提示 |
|---|---|
| 本tick开始前已处于Collecting/Chopping/Mining | AlreadyInteracting；不重置或切换目标 |
| 当前Attack输入或近战未Ready | AttackInProgress |
| 有限非零Move输入 | PlayerMoving |
| 原选择器没有可用范围内目标 | NoAvailableTarget |
| InventoryAlreadyOverCapacity / MaterialTotalCapacityExceeded / MaterialItemCapacityExceeded | NoSpace |
| 原TryBegin返回false | TargetUnavailable |
| 单项启动异常，或InvalidMoveInput等其他拒绝原因 | Failed；原异常日志保留 |
| 原预约成功 | None，清旧失败；已有None不重复写快照 |

每次失败更新Result并递增Sequence，成功清除已有失败时也递增；uint沿unchecked回绕。它是最新快照，没有新输入、RPC、事件队列或历史记录，连续结果可能被较新快照覆盖。本通道只覆盖请求启动阶段，已接入的采集完成/中断通道归[完成/中断](MapGatherOutcomeHud.md)。

CommandTargetOwnerMismatch与PlayerDead沿原日志/隐藏路径，不投影为新的失败提示。写入边界再次核实真实GhostOwner与请求NetworkId、存活状态：即使AlreadyInteracting早于原资格判断，也不会向另一玩家写反馈。缺少必需组件明确记录错误，不动态添加组件或默认反馈。

WriteFeedback在独立try/catch内记录stage=WriteFeedback、map、NetworkId、player、result及原异常。反馈写入异常不进入原预约失败清理，不撤销已成功工作；单项业务异常仍保留原CancelBegin及其独立清理日志，其余请求继续。ReadInput或共用服务/时钟前提失败仅沿原错误路径，不编造所属结果。原按NetworkId排序、最近目标/同距小PlacementIndex、资格、三类服务、忙状态采样时点与所有保存顺序保持。

没有工具或耐久不足仍按原Hands完成树木/矿点交互，[工具预警](MapToolDurabilityHud.md)只读状态；没有“缺工具导致交互失败”的新判定。NoAvailableTarget不区分距离不足、占用、耗尽或被关闭的类型；没有额外目标扫描或距离推断。

## 【CURRENT STRATEGY】原F面板显示与生命周期

默认红色提示显示2秒。Ready/Working有目标时只替换第二行，第一行F目标/工作百分比与进度颜色保持；Hidden时临时在第一行显示失败，第二行留空，反馈没有目标类型。NoSpace优先，未到期启动失败次之，再依次[完成/中断](MapGatherOutcomeHud.md)、修理、制作和工具状态；新结果NoSpace也先于普通启动失败。NoSpace反馈隐藏时仍使用原noSpaceLabel。

B面板继续接收原displayFeedback（修理优先制作），本功能不改B页脚、按钮、配方或输入。每次Show先恢复两行默认白色；失败到期后第二行重新使用原工具预警颜色。绘制仍仅Repaint，GUI.matrix/color沿原finally恢复，面板位置/尺寸、G、高亮、资源状态和世界保存提示保持。

首次观察只记当前Sequence，不重播既有结果；后续序号变化更新文字和unscaledTimeAsDouble期限，None清期限/文字。非法网络结果只清本通道并记录stage=ReadSnapshot、map、sequence、result与异常。每帧Clear只隐藏；原Configure/Reset及死亡、断线、源/玩家/World/Scene失效清序号、观察标记、期限、文字和颜色。

关闭interactionFailureHud或interactionHud隐藏新增F提示；原全部显示关闭时继续沿原绑定退出条件，新开关不强制创建/绑定HUD。新客户端时间只控制显示，不改变服务端采集完成或再生期限。输入19、原Tools所属三字段、F4/G6/资源状态4/世界保存3及玩家v4根7/Tools项3、世界v2根9/掉落项8保持；反馈不写玩家或世界存档。

## 【FACT】v23/revision26静态核对

正常Unity导入/编译完成，实际Console为0 Error。Config/Settings各九字段，Settings无Ghost，新助手不是MonoBehaviour；新反馈二字段，生成Serializer的实际State为SendToOwner、ChangeMaskBits=2、SnapshotSize=8且快照二字段。隔离Editor World不初始化游戏网络函数指针，符合当前NetCode模板；未执行收发或游戏系统。

两地图共10组合法配置读取通过，其中两组默认JSON/BuiltIn一致，另含新提示关闭、F关闭、全部原显示关闭和自定义时间/颜色/61字节及中文文案。共224份非法配置全部被原LoadValidated拒绝（每地图112），覆盖缺根/形状/缺字段/null/未知/重复键/类型、时长非正/溢出/非有限输入、颜色、六文案空白/控制字符/UTF-8长度、关闭仍校验及全部旧schema1～22。属于配置边界核对，没有逻辑单元测试。

两地图各16次，共32次隔离Editor Bake通过：JSON/BuiltIn、原升级/工具/B/F/G/全部显示关闭、原自定义升级定义/顺序、修理/背包升级/耐久预警关闭、自定义单次成本/预警值，以及新提示关闭和自定义3.5秒/#1234Ab/六中文文案。九Settings映射与初始0/None正确；原配置/反馈/布局/资源签名保持，Forest89树/36采集点/20矿点/109阻挡，Grassland53/38/18/71。

Bake前后实际Console均[0 Error,7 Warning,53 Log]，没有新增Bake错误/警告；正常编译重报两条既有PEListener UAC1001与DOTween CS0618警告。原主场景干净、未Play，临时TextAsset/克隆对象/Editor场景/World/BlobAssetStore释放，原SubScene只读使用后恢复。旧307项人工内容/编号逐字保留，新增16项后323项，见[运行入口](Runtime.md)。

## 【KNOWN ISSUES】人工边界

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v23/revision26及[运行入口](Runtime.md)十六项清单，结论来自用户反馈。清单涵盖移动/攻击/忙/无目标/容量、完成前启动拒绝及异常、反馈隔离、F优先与B原反馈、成功清除/到期/颜色、开关/绑定、所属联网/晚加入和生命周期及原玩法回归。原323项人工内容/编号逐字保留；未实际触发的独立TryBegin/异常/反馈写入失败、字形/排版、联网/序号覆盖/预测回放或生命周期用例仍UNKNOWN。旧通过保持原版本/清单，本结论不覆盖v24/revision27的[完成/中断](MapGatherOutcomeHud.md)新反馈。

AI未执行GamePlayer/PlayMode、游戏/显示系统或GUI回调、逻辑单元测试、真实存档业务I/O、命令行构建、发布、性能/带宽采样或图片检查，未创建子Agent或提交Git。跨文件事务、同槽并发、保存后意外ECS故障恢复及性能/平台/线上结论仍沿原UNKNOWN边界。
