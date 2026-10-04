# 背包展示与道具使用

[返回总导航](../AI_Understanding.md)。本页负责格子生成、背包事件与场景道具；物品数据字段和资源映射归[资源与数据](DataResources.md)。

## 入口文件

- [KnapsackControl.cs](../../Assets/Scripts/Controller/UIController/KnapsackControl.cs)：按 PlayerModel.GoodsDict 生成格子。
- [ItemBase.cs](../../Assets/Scripts/Item/ItemBase.cs)：读取场景道具配置并注册 Btn 点击。
- [ItemData.cs](../../Assets/Scripts/Item/ItemData.cs)：独立的全局 Serializable struct，字段名称、类型与 JSON 形状不变。
- [UseItemCommand.cs](../../Assets/Scripts/Command/UseItemCommand.cs)：AbstractCommand<bool>，返回 PlayerEventSystem.ChangeAll 的成功结果。
- [DetailInform.cs](../../Assets/Scripts/Controller/UIController/DetailInform.cs)：详情文字与 UseGoods 监听代码。
- [Msg.cs](../../Assets/Scripts/Msg/Msg.cs)：物品名和 UseGoods 字符串常量。
- [Goods.prefab](../../Assets/Resources/Prefabs/Goods.prefab)：通用背包格子；根节点含按钮，直接子节点为 TxtName、TxtNum。

## 【FACT】初始背包生成

MapCanvasControl.Start 的脚本顺序为先绑定场景对象池，再显式调用 KnapsackControl.OnStart；当前场景绑定来源及静态状态见[运行入口](Runtime.md)。背包获取 PlayerModel 与已注册的 FactoryUISystem 实例，确认必需 contextRect 存在后遍历 GoodsDict.Keys：

1. 跳过键 `Coin`。
2. 对当前物品数量，循环生成数量为 99 的满格，直到剩余数量不大于 99。
3. 零数量不创建格子；负数量记录错误并跳过该条目。
4. 使用缓存的 FactoryUISystem 实例调用 `Get(Msg.ItemName.Goods)` 取格子，将其父对象设为 contextRect，`worldPositionStays=false`。
5. 向 TxtNum 和 TxtName 的 Text 写入数量与物品名，并向格子 Button 添加点击监听。

场景中的 contextRect 指向 Content（fileID 1099784055），绑定来源见[运行入口](Runtime.md)。物品遍历没有显式排序，不能据字典枚举写成固定显示顺序。

CreateGrid 在单格边界隔离借出、节点获取和初始化异常，记录物品名、数量、Goods 资源路径与原异常；已借出的半成品由对象池 Discard 销毁并移除登记，清理错误单独记录，随后继续其他格子。只有成功格子计入 gridNum；缺少整个批次必需的 Content 容器时直接暴露错误。现有 99 拆格规则和容器公式保持原状。

## 【CURRENT STRATEGY】格子与容器参数

| 常量或计算 | 当前值/行为 |
|---|---|
| Row | 6 |
| Column | 10 |
| MaxGirdNum | 99，保留源码拼写 |
| needRow | `gridNum / Column + 1`，使用整数除法 |
| 容器扩展条件 | needRow > Row |
| 扩展高度 | 每个新增行增加 150 到 contextRect.sizeDelta.y |

需要行数按有效格子数向上取整；容器最多按 `max(0, needRow - Row)` 扩展行数。以上仅为脚本计算，不等于已验收的实际视觉排版。

## 【FACT】两条使用路径

### 场景中的活力苹果

`ItemBase.Init(itemName) → RecoverItem JSON 同名条目 → Btn.onClick → SendCommand<bool>(UseItemCommand(data)) → PlayerEventSystem.ChangeAll(data) → 成功后 gameObject.Release()`。

ItemBase 是 UIBase / IController / IBaseLife 实现。首次创建时读入的数据被按钮回调捕获；取出已存在池对象时只激活，不重新读取该 JSON。

GameObject.Release 扩展通过 Game.Interface 获取当前已注册的 FactoryUISystem 实例进行归还，池生命周期见[资源与数据](DataResources.md)。

该路径统一校验与提交玩家属性，成功后才回收场景道具对象；校验或保存失败时不回收。没有扣除 PlayerModel.GoodsDict 中同名物品数量。具体属性写入规则见[玩家](Player.md)。

### 背包格子与详情事件

格子点击创建一个全默认值的 `new ItemData()`，再通过 `StringEventSystem.Global.Send(Msg.Register.UseGoods, data)` 发送事件，没有按 goodName 查询 RecoverItem。

DetailInform.OnStart 将 Inform 写到子节点 Text，并注册全局 UseGoods 监听；监听回调当前不应用属性或扣减库存。Release/OnDestroy 注销监听。注册监听不是由 BtnUse 点击触发。

Map 中存在 DetailInform 脚本引用（GUID `8cdd752725b5a6f43907b68aaf6469e3`），但在已检索的 Scripts / Framework / Test 中没有发现显式调用其 OnStart 的位置；YMonoBehaviour 不自动桥接该方法，见[运行入口](Runtime.md)。

## 【CURRENT STRATEGY】背包展示刷新

KnapsackControl.OnStart 缓存当前 PlayerModel 与 FactoryUISystem，记录 Content 初始高度，并订阅 `Msg.Register.InventoryChanged`。首次初始化及每次库存事件均调用 UpdateGoods：先移除并归还旧格子，再按 GoodsDict 过滤 Coin、零数量和负数量，依照 99 上限拆格并重新生成；容器高度按有效格子数恢复到初始高度或扩展行数。Release/OnDestroy 注销事件、归还格子并恢复容器高度，重复 OnStart 不重复注册监听。

## 【KNOWN ISSUES】

- **正式 Map 背包使用未完成**：格子未构造真实物品效果，DetailInform 监听未执行属性变更，亦未建立扣减库存的调用。
- **展示数量边界**：旧存档中的负库存只记录错误并跳过展示；新的普通库存修改已拒绝负结果并移除合法归零条目，零增量不变。模型的旧数据报错与写入规则见[玩家](Player.md)。
- **正式配置覆盖范围**：当前正式玩家数据含小块肉，但恢复道具表只有馒头、活力苹果。正式 Map 中小块肉是否可使用及其效果为 `UNKNOWN`；第 7A 网络原型数值不改变该配置。

## 未知项与验收状态

`UNKNOWN`：正式 Map 的背包排序、容量、道具消耗规则、详情窗口完整交互，以及是否允许堆叠上限之外的业务例外。第 4 阶段已完成 UI 监听、刷新和释放代码的静态落地；人工 GamePlayer 交互验收仍由主线程确认，未读取界面图片。

## 【FACT】第 4C 阶段网络玩家本局背包

[CombatPrototypeInventoryItem.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeInventoryItem.cs) 是每名网络玩家 Ghost 独立持有的动态缓冲元素，`ItemName` 为 `FixedString64Bytes`，`Quantity` 为 `int`，两个字段均标注 GhostField。现有玩家 Baker 添加空缓冲；第 4D 无档新玩家为空库存，有档玩家在服务端生成时按固定 ID 恢复，沿原 Ghost 同步；不导入正式 PlayerModel 的库存或存档。

## 【CURRENT STRATEGY】第 4C 阶段物品入包与同步

服务端沿既有击杀奖励链直接入包：按 ItemName 查找目标条目，同名数量累加，名称不存在时新增一个条目；不创建第二份可变 ECS 背包，也不套用正式 UI 的 99 拆格规则。当前敌人物品奖励为 `小块肉 ×1`，名称复用 `Code_01.Msg.ItemName.小块肉`。第 4D 将完整候选库存投影为 JSON 条目，与金币/经验一起保存成功后同次提交，具体失败与归属规则见[战斗](Combat.md)。

客户端仅接收背包 Ghost 状态；原日志入口每 2 秒记录背包条目数及各条目的名称、数量。固定 ID 库存持久化格式与加载校验由[资源与数据](DataResources.md)维护，加入恢复由[玩家](Player.md)维护。正式背包 UI 和账号保持原边界；网络原型物品使用与恢复规则见本页第 7A 节，动态掉落与 G 拾取归[掉落与拾取](MapDrops.md)。

## 【KNOWN ISSUES】第 4C 阶段本局背包验收

缓冲字段与生成的 Ghost Serializer/Snapshot 已由 Unity 编译并加载，实际 SubScene 产物确认玩家初始空背包与敌人小块肉 1 配置；用户已确认第 4C 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4C 阶段通过。背包验收仅覆盖当前独立网络原型的双玩家空初始库存、独立累计与双端同步、同名合并、金币/经验/小块肉三项奖励一致、离线不补发和重新加入清空；非致命无奖、每敌人一次、多目标分别结算及原体力、移动、伤害和死亡显示回归范围见[运行入口](Runtime.md)。第 4B 及更早通过范围保持，不扩展为正式背包 UI、道具使用、存档、规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【KNOWN ISSUES】第 4D 阶段库存恢复验收

候选库存序列化、全部校验后恢复及保存成功后入包的调用点已静态核对，原背包 Ghost 字段保持；用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。库存范围覆盖多人独立保存、恢复后同名继续累计、重启恢复、坏库存拒绝加入及写盘失败三项均不到账，完整边界见[运行入口](Runtime.md)。第 4C 当时空背包重入的通过结论保持其历史范围；AI 未运行逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 7A 阶段网络物品使用

[CombatPrototypePlayerInput.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 增加 InputEvent UseItem；客户端 E 键按下当帧只给 GhostOwnerIsLocal 写入事件，沿原 NetCode 输入缓冲发送。独立 [CombatPrototypeItemUseSystem.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeItemUseSystem.cs) 在服务端预测模拟组中处理，排在敌人空间索引之后、玩家近战之前，使用现有玩家库存和体力组件。

当前原型仅使用 `Code_01.Msg.ItemName.小块肉`：每次接受消耗 1 份，恢复最多 30 点 CurrentPower，截到 UpperPower。该数值属于已确认的第 7A 网络原型规则；正式 Map 的 RecoverItem 配置、UseItemCommand 和详情路径保持原状。

## 【CURRENT STRATEGY】第 7A 阶段准入与消费提交

服务端沿 Connected、NetworkStreamInGame、未请求断线的连接 CommandTarget 确认当前启用 Simulate 的玩家，并核对 GhostOwner 与连接 NetworkId。死亡、近战阶段不是 Ready、体力已满、没有小块肉或数量不足 1 时拒绝，不消耗物品、不恢复体力；客户端不按生命或库存快照过滤输入。

资格成立后先取得必需体力引用、库存、身份和金币/经验，准备扣除 1 后的完整库存候选及截断后的体力。数量归零的条目从候选 JSON 和最终 ECS 缓冲中移除，其余物品及顺序保持；不创建第二份可变 ECS 背包。候选保持当前金币/经验，并复用 PrepareItemConsumption → SavePrepared；保存成功返回后，用预先取得的引用同次提交库存和体力。失败时该使用操作不改变库存或体力，旧正式档保留，日志包含连接、玩家、NetworkId、阶段和原始异常，继续处理其他玩家；没有自动重试。存档字段与替换规则归[资源与数据](DataResources.md)。

使用发生在玩家近战之前：进入该系统时为 Ready 的有效 E 请求可先恢复体力，再供同 tick 的攻击准入使用；满体力请求先拒绝，随后攻击扣费不改变该次拒绝结果。死亡 E 请求也先拒绝，末尾 R 复活不重新处理本 tick 的 E。体力不持久化，不改变生命、攻击序号、金币/经验或原奖励流程；网络状态沿现有 Ghost 同步。

## 【KNOWN ISSUES】第 7A 阶段物品使用验收

输入、新系统、消费候选和顺序特性已编译并静态核对；用户已确认第 7A 人工 GamePlayer 验收通过，主线程结合既有静态核对与用户反馈判定该阶段通过。范围覆盖消费/恢复、拒绝条件、双玩家同步、保存失败和重连恢复，完整边界见[运行入口](Runtime.md)。同步序列化/写盘仍会阻塞服务端，实际耗时和规模性能未测量，观察范围见[性能基线](Performance.md)。正式背包 UI 和其他网络物品效果保持各自既有未知范围；动态掉落与 G 拾取的当前边界见[掉落与拾取](MapDrops.md)。

## 【FACT】战斗地图第四阶段采集输入与产出

现有 [CombatPrototypePlayerInput.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 保留 InputEvent Gather，F 统一请求资源交互，客户端 F 单次按下只给 GhostOwnerIsLocal 写入请求。服务端 [CombatPrototypeMapGatherSystem.cs](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherSystem.cs) 在统一 F 入口之后、复活之前维护采集预约；客户端不提交采集目标或发放物品。默认 gather_apple 的交互中心距离为 X/Z 2 米、时长 1 秒，产出 vitality_apple ×1；[产出映射](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapYieldItemResolver.cs) 当前允许 vitality_apple/wood/stone，分别映射活力苹果/木材/石材，采集植物仍只产苹果，未知 ID 在配置校验时报错。E 仍只消费小块肉，苹果未接入使用效果或正式 UI。

## 【CURRENT STRATEGY】服务端预约、中断与入包保存

统一入口沿 Connected、NetworkStreamInGame、未请求断线的 CommandTarget 验证玩家归属/Simulate、存活、无攻击且近战 Ready、有限零 Move；跨采集点/树木/矿点选择最近有效目标并调用对应 TryBegin。统一距离/同距/多人顺序规则归[战斗地图](Map.md)。采集点进入 Collecting 并记录玩家、HitSequence 和完成时间；重复 F 不重置、不切换，本 tick 完成/取消后不再开新预约。

统一入口先记录本 tick 已有交互，采集系统随后维护预约。移动输入、攻击请求或非 Ready、受击序号改变、死亡、超距、断线/目标失效均取消并释放为 Available，不发物品。期间采集者不强制锁定移动或攻击，取消不恢复体力；完成时间到达仍须先满足当次资格与范围。Available/Collecting 显示，Depleted 由客户端按 Ghost 状态隐藏，没有进度 UI 或自动重试；耗尽后的再生沿下述独立服务端计时链。

完成前取得库存与状态引用；按既有 ItemName 合并或新增，检查数量溢出并预留新增缓冲容量。PrepareReward 生成保持当前金币/经验的完整库存候选，SavePrepared 成功返回后同次提交库存、清除预约并设置 Depleted，同时按配置记录再生期限。准备/保存失败记录地图、布置索引、NetworkId、物品、阶段与原异常，库存数量及资源耗尽不提交，释放预约并继续其他点；玩家须重新按 F。没有修改原奖励/消费系统或存档格式，地图耗尽状态及再生期限不写盘；客户端背包仍沿原 Ghost 缓冲同步及每 2 秒日志查看。

## 【CURRENT STRATEGY】采集物原点再生

默认 gather_apple 启用 regrowEnabled=true、regrowSeconds=600；再生字段形状保持；当前地图契约为 schemaVersion=6、默认 configRevision=9，包含 drops/treeHarvest/mining 段。Map Baker → GatherSpawnSystem 将开关和间隔送入仅服务端的 GatherConfig；仅在采集保存成功后，GatherProgress.RegrowAt 写为该次 Server World 模拟时间加间隔，Collector/StartHitSequence/FinishAt 清空。取消或准备/保存失败清空进度、释放预约，不发物品、不安排再生，也不自动重试。

[GatherRegrowSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherRegrowSystem.cs) 在 PredictedSimulationSystemGroup 的 GatherSystem 之后、PlayerRespawnSystem 之前恢复到期 Depleted 点为 Available，并清空期限及采集者。原实体、位置和 PlacementIndex 保持，客户端复用原 Ghost 状态和显示系统恢复显示；再生不入包、不保存库存，也不自动开始下一次采集。玩家须重新按 F，完整资格/预约/保存规则继续生效；regrowEnabled=false 时点保持本局耗尽。计时与耗尽仅保留当前 Server World，停止重进生成新的 Available 点，已保存库存仍沿原 v1 固定 ID 恢复。

## 【KNOWN ISSUES】战斗地图第四阶段采集验收

输入及新系统正常编译，状态 Ghost Serializer、服务端配置/进度特性、系统顺序、两种模板隔离烘焙与引用已静态核对。用户已确认第四阶段人工 GamePlayer 验收通过，主线程结合静态核对与用户反馈判定该阶段通过；范围来自第四阶段 v3/revision=3、再生关闭的版本，含 F 目标选择/计时、中断与争抢、保存失败不发物品、不耗尽并释放预约、耗尽跨端隐藏、晚加入和重启库存恢复，完整边界归 [运行入口](Runtime.md)。人工结论来自用户反馈，AI 未运行游戏系统、PlayMode 或逻辑单元测试。玩家 v1 存档保留苹果，地图耗尽及再生期限只保留本局；停止服务端再进入会重新生成采集点。第五阶段正常编译、参数烘焙和仅服务端字段已静态核对；用户已确认第五阶段人工 GamePlayer 通过，主线程结合静态核对与用户反馈判定该阶段通过，范围限至少两轮原点再生、新 F 再采集、跨端恢复/晚加入、再生关闭及失败/取消不安排再生，完整边界归 [运行入口](Runtime.md)。写盘仍同步阻塞服务端，性能未测量。

## 【FACT】地图动态掉落与 G 拾取入包

敌人首次死亡额外生成活力苹果 ×1 的插值 Ghost；原金币/经验/小块肉统一击杀奖励与保存链保持。服务端 G 单次请求仅选择本人 X/Z 2 米内最近 Landed 且未到期的掉落，同距取较小 DropId，同次更新按 NetworkId 升序处理；在线当前玩家须存活、静止且近战 Ready，没有攻击请求。任意合格玩家均可拾取，没有击杀者专属所有权。

拾取复用 PrepareReward → SavePrepared，保持当前金币/经验并合并同名库存；成功保存后同次提交库存与 Consumed，后续请求不能重复发放。保存失败不改库存数量或未到期掉落，继续其他请求，恢复后须新 G；到期仍按配置清理。世界掉落仅保留当前 Server World，已入包苹果沿原 v1 固定 ID 保存。苹果没有新增使用效果或 UI；F 预约/耗尽/600 秒再生保持。配置、运动、Ghost 和清理职责归[掉落与拾取](MapDrops.md)。用户已确认第六阶段人工 GamePlayer 通过，主线程结合静态核对与用户反馈判定该阶段通过；库存范围限保存成功入包、同名累计、争抢一次提交、保存失败保留及固定 ID 恢复，完整边界归[运行入口](Runtime.md)第六阶段九项清单。

## 【FACT】砍伐木材与库存提交

F 统一选中树木后由 TreeHarvest 预约/计时，默认 2 米/2 秒后生成一份 wood ×3 地面掉落，砍倒时不直接入包。DropPickup 改按目标实际 ItemId 解析名称；vitality_apple/wood 分别映射活力苹果/Msg.ItemName.木材，同名 checked 累加，仍先 PrepareReward → SavePrepared，再提交库存及 Consumed。原存储类/v1 格式、金币/经验、E 和玩家 Prefab 保持；树木和未拾取木材不保存，已入包木材随固定 ID 恢复。砍伐规则归[树木砍伐](MapTreeHarvest.md)，拾取规则归[掉落与拾取](MapDrops.md)。用户已确认第七阶段人工 GamePlayer 通过，主线程结合静态核对判定该阶段通过；库存范围限木材/苹果分别入包、累计/争抢、保存失败保留和固定 ID 恢复，完整边界归[运行入口](Runtime.md)第七阶段十项清单。人工结论来自用户反馈，未实际触发的独立用例仍为 UNKNOWN，原通过边界保持。

## 【FACT】树木原点再生的库存边界

默认 tree_normal 成功砍倒后 600 秒恢复，原点有存活玩家/敌人时延迟；恢复本身不创建木材、不修改库存、不调用 SavePrepared。新 F 选中树木并完成才再次生成 wood ×3，沿原 G 保存成功后入包；原 F 采集物独立再生/保存、苹果/木材名称映射、v1 存档和 E 使用保持。完整规则归[树木砍伐](MapTreeHarvest.md)；用户已确认第八阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定该阶段通过；库存范围限再生不发物品、新 H 木材/G 提交、原 F/库存回归，完整边界归[运行入口](Runtime.md)第八阶段八项清单。人工结论来自用户反馈，未触发独立失败用例仍为 UNKNOWN，历史通过范围保持。

## 【FACT】采矿石材与库存提交

统一 F 选中矿点后，服务端默认 2 米/3 秒完成只生成 stone ×3 地面掉落，完成时不直接入包。stone 显式映射新增 Msg.ItemName.石材；G 继续按实际 ItemId 调用原 PrepareReward → SavePrepared，再提交同名库存及 Consumed，原金币/经验、v1 格式和存储类保持。保存失败不改库存或未到期掉落，恢复后须新 G；已入包石材随固定 ID 库存恢复，矿点/未拾取石材只保留本局。石材没有新增使用效果或 UI，E 仍只用小块肉。规则归[采矿](MapMining.md)/[掉落](MapDrops.md)，第九阶段人工库存/保存/恢复验收为 UNKNOWN，归[运行入口](Runtime.md)。

矿点默认耗尽提交后 600 秒原点再生，占位等待；再生不发石材、不提交库存或存档，再次产出须新 F 完成后 G 保存入包。矿点期限/历史只属于当前 Server World，已入包石材仍沿原 v1 固定 ID 恢复。规则归[采矿](MapMining.md)，新阶段人工库存回归为 UNKNOWN，归[运行入口](Runtime.md)。

当前采集、砍树、采矿共用 F，统一规则归[战斗地图](Map.md)。原第四至第八阶段人工通过保留其旧版本/清单，新增跨类型交互及保存/产出回归已获用户人工通过反馈，结论限[运行入口](Runtime.md)统一 F 八项清单，未触发的独立失败用例仍为 UNKNOWN。
