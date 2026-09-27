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

- **背包使用未完成**：格子未构造真实物品效果，DetailInform 监听未执行属性变更，亦未建立扣减库存的调用。
- **展示数量边界**：旧存档中的负库存只记录错误并跳过展示；新的普通库存修改已拒绝负结果并移除合法归零条目，零增量不变。模型的旧数据报错与写入规则见[玩家](Player.md)。
- **配置覆盖范围**：当前玩家数据含小块肉，但恢复道具表只有馒头、活力苹果。小块肉是否可使用以及效果是什么为 `UNKNOWN`；不能自行补配。

## 未知项与验收状态

`UNKNOWN`：背包排序、容量、道具消耗规则、详情窗口完整交互，以及是否允许堆叠上限之外的业务例外。第 4 阶段已完成 UI 监听、刷新和释放代码的静态落地；人工 GamePlayer 交互验收仍由主线程确认，未读取界面图片。
