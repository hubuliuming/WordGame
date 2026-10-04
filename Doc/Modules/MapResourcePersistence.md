# 地图资源状态存档

返回[地图](Map.md)、[数据](DataResources.md)与[运行入口](Runtime.md)。本专题负责CombatPrototypeNetCode的服务端资源快照、恢复及准入门；玩家材料/Tools仍归原v2存档，地面掉落仍归[掉落](MapDrops.md)。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [配置DTO](../../Assets/Scripts/CombatPrototype/Map/MapResourcePersistenceConfig.cs) | enabled/saveSlotId/saveIntervalSeconds三个必填字段 |
| [运行数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourcePersistenceData.cs) | 四字段Settings、Pending/Ready/Failed恢复状态及资源身份绑定 |
| [存档DTO](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceSaveData.cs) | v1七字段根与四字段耗尽条目 |
| [布局签名](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceLayoutSignature.cs) | 烘焙边界计算资源布局/再生规则SHA-256 |
| [存储](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceSaveStore.cs) | 严格UTF-8/JSON/身份校验和临时文件原子替换 |
| [恢复](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceRestoreSystem.cs) | 三类实例生成后整体校验、恢复耗尽/期限/阻挡，再开放准入 |
| [保存](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceSaveSystem.cs) | 原资源结算/再生后采样完整状态、变更/10秒检查点/关闭保存 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原地图根追加Settings与恢复状态，不修改挂载 |
| [原准入](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeNetCodeLifecycle.cs) | Pending保留请求等待，Failed消费请求并断开，Ready沿原玩家加载 |

七个新脚本均无MonoBehaviour挂载，meta由Unity正常导入生成。原采集、砍树、采矿、三类再生、工具/修理、玩家SaveStore、G及掉落生成/清理脚本未修改；通过独立系统读取原已提交结果。Scene/SubScene/Prefab/Animator、旧meta、图片/字体/材质、资源引用、包和构建配置保持。

## 【FACT】配置与建议值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与BuiltIn当前schemaVersion=15/configRevision=18；新增必填resourcePersistence段，原所有地图段/数值保持。沿原严格字段、对象形状、标量类型与UTF-8校验，旧v1～v14拒绝，不迁移、补默认或回退来源；正常导入和烘焙后生效，无热重载。

| 字段 | 默认值 | 校验 |
|---|---|---|
| resourcePersistence.enabled | true | 必填布尔值；false不读写世界档并沿原布局重置 |
| resourcePersistence.saveSlotId | default_world | 1～61个小写ASCII字母、数字或下划线；拒绝con/prn/aux/nul/com1～com9/lpt1～lpt9 |
| resourcePersistence.saveIntervalSeconds | 10 | 有限正数秒，按Server World模拟时间决定检查点 |

关闭存档仍校验全部字段。Map Baker写入Enabled/SaveSlotId/SaveIntervalSeconds/LayoutSignature四字段，签名为64个小写十六进制字符，使用FixedString128Bytes；另写一字段恢复状态，开启为Pending、关闭为Ready。地图根仍不是Ghost；新数据由服务端系统消费，没有玩家/资源Ghost字段或输入变化。当前输入15、F/G/资源状态各4及原反馈/资源字段保持，各端同版代码/配置重新烘焙。

## 【FACT】文件与身份

服务端路径为Application.persistentDataPath/CombatPrototype/Worlds/&lt;saveSlotId&gt;/&lt;mapDefinitionId&gt;.resources.json；同槽的Forest/Grassland使用不同文件，玩家原Players/&lt;PlayerId&gt;.json路径保持。目录只在运行保存时创建，AI静态验收不读取或写入真实玩家/世界存档。

| 根字段 | 规则 |
|---|---|
| Version | 整数1，仅支持当前v1 |
| SaveSlotId / MapDefinitionId | 字符串，与当前槽/地图精确一致 |
| Seed | 正uint整数，与当前种子一致 |
| ConfigRevision | 正int整数，记录保存时配置修订，不作为布局兼容判断 |
| LayoutSignature | 字符串，与当前烘焙资源签名精确一致 |
| Resources | 仅包含耗尽植物、砍倒树木、耗尽矿点；数量不超过当前有效资源数量 |

每项恰含Kind（gather/tree/mine）、PlacementIndex（非负int）、ObjectId（当前定义ID）、RemainingSeconds（有限非负秒数）。PlacementIndex在三类资源间唯一；必须匹配当前实例、类型和物体定义，不接受重复/未知条目。剩余秒数不超过当前RegrowSeconds，关闭再生必须为0；启用再生的0表示已经到期。可用资源和工作中资源不存入Resources；重新加载沿原初始可用状态，工作进度/预约清空，须新F。

签名以固定二进制字段顺序计算SHA-256，包含格式标识、地图ID/种子、地图原点/大小/格子/高度、角色半径/留缝，以及按原PlacementIndex排列的有效资源类型/ID、位置/朝向、占地/移动阻挡、再生开关/间隔。装饰不单独存状态；改变它导致资源布置索引/位置变化时同样会改变签名。槽名、保存间隔、configRevision、HUD文字和工具修理量不参与资源签名；资源功能关闭、布局或再生规则变化可能使原档不兼容。签名不符明确拒绝准入，换槽建立新世界，旧档保留；没有自动迁移或覆盖坏档。

UTF-8严格读取，支持BOM；写UTF-8无BOM。缺失/未知/重复字段、错误类型（含数字字符串/整数位置的浮点数）、额外根后内容、不支持版本、非有限秒数、错身份或签名均失败，不部分跳过坏项。只把FileNotFoundException/DirectoryNotFoundException认作首次无档；权限/I/O/解码/JSON失败交恢复边界记录。只读正式.resources.json，遗留.tmp不是恢复来源。

## 【CURRENT STRATEGY】恢复与准入

RestoreSystem仅ServerSimulation，在Simulation组的GatherSpawn/TreeSpawn/MineSpawn之后、GoInGameServer之前运行；等待地图、Spawner与有效NetworkTime.ServerTick。首次源绑定按原对象/布置与三类已生成实例建立身份集合，同时核对必要进度/历史和树/矿障碍；缺实例、重复/多余实例或缺障碍导致整批恢复失败，原逐项生成隔离仍保持。

先完整加载并校验所有存档条目，预留树/矿阻挡历史容量，再应用快照。首次无档先原子保存空Resources的v1档，成功才Ready；初始写失败为Failed。植物设Depleted，树设Felled，矿设Depleted；CollectorNetworkId/Collector、工作时刻/工具锁定均清零。再生期限重建为本次模拟时间+剩余秒数，关闭再生为0；已到期树/矿保持正期限，以进入原占位检查。没有系统墙钟或离线补时，关闭服务器期间暂停计时。

树/矿不恢复旧World的绝对tick或全部历史。各耗尽实例建立一条本次有效ServerTick前一tick的Disabled=1基态，写FelledTick/MinedTick并关闭原障碍；原预测系统继续按同局转换历史重建阻挡。可用实例保持原空历史/启用阻挡，之后砍倒/再生继续追加原历史。位置、朝向、PlacementIndex和Ghost实体沿本次原布局生成；读取本身不发奖励或生成掉落，到期恢复沿原再生系统处理。

全部应用成功后设置Ready；Pending时原准入保留RPC等待，Failed时消费请求并断开连接，日志包含map/slot与MapResourceRestoreFailed；Ready沿原玩家ID、v1/v2加载、库存/Tools、GhostOwner/CommandTarget和连接寿命链。恢复异常在边界记录stage/map/slot/path/原异常一次，不自动重载、不将部分应用伪装成成功，也不改坏档；部分ECS应用后的意外故障恢复仍UNKNOWN。

## 【CURRENT STRATEGY】保存、失败与释放

SaveSystem仅ServerSimulation，位于预测组的三类Regrow之后、PlayerRespawn之前，因此采样原采集/砍伐/采矿结算和再生的最终状态。Ready且存档开启才绑定原恢复身份集合；每tick用预分配双缓冲采样全部资源，全部读取合法后交换缓存。工作阶段归为未耗尽，耗尽状态及启用再生的绝对期限变化触发保存；剩余秒数按最后完整采样模拟时间计算，钳制到[0,RegrowSeconds]。

平稳期间按默认10秒检查点保存剩余秒数；状态变化触发当前保存点。只有保存时建立DTO/序列化，不每tick序列化或写盘。捕获失败记录map/slot/placement/原异常，保留最后完整缓存，按间隔再采样，不把半批读取写成世界档。世界文件使用同目录.tmp、Flush(true)、File.Replace（已有正式档）或File.Move（首次）；失败保留旧正式档并保持待保存状态，下一个状态变化/检查点再尝试，无玩家奖励/耐久回滚。

资源完成与世界快照是两条独立提交链：植物原SavePrepared成功才入包/耗尽，工具完成原保存耐久才提交，G原保存才入包；新增世界保存位于这些结果之后，写失败不撤销成功结算。玩家与世界分别原子替换，异常中断时不保证跨文件事务一致，恢复以各自最近成功文件为准；没有同槽多服务端写权锁，相关并发保证UNKNOWN。同步世界写盘耗时/频率、容量和性能仍UNKNOWN。

源变化、OnStopRunning或OnDestroy尝试最后保存并释放缓存；只使用已持有完整缓存及最后采样时间，不读取已由原Spawn释放的实体，停止后清标记避免重复关闭写入。异常终止或未完成最后写盘只能恢复最近成功检查点。原Spawn继续拥有/销毁资源实例，原DropSpawn/DropCleanup继续拥有地面掉落，没有新增共享资源释放。

地面苹果/木材/石材、DropId/TTL、敌人/玩家位置与战斗状态、工作进度/预约、工具锁定、旧World阻挡全部历史均不进入本档。恢复资源不生成补偿掉落，不调用玩家存档或奖励。玩家材料与Tools仍按原写v2/读v1迁移恢复；死亡/R、F/G/1/2/3/4/B及原工具修理优先级保持。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译无C# Error，实际Assembly-CSharp反射核对四字段Settings、一字段恢复状态、七字段存档根/四字段条目及15输入；未新增Ghost字段或序列化布局。Forest/Grassland各9种隔离Editor烘焙共18次：Json/BuiltIn默认、存档关闭、自定义槽test_world/25秒、全部再生关闭、砍树关闭、采矿关闭、两类关闭及修理量/文案修改。配置两来源等价，四Settings/64字符签名/Pending或Ready映射、签名兼容范围、完整原布局及原工具/面板/F/G/资源状态/反馈/掉落Prefab初值均通过。默认树89/53、采集36/38、矿20/18、阻挡109/71保持；烘焙Console前后[1 Error,4 Warning,4 Log]一致，既有Error为UnityConnect Token Exchange网络错误；无新增编译/烘焙错误，主场景干净，临时World/Scene/TextAsset释放。

主线程代码与配置静态验收通过；新阶段人工GamePlayer为UNKNOWN，十二项清单见[运行入口](Runtime.md)。旧修理v14/17十二项及更早用户通过保持各自版本/清单。AI未调用真实世界存档读写方法、游戏/显示系统或GUI回调，未执行逻辑单元测试、GamePlayer/PlayMode、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。文件I/O/坏档/关闭恢复、到期占位/预测基态、跨文件/意外ECS失败、多玩家/同槽并发/生命周期、字形、性能/带宽、平台/线上未实际运行，仍UNKNOWN。
