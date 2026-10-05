# 采集工具耐久预警与损坏提示

返回[地图](Map.md)、[采集工具](MapGatherTools.md)与[材料面板](MapInventoryPanel.md)。本专题负责CombatPrototypeNetCode原F/B中的耐久状态颜色、文案、剩余次数和修理入口提示；工具升级/有效等级归[工具升级](MapGatherToolUpgrade.md)，真实修理归[工具修理](MapToolRepair.md)，存储契约归[资源与数据](DataResources.md)。当前地图schemaVersion=22/configRevision=25；静态核对通过，16项人工GamePlayer待验收，仍UNKNOWN。旧工具升级用户通过限v21/revision24二十二项，其他通过保持原版本/清单。

## 【FACT】文件与接入链

| 文件 | 本次职责 |
|---|---|
| [MapGatherToolDurabilityHudConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapGatherToolDurabilityHudConfig.cs) | 新普通Serializable DTO，必填根11字段 |
| [CombatPrototypeMapGatherToolDurabilityHudData.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolDurabilityHudData.cs) | 新IComponentData固定Settings11字段，无GhostField |
| [CombatPrototypeMapGatherToolDurabilityHudClient.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolDurabilityHudClient.cs) | 新普通只读助手、两工具Durability/Level缓存、状态/颜色/详情投影 |
| [MapDefinitionConfig.cs](../../Assets/Scripts/CombatPrototype/Map/MapDefinitionConfig.cs) | 原根新增gatherToolDurabilityHud字段 |
| [CombatPrototypeMapConfigValidator.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) | 原语义入口新增根非null、阈值/颜色/文案校验，契约22 |
| [CombatPrototypeDefaultMapConfigSource.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) | BuiltIn默认11值，与两地图JSON一致 |
| [CombatPrototypeMapAuthoring.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原地图根Baker写固定Settings，颜色复用既有Hex转换 |
| [CombatPrototypeMapInteractionHudBindingSystem.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 原有效绑定将新Settings交给HUD Configure |
| [CombatPrototypeMapInteractionHud.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) | 原工具快照验证后捕获只读投影；F第二行颜色/损坏提示、传两帧给B、Reset清缓存 |
| [CombatPrototypeMapInventoryPanel.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 原工具状态行标签/着色、两详情滚动行、绘制后恢复颜色 |

JSON/BuiltIn→原严格读取/语义校验→原地图Baker→地图固定Settings→原本地HUD绑定→HUD验证所属Tools及有效本级定义→只读投影→F/B。新增三个普通脚本与Unity正常生成meta；没有新MonoBehaviour挂载、Scene/SubScene/Prefab/Animator结构或已有meta/资源/包/构建配置改动。

## 【FACT】配置字段与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)和BuiltIn均为schema22/revision25。必填gatherToolDurabilityHud恰11字段，全部显式配置：

| JSON字段 | 默认值 | 约束与用途 |
|---|---|---|
| enabled | true | 严格布尔；与gatherTools.enabled共同控制新增显示 |
| warningRatio | 0.25 | 有限数值，0<criticalRatio<warningRatio<1 |
| criticalRatio | 0.10 | 同上；仅可用工具进入Critical |
| warningColorHex | #FFD166 | 严格#RRGGBB，Low黄色 |
| criticalColorHex | #FF9F43 | 严格#RRGGBB，Critical橙色 |
| brokenColorHex | #FF6B6B | 严格#RRGGBB，Broken红色 |
| warningLabel | Low | B工具状态低耐久文案 |
| criticalLabel | Critical | B工具状态极低耐久文案 |
| brokenLabel | Broken | B工具状态损坏文案 |
| remainingUsesLabel | Uses | B详情剩余次数前缀 |
| repairHintLabel | Repair | F损坏/B预警详情的原3/4修理入口提示 |

五文案均非空白、无控制字符且最多61个UTF-8字节，烘焙为FixedString64Bytes；Settings为Enabled字节、两float比例、三float3 RGB和五FixedString，共11字段。严格JSON沿原完整形状、缺失/null/未知/重复键、字段类型及UTF-8读取检查；新开关、工具或各显示关闭仍完整校验。旧地图v1～v21明确拒绝，没有迁移、默认补齐或来源回退；正常Unity导入/烘焙后生效，无运行热重载。

## 【FACT】状态优先与等级

使用原唯一所属工具快照，Durability和Level在HUD原边界校验后，以本级有效definition.MaxDurability为分母；没有复制可变工具状态。未持有沿原独立未持有分支，不作为Broken。已持有状态依次判断：

| 优先 | 条件 | 标签/颜色 |
|---|---|---|
| 1 | Durability < DurabilityCostPerCompletion | Broken/红色，即使耐久仍为正数 |
| 2 | 可用且Durability/本级MaxDurability <= criticalRatio | Critical/橙色 |
| 3 | 可用且比例 <= warningRatio | Low/黄色 |
| 4 | 可用且比例 > warningRatio | 原普通文案/白色 |

剩余可完成次数=整数向下取整Durability/DurabilityCostPerCompletion，当前默认成本1；自定义成本沿原工具定义读取，不新增费用字段。该数字只描述当前耐久能够支付的完整次数，不表示已有资源、材料或预约资格。

斧头Lv1/2/3上限60/90/120，镐子40/60/80。按原6/7升级只改变等级/上限且保留绝对耐久，所以投影缓存同时比较Durability与Level，耐久未变也更新比例、颜色和文案。例如斧耐久10在Lv1/2为Low、Lv3为Critical。原修理恢复20/15并本级封顶/保级；修理后按快照重算，仍处阈值内则保留预警，例如Lv3斧0→20仍Low。原1/2重做恢复满Lv1，显示随真实结果更新。

## 【CURRENT STRATEGY】F显示与反馈

原F面板320×104、底48、字号20、进度条10及1920×1080等比缩放保持。第一行F目标/权威进度与原进度颜色保持；仅对应斧/镐工具状态第二行使用投影颜色，名称/Lv等级/当前和本级最大耐久沿原格式。Low/Critical标签显示在B，F通过颜色表达；F不新增剩余次数行。

未持有工具保留Hands与1/2制作提示，植物保留Hands。损坏且新显示/工具/修理开启时，斧头提示3: Repair、镐子4: Repair；关闭修理则沿原1/2 Recraft at Lv1。新显示关闭恢复原F文字/白色。提示仅说明现有入口，不保证材料充足或通过服务端资格。

NoSpace第一行/第二行目标、未到期修理优先于制作等原反馈优先保持，旧反馈占第二行时仍白色；无F目标仍仅原制作/修理反馈临时显示，此时第二行对应工具状态可着色。没有独立耐久弹窗、提示Sequence、警告计时器或新的反馈事件。原制作/修理反馈仍复用已有客户端时间，采集完成仍由服务端锁定耗时决定。

## 【CURRENT STRATEGY】B详情与开关

B工具状态行在原名称/Lv等级/当前和本级最大耐久之后显示Low/Critical/Broken并着色；每把工具紧随一详情行，显示Uses整除次数，预警/损坏且修理开启时追加原3/4 Repair入口，损坏且关闭修理则原1/2 Recraft at Lv1。普通工具详情只有Uses次数；未持有详情沿原Not owned。

只有gatherToolDurabilityHud.enabled与gatherTools.enabled均true才追加两滚动行。新显示关闭恢复旧损坏标签（原inventoryPanel.brokenLabel）/白色，不追加行；工具关闭仍沿原F Hands、B Disabled，保留已获状态且不追加行。原B380×640、字号18、行32及配方/修理/背包和工具升级/丢弃/页脚保持，只增加内容滚动高度两行。

ToolLabel单独绘制工具状态并以finally恢复GUI.color；详情与其他行仍沿原白色。B/F开关独立，其他G/高亮/资源状态/世界保存显示及原按钮请求、输入合并/鼠标隔离保持。全部原显示关闭时，新开关不强行创建宿主或绑定；静态Bake确认配置仍存在，实际界面隐藏/命中须人工验收。

## 【CURRENT STRATEGY】快照与生命周期

原Binding继续枚举本地启用GhostOwnerIsLocal的唯一有效玩家/连接、地图源和生命，沿原宿主及Configure。HUD先验证Tools条数、ID/唯一槽、Level、Durability与本级范围，再向普通助手Capture；助手不保存DynamicBuffer跨帧、不写ECS/文件、不自行遍历World或读取输入。它只缓存Durability/Level和显示字符串/颜色；本次不改变原验证边界。

Configure先Reset再保存固定只读设置；每帧Clear只隐藏原显示，保留缓存避免重复生成。死亡、断线、来源/玩家变化、World/Scene释放等原Reset路径释放新状态、恢复白色并清标签/详情；B Reset同步清两个只读帧。重新绑定根据当前快照重新计算，不重播历史耐久警告。

## 【FACT】玩法、网络与存储边界

未修改服务端工具使用/制作/修理/升级、资源产出/范围/600秒再生、保存先于扣料/提交、原输入请求或玩法系统。工具仍唯一ToolId/Durability/Level三GhostField、内部容量2且SendToOwner；输入19字段，F4/G6/资源状态4/世界保存3与原反馈保持。新Settings无GhostField，显示没有新网络事件或计时字段。

玩家写Version4、根7/Tools项3，合法旧档迁移和所有候选保存沿原契约；世界v2、根9/掉落项8、路径/资源布局签名保持，没有新存档字段或业务读写调用。本次显示不影响等级、耐久、材料、工作完成或已保存状态。

## 【FACT】已完成的静态核对

正常Unity导入/编译完成且实际Console为0 Error；元数据确认新Config/Settings各11字段、无新GhostField/MonoBehaviour、输入19/工具三字段所属同步/玩家Version4/世界根9保持。两默认JSON与BuiltIn新值一致，其他配置数值保持。

两份地图各120份非法配置共240份，通过原LoadValidated入口全部拒绝，覆盖必填/形状/缺失/null/未知/重复键/类型、阈值顺序/边界/溢出及非有限输入、三颜色格式、五文案空白/控制字符/UTF-8长度、关闭时仍校验与旧v21。属于配置边界检查，不是游戏逻辑单元测试。

两份地图各14次，共28次隔离Editor Bake：JSON/BuiltIn默认、工具升级/工具/B/F/G/全部旧显示关闭、自定义四条升级定义与顺序、修理/背包升级关闭、新预警关闭、自定义单次成本2/3、自定义阈值0.4/0.2和颜色/五中文文案。全部新11Settings与来源一致；原根/反馈/输入/存储字段、资源引用、布置/保护区与资源签名保持。Forest仍89树/36采集点/20矿点/109阻挡，Grassland53/38/18/71；Player初始Tools空、容量Lv1、原反馈全零。

Bake前后实际Console均[0 Error,7 Warning,53 Log]，没有新增Bake警告/错误；正常编译重报两条已有PEListener UAC1001与DOTweenPreviewManager CS0618警告。原主场景CombatPrototypeNetCode干净且未Play，所有临时TextAsset/克隆对象/World/BlobAssetStore与临时Editor场景释放，原SubScene只读使用后恢复。

## 【KNOWN ISSUES】人工验收与未验证范围

本阶段[运行入口](Runtime.md)16项人工GamePlayer仍UNKNOWN，包括阈值边界/正耐久损坏、等级变化但耐久未变时刷新、修理仍低耐久、F反馈优先、B两行与GUI颜色恢复、开关/绑定、晚加入/联网/生命周期、原玩法回归。原291项编号/内容逐字保留，新增后307项；工具升级用户通过仍限v21/revision24二十二项，旧结论不覆盖新颜色和提示。

AI未执行GamePlayer/PlayMode、游戏/显示系统或GUI回调、逻辑单元测试、真实玩家/世界存档业务I/O、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。中文字形/排版、网络延迟/预测回放及未触发独立边界、性能/带宽/平台/线上仍UNKNOWN；跨文件原子一致/防重复、同槽并发和保存后意外ECS故障恢复仍沿原未知边界。
