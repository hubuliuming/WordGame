# 采集完成与中断反馈

返回[地图](Map.md)、[F显示](MapInteractionHud.md)、[启动失败](MapInteractionFailureHud.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode；Forest/Grassland Json与BuiltIn当前schemaVersion=24/configRevision=27。本专题负责植被采集、砍树和采矿的服务端结算/取消结果及现有F提示区显示。静态验收通过，人工GamePlayer待验收；旧启动失败人工通过仍限v23/revision26十六项，旧通过保持原版本/清单。

## 【FACT】文件与接入

| 职责 | 文件 |
|---|---|
| 十三字段DTO | [MapGatherOutcomeHudConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapGatherOutcomeHudConfig.cs) |
| 十三字段固定Settings | [GatherOutcomeHudData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherOutcomeHudData.cs) |
| 结果枚举及三字段所属反馈 | [GatherOutcomeFeedback](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherOutcomeFeedback.cs) |
| 独立服务端写入/原因映射 | [GatherOutcomeFeedbackUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherOutcomeFeedbackUtility.cs) |
| 普通客户端缓存 | [GatherOutcomeHudClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherOutcomeHudClient.cs) |
| 配置与根Baker | [MapDefinitionConfig](../../Assets/Scripts/CombatPrototype/Map/MapDefinitionConfig.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[MapAuthoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 所属反馈初值与F开始清除 | [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs)、[MapInteractionSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs) |
| 原完成/取消入口 | [GatherSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherSystem.cs)、[TreeHarvestSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapTreeHarvestSystem.cs)、[MineHarvestSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapMineHarvestSystem.cs) |
| 原客户端绑定/宿主 | [BindingSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs)、[InteractionHud](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) |
| 显式配置 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

五个新脚本为普通代码，meta由Unity正常导入生成；不新增MonoBehaviour、Scene/SubScene/Prefab/Animator结构或资源绑定。原相机注册、本地GhostOwnerIsLocal枚举、HUDStateSystem、选目标/忙状态采样及绘制结构保持。原Player Ghost追加所属反馈，各端须使用同版代码/配置并重新烘焙；原输入和已有组件字段保持。

## 【FACT】JSON与固定配置

必填gatherOutcomeHud恰十三字段，两个地图与BuiltIn一致：

| 字段 | 默认值 | 约束 |
|---|---|---|
| enabled | true | bool；关闭仍完整校验 |
| feedbackSeconds | 2 | 有限正float，仅客户端显示寿命 |
| completedColorHex | #6ED88A | 严格#RRGGBB |
| interruptedColorHex | #FFB454 | 同上 |
| failedColorHex | #FF6B6B | 同上 |
| gatherCompletedLabel | Gathering completed | 非空白、无控制字符、最多61 UTF-8字节 |
| treeCompletedLabel | Tree felled | 同上 |
| mineCompletedLabel | Mining completed | 同上 |
| movingLabel | Interrupted: moving | 同上 |
| attackingLabel | Interrupted: attacking | 同上 |
| hitLabel | Interrupted: hit | 同上 |
| outOfRangeLabel | Interrupted: out of range | 同上 |
| failedLabel | Resource work failed | 同上 |

NoSpace复用interactionHud.noSpaceLabel，没有第二份容量文案。原JsonReader沿DTO完整检查UTF-8、形状、缺失/null/未知/重复键及标量类型，语义复用Positive/HighlightColor/HudLabel；旧地图v1～v23明确拒绝，不迁移、不补默认段、不回退来源。正常导入和烘焙后生效，无热重载；英文为默认值，中文可配置，实际字形与排版UNKNOWN。

原Map Baker在唯一地图根写十三字段Settings：Enabled(byte)、FeedbackSeconds(float)、CompletedColor/InterruptedColor/FailedColor(float3 RGB)与八个FixedString64Bytes。Settings不是Ghost；原绑定在地图源/本地玩家变化时传入HUD。新普通缓存只持有配置、文字、Kind/Result、观察序号、期限和颜色，不跨帧持有DynamicBuffer。

## 【CURRENT STRATEGY】真实完成与取消

服务端沿原资源系统处理，不从客户端百分比或FinishAt独立推断完成：

| 原入口 | 发送时点 |
|---|---|
| 植被Complete | PrepareReward/SavePrepared成功，库存及Depleted/RegrowAt完整提交，原完成日志结束后返回true；调用者才发送Completed |
| Tree/Mine Complete | 原有效地面掉落已初始化登记，工具使用时原SavePrepared成功，耐久/进度/阻挡历史/障碍与Felled/Depleted完整提交后正常返回；调用者才发送Completed |
| 三类Cancel | 原进度清空、Available/Standing与占用复位及原取消日志完成后，发送对应中断/失败 |
| 成功的新F预约 | 原预约已完成后独立写None，清旧完成/中断；原启动失败通道也沿原规则清除 |

植被Complete现在返回明确成功bool并输出rewardSaved；该标记只用于禁止保存后的部分提交异常发布取消反馈，原容量拒绝、PrepareReward→SavePrepared→库存/耗尽提交与原Cancel处理保持。树木/矿点沿原durabilitySaved及保存前回滚/保存后暴露异常路径；完成函数主体只适配取消时传入原Collector实体，不重做结算。Collector身份在取消前保存，反馈不会读取已清空进度推断所属。

| 原取消原因 | 反馈 |
|---|---|
| PlayerMoving | PlayerMoving；橙色movingLabel |
| AttackInProgress | AttackInProgress；橙色attackingLabel |
| PlayerHit | PlayerHit；橙色hitLabel |
| OutOfRange | OutOfRange；橙色outOfRangeLabel |
| InventoryAlreadyOverCapacity / MaterialTotalCapacityExceeded / MaterialItemCapacityExceeded | NoSpace；复用原容量文案，红色 |
| InvalidMoveInput / ProcessingFailed / SettlementFailed / DropOrSaveOrCommitFailed | 实际Cancel成功后Failed，红色failedLabel |
| ReservationFailed / CollectorOffline / CommandTargetOwnerMismatch / PlayerDead | 不写新结果；启动回滚沿原F失败通道，其他沿原日志/隐藏/Reset |

保存后的部分ECS提交异常不发Completed，也不宣称取消/回滚成功，沿原日志与UNKNOWN恢复边界。反馈不会改变原产出或进行额外保存：植被成功直接入包，砍树/采矿成功产生地面掉落，拾取仍由G完成；未持有/损坏工具仍Hands。取消不扣耐久、不补发物品、不自动重试。原范围、最近/同距小PlacementIndex、时长、升级、再生与所有保存格式/顺序保持。

## 【FACT】三字段所属最新快照

CombatPrototypeMapGatherOutcomeFeedback恰Sequence(uint)、Kind(byte)、Result(byte枚举)三个GhostField，SendToOwner，原Player Baker初始化0/0/None。Kind沿原资源类型None=0/Gather=1/Tree=2/Mine=3；Result为None=0、Completed=1、PlayerMoving=2、AttackInProgress=3、PlayerHit=4、OutOfRange=5、NoSpace=6、Failed=7。None要求Kind=0，其他结果须有合法资源类型。

每次完成/取消结果递增Sequence；成功预约清除已有结果也递增，已有None不重复更新，uint沿unchecked回绕。只传最新快照，无新输入、RPC、事件队列或存档字段，较新结果可能覆盖先前结果。

共享写入辅助在独立try/catch内映射原原因、核实真实GhostOwner/NetworkId及存活状态并写入。缺少必需组件不动态补齐；错误记录stage=WriteFeedback、map/NetworkId/player/kind/result/reason与原异常。错误不传播进原结算、回滚或CancelBegin，不撤销成功业务；未知原因也在该边界显式报错。只向原Collector的合法所属玩家写反馈。

## 【CURRENT STRATEGY】原F提示区与生命周期

默认绿色完成、橙色中断、红色失败，各2秒。Ready/Working只替换第二行，第一行目标/百分比和进度条保持；Hidden第一行显示结果，第二行Gather为Hands、Tree为当前斧头、Mine为当前镐头状态，工具颜色沿原耐久预警。启动失败的Hidden第二行仍空。

F优先级为NoSpace > 原启动失败 > 完成/中断 > 修理 > 制作 > 工具。原NoSpace目标两行最高；新结果NoSpace也优先于普通启动失败。B继续接收原displayFeedback（修理优先制作），新结果不进入B页脚。G、高亮、资源状态、世界保存及原面板位置/尺寸保持；每帧恢复两行白色，到期重新使用原反馈/工具颜色，绘制仍仅Repaint且沿原finally恢复GUI.matrix/color。

首次观察只记录Sequence，不回放初始结果；后续变化投影文案/Kind/Result与unscaledTimeAsDouble期限，None清文字/类型/结果/颜色和期限。非法网络结果或类型只清本通道并记录stage=ReadSnapshot、map/sequence/kind/result/异常。Clear逐帧只隐藏，Configure/Reset及死亡、断线、源/玩家/World/Scene失效清观察标记、序号、文字、类型、结果、颜色和期限。

关闭gatherOutcomeHud或interactionHud隐藏新F显示；原全部显示关闭时仍沿原绑定退出条件，新开关不强制创建/绑定HUD。客户端计时只控制显示，不改变服务端工作/再生期限。输入19、Tools3、原F4/G6/资源状态4/世界保存3、启动失败2及玩家v4根7/Tools项3、世界v2根9/掉落项8保持。

## 【FACT】静态核对

正常Unity导入/编译完成，实际Console为0 Error。DTO与Settings各十三字段，Settings无Ghost，新缓存非MonoBehaviour；所属反馈三字段，隔离Bake后实际生成Serializer State为SendToOwner、ChangeMaskBits=3、SnapshotSize=12，Snapshot字段Sequence/Kind/Result各uint。s_StateInitialized=false符合当前NetCode模板对Editor World不初始化游戏网络函数指针的规则，不代表收发已验证。

两地图共14组合法配置读取通过：Json/BuiltIn默认一致、新显示关闭、F关闭、全部原显示关闭、新旧全部显示关闭、原启动失败关闭与自定义3.5秒/三颜色/61字节及中文文案。336份非法配置全部由原LoadValidated拒绝（每地图168），覆盖根形状/缺段、十三字段缺失/null/错类型/重复键、未知键、时长非正/溢出/下溢/非有限输入、三颜色、八文案空白/控制字符/UTF-8长度、关闭仍验证及旧schema1～23；没有逻辑单元测试。

两地图各18次，共36次隔离Editor Bake通过，覆盖Json/BuiltIn、原升级/工具/B/F/G/全部原显示关闭、自定义原升级定义/顺序、修理/背包升级/耐久预警/启动失败关闭、原单次成本/预警及文案自定义、新显示关闭和新自定义3.5秒/三颜色/八中文文案。十三Settings与Player初值0/0/None正确，原配置/反馈/布局/资源签名保持：Forest89树/36采集点/20矿点/109阻挡，Grassland53/38/18/71。

Bake前后实际Console均[0 Error,6 Warning,53 Log]，没有新增Bake错误/警告/日志。主场景干净、未Play，结束时仍一场景三根对象；临时TextAsset/克隆对象/Editor场景/World/BlobAssetStore释放，原SubScene只读使用后恢复。原323项人工内容/编号逐字保留，本阶段追加16项后共339项，见[运行入口](Runtime.md)。

## 【KNOWN ISSUES】人工边界

主线程静态验收通过；本阶段v24/revision27十六项人工GamePlayer待验收，尚未获得用户通过反馈。未实际触发的完成/取消/异常与反馈写入隔离、容量/优先级、显示/字形/排版、身份/多玩家/晚加入/重连、序号覆盖/预测回放及生命周期用例均UNKNOWN。旧人工通过只覆盖原版本/清单，不能扩大为本阶段通过。

AI未执行GamePlayer/PlayMode、游戏或显示系统、GUI回调、逻辑单元测试、真实存档业务I/O、命令行构建/发布、性能/带宽采样或图片检查，未创建子Agent/提交Git。保存后意外ECS故障恢复、跨文件事务、同槽并发及性能/平台/线上结论仍沿原UNKNOWN边界。
