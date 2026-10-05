# 资源状态与再生提示

返回[战斗地图](Map.md)。本专题负责CombatPrototypeNetCode三类资源的状态文字与所属再生秒数；原采集/砍伐/采矿、掉落、工具、阻挡、存档及再生仍归原模块。人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [配置](../../Assets/Scripts/CombatPrototype/Map/MapResourceStatusHudConfig.cs) | resourceStatusHud的11个必填字段 |
| [数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceStatusHudData.cs) | 地图Settings与玩家所属四字段Ghost快照 |
| [目标选择](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceStatusTargetSelector.cs) | 优先原F身份，否则按原各类距离选最近状态目标 |
| [服务端采样](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceStatusHudStateSystem.cs) | 原F HUD采样后读取真实阶段/期限，只写所属显示帧 |
| [客户端显示](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceStatusHudClient.cs) | 缓存文案/样式与独立一行绘制 |
| [原绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) / [宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) | 本地所属存活玩家、Configure/Show/Clear/Reset/Draw |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 原地图根追加Settings，原玩家实体追加Hidden快照 |

五个新脚本的meta由Unity正常导入生成。Scene/SubScene/Prefab/Animator结构、旧meta、资源绑定与材质/字体/图片保持；Main Camera原HUD委托普通帮助类，没有新MonoBehaviour挂载、Canvas、节点、世界标记或树桩。玩家烘焙后的Ghost布局新增一个所属组件，各端须同版代码/配置并重新烘焙；没有新版本协商协议。

## 【FACT】当前JSON契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为schemaVersion=21/configRevision=24。原geometry/layout/movement/drops/treeHarvest/mining/gatherTools/interactionHud/pickupHud/interactionHighlight/inventoryPanel/inventoryDrop/population/spawn及新增resourceStatusHud均必填。新段全部11字段沿原严格UTF-8、对象形状、未知/缺失/重复字段与标量类型校验；旧v1～v20拒绝，不迁移、补字段或回退来源。仅正常导入/烘焙后生效，没有热重载。

| 字段 | 当前默认值 | 校验 |
|---|---|---|
| enabled | true | 布尔值，独立控制资源状态显示 |
| panelWidthPixels | 400 | 有限，(32,1920] |
| panelHeightPixels | 52 | 有限正数，至少fontSize+32 |
| bottomMarginPixels | 268 | 有限非负；高度+底距≤1080，底距≥G底距+G高度+16 |
| fontSize | 20 | 正整数 |
| availableLabel | Available | 非空白、无控制字符、最多61 UTF-8字节 |
| workingLabel | Working | 同上 |
| occupiedLabel | In use | 同上 |
| regrowingLabel | Regrows in | 同上，后接权威整数秒数与s |
| waitingLabel | Waiting to regrow | 同上 |
| depletedLabel | No regrowth | 同上 |

disabled仍校验全部字段。名称复用interactionHud.gatherLabel/treeLabel/mineLabel，不另设资源名称真值。F仍320×104/底48、G改400×84/底168，高亮14值保持；状态行与默认G相隔16像素。默认三类原点再生true/600秒、空间/种子/32敌人/出生、工具与丢弃数值保持。

## 【CURRENT STRATEGY】目标、状态与权威秒数

服务端PredictedSimulation中在原InteractionHudStateSystem之后采样，原资源维护/再生及PlayerRespawn均已完成。每tick按资源捕获一次阶段、X/Z位置、原交互距离、Collector与RegrowAt，复用列表/身份缓存，再为Connected/InGame、未请求断线、CommandTarget所有权匹配、启用Simulate、存活且位置有限的玩家生成显示帧；只在字段变化时写入。状态采样不要求静止或近战Ready，不改变原F/G资格。

原F为Working、Ready或NoSpace时优先Kind/PlacementIndex身份：Working仍跟随本人原Collector，其他更近资源不使其切换；Ready/NoSpace仍对应原F有效目标；[容量](MapInventoryCapacity.md)不足的植物仍为Available。原F为Hidden时，在各类原交互距离内按X/Z中心距离平方选择最近的可用、使用中或耗尽资源，同距取小PlacementIndex，无类型优先。当前各类距离2米；关闭砍伐/采矿不选相应类型。该后备选择只用于状态文字，不改变实际F/G目标或原圆环身份，不发预约/输入/物品。

| Mode | 显示状态与依据 |
|---|---|
| 0 Hidden | 无有效附近目标、显示关闭或所属生命周期失效 |
| 1 Available | Available/Standing/Available；表示资源阶段可用，原工具与交互资格仍独立判断 |
| 2 Working | Collecting/Chopping/Mining且原Collector为本人 |
| 3 Occupied | 原活动阶段的Collector为他人 |
| 4 Regrowing | 原耗尽阶段且再生开启，服务端时间小于RegrowAt |
| 5 Waiting | 期限已到但原资源仍耗尽，等待权威实际恢复；不由此推断具体占位/失败原因 |
| 6 Depleted | 原耗尽阶段且再生关闭，本局不再生 |

Regrowing秒数为ceil(max(RegrowAt-服务端模拟时间,0))；以整秒桶变化写入，模式/身份变化不等待下一秒。服务端继续按原期限、占位检查及恢复链判断，倒计时不恢复资源。树/矿到期被原点角色占位或原恢复尚未成功时保持Waiting；原系统恢复后才显示Available。植物再生不新增占位规则；取消/保存失败不另设再生期限。禁用再生不显示倒计时。

玩家CombatPrototypeMapResourceStatusHudState仅SendToOwner同步Mode(byte枚举)、Kind(byte，1 Gather/2 Tree/3 Mine)、PlacementIndex(int)、RemainingSeconds(float，GhostField Quantization=0)。Hidden为0/0/-1/0；非Regrowing秒数为0，Regrowing为有限正整数秒。当前19输入（SaveWorld归[F5](MapWorldSaveHud.md)，修理归[专题](MapToolRepair.md)）、F四字段/G六字段、资源Ghost字段、工具/制作/丢弃反馈与v2写盘/v1读取迁移保持。Collector实体、绝对RegrowAt、工作计时、库存/资源/掉落状态和期限仍只属于原权威链；不新增世界状态存档。

## 【CURRENT STRATEGY】显示开关、生命周期与失败

状态帮助类只读Settings和最新所属快照，显示“原名称 | 状态”或“原名称 | Regrows in Ns”，不读取PlayerView/资源客户端位置，不使用客户端墙钟或unscaledTime补算/提前归零。网络延迟可能使剩余秒数暂时滞后；提示不保证当前按键可执行、资源恢复或保存成功。

宿主依次绘制高亮、背包、G文字、状态文字、F文字。状态行底部居中，按min(屏幕宽/1920,屏幕高/1080)等比缩放，黑底alpha0.7/白字、内置GUI字体/whiteTexture、richText=false，仅Repaint；缓存文案/样式并恢复GUI.matrix/color，不消费GUI事件或新增按钮。当前英文文案，中文可配置但字形覆盖未确认。

F采样条件为F文字开启、启用F高亮通道或resourceStatusHud.enabled；仅状态显示开启时仍保留原F优先身份。G采样仍只取G文字或启用G高亮通道。三文字/全部高亮/状态均关闭才收起整体绑定；各显示开关互不关闭原玩法。仅本地启用GhostOwnerIsLocal且所属Connected/InGame存活玩家显示，不对该可启用查询使用单例API。死亡/断线、无本地玩家/地图、玩家或地图源改变、World/Scene停止与释放沿Clear/Reset清状态文案、样式和快照，不保留上一局倒计时。

资源捕获失败记录地图/类型/布置索引/实体/阶段和原异常，排除当前项并继续；已失败身份不会在每个玩家处重复记录或转选另一F目标。F优先身份缺失/阶段不符、玩家帧失败按连接隔离，当前显示Hidden；未采样者、disabled与系统停止清Hidden。非法网络快照或绘制失败由新显示边界记录并只清状态行，保留原F/G/B及圆环。必需Settings、组件、HUD注册或来源缺失暴露错误，不查找节点、创建替代对象或补默认配置。

## 【KNOWN ISSUES】静态核对与人工边界

v13/16资源状态阶段正常Unity编译无C# Error，所属Ghost Serializer已生成；4字段/SendToOwner/RemainingSeconds无量化、11配置/Settings、原F/G各4及输入13字段经静态反射核对。Forest/Grassland各7次隔离Editor烘焙共14次：Json默认、BuiltIn默认、状态关闭、仅状态开启、全部显示关闭、三类再生关闭、采矿关闭。两来源一致，全部新Settings、玩家Hidden初值、原F/G/丢弃反馈、掉落Prefab/Prepared及完整配置对应布局核对通过；默认树89/53、采集36/38、矿20/18、阻挡109/71保持。烘焙Console前后[0 Error,7 Warning,47 Log]相同，主场景干净，临时World/Scene/TextAsset释放。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=13/configRevision=16及[运行入口](Runtime.md)资源状态十项清单，人工结论来自用户反馈。旧高亮v12/15十项、G文字v11/14十项、丢弃v10/13十二项、面板v9/12十二项、工具v8/11十二项、F HUD v7/10八项及更早用户通过保持各自版本/清单。未实际触发的倒计时/到期等待/再生关闭精确边界、后备最近目标/原F锁定时序、移动攻击显示、多玩家/晚加入/断线及独立配置/快照/绘制/保存失败仍UNKNOWN；性能/带宽、平台/线上与旧保存成功后意外ECS恢复未验证。AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

当前v21/24的[工具修理](MapToolRepair.md)不修改本专题四字段、目标优先级或资源/再生期限；绑定增加修理反馈与按钮输入交接，原状态/倒计时仍只读权威链。修理静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN；资源状态用户通过仍限v13/16十项。

## 【FACT】地图资源存档接入边界

资源档恢复剩余秒数为本次服务端RegrowAt，状态行仍读原权威四字段；离线计时暂停，不由客户端推演恢复。资源存档人工通过限v15/18十二项，未触发用例UNKNOWN，见[资源存档](MapResourcePersistence.md)。 掉落恢复及同文件快照归[掉落存档](MapDropPersistence.md)，人工通过限v16/19十二项，未触发用例UNKNOWN。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：该阶段17输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。

## 【FACT】容量等级接入边界

[升级](MapInventoryCapacityUpgrade.md)：个人等级改变容量，资源状态仍沿原目标/Available与再生投影；四所属字段、期限和预约规则保持。新链静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧通过保持原版本/清单。
