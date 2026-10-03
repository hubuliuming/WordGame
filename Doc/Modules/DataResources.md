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

## 【KNOWN ISSUES】静态问题与边界

- 已核实相关机制：[XPAutoSave.cs](../../Assets/YFramework/Editor/AutoSaveScene/XPAutoSave.cs) 通过 InitializeOnLoad 注册 EditorApplication.update，间隔到达且不在 PlayMode 时调用 EditorSceneManager.SaveScene 保存当前活动编辑场景；[AutoSaveSettings.asset](../../Assets/YFramework/Editor/AutoSaveScene/AutoSaveSettings.asset) 当前为 autoSaveScene=1、intervalTime=30、showMessage=0，[AutoSaveWindow.cs](../../Assets/YFramework/Editor/AutoSaveScene/AutoSaveWindow.cs) 提供菜单 YFramework/AutoSaveScene。该机制只保存当前活动编辑场景；已观察到的字段覆盖现象与该机制吻合，但唯一历史覆盖来源仍为 UNKNOWN，不能据本次静态引用留存认定覆盖来源已完整查明。
- Android 分支产生 jar URI，但 JsonUti 仍使用文件流；当前文件读写方式与 Android URI 接入没有在代码中衔接。平台实际运行结果为 `UNKNOWN`。
- 玩家加载失败保持显式错误，保存失败会返回 false；JsonUti 仍直接写原文件，尚无原子替换/备份保证。敌人/道具按名称取不到配置或必需组件缺失时，对象池清理该失败实例并保留原异常；背包仅在独立格子边界记录、跳过失败项，不提供配置或节点兜底。
- 编辑器重写菜单是覆盖写入入口；其数值来源与当前文件不同，执行记录不能被当作玩家运行存档的来源证明。

## 【FACT】第 4D 阶段网络原型存档格式与路径

[CombatPrototypePlayerSaveData.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveData.cs) 定义版本化存档，[CombatPrototypePlayerSaveStore.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) 是服务端唯一文件读写入口。路径为 `Application.persistentDataPath/CombatPrototype/Players/<PlayerId>.json`；当前 Editor 的根目录实际为 `C:/Users/91611/AppData/LocalLow/DefaultCompany/Code_01`。正式 `Assets/streamingAssets/Data/PlayerData/Player.json` 与 PlayerDataStore 保持原状。

| 字段 | 当前契约与加载检查 |
|---|---|
| Version | 必需整数，当前仅支持 1 |
| PlayerId | 必需字符串，必须与请求的已验证 ID 按 Ordinal 完全一致 |
| Coin / Experience | 必需整数，范围 0～int.MaxValue |
| Items | 必需数组，允许空数组；每项恰含 ItemName、Quantity |
| ItemName | 必需字符串，非空白、严格 UTF-8 有效且字节数不超过 FixedString64Bytes.UTF8MaxLengthInBytes；同名 Ordinal 重复拒绝整个存档 |
| Quantity | 必需整数，范围 1～int.MaxValue |

根对象恰含上述五个字段，库存项恰含上述两个字段；缺字段、未知字段、JSON 重复属性、错误类型（含浮点数或数字字符串）、不支持版本、非法数值及不匹配身份均拒绝。UTF-8 严格读取（支持 UTF-8 BOM）、UTF-8 无 BOM 写入；没有迁移、自动修正、跳过坏库存项或重写坏档。

第 6A 玩家生命、上限、受击序号和死亡标记均不加入该 JSON，体力也不持久化；重新生成沿既有玩家 Baker 初值初始化生命与体力，金币/经验/背包仍按固定 ID 恢复。生命职责归[玩家](Player.md)，原 Prefab 新参数与实际烘焙归[运行入口](Runtime.md)。

## 【FACT】第 4D 阶段开发 ID 配置

[CombatPrototypeDevelopmentIdentity.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeDevelopmentIdentity.cs) 优先读取进程参数 `-combatPrototypePlayerId <ID>`，该参数只允许出现一次，并对当前进程内的客户端 World 生效。

无该参数时，Editor 读取 [UserSettings/CombatPrototypeDevelopmentIdentity.json](../../UserSettings/CombatPrototypeDevelopmentIdentity.json)：Version=1、Clients 为按 World.Name 精确匹配的对象，当前明确配置 `ClientWorld: player-a`；多个 World 使用该文件时须分别列出各自的明确 ID。

独立第二进程可声明 `-combatPrototypePlayerId player-b`；非 Editor 缺参数或 Editor 缺配置项时显式报错并断开，不随机分配或回退。

[CombatPrototypePlayerIdentity.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerIdentity.cs) 在客户端配置与服务端 RPC 两个输入边界使用同一规则：ID 长度 1～32，仅小写 ASCII 字母、数字、下划线、连字符；拒绝 con/prn/aux/nul 与 com1～com9、lpt1～lpt9 等保留文件名。该限制使各 ID 的文件名固定且不含路径分隔符；固定 ID 只用于开发存档，不是账号认证。

## 【CURRENT STRATEGY】第 4D 阶段读取与写入边界

服务端以 FileMode.Open 读取正式文件，只把 FileNotFoundException 或 DirectoryNotFoundException 认作首次无档，采用已确认的 0/0 与空库存；权限、I/O、解码、JSON 或字段校验错误交回握手边界记录并拒绝当前玩家，原文件保留。读取只认正式 `.json`，失败遗留的 `.json.tmp` 不作为可恢复存档。

每个在线击杀奖励先准备完整最终金币/经验、目标物品及必要缓冲容量，再把当前库存与目标最终值投影为一个 JSON 候选。存储类序列化该候选，在同一目录写 `<PlayerId>.json.tmp`、Flush(true)，有旧正式文件时 File.Replace，无旧文件时 File.Move；服务端运行时按该固定路径创建 Players 目录。只有保存函数成功返回后才修改三项 ECS 状态，失败时旧正式文件与旧玩家数值保持，事件消费与后续隔离见[战斗](Combat.md)。

没有另外的备份文件、定时保存、断线补存或退出保存；准入恢复由[玩家](Player.md)维护，原型背包规则由[背包与道具](Inventory.md)维护。AI 静态核对未调用读写存档的业务方法，实际保存与恢复由人工 GamePlayer 验收确认。

## 【KNOWN ISSUES】第 4D 阶段存储验收

四份脚本已由 Unity 编译并加载；文件路径、JSON 校验、临时文件替换与奖励提交顺序已静态核对。用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过；存储范围覆盖多人独立文件、重连/重启恢复、重复身份与坏档拒绝、I/O 失败保留旧档且不部分到账，完整边界见[运行入口](Runtime.md)。本规则仅覆盖当前开发原型的单服务端本地文件写入，不包含正式账号、跨服务器协调、掉电恢复保证、迁移或平台构建验收；AI 未运行逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 5A 阶段存档耗时入口

当前存储类在 Load 与 SavePrepared 入口提供 Profiler 标记，文件契约与原提交顺序保持。标记计量边界、单位、调用次数与待采集结果统一归[性能基线](Performance.md)；标记编译完成不表示实际存档耗时已测得。

## 未知项与验收状态

`UNKNOWN`：正式存档/配置职责划分、资源跨场景运行结果、Android 文件访问方案，以及表格到 JSON 的导入链。`Assets/Instructions/Item/ReconveItem.xls` 仅核实存在，未读取表格内容。

第 2 阶段玩家状态与存储改动的实际行为和日志已有用户核对正确的反馈，主线程已结合静态检查判定该阶段通过。第 3 阶段 MapCanvasControl.ItemParent 序列化引用已补齐并经延时复读留存；用户针对该阶段人工 GamePlayer 清单反馈“已确认正常”，主线程结合静态验收已判定该阶段通过，范围见[运行入口](Runtime.md)。AI 未执行上述菜单、逻辑单元测试、GamePlayer PlayMode 或平台构建；该结论不扩展至后续功能或平台验证。

## 【FACT】第 7A 阶段消费候选与存档兼容

存储类增加 PrepareItemConsumption，将当前完整库存和目标扣除后的数量投影为同一 v1 候选；目标归零时省略该项，其他条目及顺序保持，允许消费最后一项后 Items 为空数组。Version、PlayerId、Coin、Experience 和 Items 的字段契约、严格加载检查与原奖励投影不变，CurrentPower/UpperPower 仍不入 JSON。

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

开发身份仍使用原 -combatPrototypePlayerId 优先及 CombatPrototypeDevelopmentIdentity.json 按 World.Name 读取规则。窗口只展示实际来源并提供文件定位，不复制或改写 PlayerId。原服务端存档路径、格式、固定 ID 契约以及正式 PlayerDataStore JSON 保持。生命、体力、攻击等玩法数值仍保存于各 Authoring 所属资源；地图默认来源提供尺寸、布局、出生原点与敌人总量，原 Spawner 保留列数/间距，两者沿烘焙链生效，配置契约归[战斗地图](Map.md)，均不加入启动 JSON。

## 【KNOWN ISSUES】Editor 启动配置验证范围

文件、字段、UTF-8 和源码边界已静态核对，新脚本由 Unity 编译；坏配置的实际启动表现、端口占用、保存失败与重复模式切换仍待人工 GamePlayer 验证，保持 UNKNOWN。本轮未调用玩家存档 Load/SavePrepared，不改变已有第 4D/7A 存档人工通过范围。

## 【FACT】网络玩家美术资源入口

玩家 Ghost 的原 ClientPrefab 继续引用 CombatPrototypeNetworkPlayerView；其中 VisualRoot 引用新增灰衣修士模型，角色设定图、网格/材质、动画控制器/Clip/遮罩和预览的路径及用途统一见[玩家美术资源表](../PlayerArt.md)。新资源及脚本 meta 由 Unity 正常导入生成，既有 GUID、Bundle、包与构建配置保持。

## 【FACT】网络战斗地图配置与资源

Assets/Scripts/CombatPrototype/Map/ 包含四类可序列化配置、配置集合、来源接口、默认来源、边界校验、Authoring/Baker 和地图数据/显示脚本。当前使用默认来源，没有实际地图 JSON 文件读取或运行状态写盘；资源键通过 SubScene 的 GroundMaterials、DecorationPrefabs 显式绑定，字段契约与资源映射统一归[战斗地图](Map.md)。

新资源位于 Assets/Art/Map/CombatPrototype/ 与 Assets/Prefabs/CombatPrototype/Map/，包括 4 个材质、2 个网格和 2 个静态装饰 Prefab。新增资源和脚本 meta 由 Unity 导入生成；既有玩家/敌人资源、旧 meta、Bundle、包与构建设置保持。地表运行网格及装饰实体由客户端地图显示系统拥有和清理，共享资源不随地图根实体释放而销毁。该模块没有调用背包或玩家存档 Load/SavePrepared，也不保存地图对象状态。
