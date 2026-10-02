# 框架、工具与已有说明导航

[返回总导航](../AI_Understanding.md)。本页限定为已核实能力与读取入口，不将工具库目录视为本游戏已启用功能，也不复制第三方库的完整说明。

## 【FACT】框架职责

| 范围 | 已核实入口 | 当前关系 |
|---|---|---|
| QFramework 架构 | [QFramework.cs](../../Assets/QFramework/Framework/Scripts/QFramework.cs) | Game 继承 Architecture；业务使用 IController、AbstractModel、AbstractSystem、AbstractCommand |
| QFramework 事件 | 同上及 [Msg.cs](../../Assets/Scripts/Msg/Msg.cs) | 玩家详情使用 UpdateShowData 类型事件；库存已发送 InventoryChanged 类型事件但展示未订阅；格子使用请求仍走字符串全局事件 |
| 工程 UI | [UIBase.cs](../../Assets/Framework/UI/UIBase.cs) | UIBase 继承 YMonoBehaviour；同文件的 Framework.UI.UIManager 管理显示栈 |
| YFramework Mono | [YMonoBehaviour.cs](../../Assets/YFramework/Framework/YMonoBehaviour.cs)、[MonoGlobal.cs](../../Assets/YFramework/Framework/MonoGlobal.cs) | 自定义 OnStart 接口与共享协程宿主；调用限制见下文 |
| 程序集 | [YFramework.asmdef](../../Assets/YFramework/YFramework.asmdef)、[YFramework.Editor.asmdef](../../Assets/YFramework/Editor/YFramework.Editor.asmdef) | 编辑器程序集 includePlatforms 仅为 Editor；不能据目录名推断其他程序集依赖 |
| JSON 工具 | [JsonUti.cs](../../Assets/YTools/ConfigUtil/Json/JsonUti.cs) | 当前玩家、敌人、道具文件读写的实际依赖；细节归[资源与数据](DataResources.md) |

本工程使用的 UIBase 与 QFramework UIKit 是不同命名空间、不同代码入口；发现 UI 问题时从实际继承和调用处追踪。

## 【CURRENT STRATEGY】初始化与生命周期边界

Architecture 首次访问 Interface 才创建对象；Game 的注册项、模型先于系统的初始化阶段归[运行入口](Runtime.md)。命令执行时由架构设置命令的 Architecture，再调用 Execute。框架已有 AbstractCommand<TResult> 和 SendCommand<TResult>(ICommand<TResult>) 的同步返回值能力；UseItemCommand 使用 bool 结果，ItemBase 显式调用 SendCommand<bool> 决定是否回收。玩家存储以现有 IUtility 机制注册。

FactoryUISystem 仍使用既有 AbstractSystem 注册入口，OnInit 不再查找场景对象。当前 AbstractSystem 只有 OnInit，没有 OnDeinit / Dispose；场景池绑定与结束清理由 MapCanvasControl 显式调用，系统本身按绑定引用隔离各次场景生命周期。具体实例登记和销毁边界归[资源与数据](DataResources.md)，未改动 QFramework。

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

在 `Assets/Scripts`、`Assets/Framework`、`Assets/Test` 的文本检索范围内，未检出 HttpService、ProtoSerializer 的引用。当前已核实玩家链是本地 JSON；后端地址、登录/token 接入、业务消息类型及网络模型同步均为 `UNKNOWN`。网络实体包安装版本和独立原型接入边界归[运行入口](Runtime.md)记录。

已有 [Unity 前端后端协议格式说明](../../Assets/YFramework/Network/Http/UnityBackendProtocolGuide.md)自述为可复用接入口径与示例。协议参考继续归该文档；其中登录、关卡、资源回写示例不能当作本游戏已实现的业务，也没有据此新增协议专题或后端模块。

## 已有笔记与其他范围

[Assets/Note/Time.md](../../Assets/Note/Time.md)与[YFramework/Note/Time.md](../../Assets/YFramework/Note/Time.md)均记录 DateTime 格式字母含义，当前保留原位置；它们不是计时系统或游戏时间策略说明。

`Assets/YFramework/Network/LegacySocket/`、`Math/`、`Collections/`、`Extension/`、`Components/` 及 YTools 的其他目录已定位，但不在本页展开完整语义。历史版本说明与 DevTarget 不用于证明当前能力。第三方 QFramework 工具、DOTween 以及其他插件不作全量审计。

## 未知项与验收状态

## 【FACT】第 1 阶段切片框架边界

战斗切片脚本位于 `Assets/Scripts/CombatPrototype/`，使用 Unity Input System 与 Physics 基础 API，不新增 QFramework 注册项、网络 World、ECS 系统或资源加载入口；现有框架初始化、UI 生命周期和存档边界保持不变。

`UNKNOWN`：全部插件/程序集在当前 Unity 版本的编译兼容性、工具在所有场景中的挂载情况、网络联调结果和自动绑定的全路径行为。框架导航没有运行测试或构建，也没有实际访问后端。

## 【FACT】第 2B / 第 3A 阶段 NetCode 接入边界

`Assets/Scripts/CombatPrototype/Networking/` 使用 Netcode for Entities 6.5.0 官方生命周期、Ghost 和输入 API。`ClientServerBootstrap` 配合独立主场景 `OverrideAutomaticNetcodeBootstrap` 标记控制网络 World；未启用标记时创建本地 World。`GoInGame` RPC 统一负责玩家生成与连接绑定，`LinkedEntityGroup` 管理断线销毁，`IInputComponentData`/`InputEvent`、`AutoCommandTarget`、`GhostOwnerIsLocal` 与 `Simulate` 组成输入及预测链。

Authoring/Baker、预测玩家移动、敌人批量生成、敌人目标选择与移动、空间索引、服务端近战、伤害事件结算、状态日志、玩家 Mono 表现和敌人实体显示系统按职责分开。两个 Ghost Prefab 由唯一 SubScene Spawner 显式引用；玩家通过官方 `GhostPresentationGameObjectAuthoring`、`GhostPresentationGameObjectEntityOwner` 与 Transform 同步系统接入 GameObject 表现，敌人通过 Entities Graphics 显示，当前渲染配置见[运行入口](Runtime.md)。旧 EnemyView 资产和脚本保留，敌人 Ghost 已清空 ClientPrefab；新的显示系统只读同步死亡标记，不承担伤害结算。

该目录不注册到 QFramework Game 架构，不修改正式 Map/第 1 阶段入口、UI 或存档；渲染管线配置由本页第 3B-1 节界定。代码编译和 Editor 烘焙已核对；用户已确认第 2B 阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。结论仅覆盖当前独立网络原型的双玩家加入退出、移动朝向与输入预测、基础近战、单敌人状态和 Mono 表现，不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收；AI 未执行逻辑单元测试、PlayMode 或构建。

本页保留能力边界；具体业务已知问题在[玩家](Player.md)、[背包与道具](Inventory.md)、[战斗](Combat.md)、[资源与数据](DataResources.md)中维护。

## 【CURRENT STRATEGY】第 3A 阶段 ECS 群体职责

群体实现限定在现有 Networking 目录：生成系统负责独立条目失败隔离；敌人移动系统从在线连接选目标；空间系统维护只含存活敌人的格子索引；近战系统产生命中事件；伤害系统消费并清空事件。移动配置、目标和伤害事件缓冲通过 GhostComponent(PrefabType = GhostPrefabType.Server) 限定为服务端组件，生命与死亡沿用原 GhostField。只有 Spawner 是单例，不再把敌人当作单例读取。

群体逻辑沿用已有 SubScene 和敌人 Ghost，敌人当前实体表现见第 3B-2 节，不接入正式业务架构；当前管线为 URP，渲染前置配置的验收与第 3A 群体逻辑验收分开记录。代码编译及实际烘焙资源已核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。结论仅覆盖当前独立网络原型的 32 敌人生成、最近在线玩家追踪及玩家加入/退出后的目标切换、停止距离与无在线玩家时停止追踪、群体近战每目标去重和伤害 25、死亡停止及双端 HP、死亡与统计一致性；不扩展为正式 Map、平台构建、大规模性能或线上联调验收。第 3A 通过结论不包含第 3B-2 实体表现及第 6A 反击/生命验收；寻路避障仍未接入；AI 未执行逻辑单元测试、PlayMode 或构建。

## 【CURRENT STRATEGY】第 3B-1 阶段渲染与表现边界

当前 URP Forward+、Linear 和 SRP Batcher 为全局渲染前置配置，两份网络 View 共享 URP/Lit 材质，路径和参数由[运行入口](Runtime.md)维护。玩家保留 GhostPresentationGameObjectAuthoring → ClientPrefab → Transform 同步的表现调用链；敌人根实体渲染及死亡隐藏由第 3B-2 节界定。

包、配置、材质和实际烘焙静态验收通过；用户已确认第 3B-1 人工 GamePlayer 验证通过，主线程结合静态检查与用户反馈判定第 3B-1 阶段通过。人工验收范围仅覆盖网络场景外观、亮度与边缘效果、Map UI/TMP/自定义 Outline Shader 兼容，以及切换 URP/Linear 后双端 32 敌人群体行为回归；不扩展为平台构建、大规模性能或线上联调验收。第 3A 与第 3B-1 通过结论保持原验收范围，不包含第 3B-2 敌人实体渲染与死亡隐藏；AI 未执行逻辑单元测试、PlayMode、构建、发布或图片读取。

## 【CURRENT STRATEGY】第 3B-2 阶段敌人表现职责

`CombatPrototypeEnemyRenderSystem` 位于现有 Networking 目录，仅在客户端 PresentationSystemGroup、EntitiesGraphicsSystem 之前读取 CombatPrototypeEnemyState.IsDead，控制根实体 MaterialMeshInfo 的启用状态；查询忽略组件启用筛选，保留对已隐藏敌人的更新。显示系统不承担服务端生成、追踪、目标选择、近战或伤害结算，不修改网络同步字段。

敌人 Ghost 根节点使用 MeshFilter/MeshRenderer 烘焙 Entities Graphics 组件；原 GhostPresentationGameObjectAuthoring 保留但两端 Prefab 引用均为空，实际烘焙根实体已无 Mono 表现引用。玩家保留原 Mono 桥接，旧 EnemyView 资产与脚本不删除；资源与实际烘焙事实由[运行入口](Runtime.md)维护。

## 【KNOWN ISSUES】第 3B-2 阶段表现验收

新系统已由 Unity 编译并确认类型已加载，资源和实际烘焙已静态核对；用户已确认第 3B-2 人工 GamePlayer 验证通过，主线程结合既有静态检查与用户反馈判定第 3B-2 阶段通过。验收仅覆盖当前独立网络原型的双端敌人显示与移动、死亡隐藏无重复显示或残影、重新加入后的死亡状态，以及 HP/存活/死亡统计一致性。第 2B、第 3A 与第 3B-1 通过结论保持各自原范围；本阶段不扩展为规模性能、平台构建或线上联调验收。AI 未运行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4A 阶段网络体力职责

现有 Networking 目录新增独立 `CombatPrototypePlayerResource` ECS 组件，`CurrentPower`/`UpperPower` 为整数 GhostField。原玩家 Authoring/Baker 负责体力初值、上限与近战成本的烘焙及配置合法性边界；原服务端近战系统负责攻击准入和唯一运行时体力消耗，原日志系统读取状态。没有新增资源服务、恢复系统或本地预测扣费链。

## 【CURRENT STRATEGY】第 4A 阶段框架边界

体力随每个玩家 Ghost 和原连接生命周期独立存在，由服务端消耗、客户端接收快照；规则与参数分别见[玩家](Player.md)、[战斗](Combat.md)，Prefab/烘焙与日志入口见[运行入口](Runtime.md)。原输入与移动预测、群体敌人和实体表现职责保持；不注册到 QFramework `Game`，不调用 `PlayerModel`/`PlayerDataStore`，体力不持久化，不接账号、正式背包或自动恢复；奖励职责见第 4B 节，背包见第 4C 节，独立网络存档见第 4D 节，玩家受击职责见第 6A 节。

## 【KNOWN ISSUES】第 4A 阶段接入验收

代码编译、Ghost 字段加载及实际 SubScene 产物已完成静态核对；用户已确认第 4A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4A 阶段通过。验收仅覆盖当前独立网络原型的双玩家独立体力与同步、成功启动一次扣 10（含空挥）、忙碌拒绝且阶段继续、耗尽后拒绝且序号不增加、无自动恢复、重新加入恢复 100，以及原移动、群体伤害和死亡显示回归，完整范围见[运行入口](Runtime.md)。第 2B、第 3A、第 3B 的既有人工通过范围保持原状，不形成规模性能、平台构建或线上联调结论；AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4B 阶段本局奖励职责

现有 Networking 目录新增 `CombatPrototypePlayerReward`、`CombatPrototypeKillRewardConfig`/`CombatPrototypeKillRewardEvent` 与 `CombatPrototypeRewardSystem`。玩家组件保存本局金币和经验 GhostField；敌人配置与事件仅服务端保留；既有伤害系统负责在首次死亡时产出致命一击事件，独立奖励系统在其后确认在线玩家并统一写入累计值，原日志系统只读取结果。

## 【CURRENT STRATEGY】第 4B 阶段框架边界

奖励归属复用当前有效网络连接和 CommandTarget，玩家状态复用原 Ghost/连接生命周期；没有独立账号、重连补发或持久去重账本。奖励系统对独立事件隔离失败，当前单次金币/经验/第 4C 物品先准备完整结果，按第 4D 保存成功后同次提交；业务规则由[战斗](Combat.md)与[玩家](Player.md)维护，存档格式由[资源与数据](DataResources.md)维护，资源与日志入口由[运行入口](Runtime.md)维护。

原伤害顺序、体力、移动预测、敌人群体与表现职责保持；不注册到 QFramework Game，不调用正式 PlayerModel/PlayerDataStore，不接正式背包、实体掉落或 UI，也不改变 Scene/Prefab 层级和组件挂载；本局背包范围见第 4C 节。

## 【KNOWN ISSUES】第 4B 阶段接入验收

代码编译、类型/生成的 Ghost Serializer 与实际 SubScene 产物已完成静态核对；用户已确认第 4B 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4B 阶段通过。验收仅覆盖当前独立网络原型的非致命无奖、致命一击归属并获得金币 1/经验 10、每敌人一次奖励和多目标分别结算、双玩家累计独立与双端同步、离线不补发/重入归零及原体力、伤害、死亡显示回归，完整范围见[运行入口](Runtime.md)。第 4A 及此前阶段通过范围保持，不形成规模性能、平台构建或线上联调结论。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4C 阶段本局背包职责

现有 Networking 目录新增 `CombatPrototypeInventoryItem` Ghost 缓冲元素；玩家 Baker 负责空初值，敌人 Authoring/Baker 负责物品奖励配置，伤害系统将物品数据复制到原击杀事件，既有奖励系统负责金币/经验/目标物品整体准备与提交，原日志系统只读背包。名称复用 `Code_01.Msg.ItemName.小块肉`，未修改正式消息定义、正式库存数据或 UI。

## 【CURRENT STRATEGY】第 4C 阶段框架边界

背包随每名玩家 Ghost 独立存在，按原连接生命周期销毁和重新生成，第 4D 在服务端生成时恢复固定 ID 存档，客户端仅同步；归属与在线校验继续由原奖励链承担。该背包没有注册到 QFramework Game，没有新的账号、道具使用、掉落、拾取或复杂事务系统；同名合并由[背包与道具](Inventory.md)维护，统一奖励规则由[战斗](Combat.md)维护，存档职责见第 4D 节。

## 【KNOWN ISSUES】第 4C 阶段接入验收

代码编译、背包 Ghost Serializer/Snapshot 和实际 SubScene 初值已静态核对；用户已确认第 4C 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4C 阶段通过。验收仅覆盖当前独立网络原型的双玩家空初始库存、独立累计与双端同步、非致命无奖、致命一击三项奖励一致、同名合并、每敌人一次和多目标分别结算、离线不补发与重新加入清空，以及原体力、移动、伤害和死亡显示回归，完整范围见[运行入口](Runtime.md)。第 4B 及以前通过结论保持原范围，不形成规模性能、平台构建或线上联调结论。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 【FACT】第 4D 阶段身份与存储职责

开发身份配置负责按进程参数或 Editor World 读取明确 ID；服务端身份组件仅记录已准入 ID；存档数据保存版本、身份、金币、经验与库存条目；独立存储类负责严格读取、候选 JSON 投影和同目录临时文件替换。原握手负责身份占用与恢复后生成，原奖励负责在线归属、完整准备、存档调用和同次 ECS 提交，原日志只读状态；没有新增 ECS 游戏系统或 QFramework 注册项。

## 【CURRENT STRATEGY】第 4D 阶段边界

固定 ID 是客户端声明的开发标识，不承担正式认证；NetworkId 仍用于现有连接、输入和击杀归属。存档服务只在服务端握手与成功奖励路径被调用，不进入客户端预测循环，不读取或覆盖正式 PlayerDataStore 的 JSON，不接账号、正式 UI、掉落或持久去重。Scene/Prefab/Animator 及原组件挂载保持；具体准入由[玩家](Player.md)、提交规则由[战斗](Combat.md)、文件契约由[资源与数据](DataResources.md)维护。

## 【KNOWN ISSUES】第 4D 阶段接入验收

四份新增脚本、原接入系统及 RPC 生成类型已由 Unity 编译并加载，资源结构与差异已静态核对；用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。存档恢复、重复 ID/坏档隔离与保存失败的完整通过范围见[运行入口](Runtime.md)。本地同步序列化和写盘有必要分配与阻塞，本阶段没有规模性能、跨服务器并发、平台构建或线上联调通过结论；AI 未运行逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 5A 阶段观察边界

既有存储类使用 Unity.Profiling.ProfilerMarker 观察原 Load/SavePrepared 调用，不改变握手、奖励或同步职责。测量复用 Unity MCP/Profiler，未新增 ECS 游戏系统或 QFramework 注册项；指标范围与 UNKNOWN 运行状态归[性能基线](Performance.md)。

## 【FACT】第 6A 阶段生命与双向战斗职责

现有 Networking 目录增加五份脚本：PlayerHealth 只定义网络生命；PlayerDamageEvent 定义服务端玩家伤害事件；EnemyAttack 定义服务端攻击配置/状态；EnemyAttackSystem 负责锁定、阶段推进与命中事件；PlayerDamageSystem 负责玩家生命结算和缓冲消费。既有玩家/敌人 Authoring 负责烘焙，两个移动系统与服务端近战系统读取对应死亡/阶段门槛，原日志系统只读生命。

## 【CURRENT STRATEGY】第 6A 阶段接入边界

两个新增游戏系统只进入 ServerSimulation 的 PredictedSimulationSystemGroup，分别排在原 RewardSystem 和新 EnemyAttackSystem 之后；客户端生命来自 Ghost 快照，移动系统继续使用原输入预测并依据同步死亡标记停动。玩家伤害缓冲、敌人攻击配置及状态均限定 Server Prefab；生命四字段同步。资源与静态烘焙事实归[运行入口](Runtime.md)，业务规则归[玩家](Player.md)与[战斗](Combat.md)。

生命链复用既有 Ghost、CommandTarget、LinkedEntityGroup 与 Authoring，没有 QFramework 注册、正式 PlayerModel/PlayerDataStore 接入、存档格式变化或新 UI/表现系统。

## 【KNOWN ISSUES】第 6A 阶段接入验收

编译、生成的生命 Serializer/Snapshot、实际烘焙参数和资源边界已静态核对；用户已确认第 6A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过，完整范围见[运行入口](Runtime.md)。客户端与服务端使用同一第 6A Ghost 版本，旧阶段人工通过范围保持；规模性能仍为 `UNKNOWN`，第 5A 恢复后按用户“不启动，静态检测”完成本轮只读核对，尚未取得运行采样，采集覆盖缺口归[性能基线](Performance.md)。人工通过来自用户反馈，AI 未执行游戏系统、PlayMode、逻辑单元测试或构建。
