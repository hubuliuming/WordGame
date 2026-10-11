# 背包容量扩展与升级

返回[背包](Inventory.md)、[地图](Map.md)与[材料容量](MapInventoryCapacity.md)。本专题负责 CombatPrototypeNetCode 的个人永久容量等级、升级配方、所属反馈和 B/5 输入。玩家文件的唯一契约归[资源与数据](DataResources.md)，人工清单归[运行入口](Runtime.md)。当前地图 schemaVersion=41/configRevision=44；用户已确认人工GamePlayer通过，限v20/revision23升级十六项。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [UpgradeConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryCapacityUpgradeConfig.cs) / [LevelConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryCapacityUpgradeLevelConfig.cs) | 必填 inventoryCapacityUpgrade 与等级条目的严格 JSON DTO |
| [UpgradeData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradeData.cs) | 原地图根的固定 Settings/Definition，以及玩家所属 Level/Feedback |
| [UpgradeSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradeSystem.cs) | 服务端资格、旧操作优先、配方、完整候选保存与升级提交 |
| [UpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradePanel.cs) | 原 B 滚动区内的只读等级/容量/配方预览及一次按钮请求 |
| [FeedbackClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUpgradeFeedbackClient.cs) | 本人序号变化、结果文案与显示期限 |
| [CapacityUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUtility.cs) | 根据玩家等级选实际容量定义；原 F/G 整批入包与超限规则 |
| [输入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 数字5与面板按钮合并到同一所属 InputEvent；交接只读快照 |
| [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) / [准入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeNetCodeLifecycle.cs) | Level=1、零反馈初值，以及固定 ID 存档等级恢复 |
| [玩家存储](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) | v4完整候选（工具/容量等级）及唯一SavePrepared入口 |

六个普通 C# 脚本及对应 meta 由正常 Unity 导入接入，没有新增 MonoBehaviour 挂载。原 Scene/SubScene/Prefab/Animator、旧 meta、资源引用、包与构建设置保持。等级不进入世界资源文件；没有新 RPC、另一份库存或配置热重载，各端使用同版代码/配置并重新烘焙。

## 【FACT】等级与默认配置

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)默认值一致。

| 目标等级 | 三种材料总上限 | 苹果/木材/石材单种上限 | 从前一级升级的成本 |
|---|---:|---:|---|
| Lv1 | 300 | 各200 | 新玩家与合法旧档迁移初值 |
| Lv2 | 450 | 各300 | 木材20、石材10 |
| Lv3 | 600 | 各400 | 木材40、石材20 |

Lv1 继续由 inventoryCapacity 定义；inventoryCapacityUpgrade.levels 只配置 Lv2/Lv3。每名玩家持有独立等级，最多 Lv3，每次请求只升一级；死亡/R 不重置，固定 ID 重连/服务端重启沿玩家文件恢复。

| 配置字段 | 当前默认值与约束 |
|---|---|
| enabled | true，必填布尔；只控制付费升级 |
| feedbackSeconds | 2，有限正数；只决定客户端结果显示时长 |
| upgradeLabel / upgradeButtonLabel | Backpack upgrade / Upgrade |
| levelLabel / maxLevelLabel | Lv / Max level |
| successLabel / rejectedLabel / failureLabel | Backpack upgraded / Upgrade rejected / Upgrade failed |
| levels | 恰好两个非 null、唯一且合法的等级2/3；数组顺序可调整 |
| levels[].level | 必填 int，2或3 |
| levels[].maxTotalQuantity | 必填 int，严格大于前一级总上限 |
| levels[].items | 三个非 null 条目，vitality_apple/wood/stone各一次；沿原两字段材料 DTO |
| levels[].items[].maxQuantity | 必填 int，按 itemId 严格大于前一级同种上限 |
| levels[].woodQuantity / stoneQuantity | 必填正整数；相应等级的完整升级配方 |

根10字段、每级5字段、每种材料2字段。七个文案均非空白、无控制字符、最多61个 UTF-8 字节。沿原严格 UTF-8、完整对象形状、缺失/未知/重复字段、标量类型和语义检查；关闭容量、升级或显示也完整校验。旧地图 v1～v22 明确拒绝，没有补字段、来源回退或自动迁移。

原地图根追加 UpgradeSettings 九字段：Enabled、FeedbackSeconds及七文案；两条 UpgradeDefinition 各七字段：Level、MaxTotalQuantity、AppleMaxQuantity、WoodMaxQuantity、StoneMaxQuantity、WoodQuantity、StoneQuantity。Baker 按已验证的 itemId 映射数值，未改变原容量2字段 Settings/三条3字段 Definition。

## 【CURRENT STRATEGY】资格、保存与提交

UpgradeSystem 位于服务端 PredictedSimulationSystemGroup，InventoryDrop 之后、统一 F/PlayerRespawn 之前。只处理 Connected/InGame、CommandTarget 与所有权匹配、启用 Simulate 的当前玩家；复用原 F 资格，必须存活、静止、无 Attack 请求且近战 Ready。存在植物 Collecting、树木 Chopping 或矿点 Mining 预约时拒绝。

同 tick 有 F/G/E/R、制作1/2、修理3/4或丢弃请求时，升级拒绝，即使旧请求自身未成功也保持旧操作优先。数字5与按钮均写同一个 UpgradeInventoryCapacity InputEvent；原输入由16变17字段，没有目标、等级或成本命令。F5 仍沿世界保存入口，未加入升级旧操作互斥表；其资格和冷却保持原规则。

容量总开关或升级开关关闭时拒绝付费升级；Lv3 返回 MaxLevel。服务端根据当前 Level 查下一等级定义，再核对同名木材/石材是否足额，不从客户端预览取得成本。材料不足不保存或扣减；每份合法请求独立处理，错误日志带 stage/map/NetworkId/player，保存后的错误另有 saved 标记；所有权不符不向错误接收者写反馈。

提交顺序为：取得 Level/Feedback 可写引用与库存/工具 → 准备全部扣料、保留其余库存/金币/经验/Tools的完整 v3 候选 → 原 SavePrepared 成功 → 非结构性提交两项材料及新 Level → 本人 Success。归零条目沿原缓冲移除规则清掉；文件替换之前不修改玩家材料或等级，失败返回 Failed，旧正式文件和 ECS 保持。没有自动重试、退级或返还材料入口。

玩家当前Version=4，容量字段InventoryCapacityLevel自v3接入，严格整数1～3；v1/v2仅内存补容量Lv1，v3保留原容量级。Tools旧v2/v3二字段补工具Lv1、v4三字段恢复工具等级，读取不写盘、不赠工具，下一正常保存v4。坏档继续整档拒绝，完整契约见[资源与数据](DataResources.md)/[工具升级](MapGatherToolUpgrade.md)。

原九个保存入口——击杀奖励、E使用、植物F、G拾取、工具制作、修理、背包丢弃、树木/矿点工具完成——均把当前实际玩家等级加入候选，避免后续保存把等级改回1。原 SavePrepared 路径、临时文件/Flush/替换、保存先于 ECS 提交及各自失败隔离保持。

## 【CURRENT STRATEGY】有效容量与开关

服务端 F 预约/完成、G 接收及所属 F/G 提示均根据实际玩家等级选容量；B Snapshot 同步选择相同定义。只读 Snapshot 在等级变化时刷新上限及文字，即使库存数量未变也更新。三种材料每件计1、总量与单种同时检查、整批接收或拒绝、最近目标/同距规则保持；Tree/Mine 完成仍只生成地面物。小块肉、其他合法名称、Tools、金币/经验保持原容量边界。

合法旧超限库存仍完整恢复并显示，不截断、不删档或拒绝准入。升级本身只要求资格和材料足额；扣料后按新等级上限重新判定，仍超限则所有受管新入包继续拒绝。配置调整后重烘焙也沿原超限规则。

inventoryCapacity.enabled=false 时 B 显示 Unlimited，F/G 原整批结算不设材料容量；付费升级禁用，已获得等级仍保留。仅 inventoryCapacityUpgrade.enabled=false 时禁用升级，已有 Lv2/Lv3 限制仍有效。F/G/B 及其他显示开关只影响展示；关闭 B 或全部显示仍可数字5，由服务器给出实际结果。

## 【CURRENT STRATEGY】B 预览、所属反馈与生命周期

原 B 滚动区在材料之后、工具之前增加10行：标题、当前→下一等级、总/苹果/木材/石材上限、现有/所需配方、缺口、5升级按钮及结果。Lv3 显示 Max level，关闭容量显示 Unlimited；库存投影有效、材料足额、两个开关开启且未满级时按钮可用。预览未判定移动/战斗/预约资格，按钮可用不保证服务器接受。

按钮沿原有效面板鼠标按下标记消费一次；数字5单次按下可在 B 关闭时提交。原380×640、字号18/行高32、滚动/指针隔离与制作/修理/Drop/All保持，打开 B 不暂停世界或战斗。客户端不扣料、不升级、不保存，Server Level 与反馈到账后才显示结果。

新增玩家 CapacityLevel 的 Level 一个 GhostField；UpgradeFeedback 的 Sequence(uint)/Result(byte enum)两个 GhostField，均 SendToOwner。Result 固定 None=0、Success=1、Disabled=2、MaxLevel=3、InsufficientMaterials=4、Busy=5、ExistingOperationHasPriority=6、PlayerUnavailable=7、Failed=8。成功/满级/失败分别使用对应配置，其余拒绝统一 Upgrade rejected；详细原因在服务端日志。

升级反馈只在本通道显示，客户端 unscaledTime 仅决定2秒期限。初次绑定只观察既有 Sequence，不重播旧结果；非法 Result 或 None搭配非零序号记录原异常并隐藏本反馈。关闭B清未提交请求和滚动位置；无有效玩家帧隐藏面板并清请求。死亡/断线及源/玩家/World/Scene变化沿原绑定Reset清投影、旧序号/文字/期限。等级永久状态由服务器/玩家档保留，不由显示清理重置；已进入 NetCode 命令的请求仍按服务端当次资格处理。

## 【CURRENT STRATEGY】配方分类、搜索与偏好关联

当前v41/revision44的[配方偏好](MapInventoryRecipePreferences.md)把配方类别与已应用关键词接入本机偏好v4九字段；独立保存开关、严格v1/v2/v3迁移、关闭项保留和重置/延迟/失败规则归专题。材料与工具状态、原输入/Ghost及服务器事务/玩家和世界档案保持；本阶段待人工。搜索用户通过仍限v40/revision43十六项，分类仍限v39/revision42原清单，其他旧通过保持原范围，未触发独立用例仍UNKNOWN。

## 【KNOWN ISSUES】静态与人工边界

v20容量升级阶段正常Unity编译通过，输入17字段、当时玩家存储v3、等级和反馈的生成 Serializer/Snapshot；所属属性及全部新字段已核对。Forest/Grassland各11组隔离 Editor Bake，共22次：Json/BuiltIn默认、关闭容量、自定义基础容量/顺序/旧文案、分别关闭F/G/B、全部显示关闭及容量关闭组合、单独关闭升级、自定义两级上限/成本/顺序/七文案/反馈1.5秒。新 Settings/两级 Definition、Lv1/零反馈、原玩家/工具/三掉落 Prefab/显示与持久化初值符合，Json/BuiltIn等价。

两份地图各48个非法配置，共96份，均经真实严格 JSON 来源拒绝；覆盖根/字段缺失或未知、null、错误类型、非法等级/数量、非递增上限、坏材料ID、坏文案及关闭开关仍校验。上述仅是配置/元数据核对，没有调用升级系统、存档业务读写或 GUI 回调。

与修改前快照比较，区块/格子/装饰位置朝向/障碍及资源布局签名保持；默认森林/草地树89/53、采集36/38、矿20/18、阻挡109/71保持。Bake Console前后[0 Error,8 Warning,113 Log]，无新增 Bake 警告，含六条既有运行警告及未修改 PEListener/DOTween 的两条编译警告；主场景干净，临时 World/Scene/TextAsset 已释放。

用户已确认本阶段人工GamePlayer通过；主线程结合既有代码/配置静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v20/revision23及[运行入口](Runtime.md)升级十六项，人工结论来自用户反馈。旧253项内容/编号及容量 v19/revision22 用户通过保持原版本/清单。实际扣料/重复与同tick输入、保存失败/迁移/坏档/等级保持、多玩家/晚加入/生命周期、运行字体/滚动布局、同步与延迟均未由 AI 验证；性能/带宽/平台/线上、跨文件原子一致/防重复、同槽并发及保存后意外 ECS 故障恢复仍 UNKNOWN。

AI 未执行 GamePlayer/PlayMode、游戏/显示系统或 GUI 回调、逻辑单元测试、命令行构建、发布、性能采样、图片检查或真实玩家/世界存档 I/O，未创建子Agent、未提交 Git。

## 【FACT】详情扩容用途

v32/revision35详情阶段的[材料详情](MapInventoryDetails.md)沿原RequireUpgradeDefinition读取Lv2/Lv3配方；容量/升级开关启用且未满级时，只列当前下一等级木材/石材需求。单种容量从原Snapshot当前等级读取，容量关闭或未受管材料显示Unlimited；原扩容资格/事务及玩家v4容量等级保持。详情人工已获用户通过反馈，限v32/revision35十六项，未触发独立用例仍UNKNOWN，原扩容用户通过仍限v20/revision23十六项。

## 【FACT】收藏显示边界

v33/revision36收藏阶段的[收藏](MapInventoryFavorites.md)：收藏不参与容量统计或等级/配方/5请求，完整库存与原升级事务保持；原扩容用户通过范围保持。人工收藏十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN，旧通过仍限各自版本/清单。

## 【FACT】仅看收藏边界

v34/revision37筛选阶段的[收藏筛选](MapInventoryFavoritesFilter.md)：筛选不影响全量容量统计、扩容配方/等级或5输入及服务器事务。本阶段十六项人工GamePlayer已获用户通过反馈，限上述版本及运行入口清单；未触发独立用例仍UNKNOWN，旧通过范围保持。

## 【FACT】收藏计数边界

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)：计数不参与完整容量/等级、扩容配方或5资格。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；筛选旧通过限v34/revision37及其他阶段原版本/清单。

## 【FACT】收藏保护与容量升级

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：收藏材料仍计完整容量并参与原扩容配方/5资格，容量等级与服务端保存链保持；收藏丢弃保护不限制原扩容消耗。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】容量升级配方的收藏提示

[收藏提示](MapInventoryFavoritesConsumptionHint.md)沿原CapacityUpgradePanel.Capture读取实际当前级的下一定义WoodQuantity/StoneQuantity和同一已应用Favorites。容量与升级均开启、未满级且正成本材料已收藏时显示一行缓存文字，材料不足仍显示；满级/关闭隐藏。原等级/配方/CanUpgrade、5与一次按钮请求/所属结果及完整候选保存保持；GUI行与Panel滚动总高度共用可选行计数。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，升级旧通过仍限v20/revision23十六项。

## 【CURRENT STRATEGY】配方消耗确认

原CapacityUpgradePanel.Capture按实际当前级下一定义的木石成本、完整材料数、当前容量等级和同一Favorites捕获确认候选。原5面板按钮行可替换为Confirm/Cancel，数字5直达；关闭容量/升级、满级或材料不足沿原资格，容量/配方/所属反馈及完整候选保存保持。 单个待确认、取消条件与滚动/点击许可归[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；本专题旧通过及v37提示通过均保持各自原版本/清单。
