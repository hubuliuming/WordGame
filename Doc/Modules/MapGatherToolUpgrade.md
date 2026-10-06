# 采集工具升级与效率提升

返回[采集工具](MapGatherTools.md)、[工具修理](MapToolRepair.md)与[材料面板](MapInventoryPanel.md)。本专题负责 CombatPrototypeNetCode 的工具 Lv1～Lv3、B/6/7 升级、有效耐久上限与采集耗时；玩家文件完整契约归[资源与数据](DataResources.md)，人工清单归[运行入口](Runtime.md)。当前地图 schemaVersion=34/configRevision=37，玩家存档 Version=4；用户已确认本阶段人工 GamePlayer 通过，主线程结合既有静态核对判定通过，限 v21/revision24 及[运行入口](Runtime.md)二十二项清单，结论来自用户反馈。未实际触发的独立边界仍为 UNKNOWN。背包升级及更早用户通过仍限各自原版本/清单。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [UpgradeConfig](../../Assets/Scripts/CombatPrototype/Map/MapGatherToolUpgradeConfig.cs) / [LevelConfig](../../Assets/Scripts/CombatPrototype/Map/MapGatherToolUpgradeLevelConfig.cs) | gatherToolUpgrade 根与 toolId/level 平铺条目的严格 JSON DTO |
| [UpgradeData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradeData.cs) | 原地图根固定 Settings/Definitions、玩家所属升级反馈 |
| [UpgradeSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradeSystem.cs) | 资格、旧操作优先、单级配方及完整候选保存后提交 |
| [UpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradePanel.cs) / [FeedbackClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradeFeedbackClient.cs) | 原 B 滚动区的只读预览、一次按钮请求及本人结果期限 |
| [ToolUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUtility.cs) | 按实际 Level 选有效 MaxDurability/DurationMultiplier；开始锁定实际耗时 |
| [原制作](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolCraftSystem.cs) / [原修理](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairSystem.cs) | 制作/重做满耐久 Lv1；修理按当前级上限封顶且保级 |
| [输入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) / [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | UpgradeAxe/UpgradePickaxe 两个事件及零所属反馈 |
| [存储](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) / [SaveTool](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveTool.cs) / [准入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeNetCodeLifecycle.cs) | v4 三字段工具候选、合法旧档内存迁移及等级恢复 |
| [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) / [HUD](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) / [原 B](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) / [修理预览](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairPanel.cs) | 所属工具校验、有效上限与等级变化缓存刷新 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [配置校验](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) | 原根追加固定升级配置，完整形状/语义校验 |

六个新普通 C# 脚本及对应 meta 由正常 Unity 导入接入；客户端助手没有 MonoBehaviour 挂载。原 Scene/SubScene/Prefab/Animator、旧 meta、资源引用、图片/字体、包和构建配置保持。玩家工具 Ghost/输入布局变化，各端须使用同版代码、配置和重新烘焙数据；没有新 RPC 或运行配置协商。

## 【FACT】当前配置与默认值

Forest、Grassland JSON 与 BuiltIn 默认一致。新必填 gatherToolUpgrade 根恰含11字段，Settings10字段；levels 恰四条，每条6字段，按 toolId+level 唯一匹配，数组顺序可调整。Lv1 沿原 gatherTools，升级段仅定义 Lv2/Lv3。

| 根字段 | 默认值 |
|---|---|
| enabled / feedbackSeconds | true / 2.0 |
| upgradeLabel / upgradeButtonLabel | Tool upgrade / Upgrade |
| levelLabel / maxLevelLabel | Lv / Max level |
| successLabel / rejectedLabel / failureLabel | Tool upgraded / Upgrade rejected / Upgrade failed |
| recraftLabel | Recraft at Lv1 |
| levels | 下表的两工具 × 两级平铺条目 |

| 工具 | 等级 | maxDurability | durationMultiplier | 默认实际耗时 | 从前一级升级木/石 |
|---|---:|---:|---:|---:|---|
| stone_axe | 1 | 60 | 0.75 | 1.5秒 | 制作木3/石2 |
| stone_axe | 2 | 90 | 0.60 | 1.2秒 | 12 / 8 |
| stone_axe | 3 | 120 | 0.50 | 1.0秒 | 24 / 16 |
| stone_pickaxe | 1 | 40 | 0.75 | 2.25秒 | 制作木2/石3 |
| stone_pickaxe | 2 | 60 | 0.60 | 1.8秒 | 8 / 12 |
| stone_pickaxe | 3 | 80 | 0.50 | 1.5秒 | 16 / 24 |

每条 levels 字段为 toolId、level、maxDurability、durationMultiplier、woodQuantity、stoneQuantity。ID 仅 stone_axe/stone_pickaxe；level 仅整数2/3，不重复或缺级。maxDurability 相比前一级严格增大；倍率有限、正数、<=1且逐级严格减小，乘对应 treeHarvest/mining 基础耗时后仍为有限正数；木/石成本均为正整数。feedbackSeconds 有限正数；八个文案均非空白、无控制字符且最多61 UTF-8字节。

新段沿原严格 UTF-8、全部字段、类型、未知/重复键与数组条目校验，任意相关开关关闭也须完整合法。旧地图 v1～v22 明确拒绝，不补默认段或回退来源；配置仅在正常导入/烘焙后生效，无热重载。关闭 gatherToolUpgrade.enabled 只拒绝新升级，已获等级的有效上限/倍率仍使用。关闭 gatherTools.enabled 则沿原徒手路径并禁用制作、修理、升级，等级保留；重新开启按已持有等级与当前配置使用。

## 【CURRENT STRATEGY】工具等级与原操作

Level 属于当前这把工具记录，每种仍只有一个独立工具槽；不是永久玩家技能。每次只升级一级，最高 Lv3。升级保留当前绝对耐久：例如斧头30/60→30/90，0/60→0/90；不按比例换算或补耐久。损坏0仍可升级，升级后仍沿徒手路径，修理后达到单次成本才可加速。

3/4 修理保持原配方木1/石1、恢复斧头20/镐子15，恢复=min(恢复量,本级最大值-当前耐久)，近满仍扣完整配方；修理保持 Level。1/2 只沿原未持有或耐久不足单次成本的资格制作，创建/覆盖满耐久 Lv1；重做已损坏高等级工具会失去该把工具旧等级，B 按钮明确显示 Recraft at Lv1。仍可用工具不能重做。死亡/R不重置当前工具，固定ID重连/重启从玩家档恢复。

F 原三类最近目标、距离、同距索引和资源互斥保持。树/矿开始时按本级倍率设置原 ActualDuration/FinishAt，工作期间不改耗时；活动预约时升级拒绝。成功完成仍按原成本1扣耐久，取消/失败不扣，工具候选保留 Level。植物徒手1秒、树/矿基础2/3秒、地面wood/stone×3、距离、600秒再生、阻挡与战斗行为保持。

## 【CURRENT STRATEGY】服务端升级与保存事务

数字6请求斧头、7请求镐子；按钮与键盘合并到各自同一 InputEvent，单次按下触发，按住不连续；同tick6/7只处理斧头。UpgradeSystem在InventoryCapacityUpgradeSystem之后、统一F入口与PlayerRespawn之前执行，继承原伤害/制作/修理/丢弃链顺序。

只收集 Connected/InGame、无断线请求且 CommandTarget 指向启用 Simulate 的网络玩家；复用 F 的归属、存活、有限且零 Move、无 Attack 及近战 Ready 检查。归属不符不覆盖真正所属玩家反馈；任意植物Collecting/树Chopping/矿Mining预约拒绝。必须已持有该工具，未满级、开关开启且木/石足额。

同tick F/G/E/R、制作1/2、修理3/4、Drop/All、背包升级5任一请求均优先，工具升级拒绝，即使旧操作自身也失败；不改变旧业务之间的优先规则。F5继续按原世界资格/冷却独立处理。

先取得提交引用/缓冲和完整状态，准备扣料与仅 Level+1 的工具副本；复用 PrepareToolCraft 投影完整 Items、两个 Tools、实际 InventoryCapacityLevel、金币/经验/身份。SavePrepared 正式替换成功后才无结构变更/新增分配扣木石、移除归零材料、写工具新Level和Success反馈。保存前失败保持原材料、工具、容量等级与正式档，不自动重试；每请求隔离记录 stage/map/NetworkId/player/tool/saved及原异常，其余请求继续。保存后意外ECS异常不以旧档补偿，故障恢复保证仍 UNKNOWN。

## 【FACT】同步、v4 与旧档迁移

原 CombatPrototypeMapGatherTool 保持唯一可变状态及内部容量2，每条 ToolId(FixedString64Bytes)、Durability(int)、Level(int) 三个 GhostField 均 SendToOwner。新增 CombatPrototypeMapToolUpgradeFeedback 的 Sequence(uint)、Kind(byte工具枚举)、Result(byte结果枚举) 三个 GhostField 同样仅所属，Baker初始0/None/None。Result依次None、Success、Disabled、NotOwned、MaxLevel、InsufficientMaterials、Busy、ExistingOperationHasPriority、PlayerUnavailable、Failed；反馈不作为耐久或等级真值。当前输入19字段；地图配置不由Ghost发送，原F4/G6/资源状态4/世界保存3保持。

| 玩家文件版本 | 根字段数 | Tools条目 | 加载到当前内存 |
|---|---:|---|---|
| v1 | 5 | 无Tools | 空Tools，容量Lv1 |
| v2 | 6 | ToolId/Durability，两字段 | 每把工具补Lv1、耐久保持，容量Lv1 |
| v3 | 7 | ToolId/Durability，两字段 | 每把工具补Lv1、耐久保持，原InventoryCapacityLevel保留 |
| v4 | 7 | ToolId/Durability/Level，三字段 | 两种等级原值恢复 |

v4 Tools最多两条唯一已知ID；Level严格整数1～3，耐久严格整数0～本级配置最大值，0合法。旧v2/v3先校验旧二字段形状和Lv1最大值，再只在内存补工具Lv1；合法v3容量等级不能重置为1。金币/经验/库存保持，读取不写盘、不赠工具或耐久，下一正常保存统一写v4。缺失/额外/重复字段、null、未知/重复ID、错误类型、不支持版本和非法级别/耐久整档拒绝，原坏档保留；不批量重写或修正。

所有既有玩家候选保存入口，以及背包/工具升级，都携带当前工具Level和实际容量Level；工具完成的扣耐久副本、修理副本保级，重做显式Lv1。原路径、UTF-8、临时文件Flush/原子替换保持。世界档仍v2、根9/掉落条目8、槽/签名和路径保持；世界档不保存工具等级，没有新增跨文件事务。

## 【CURRENT STRATEGY】B 预览与显示生命周期

原B修理区之后新增14滚动行：标题、每工具六行（名称/当前→下一等级和状态、当前耐久/上限→保留耐久/新上限、当前→下一采集秒数、现有/所需木石、缺口、6/7按钮）及本人结果。未拥有/满级/材料不足、工具或升级开关关闭、库存展示快照无效时按钮禁用；预览不判定移动/战斗/预约，服务器资格最终决定。

HUD统一校验所属工具ID/重复/Level/本级耐久上限，只读缓存四条升级定义；原F第二行与B工具状态显示Lv及有效最大值。B/修理预览将Level纳入缓存，即使升级保持耐久和其他数量不变也刷新上限/恢复量。未拥有的本地显示Level0仅代表缺槽，不写入工具Ghost/玩家档。

升级结果只在B本通道使用配置文案显示默认2秒：成功/满级/失败分别显示对应文案，其余拒绝统一Upgrade rejected，服务端日志保留具体原因。初次绑定只观察当前Sequence，不重播旧反馈；非法结果/工具身份或None搭配非零Sequence清该反馈并记录异常。客户端unscaledTime只控制显示期限，不参与权威升级/采集完成。

按钮沿原可见有效绑定的鼠标按下标记提交一次；关闭B清待提交请求/滚动位置，已进入命令的请求由服务器判断。无有效帧、死亡/断线、玩家/地图源/World/Scene失效沿原Reset清投影/旧文字/序号/期限，不重置服务器工具。关闭B或全部显示不关闭数字6/7；客户端没有扣料、保级、恢复耐久或存档写入入口。原380×640、字号18/行高32、指针/滚轮隔离及F/G/高亮/资源状态显示规则保持。

## 【KNOWN ISSUES】静态证据与人工范围

正常Unity编译通过，实际新类型及生成Serializer/Snapshot核对工具三字段、升级反馈三字段及SendToOwner，输入19、玩家存储v4/根7/工具项3。新增配置根11/条目6、Settings10/Definition6×4和原F/G/世界/面板布局已核对。Forest/Grassland各11组隔离Editor Bake共22次：Json/BuiltIn默认、新升级关闭、工具关闭、分别B/F/G关闭、全部显示关闭、自定义四档最大值/倍率/木石成本/八文案/反馈1.75秒及平铺数组顺序、修理关闭、背包升级关闭。值/初值/原工具及三掉落Prefab对应，两个来源一致。

两地图各68份非法配置，共136份均被实际严格来源拒绝：缺失/未知/重复字段、null、类型错误、坏ID/级别/成本、非递增最大值/非递减倍率、非有限/非正值、非法文案、旧schema20及关闭仍校验。上述只验证配置与烘焙元数据，未调用升级/采集/修理系统、存档业务方法或GUI回调。

与修改前v20快照比较，区块/格子/布置位置朝向/障碍及资源签名保持，森林/草地树89/53、采集36/38、矿20/18、阻挡109/71。Bake Console前后[0 Error,8 Warning,113 Log]，无新增Bake警告；主场景干净，临时World/Scene/TextAsset释放。git差异范围/UTF-8/链接与原269项内容保留检查通过。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有编译、配置/所属Serializer与隔离Editor Bake静态核对判定通过，范围限CombatPrototypeNetCode、v21/revision24及[运行入口](Runtime.md)二十二项清单，人工结论来自用户反馈。原291项内容/编号和旧通过范围保持；未实际触发的精确同tick、独立准备/保存/提交失败、旧档/坏档边界、网络延迟/预测回放及显示/生命周期用例仍UNKNOWN，AI未验证上述运行行为；性能/带宽/平台/线上、跨文件原子一致/防重复、同槽并发及保存后意外ECS恢复仍UNKNOWN。AI未运行GamePlayer/PlayMode、游戏/显示系统/GUI回调、逻辑单元测试、真实玩家/世界存档I/O、命令行构建、发布、采样或图片检查，未创建子Agent/提交Git。

## 【FACT】等级变化与耐久预警

[耐久预警](MapToolDurabilityHud.md)读取原有效本级上限，斧头60/90/120、镐子40/60/80；升级保持绝对耐久，因此相同耐久可能跨预警阈值，缓存同时比较Durability/Level。显示不恢复耐久、不扣材料或改变耗时/等级/保存链。预警阶段v22/25静态及用户人工通过，限v22/revision25十六项，未触发用例UNKNOWN；本专题既有编译/136配置拒绝/22次Bake及用户人工通过仍限工具升级v21/revision24二十二项。

## 【FACT】详情升级用途

v32/revision35详情阶段的[材料详情](MapInventoryDetails.md)沿原RequireUpgradeDefinition复制只读Lv2/Lv3配方，按真实ToolId+Level匹配，不依赖数组顺序；工具/升级开关启用且持有未满级工具才列下一等级需求。满级或未持有不列升级用途，原升级资格/扣料/效率及保存保持；详情人工已获用户通过反馈，限v32/revision35十六项，未触发独立用例仍UNKNOWN，原工具升级用户通过仍限v21/revision24二十二项。

## 【FACT】收藏显示边界

v33/revision36收藏阶段的[收藏](MapInventoryFavorites.md)：收藏不修改工具等级/耐久/效率、原配方来源、6/7请求或升级事务；原工具升级用户通过范围保持。人工收藏十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN，旧通过仍限各自版本/清单。

## 【FACT】仅看收藏边界

当前v34/revision37的[收藏筛选](MapInventoryFavoritesFilter.md)：筛选不参与工具有效等级/耐久/效率或配方/6/7请求，预览与服务端升级使用原完整数据。本阶段十六项待人工GamePlayer验收，旧通过范围保持。
