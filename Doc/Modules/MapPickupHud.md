# 掉落物拾取提示与目标显示

返回[地图](Map.md)、[掉落](MapDrops.md)和[F交互显示](MapInteractionHud.md)。本专题负责 CombatPrototypeNetCode 的 G 目标文字提示、服务端显示快照及本地绘制；实际拾取、保存入包、生成/运动/到期/释放仍归原掉落链，人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [MapPickupHudConfig](../../Assets/Scripts/CombatPrototype/Map/MapPickupHudConfig.cs) | pickupHud 的九个必填 JSON 字段 |
| [PickupHudData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudData.cs) | 原地图根显示 Settings 与玩家所属四字段快照 |
| [DropTargetSelector](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropTargetSelector.cs) | 原 G 与新提示共用的只读最近掉落选择 |
| [PickupHudStateSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudStateSystem.cs) | 服务端在 PlayerRespawn 后采样资格/目标，只提交显示状态 |
| [PickupHudClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudClient.cs) | 普通 C# 帮助类，缓存文案/样式并在 Repaint 绘制 |
| [原G入口](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs) | GetPickupHintRejection 共用原资格，Select 共用目标；结算块保持 |
| [原HUD宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 原 Main Camera 组件委托独立 G 面板，沿原本地玩家/连接与生命周期绑定 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 地图 Settings 与玩家初始 Hidden 数据 |

五个新脚本及 meta 已由正常 Unity 导入。新客户端类不是 MonoBehaviour，未新增组件挂载或 Scene/SubScene/Prefab/Animator 结构，也未修改旧 meta、资源绑定、网格/材质/字体/图片、包或构建配置。Baker 追加玩家 ECS 显示数据，烘焙后的 Ghost 布局变化；各端须同版代码、配置并重新烘焙。当前输入15字段的新增修理归[专题](MapToolRepair.md)，G没有新增目标命令、RPC、版本协商或玩家存档字段。

## 【FACT】当前 JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为 schemaVersion=15/configRevision=18，pickupHud段及全部九字段必填，[interactionHighlight](MapInteractionHighlight.md)亦为必填地图段。沿原严格 UTF-8/缺失/未知/重复字段/类型和语义校验；旧 v1～v13 明确失败，不迁移、补默认段或回退来源。正常导入/烘焙后生效，没有运行热重载。

| 字段 | 当前默认值 | 契约 |
|---|---|---|
| enabled | true | 仅控制 G 文字；G 文字与 G 高亮均关闭时快照 Hidden，原 G 结算继续 |
| panelWidthPixels | 400 | 有限，32 < 宽度 <= 1920 |
| panelHeightPixels | 52 | 有限正数，至少 fontSize+32 |
| bottomMarginPixels | 168 | 有限非负；高度+底距<=1080，且至少为 F 面板底距+高度+16 |
| fontSize | 20 | 正整数 |
| pickupLabel | Pick up | 非空白、无控制字符、最多61个UTF-8字节 |
| appleLabel | Vitality Apple | 同上，对应 vitality_apple |
| woodLabel | Wood | 同上，对应 wood |
| stoneLabel | Stone | 同上，对应 stone |

关闭显示仍校验全部字段、几何关系与文案。F 面板尺寸改变时，G 底距须满足上述间隔；没有自动搬移或默认位置兜底。拾取距离唯一读取原 drops.pickupDistanceMeters，默认2米；没有另设提示半径。标签独立于 inventoryDrop.items，仍支持原三种掉落物品，不因敌人掉落或某种产出功能关闭而关闭已有物体的提示。

## 【CURRENT STRATEGY】共用资格与目标选择

原 G 的在线请求、资格和目标逻辑只抽出只读函数，没有改变保存/提交、请求排序或系统次序。GetPickupHintRejection 沿原顺序检查所属 NetworkId、存活、无 Attack 请求且近战 Ready、有限零 Move，原拒绝原因保持。连接须 Connected、InGame、未请求断线，CommandTarget 指向当前启用 Simulate 玩家。新采样不要求本 tick 按 G，错误归属连接不覆盖真实所属玩家快照；当前 G 不检查 F 预约，新提示也不新增此限制。

DropTargetSelector 沿原算法遍历 Landed 且未到期掉落，按本人 X/Z 中心距离筛选，距离<=原 PickupDistance，取最近一个；精确同距选较小 DropId。Prepared、Airborne、Consumed、到期和超范围物体排除，lifetime=0 的原永不到期规则保持。按 G 仍由实际处理 tick 重新选择，不上传客户端目标，也不锁定/预约正在显示的物体；多人请求仍按 NetworkId 升序，保存成功才库存提交/Consumed。

PickupHudStateSystem 在服务端 PredictedSimulation、PlayerRespawn 后执行，读取本 tick 原掉落运动/拾取/到期处理后的状态及服务端模拟时间。每 tick 重建所属玩家显示帧，只有字段变化才写入；无目标、资格拒绝、G文字与G高亮均关闭、连接失效、未采样和系统停止时清为Hidden；采样条件为pickupHud.enabled或interactionHighlight.enabled且gTargetsEnabled。单个玩家采样异常记录地图、NetworkId、玩家、DropId、阶段和原异常，继续其他玩家，未形成有效帧者清空。必需组件/配置缺失明确暴露，不创建默认数据或替代服务。

提示只写 PickupHudState；不修改 DropPhase/Quantity/进度、库存、Tools、奖励、资源预约、输入或存档，不控制运动、到期和释放。保存失败后的未到期目标仍可显示；提示不是保存成功标志，也没有新增拾取成功/失败反馈、动画或自动拾取。

## 【FACT】所属 Ghost 快照

CombatPrototypeMapPickupHudState 使用 OwnerSendType=SendToOwner，四个 GhostField 仅发送给所属玩家：

| 字段 | 当前含义 |
|---|---|
| Mode | byte 枚举：0 Hidden、1 Ready |
| DropId | int，Ready 为原正数 ID；Hidden 为0 |
| ItemId | FixedString64Bytes，原 vitality_apple/wood/stone；Hidden 为空 |
| Quantity | int，Ready 为目标实际正数量；Hidden 为0 |

没有同步世界位置、到期时间或另一套工作计时器，原四字段 F 状态保持。新状态不是输入/结算依据；网络延迟可能使提示滞后，显示后目标也可能被其他玩家取走或到期。当前位置下的 G 实际目标始终由服务端决定。

## 【CURRENT STRATEGY】显示、开关与释放

沿原绑定枚举启用 GhostOwnerIsLocal 并核对所属 Connected/InGame 连接，只取本地存活玩家快照；不在含可启用组件的查询上调用单例 API。PickupHudClient只读取烘焙Settings和显示快照，不读PlayerView/世界坐标，不修改玩家或服务端数据。原绑定另委托[高亮解析](MapInteractionHighlight.md)读取对应客户端掉落位置；Main Camera原宿主先画G/F圆环，再走原B、G、F面板路径，原输入和制作/丢弃反馈保持。

Ready 显示“G  Pick up  物品文案 ×实际数量”，Hidden 收起；DropId 保留在快照中供目标身份记录，屏幕不显示数值 ID。1920×1080 参考像素按 min(屏幕宽/1920,屏幕高/1080) 等比缩放，底部居中。默认400×52、底距168、字号20，与原320×104、底距48的 F 面板间隔16像素；两种提示可同时显示。黑色背景alpha=0.7、白色文字，复用内置GUI字体及Texture2D.whiteTexture，richText=false；仅Repaint绘制，缓存稳定文案/样式，恢复GUI.matrix/color，不接管鼠标或键盘事件。

pickupHud.enabled、interactionHud.enabled、inventoryPanel.enabled与interactionHighlight的主开关/F/G通道独立。关闭G文字仍可显示G高亮和实际拾取；关闭F HUD和背包面板仍可只显示G。三文字全关、无高亮通道且资源状态关闭时原绑定收起，键盘原玩法继续。死亡/断线、无本地玩家或地图、玩家/地图源变化、World/Scene停止及释放沿原Clear/Reset清掉可见状态和缓存，不保留上一局目标。非法网络快照明确报错并保持 G 隐藏，不以默认标签伪装有效目标。

## 【KNOWN ISSUES】静态证据与人工边界

v11/14拾取文字阶段正常Unity编译无C# Error，五脚本/meta、新Serializer/Snapshot 四字段与 SendToOwner、原13输入字段及系统入口已静态核对。Forest/Grassland 各覆盖 Json 默认、BuiltIn 默认、Json 关闭 G 提示、Json 关闭 F HUD和背包但保留G、Json 三种显示全关闭，共十次隔离 Editor 烘焙。schema11/revision14、全部新 Settings、玩家 Hidden/DropId0/空ItemId/Quantity0、原 F 初值/丢弃反馈、原掉落 Prefab/Prepared 和原布局一致。森林/草原树木89/53、采集点36/38、矿点20/18、阻挡109/71保持，布置位置/朝向逐项一致。

编译前 Console [0 Error,5 Warning,48 Log]；编译新增两条来自未修改 PEListener/DOTweenPreviewManager 的既有代码警告，没有新增 C# Error。隔离烘焙前后均[0 Error,7 Warning,48 Log]，无新增烘焙错误/警告；原五条运行 Tick Batching 警告保留，没有清空Console。原主场景干净，临时 World/Scene 与临时 TextAsset 已释放。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限CombatPrototypeNetCode、schemaVersion=11/configRevision=14及[运行入口](Runtime.md)十项清单。人工结论来自用户反馈；未实际触发的独立排版/字形/缩放、临界距离/同距、同tick、延迟/预测回放、多玩家/晚加入、断线/重连及配置/快照/保存失败仍为UNKNOWN。原丢弃v10/13、面板v9/12、工具v8/11、F HUD v7/10等用户通过保持各自版本/清单，不覆盖本次新显示。未验证的字体覆盖、运行性能/带宽、平台/线上及原保存成功后意外ECS恢复仍UNKNOWN。AI未运行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

v12/15的[高亮](MapInteractionHighlight.md)按原DropId解析同一客户端Landed Ghost，不另选最近目标或改变按G时的服务端选择。已核对新增14配置值、原G/F初值与13输入；正常编译及14次隔离Editor烘焙静态通过。用户确认高亮人工通过限v12/15十项，未实际触发的独立排版/网络/生命周期与失败用例仍UNKNOWN；上述v11/14文字提示用户通过保持原范围。

v13/16的[资源状态](MapResourceStatusHud.md)在G面板上方增加一行；G的开关、采样资格、四字段、DropId与拾取规则保持。资源状态不显示掉落期限，也不改变G蓝圈；仅状态开启仍保留整体绑定。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧G/高亮通过保持原版本/清单。

当前v15/18的[工具修理](MapToolRepair.md)新增两个输入和所属结果，遇同tick G请求时修理拒绝；G目标/资格/入包/保存/显示均保持原链，新增修理静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN。

## 【FACT】地图资源存档接入边界

世界档只存三类资源耗尽/再生，不存地面掉落；原G目标/所属四字段/TTL和入包事务保持。新阶段人工UNKNOWN，见[资源存档](MapResourcePersistence.md)。
