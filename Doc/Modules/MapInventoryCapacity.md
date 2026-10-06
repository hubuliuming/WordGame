# 材料背包容量与拾取限制

返回[背包](Inventory.md)和[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的材料数量上限、服务端入包检查及 F/G/B 显示。人工清单归[运行入口](Runtime.md)，玩家档案归[资源与数据](DataResources.md)。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [CapacityConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryCapacityConfig.cs) / [ItemConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryCapacityItemConfig.cs) | inventoryCapacity 根及材料条目的严格 JSON DTO |
| [CapacityData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityData.cs) | 原地图根上的固定 Settings 与三条 Definition，无 GhostField |
| [CapacityUtility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryCapacityUtility.cs) | 按烘焙定义读取材料库存，F整批判定、G可接收量及定义查找；不修改库存或保存 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) / [校验](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) | 必填配置校验与固定 ECS 数据烘焙 |
| [统一 F](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs) / [采集](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherSystem.cs) | 植物预约前检查及完成前复核 |
| [G 拾取](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropPickupSystem.cs) | 最近目标选定后、准备保存候选前检查 |
| [F 采样](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudStateSystem.cs) / [G 采样](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudStateSystem.cs) | 复用同一服务端判定生成所属 NoSpace 状态 |
| [材料面板](MapInventoryPanel.md) / [F 提示](MapInteractionHud.md) / [G 提示](MapPickupHud.md) | 只读容量投影、总量/单种上限与空间不足提示 |
| [目标高亮](MapInteractionHighlight.md) / [资源状态](MapResourceStatusHud.md) | 兼容 NoSpace，继续识别原目标与可用资源状态 |

容量v19阶段接入四个普通 C# 脚本及正常导入 meta；等级接入归[升级](MapInventoryCapacityUpgrade.md)。没有新增 MonoBehaviour 挂载或可变背包；Scene/SubScene、Prefab、Animator、旧 meta、资源引用、包与构建设置保持。正式 Map 的 QFramework 背包/99 拆格仍属原链。

## 【FACT】JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为 schemaVersion=40/configRevision=43。新增必填 inventoryCapacity：

```json
"inventoryCapacity": {
  "enabled": true,
  "maxTotalQuantity": 300,
  "items": [
    { "itemId": "vitality_apple", "maxQuantity": 200 },
    { "itemId": "wood", "maxQuantity": 200 },
    { "itemId": "stone", "maxQuantity": 200 }
  ]
}
```

| 字段 | 当前默认值与约束 |
|---|---|
| enabled | 必填布尔 true；仅控制材料入包容量限制 |
| maxTotalQuantity | 必填正整数 300；三种材料合计上限 |
| items | 必填数组，恰好三个非 null 条目；顺序可变 |
| items[].itemId | vitality_apple、wood、stone 各一次；未知/重复 ID 拒绝 |
| items[].maxQuantity | 各为正整数 200；与总量上限同时生效，不要求小于总量上限 |
| interactionHud.noSpaceLabel | 新增必填 Not enough space；F 配置/Settings 各10字段 |
| pickupHud.noSpaceLabel | 新增必填 Not enough space；G 配置/Settings 各18字段 |
| inventoryPanel.capacityLabel / unlimitedLabel | 新增必填 Capacity / Unlimited；面板配置/Settings 当前各55字段，含36文案、两模式ID及一偏好文件ID；新增项归[排序筛选](MapInventoryListView.md)、[搜索](MapInventorySearch.md)与[本机偏好](MapInventoryPreferences.md) |

沿原严格 UTF-8、完整对象形状、字段/类型与未知/缺失/重复键校验；文案非空白、无控制字符、最多61个 UTF-8 字节。enabled=false 或显示关闭仍校验全部配置；旧地图 v1～v27 明确失败，没有补字段、来源回退或运行热重载。各端使用同版代码、配置并重新烘焙。

原地图根追加 CapacitySettings 的 Enabled(byte)/MaxTotalQuantity(int)，Definition 缓冲含 ItemId/ItemName(FixedString64Bytes)/MaxQuantity(int)，三条按原产出映射写入。配置通过正常 JSON/BuiltIn → 校验 → Map Baker 接入，不写玩家或世界档案。

## 【CURRENT STRATEGY】数量与接收规则

三种材料每件计1单位，仍按 ItemName 同名累计，不按格子/重量计量或拆分堆叠。小块肉、其他合法库存名称、Tools、金币及经验不计入材料容量；E 与原敌人小块肉奖励链保持。总量和加法使用 long，合法旧库存的三种 int 数量求和不会发生32位溢出；原提交数量仍为 int，checked 规则保持。

F植物仍整批接收或拒绝。G按[部分拾取](MapDropPartialPickup.md)开关计算：默认剩余总容量2而最近wood×3时接收2、原堆留1；关闭partialPickupEnabled时整堆拒绝。总量与单种余量同时约束，零余量或既有超限库存仍拒绝，不自动选择较远目标；原InventoryAlreadyOverCapacity、MaterialTotalCapacityExceeded、MaterialItemCapacityExceeded原因保持。

已有任一材料或总量超过当前配置上限时，全部受管材料的新入包均拒绝。固定 ID 加入/恢复不增加容量校验，不截断、不删除、不拒绝合法旧库存；已超限玩家档仍按完整v4写入/合法v1～v3内存迁移规则保存，等级保留。降低配置上限并重新烘焙也按此规则处理。制作、修理、使用、Single/All 丢弃仍沿原资格与事务，容量不单独阻止这些操作；库存恢复到总量和所有单种均不超限后，新 F/G 可重新尝试。

## 【CURRENT STRATEGY】服务端检查与提交顺序

统一 F 仍先按原跨类型距离/同距规则选最近有效目标。只有 Gather 植物在 TryBegin 之前读取当前权威库存并检查其整份 YieldQuantity；拒绝不建立预约，资源仍 Available。Tree/Mine 沿原工作预约及工具选择，不因背包满而拒绝砍伐/采矿；成功完成仍只生成地面掉落物，随后由 G 检查入包。

植物 Collecting 仍沿原资格/中断/完成时间。G 可在采集中入包，因此 Gather 完成时再次读取实际库存，检查通过才构造 PrepareReward 候选、预留缓冲并调用 SavePrepared。容量不足沿原 Cancel 清空进度/采集者并恢复 Available，不入包、不耗尽、不安排再生、不保存该次采集；没有自动重试或提前占用容量。

G 在原资格及最近 Landed/未到期目标选择后，先取得实际物品/数量和库存，再检查容量。拒绝发生在 EnsureCapacity、PrepareReward 与 SavePrepared 之前，原 DropId/ItemId/Quantity/Phase/ExpiresAt 保持；寿命仍正常推进，其他玩家仍可按原顺序竞争。本次可接收量大于0才沿原候选保存成功 → 同次提交库存/剩余Quantity，领空才Consumed；独立准备/保存失败继续沿原隔离链。

重复或负数的受管材料库存、无对应定义及非正入包数量属于数据错误，抛出原异常，由既有请求/资源/玩家采样边界记录并隔离，不伪装成普通满包；关闭容量时沿原结算数据校验与 checked 边界。客户端不扣减材料、提交容量或发送目标，全部显示关闭也不影响服务端检查。

## 【CURRENT STRATEGY】F/G/B 显示与生命周期

F 原四字段保持，Mode(byte)在 Hidden=0/Ready=1/Working=2 后追加 NoSpace=3。仅空闲、已选中的 Gather 目标接收失败时进入 NoSpace，Kind/PlacementIndex 保持该目标，ProgressPermille=0。工作期间继续 Working 与原进度，完成复核拒绝并释放后再投影空间不足。第一行 Not enough space，第二行 F Gather Apple；此状态第二行显示目标，原制作/修理反馈仍可在 B 页脚观察。Ready/Working 显示及输入发送保持。

G当前七字段，新增PickupQuantity；Mode保持Hidden=0/Ready=1/NoSpace=2；保留 DropId/ItemId/Quantity 与实际寿命投影。PickupQuantity=0时第一行显示G Not enough space 物品×地面数量；可领取部分时Ready显示本次可领量/地面量，第二行继续原到期/预警/永久提示。空间不足不延长寿命、不改变寿命色或追加拾取成功反馈。

NoSpace 的 F/G 圆环仍按原身份解析，分别沿 Ready 黄色与 G 蓝色；不换较远目标或依据客户端距离重选。资源状态优先识别原 F NoSpace 身份，并要求资源仍 Available；背包满不把植物标记为耗尽或占用。资源/工具/掉落 Ghost、资源状态四字段、保存所属三字段保持；当前19输入及等级/反馈归[升级](MapInventoryCapacityUpgrade.md)。

B 材料标题下显示 Capacity 当前总量/当前等级总上限，受管行显示数量/对应等级单种上限；Lv1为300/各200，Lv2为450/各300，Lv3为600/各400，未受管行保持原x数量。关闭容量后总量行显示 当前总量/Unlimited，材料行沿原数量显示。原只读 Snapshot 缓存等级与定义，上限在等级变化时刷新，即使数量未变也更新文字；非法条目仍逐项记录/跳过并禁用原按钮，不通过展示修正库存。增加一行滚动内容，原380×640、字号18/行高32、Drop/All/制作/修理与鼠标隔离保持。

F/G/B 显示开关仍独立。无本地玩家、死亡、断线、玩家/地图源变化、World/Scene停止时沿原绑定清显示、投影和未提交按钮请求；没有新监听、客户端计时器或保存入口。提示仅代表最近所属快照，延迟或同 tick 其他入包可使显示与实际按键结果不同，服务端完成检查为准。F预约/完成、G及F/G采样均读取个人CapacityLevel；升级关闭保留已有等级上限，容量关闭显示Unlimited并禁用付费升级，永久等级仍保留。

## 【CURRENT STRATEGY】配方分类与搜索关联

当前v40/revision43的[配方搜索](MapInventoryRecipeSearch.md)在原分类上按配置名称/操作文案匹配七项配方，复用独立搜索实例与焦点隔离；单项显隐及高度、文本变化清七请求/待确认归专题。材料与工具状态、原输入/事务/存档链保持；关键词仅本绑定内存，不入偏好v3。本阶段待人工，分类已通过仍限v39/revision42原清单，其他旧通过保持原范围。

## 【KNOWN ISSUES】静态核对与人工边界

正常 Unity 编译无 C# Error，四个新脚本/meta 与配置/Settings、原所属字段已核对。Forest/Grassland各覆盖 Json默认、BuiltIn默认、关闭容量、自定义150总量/71苹果/83木材/97石材及调整定义顺序/四文案、单独关闭F/G/B、全部显示关闭与容量关闭组合，共18次隔离 Editor Bake。新容量2字段Settings/3条三字段Definition、F10/G18/B30配置与Settings、玩家初值及既有工具/掉落/持久化参数符合；Json/BuiltIn等价。

与本阶段修改前快照比较，所有区块/格子/装饰位置朝向/障碍与资源布局签名保持，默认森林/草原矿点20/18、树木89/53、采集点36/38、阻挡109/71保持。Bake Console前后均[0 Error,8 Warning,74 Log]，无新增Bake警告；当时含六条既有运行警告和未修改PEListener/DOTween的两条编译警告，末次元数据核对Console为[0,6,74]。主场景干净，临时World/Scene已释放。

用户已确认本阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定通过，限 CombatPrototypeNetCode、v19/revision22 及[运行入口](Runtime.md)材料容量十六项清单；人工结论来自用户反馈。旧 v18/revision21 的 F5/保存提示与其余历史237项保持各自版本/清单。未实际触发的精确容量/整批/同 tick/G并发采集中满包、旧超限恢复、异常库存/保存失败、多玩家/晚加入/生命周期、实际排版/字体仍为 UNKNOWN；性能/带宽、平台与线上未验证。

v19容量阶段玩家/世界格式保持；当前玩家v4/两类等级规则归[工具升级](MapGatherToolUpgrade.md)，世界格式/路径、资源/掉落寿命/再生及原保存事务保持；跨文件原子一致、防重复、同槽并发和保存成功后意外 ECS 故障恢复仍为原 UNKNOWN。AI只执行编译、配置/元数据检查与隔离 Editor Bake，未执行 GamePlayer/PlayMode、游戏/显示系统或GUI回调、逻辑单元测试、命令行构建、发布、采样、图片检查或真实存档读写，未创建子Agent或提交Git。

## 【FACT】合并后的接收量

v26/revision29的[地面合并](MapDropMerge.md)只改原地面数量/期限；G按[部分拾取](MapDropPartialPickup.md)开关读取当前最近堆：默认领取容量可容纳的部分，零余量NoSpace；关闭开关恢复完整Quantity判定，不改选较远物。地面合并上限99不改inventoryCapacity/等级定义，也不拒绝、钳制或拆分已有大堆；合并本身不修改库存、容量Level或保存玩家。合并链静态及用户人工通过限v26/revision29十六项，部分拾取用户人工通过限v27/revision30十六项；未触发独立用例UNKNOWN；旧容量/升级通过保持原版本/清单。

## 【FACT】列表筛选与完整容量统计

当前v40/revision43的[排序筛选](MapInventoryListView.md)只改变B材料列表可见行/顺序。Snapshot仍先校验全部原库存并统计受管苹果/木材/石材总量及当前等级上限，筛选资源时隐藏的苹果仍占容量；非法独立库存条目照原记录/跳过并置InventoryValid=false，筛选不能放开制作/修理/丢弃/升级资格。服务器F/G接收、部分拾取与容量等级/玩家保存链保持；静态及用户人工通过限v28/revision31十六项，人工结论来自用户反馈；未触发的独立用例UNKNOWN。

当前[搜索](MapInventorySearch.md)与分类取交集后再排序，仍只投影B材料可见行；完整Snapshot先校验全部库存并统计容量，隐藏材料继续参与受管总量与原配方/业务资格。搜索静态及用户人工GamePlayer通过，限v29/revision32及运行入口十六项，结论来自用户反馈；未实际触发的独立用例UNKNOWN。
