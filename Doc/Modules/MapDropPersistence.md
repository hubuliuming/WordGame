# 地面掉落物存档与恢复

返回[地图](Map.md)、[世界资源存档](MapResourcePersistence.md)、[掉落玩法](MapDrops.md)与[运行入口](Runtime.md)。本专题负责CombatPrototypeNetCode服务端地面物快照及恢复；G和背包丢弃的玩家保存事务仍归原模块。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [掉落DTO](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSaveData.cs) | 八字段掉落条目 |
| [校验](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSaveValidator.cs) | 严格JSON形状/编号/物品/数量/位置/期限 |
| [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPersistenceBindings.cs) | 按ItemId复用原inventoryDrop显式定义与Prepared Ghost Prefab |
| [快照](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPersistenceSnapshot.cs) | 原DropSpawn所有权采样、归一化落点、完整缓存比较与DTO投影 |
| [恢复](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPersistenceRestore.cs) | 恢复编号上限、逐物登记所有权，失败清理本批并整体失败 |
| [原DropSpawn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnSystem.cs) | Pending/Failed不生成敌人新物；恢复入口及原统一编号/所有权 |
| [原生成工具](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnUtility.cs) | 按保存落点/数量/寿命初始化Landed，无再次随机散落 |
| [世界保存](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceSaveSystem.cs) | 资源与掉落同时完整采样/保存，变更/10秒检查点/关闭 |
| [世界恢复](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceRestoreSystem.cs) | 三类资源与掉落全部成功后才Ready准入 |
| [世界文件](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapResourceSaveStore.cs) | 同文件写v2、合法v1内存迁移，原UTF-8/原子替换 |

掉落存档接入的五个脚本无MonoBehaviour挂载，meta由Unity正常导入生成。原G、DropMotion/DropCleanup、InventoryDrop提交、三类资源/再生、工具/修理、玩家SaveStore与Networking准入脚本保持；掉落存档链不新增输入、Ghost字段或场景挂载。Scene/SubScene/Prefab/Animator、旧meta/显式资源引用、图片/字体/材质、包与构建配置保持。

## 【FACT】配置与文件

Forest/Grassland Json与BuiltIn当前schemaVersion=38/configRevision=41。resourcePersistence六字段必填：enabled=true、saveSlotId=default_world、saveIntervalSeconds=10、saveGroundDrops=true、manualSaveEnabled=true及manualSaveCooldownSeconds=5；手动规则归[F5](MapWorldSaveHud.md)。saveGroundDrops严格布尔，旧地图v1～v37拒绝；关闭仍完整校验，正常导入/烘焙后生效，无热重载。Map Baker写七字段Settings：Enabled/SaveGroundDrops/SaveSlotId/SaveIntervalSeconds/LayoutSignature/ManualSaveEnabled/ManualSaveCooldownSeconds，恢复状态仍一字段Pending/Ready/Failed；当前输入19、Drop Ghost4、F4/G7/资源状态4及原反馈保持。

两个开关同时开启才恢复/保存掉落。仅saveGroundDrops=false时，资源照常恢复，地面物沿旧规则重启清空；下一成功世界快照写Drops=[]/LastDropId=0，会替换旧地面快照。enabled=false时不读写任何世界档、保留原文件。drops.enabled只控制敌人新掉落，inventoryDrop.enabled只控制新丢弃，不决定已有地面物的持久化；关闭这两功能仍保留原绑定。

路径保持`Application.persistentDataPath/CombatPrototype/Worlds/<saveSlotId>/<mapDefinitionId>.resources.json`，没有额外掉落文件。v2根恰含Version/SaveSlotId/MapDefinitionId/Seed/ConfigRevision/LayoutSignature/Resources/LastDropId/Drops九字段。原资源身份/布局与再生签名、Resources四字段规则归资源存档；新增开关、掉落绑定与TTL不加入资源签名，原资源布局/签名保持。

| 新字段 | 规则 |
|---|---|
| LastDropId | 非负int，保存已分配编号上限，包含失败空号；恢复后新编号从上限+1 checked递增 |
| Drops | 数组，仅已提交且未到期的Airborne/Landed地面物；不含Prepared/Consumed或CleanupQueued |
| DropId / ItemId / Quantity | 正int唯一编号且<=LastDropId；物品白名单vitality_apple/wood/stone；数量正int |
| PositionX / PositionY / PositionZ | 有限且可表示为float的世界落点；不钳制到地图内或做避让 |
| HasExpiry / RemainingLifetimeSeconds | 必填布尔/有限非负秒；false时余时必须0表示永久；true/0表示已到期 |

每项恰含表中八字段。严格UTF-8支持BOM、写无BOM；缺失/未知/重复字段、错误类型、非有限值、不支持版本、错槽/地图/seed/资源签名、坏编号/物品/数量/位置/期限均整档失败，不跳过坏项或回退初始图/.tmp。开启恢复时各ItemId必须匹配当前inventoryDrop.items显式Prefab，核对原Prefab/GhostType/LocalTransform/DropState/DropProgress及Prepared初态；缺失/坏绑定明确失败，不查找或创建兜底资源。关闭新开关仍严格校验v2字段与物品白名单，但不要求被忽略掉落的运行绑定。

合法世界v1严格读取原七根字段及原资源条目，保持耗尽/余时与签名校验；仅在内存补Drops=[]、LastDropId=0，读取不直接写盘。下一原状态变化/检查点/正常关闭保存写v2；不批量迁移、修正坏档或凭v1补造旧地面物。玩家文件写v4，合法旧档内存迁移归[工具升级](MapGatherToolUpgrade.md)；原候选保留工具与容量Level、路径保持。

## 【CURRENT STRATEGY】快照与恢复

世界Save在原三类Regrow及DropCleanup/InventoryDrop之后采样已提交结果。资源使用原双数组，掉落使用可复用双列表及编号集合；从DropSpawn原有效所有权枚举，已被EndSimulation ECB销毁的实体跳过。Prepared、Consumed、CleanupQueued及已到期物排除；Airborne取原EndPosition，Landed取当前LocalTransform.Position，归一化为重启后的落地快照。飞行移动本身不导致逐帧写盘；新增/移除物、编号上限、数量/落点/绝对期限或资源状态变化触发保存，平稳期间按10秒检查点更新余时。

资源及掉落均捕获完整合法才交换缓存并更新统一observedTime，任何捕获失败均保留上一完整快照，按原间隔重试，日志带map/slot/placement及DropId/itemId/原异常。只有保存时投影DTO/JSON；Resources/Drops/LastDropId同一次.tmp/Flush(true)/原子替换，关闭或源变化只读最后完整缓存与采样时刻，不读取已释放实体。

Restore仍在三类资源生成后、玩家准入前等待有效ServerTick。先完整校验世界文件及资源/掉落绑定，再恢复资源并初始化原DropSpawn所有权与编号上限。保存时飞行物直接在原定落点恢复Landed，原数量/ID保留；旋转为原identity、缩放用当前drops.visualScale，不重播飞行或重新散落。StartPosition/EndPosition均为保存落点，StartedAt为本次模拟时间，CleanupQueued=0；HasExpiry为true时ExpiresAt=本次时间+余时，为false时为0。true/0合法到期条目不生成，永久物不变成到期物。掉落已有寿命按档案恢复，新Lifetime只影响新生成物，离线时间不扣寿命。

仅初始化并登记成功的实例计入本批有效列表；一项创建/初始化/登记失败时清理本批已恢复掉落、保留原异常及独立清理错误，整体Failed拒绝准入，不部分Ready。编号不复用，达到int上限后的新分配沿原checked溢出错误；原DropSpawn继续拥有/清理实例，原DropCleanup/ECB处理G消耗和到期。恢复本身不扣材料/工具、发奖励或直接入包；之后G、目标文字与高亮沿原编号/身份链。

## 【KNOWN ISSUES】失败及验收边界

世界写失败保留旧正式档，继续本局并在原保存点重试，不回滚成功G/丢弃/采集/工具结算。玩家与世界分别原子替换，G已入包后若世界更新失败再异常中断，重启可能恢复旧掉落；本阶段未接跨文件防重复事务、同槽多服务端协调或意外ECS故障恢复保证。

掉落存档v16/19阶段正常Unity编译无C# Error，实际Assembly字段反射核对Settings5/配置4/世界根9/掉落8以及原Input15/DropGhost4。Forest/Grassland各12种隔离Editor烘焙共24次：Json/BuiltIn、世界关闭、自定义槽/25秒、全部再生关闭、树/矿/两类关闭、修理量/文案修改、掉落保存关闭、敌人新掉落关闭和背包丢弃关闭。两来源等价、保存开关映射、原资源签名兼容范围、完整原布局、三个既有掉落Prefab及反馈/工具/面板/F/G/高亮/资源状态初值均通过；默认树89/53、采集36/38、矿20/18、阻挡109/71保持。Console前后[0 Error,1 Warning,0 Log]相同，主场景干净，临时World/Scene/TextAsset释放。

主线程代码/配置静态验收通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v16/19、世界v2及[运行入口](Runtime.md)十二项清单，人工结论来自用户反馈。旧资源存档人工通过仍限v15/18、世界v1与十二项，其余旧用户通过保持各自版本/清单。未实际触发的独立I/O/v1迁移/重启/飞行窗口/TTL与永久物、配置/解析/绑定/捕获/创建/保存/清理失败、多玩家/晚加入/生命周期/时序仍UNKNOWN；跨文件原子一致与防重复、意外ECS故障恢复、同槽并发、未覆盖字形及性能/带宽/平台/线上不属于通过范围，仍UNKNOWN。AI未调用真实世界/玩家存档读写方法，未运行游戏/显示系统、GUI回调、逻辑单元测试、GamePlayer/PlayMode、构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：该阶段17输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。

## 【FACT】合并数量与期限的持久化

v26/revision29接入的[地面合并](MapDropMerge.md)沿原Owner集合转移数量/最早期限并将来源Consumed；原快照排除来源、比较目标新数量/期限/条目数，LastDropId保留分配上限与空号。世界v2根9/掉落8、路径、资源签名及玩家v4保持，无新文件/迁移/玩家保存。恢复物成功Landed且Ready后才参与合并，合法超99旧堆不拒绝或拆分，有限/永久不混合，离线暂停保持；关闭保存与写失败沿原规则，失败不回滚本局合并。新链静态及用户人工通过限v26/revision29十六项，未触发独立用例UNKNOWN，跨文件一致/意外ECS恢复/同槽并发仍UNKNOWN；原掉落存档通过限v16/revision19十二项。

## 【FACT】部分拾取后的剩余堆

当前v38/revision41的[部分拾取](MapDropPartialPickup.md)在玩家候选保存成功后仅减原堆Quantity，余量为正仍Landed且保留DropId/落点/ExpiresAt；原快照已比较Quantity，因此沿原世界保存点写剩余量，领空Consumed排除。世界v2九根/八掉落字段、LastDropId、资源签名、离线暂停与保存失败规则保持，没有新增跨文件事务；静态及用户人工通过限v27/revision30十六项，未触发独立用例UNKNOWN。
