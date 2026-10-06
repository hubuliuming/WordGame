# 掉落物拾取提示与目标显示

返回[地图](Map.md)、[掉落](MapDrops.md)和[F交互显示](MapInteractionHud.md)。本专题负责 CombatPrototypeNetCode 的 G 目标文字提示、服务端显示快照及本地绘制；实际拾取、保存入包、生成/运动/到期/释放仍归原掉落链，人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [MapPickupHudConfig](../../Assets/Scripts/CombatPrototype/Map/MapPickupHudConfig.cs) | pickupHud 的18个必填 JSON 字段 |
| [PickupHudData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudData.cs) | 原地图根18字段Settings与玩家所属七字段快照 |
| [DropTargetSelector](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropTargetSelector.cs) | 原 G 与新提示共用的只读最近掉落选择 |
| [PickupHudStateSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudStateSystem.cs) | 服务端在 PlayerRespawn 后采样资格/目标，只提交显示状态 |
| [PickupHudClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudClient.cs) | 原G行及双行布局；读取错误在显示边界记录并收起 |
| [寿命快照](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupLifetimeHudSnapshot.cs) | 服务端读取目标实际ExpiresAt，投影寿命模式/向上取整秒数 |
| [寿命绘制](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupLifetimeHudClient.cs) | 校验所属寿命字段，缓存文案与预警颜色，沿原Repaint委托绘制 |
| [原G入口](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs) | GetPickupHintRejection 共用原资格，Select 共用目标；入包检查归[容量](MapInventoryCapacity.md) |
| [原HUD宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 原 Main Camera 组件委托独立 G 面板，沿原本地玩家/连接与生命周期绑定 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 地图 Settings 与玩家初始 Hidden 数据 |

v11/14阶段五个新脚本及meta、v17/20寿命接入两个普通助手及meta均由正常Unity导入。新客户端类不是 MonoBehaviour，未新增组件挂载或 Scene/SubScene/Prefab/Animator 结构，也未修改旧 meta、资源绑定、网格/材质/字体/图片、包或构建配置。Baker 追加玩家 ECS 显示数据，烘焙后的 Ghost 布局变化；各端须同版代码、配置并重新烘焙。当前输入19字段，SaveWorld归[F5](MapWorldSaveHud.md)，修理归[专题](MapToolRepair.md)，G没有新增目标命令、RPC、版本协商或玩家存档字段。

## 【FACT】当前 JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为 schemaVersion=36/configRevision=39，pickupHud段及全部18字段必填，[interactionHighlight](MapInteractionHighlight.md)亦为必填地图段。沿原严格 UTF-8/缺失/未知/重复字段/类型和语义校验；旧 v1～v35 明确失败，不迁移、补默认段或回退来源。正常导入/烘焙后生效，没有运行热重载。

| 字段 | 当前默认值 | 契约 |
|---|---|---|
| enabled | true | 仅控制 G 文字；G 文字与 G 高亮均关闭时快照 Hidden，原 G 结算继续 |
| panelWidthPixels | 400 | 有限，32 < 宽度 <= 1920 |
| panelHeightPixels | 84 | 有限正数；lifetimeEnabled时至少2*fontSize+40，否则至少fontSize+32 |
| bottomMarginPixels | 168 | 有限非负；高度+底距<=1080，且至少为 F 面板底距+高度+16 |
| fontSize | 20 | 正整数 |
| pickupLabel | Pick up | 非空白、无控制字符、最多61个UTF-8字节 |
| appleLabel | Vitality Apple | 同上，对应 vitality_apple |
| woodLabel | Wood | 同上，对应 wood |
| stoneLabel | Stone | 同上，对应 stone |
| noSpaceLabel | Not enough space | 同上；[容量](MapInventoryCapacity.md)拒绝时使用 |
| lifetimeEnabled | true | 必填布尔，控制G目标的寿命第二行 |
| expiryWarningEnabled | true | 必填布尔，仅寿命开启且真实余时不超过阈值时预警 |
| expiryWarningSeconds | 30 | 有限正数，关闭仍校验，不受新掉落600秒默认值限制 |
| expiresInLabel / permanentLabel / expiringSoonLabel / secondsLabel | Expires in / Permanent / Expiring soon / s | 四个必填文案，沿原非空白/无控制字符/最多61个UTF-8字节规则 |
| expiryWarningColorHex | #FFB454 | 必填#RRGGBB，六位大小写十六进制；Baker写RGB float3，文字alpha为1 |

关闭显示仍校验全部字段、几何关系与文案。F 面板尺寸改变时，G 底距须满足上述间隔；没有自动搬移或默认位置兜底。拾取距离唯一读取原 drops.pickupDistanceMeters，默认2米；没有另设提示半径。标签独立于 inventoryDrop.items，仍支持原三种掉落物品，不因敌人掉落或某种产出功能关闭而关闭已有物体的提示。

## 【CURRENT STRATEGY】共用资格与目标选择

原 G 的在线请求、资格和目标逻辑只抽出只读函数，没有改变保存/提交、请求排序或系统次序。GetPickupHintRejection 沿原顺序检查所属 NetworkId、存活、无 Attack 请求且近战 Ready、有限零 Move，原拒绝原因保持。连接须 Connected、InGame、未请求断线，CommandTarget 指向当前启用 Simulate 玩家。新采样不要求本 tick 按 G，错误归属连接不覆盖真实所属玩家快照；当前 G 不检查 F 预约，新提示也不新增此限制。

DropTargetSelector 沿原算法遍历 Landed 且未到期掉落，按本人 X/Z 中心距离筛选，距离<=原 PickupDistance，取最近一个；精确同距选较小 DropId。Prepared、Airborne、Consumed、到期和超范围物体排除，lifetime=0 的原永不到期规则保持。按 G 仍由实际处理 tick 重新选择，不上传客户端目标，也不锁定/预约正在显示的物体；选中目标后按[部分拾取](MapDropPartialPickup.md)共用容量接口计算可领取量；零余量或开关关闭且整堆放不下时拒绝，不改选较远目标。多人请求仍按NetworkId升序，保存成功才提交库存/原堆余量，领空才Consumed。

PickupHudStateSystem 在服务端 PredictedSimulation、PlayerRespawn 后执行，读取本 tick 原掉落运动/拾取/到期处理后的状态及服务端模拟时间。每 tick 重建所属玩家显示帧，只有字段变化才写入；无目标、资格拒绝、G文字与G高亮均关闭、连接失效、未采样和系统停止时清为Hidden；采样条件为pickupHud.enabled或interactionHighlight.enabled且gTargetsEnabled。单个玩家采样异常记录地图、NetworkId、玩家、DropId、阶段和原异常，继续其他玩家，未形成有效帧者清空。必需组件/配置缺失明确暴露，不创建默认数据或替代服务。

提示只写 PickupHudState；不修改 DropPhase/Quantity/进度、库存、Tools、奖励、资源预约、输入或存档，不控制运动、到期和释放。保存失败后的未到期目标仍可显示；目标提示不是保存成功标志；实际G结果由[拾取反馈](MapPickupFeedbackHud.md)独立投影到原面板，拾取动画和自动拾取未接入。

## 【FACT】所属 Ghost 快照

CombatPrototypeMapPickupHudState使用OwnerSendType=SendToOwner，七个GhostField仅发送给所属玩家；当前Serializer/Snapshot已包含PickupQuantity：

| 字段 | 当前含义 |
|---|---|
| Mode | byte 枚举：0 Hidden、1 Ready、2 NoSpace |
| DropId | int，Ready/NoSpace 为原正数 ID；Hidden 为0 |
| ItemId | FixedString64Bytes，原 vitality_apple/wood/stone；Hidden 为空 |
| Quantity | int，Ready/NoSpace为目标地面实际正数量；Hidden为0 |
| PickupQuantity | int，本次容量可接收量；Ready为1～Quantity，NoSpace/Hidden为0；G实际处理时重新计算 |
| LifetimeMode | byte枚举：0 None、1 Timed、2 ExpiringSoon、3 Permanent；Hidden或G文字/寿命关闭时None |
| RemainingSeconds | float、Quantization=0；Timed/ExpiringSoon为有限正整数秒，Permanent/None/Hidden为0 |

不发送世界位置或绝对ExpiresAt；仅投影目标余时，不新增工作计时器。原F及资源状态各四字段、Drop Ghost四字段保持；当前19输入的SaveWorld归[F5](MapWorldSaveHud.md)；当前G布局由六变七，各端须同版重新烘焙。新状态不是输入/结算依据；网络延迟可能使提示滞后，显示后目标也可能被其他玩家取走或到期。当前位置下的 G 实际目标始终由服务端决定。

## 【CURRENT STRATEGY】显示、开关与释放

沿原绑定枚举启用 GhostOwnerIsLocal 并核对所属 Connected/InGame 连接，只取本地存活玩家快照；不在含可启用组件的查询上调用单例 API。PickupHudClient只读取烘焙Settings和显示快照，不读PlayerView/世界坐标，不修改玩家或服务端数据。原绑定另委托[高亮解析](MapInteractionHighlight.md)读取对应客户端掉落位置；Main Camera原宿主先画G/F圆环，再走原B、G、F面板路径，原输入和制作/丢弃反馈保持。

NoSpace第一行将Pick up替换为noSpaceLabel，保留原物品/实际数量与寿命第二行；Ready全量显示“G  Pick up  物品文案 ×地面数量”，部分可领取时显示“×本次可领量/地面量”；寿命开启时第二行显示“Expires in 120s”；真实余时≤30秒且预警开启时显示橙色“Expiring soon 30s”；ExpiresAt=0显示白色“Permanent”。服务端按当前目标的实际ExpiresAt减模拟时间、向上取整投影秒数，预警使用未取整余时；不从drops.lifetimeSeconds重新计算、不在客户端用墙钟递减。到期仍由原G选择/清理链处理，合法未到期物不提前显示0秒；保存恢复后显示原恢复期限，离线暂停规则保持。状态变化才写七字段，平稳同秒不因时间推进重复改快照；网络延迟可使文字滞后，实际到期/拾取以服务端为准。

默认G为400×84、底距168、字号20；资源状态行400×52的底距由236改268，保持与G间隔16像素，F仍320×104/底48。两行各字号+4高、间隔8，整体垂直居中；寿命关闭沿单行G路径，默认配置高度仍84，可显式配置52并同步状态底距236。Hidden收起原目标与寿命；有效[拾取反馈](MapPickupFeedbackHud.md)仍按其期限显示。沿1920×1080参考像素等比缩放、黑底alpha0.7、普通白字、内置GUI字体和Texture2D.whiteTexture、richText=false；只在父G面板的Repaint委托绘制，缓存稳定文案/样式并恢复GUI.matrix/color，不消费键鼠事件。

pickupHud.enabled、interactionHud.enabled、inventoryPanel.enabled与interactionHighlight的主开关/F/G通道独立。关闭G文字仍可显示G高亮和实际拾取；关闭F HUD和背包面板仍可只显示G。三文字全关、无高亮通道且资源状态/存档HUD均关闭时原绑定收起，键盘原玩法继续。死亡/断线、无本地玩家或地图、玩家/地图源变化、World/Scene停止及释放沿原Clear/Reset清掉可见状态和缓存，不保留上一局目标。非法目标快照明确报错并保持G隐藏，不以默认标签伪装有效目标；非法[拾取结果](MapPickupFeedbackHud.md)只清结果通道，原有效目标/寿命保持。Clear逐帧隐藏，Reset清目标与新增结果观察/期限。

## 【KNOWN ISSUES】静态证据与人工边界

v11/14拾取文字阶段正常Unity编译无C# Error，五脚本/meta、新Serializer/Snapshot 四字段与 SendToOwner、原13输入字段及系统入口已静态核对。Forest/Grassland 各覆盖 Json 默认、BuiltIn 默认、Json 关闭 G 提示、Json 关闭 F HUD和背包但保留G、Json 三种显示全关闭，共十次隔离 Editor 烘焙。schema11/revision14、全部新 Settings、玩家 Hidden/DropId0/空ItemId/Quantity0、原 F 初值/丢弃反馈、原掉落 Prefab/Prepared 和原布局一致。森林/草原树木89/53、采集点36/38、矿点20/18、阻挡109/71保持，布置位置/朝向逐项一致。

编译前 Console [0 Error,5 Warning,48 Log]；编译新增两条来自未修改 PEListener/DOTweenPreviewManager 的既有代码警告，没有新增 C# Error。隔离烘焙前后均[0 Error,7 Warning,48 Log]，无新增烘焙错误/警告；原五条运行 Tick Batching 警告保留，没有清空Console。原主场景干净，临时 World/Scene 与临时 TextAsset 已释放。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限CombatPrototypeNetCode、schemaVersion=11/configRevision=14及[运行入口](Runtime.md)十项清单。人工结论来自用户反馈；未实际触发的独立排版/字形/缩放、临界距离/同距、同tick、延迟/预测回放、多玩家/晚加入、断线/重连及配置/快照/保存失败仍为UNKNOWN。原丢弃v10/13、面板v9/12、工具v8/11、F HUD v7/10等用户通过保持各自版本/清单，不覆盖本次新显示。未验证的字体覆盖、运行性能/带宽、平台/线上及原保存成功后意外ECS恢复仍UNKNOWN。AI未运行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

v12/15的[高亮](MapInteractionHighlight.md)按原DropId解析同一客户端Landed Ghost，不另选最近目标或改变按G时的服务端选择。已核对新增14配置值、原G/F初值与13输入；正常编译及14次隔离Editor烘焙静态通过。用户确认高亮人工通过限v12/15十项，未实际触发的独立排版/网络/生命周期与失败用例仍UNKNOWN；上述v11/14文字提示用户通过保持原范围。

v13/16的[资源状态](MapResourceStatusHud.md)在G面板上方增加一行；G的开关、采样资格、四字段、DropId与拾取规则保持。资源状态不显示掉落期限，也不改变G蓝圈；仅状态开启仍保留整体绑定。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧G/高亮通过保持原版本/清单。

当前v23/26的[工具修理](MapToolRepair.md)新增两个输入和所属结果，遇同tick G请求时修理拒绝；G目标/资格/入包/保存/显示均保持原链，新增修理静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN。

## 【FACT】地图资源存档接入边界

世界v2档开启掉落开关时恢复地面物及编号上限，详见[掉落存档](MapDropPersistence.md)；原G目标/TTL和入包事务保持；所属显示由四变六，寿命仅属上述显示投影。资源存档人工通过限v15/18十二项，未触发用例UNKNOWN，见[资源存档](MapResourcePersistence.md)。

## 【KNOWN ISSUES】寿命提示与到期预警的验收边界

v17/revision20寿命链正常Unity编译无C# Error；实际Assembly与生成Serializer/Snapshot六字段、SendToOwner、RemainingSeconds无量化、17配置/Settings及两助手已核对。原15输入、F/资源状态各4、Drop Ghost4、世界根9/掉落条目8保持。Forest/Grassland各13种隔离Editor烘焙共26次：Json/BuiltIn、寿命关闭、预警关闭、G文字关闭、文字/G圈均关、自定义5.5秒/四文案/秒单位/RGB与尺寸、资源状态关闭、寿命关闭且恢复52/236布局、仅G文字、永久Lifetime=0、世界存档关闭、地面存档关闭。两来源等价，17Settings及Hidden/None/0初值、三掉落Prefab/Prepared、原工具/反馈/F/高亮/资源状态、完整原布置与资源签名均通过；森林/草地树89/53、采集36/38、矿20/18、阻挡109/71保持。编译后及隔离烘焙前后Console均[0 Error,0 Warning,0 Log]，主场景干净、临时World/Scene/TextAsset释放；AI未主动清Console。

主线程代码/配置静态验收通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、schemaVersion=17/configRevision=20及[运行入口](Runtime.md)十二项清单。人工结论来自用户反馈。原G文字v11/14十项、高亮v12/15十项、资源状态v13/16十项、工具修理v14/17十二项、资源存档v15/18十二项、掉落存档v16/19十二项及更早用户通过保持旧版本/清单，不覆盖新增寿命显示。未实际触发的独立倒计时/30秒或小数阈值边界/永久物/恢复余时、排版/颜色/字形、模式与目标切换、同tick/延迟/多玩家/晚加入/断线及配置/绑定/快照/绘制/保存/清理失败仍UNKNOWN；带宽/性能/平台/线上、玩家与世界跨文件原子一致/防重复及意外ECS恢复不属于通过范围。未修改Scene/SubScene/Prefab/Animator/旧meta/资源引用、原G/选择/运动/到期/保存链、输入或HUD宿主/绑定脚本；两助手不挂组件。AI未运行游戏/显示系统/GUI回调、GamePlayer/PlayMode、逻辑单元测试、构建、发布、性能采样或图片检查，未读取真实存档、创建子Agent或提交Git。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：该阶段17输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。

## 【FACT】容量等级接入边界

[升级](MapInventoryCapacityUpgrade.md)：G采样按实际Level选择容量，NoSpace仍保留原目标与寿命；原六所属字段和目标/到期规则保持。新链静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧通过保持原版本/清单。

## 【FACT】实际G结果的独立接入

v25/revision28阶段接入必填[拾取反馈](MapPickupFeedbackHud.md)九字段Settings，Player Baker另加Sequence/Result/ItemId/Quantity四字段SendToOwner；原六字段目标/寿命与服务端StateSystem、选择器保持。原G保存/库存/Consumed完整提交后才显示成功，实际拒绝或保存前失败显示原因；写入独立隔离，保存后部分提交异常沿原日志/UNKNOWN。原NoSpace目标优先并保留数量/寿命，有效新NoSpace窗口内该行标红；其他结果复用原面板单行，暂时隐藏目标/寿命，到期恢复当前快照，原G高亮继续使用实际DropId。关闭新显示/G文字及原全部显示退出、首次不重播/Reset沿原绑定，新开关不强制HUD。静态及用户人工通过限v25/revision28十六项，未触发独立用例UNKNOWN；旧G文字/寿命通过保持原版本/清单。

## 【FACT】合并后的真实G目标

v26/revision29的[合并](MapDropMerge.md)在G前更新原掉落数量/期限并Consumed来源，服务端七字段采样读取当前真实地面Quantity、容量可领PickupQuantity及最早余时；来源不再可选，编号/位置保留的目标沿原高亮解析。G按[部分拾取](MapDropPartialPickup.md)开关结算，零余量NoSpace仍保留地面数量/寿命；新增所属PickupQuantity，目标仍由服务端重选。合并链静态及用户人工通过限v26/revision29十六项，部分拾取用户人工通过限v27/revision30十六项；未触发独立用例UNKNOWN，旧G提示/寿命及v25拾取反馈通过限原版本/清单。
