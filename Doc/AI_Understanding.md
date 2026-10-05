# AI 项目导航

## 项目身份与使用规则

- 产品名 `Code_01`，Unity版本 `6000.5.6f1 (0e0577a1a2ac)`。依据：[项目设置](../ProjectSettings/ProjectSettings.asset)、[版本文件](../ProjectSettings/ProjectVersion.txt)。
- 执行流程与授权边界见 [AGENTS.md](../AGENTS.md)，代码写法见 [CodeRule.md](../CodeRule.md)。导航不替代执行规则。
- 文档中的【FACT】记录已核实的源码、配置或序列化文本状态；源码只能证明当前实现，不自动成为已确认业务约定。【CURRENT STRATEGY】描述现有实现采用的路径；【KNOWN ISSUES】区分静态可见问题与运行验收结果。
- 业务意图、未读范围和未验证运行结果保持 `UNKNOWN`。本库的静态记录不能作为 GamePlayer PlayMode 已通过的依据。
- 按表进入模块，再沿链接跨模块读取；历史默认不读。

## 按任务阅读

| 任务、问题或搜索词 | 先读 | 需要时再读 |
|---|---|---|
| 地图F/G/工具/存档 | [地图](Modules/Map.md) | [F5/HUD](Modules/MapWorldSaveHud.md) |
| 从哪里启动、Map、按键、场景按钮、OnStart | [运行入口](Modules/Runtime.md) | [资源与数据](Modules/DataResources.md) |
| 属性、生命、体力、等级、金币、刷新 | [玩家](Modules/Player.md) | [数据文件](Modules/DataResources.md) |
| 野猪、BtnAttack、伤害、掉落、战斗奖励 | [战斗](Modules/Combat.md) | [玩家](Modules/Player.md) |
| 网络敌人反击、玩家受伤、死亡停动与手动复活 | [战斗](Modules/Combat.md) | [玩家](Modules/Player.md)、[运行验收](Modules/Runtime.md) |
| 玩家/敌人美术动画 | [玩家](PlayerArt.md)、[敌人](EnemyArt.md) | — |
| AI 出图、末世玄幻 | [模板](CombatImagePromptTemplate.md) | — |
| 背包、制作、丢弃 | [背包](Modules/Inventory.md) | [丢弃](Modules/MapInventoryDrop.md) |
| JSON、streamingAssets、Resources、对象池、重写数据菜单 | [资源与数据](Modules/DataResources.md) | 对应业务模块 |
| 固定玩家 ID、服务端存档、重连恢复、坏档、保存失败 | [资源与数据](Modules/DataResources.md) | [玩家准入](Modules/Player.md)、[奖励提交](Modules/Combat.md)、[运行验收](Modules/Runtime.md) |
| 规模、性能基线、Tick、帧耗时、GC、RTT、快照、预测误差、存档耗时 | [性能基线](Modules/Performance.md) | [运行入口](Modules/Runtime.md)、[资源与数据](Modules/DataResources.md) |
| QFramework、YFramework、UIBase、AutoBind、HTTP、protobuf、计时工具 | [框架与工具](Modules/Framework.md) | [运行入口](Modules/Runtime.md) |

## 模块与目录地图

| 真实目录 | 内容与边界 |
|---|---|
| `Assets/Scripts/` | Game 注册入口、玩家/敌人、命令、UI 控制、对象池、消息、数据与编辑器辅助 |
| `Assets/Framework/UI/` | 本工程的 UIBase 与 UIManager；区别于 QFramework UIKit |
| `Assets/Scenes/` | Map、SampleScene、DataSetting 场景文件；当前构建列表仅启用 Map |
| `Assets/Resources/Prefabs/` | 已核实主链路使用的野猪、活力苹果与 Goods Prefab |
| `Assets/streamingAssets/Data/` | 玩家 JSON、敌人 JSON、恢复道具 JSON；注意磁盘目录的真实大小写 |
| `Assets/QFramework/` | Architecture、命令、事件及工具代码；只按被调用入口追踪 |
| `Assets/YFramework/` | Mono 基类、编辑器绑定、扩展、调度、网络等通用能力 |
| `Assets/YTools/` | JSON、XML、Excel、UI 等工具；目录存在不代表业务已接入 |
| `Assets/Test/` | 包含 Map 中使用的 TestController；不能仅凭目录名视为单元测试 |
| `Assets/Instructions/`、`Assets/Note/` | 表格文件和简短笔记；表格内容与业务导入关系未核实 |
| `Assets/Plugings/`、`Assets/Art/`、`Assets/Font/`、`Assets/TextMesh Pro/` | 插件与素材范围；不由素材名称或视觉内容推断玩法 |
| `Packages/`、`ProjectSettings/` | 包声明、Unity 版本、工程与构建场景设置 |
| `Doc/Modules/` | 当前模块事实与未知项；同一规则只有一个主文档 |
| `Doc/ChangeLog/` | 已发生的变更记录；仅追溯时读取 |

`Library/`、`Temp/`、`obj/`、`Logs/` 是工程现场目录，不作为业务规则来源。

## 【FACT】全局确认事实

- [构建场景列表](../ProjectSettings/EditorBuildSettings.asset)只含启用的 `Assets/Scenes/Map.unity`。这不等同于已确认发布平台或全部场景用途。
- [Game.cs](../Assets/Scripts/Game.cs)继承 QFramework 的 `Architecture<Game>`，注册玩家/物品模型、玩家存储与日志工具、对象池与玩家事件系统。实际启动与注册阶段见[运行入口](Modules/Runtime.md)。
- 正式玩家数据链使用原本地 JSON；独立网络原型已接入开发固定 ID 的服务端 JSON 存档，格式与边界见[资源与数据](Modules/DataResources.md)。固定 ID 由客户端声明，未接正式账号认证；HTTP 与 protobuf 工具的存在不证明正式登录或后端能力。网络边界见[框架与工具](Modules/Framework.md)。
- [manifest.json](../Packages/manifest.json)声明了 UGUI、2D、AI Navigation 等包；包声明不能证明具体能力已经在主流程使用。

## 全局未知项与验收状态

- `UNKNOWN`：正式玩法目标、发布平台、完整产品流程与数值设计意图。
- `UNKNOWN`：SampleScene、DataSetting 的产品用途，以及尚未逐项核实的场景和资源绑定。
- 第 2 阶段玩家状态与存储改动已有用户“实际行为和日志均已核对正确”的反馈，主线程结合代码、文档、资源静态检查判定该阶段通过；不表示 AI 执行过测试或构建，也不扩展为完整平台验收。
- `UNKNOWN`：后续阶段的运行结果，以及完整工程编译、平台构建和服务器联调结果。
- 用户已确认第 4D 网络原型存档人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过；范围覆盖多人独立保存、重连/服务端重启恢复及失败隔离，具体边界见[运行入口](Modules/Runtime.md)。既有阶段通过范围保持，规模性能与平台验收仍为 `UNKNOWN`。
- 第 5A 已确认 2 玩家/32 敌人的性能口径，存档 Profiler 标记已编译并注册；第 5B 已补齐项目外采集助手并通过主线程静态验收，按用户要求保持 GamePlayer、采样与运行回调关闭。尚无运行样本，数据有效性与最终性能验收仍为 `UNKNOWN`；覆盖、使用边界与静态证据见[性能基线](Modules/Performance.md)。
- 第 6A 敌人反击与玩家受伤已获用户人工通过反馈，主线程结合静态核对判定通过。生命归[玩家](Modules/Player.md)，战斗归[战斗](Modules/Combat.md)，完整验收边界见[运行入口](Modules/Runtime.md)；不含规模性能或平台验收。
- 用户已确认第 6B 手动复活人工 GamePlayer 验收通过，主线程结合静态核对判定该阶段通过。规则归[玩家](Modules/Player.md)，敌人锁定归[战斗](Modules/Combat.md)，范围归[运行入口](Modules/Runtime.md)。
- 第 7A 网络物品使用已获用户人工通过反馈，主线程结合静态核对判定通过。规则归[背包与道具](Modules/Inventory.md)，范围见[运行入口](Modules/Runtime.md)；不含规模性能或平台验收。
- 第 3 阶段 MapCanvasControl.ItemParent 已在 Map 场景文本中绑定现有 ItemParent；用户针对该阶段人工 GamePlayer 清单反馈“已确认正常”，主线程结合既有静态验收判定该阶段通过，范围见[运行入口](Modules/Runtime.md)。
- 第 4 阶段 KnapsackControl、PlayerDetailsControl、DetailInform 已完成事件订阅、刷新与释放代码的静态落地；人工 GamePlayer 交互验收、详情使用效果及完整工程编译仍未完成。
- 资源绑定据 Scene/Prefab/meta 文本；仅查看本次授权新图。
- `CodeRule.md` 标题为“Cocos 代码规范”，真实工程配置为 Unity；保留原规则，标题的引擎适用意图为 `UNKNOWN`，不能据此改变工程身份或擅改规范。
