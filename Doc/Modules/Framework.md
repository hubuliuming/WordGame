# 框架、工具与已有说明导航

[返回总导航](../AI_Understanding.md)。本页限定为已核实能力与读取入口，不将工具库目录视为本游戏已启用功能，也不复制第三方库的完整说明。

## 【FACT】框架职责

| 范围 | 已核实入口 | 当前关系 |
|---|---|---|
| QFramework 架构 | [QFramework.cs](../../Assets/QFramework/Framework/Scripts/QFramework.cs) | Game 继承 Architecture；业务使用 IController、AbstractModel、AbstractSystem、AbstractCommand |
| QFramework 事件 | 同上及 [Msg.cs](../../Assets/Scripts/Msg/Msg.cs) | 玩家详情使用架构类型事件；背包使用字符串全局事件，二者不是同一通道 |
| 工程 UI | [UIBase.cs](../../Assets/Framework/UI/UIBase.cs) | UIBase 继承 YMonoBehaviour；同文件的 Framework.UI.UIManager 管理显示栈 |
| YFramework Mono | [YMonoBehaviour.cs](../../Assets/YFramework/Framework/YMonoBehaviour.cs)、[MonoGlobal.cs](../../Assets/YFramework/Framework/MonoGlobal.cs) | 自定义 OnStart 接口与共享协程宿主；调用限制见下文 |
| 程序集 | [YFramework.asmdef](../../Assets/YFramework/YFramework.asmdef)、[YFramework.Editor.asmdef](../../Assets/YFramework/Editor/YFramework.Editor.asmdef) | 编辑器程序集 includePlatforms 仅为 Editor；不能据目录名推断其他程序集依赖 |
| JSON 工具 | [JsonUti.cs](../../Assets/YTools/ConfigUtil/Json/JsonUti.cs) | 当前玩家、敌人、道具文件读写的实际依赖；细节归[资源与数据](DataResources.md) |

本工程使用的 UIBase 与 QFramework UIKit 是不同命名空间、不同代码入口；发现 UI 问题时从实际继承和调用处追踪。

## 【CURRENT STRATEGY】初始化与生命周期边界

Architecture 首次访问 Interface 才创建对象；Game 的注册项、模型先于系统的初始化阶段归[运行入口](Runtime.md)。命令执行时由架构设置命令的 Architecture，再调用 Execute。

YMonoBehaviour 定义虚 OnAwake、抽象 OnStart、MonoSelf 与 IgnoreSelf，没有统一 Unity 生命周期调度。MonoGlobal.Instance 在首次获取时创建 GameObject 并挂载自身，Awake 调用 DontDestroyOnLoad。工具调用中出现 MonoGlobal 不代表主场景预先挂载了它。

## 【FACT】绑定与调度工具

| 任务 | 入口与核实范围 |
|---|---|
| 自动绑定规则 | [AutoBindRules.cs](../../Assets/YFramework/Framework/AutoBindE/AutoBindRules.cs)声明节点标记、目标组件类型、IAutoBindMono 与 AutoBindFieldAttribute；TMP 类型通过解析存在性加入规则 |
| 编辑器自动绑定 | [AutoBindEditor.cs](../../Assets/YFramework/Editor/AutoBindE/AutoBindEditor.cs)具有 CONTEXT/MonoBehaviour/AutoBind 菜单与代码生成/字段绑定入口；本页未审计其全部改写行为 |
| 延迟与隔帧执行 | [ActionKit.cs](../../Assets/YFramework/Kit/Scheduling/ActionKit.cs)：Delay 通过 WaitForSeconds，DelayOneFrame 通过 yield null；默认协程宿主为 MonoGlobal，也有显式宿主重载 |
| 定频回调 | [ActionSpan.cs](../../Assets/YFramework/Kit/Scheduling/ActionSpan.cs)：ActionKit.SecondsFixedUpdate 创建对象并挂载 ActionFixedUpdate |
| 计时器 | [TimerManager.cs](../../Assets/YFramework/Kit/Scheduling/TimerManager.cs)：Register、Pause、Resume、ReStart、StopTimer 等入口；Update 推进并移除结束项 |
| TimerKit | [TimerKit.cs](../../Assets/YFramework/Kit/Scheduling/TimerKit.cs)：Register 包装目前被注释，不能描述为已有可调用注册 API |
| 表格工具 | [ExcelSystem.cs](../../Assets/YTools/ConfigUtil/EPPlus/Scripts/ExcelSystem.cs)：GetInfo 使用 EPPlus 读取指定表；本游戏表格导入调用链尚未确认 |

除明确列出的主业务依赖外，工具的运行接入情况为 `UNKNOWN`。没有执行 AutoBind、生成脚本或创建运行时对象。

## 【FACT】HTTP 与 protobuf 能力

- [HttpService.cs](../../Assets/YFramework/Network/Http/HttpService.cs)通过 UnityWebRequest 提供 GetAsync 和 PostRawAsync，使用原始字节收发；请求由 HttpEnvironment、HttpRequestOptions 决定地址、Header、超时等。
- [HttpEnvironment.cs](../../Assets/YFramework/Network/Http/HttpEnvironment.cs)接收 BaseUrl，ResolveUrl 支持相对路径与绝对 URL；环境名 DevLocal / Test / Prod 是工具定义，不证明当前项目有相应部署。
- [ProtoSerializer.cs](../../Assets/YFramework/Network/Protocol/ProtoSerializer.cs)按消息类型注册编码/解码委托，缺少注册时抛错；packet 编解码可替换。默认编码只返回 body 的副本，默认解码产生 cmd=0 的 packet。
- [ProtoWireCodec.cs](../../Assets/YFramework/Network/Protocol/ProtoWireCodec.cs)提供 protobuf wire 层读写与跳过字段能力；不能据此推断全部业务协议都已注册。

在 `Assets/Scripts`、`Assets/Framework`、`Assets/Test` 的文本检索范围内，未检出 HttpService、ProtoSerializer 的引用。当前已核实玩家链是本地 JSON；后端地址、登录/token 接入、业务消息类型及网络模型同步均为 `UNKNOWN`。

已有 [Unity 前端后端协议格式说明](../../Assets/YFramework/Network/Http/UnityBackendProtocolGuide.md)自述为可复用接入口径与示例。协议参考继续归该文档；其中登录、关卡、资源回写示例不能当作本游戏已实现的业务，也没有据此新增协议专题或后端模块。

## 已有笔记与其他范围

[Assets/Note/Time.md](../../Assets/Note/Time.md)与[YFramework/Note/Time.md](../../Assets/YFramework/Note/Time.md)均记录 DateTime 格式字母含义，当前保留原位置；它们不是计时系统或游戏时间策略说明。

`Assets/YFramework/Network/LegacySocket/`、`Math/`、`Collections/`、`Extension/`、`Components/` 及 YTools 的其他目录已定位，但不在本页展开完整语义。历史版本说明与 DevTarget 不用于证明当前能力。第三方 QFramework 工具、DOTween 以及其他插件不作全量审计。

## 未知项与验收状态

`UNKNOWN`：全部插件/程序集在当前 Unity 版本的编译兼容性、工具在所有场景中的挂载情况、网络联调结果和自动绑定的全路径行为。框架导航没有运行测试或构建，也没有实际访问后端。

本页保留能力边界；具体业务已知问题在[玩家](Player.md)、[背包与道具](Inventory.md)、[战斗](Combat.md)、[资源与数据](DataResources.md)中维护。
