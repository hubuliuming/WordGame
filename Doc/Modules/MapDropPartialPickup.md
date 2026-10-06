# 掉落物按背包余量部分拾取

返回[地图](Map.md)、[掉落](MapDrops.md)、[容量](MapInventoryCapacity.md)、[G提示](MapPickupHud.md)、[拾取结果](MapPickupFeedbackHud.md)、[掉落存档](MapDropPersistence.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode；Forest/Grassland Json与BuiltIn当前schemaVersion=31/configRevision=34。部分拾取v27/revision30的代码/配置/烘焙/Serializer静态核对通过，用户已确认该阶段人工GamePlayer通过，范围见运行入口十六项。旧合并用户通过限v26/revision29十六项，旧G结果限v25/revision28十六项，其余旧范围保持。

## 【FACT】文件与配置

| 职责 | 实际文件 |
|---|---|
| bool DTO与Server Settings | [MapDropConfig](../../Assets/Scripts/CombatPrototype/Map/MapDropConfig.cs)、[DropData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropData.cs) |
| 版本、默认值与烘焙 | [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 共用只读接收数量 | [CapacityUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUtility.cs) |
| 实际G提交 | [DropPickupSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs) |
| 七字段所属快照与采样 | [PickupHudData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudData.cs)、[StateSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudStateSystem.cs) |
| 目标文字与缓存/合法性 | [PickupHudClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudClient.cs) |
| 显式来源 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

仅十个现有脚本与两地图JSON接入。没有新增脚本、组件类型、输入或Scene/SubScene/Prefab/Animator/meta/资源/包/构建配置；原绑定整体传递G状态，Player Baker继续添加原Hidden默认值。原植物、树木/矿点/工具、DropMotion/Spawn/TargetSelector/Merge/Cleanup、玩家SaveStore与世界保存/恢复代码保持。

原必填drops增加一项，以下仅为新字段摘录，其他十一字段仍完整必填：

```json
"drops": {
  "partialPickupEnabled": true
}
```

strict bool，Json/BuiltIn默认true；Baker写原DropSettings.PartialPickupEnabled(byte)，Settings共14字段、PrefabType.Server且0 GhostField。开关独立于敌人新掉落drops.enabled、inventoryDrop、dropMerge、容量付费升级及所有显示开关。false恢复原整堆接收；inventoryCapacity.enabled=false时两种开关均沿原整堆接收及checked边界。

原Reader按DTO严格检查UTF-8/完整形状、缺失/null/重复或未知字段、标量类型；当前仅schema30，revision/seed仍须正数。旧v1～v30拒绝，关闭仍验证，无默认补齐/来源回退/运行热重载。正常导入/烘焙后生效，各端须同版代码/配置并重新烘焙。寿命、产出/丢弃数量、合并默认0.8米/99份/0.2秒及其他原字段保持。

## 【CURRENT STRATEGY】容量与本次数量

GetPickupQuantity供实际G与服务端G提示共用；先调用逐字保持的GetRejection，原F预约/完成仍走整批规则。整堆可接收时直接返回地面Quantity；开关关闭或任一既有受管材料/总量已超限时返回0及原拒绝原因。只有开关开启且整堆因余量不足被拒绝时再扫描已校验的库存，计算：

本次量=min(地面Quantity，总上限-当前总量，当前物品上限-当前物品量)。

使用本人CapacityLevel：Lv1读取原定义，Lv2/3按Level/ItemId读取原升级定义；升级关闭不重置已得等级。三种材料每件1单位，总量和差值使用long，本次量为不超过原正int堆量的int；没有新分配或可变背包。正余量接收对应部分，零余量保留原NoSpace原因。重复/负库存、非正来料、非法等级或缺定义沿原异常边界暴露，不修正、补造或伪装空间不足；容量关闭仍沿原接收/checked行为。

例如wood×10、总余量3且木材余量8，本次3、剩余7；总余量8但木材余量2，本次2。已有其他材料超单种上限时，即使木材仍有余量也拒绝全部新增受管入包，原合法旧库存恢复/丢弃/制作/修理规则保持。

## 【CURRENT STRATEGY】G选择、保存与提交

沿原在线归属、存活/静止/近战Ready资格和NetworkId升序。每次G重新按X/Z距离与精确同距小DropId选择最近Landed且未到期物；不上传客户端目标、按提示量结算、自动改选较远物或持续按住自动重试。Prepared/Airborne/Consumed/到期物沿原排除，原2米距离保持。

容量计算本次量后，在保存前checked准备原堆减本次量、同名库存加本次量，预留新增库存缓冲并构造原PrepareReward候选。候选保留金币/经验/Tools/CapacityLevel；SavePrepared正常返回后同次提交库存和原堆余量。余量>0只写原Quantity，保持Landed、DropId/ItemId/位置/旋转/缩放及ExpiresAt/StartedAt；余量=0沿原Consumed清理，不建立新拆分实体。成功日志含实际增量、库存累计和remainingGroundQuantity，原四字段成功反馈携带实际领取量。

准备或保存失败时原库存和地面量/身份/期限保持，按原请求边界记录stage/map/DropId/NetworkId/player/itemId/异常并继续其他玩家；恢复后须新G。SavePrepared返回后发生意外部分ECS提交异常时仍只沿原日志/UNKNOWN，不保证回滚或追加保存。反馈写入保持独立隔离；完整提交结束前不发布成功。

多人可先后从同编号剩余堆领取不同部分，后续玩家读取前一成功后的真实余量并重新计算本人容量；已经领取的数量不能再次发放。原采集预约期间允许G、同tick其他输入的旧资格/优先级保持，不新增工作预约或材料预占。

## 【FACT】G快照与显示

CombatPrototypeMapPickupHudState仍SendToOwner，Mode/DropId/ItemId/Quantity/LifetimeMode/RemainingSeconds六字段保持，追加int PickupQuantity，共七个GhostField。Quantity仍为地面完整量；Ready的PickupQuantity为1～Quantity，NoSpace/Hidden为0，Hidden其他字段亦原零/空。Mode仍Hidden=0/Ready=1/NoSpace=2，没有新枚举。

StateSystem采样实际库存/等级及新开关，和G共用GetPickupQuantity。字段变化才提交，比较包含PickupQuantity，因此地面量未变而容量/等级改变时也更新。提示仅属当前快照，真实G仍以服务端重新选择/计算为准；显示或高亮单独关闭、全部关闭、死亡/断线/源及玩家变化、停止/Reset继续沿原Hidden与缓存释放。

全量Ready仍“G  Pick up  Wood ×10”；部分Ready为“G  Pick up  Wood ×3/10”，分子本次可领、分母地面量；零余量NoSpace保留原地面量、文案/寿命。原标签、400×84/字号20/底168、寿命第二行和高亮身份保持；优先级仍为NoSpace目标>结果>Ready目标。成功结果仍用本次正增量暂替目标，到期恢复当前目标。客户端检查新字段范围及Mode一致性，异常保持原明确报错/收起，不默认补量；缓存比较追加PickupQuantity。AI未运行GUI绘制；未实际触发的字形/排版及网络时序独立用例仍UNKNOWN。

## 【FACT】剩余堆、合并与存档

剩余堆继续沿原寿命到期，不重置期限或刷新600秒；仍参与原同ItemId整份合并规则，之后可被合入其他编号或改变Quantity，有限寿命仍取最早期限。合并本身不部分转移，新拾取也不按合并上限拆分大堆。原DropGhost四字段同步剩余量，Consumed沿原移除。

原世界快照已比较Quantity，成功部分领取后的剩余量沿原保存点写入；领空Consumed排除。世界v2根九/掉落八字段、LastDropId空号、路径/资源签名与离线暂停保持，恢复仍原编号/数量/余时，Ready后再参与合并。玩家v4/Tools/容量等级格式与原保存方式保持；世界写失败保留旧正式档而不回滚本局拾取。玩家与世界分别保存，跨文件一致/防重复、同槽并发及异常中断恢复仍原UNKNOWN。

## 【KNOWN ISSUES】静态与人工边界

部分拾取v27阶段正常Unity脚本编译完成，当时Console无Error；初始[0 Error,2 Warning,3 Log]，编译后[0,5,3]，隔离Bake前后同[0,5,3]。新增记录为未修改PEListener/DOTween编译警告与MCP WebSocket警告，原Package/Input Manager警告保留；未清空Console，没有本阶段JSON导入错误。

部分拾取v27阶段258份非法配置全部拒绝（每地图129），42组合法读取通过（每地图21）：新增bool缺失/null/错形状/字符串/数字/非有限/重复未知键、其他功能关闭仍必填、drops根错误、旧v1～v26及原合并契约；合法含Json/BuiltIn等价、部分关闭/容量关闭/两者关闭、总13及苹果5木7石9、各上限1、定义/等级顺序改变、掉落/保存/显示/工具关闭与永久/合并数值边界。只验证配置读取，不调用接收数量逻辑作为单元测试。

部分拾取v27阶段两地图各20次，共40次隔离Editor Bake通过：默认Json/BuiltIn、部分/容量两类开关及组合、合并/新产出/丢弃/世界与掉落保存关闭、永久新物、G文字/高亮通道/结果/全部显示关闭、寿命关闭、自定义与最小容量、定义/等级换序。新byte开关映射及PickupQuantity=0初值、原所有配置/反馈/Prefab引用、完整布局与资源签名均一致；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。只读源SubScene，临时TextAsset/克隆/Editor场景/World/BlobAssetStore释放，主场景干净、未Play、一场景三根对象。

实际生成Serializer与Snapshot含七字段、7 mask bits、Marshal结构88字节、SendToOwner；编译生成复制/预测/序列化/反序列化入口存在，未调用实际收发函数。输入19、DropGhost4、Tools3、F4/资源4/世界保存3/F失败2/F结果3/G结果4，玩家v4根7/Tools项3与世界v2根9/掉落项8保持。

部分拾取v27阶段原371项人工内容/编号逐字保留，追加该阶段16项后共387项，归[运行入口](Runtime.md)。用户已确认本阶段人工GamePlayer通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v27/revision30及十六项清单，人工结论来自用户反馈；旧合并/v26与拾取结果/v25用户通过保持原十六项及其他旧范围。未实际触发的独立数量/容量/等级/距离临界、保存失败/异常隔离、合并/寿命/恢复、多玩家/延迟/预测回放、展示/生命周期用例，以及意外ECS恢复、跨文件事务、同槽并发、性能/平台/线上仍UNKNOWN。AI未执行GetPickupQuantity或G/HUD/GUI系统、GamePlayer/PlayMode、逻辑单元测试、真实玩家/世界存档业务I/O、命令行构建/发布、采样或图片，未创建子Agent/提交Git。
