# JSON 数据、资源引用与对象池

[返回总导航](../AI_Understanding.md)。本页是当前文件路径、配置数值、资源映射及对象池生命周期的主文档；不把配置快照解释为已确认游戏默认值。

## 入口文件

| 文件 | 作用 |
|---|---|
| [MsgPaths.cs](../../Assets/Scripts/Msg/MsgPaths.cs) | JSON 路径的平台分支 |
| [JsonUti.cs](../../Assets/YTools/ConfigUtil/Json/JsonUti.cs) | Newtonsoft.Json + 同步文件流读写 |
| [PlayerDataStore.cs](../../Assets/Scripts/Features/Player/Data/PlayerDataStore.cs) | 玩家数据 Load / Save 的 IUtility 入口，复用 JsonUti 与既有路径 |
| [Msg.cs](../../Assets/Scripts/Msg/Msg.cs) | 资源路径、对象池名称与事件名称 |
| [FactoryUISystem.cs](../../Assets/Scripts/Factory/FactoryUISystem.cs) | 场景池绑定、ObjectPool 注册、加载、借出、归还、销毁与场景清理 |
| [EditorTest.cs](../../Assets/Scripts/Editor/EditorTest.cs) | 三个重写 JSON 菜单及敌人编辑器入口 |
| [EnemyEditor.cs](../../Assets/Scripts/Editor/EnemyEditor.cs) | 敌人名称/HP 编辑器窗口代码 |
| [DataSetting.cs](../../Assets/Scripts/DataSetting/DataSetting.cs) | 声明 EnemyData 字段；自身没有保存流程 |

## 【FACT】JSON 位置与读写

| MsgPaths.Config 字段 | 已存在的磁盘文件 | 消费者 |
|---|---|---|
| PlayerData | [Data/PlayerData/Player.json](../../Assets/streamingAssets/Data/PlayerData/Player.json) | PlayerDataStore.Load / Save；模型加载与玩家系统统一保存 |
| RecoverItem | [Data/RecoverItem.json](../../Assets/streamingAssets/Data/RecoverItem.json) | ItemBase.Init |
| Enemy | [Data/Enemy.json](../../Assets/streamingAssets/Data/Enemy.json) | EnemyBase.Init |

源码构造的路径不带扩展名；JsonUti 在输入末尾不是 `.json` 时追加扩展名。

- UNITY_EDITOR 及其他平台分支：`Application.streamingAssetsPath + "/Data/..."`。
- UNITY_ANDROID 分支：`"jar:file://" + Application.dataPath + "!/assets/Data/..."`。
- JsonUti.ReadFromJson 使用 StreamReader 和 JsonConvert.DeserializeObject；WriteToJson 使用 StreamWriter 和缩进序列化，直接写入给定路径。
- 当前调用未切换到 persistentDataPath；没有在 JsonUti 中实现网络下载、Android jar 内容读取、目录创建或单项异常隔离。
- PlayerDataStore.Load 对缺失 PlayerData、goodsDict 或必需 Coin 键显式抛出数据错误，不创建默认存档。Save 捕获文件写入/序列化异常并记录路径及原异常，返回 false；调用方回滚本次内存改动。原 JSON 形状、字段与存储路径不变。
- 磁盘实际目录拼写为 `Assets/streamingAssets`。目标平台大小写与可写性未经过构建验证。

## 【FACT】当前玩家 JSON 值

这是当前文件内容，不是重开游戏、新建角色或编辑器菜单的已确认初始数值。

| 字段 | 当前值 | 字段 | 当前值 |
|---|---|---|---|
| Name | 小明 | Level | 2 |
| Hp | 50 | UpperHp | 200 |
| Power | 60 | UpperPower | 100 |
| Exp | 572 | Attack | 24 |
| Defence | 8 | Speed | 10 |
| UpperAttack | 24 | UpperDefence | 0 |
| UpperSpeed | 10 | goodsDict.Coin | 480 |
| goodsDict.馒头 | 5 | goodsDict.小块肉 | 40 |

数据结构与数值约束归[玩家](Player.md)。当前 Defence=8、UpperDefence=0、下限=4 的旧数据按已确认规则在加载时只报错并保留数值，不自动修正 JSON 或内存；其余独立操作不因此被拦截。

## 【FACT】敌人与恢复道具配置

[EnemyData.cs](../../Assets/Scripts/Features/Enemy/EnemyData.cs) 是 Code_01.Enemy 下的独立 Serializable struct，Award 仍嵌套其中且 Exp 仍为 int；EnemyBase.data / initData 字段语义不变。Enemy.json 只有 `野猪` 条目：

| 字段 | 当前值 |
|---|---|
| Name | null |
| HP / Attack / Defence / Speed | 100 / 10 / 3 / 5 |
| CostPower | 10 |
| award.Exp / award.Coin / award.GoodsName | 10 / 20 / 小块肉 |

[ItemData.cs](../../Assets/Scripts/Item/ItemData.cs) 是从 ItemBase 抽出的独立 Serializable struct，包含 `changeHp`、`changePower`、`changeLevel`、`changeExp`（long）、`changeAttack`、`changeDefence`、`changeSpeed`、`changeCoin`，以及 changeUpperHp、changeUpperPower、changeUpperAttack、changeUpperDefence、changeUpperSpeed；其余数值字段均为 int。

RecoverItem.json 当前有两个键：

| 键 | 非零字段 | 其余 ItemData 字段 |
|---|---|---|
| 馒头 | changeHp=5、changePower=20 | 均为 0 |
| 活力苹果 | changeHp=10、changePower=10、changeAttack=2 | 均为 0 |

当前表无小块肉效果条目。道具消费路径见[背包与道具](Inventory.md)。

## 【FACT】资源与组件映射

FactoryUISystem.OnInit 不访问场景；MapCanvasControl.Start 将 `ItemParent` 字段传给 BindScene，由该实例创建三个池。Map 场景文本中的该字段已绑定现有 ItemParent 的 RectTransform；序列化来源及第 3 阶段初始化验收范围见[运行入口](Runtime.md)，用户已反馈该阶段正常。

| 池键 | Resources.Load 路径 | 实际 Prefab 与文本核实内容 |
|---|---|---|
| 活力苹果 | Prefabs/Item/活力苹果 | [活力苹果.prefab](../../Assets/Resources/Prefabs/Item/活力苹果.prefab)：根脚本 ItemBase，GUID `f1aabea48a873d04e8cf52e9e8124136`；直接子节点 Btn |
| 野猪 | Prefabs/Enemy/野猪 | [野猪.prefab](../../Assets/Resources/Prefabs/Enemy/野猪.prefab)：根脚本 EnemyBase，GUID `372e18142db430848ba84c60f760c00a`；直接子节点 BtnAttack |
| Goods | Prefabs/Goods | [Goods.prefab](../../Assets/Resources/Prefabs/Goods.prefab)：根按钮、直接子节点 TxtName / TxtNum |

资源路径不带扩展名。Msg 虽还定义馒头、小块肉等名字，FactoryUISystem.BindScene 没有为每个物品名都建池；字典库存与 Prefab 池键不是同一集合。

## 【CURRENT STRATEGY】对象池生命周期

- `_pools`、全部实例登记 `_instances` 和借出集合 `_borrowed` 均属于 Game 已注册的 FactoryUISystem 实例。MapCanvas 与背包缓存该实例，TestController 点击时获取它；现有 GameObject.Release 扩展也转到同一实例。
- BindScene 先清理旧绑定，再接收当前 ItemParent 并创建三个 ObjectPool；不使用 GameObject.Find。缺失必需父节点、未绑定时借出或借出未知池键均显式抛错，不返回兜底对象。
- 首次创建：Resources.Load → Instantiate(parent) → go.name 设为池键 → 道具和敌人调用 IBaseLife.Init；初始化成功后才登记实例。创建或初始化失败时销毁当前半成品并重新抛出原异常。
- 借出：激活对象，敌人额外 InitData，成功后登记为借出；道具已捕获的数据不重新加载。借出回调异常时清除当前实例登记、销毁当前对象并暴露原异常。
- 归还：按实例登记确定所属池，移除借出记录，SetActive(false) 并恢复到绑定的 ItemParent（worldPositionStays=false）；不依赖对象当前名称或父节点。重复归还显式报错；成功归还不再打印“工厂对象池中不存在该物体”。
- 销毁：池达到容量而销毁对象时走销毁回调；借出后格子初始化失败通过 Discard 移除登记并销毁该项。销毁异常记录池名与原始异常，独立条目继续清理。
- ClearScene 以 ReferenceEquals 比较 ItemParent 的绑定来源；ClearPools 先清空绑定，再通过池 Clear 销毁池内对象，最后遍历剩余实例登记销毁尚未归还对象，包括已改挂 Content 的 Goods。已经被 Unity 先销毁的子对象只移除登记，不依赖父子 OnDestroy 顺序。
- MapCanvas.OnDestroy 显式调用 ClearScene；重复调用无副作用。新场景 BindScene 替换为新引用后，旧场景的延迟 ClearScene 不影响新池。第 3 阶段场景池生命周期清单已有用户正常反馈，主线程已判定该阶段通过；验收边界见[运行入口](Runtime.md)。

## 【FACT】编辑器写入入口

EditorTest 声明以下菜单；三个重写菜单直接写入上表对应 JSON，窗口菜单仅打开编辑器：

| 菜单 | 行为 |
|---|---|
| Tools/重写写入ItemJson | 用代码内的馒头、活力苹果条目覆盖道具表 |
| Tools/重写写入EnemyJson | 用代码内的野猪条目覆盖敌人表 |
| Tools/重写写入PlayerDataJson | 用代码内的 PlayerData 覆盖玩家文件 |
| Tools/打开EnemyEditorWindow %e | 打开 EnemyEditor |

重写道具菜单中的活力苹果 changeAttack=0，而当前 JSON 是 2。重写玩家菜单的 Exp=282、Power=100、Hp=200、Attack=10、UpperAttack=10、Coin=100，仅含馒头 5；这些与当前文件快照不同，不能互相替代。EnemyEditor 当前编辑名字与 HP 的临时字段，没有从窗口保存 Enemy.json 的执行代码。

## 【KNOWN ISSUES】静态问题与边界

- 已核实相关机制：[XPAutoSave.cs](../../Assets/YFramework/Editor/AutoSaveScene/XPAutoSave.cs) 通过 InitializeOnLoad 注册 EditorApplication.update，间隔到达且不在 PlayMode 时调用 EditorSceneManager.SaveScene 保存当前活动编辑场景；[AutoSaveSettings.asset](../../Assets/YFramework/Editor/AutoSaveScene/AutoSaveSettings.asset) 当前为 autoSaveScene=1、intervalTime=30、showMessage=0，[AutoSaveWindow.cs](../../Assets/YFramework/Editor/AutoSaveScene/AutoSaveWindow.cs) 提供菜单 YFramework/AutoSaveScene。该机制只保存当前活动编辑场景；已观察到的字段覆盖现象与该机制吻合，但唯一历史覆盖来源仍为 UNKNOWN，不能据本次静态引用留存认定覆盖来源已完整查明。
- Android 分支产生 jar URI，但 JsonUti 仍使用文件流；当前文件读写方式与 Android URI 接入没有在代码中衔接。平台实际运行结果为 `UNKNOWN`。
- 玩家加载失败保持显式错误，保存失败会返回 false；JsonUti 仍直接写原文件，尚无原子替换/备份保证。敌人/道具按名称取不到配置或必需组件缺失时，对象池清理该失败实例并保留原异常；背包仅在独立格子边界记录、跳过失败项，不提供配置或节点兜底。
- 编辑器重写菜单是覆盖写入入口；其数值来源与当前文件不同，执行记录不能被当作玩家运行存档的来源证明。

## 未知项与验收状态

`UNKNOWN`：正式存档/配置职责划分、资源跨场景运行结果、Android 文件访问方案，以及表格到 JSON 的导入链。`Assets/Instructions/Item/ReconveItem.xls` 仅核实存在，未读取表格内容。

第 2 阶段玩家状态与存储改动的实际行为和日志已有用户核对正确的反馈，主线程已结合静态检查判定该阶段通过。第 3 阶段 MapCanvasControl.ItemParent 序列化引用已补齐并经延时复读留存；用户针对该阶段人工 GamePlayer 清单反馈“已确认正常”，主线程结合静态验收已判定该阶段通过，范围见[运行入口](Runtime.md)。AI 未执行上述菜单、逻辑单元测试、GamePlayer PlayMode 或平台构建；该结论不扩展至后续功能或平台验证。
