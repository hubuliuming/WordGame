# 背包展示与道具使用

[返回总导航](../AI_Understanding.md)。本页负责格子生成、背包事件与场景道具；物品数据字段和资源映射归[资源与数据](DataResources.md)。

## 入口文件

- [KnapsackControl.cs](../../Assets/Scripts/Controller/UIController/KnapsackControl.cs)：按 PlayerModel.GoodsDict 生成格子。
- [ItemBase.cs](../../Assets/Scripts/Item/ItemBase.cs)：读取场景道具配置并注册 Btn 点击。
- [UseItemCommand.cs](../../Assets/Scripts/Command/UseItemCommand.cs)：调用 PlayerEventSystem.ChangeAll。
- [DetailInform.cs](../../Assets/Scripts/Controller/UIController/DetailInform.cs)：详情文字与 UseGoods 监听代码。
- [Msg.cs](../../Assets/Scripts/Msg/Msg.cs)：物品名和 UseGoods 字符串常量。
- [Goods.prefab](../../Assets/Resources/Prefabs/Goods.prefab)：通用背包格子；根节点含按钮，直接子节点为 TxtName、TxtNum。

## 【FACT】初始背包生成

由 MapCanvasControl.Start 显式调用 KnapsackControl.OnStart，获取 PlayerModel 后遍历其 GoodsDict.Keys：

1. 跳过键 `Coin`。
2. 对当前物品数量，循环生成数量为 99 的满格，直到剩余数量不大于 99。
3. 为剩余数量再创建一格；没有对剩余为 0 或负数作过滤。
4. 使用 `FactoryUISystem.Get(Msg.ItemName.Goods)` 取格子，将其父对象设为 contextRect，`worldPositionStays=false`。
5. 向 TxtNum 和 TxtName 的 Text 写入数量与物品名，并向格子 Button 添加点击监听。

场景中的 contextRect 指向 Content（fileID 1099784055），绑定来源见[运行入口](Runtime.md)。物品遍历没有显式排序，不能据字典枚举写成固定显示顺序。

## 【CURRENT STRATEGY】格子与容器参数

| 常量或计算 | 当前值/行为 |
|---|---|
| Row | 6 |
| Column | 10 |
| MaxGirdNum | 99，保留源码拼写 |
| needRow | `gridNum / Column + 1`，使用整数除法 |
| 容器扩展条件 | needRow > Row |
| 扩展高度 | 每个新增行增加 150 到 contextRect.sizeDelta.y |

这些是 KnapsackControl 的计算参数，不等于已验收的实际视觉排版。比如 gridNum 恰为 Column 的整数倍时，现有公式仍额外计一行。

## 【FACT】两条使用路径

### 场景中的活力苹果

`ItemBase.Init(itemName) → RecoverItem JSON 同名条目 → Btn.onClick → UseItemCommand(data) → PlayerEventSystem.ChangeAll(data) → gameObject.Release()`。

ItemBase 是 UIBase / IController / IBaseLife 实现。首次创建时读入的数据被按钮回调捕获；取出已存在池对象时只激活，不重新读取该 JSON。

该路径修改玩家属性并回收场景道具对象，没有扣除 PlayerModel.GoodsDict 中同名物品数量。具体属性写入规则见[玩家](Player.md)。

### 背包格子与详情事件

格子点击创建一个全默认值的 `new ItemBase.ItemData()`，再通过 `StringEventSystem.Global.Send(Msg.Register.UseGoods, data)` 发送事件，没有按 goodName 查询 RecoverItem。

DetailInform.OnStart 将 Inform 写到子节点 Text，并在 BtnUse 点击回调里注册 UseGoods 监听。监听回调只转型数据，实际属性调用仍是注释。注册监听发生在点击 BtnUse 时，不是 OnStart 本身收到事件时。

Map 中存在 DetailInform 脚本引用（GUID `8cdd752725b5a6f43907b68aaf6469e3`），但在已检索的 Scripts / Framework / Test 中没有发现显式调用其 OnStart 的位置；YMonoBehaviour 不自动桥接该方法，见[运行入口](Runtime.md)。

## 【KNOWN ISSUES】

- **刷新未完成**：UpdateGoods 保留 TODO 与空循环；OnStart 只建立初始格子。已核实链路没有在数量变化后刷新现有格子。
- **背包使用未完成**：格子未构造真实物品效果，DetailInform 监听未执行属性变更，亦未建立扣减库存的调用。
- **重复初始化**：OnStart 未先清空旧格子或恢复容器高度，CreateGrid 每次都 AddListener；重复调用的运行结果未验收。
- **数量边界**：剩余数量为 0 或负数时也会创建格子。玩家字典允许负数量的来源见[玩家](Player.md)。
- **配置覆盖范围**：当前玩家数据含小块肉，但恢复道具表只有馒头、活力苹果。小块肉是否可使用以及效果是什么为 `UNKNOWN`；不能自行补配。

## 未知项与验收状态

`UNKNOWN`：背包排序、容量、动态刷新策略、道具消耗规则、详情窗口完整交互，以及是否允许堆叠上限之外的业务例外。未读取界面图片；没有 GamePlayer 交互通过记录。
