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

`UNKNOWN`：等级上限的产品要求、正式初始数据及后续存档迁移策略。金币赋值、Hp 增量判定、体力存量标记和普通库存边界已按确认规则改写。第 2 阶段已有主线程通过结论；第 4 阶段 UI 生命周期与刷新已完成静态落地，人工 GamePlayer 交互验收仍待主线程确认。

相关模块：[运行入口](Runtime.md)、[战斗](Combat.md)、[背包与道具](Inventory.md)。
