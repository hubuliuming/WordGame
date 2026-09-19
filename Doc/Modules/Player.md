# 玩家模型、属性与刷新

[返回总导航](../AI_Understanding.md)。本页负责玩家状态的控制逻辑；磁盘 JSON 数值归[资源与数据](DataResources.md)，战斗和使用道具的触发方式归各自模块。

## 入口文件

- [PlayerData.cs](../../Assets/Scripts/Features/Player/Data/PlayerData.cs)：数据结构、下限常量。
- [PlayerModel.cs](../../Assets/Scripts/Features/Player/Data/PlayerModel.cs)：读取数据、属性 setter、保存与刷新事件。
- [PlayerEventSystem.cs](../../Assets/Scripts/Features/Player/System/PlayerEventSystem.cs)：增量属性修改、升级、物品字典变更。
- [PlayerDetailsControl.cs](../../Assets/Scripts/Controller/UIController/PlayerDetailsControl.cs)：监听类型事件并更新 Text。
- [GoodsModel.cs](../../Assets/Scripts/Model/GoodsModel.cs)：另一个已注册的空物品字典模型。
- [Msg.cs](../../Assets/Scripts/Msg/Msg.cs)：UpdateShowData 类型事件与字符串消息名。

## 【FACT】状态归属

`PlayerData.property` 保存 Name、Hp、Power、Level、Exp、Attack、Defence、Speed，以及 UpperHp、UpperPower、UpperAttack、UpperDefence、UpperSpeed。`Exp` 为 long，其余数值为 int。`goodsDict` 是物品名到数量的字典，金币以 `"Coin"` 为键。

PlayerModel.OnInit 通过 JsonUti 读取 PlayerData。当前背包和金币均使用 PlayerModel 内部的 goodsDict；GoodsModel 虽在 Game 注册，但自己的 GoodsDict 仅被初始化为空字典，不能把它写成背包主数据源。

`IsDied`、`IsEmptyPower` 是 PlayerModel 的布尔属性，不在 PlayerData JSON 结构中，OnInit 没有从已读 Hp / Power 重新计算它们。

## 【CURRENT STRATEGY】变更、通知与存储

主要路径为：

`命令或调用方 → PlayerEventSystem.Change* → PlayerModel 属性 setter → 类型事件/JSON 写入`。

- ChangeLevel、ChangeExp、ChangePower、ChangeHp、ChangeAttack、ChangeDefence、ChangeSpeed、ChangeCoin 和各 ChangeUpper 方法按传入增量执行 `+=`；增量为 0 时直接返回。
- ChangeName 拒绝空字符串。ChangeAll 按 ItemData 字段调用各属性变更方法，不处理物品数量扣减。
- Name、Level、Exp、Power、Hp、Attack、Defence、Speed、Coin 的正常 setter 分支发送 `Msg.Register.UpdateShowData`，随后保存 JSON。早退分支见下文。
- 各 Upper 属性 setter 正常分支保存 JSON，不发送 UpdateShowData。GoodsDict setter 保存 JSON，未实现背包刷新；UpdateLocalData 仅执行保存。
- PlayerDetailsControl.OnStart 查找直接子节点 Text、获取 PlayerModel、注册类型事件并立即 UpdateShow。展示昵称、等级、经验、生命、体力、攻击、防御、速度、金币。
- ChangeGoodsDic 修改 PlayerModel.GoodsDict 后调用 UpdateLocalData；已存在键执行数量相加，否则添加键。它没有发送背包更新或详情刷新事件。

存储位置和平台限制见[资源与数据](DataResources.md)，不要将这些同步文件写入描述为网络存档。

## 【FACT】限制值与分支

| 项目 | 当前源码行为 |
|---|---|
| LimitMinPower / LimitMinHP | 50 / 100；用于对应上限 setter |
| LimitMinAttack / LimitMinDefence / LimitMinSpeed | 5 / 4 / 5 |
| MaxLevel | 常量为 100；Level setter 与 LevelUp 未使用它限制等级 |
| Power | 传入值小于 0 时设置 IsEmptyPower=true 并返回，原 Power 不变；否则截到 UpperPower，写值并清除标记 |
| Hp | 先执行 CheckChangeDied，早退时跳过通知与保存；未早退时截到 UpperHp，写值并清除 IsDied |
| Attack / Defence / Speed | 先判断低于各自最小值，再用 else-if 判断高于上限；是有顺序的条件分支 |
| UpperPower | 低于 50 时改为 50，再写入保存 |
| UpperHp | 低于 100 时日志后返回，未写入；其他值写入保存 |
| UpperAttack / UpperSpeed | 分别至少为 5，再写入保存 |
| UpperDefence | 传入 0 时返回；其他值至少为 4，再写入保存 |

`LevelUp()` 单次判断所需经验 `Level * 100 + 100`，足够则升一级并扣除该次所需经验，否则打印“经验不足升级”。ChangeExp 没有自动调用 LevelUp；在 Scripts / Framework / Test 检索范围未检出 LevelUp 调用点。

`EnableAttack()` 仅检查两个状态标记都为 false。具体战斗顺序见[战斗](Combat.md)。

## 【KNOWN ISSUES】静态已见问题

- **金币未赋值**：Coin setter 里写入 `_playerData.goodsDict["Coin"]` 的语句被注释；ChangeCoin 仍触发通知和保存，但不会通过该 setter 改变字典金币数。
- **Hp 判死参数语义不一致**：ChangeHp 用 `Hp += value` 将最终 Hp 传入 setter，CheckChangeDied 却判断 `旧 Hp <= 0 || 旧 Hp + 传入值 <= 0`。命中时写 Hp=0、设置 IsDied 并返回，不通知、不保存；旧 Hp 已为 0 时也会走早退。这里记录原代码条件，正确的死亡/恢复规则为 UNKNOWN。
- **体力标记与存量分离**：Power 小于 0 的早退路径不改存量；恰为 0 的普通路径会清除 IsEmptyPower。攻击是否允许不能等同于“当前体力足够扣除成本”。
- **负物品数量**：ChangeGoodsDic 对缺失键也允许添加负数量，代码保留对应 TODO；是否允许负库存没有已确认业务约定。
- **刷新生命周期**：PlayerDetailsControl 注册事件后未在该类实现注销；重复 OnStart 或销毁后的运行表现未验收。

## 未知项与验收状态

`UNKNOWN`：等级上限的产品要求、死亡与恢复策略、体力不足时的战斗规则、物品数量边界。上述静态问题没有在本次任务修复，也没有 GamePlayer 复现/回归结果。

相关模块：[运行入口](Runtime.md)、[战斗](Combat.md)、[背包与道具](Inventory.md)。
