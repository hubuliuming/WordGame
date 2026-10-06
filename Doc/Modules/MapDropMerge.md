# 同类地面掉落物合并

返回[地图](Map.md)、[掉落与拾取](MapDrops.md)、[掉落存档](MapDropPersistence.md)、[背包丢弃](MapInventoryDrop.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode；Forest/Grassland Json与BuiltIn当前schemaVersion=35/configRevision=38。用户已确认本阶段人工GamePlayer通过，主线程结合既有代码、配置及隔离烘焙静态核对判定通过，限v26/revision29及运行入口十六项；人工结论来自用户反馈；旧G结果通过仍限v25/revision28十六项，其他旧通过保持原版本/清单。

## 【FACT】入口与文件

| 职责 | 文件 |
|---|---|
| 四字段DTO | [MapDropMergeConfig](../../Assets/Scripts/CombatPrototype/Map/MapDropMergeConfig.cs) |
| 四字段服务端Settings | [DropMergeData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropMergeData.cs) |
| 扫描节奏、所属集合及错误隔离 | [DropMergeSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropMergeSystem.cs) |
| 资格、批次数据、选对及数量/期限提交 | [DropMergeUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropMergeUtility.cs) |
| 配置接入 | [MapDefinitionConfig](../../Assets/Scripts/CombatPrototype/Map/MapDefinitionConfig.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 原落地/编号与集合 | [Motion](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropMotionSystem.cs)、[Spawn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnSystem.cs) |
| 原G与清理 | [Pickup](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs)、[Cleanup](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropCleanupSystem.cs) |
| 显式来源 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

四个新脚本为普通代码，meta由Unity正常导入生成；没有新增MonoBehaviour、Scene/SubScene/Prefab/Animator结构或绑定。合并代码仍限地图DTO/校验/BuiltIn/根Baker及非到期清理，Motion/Spawn、三类资源/工具、玩家输入/保存、高亮与世界保存/恢复保持；当前G结算及所属HUD按[部分拾取](MapDropPartialPickup.md)接收规则执行。

## 【FACT】JSON契约与建议默认值

地图根必填dropMerge及恰四字段，Json/BuiltIn一致：

```json
{
  "dropMerge": {
    "enabled": true,
    "mergeDistanceMeters": 0.8,
    "maxStackQuantity": 99,
    "scanIntervalSeconds": 0.2
  }
}
```

| 字段 | 类型与约束 | 实际用途 |
|---|---|---|
| enabled | 严格bool | 仅开关服务端地面合并；独立于敌人新掉落、丢弃、采集和显示开关 |
| mergeDistanceMeters | 有限正float | X/Z中心距离，边界相等允许合并；平方比较使用double避免有限float平方溢出/下溢 |
| maxStackQuantity | 正32位int | 单对合计不能超过上限；不改库存容量或原生成数量 |
| scanIntervalSeconds | 有限正float | 服务端模拟时间的扫描间隔；到时间才扫描，每次最多执行一轮 |

原JsonReader严格检查UTF-8、完整形状、缺失/null/未知/重复键、标量类型及float可表示范围；Positive验证距离/间隔，数量必须大于0。关闭仍完整校验，旧地图v1～v28明确拒绝，没有迁移、补默认、来源回退或运行热重载。正常导入/烘焙后生效，各端使用同版代码/配置并重新烘焙。

根Baker写Enabled(byte)、MergeDistance(float)、MaxStackQuantity(int)、ScanInterval(float)四Settings。标注GhostPrefabType.Server且没有GhostField；地图根不是Ghost。原掉落DropId/ItemId/Quantity/Phase四GhostField及LocalTransform同步保持，没有新输入、RPC、玩家反馈、玩家/世界存档字段或联网配置协议。

## 【CURRENT STRATEGY】服务端扫描与合并

DropMergeSystem仅ServerSimulation，在PredictedSimulationSystemGroup中UpdateAfter原DropMotion、UpdateBefore原DropPickup；随后原DropCleanup、资源处理/再生、世界保存及HUD采样继续。新系统等待地图RestoreState.Ready，仅从当前源DropSpawn.GetOwnedDrops读取已登记实例及原编号上限，不从客户端或另一地图收集目标。敌人掉落、树木/矿点产出、背包丢弃与成功恢复物共用该集合。

默认每0.2秒扫描；首次有效源立即具备扫描资格，下一期限为本次模拟时间+间隔，不循环补扫错过的轮次。未Ready或关闭时不合并；源更换、停止及World销毁清源、扫描期限、候选列表和编号集合。普通托管集合复用容量；原DropSpawn仍拥有实例与编号，新系统不递增/复用编号或改动其集合。

原EndSimulation ECB已正常移除的实例跳过；其余直接读取必需DropState/DropProgress。只有Landed、未到期且CleanupQueued=0参与；Airborne、Prepared、Consumed和已排队/到期物排除。参与项检查正数量、正且不超过原分配上限的编号、已知vitality_apple/wood/stone、有限位置及有限非负期限，重复候选编号记录错误并跳过当前项。必需组件缺失明确暴露，不补组件或伪造数据。

合法候选按DropId升序；每个来源只考虑前面仍存活的较小编号目标。须ItemId完全相同、寿命类别一致且合计不超过上限；在距离内取最近，精确同距取较小DropId。保留目标的位置、旋转、缩放和编号，当前批次缓存随成功结果同步更新；被吸收项不再作为目标。合计超限直接跳过，不部分合并或拆分；已有Quantity>上限的大堆仍合法、独立保留，也不参与合并。默认上限99不代替背包总量/单种上限，All原始大堆不按99拆分。

提交前取得来源/目标状态及目标进度可写引用、来源进度，核实所选身份/数量/期限/资格，checked准备合计数量；全部准备成功后写目标Quantity、目标ExpiresAt及来源Phase=Consumed，不做结构变更。目标有限寿命取两者最早到期时间；两者永久时保持0，有限与永久互不合并。不刷新StartedAt或寿命：新物合入旧堆可能更早到期，整堆在最早期限到期。成功后记录双方编号、物品、来源/目标原数量、新数量及期限；合并本身不扣/发库存、工具或奖励，不发布拾取成功。

被吸收物沿原CleanupQueued/EndSimulation ECB释放，客户端原Consumed隐藏与Ghost移除保持。Cleanup非到期reason由PickedUp改为Consumed，涵盖真实拾取及合并释放；到期仍Expired，实际G提交仍有原拾取完成日志。

## 【FACT】G、显示与存档边界

G仍由服务端按玩家距离/同距小DropId选择当前真实目标，NetworkId升序、移动/攻击/死亡资格及SavePrepared先于库存及地面余量或Consumed提交保持。当前G按[部分拾取](MapDropPartialPickup.md)开关接收：默认领取容量可容纳的数量，零余量NoSpace，关闭恢复整堆判定，仍不改选较远物。G七字段含真实地面量与可领量；高亮及四字段[拾取结果](MapPickupFeedbackHud.md)沿原真实身份，成功反馈携带本次实际领取增量。F/B、世界保存HUD及显示关闭条件没有新接入。

世界保存沿原完整快照：Consumed来源排除，保留目标的新数量、原位置/编号及最早期限；原比较能检测数量/期限/条目数变化。LastDropId保留原分配上限和被吸收编号空号，不回退或复用。世界v2路径、根9字段、掉落项8字段、资源布局/再生签名与玩家v4格式保持。成功恢复物仍先按旧档原数量/编号/余时Landed恢复，Ready后才参与新扫描；合法超99旧堆不拒绝、不拆分，离线暂停计时规则保持。

世界/掉落保存关闭时沿原不保存/不恢复规则，合并开关独立。世界写失败仍保留旧正式档、当前合并继续存在，沿原保存点重试；不回滚本局合并或追加玩家保存。玩家/世界跨文件一致性、防重复、同槽并发及异常ECS恢复没有新增保证。

## 【KNOWN ISSUES】错误隔离与静态证据

整批地图/Settings/恢复状态/Owner依赖失效记录Drop merge batch failed及ReadMap/ReadSettings/ReadOwner、map/source/原异常，停止本轮。单项读取错误记录ReadCandidate、map/DropId/itemId/ownerIndex/entity/原异常，继续后续项；单对选择/准备提交/完成日志错误记录SelectPair/CommitPair/LogMerge、map/sourceDropId/targetDropId/itemId/quantity/entity/原异常，继续后续来源。准备失败不开始写入；没有兜底服务、组件、默认值、配对回退或自动恢复。意外部分ECS写入故障恢复仍UNKNOWN。

正常脚本编译完成，四新类型及4字段DTO/Settings已加载；元数据确认ServerSimulation、Motion之后/Pickup之前、Settings仅Server且0 GhostField。合并v26阶段核对输入19、DropGhost4、Tools3、F4/G6/资源4/世界保存3/F失败2/F结果3/G结果4、玩家v4根7/Tools项3与世界v2根9/掉落项8保持。

194份非法配置全部拒绝（每地图97），28组合法读取通过（每地图14）：根/四字段形状、缺失/null/类型、未知/重复键、非有限或非正距离/间隔、非正/越界/非整数数量、关闭仍校验、旧v1～v25及额外内容；合法覆盖Json/BuiltIn一致、新/原功能关闭、永久新掉落、自定义1.5米/7份/0.35秒和float/int边界。最大float检查数据使用精确double数值写JSON，避免float文本舍入越过原Reader上界；Reader代码保持。

两地图各32次，共64次隔离Editor Bake通过：原22类配置/关闭/自定义组合，新增合并关闭/自定义、敌人新掉落关闭、丢弃关闭、掉落保存关闭、世界保存关闭、永久新掉落、数值上下界及全部显示/合并关闭。四Settings及原所有反馈初值/配置/资源引用、布局/签名保持；Forest89树/36采集点/20矿点/109阻挡，Grassland53/38/18/71。源SubScene只读使用，临时TextAsset/克隆对象/Editor场景/World/BlobAssetStore释放，主场景干净、未Play、一场景三根对象。

Console在早期新JSON导入时留下两条旧程序集未知dropMerge字段记录；新程序集加载后已重新导入两JSON，当前严格读取和全部Bake通过。Bake前后实际Console均[2 Error,5 Warning,53 Log]，无新增Bake记录，未清空Console，不宣称历史Error为0。

## 【KNOWN ISSUES】人工验收边界

原355项人工内容/编号逐字保留，本阶段追加16项后共371项，归[运行入口](Runtime.md)。用户已确认本阶段人工GamePlayer通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v26/revision29及运行入口十六项；人工结论来自用户反馈。未实际触发的数量守恒/身份选择、距离与数量临界、寿命/永久混合、多人争抢、容量及保存失败、所属集合/恢复/生命周期、时序与客户端展示独立用例仍UNKNOWN。旧G反馈通过保持v25/revision28十六项及旧阶段原版本/清单。

AI未执行合并工具或游戏/显示系统、GUI回调、GamePlayer/PlayMode、逻辑单元测试、真实玩家/世界存档业务I/O、命令行构建/发布、性能/带宽采样或图片检查，未创建子Agent或提交Git。双层候选遍历与原Owner历史引用的实际规模成本、平台和线上联调仍UNKNOWN。
