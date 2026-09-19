# AI 项目导航

## 项目身份与使用规则

- 工程产品名为 `Code_01`，Unity 版本为 `6000.5.6f1 (0e0577a1a2ac)`。依据：[项目设置](../ProjectSettings/ProjectSettings.asset)、[版本文件](../ProjectSettings/ProjectVersion.txt)。
- 执行流程与授权边界见 [AGENTS.md](../AGENTS.md)，代码写法见 [CodeRule.md](../CodeRule.md)。本导航不替代二者。
- 文档中的【FACT】记录已核实的源码、配置或序列化文本状态；源码只能证明当前实现，不自动成为已确认业务约定。【CURRENT STRATEGY】描述现有实现采用的路径；【KNOWN ISSUES】区分静态可见问题与运行验收结果。
- 业务意图、未读范围和未验证运行结果保持 `UNKNOWN`。本库的静态记录不能作为 GamePlayer PlayMode 已通过的依据。
- 先按下表进入命中模块，跨模块问题再沿相关链接读取。历史不属于默认阅读范围。

## 按任务阅读

| 任务、问题或搜索词 | 先读 | 需要时再读 |
|---|---|---|
| 从哪里启动、Map、按键、场景按钮、OnStart | [运行入口](Modules/Runtime.md) | [资源与数据](Modules/DataResources.md) |
| 属性、生命、体力、等级、金币、刷新 | [玩家](Modules/Player.md) | [数据文件](Modules/DataResources.md) |
| 野猪、BtnAttack、伤害、掉落、战斗奖励 | [战斗](Modules/Combat.md) | [玩家](Modules/Player.md) |
| 背包、Goods、99、UseGoods、活力苹果 | [背包与道具](Modules/Inventory.md) | [资源与数据](Modules/DataResources.md) |
| JSON、streamingAssets、Resources、对象池、重写数据菜单 | [资源与数据](Modules/DataResources.md) | 对应业务模块 |
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
- [Game.cs](../Assets/Scripts/Game.cs)继承 QFramework 的 `Architecture<Game>`，注册玩家/物品模型、日志工具、对象池与玩家事件系统。实际启动与注册阶段见[运行入口](Modules/Runtime.md)。
- 已核实的玩家数据链使用本地 JSON；HTTP 与 protobuf 工具的存在不证明本游戏具有登录、联网存档或后端结算。网络边界见[框架与工具](Modules/Framework.md)。
- [manifest.json](../Packages/manifest.json)声明了 UGUI、2D、AI Navigation 等包；包声明不能证明具体能力已经在主流程使用。

## 全局未知项与验收状态

- `UNKNOWN`：正式玩法目标、发布平台、完整产品流程与数值设计意图。
- `UNKNOWN`：SampleScene、DataSetting 的产品用途，以及尚未逐项核实的场景和资源绑定。
- `UNKNOWN`：当前工程编译、GamePlayer PlayMode、平台构建和服务器联调结果。本次文档初始化未执行这些操作。
- 资源核实依据为 Scene / Prefab / meta 的文本；没有读取图片、纹理或截图内容。
- `CodeRule.md` 标题为“Cocos 代码规范”，真实工程配置为 Unity；保留原规则，标题的引擎适用意图为 `UNKNOWN`，不能据此改变工程身份或擅改规范。
