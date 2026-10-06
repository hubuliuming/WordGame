# 玩家模型、属性与刷新

[返回总导航](../AI_Understanding.md)。本页负责玩家状态的控制逻辑；磁盘 JSON 数值归[资源与数据](DataResources.md)，战斗和使用道具的触发方式归各自模块。

## 入口文件

- [PlayerData.cs](../../Assets/Scripts/Features/Player/Data/PlayerData.cs)：数据结构、下限常量。
- [PlayerModel.cs](../../Assets/Scripts/Features/Player/Data/PlayerModel.cs)：状态、只读属性/库存与数值约束。
- [PlayerDataStore.cs](../../Assets/Scripts/Features/Player/Data/PlayerDataStore.cs)：玩家 JSON 的 Load / Save。
- [PlayerEventSystem.cs](../../Assets/Scripts/Features/Player/System/PlayerEventSystem.cs)：统一准备、校验、应用、保存与通知；Change* 返回 bool。
- [PlayerDetailsControl.cs](../../Assets/Scripts/Controller/UIController/PlayerDetailsControl.cs)：监听类型事件并更新 Text。
- [GoodsModel.cs](../../Assets/Scripts/Model/GoodsModel.cs)：另一个已注册的空物品字典模型。
- [Msg.cs](../../Assets/Scripts/Msg/Msg.cs)：UpdateShowData、InventoryChanged 类型事件与字符串消息名。

## 【FACT】状态归属

`PlayerData.property` 保存 Name、Hp、Power、Level、Exp、Attack、Defence、Speed，以及 UpperHp、UpperPower、UpperAttack、UpperDefence、UpperSpeed。`Exp` 为 long，其余数值为 int。`goodsDict` 是物品名到数量的字典，金币以 `"Coin"` 为键。

PlayerModel.OnInit 通过 Game 注册的 PlayerDataStore 读取 PlayerData。当前背包和金币均使用 PlayerModel 内部唯一的 goodsDict，对外提供 ReadOnlyDictionary 包装的 IReadOnlyDictionary；全部公开属性只读。GoodsModel 虽在 Game 注册，但自己的 GoodsDict 仅被初始化为空字典，未接入背包主数据源。

`IsDied`、`IsEmptyPower` 不写入 JSON，分别由当前 Hp <= 0、Power <= 0 派生；加载后立即反映已有存量。死亡后的普通 Hp 增量保持 Hp=0，其他属性仍可按各自规则变更。

## 【CURRENT STRATEGY】变更、通知与存储

主要路径为：

`命令或调用方 → PlayerEventSystem.Change* / ChangeAll → PlayerModel 准备最终值 → 统一应用 → PlayerDataStore.Save → 类型事件`。

- ChangeLevel、ChangeExp、ChangePower、ChangeHp、ChangeAttack、ChangeDefence、ChangeSpeed、ChangeCoin、各 ChangeUpper 方法及 ChangeGoodsDic 的数值参数均是增量；零增量不产生变化。
- ChangeName 接收最终名称，空值或空字符串报错并返回 false。ChangeAll 接收独立 ItemData，可同时接收物品增量字典；未接入道具库存消耗。
- 多字段操作先准备全部最终值；任一业务校验失败即返回 false，不应用属性、扣费或奖励。属性使用 struct 候选值，库存只准备本次涉及项，不复制整个库存。
- 一次实际发生变化的 ChangeAll、LevelUp、库存操作或战斗结算只调用一次 Save。保存失败由 PlayerDataStore 记录路径和原始异常，系统回滚本次内存改动并返回 false；场景道具和胜利敌人对象仅在成功后回收。
- 实际属性或金币变化在保存成功后发送一次 UpdateShowData；普通库存变化发送一次 InventoryChanged。两类变化同时存在时各发一次。无实际变化时返回 true，不保存、不发送事件。
- PlayerDetailsControl.OnStart 查找直接子节点 Text、获取 PlayerModel、注册 UpdateShowData 并立即 UpdateShow，继续展示昵称、等级、经验、生命、体力、攻击、防御、速度、金币。
- InventoryChanged 已由 KnapsackControl 订阅；事件触发后重建格子并按有效数量更新容器高度，释放时注销订阅。

存储位置和平台限制见[资源与数据](DataResources.md)，不要将这些同步文件写入描述为网络存档。

## 【FACT】限制值与分支

| 项目 | 当前实现 |
|---|---|
| LimitMinPower / LimitMinHP | 50 / 100，限制体力上限与生命上限 |
| LimitMinAttack / LimitMinDefence / LimitMinSpeed | 5 / 4 / 5，限制对应当前值与上限 |
| MaxLevel | 常量仍为 100，未新增等级限制 |
| Power | 扣除后小于 0 报错拒绝，恰为 0 合法；恢复截到本次最终 UpperPower |
| Hp | 活着时对旧 Hp 加一次增量，截到 0 至本次最终 UpperHp；已死亡后普通恢复保持 0 |
| Attack / Defence / Speed | 本次修改该属性时，先检查对应上限合法，再将当前值加增量截到下限与上限之间 |
| 各 Upper 属性 | 非零修改后低于对应下限时明确报错并拒绝；不再静默替换默认值 |
| Coin | ChangeCoin 为专用入口，ChangeGoodsDic 的 Coin 键也转到此入口；最终值至少 0，必需 Coin 键保留 |
| 普通库存 | 负结果、扣减不存在的条目或超出 int 范围时记录物品与原因并拒绝；正数新增，合法归零删除，零增量不变 |

ChangeAll 先得到本次最终上限，再据其约束本次有非零增量的对应属性；单独修改上限不重写未请求修改的当前值。等级、经验、上限及体力加法发生溢出时明确报错拒绝；Hp 和攻防速使用 long 中间值后按各自边界截取，金币与普通库存拒绝超出 int 上界的结果。

加载旧属性数据时统一检查每组数值，只报错、保留原值，不自动写回。当前 Defence=8、UpperDefence=0、下限=4 的错误在加载时由模型报一次；无关属性、金币和库存操作不重复检查或修正该组数值。若新操作涉及仍不合法的防御上限/属性，则本次操作报错拒绝。

`LevelUp()` 单次判断所需经验 `Level * 100 + 100`，足够时把升一级与扣经验作为一次操作提交，否则打印“经验不足升级”并返回 false。ChangeExp 不自动升级；在 Scripts / Framework / Test 检索范围未检出 LevelUp 调用点。

`EnableAttack(CostPower)` 检查玩家未死亡、成本非负且不超过当前体力；等于当前体力可扣至 0，其后不能支付正成本。具体循环规则见[战斗](Combat.md)。

## 【KNOWN ISSUES】静态边界

- 当前旧存档的 Defence / UpperDefence 仍不合法，按已确认规则只报错、暂不修正；该数据问题未解决。
- PlayerDetailsControl 在 Release/OnDestroy 注销 UpdateShowData；重复 OnStart 只刷新一次，不重复注册。
- 库存事件已发出并由 KnapsackControl 订阅，详见[背包与道具](Inventory.md)。
- PlayerDataStore 仍复用同步 JsonUti 直接写原路径；保存失败回滚内存不代表磁盘文件具备原子替换或备份能力，平台存储边界见[资源与数据](DataResources.md)。

## 未知项与验收状态

## 【FACT】第 1 阶段玩家控制切片

`CombatPrototypePlayerController` 是独立的 3D 测试入口，要求同一对象挂载 `CharacterController`，并通过序列化 `cameraTransform` 获取相机水平朝向；WASD 移动、旋转和重力仅作用于该测试对象，不改变现有 `PlayerModel`、`PlayerControl` 或存档调用链。

`UNKNOWN`：等级上限的产品要求、正式初始数据及后续存档迁移策略。金币赋值、Hp 增量判定、体力存量标记和普通库存边界已按确认规则改写。第 1 阶段独立玩家控制切片已获用户人工 GamePlayer 验收确认，结论不扩展到正式玩家链。第 2 阶段已有主线程通过结论；第 4 阶段 UI 生命周期与刷新已完成静态落地，人工 GamePlayer 交互验收仍待主线程确认。

## 【FACT】第 2B 阶段玩家网络链

`CombatPrototypePlayerNetCodeAuthoring` 为玩家 Ghost 烘焙速度 `5`、`CombatPrototypePlayerInput` 和近战数据；NetCode 为 `IInputComponentData` 生成输入缓冲。服务端握手后设置 `GhostOwner.NetworkId`、启用 `AutoCommandTarget`，并把连接 `CommandTarget` 指向玩家；玩家加入连接 `LinkedEntityGroup`，跟随断线销毁。

玩家 Ghost 使用 `OwnerPredicted`：本地拥有者预测，其他客户端插值。`CombatPrototypePlayerInputSystem` 只给 `GhostOwnerIsLocal` 写入由当前本地镜头水平角转换的世界 X/Z Move，以及空格/鼠标左键 `InputEvent` 攻击事件；第 6B 增加 R 键 `Respawn`，第 7A 增加 E 键 `UseItem`，F 键 `Gather` 现承载采集/砍树/采矿统一请求，第六阶段增加 G 键 `Pickup`，`HarvestTree`/`Mine` 字段保留，H/J 停止触发与消费。`CombatPrototypePlayerMovementSystem` 等待地图数据，在 Client/Server 预测组中只处理带 `Simulate` 且存活的实体；移动输入拒绝非有限值并限制长度，位移通过共享的 CombatPrototypeMapMovementUtility 对当前启用阻挡圆进行扫掠及滑动，再更新 LocalTransform。角色 Y、速度和按输入设置朝向的规则保持；地图占地、留缝及烘焙阻挡数据归[战斗地图](Map.md)。网络玩家仍不接第 1 阶段 CharacterController 脚本的移动、重力或碰撞链；网络镜头与输入转换采用本页“本地跟随镜头”定义的独立入口。

## 【FACT】地图采集与玩家状态

F 由服务端跨采集点/树木/矿点确认最近有效目标，采集点由原系统计时；移动/攻击/受击/死亡/超距/断线会释放预约，完成后保存并入包活力苹果，E 仍仅使用小块肉。第五阶段保存成功后按默认 600 秒服务端期限恢复原采集点，玩家须新 F 请求；再生不恢复玩家生命/体力、不发物品，输入字段及原资格/中断规则保持。目标、计时和提交规则归 [背包与道具](Inventory.md)。没有新增玩家组件或修改玩家 Prefab；用户已确认地图第四阶段人工 GamePlayer 通过，主线程结合既有静态核对判定该阶段通过；玩家范围限采集输入/中断、死亡/R 复活、多人同步/重连及原玩家回归；用户已确认第五阶段人工 GamePlayer 通过，主线程结合既有静态核对判定该阶段通过，玩家范围限新 F 循环再采集、多人同步/晚加入、重连/重启及原玩家回归，完整边界归 [运行入口](Runtime.md)。

## 【KNOWN ISSUES】地图移动阻挡验收

静态脚本与地图烘焙数据已核对；用户已确认地图第三阶段人工 GamePlayer 验收通过，主线程结合既有静态核对判定该阶段通过。玩家范围覆盖树木阻挡、滑动、本地预测与远端同步、死亡/复活及原玩法回归，完整边界归[运行入口](Runtime.md)。性能、平台构建和线上联调仍为 UNKNOWN。

## 【KNOWN ISSUES】第 2B 阶段玩家验收

玩家 Ghost、输入缓冲与预测组件已通过 Editor 烘焙数据核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。玩家验收仅覆盖当前独立网络原型的两个玩家加入/退出、移动与朝向同步及本地输入预测，不扩展为正式 Map、平台构建、大规模性能或线上联调验收。正式 PlayerModel、PlayerDataStore、属性与存档调用链未接入该独立原型；AI 未执行逻辑单元测试、PlayMode 或构建。

相关模块：[运行入口](Runtime.md)、[战斗](Combat.md)、[背包与道具](Inventory.md)。

## 【FACT】第 3A 阶段玩家与敌人目标关系

群体敌人的目标入口复用当前在线连接的 CommandTarget.targetEntity，只接受已进入游戏且连接状态为 Connected 的有效玩家；第 6A 同时排除玩家生命组件标记为死亡的目标。目标选择读取服务端玩家移动后的 X/Z 位置，断线或死亡玩家不会继续作为追踪候选，无候选时敌人停止。玩家 Ghost、移动速度、输入、预测和近战时序参数保持原值；近战命中由空间查询生成各敌人的伤害事件，详见[战斗](Combat.md)。

## 【KNOWN ISSUES】第 3A 阶段玩家关联验收

用户已确认第 3A 人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。玩家关联验收仅覆盖当前独立网络原型的玩家加入/退出、最近在线目标切换、无在线玩家时停止追踪及群体近战关联行为；群体近战每目标去重、伤害 25 及双端状态一致性的验收边界见[战斗](Combat.md)。第 2B 玩家验收结论保持原范围，本次第 3A 通过结论不扩展为正式 Map、平台构建、大规模性能或线上联调验收。AI 未执行逻辑单元测试、PlayMode 或构建。

## 【FACT】第 4A 阶段网络玩家体力

每个网络玩家 Ghost 独立持有 `CombatPrototypePlayerResource`，整数 `CurrentPower` 与 `UpperPower` 均为 `GhostField`。现有 `CombatPrototypePlayerNetCodeAuthoring.Baker` 从玩家 Prefab 的 `InitialPower=100`、`UpperPower=100` 烘焙当前值与上限，并把 `AttackPowerCost=10` 写入近战配置；配置边界统一检查 `0 <= InitialPower <= UpperPower`、成本非负，不修正非法配置。

## 【CURRENT STRATEGY】第 4A 阶段体力生命周期

体力由服务端近战系统在成功启动攻击时扣除，客户端只接收 Ghost 状态并记录日志，不做本地扣费或体力预测。具体接受、拒绝与时序规则见[战斗](Combat.md)。没有自动恢复；第 6B 手动复活由服务端补满体力，第 7A 物品使用在保存消费成功后恢复体力，规则见[背包与道具](Inventory.md)。连接销毁时沿原 LinkedEntityGroup 销毁玩家，重新加入沿原 GoInGame 握手生成新玩家，从烘焙初值 100 开始。网络体力不接入正式 `Game`、`PlayerModel`、`PlayerDataStore`，体力没有账号或持久化链；金币、经验与背包的固定 ID 恢复见本页第 4D 节。

## 【KNOWN ISSUES】第 4A 阶段玩家验收

新组件与字段、玩家 Prefab 参数及实际 SubScene 烘焙已完成静态核对；用户已确认第 4A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4A 阶段通过。玩家验收仅覆盖当前独立网络原型的双玩家体力独立性及双端同步、成功启动攻击一次扣 10（含空挥）、忙碌输入拒绝且阶段继续推进、10 次成功启动后体力耗尽并拒绝后续攻击且序号不增加、无自动恢复、断线重新加入恢复 100，以及原移动回归；群体伤害与死亡显示回归范围见[战斗](Combat.md)。第 2B、第 3A 与第 3B 的既有通过范围保持原状，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未运行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4B 阶段本局奖励状态

每个网络玩家 Ghost 独立持有 `CombatPrototypePlayerReward`，整数 `Coin` 与 `Experience` 均为 GhostField。现有玩家 Baker 添加该组件，烘焙金币和经验初值均为 0；玩家 Authoring 序列化字段与玩家 Prefab 保持。该数据属于独立网络原型，现由第 4D 固定 ID 存档保存和恢复，不是正式 PlayerModel 的金币、经验或存档。

## 【CURRENT STRATEGY】第 4B 阶段本局奖励生命周期

`CombatPrototypeRewardSystem` 在服务端伤害系统之后消费击杀奖励事件，从带 NetworkStreamInGame、NetworkId、CommandTarget 且状态为 Connected 的连接确认接收玩家。当前结算包含第 4C 物品，并按第 4D 在完整候选存档写入成功后同次提交金币、经验和目标物品；客户端仅接收 Ghost 并沿用状态日志。归属、整体失败与每敌人奖励规则见[战斗](Combat.md)。

结算时攻击者已离线则记录 AttackerOffline 并消费事件，不补发；连接销毁时玩家沿原 LinkedEntityGroup 销毁。第 4D 重新加入按固定 ID 恢复金币/经验与背包，仅无档新玩家从 0/0 与空背包开始；体力仍从 100 开始。正式账号、PlayerModel/PlayerDataStore、正式背包、实体掉落、升级和奖励 UI 保持原边界；本局背包见第 4C 节。

## 【KNOWN ISSUES】第 4B 阶段玩家验收

代码编译、两个 GhostField 及其生成的 Serializer/Snapshot、实际烘焙的 0/0 初值已完成静态核对；用户已确认第 4B 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4B 阶段通过。玩家验收仅覆盖当前独立网络原型的致命一击归属并获得金币 1/经验 10、双玩家累计独立与双端同步、离线不补发和重新加入归零；非致命无奖、每敌人一次、多目标分别结算及原体力/伤害/死亡显示回归范围见[战斗](Combat.md)。既有第 2B、第 3A、第 3B、第 4A 通过结论保持原范围，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4C 阶段本局背包生命周期

每名玩家独立持有 `CombatPrototypeInventoryItem` Ghost 缓冲，玩家 Baker 烘焙为空，玩家 Authoring 序列化参数与玩家 Prefab 保持。运行实体随原连接生命周期销毁；第 4D 再次加入按固定 ID 恢复背包与金币/经验，无档新玩家为空背包与 0/0，所有加入均为体力 100/100。库存名称与数量、同名累加和同步规则由[背包与道具](Inventory.md)维护，玩家不接正式存档或账号身份。

## 【KNOWN ISSUES】第 4C 阶段玩家验收

编译、背包 Ghost 字段及实际烘焙初值已静态核对；用户已确认第 4C 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4C 阶段通过。玩家验收仅覆盖当前独立网络原型的双玩家空初始背包、独立累计与双端同步、致命一击金币/经验/小块肉三项奖励一致、同名合并、离线不补发、重新加入空背包，以及原体力和移动回归；非致命无奖、每敌人一次、多目标分别结算、伤害和死亡显示回归范围见[战斗](Combat.md)。第 4B 和更早通过结论保持原范围，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4D 阶段固定身份与玩家准入

`CombatPrototypeGoInGameRequest` 携带 `FixedString64Bytes PlayerId`。服务端在同一个 GoInGame 入口验证 ID、读取并完整校验存档后才排入玩家实例化；新增 `CombatPrototypePlayerIdentity` 在准入时仅添加到服务端玩家，未加入 GhostField 或玩家 Baker，未新增 Prefab 挂载。身份配置与存档字段由[资源与数据](DataResources.md)维护。

## 【CURRENT STRATEGY】第 4D 阶段加入、重复身份与恢复

已进入游戏的同一连接重复 RPC 沿原规则消费，不生成第二个玩家；同一更新内每个连接只处理一次。服务端从 Connected、NetworkStreamInGame、NetworkId、CommandTarget 指向的有效玩家收集占用 ID，并在接受本次请求后立即占用该 ID，覆盖同一更新内不同连接竞争同一 ID 的情况。已在线 ID 拒绝新连接，保留原玩家；无效 ID、坏档或读取失败记录错误并请求断开，仅终止当前请求。

完整恢复Coin、Experience、库存与Tools后，沿原 GhostOwner、AutoCommandTarget、CommandTarget、生成位置及 LinkedEntityGroup 绑定。仅无档新玩家采用0/0、空库存/工具；v1读取迁移工具为空；体力仍为烘焙 100/100，第 6A 生命也从烘焙 100/100 开始，攻击状态与位置重新初始化。断线销毁仍由原 NetCode 生命周期负责；成功奖励/E/F植物/G/工具制作或工具完成时保存，候选保留Tools；不增加断线补存或退出回调。

## 【KNOWN ISSUES】第 4D 阶段玩家验收

身份组件、握手系统及新 RPC Serializer 已由 Unity 编译并加载；用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。玩家范围覆盖独立 ID 加入、同 ID 重连/服务端重启恢复、重复 ID 拒绝、坏档隔离及体力重置，完整边界见[运行入口](Runtime.md)。第 4B/4C 的归零与空背包记录保持当时范围；第 4D 人工通过来自本阶段用户反馈，AI 未运行逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 6A 阶段网络玩家生命

[CombatPrototypePlayerHealth.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerHealth.cs) 保存每名玩家独立的 float CurrentHealth、MaxHealth、uint HitSequence 与 byte IsDead，四项均为 GhostField。原玩家 Baker 从 InitialHealth=100、MaxHealth=100 烘焙生命，受击序号和死亡标记为 0；只接受有限的 `0 < InitialHealth <= MaxHealth`，非法配置直接暴露错误。

玩家同时持有仅服务端保留的 [CombatPrototypePlayerDamageEvent](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerDamageEvent.cs) 空缓冲，每条事件包含敌人实体、攻击序号和伤害。客户端接收生命快照，生命扣减由服务端伤害系统结算，局内恢复由第 6B 复活系统写入，不调用正式 PlayerModel 或 PlayerDataStore；事件产生与结算顺序由[战斗](Combat.md)维护。

## 【CURRENT STRATEGY】第 6A 阶段死亡与生命生命周期

CurrentHealth 降至 0 后设置 IsDead=1，后续伤害不再扣血或递增受击序号。玩家移动系统在读入移动与旋转前检查死亡标记；服务端近战系统在死亡分支取消未完成阶段并清零计时，后续攻击输入记录 PlayerDead，不再扣体力、递增攻击序号或生成伤害。已经启动攻击的原体力扣费保持，原存活玩家攻击流程沿用。

死亡玩家保留连接、实体、金币/经验/库存及 Mono 显示，敌人排除其追踪与反击资格；局内手动复活见第 6B 节。当前显示已接入受击与死亡倒地，规则见[玩家美术](../PlayerArt.md)，尚未接入死亡隐藏或治疗。断线仍沿原 LinkedEntityGroup 销毁玩家，再次加入重新生成生命 100/100、受击序号 0、未死亡；金币/经验/背包继续按第 4D 固定 ID 恢复，生命不入存档。

## 【KNOWN ISSUES】第 6A 阶段玩家验收

生命 Ghost Serializer/Snapshot 四字段、实际生命初值与空伤害缓冲已静态核对；用户已确认第 6A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。玩家范围覆盖双玩家生命独立与双端同步、死亡后停止移动/攻击且不再扣体力或增加攻击序号、敌人排除死亡玩家、重新加入重置生命并恢复原固定 ID 奖励/背包，完整边界由[运行入口](Runtime.md)维护。第 4D 及以前的通过范围保持；本结论不包含规模性能、平台构建或线上联调，AI 未执行游戏系统、PlayMode 或逻辑单元测试。

## 【FACT】第 6B 阶段手动复活入口

[CombatPrototypePlayerInput.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 在现有 IInputComponentData 中增加 InputEvent Respawn；客户端 R 键按下当帧只给 GhostOwnerIsLocal 写入事件，沿原 NetCode 输入缓冲发送。输入不按客户端生命快照过滤，死亡资格由服务端当次伤害结算后的 IsDead 判定。

[CombatPrototypePlayerRespawnSystem.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerRespawnSystem.cs) 只进入 ServerSimulation 的 PredictedSimulationSystemGroup，并在玩家伤害结算之后更新。它沿 Connected、NetworkStreamInGame、未请求断线的连接 CommandTarget 读取玩家，只处理当前启用 Simulate 的玩家；GhostOwner 必须与连接 NetworkId 一致。存活请求记录 PlayerAlive 并拒绝，归属不一致记录 CommandTargetOwnerMismatch 并拒绝。

## 【CURRENT STRATEGY】第 6B 阶段复活状态

服务端确认死亡后，先取得必需玩家组件与伤害缓冲引用，再清理旧敌人锁定，清空玩家伤害缓冲；玩家近战回 Ready、计时为 0，CurrentPower=UpperPower、CurrentHealth=MaxHealth、IsDead=0。位置通过与加入流程共用的 CombatPrototypeMapSpawnUtility 从[地图配置](Map.md)计算，当前仍为 (NetworkId * 2, 1, 0)，旋转和缩放保留。地图采集系统位于伤害之后、复活之前，死亡预约先取消，R 复活不重处理同 tick 的 F。复活沿用原玩家实体、连接和身份，不创建新 Ghost；HitSequence、玩家及敌人攻击序号不因复活归零或递增，金币/经验/库存与存档保持原值。

复活发生在本 tick 已完成的移动、近战、反击和玩家伤害之后，本 tick 不再次移动或攻击，后续 tick 恢复原操作与敌人候选资格；同 tick 的 R 请求若在该结算点已死亡，也按死亡状态处理。客户端等待原生命/体力/位置快照，不自行恢复生命或预测复活。旧敌人挥击的取消与后摇规则归[战斗](Combat.md)。

必需组件或缓冲获取失败记录连接、玩家、NetworkId、阶段与原始异常，终止当前玩家处理并继续其他连接，不补默认组件或状态。没有复活保护时间、自动复活、额外复活费用或持久化生命/体力。

## 【KNOWN ISSUES】第 6B 阶段玩家验收

输入字段、独立系统及 NetCode 生成类型已编译并加载，源代码与编译后系统特性、文件边界已静态核对；用户已确认第 6B 人工 GamePlayer 验收通过，主线程结合既有静态核对与用户反馈判定该阶段通过。玩家范围覆盖 R 键死亡复活、满生命/满体力与原加入位置、双玩家独立及双端同步、存活请求拒绝、重复输入、序号与奖励/库存保持和原操作/存档回归，完整边界归[运行入口](Runtime.md)。复活点附近有敌人时仍可再次受伤或死亡；第 6A 及此前通过结论保持，人工通过来自用户反馈，AI 未运行 GamePlayer 或逻辑单元测试。

## 【FACT】第 7A 阶段体力恢复接入

E 键 UseItem 沿原命令链发送；服务端 CombatPrototypeItemUseSystem 在近战前、保存成功后写入现有体力组件，完整规则归[背包与道具](Inventory.md)。

## 【CURRENT STRATEGY】第 7A 阶段玩家状态边界

使用只写目标库存与 CurrentPower，原实体、生命和序号保持。体力不持久化；重连恢复消费后的库存，体力仍为 100/100。R 复活仍补满体力且保留库存。

## 【KNOWN ISSUES】第 7A 阶段玩家验收

用户反馈第 7A 人工验收通过，主线程结合静态核对判定通过；玩家验收范围见[运行入口](Runtime.md)。

## 【FACT】网络原型本地跟随镜头与移动输入

[CombatPrototypePlayerInput.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 保留 Move、Attack、Respawn、UseItem 四个原输入字段，另由地图第四阶段增加 Gather，第六阶段增加 Pickup。WASD 在原归一化之后，调用本客户端 World 的 CombatPrototypeCameraBindingSystem，再由 CombatPrototypeFollowCamera 当前水平角旋转到世界 X/Z Move；相机操作每个渲染帧采集一次，同一水平角应用到画面。初始角为 0°，W/S 为画面前后、A/D 为画面左右，移动速度仍为 5。

## 【CURRENT STRATEGY】镜头归属与玩家生命周期

本地跟随只读取 GhostOwnerIsLocal 玩家经官方桥接得到的显示根 Transform，以及同步的 IsDead。输入转换后继续使用原 CombatPrototypePlayerMovementSystem 的同一客户端预测/服务端模拟与朝向写入；服务端不读取镜头，不新增 Ghost 字段，不改攻击、体力、物品、奖励或存档结算。

死亡保持本人镜头；复活从死亡转为存活时重置位置跟随并保留当前视角。新实体首次绑定与重连恢复默认视角，且在生成本次移动命令前完成重置；没有本地玩家时解除目标。镜头配置、绑定链和完整人工范围归[运行入口](Runtime.md)。

## 【KNOWN ISSUES】本地镜头玩家验收

源码、Unity 编译及原四个输入字段已静态核对；用户明确反馈尚未进行人工验收，相机相对方向、斜向速度、双客户端归属、复活/重连与原移动/攻击/物品回归的人工 GamePlayer 结果仍为 UNKNOWN。既有第 2B～第 7A 玩家人工通过范围保持。AI 未运行游戏系统、PlayMode、逻辑单元测试或构建。

## 【FACT】网络原型角色动画表现

PlayerView 的现有网络 Owner 连接客户端表现脚本，后者只读取所属 World/Entity 的攻击阶段、攻击序号、受击序号与死亡标记，再驱动绑定的 Animator。移动取官方桥接完成后的水平显示位移，死亡保留根节点而让角色倒地，复活沿原生命/位置快照恢复站立；完整资源、优先级及生命周期归[玩家美术](../PlayerArt.md)。该链不接正式 PlayerModel，也不写网络状态或存档。

## 【KNOWN ISSUES】玩家动画验收

脚本编译、资源绑定和 Clip 预览已通过主线程静态核对；人工 GamePlayer 的本地/远端动作、移动中攻击、受击、死亡/复活与断线重连仍为 `UNKNOWN`。既有玩家网络和相机的验收边界保持，静态预览不替代人工运行通过。

## 【FACT】地图动态掉落拾取输入

[CombatPrototypePlayerInput.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) 在原 IInputComponentData 增加 InputEvent Pickup；G 单次按下仅给 GhostOwnerIsLocal 写入事件，所有端须使用相同输入布局。客户端不提交目标、生成掉落或入包；服务端在玩家伤害之后、F 采集与 R 复活之前验证在线/归属/Simulate、存活、静止及近战 Ready，并选择最近已落地目标，保存成功才入包和消耗。原移动/攻击/R/E/F 输入及玩家 Prefab 保持；同次死亡后的 G 被拒绝，R 不补发该次拾取。完整资格、目标及同步边界归[掉落与拾取](MapDrops.md)。用户已确认第六阶段人工 GamePlayer 通过，主线程结合静态核对与用户反馈判定该阶段通过；玩家范围限 G 单次输入/拒绝条件、多人同步/重连和死亡/R/F/E 等原玩家回归，完整边界归[运行入口](Runtime.md)第六阶段九项清单。

## 【FACT】地图树木砍伐输入与动态阻挡

当前 F 统一选中树木后调用 TreeHarvest.TryBegin，原 HarvestTree 字段保留但 H 不再触发/消费。TreeHarvest 继续计时/中断/产出，移除 F 采集优先；动态阻挡、原玩家 Prefab/移动/镜头/动画及 G/E/R 保持。规则归[树木](MapTreeHarvest.md)/[统一交互](Map.md)。用户已确认第七阶段人工通过，仍限原 v5/revision=6 的 H 资格/请求、中断/F 优先、争抢、阻挡/同步及[运行入口](Runtime.md)十项清单；该反馈不覆盖统一 F，未触发独立用例仍为 UNKNOWN。

## 【FACT】树木再生的玩家占位与移动

树木再生在服务端砍伐之后、R 复活之前检查，全部存活网络玩家的位置参与原点占位，包含当前未启用 Simulate 的玩家；死亡玩家不占位。默认中心距离不超过 0.5+0.4+0.01=0.91 米时等待，移出后原树恢复 Standing/显示；不会推开或传送玩家。玩家原预测移动按 Ghost 的砍倒/再生阻挡历史读取当前 tick 的 Disabled，取消预约及新 F 保留既往历史；输入、玩家资源、镜头、姿态和移动算法保持。细节归[树木砍伐](MapTreeHarvest.md)，用户已确认第八阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定该阶段通过；玩家范围限占位等待、再生阻挡、多轮 H/跨端移动与原玩法回归，完整边界归[运行入口](Runtime.md)第八阶段八项清单。人工结论来自用户反馈，精确边界、同 tick、延迟/回放和未触发失败用例仍为 UNKNOWN。

## 【FACT】地图采矿输入与动态阻挡

当前 F 由服务端统一选择最近有效资源目标，选中矿点时调用 MineHarvest.TryBegin；Mine 字段保留，J 不再触发/消费。有效在线 CommandTarget/归属、启用 Simulate、存活、零 Move、无攻击且近战 Ready 才合格。玩家只持有一个资源预约，重复 F 不重置或切换，完成/取消本 tick 不再启动；旧 F/H 优先判断移除。原资格/中断及玩家 Prefab/镜头/移动/动画和 G/E/R 保持；矿点阻挡按转换历史重建。规则归[采矿](MapMining.md)/[统一交互](Map.md)，第九阶段原 J 人工仍为 UNKNOWN；统一 F 已获用户人工通过反馈，范围与未触发用例归[运行入口](Runtime.md)八项清单。

矿点默认 600 秒原点再生前收集全部存活玩家位置，包含未启用 Simulate 的实体；X/Z 中心距离小于等于 0.75+0.4+0.01=1.16 米时等待，死亡玩家不占位。空闲后原矿点恢复 Available，须新 F 再次采矿；不推开玩家或新增玩家组件。规则归[采矿](MapMining.md)，用户已确认该阶段人工 GamePlayer 通过；玩家范围限原点占位等待、离开/死亡后恢复及移动阻挡/输入回归，完整边界归[运行入口](Runtime.md)矿点再生八项清单及 v6/revision=9，未触发的精确距离/特殊 Simulate/预测时序仍为 UNKNOWN。

## 【FACT】资源交互 HUD 的所属玩家数据

当前 Player Baker 在原玩家实体追加初始 Hidden 的 HUD State，四字段按 SendToOwner 同步；客户端仅枚举启用 GhostOwnerIsLocal 的玩家并在死亡/断线/地图停止时收起。HUD 只展示原 F 目标/进度，原玩家属性、输入和复活流程保持；字段、Main Camera 挂载、注册/解绑及 JSON 归[交互显示](MapInteractionHud.md)。正常编译/所属 Serializer/初始烘焙已静态核对；用户已确认本阶段人工 GamePlayer 通过，限[运行入口](Runtime.md)HUD 八项清单及 v7/revision=10，未实际触发的跨端/预测时序/生命周期和独立失败仍为 UNKNOWN。

## 【FACT】所属工具、输入与生命边界

原Player Baker追加空GatherTool缓冲与零CraftFeedback，两者SendToOwner；数字1/2单次InputEvent只由本地GhostOwnerIsLocal写入，F沿原选择自动使用对应工具。准入先完整校验v1/v2存档及当前工具定义，再恢复耐久；死亡/R保留工具，重连不补满。原生命/体力/近战/位置、Prefab/Animator与镜头规则保持，工具资格复用原F条件并拒绝资源预约期间制作。所属HUD读取工具/反馈，详细规则归[采集工具](MapGatherTools.md)/[交互HUD](MapInteractionHud.md)。v8/revision=11正常编译/烘焙静态通过，用户确认人工通过，限v8/revision=11及[运行入口](Runtime.md)十二项清单，未实际触发的归属/预测时序/生命周期及独立失败仍为UNKNOWN；旧用户通过保持原范围。

## 【FACT】材料面板的本地玩家输入

[制作面板](MapInventoryPanel.md)读取本地启用GhostOwnerIsLocal的玩家及其Connected/InGame所属连接；B仅本地开关，按钮沿原CraftAxe/CraftPickaxe字段提交，未增Ghost/命令字段。面板内左键不触发攻击、滚轮不传镜头，面板外及键盘操作保持；死亡/断线/玩家或地图源变化/World释放清掉未提交按钮请求、投影与旧反馈。原生命、体力、R、库存/工具恢复和v2存档保持，正常编译/十四次隔离烘焙静态通过，用户确认人工通过限v9/revision12及[运行入口](Runtime.md)面板十二项；未触发的归属/生命周期/预测时序及独立失败仍UNKNOWN。

v10/13接入的[背包丢弃](MapInventoryDrop.md)已接入。玩家输入新增DropInventory事件、InventoryDropItem/InventoryDropMode byte，玩家Baker增加Sequence/Kind/Quantity/Result所属反馈；原身份、生命/体力、工具、R和v2存档行为保持。编译/16次隔离烘焙静态通过，用户确认丢弃人工通过限[运行入口](Runtime.md)v10/13十二项，未触发独立用例UNKNOWN；原用户通过限各自版本/清单。

v11/14阶段的[G提示](MapPickupHud.md)由玩家Baker追加Hidden初值、Mode/DropId/ItemId/Quantity四字段所属Ghost；原13输入、F四字段、丢弃反馈、生命/体力/R/库存/Tools及v2存档保持。客户端沿原本地玩家/Connected/InGame绑定，死亡/断线及World/玩家/地图源变化清掉旧提示；显示不开辟客户端结算入口。编译/十次隔离烘焙静态通过，用户确认新显示人工通过，限v11/14十项，未触发用例UNKNOWN，旧通过保持原范围。

v12/15的[高亮](MapInteractionHighlight.md)沿原本地所属存活玩家/Connected/InGame绑定，仅读取F/G四字段及对应客户端目标位置；未改玩家Baker、13输入、Ghost字段、生命/体力/R/工具或存档。死亡/断线、玩家/地图源及World/Scene失效清圆环与缓存。高亮静态及用户人工通过限v12/15十项，未触发独立用例UNKNOWN，旧通过保持原范围。

v13/16的[资源状态](MapResourceStatusHud.md)由Player Baker追加所属Mode/Kind/PlacementIndex/RemainingSeconds四字段，初始Hidden/0/-1/0；原13输入、F/G各四字段、生命/体力/工具/存档保持。仅Connected/InGame、所有权匹配、启用Simulate且存活者获得状态，死亡/断线与World/Scene变化清显示。新布局/编译/烘焙静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧通过保持原范围。

当前v23/26的[工具修理](MapToolRepair.md)：Player Baker追加零修理反馈Sequence/Kind/Result，仅SendToOwner；修理阶段新增RepairAxe/RepairPickaxe；当前含SaveWorld/背包升级/工具升级共19输入字段。复用原所属资格/唯一Tools，玩家v4保存携带工具/容量等级，生命/体力/R及资源显示快照保持；各端同版重新烘焙。新链静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN，旧玩家用户通过保持原版本/清单。

## 【FACT】地图资源存档接入边界

地图资源恢复为Pending时原准入等待，Failed时断开，Ready才沿原身份/玩家v4及合法v1/v2/v3恢复；新门位于三类资源生成之后。原生命/体力/R与玩家保存保持，当前19输入及新增所属显示归[F5](MapWorldSaveHud.md)，完整边界归[资源存档](MapResourcePersistence.md)，资源存档人工通过限v15/18十二项，未触发用例UNKNOWN。 掉落恢复及同文件快照归[掉落存档](MapDropPersistence.md)，人工通过限v16/19十二项，未触发用例UNKNOWN。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)由Player Baker追加Mode/ManualSequence/ManualResult三字段、Hidden/0/None初值且SendToOwner；SaveWorld沿原F5单次输入，所有端同版重新烘焙。原身份/生命/体力/R与玩家v2保存保持；静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN。

## 【FACT】材料容量与玩家恢复

[容量](MapInventoryCapacity.md)沿原所属库存与超限恢复规则，用户人工通过限v19/revision22十六项；[升级](MapInventoryCapacityUpgrade.md)由原Player Baker追加Level=1及Sequence0/ResultNone，三个新GhostField均SendToOwner，数字5与6/7事件使当前输入19。准入完整校验v4/合法v1/v2/v3后恢复工具与容量等级；死亡/R保留。Prefab/Animator结构、生命/体力/位置保持，旧九保存入口携带Level。新链编译/22次Bake静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧通过仍限原版本/清单。

## 【FACT】工具等级与升级所属数据

[工具升级](MapGatherToolUpgrade.md)沿原工具缓冲追加Level(int)，ToolId/Durability/Level均SendToOwner；原Player Baker追加Sequence0/KindNone/ResultNone升级反馈，三GhostField所属。当前输入19，6/7只给本地启用GhostOwnerIsLocal写一次事件。服务器复用F资格、在背包升级后/F与R前检查旧请求/资源忙并保存后提交；死亡/R不清Tools，重连恢复v4实际等级。v2/v3工具补Lv1，v3原容量级保留，读取不写盘；完整迁移归[资源与数据](DataResources.md)。生命/体力/战斗/Prefab/Animator保持。新链静态及用户人工通过限v21/revision24二十二项，未触发用例UNKNOWN；旧通过不扩展。

## 【FACT】工具耐久的客户端预警

[耐久预警](MapToolDurabilityHud.md)只读原本地所属Tools快照与有效本级上限，新增固定地图Settings不含Ghost字段；原HUD已完成ID/唯一槽/等级/耐久范围验证后计算25%/10%和不足单次成本的优先损坏状态。没有新输入、玩家组件、反馈事件或存档字段，19输入、工具三字段SendToOwner与v4保存契约保持；等级/耐久只读缓存、每帧隐藏沿Clear，死亡/断线/源/玩家/World/Scene失效沿原Reset清颜色及状态。静态及用户人工通过，限v22/revision25十六项，未触发显示/联网/生命周期分支UNKNOWN；用户工具升级通过仍限v21/revision24二十二项。

## 【FACT】统一F失败所属快照

原Player Baker为[F失败提示](MapInteractionFailureHud.md)追加Sequence(uint)=0、Result(byte枚举)=None，两GhostField均SendToOwner；输入仍19，Tools/F/G/资源状态/世界保存原字段、生命/体力/R、Prefab与v4存档保持。服务端复用原F拒绝原因并在写入边界核实GhostOwner/存活；归属不匹配不写另一玩家，反馈错误独立记录且不取消成功预约。成功启动清旧失败，客户端初次观察不重播、原死亡/断线/绑定失效Reset清新增状态；仅最新快照，无事件队列或存档。编译/实际Serializer/32次隔离Bake静态通过，初值0/None正确；用户确认新提示人工通过，限v23/revision26十六项；未触发的独立联网/生命周期用例UNKNOWN。

## 【FACT】采集完成与中断所属快照

原Player Baker为[采集结果](MapGatherOutcomeHud.md)追加Sequence(uint)=0、Kind(byte)=0、Result(byte枚举)=None三GhostField，SendToOwner；原启动失败二字段保持。实际结算/Cancel成功后向原Collector写最新结果，核实GhostOwner/NetworkId及存活，写入失败独立记录、不撤销业务。成功新F清旧结果；客户端初次不回放，原死亡/断线/绑定失效Reset清缓存。Player Ghost布局追加组件，各端须同版重新烘焙；输入19、Tools3、生命/体力/R、Prefab及v4存档字段保持。v24/revision27静态及用户人工通过，限十六项清单，未触发独立用例UNKNOWN；原通过范围保持。

## 【FACT】实际G拾取的所属结果

原Player Baker为[拾取反馈](MapPickupFeedbackHud.md)追加Sequence(uint)=0、Result(byte枚举)=None、ItemId(FixedString64Bytes)为空、Quantity(int)=0四GhostField，仅SendToOwner。成功携带原物品ID与本次正增量，其他结果空ItemId/0；写入核实GhostOwner/NetworkId与存活，归属错误/死亡不写新结果，错误独立隔离。客户端首次仅观察序号，死亡/断线/源或玩家失效Reset清缓存，只有最新结果无事件队列。Player Ghost布局追加组件，须各端同版重新烘焙；原输入19、Tools3、F4/G6/资源4/世界保存3/F失败2/F结果3、生命/体力/R、Prefab及玩家v4存档保持。v25/revision28静态及用户人工通过限十六项，未触发独立用例UNKNOWN；F完成/中断通过仍限v24/revision27十六项，旧通过保持原版本/清单。

## 【FACT】G目标可领取数量

当前v30/revision33的[部分拾取](MapDropPartialPickup.md)在原CombatPrototypeMapPickupHudState增加int PickupQuantity，第七个GhostField仍SendToOwner；Player Baker继续添加原Hidden默认值，七字段均零/空初态，组件挂载与Player Prefab保持。实际G读取本人的容量Level/库存决定本次量，所属结果仍原四字段且成功数量为实际增量。输入19、Tools3、F4/资源状态4/世界保存3及原反馈、生命/体力/R、玩家v4/世界v2保持；各端同版重新烘焙。部分拾取v27阶段编译、Serializer七字段/7 mask bits/88字节结构与40次隔离Bake静态通过；用户人工通过限v27/revision30十六项，未触发独立用例UNKNOWN；旧通过仍限原版本/清单。

## 【CURRENT STRATEGY】B材料搜索的本地键盘采样

[材料搜索](MapInventorySearch.md)沿原客户端Binding→HUD→Panel返回blocksKeyboard。输入系统先读取该值；编辑及获得/释放焦点同帧用null gameplayKeyboard采样，使Move归零且不写空格、E/R/F/G/F5、1～7的新键盘事件，B留作文本，镜头ReadMove接收同一隔离键盘而不新增Z/X转动。Enter/Esc结束编辑（活跃IME合成期间保留候选键），之后B可关闭。点击面板外释放焦点的当次左键不写Attack；其他原鼠标/按钮、已提交输入及服务端动作保持，不暂停战斗。

输入仍十九实例字段，玩家Ghost/Tools/反馈与v4档案布局保持；搜索词/焦点不进网络或玩家/世界存档。v29/revision32搜索阶段编译、配置和隔离Bake静态通过；用户已确认搜索人工GamePlayer通过，限v29/revision32及运行入口十六项，结论来自用户反馈；未实际触发的独立输入/生命周期/联网用例UNKNOWN，原各阶段用户通过仍限原版本/清单。

当前v30/revision33的[本机显示偏好](MapInventoryPreferences.md)由原客户端Panel在有效绑定后处理，PlayerInput/Ghost/玩家档案未修改，输入仍十九字段；偏好文件仅包含版本、地图ID和三个显示值，不包含玩家ID/库存/工具。搜索文本恢复不取得焦点，原键盘隔离保持；静态核对通过，人工十六项待验收。
