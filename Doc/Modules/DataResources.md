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
- 上述正式 JSON 调用未切换到 persistentDataPath；没有在 JsonUti 中实现网络下载、Android jar 内容读取、目录创建或单项异常隔离。独立网络原型使用本页第 4D 节的存储类与路径，不复用这些正式存档调用。
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

## 【CURRENT STRATEGY】配方分类与搜索关联

当前v40/revision43的[配方搜索](MapInventoryRecipeSearch.md)在原分类上按配置名称/操作文案匹配七项配方，复用独立搜索实例与焦点隔离；单项显隐及高度、文本变化清七请求/待确认归专题。材料与工具状态、原输入/事务/存档链保持；关键词仅本绑定内存，不入偏好v3。本阶段待人工，分类已通过仍限v39/revision42原清单，其他旧通过保持原范围。

## 【KNOWN ISSUES】静态问题与边界

- 已核实相关机制：[XPAutoSave.cs](../../Assets/YFramework/Editor/AutoSaveScene/XPAutoSave.cs) 通过 InitializeOnLoad 注册 EditorApplication.update，间隔到达且不在 PlayMode 时调用 EditorSceneManager.SaveScene 保存当前活动编辑场景；[AutoSaveSettings.asset](../../Assets/YFramework/Editor/AutoSaveScene/AutoSaveSettings.asset) 当前为 autoSaveScene=1、intervalTime=30、showMessage=0，[AutoSaveWindow.cs](../../Assets/YFramework/Editor/AutoSaveScene/AutoSaveWindow.cs) 提供菜单 YFramework/AutoSaveScene。该机制只保存当前活动编辑场景；已观察到的字段覆盖现象与该机制吻合，但唯一历史覆盖来源仍为 UNKNOWN，不能据本次静态引用留存认定覆盖来源已完整查明。
- Android 分支产生 jar URI，但 JsonUti 仍使用文件流；当前文件读写方式与 Android URI 接入没有在代码中衔接。平台实际运行结果为 `UNKNOWN`。
- 玩家加载失败保持显式错误，保存失败会返回 false；JsonUti 仍直接写原文件，尚无原子替换/备份保证。敌人/道具按名称取不到配置或必需组件缺失时，对象池清理该失败实例并保留原异常；背包仅在独立格子边界记录、跳过失败项，不提供配置或节点兜底。
- 编辑器重写菜单是覆盖写入入口；其数值来源与当前文件不同，执行记录不能被当作玩家运行存档的来源证明。

## 【FACT】第 4D 阶段网络原型存档格式与路径

[CombatPrototypePlayerSaveData.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveData.cs) 定义版本化存档，[CombatPrototypePlayerSaveStore.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) 是服务端唯一文件读写入口。路径为 `Application.persistentDataPath/CombatPrototype/Players/<PlayerId>.json`；当前 Editor 的根目录实际为 `C:/Users/91611/AppData/LocalLow/DefaultCompany/Code_01`。正式 `Assets/streamingAssets/Data/PlayerData/Player.json` 与 PlayerDataStore 保持原状。

| 字段 | 当前契约与加载检查 |
|---|---|
| Version | 必需整数；写入4，读取1/2/3/4；合法旧版本只在内存迁移 |
| PlayerId | 必需字符串，必须与请求的已验证 ID 按 Ordinal 完全一致 |
| Coin / Experience | 必需整数，范围 0～int.MaxValue |
| Items | 必需数组，允许空数组；每项恰含 ItemName、Quantity |
| ItemName | 必需字符串，非空白、严格 UTF-8 有效且字节数不超过 FixedString64Bytes.UTF8MaxLengthInBytes；同名 Ordinal 重复拒绝整个存档 |
| Quantity | 必需整数，范围 1～int.MaxValue |
| Tools | v2/v3/v4必填，最多两条唯一已知ID；旧项ToolId/Durability两字段，v4项ToolId/Durability/Level三字段；Level整数1～3，耐久0～本级最大值，0合法 |
| InventoryCapacityLevel | v3/v4必填整数1～3；v1/v2迁移为Lv1，v3原等级保留，按固定ID恢复 |

v1根五字段、v2含Tools共六字段、v3/v4含InventoryCapacityLevel共七字段；Items项始终两字段，Tools项在v2/v3两字段、v4三字段。缺失/未知/重复字段、错误类型（浮点/数字字符串）、坏版本/身份/数值整档拒绝。严格UTF-8支持BOM，写无BOM；合法v1/v2/v3完整校验后仅内存迁移v4：v1 Tools空、v2/v3原Tools补Level1且耐久保持，v1/v2容量Lv1、v3原容量等级保持。读取不写盘、不赠材料/工具/耐久，下一正常保存写v4，不批量重写或修正坏档。[背包升级](MapInventoryCapacityUpgrade.md)用户通过限v20/23十六项；[工具升级](MapGatherToolUpgrade.md)静态及用户人工通过，限v21/revision24二十二项，未触发用例UNKNOWN。

第 6A 玩家生命、上限、受击序号和死亡标记均不加入该 JSON，体力也不持久化；重新生成沿既有玩家 Baker 初值初始化生命与体力，金币/经验/背包仍按固定 ID 恢复。生命职责归[玩家](Player.md)，原 Prefab 新参数与实际烘焙归[运行入口](Runtime.md)。

## 【FACT】第 4D 阶段开发 ID 配置

[CombatPrototypeDevelopmentIdentity.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeDevelopmentIdentity.cs) 优先读取进程参数 `-combatPrototypePlayerId <ID>`，该参数只允许出现一次，并对当前进程内的客户端 World 生效。

无该参数时，Editor 读取 [UserSettings/CombatPrototypeDevelopmentIdentity.json](../../UserSettings/CombatPrototypeDevelopmentIdentity.json)：Version=1、Clients 为按 World.Name 精确匹配的对象，当前明确配置 `ClientWorld: player-a`；多个 World 使用该文件时须分别列出各自的明确 ID。

独立第二进程可声明 `-combatPrototypePlayerId player-b`；非 Editor 缺参数或 Editor 缺配置项时显式报错并断开，不随机分配或回退。

[CombatPrototypePlayerIdentity.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerIdentity.cs) 在客户端配置与服务端 RPC 两个输入边界使用同一规则：ID 长度 1～32，仅小写 ASCII 字母、数字、下划线、连字符；拒绝 con/prn/aux/nul 与 com1～com9、lpt1～lpt9 等保留文件名。该限制使各 ID 的文件名固定且不含路径分隔符；固定 ID 只用于开发存档，不是账号认证。

## 【CURRENT STRATEGY】第 4D 阶段读取与写入边界

服务端以 FileMode.Open 读取正式文件，只把 FileNotFoundException 或 DirectoryNotFoundException 认作首次无档，采用已确认的0/0、空库存、空工具与容量Lv1；权限、I/O、解码、JSON 或字段校验错误交回握手边界记录并拒绝当前玩家，原文件保留。读取只认正式 `.json`，失败遗留的 `.json.tmp` 不作为可恢复存档。

每个在线击杀奖励先准备完整最终金币/经验、目标物品及必要缓冲容量，再把当前库存、Tools、实际容量等级及目标最终值投影为一个v4 JSON候选，Tools包含当前Level。存储类序列化该候选，在同一目录写 `<PlayerId>.json.tmp`、Flush(true)，有旧正式文件时 File.Replace，无旧文件时 File.Move；服务端运行时按该固定路径创建 Players 目录。只有保存函数成功返回后才修改三项 ECS 状态，失败时旧正式文件与旧玩家数值保持，事件消费与后续隔离见[战斗](Combat.md)。

玩家存档没有另外的备份文件、定时保存、断线补存或退出保存；准入恢复由[玩家](Player.md)维护，原型背包规则由[背包与道具](Inventory.md)维护。AI 静态核对未调用读写存档的业务方法，实际保存与恢复由人工 GamePlayer 验收确认。

## 【KNOWN ISSUES】第 4D 阶段存储验收

四份脚本已由 Unity 编译并加载；文件路径、JSON 校验、临时文件替换与奖励提交顺序已静态核对。用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过；存储范围覆盖多人独立文件、重连/重启恢复、重复身份与坏档拒绝、I/O 失败保留旧档且不部分到账，完整边界见[运行入口](Runtime.md)。本规则仅覆盖当前开发原型的单服务端本地文件写入，不包含正式账号、跨服务器协调、掉电恢复保证、迁移或平台构建验收；AI 未运行逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 5A 阶段存档耗时入口

当前存储类在 Load 与 SavePrepared 入口提供 Profiler 标记，文件契约与原提交顺序保持。标记计量边界、单位、调用次数与待采集结果统一归[性能基线](Performance.md)；标记编译完成不表示实际存档耗时已测得。

## 未知项与验收状态

`UNKNOWN`：正式存档/配置职责划分、资源跨场景运行结果、Android 文件访问方案，以及表格到 JSON 的导入链。`Assets/Instructions/Item/ReconveItem.xls` 仅核实存在，未读取表格内容。

第 2 阶段玩家状态与存储改动的实际行为和日志已有用户核对正确的反馈，主线程已结合静态检查判定该阶段通过。第 3 阶段 MapCanvasControl.ItemParent 序列化引用已补齐并经延时复读留存；用户针对该阶段人工 GamePlayer 清单反馈“已确认正常”，主线程结合静态验收已判定该阶段通过，范围见[运行入口](Runtime.md)。AI 未执行上述菜单、逻辑单元测试、GamePlayer PlayMode 或平台构建；该结论不扩展至后续功能或平台验证。

## 【FACT】第 7A 阶段消费候选与存档兼容

存储类增加 PrepareItemConsumption，将当前完整库存、当前Tools和目标扣除后的数量投影为同一v2候选；目标归零时省略该项，其他条目及顺序保持，允许消费最后一项后 Items 为空数组。原PlayerId/Coin/Experience/Items字段契约与严格加载检查保持；当前写v2，所有奖励/消费候选保留Tools，工具字段归[采集工具](MapGatherTools.md)，CurrentPower/UpperPower 仍不入 JSON。

## 【CURRENT STRATEGY】第 7A 阶段消费保存

服务端物品使用系统先准备候选，再调用原 SavePrepared，只有成功返回后才修改库存和体力；保存失败时该使用操作两项均保持，旧正式文件保留，继续后续玩家。没有新增文件路径、迁移、定时保存、断线补存或其他写盘入口。消费规则由[背包与道具](Inventory.md)维护，用户已确认第 7A 运行验收通过，范围归[运行入口](Runtime.md)。

## 【FACT】Editor 启动配置文件与字段

[CombatPrototypeStartupSettingsStore.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeStartupSettingsStore.cs) 是原型 Editor 启动配置的唯一读写入口，文件位于 [UserSettings/CombatPrototypeStartupSettings.json](../../UserSettings/CombatPrototypeStartupSettings.json)。根对象恰含六个字段，JSON 重复属性、缺失/未知字段、错误类型、不支持版本、非法模式/角色/地址/端口和联机关闭后台运行均拒绝；读取使用严格 UTF-8 并支持 UTF-8 BOM，写入为 UTF-8 无 BOM。

| 字段 | 当前文件值 | 校验及用途 |
|---|---|---|
| Version | 1 | 必需整数，仅支持 1 |
| GameMode | Online | 精确字符串 SinglePlayer / Online |
| OnlineRole | Host | 精确字符串 Host / Client / Server；单机连接不使用该角色 |
| ServerAddress | 127.0.0.1 | 必需合法 IPv4，拒绝 0.0.0.0；只有 Online/Client 使用该连接地址 |
| Port | 7979 | 必需整数 1～65535；单机使用固定 IPC 通道 7979 |
| RunInBackground | true | 必需布尔；Online 必须为 true，SinglePlayer 可配置 |

当前值是本轮建立的开发启动设置，不是正式产品模式或发布默认值。全部字段均执行输入校验，即使当前模式不使用其中的连接字段；保存后由 Bootstrap 读取一次形成不可变快照。读取缺失或坏文件直接失败，不创建默认文件、不修正字段、不迁移版本，也不读取失败遗留的 .tmp 文件。

## 【CURRENT STRATEGY】Editor 配置保存与既有数据边界

窗口“保存并校验”将已验证配置序列化，在同一 UserSettings 目录写 .json.tmp 并 Flush(true)，已有文件时 File.Replace，无旧文件时 File.Move。失败保留显式错误；UserSettings 目录须已存在，不为必需目录增加运行时兜底创建。窗口候选值与磁盘文件明确区分，进入 PlayMode 后禁用保存/重读。

[.gitignore](../../.gitignore) 仅新增 /UserSettings/CombatPrototypeStartupSettings.json 的忽略规则；其他 UserSettings 文件的管理方式保持。没有启动设置文件的开发环境可在该窗口明确保存候选值建立配置，启动入口自身不自动补配置。

开发身份仍使用原 -combatPrototypePlayerId 优先及 CombatPrototypeDevelopmentIdentity.json 按 World.Name 读取规则。窗口只展示实际来源并提供文件定位，不复制或改写 PlayerId。原服务端存档路径、格式、固定 ID 契约以及正式 PlayerDataStore JSON 保持。生命、体力、攻击等玩法数值仍保存于各 Authoring 所属资源；地图配置来源提供尺寸、布局、出生原点与敌人总量，原 Spawner 保留列数/间距，两者沿烘焙链生效，配置契约归[战斗地图](Map.md)，均不加入启动 JSON。

## 【KNOWN ISSUES】Editor 启动配置验证范围

文件、字段、UTF-8 和源码边界已静态核对，新脚本由 Unity 编译；坏配置的实际启动表现、端口占用、保存失败与重复模式切换仍待人工 GamePlayer 验证，保持 UNKNOWN。本轮未调用玩家存档 Load/SavePrepared，不改变已有第 4D/7A 存档人工通过范围。

## 【FACT】网络玩家美术资源入口

玩家 Ghost 的原 ClientPrefab 继续引用 CombatPrototypeNetworkPlayerView；其中 VisualRoot 引用新增灰衣修士模型，角色设定图、网格/材质、动画控制器/Clip/遮罩和预览的路径及用途统一见[玩家美术资源表](../PlayerArt.md)。新资源及脚本 meta 由 Unity 正常导入生成，既有 GUID、Bundle、包与构建配置保持。

## 【FACT】网络敌人美术资源入口

敌人 Ghost 的 ClientPrefab 绑定既有 CombatPrototypeNetworkEnemyView，ServerPrefab 为空；View 的 VisualRoot 嵌套腐化荒猪模型，双方原根占位 Renderer 关闭。设定图、独立网格/材质、单层控制器、七个 Clip 与采样预览位于 Assets/Art/Enemy/CombatPrototype/，用途与规格归[敌人美术资源表](../EnemyArt.md)。现有 Prefab/脚本 GUID 保持，新脚本及资源 meta 由 Unity 正常导入生成；没有更改 Bundle、包或构建配置。

## 【FACT】网络战斗地图配置与资源

Assets/Scripts/CombatPrototype/Map/包含四类配置、地图子段MapResourcePersistenceConfig/MapMovementConfig/MapDropConfig/MapTreeHarvestConfig/MapMiningConfig/MapGatherToolsConfig/MapInteractionHudConfig/MapPickupHudConfig/MapInteractionHighlightConfig/MapResourceStatusHudConfig/MapWorldSaveHudConfig/MapInventoryPanelConfig/MapInventoryCapacityConfig及条目/MapInventoryDropConfig/MapGatherToolDurabilityHudConfig/MapInteractionFailureHudConfig/MapGatherOutcomeHudConfig/MapPickupFeedbackHudConfig、配置集合/来源、严格JSON读取与校验、Authoring/Baker、地图显示与共享移动工具。SubScene的SourceMode=Json，显式绑定两份地图JSON及三份共享数组；材质/物体通过GroundMaterials/DecorationPrefabs稳定键绑定。契约和来源归[地图](Map.md)：当前schemaVersion=40/configRevision=43，必填resourcePersistence/movement/drops/treeHarvest/mining/gatherTools/interactionHud/pickupHud/interactionHighlight/resourceStatusHud/worldSaveHud/inventoryPanel/inventoryCapacity/inventoryCapacityUpgrade/inventoryDrop/gatherToolUpgrade/gatherToolDurabilityHud/interactionFailureHud/gatherOutcomeHud/pickupFeedbackHud；生态含treeObjectId/gatherObjectId/mineObjectId/mineDensityPer100m2，物体含tree_normal/gather_apple/mine_rock。JSON在烘焙时成为固定ECS配置/布置及Disabled阻挡缓冲，相关Baker登记内容依赖；不增加玩家/敌人Prefab序列化字段，无外部配置运行加载或热重载；世界资源写盘归独立存档链。

新资源位于 Assets/Art/Map/CombatPrototype/ 与 Assets/Prefabs/CombatPrototype/Map/，包括 8 个材质、6 个网格和 9 个物体 Prefab；草丛/碎石仍为静态，树木在砍伐关闭时使用原静态资源，GatherApple.mat、GatherApple.asset、GatherApple.prefab 为新单根插值 Ghost 采集资源。新增资源和脚本 meta 由 Unity 导入生成；既有玩家/敌人资源、旧 meta、Bundle、包与构建设置保持。地表运行网格及静态装饰由客户端地图显示系统拥有和清理，开启砍伐/采矿时树木/矿点 Ghost 分别由服务端 TreeSpawn/MineSpawn 拥有和清理，共享资源不随地图根实体释放而销毁。砍伐开启时树木为 Ghost，阻挡数据仍随地图根存在；玩家预测与敌人移动读取同一结构，按权威砍倒时刻更新 Disabled。服务端采集复用 PrepareReward → SavePrepared 后再提交库存及耗尽，按烘焙的 regrowEnabled/regrowSeconds 记录仅服务端 RegrowAt；到期只复用原 Ghost 恢复 Available，不发物品或写盘。默认 gather_apple 启用 600 秒再生，vitality_apple 显式映射活力苹果，不新增存档字段或修改旧存储类。玩家金币/经验/完整库存及Tools保存为v2，读取v1迁移；该采集/再生阶段的地图耗尽/期限不保存，重启从Available重建；当前世界档开启时恢复耗尽与剩余期限。产出/失败规则归 [背包与道具](Inventory.md)，用户已确认第四阶段人工 GamePlayer 通过，主线程结合既有静态核对判定该阶段通过；数据范围限采集保存失败不入包/不耗尽、旧正式档保留、恢复存储后重新采集、原固定 ID 库存恢复与服务端重启资源重建，完整边界归 [运行入口](Runtime.md)。第五阶段只新增独立再生系统脚本及 Unity 生成的 meta，沿用原资源与显式绑定；再生配置烘焙已静态核对；用户已确认第五阶段人工 GamePlayer 通过，主线程结合既有静态核对判定该阶段通过，范围限再生不重复入包/写盘、关闭与失败/取消分支、同局期限及服务端重启按既有规则重建，完整边界归运行入口。存档耗时、平台和线上验证仍为 UNKNOWN。

## 【FACT】地图掉落配置、资源与存档边界

第六阶段在两份地图 JSON 增加必填 drops 段，BuiltIn 值一致。原 DecorationPrefabs 追加 drop_apple → [DroppedApple.prefab](../../Assets/Prefabs/CombatPrototype/Map/DroppedApple.prefab)，该新单根插值 Ghost 复用 GatherApple.asset/.mat，没有创建或修改网格/材质；原主场景、玩家/敌人及种植采集 Prefab、Animator、旧 meta、包与构建设置保持。新脚本和 Prefab 的 meta 由 Unity 导入生成。完整字段、Editor 入口、资源及状态归[掉落与拾取](MapDrops.md)。

当前沿原存储类写v2，候选保留Tools并兼容读取v1。拾取沿 PrepareReward → SavePrepared 成功后提交库存及 Consumed，金币/经验不变；未拾取的 DropId、位置、飞行/落地/到期期限均不写盘，服务端重启清空地面掉落，已经入包的苹果随当前玩家库存恢复。用户已确认第六阶段人工 GamePlayer 通过，主线程结合静态核对与用户反馈判定该阶段通过；存储范围限成功保存、失败保留旧档/库存/未到期掉落及固定 ID 重连/重启恢复，完整边界归[运行入口](Runtime.md)第六阶段九项清单。同步写盘耗时、规模性能、平台构建与线上联调仍为 UNKNOWN；原第四/第五阶段通过范围保持。

## 【FACT】树木砍伐资源与存档边界

新增 HarvestableTree.prefab 复用 TreeNormal 网格/材质；新增 DroppedWood.prefab 与程序生成的 DroppedWood.asset/.mat，原 DecorationPrefabs 仅追加 tree_harvest/drop_wood 两个引用。新资源/脚本 meta 由 Unity 导入生成，旧资源与旧 meta 保持。资源路径、Ghost 字段、Editor 创建/绑定及生命周期归[树木砍伐](MapTreeHarvest.md)。

TreeState 保留四个 Ghost 字段，另以阻挡转换历史同步各次砍倒/再生的权威 tick 和 Disabled，供客户端预测重建；当时不写世界档，树木/地面木材随本局释放，重启恢复原布局Standing；当前世界档规则见资源存档。wood 显式映射“木材”，成功G后沿现有库存保存/恢复，候选保留Tools、写v2/读取v1迁移；正式档替换规则保持。静态引用/烘焙已核对；用户已确认第七阶段人工 GamePlayer 通过，主线程结合静态核对判定该阶段通过，数据范围限木材保存/失败保留、双端/晚加入、固定 ID 重连及重启恢复，完整边界归[运行入口](Runtime.md)第七阶段十项清单。人工结论来自用户反馈，未实际触发的独立用例与存档耗时、性能、平台和线上验证仍为 UNKNOWN。

## 【FACT】树木再生配置与历史缓冲

objects.tree_normal 与 BuiltIn 复用 regrowEnabled=true/regrowSeconds=600；树木再生复用原字段，第八阶段版本为 v5/revision=7，当前整组契约见地图配置节。Map Baker 复制到 TreeSettings，TreeProgress 增加仅服务端 RegrowAt；原 TreeAuthoring 烘焙空 CombatPrototypeMapTreeBlockingEvent 缓冲，每条 TransitionTick/Disabled 两字段通过 Ghost 同步，内部容量 4，同局历史随轮次增长并随树木实例释放。新 TreeRegrowSystem 在服务端处理到期、占位和恢复；原资源/Prefab/Scene/Animator/旧 meta 与存储类保持，只新增系统脚本及 Unity 生成的 meta。该阶段重启按原布局生成Standing、空历史及零期限；已入包木材沿原 v1 恢复。编译、历史 Serializer/Snapshot 和两种模板隔离烘焙已静态核对，用户已确认第八阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定该阶段通过，数据范围限配置/开关、多轮同步、晚加入/重启与释放，完整边界归[运行入口](Runtime.md)第八阶段八项清单及 v5/revision=7。人工结论来自用户反馈；未触发的独立时序/失败用例及历史内存/网络性能仍为 UNKNOWN。

## 【FACT】采矿配置、资源与存档边界

MapMiningConfig 为必填 mining 段，默认 enabled=true、mine_rock、3 秒、stone ×3、drop_stone；生态 mineDensityPer100m2 为 grassland/forest/rocky 的 0.1/0.2/1，矿点占地 0.75、间距 2.5、交互 2 米、只阻挡移动，复用物体再生 true/600 秒。新增 MineableRock.asset/.mat、MineableRock.prefab、DroppedStone.prefab；两个单根插值动态 Ghost 共用新灰色材质/网格，带 LinkedEntityGroupAuthoring，无 Owner/AutoCommandTarget/Collider/Animator。只在原 DecorationPrefabs 追加 mine_rock/drop_stone，旧资源、旧 meta、Scene 根/组件关系保持。

MineState 同步 PlacementIndex/Phase/CollectorNetworkId/MinedTick 四字段，MineSettings/Progress 仅服务端，含再生开关/间隔及 RegrowAt；矿点 Baker 烘焙空 MineBlockingEvent，TransitionTick/Disabled 两字段 Ghost 同步、内部容量 4，本局全部轮次保留并随实例释放。有效矿点在初始化完成后才登记 MineSpawn 所有权，石材由原 DropSpawn 拥有和清理，共享资源不随实例销毁。stone 映射“石材”，原存储类写v2、读取v1迁移，G候选保留Tools并保存/恢复库存；该阶段矿点耗尽、进度与地面石材不写盘，重启按原布局恢复Available、空历史及零期限；当前资源档开启时恢复耗尽/期限，原预约仍不保存，地面掉落按当前持久化开关恢复。资源/配置/生命周期归[采矿](MapMining.md)，第九阶段人工跨端/保存失败/重启/清理为 UNKNOWN，归[运行入口](Runtime.md)。

## 【FACT】统一 F 资源交互的配置边界

F 复用原 Gather 输入，HarvestTree/Mine 字段保留但 H/J 停止触发/消费。服务端统一入口跨三类选目标，继续使用各类型既有距离、耗时与产出 JSON 字段；当前为schemaVersion=40/configRevision=43，原距离/耗时/产出接口、来源/烘焙、资源绑定及原玩家Items契约保持，当前玩家存档写v4且保留Tools。统一 F 本身只增加两个交互脚本与 meta；矿点再生复用物体配置字段并增加 ECS 历史缓冲。当前[交互显示](MapInteractionHud.md)新增五个脚本/meta、地图显示配置及玩家所属 HUD 快照，主场景 Main Camera 仅追加一个 HUD 组件；Prefab/Animator与旧meta保持；存储类工具迁移/候选归[采集工具](MapGatherTools.md)。规则归[地图](Map.md)；人工交互/保存回归已获用户通过反馈，结论限[运行入口](Runtime.md)统一 F 八项清单，未触发的独立用例仍为 UNKNOWN。HUD 用户人工通过限 v7/revision=10 及运行入口对应八项清单，未实际触发的独立配置/烘焙/运行失败仍为 UNKNOWN。

矿点再生配置、仅服务端字段、历史 Ghost Serializer 及八次隔离烘焙已静态核对，默认 true/600 秒，关闭再生 false/600 和关闭采矿的烘焙结果一致。新 MineRegrowSystem 脚本及 meta GUID=9da8de47253e52f4ba3b80983082bcc3 由 Unity 导入；MineState 四字段保持，新增缓冲改变矿点烘焙后的 Ghost 布局，各端须使用同版代码、配置及重新烘焙的数据。用户已确认矿点再生人工 GamePlayer 通过，资源范围限[运行入口](Runtime.md)矿点再生八项清单及 v6/revision=9，规则归[采矿](MapMining.md)；未实际触发的晚加入/预测时序/独立失败分支仍为 UNKNOWN，历史内存/网络开销未测量。

## 【FACT】采集工具的数据与保存接入

gatherTools为必填地图段，当前v22/revision=25的Lv1默认斧头60/木3石2、镐子40/木2石3、成本1/倍率0.75，详细字段归[采集工具](MapGatherTools.md)。玩家准入先完整校验旧库存与Tools，再实例化/恢复；各个奖励、E、F植物、G、制作及工具完成候选都包含Tools，仍由SavePrepared同一路径替换正式档。使用工具的树木/矿点完成先保存耐久后提交资源，徒手不新增工具写盘；该工具阶段世界资源/掉落/期限不保存；当前资源状态持久化见资源存档。正常编译/所属Serializer/十次隔离烘焙已静态核对；用户确认工具人工通过限v8/revision=11及[运行入口](Runtime.md)十二项清单；未实际触发的独立v1迁移/坏Tools/候选保存/故障恢复用例仍为UNKNOWN，旧存储用户通过仅限原版本/清单。同步写盘耗时/性能和文件替换后意外ECS异常仍未验证。

## 【FACT】材料面板配置与数据边界

v9面板阶段：inventoryPanel必填25字段，默认true/初始关闭、380×640/右24/上64/字号18/行32及17个英文文案；严格字段/类型、几何与61UTF-8字节文案校验，Json/BuiltIn为v9/revision12，旧地图v1～v8明确失败，不补段或回退来源。当时四个职责脚本及Unity正常生成meta，Map Baker只追加固定显示Settings，未改变玩家Ghost/输入或存储类；v10丢弃布局变化见下段。详细契约归[制作面板](MapInventoryPanel.md)。编译、导入/类型与十四次隔离Editor烘焙静态通过，用户确认面板人工通过限v9/revision12及[运行入口](Runtime.md)十二项，未实际触发的独立配置/创建/准备/保存/提交/清理失败仍UNKNOWN。

v10/13接入的[背包丢弃](MapInventoryDrop.md)复用原PrepareItemConsumption/SavePrepared投影完整Items/Tools候选，先创建Prepared并重取引用，保存成功才扣库存/激活掉落；存储类、v2契约、v1迁移及路径替换不变。四新脚本/meta与地图根Settings/定义、玩家所属反馈和输入三字段已导入，现有三个掉落Prefab及资源绑定保持。地面掉落仍不写盘，重启丢失未拾取物且不恢复已保存扣减；保存成功后意外ECS故障恢复未知。编译/16次隔离烘焙静态通过，用户确认丢弃人工通过限[运行入口](Runtime.md)v10/13十二项，未触发的独立配置/创建/保存/提交/清理失败仍UNKNOWN。

当前v23/revision26必填[G提示](MapPickupHud.md)pickupHud18字段：true/400×84/底168/字号20、原四文案、Not enough space和寿命true/预警true/30秒、Expires in/Permanent/Expiring soon/s/#FFB454；沿原严格字段/类型、有限尺寸、文案与新增双行高度/颜色校验，旧v1～v22拒绝，无补默认/回退或热重载。Baker写18Settings，玩家原所属G显示由四变六，增加LifetimeMode及无量化RemainingSeconds；两新普通助手/meta沿原G目标、Main Camera宿主/绑定与Repaint显示，资源状态底距236改268。当前19输入，F/资源状态各4、Drop Ghost4、世界v2及资源绑定/G原保存到期保持；玩家v4/合法v1/v2/v3内存迁移归本节存储契约。v11/14原文字十项用户通过保持旧范围；寿命编译/实际Serializer反射/26次隔离Editor烘焙静态通过，用户人工通过限v17/revision20十二项，未触发独立用例UNKNOWN，清单归[运行入口](Runtime.md)。

当前v23/revision26必填[interactionHighlight](MapInteractionHighlight.md)14字段，默认主/F/G开启、采集/树/矿/掉落半径0.65/0.9/0.9/0.45米、线宽3/48段、黄/绿/蓝色、透明度0.9/地面偏移0.03米。关闭仍严格校验；旧v1～v22拒绝，不补段或回退。五新职责脚本及Unity正常生成meta，原Map Baker只追加固定Settings，原输入/F/G显示字段及资源/存储契约保持。v12/15编译及14次隔离Editor烘焙静态通过，用户确认高亮人工通过限v12/15十项，未触发独立用例UNKNOWN。

当前v23/26必填[resourceStatusHud](MapResourceStatusHud.md)11字段：true/400×52/底268/字号20和六文案；严格形状/类型、有限尺寸、字号容纳、G间隔16与61 UTF-8字节文案校验。五职责脚本及Unity生成meta、地图Settings和所属四字段已接入，无新输入或存档字段，原F/G与资源/再生/掉落契约保持。v13/16编译、Serializer/SendToOwner及14次隔离烘焙静态通过，用户确认人工通过限v13/16十项，未触发用例UNKNOWN；旧通过保持原版本/清单。

v23/revision26修理阶段的[工具修理](MapToolRepair.md)必填gatherTools.repairEnabled=true/repairFeedbackSeconds=2及每工具恢复20/15、木1石1；修理新增Repair/Repair/Full durability三文案；修理阶段面板30字段；当前面板107字段/73文案、三模式ID及一偏好文件ID，排序筛选归[展示](MapInventoryListView.md)，容量文案归[容量](MapInventoryCapacity.md)。四新脚本及正常生成meta、原工具Settings/Definitions和Player零所属修理反馈已接入，该v23阶段输入19、F4/G6/资源状态4字段；PrepareToolCraft候选投影和SavePrepared复用，当前候选写v4并保留工具/容量Level，合法旧档迁移归本节契约；路径/工具槽/世界状态保持。编译/Serializer/18次隔离Editor烘焙静态通过；用户确认修理人工通过，限v14/17及[运行入口](Runtime.md)十二项；未触发独立保存/恢复失败仍UNKNOWN，旧阶段通过保持原版本/清单。

## 【FACT】地图资源状态持久化

当前v23/26必填resourcePersistence，默认true/default_world/10秒及saveGroundDrops=true；七个职责脚本及Unity正常生成meta，Map根七字段Settings/一字段恢复状态和原准入门已接入。世界资源/掉落写v2、读v1迁移，存于`persistentDataPath/CombatPrototype/Worlds/<saveSlotId>/<mapDefinitionId>.resources.json`，根九字段/耗尽条目四字段/掉落条目八字段，按槽/地图/seed/资源布局与再生规则签名整体校验；ConfigRevision仅记录。严格UTF-8/JSON、只读正式档及.tmp/Flush(true)/原子替换规则归[资源存档](MapResourcePersistence.md)。

耗尽/砍倒与剩余秒数恢复，离线暂停；预约/工作进度清空，树/矿重建本次阻挡基态。状态变更、10秒检查点和关闭保存独立于原玩家先保存后提交链；世界写失败保留旧档、继续本局并在下个保存点重试，不回滚已成功采集。玩家写v4/合法旧档内存迁移，候选保留工具/容量等级；掉落开关开启时恢复未到期地面物，无跨文件事务或同槽多服务端并发保证。资源存档v15/18阶段正常编译/反射和18次隔离Editor烘焙静态通过，用户确认人工通过限v15/18及[运行入口](Runtime.md)十二项；未实际触发的独立I/O/坏档/替换/恢复/生命周期用例，以及跨文件/ECS、同槽并发、性能/带宽/平台/线上仍UNKNOWN。掉落存档用户人工通过限v16/19、世界v2及十二项，未触发用例UNKNOWN，见[掉落存档](MapDropPersistence.md)。

地图v18/revision21的[F5/保存提示](MapWorldSaveHud.md)新增resourcePersistence.manualSaveEnabled=true/全局冷却5秒及worldSaveHud16必填字段；持久化配置6/Settings7、HUD默认400×84/底336/字号20/反馈3秒/#FF6B6B。关闭仍严格校验，原资源签名、世界根9/掉落条目8、玩家/世界v2与合法v1读取保持。编译/字段/26次隔离烘焙静态及用户人工通过，限v18/revision21十二项，未触发独立用例UNKNOWN。

## 【FACT】材料容量配置与旧存档

[容量](MapInventoryCapacity.md)根3字段/条目2字段，默认true/总量300/三种各200；F10/G18/B30配置与Settings，新增四脚本/meta及原根2字段Settings/三条3字段Definition。原玩家库存、Tools、世界v2格式/路径与资源签名保持，合法旧超限库存仍按固定ID加载；限制仅在新入包前生效，不能截断或删档。编译/18次隔离Bake静态通过，用户确认人工通过限v19/22十六项，未触发用例UNKNOWN，见[运行入口](Runtime.md)。

## 【FACT】容量等级的候选保存边界

[容量升级](MapInventoryCapacityUpgrade.md)新增PrepareCapacityUpgrade，完整扣料/等级候选仍先SavePrepared后提交；原奖励、E、植物F、G、制作、修理、丢弃、树木/矿点工具完成九入口均携带当前Level，避免覆盖为Lv1。世界仍v2、签名/路径保持；用户人工通过限v20/revision23升级十六项，未触发的独立迁移/坏档/保存失败与保级用例仍UNKNOWN，旧存储通过保持原版本/清单。

## 【FACT】工具等级、有效定义与v4候选

[工具升级](MapGatherToolUpgrade.md)新增必填gatherToolUpgrade根11字段、四条6字段定义；地图当前v40/revision43。原ToolId/Durability缓冲追加Level，三GhostField所属同步，升级反馈另有Sequence/Kind/Result三字段；输入新增6/7至19字段。原准入严格校验旧工具二字段/v4三字段后恢复等级；ForLevel按实际级取上限/倍率，修理与HUD共用有效最大值，升级保留绝对耐久、重做显式满Lv1。所有ProjectTools分支及完整候选保存工具Level和实际容量Level，世界v2/资源签名与路径保持。编译/生成Serializer、136份非法配置拒绝及22次隔离Editor Bake静态通过；用户确认人工GamePlayer通过限v21/revision24二十二项，未触发独立用例UNKNOWN；原269项及旧用户通过保持，完整当前契约与人工边界归[专题](MapGatherToolUpgrade.md)/[运行入口](Runtime.md)。

## 【FACT】采集工具耐久预警配置

地图schemaVersion22/configRevision25新增必填[gatherToolDurabilityHud](MapToolDurabilityHud.md)11字段，Json/BuiltIn默认值一致；沿原严格UTF-8/JSON缺失/未知/重复字段/类型校验，阈值有限且0<criticalRatio<warningRatio<1、颜色严格#RRGGBB、五文案非空白/无控制字符/最多61 UTF-8字节，关闭仍验证，旧地图v1～v22拒绝。原Baker在唯一地图根写11字段固定Settings，不由Ghost同步；绑定→HUD已验证工具快照→普通只读投影→F/B显示。三新普通脚本及Unity正常生成meta，原Scene/SubScene/Prefab/Animator/旧meta/资源/包/构建保持。输入19、唯一工具ToolId/Durability/Level三字段SendToOwner，F4/G6/资源状态4/世界保存3、玩家写v4/根7/Tools项3与世界v2/根9/掉落项8均保持。编译/元数据、240份非法配置拒绝与28次隔离Bake静态通过，布局/签名保持；用户确认预警人工GamePlayer通过，限v22/revision25十六项，未触发的独立字形/排版/联网/生命周期分支UNKNOWN。

## 【FACT】统一F失败显示配置与同步

地图v23/revision26新增必填[interactionFailureHud](MapInteractionFailureHud.md)九字段，Json/BuiltIn默认true/2秒/#FF6B6B及六文案；NoSpace沿原interactionHud.noSpaceLabel。严格形状/缺失/null/未知/重复键/类型/UTF-8与正有限时长/#RRGGBB/61字节文案校验，关闭仍验证，旧地图v1～v22拒绝，无默认补齐、来源回退或热重载。原地图根写九字段固定Settings，无Ghost；原Player Baker另加Sequence0/ResultNone二字段SendToOwner反馈。四新普通脚本与Unity生成meta，原F服务/绑定/HUD接入；仅请求启动失败显示，不写存档或改变保存调用。输入19、Tools3、F4/G6/资源状态4/世界保存3，玩家v4根7/Tools项3及世界v2根9/掉落项8保持。编译/实际生成Serializer、224份非法配置拒绝与32次隔离Bake静态通过，原布局/资源签名保持；用户确认本阶段人工GamePlayer通过，限v23/revision26十六项；未触发的独立显示/错误隔离/联网/生命周期用例UNKNOWN。

## 【FACT】采集完成与中断配置及所属同步

当前地图v26/revision29必填[采集结果](MapGatherOutcomeHud.md)gatherOutcomeHud十三字段，Json/BuiltIn默认2秒、完成绿/中断橙/失败红及八文案；NoSpace沿原F容量文案。原严格读取与固定Settings映射保持，关闭仍验证，旧地图v1～v25拒绝，无补齐/回退/热重载。原Player Baker追加Sequence0/Kind0/ResultNone三字段SendToOwner最新反馈；各端须同版重新烘焙。五新普通脚本/meta，原资源引用/布局/兼容签名、输入及玩家v4/世界v2存档保持。静态及用户人工通过，限v24/revision27十六项，未触发独立用例UNKNOWN；旧v23启动失败通过保持原清单。

## 【FACT】拾取结果配置与所属快照

当前地图v26/revision29必填[拾取反馈](MapPickupFeedbackHud.md)pickupFeedbackHud九字段，Json/BuiltIn默认true/2秒、绿#6ED88A/红#FF6B6B及五文案；原pickupHud提供物品/容量文案。严格形状/完整字段/类型/UTF-8与正有限时长/#RRGGBB/61字节文案校验，关闭仍校验，旧v1～v25拒绝，无补齐/回退/热重载。根Baker写九固定Settings，不是Ghost；原Player Baker初始化四字段Sequence0/ResultNone/空ItemId/Quantity0，SendToOwner最新快照，各端同版重新烘焙。五普通脚本及Unity生成meta，原资源/布局/引用/签名、输入19、原反馈和玩家v4/世界v2存档字段保持。256份非法配置拒绝、18组合法读取及44次隔离Bake静态通过，实际Serializer四字段/4 mask bits/76字节Snapshot；Editor网络函数指针未初始化，不作为收发验收。用户人工通过限v25/revision28十六项，未触发独立用例UNKNOWN；旧F完成/中断通过限v24/revision27十六项。

## 【FACT】地面合并配置与原快照

当前v40/revision43根必填[dropMerge](MapDropMerge.md)四字段，Json/BuiltIn默认true/0.8米/99份/0.2秒；原严格UTF-8/形状/完整字段/类型与语义检查保持，距离/间隔有限正float、上限正int，关闭仍校验，旧v1～v39拒绝，无补齐/回退/热重载。根Baker写四字段仅Server的Settings、0 GhostField；原掉落四同步字段、输入19、Tools3及所有所属反馈、玩家v4/世界v2路径/字段和资源布局/签名保持。世界快照沿原数量/期限/条目比较保存合并结果，来源Consumed排除、LastDropId不回退；Ready后的恢复物可参与，合法大堆不拒绝/拆分。合并v26阶段编译/元数据、194份非法配置拒绝、28组合法读取与64次隔离Bake静态通过；用户已确认合并人工GamePlayer通过，主线程结合既有静态核对判定通过，限v26/revision29十六项，未触发独立用例UNKNOWN；旧通过保持原范围。

## 【FACT】部分拾取配置与G布局

当前v40/revision43的drops包含必填严格bool partialPickupEnabled=true，drops共12字段、原Server Settings共14字段，新增byte PartialPickupEnabled且0 GhostField；false恢复整堆规则，drops.enabled只控制敌人新物，容量关闭沿原整堆接收/checked边界。旧v1～v39、缺失/null/错类型/重复或未知键拒绝，关闭仍完整校验，无补默认/回退/热重载。两地图Json/BuiltIn一致，正常导入/烘焙后生效。

G目标增加int PickupQuantity，当前七个SendToOwner GhostField，真实地面Quantity保持；生成Serializer/Snapshot七字段、7 mask bits/88字节结构已静态核对，实际收发/带宽不由结构大小推断。输入19、DropGhost4、Tools3/F4/资源状态4/世界保存3及原结果、玩家v4/世界v2字段和资源签名保持。部分拾取v27阶段258份非法配置拒绝、42组合法读取、40次隔离Bake通过；完整接收/提交规则归[部分拾取](MapDropPartialPickup.md)；用户人工通过限v27/revision30十六项，未触发独立用例UNKNOWN。

## 【FACT】背包列表展示配置

v28/revision31排序筛选阶段两地图Json/BuiltIn在inventoryPanel新增十四必填字段，当时配置/原Settings各44：严格bool sortEnabled/filterEnabled=true；defaultSortMode=type（original/type/quantity）、defaultFilterMode=all（all/resources/supplies/other）；十文案沿原非空白/无控制字符/61 UTF-8字节界限。开关映射byte、模式映射byte枚举、文案映射FixedString64Bytes，Settings无GhostComponent且0 GhostField；关闭仍完整校验，旧v1～v27拒绝，无补齐/回退或热重载。默认值/显示行为归[排序筛选](MapInventoryListView.md)。

1078份非法配置拒绝、62组合法读取与64次隔离Editor Bake静态通过，全部44Settings及原布局/资源签名、输入19/G7/Drop4/Tools3和所有原反馈、玩家v4根7/工具项3、世界v2根9/掉落项8保持。本地选择不写输入或玩家/世界档案；用户已确认人工GamePlayer通过，主线程结合既有静态核对判定通过，限v28/revision31及[运行入口](Runtime.md)十六项；结论来自用户反馈，未实际触发的独立排序/筛选/按下抬起与联网/生命周期、真实保存/恢复及性能结论仍UNKNOWN。

## 【FACT】背包材料搜索配置

v29/revision32搜索阶段两地图Json/BuiltIn在inventoryPanel原44字段上追加searchEnabled/searchIgnoreCase=true、searchMaxLength=32和Search/Name keyword/Clear/No search results四文案，配置/Settings各51字段（36文案、两模式ID）。最大长度为1～64的严格int，四文案沿61 UTF-8字节规则；关闭搜索或面板仍全量验证，旧v1～v28拒绝，无补齐/回退/热重载。两个开关映射byte，长度映射int，文案映射FixedString64Bytes；Settings无GhostComponent且0 GhostField。当前完整规则归[搜索](MapInventorySearch.md)。

本阶段1410份非法配置拒绝、76组合法读取与82次隔离Editor Bake静态通过；全部51Settings、原布局/签名、输入19/Drop4/Tools3及全部原反馈与玩家v4/世界v2布局保持。该搜索阶段搜索词和焦点纯本地，未写档案；当前查询可写独立[显示偏好](MapInventoryPreferences.md)，焦点不保存。用户已确认本阶段人工GamePlayer通过，主线程结合既有静态核对判定通过，限v29/revision32及运行入口十六项，结论来自用户反馈；未实际触发的独立配置/显示/保存/联网用例UNKNOWN，旧排序筛选人工通过仍限v28/revision31原清单。

## 【FACT】本机背包显示偏好文件

当前两地图Json/BuiltIn为v40/revision43；inventoryPanel原89字段保留，追加[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)四字段及[配方筛选](MapInventoryRecipeFilter.md)七字段及[配方搜索](MapInventoryRecipeSearch.md)七字段，DTO/Settings各107。确认开关严格bool→byte，三确认文案非空白/无控制字符/最多61 UTF-8字节→FixedString64Bytes；关闭仍验证，旧地图v1～v39拒绝，无补齐/回退/热重载。当前73文案、三模式ID及一偏好文件ID，原参数保持。

[Store](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelPreferencesStore.cs)采用独立版本3的七字段JSON：version、mapDefinitionId、sortMode、filterMode、searchText、favoriteItemNames、favoritesOnly（严格bool）；不复用玩家/世界档案或SavePrepared。路径为Application.persistentDataPath/CombatPrototype/Client/InventoryDisplay/<preferencesFileId>/<mapDefinitionId>.json，只有原客户端Panel的有效绑定调用。合法UTF-8可带BOM；完整七字段/类型、重复键、版本/地图ID、已知模式、搜索最多64 UTF-16单元及控制/孤立代理项检查；收藏数组最多256个Ordinal唯一字符串，非空白/无控制或孤立代理项/最多61 UTF-8字节，暂不在库存的名称和超过当前显示上限的已存记录保留。严格v1五字段保留原选择/搜索并补空收藏，v2六字段保留原收藏，二者favoritesOnly=false并内存迁移v3；读取不写回，下一实际已应用偏好变化才保存v3。v3缺失/错类型布尔值或其他非法记录整档拒绝。正式文件不存在属于首次使用，不创建或立即写入；其他读取错误明确日志并暂停本绑定偏好I/O，临时显示操作仍可用，不覆盖坏档。

只观察已应用值，变化后客户端unscaledTime延迟写入；Close/Reset立即提交待保存值。临时.json.tmp写入并Flush(true)，已存在正式文件以File.Replace替换，否则File.Move；失败保留旧正式文件，本绑定不自动重试，重建绑定后可重新读取。关闭能力（含新收藏筛选）或禁止保存搜索时，不应用/不覆盖对应原字段；载入时按当前搜索长度截断只供使用，不自动写回。完整生命周期与配置接口归[偏好](MapInventoryPreferences.md)。

v30/revision33保存阶段正常编译与55Settings/三个普通类/五字段协议元数据、1612份非法地图配置拒绝/96组合法读取、98次隔离Editor Bake静态通过；原输入19/Drop4/Tools3/G7及全部反馈、玩家v4根7/工具项3、世界v2根9/掉落项8、原布局/签名保持。偏好人工十六项已获用户通过反馈，限v30/revision33及[运行入口](Runtime.md)本阶段清单，人工结论来自用户反馈；AI未执行偏好实际I/O。未实际触发的独立读写/关闭/重启/故障与多人场景、平台File.Replace/Flush语义、同机同地图多进程并发写与异常断电仍UNKNOWN。

v31/revision34重置阶段显示重置只让原Panel恢复当前配置默认模式/空查询，由原Preferences.Capture延迟保存；关闭能力/禁止保存搜索的原字段仍保留，保存关闭或I/O暂停仅临时生效，不删除/迁移文件、不重新开启I/O。独立v1五字段、本机/地图路径、.tmp/Flush/Replace或Move边界保持；57字段、1716份非法配置拒绝/112组合法读取及112次隔离Bake静态通过，人工十六项已获用户通过反馈，限本阶段版本/清单，未触发独立用例仍UNKNOWN，完整规则归[偏好](MapInventoryPreferences.md)。

## 【FACT】材料详情配置与保存边界

v32/revision35详情阶段新增detailsEnabled=true及十三文案，共十四必填inventoryPanel字段；全量71配置/Settings和零GhostField静态核对，正常Unity编译、2414份非法配置拒绝/128组合法读取、128次隔离Editor Bake通过。新Details普通类及Unity生成meta已注册；原输入19、Tools3、反馈与玩家v4/世界v2/偏好v1五字段保持。详情选择不进入任何档案、不调用保存或自动创建记录，本阶段人工十六项已获用户通过反馈，限v32/revision35及运行入口清单；未触发独立用例仍UNKNOWN，完整规则与验收边界归[详情](MapInventoryDetails.md)。

v33/revision36收藏阶段[收藏](MapInventoryFavorites.md)专题记录六新增字段、当时v2读写兼容边界、2692份非法配置拒绝/146组合法读取及146次隔离Editor Bake静态证据；人工十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN，清单归[运行入口](Runtime.md)。

## 【FACT】仅看收藏配置与偏好

v34/revision37筛选阶段追加favoritesFilterEnabled/defaultFavoritesOnly及四文案，建议true/false与Favorites/All items/Favorites only/No matching favorites，DTO/Settings83字段、零GhostField。六字段完整/严格校验，关闭仍验证，旧地图v1～v33拒绝；两地图Json/BuiltIn一致。偏好v3七字段新增严格bool favoritesOnly，严格兼容v1/v2、读取不写回；当前绑定只观察已应用且启用的新模式，关闭保留已读值，原玩家v4/世界v2路径/字段与事务保持。

正常Unity编译/元数据、2986份非法配置拒绝/174组合法读取及174次隔离Editor Bake静态通过，原配置/反馈/引用/布置和兼容签名保持；人工十六项已获用户通过反馈，限CombatPrototypeNetCode、v34/revision37及运行入口清单；未触发独立用例仍UNKNOWN。完整规则与验收边界归[收藏筛选](MapInventoryFavoritesFilter.md)/[运行入口](Runtime.md)，此处不扩写执行日志。

## 【FACT】收藏数量提示接口

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)追加必填favoritesCountEnabled=true、favoritesCountLabel=Favorites，复用原favoritesMaxCount=64/favoritesFullLabel=Favorite limit。DTO/Settings85字段、零GhostField，59文案；严格bool和61 UTF-8字节文案、关闭仍验证，旧地图v1～v34拒绝。Json/BuiltIn、编译/元数据、3084份非法配置拒绝/202组合法读取与202次隔离Editor Bake静态核对通过。计数来自已应用Name集合，不新增偏好v3七字段、玩家v4/世界v2字段或保存入口；完整生命周期和用户确认人工十六项通过边界归计数专题及[运行入口](Runtime.md)，限CombatPrototypeNetCode、v35/revision38及清单，未实际触发的独立用例仍UNKNOWN。

## 【FACT】收藏材料丢弃保护配置

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：inventoryPanel新增必填favoritesDropProtectionEnabled=true、favoritesProtectedLabel=Favorite protected，DTO严格bool/string映射原Settings byte/FixedString64Bytes；总87字段、60文案、零GhostField。仅本机实际已应用收藏参与保护，不新增本机偏好v3七字段、玩家v4/世界v2字段或读写入口。3182份非法配置拒绝/234组合法读取与234次隔离Editor Bake静态通过，旧地图v1～v35拒绝，关闭仍校验；全部原资源布置/兼容签名与保存协议保持。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【FACT】收藏材料消耗提示契约

v37/revision40提示阶段的[收藏提示](MapInventoryFavoritesConsumptionHint.md)：inventoryPanel原87字段原顺序/值保留，追加必填favoritesConsumptionHintEnabled=true、favoritesConsumptionHintLabel=Uses favorites，总89字段/61文案，严格bool/string→byte/FixedString64Bytes、零GhostField，关闭仍验证。仅本机原B七份配方读取真实木石收藏/实际正成本，旧地图v1～v36拒绝，无补默认/回退/热重载。正常Unity编译、3280份非法配置拒绝/276组合法读取与两地图各138次、共276次隔离Editor Bake静态通过，89Settings和原元数据/布局/签名保持。偏好v3七字段、玩家v4/世界v2及I/O入口保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，保护用户通过仍限v36/revision39十六项，完整证据与边界归专题和[运行入口](Runtime.md)。

## 【FACT】收藏材料消耗确认契约

当前v40/revision43的[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)：原inventoryPanel89字段/顺序/值保留，追加必填favoritesConsumptionConfirmationEnabled=true、favoritesConsumptionConfirmationLabel=Use favorites?、favoritesConsumptionConfirmLabel=Confirm、favoritesConsumptionCancelLabel=Cancel，另有[配方筛选](MapInventoryRecipeFilter.md)七字段及[配方搜索](MapInventoryRecipeSearch.md)七字段，当前共107字段/73文案。严格bool/string→byte/FixedString64Bytes，零GhostField；关闭确认/收藏/面板或提示仍全量验证，旧v1～v39拒绝，无补默认/回退/热重载。以下确认静态证据仍限v38/revision41：正常Unity编译、3492份非法配置全部拒绝/334组合法读取及两地图各167次、共334次隔离Editor Bake静态通过；93Settings与原布局/引用/签名/反馈保持。新增一个普通C#确认类及正常Unity导入meta，无新组件/挂载、RPC/输入/保存字段或I/O入口。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，既有提示通过仍限v37/revision40，其他旧通过保持原版本/清单；证据与边界归专题及[运行入口](Runtime.md)。
