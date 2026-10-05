# 运行入口与 UI 调用链

[返回总导航](../AI_Understanding.md)。本页负责启动、控制权与场景接入；属性、背包、战斗和资源细节分别归各模块。

## 入口文件

| 文件 | 作用 |
|---|---|
| [Map.unity](../../Assets/Scenes/Map.unity) | 当前构建列表启用的场景，含 MapCanvas 及其序列化引用 |
| [MapCanvasControl.cs](../../Assets/Scripts/Controller/UIController/Map/MapCanvasControl.cs) | Start 编排架构、场景池绑定与两个面板的 OnStart；初始化完成后接受按键；OnDestroy 清理场景池 |
| [Game.cs](../../Assets/Scripts/Game.cs) | Architecture 注册点 |
| [QFramework.cs](../../Assets/QFramework/Framework/Scripts/QFramework.cs) | Interface 懒初始化、模型/系统初始化、命令与类型事件 |
| [TestController.cs](../../Assets/Test/TestController.cs) | 场景按钮创建敌人和道具 |
| [UIBase.cs](../../Assets/Framework/UI/UIBase.cs) | 工程 UI 基类与 UIManager |
| [YMonoBehaviour.cs](../../Assets/YFramework/Framework/YMonoBehaviour.cs) | 自定义 OnAwake / OnStart 声明 |

## 【FACT】第 0 阶段网络与运行时基础核对

项目当前 Unity 版本为 `6000.5.6f1`。已核对相关包：Input System `1.20.0`；Entities、Entities Graphics、Unity Physics、Netcode for Entities 与 Transport `6.5.0`；Cinemachine `3.1.7`；AI Navigation `2.0.14`；Burst `1.8.29`；Collections `6.5.0`；Mathematics `1.4.0`。

当前 NetCode 入口为 `Assets/Scripts/CombatPrototype/Networking/CombatPrototypeNetCodeBootstrap.cs`；网络启动由独立 `CombatPrototypeNetCode.unity` 主场景上的官方 `OverrideAutomaticNetcodeBootstrap` 标记启用。未启用该标记时只创建默认本地 World；正式 Map 与第 1 阶段切片没有该网络标记。玩家 Ghost、输入和服务端战斗链的当前边界见本页第 2B / 第 3A 阶段。

当前渲染管线为 URP `17.5.0`，使用 Forward+、Linear 色彩空间与 SRP Batcher。Graphics 默认管线及六个 Quality 档位均引用 `Assets/Settings/Rendering/CombatPrototypeUniversalRenderPipeline.asset`，Renderer 为同目录的 `CombatPrototypeUniversalRenderer.asset`；当前画质档仍为 Ultra（索引 5）。渲染配置与人工回归边界见本页第 3B-1 节。

当前 Packages 已包含用户预先添加的 Addressables `2.9.1`，工程存在 `Assets/AddressableAssetsData/` 设置资源。本次网络原型使用 SubScene/Prefab 的显式引用，未调用或迁移 Addressables 加载链；现有 QFramework/Resources 链的迁移不属于本阶段。

## 【FACT】场景绑定

Map 的 `MapCanvas` 对象为激活状态，挂载 `MapCanvasControl`。以下为 Scene 文本与对应脚本 meta 核对结果：

| 引用 | Scene 中的 fileID | 对应对象/组件 |
|---|---|---|
| MapCanvasControl | 271186376 | 脚本 GUID `4d0569183ec2d064e8c075944eb82749` |
| PlayerDetails 字段 | 236714923 | PlayerDetailsControl；GUID `ebb2a445175a0e343a6ae9a7b9b473ff` |
| KnapsackControl 字段 | 1772732472 | KnapsackControl；GUID `bbf53d1c90daa7d40a8486c5811a1b25` |
| KnapsackControl.contextRect | 1099784055 | Content 的 RectTransform；格子挂载入口 |
| MapCanvasControl.ItemParent 字段 | 1747035444 | 已绑定现有 ItemParent 的 RectTransform，所属对象 1747035443；对象层级未变 |

`Knapsack` 对象的序列化初始状态为关闭；MapCanvas 仍通过字段直接调用它的 `OnStart()`。

MapCanvasControl.ItemParent 的序列化引用已核对并留存；第 3 阶段运行反馈与验收结论见本页“未知项与验收状态”。相关自动保存机制与覆盖来源边界见[资源与数据](DataResources.md)，写入及延时复读证据见本月 ChangeLog。

## 【FACT】初始化链

1. Unity 调用 `MapCanvasControl.Start()`，先经 `GetArchitecture() → Game.Interface` 获取架构。首次访问 Interface 时，QFramework 创建 Game 并调用 `Game.Init()`。
2. MapCanvas 从该架构获取已注册的 FactoryUISystem 实例；本次架构初始化完成后才进入场景绑定。
3. Game 注册 `PlayerDataStore` 存储 utility，以及 `PlayerModel`、`LogUtility`、`GoodsModel`、`FactoryUISystem`、`PlayerEventSystem`。
4. QFramework 执行注册补丁入口 `OnRegisterPatch`，随后初始化模型集合，再初始化系统集合。两类集合是 HashSet；源码的注册书写顺序不能当作同类对象之间的稳定初始化顺序契约。
5. PlayerModel 经 PlayerDataStore.Load 读取玩家 JSON；FactoryUISystem.OnInit 不访问场景；PlayerEventSystem 获取 PlayerModel 与 PlayerDataStore。相关细节见[玩家](Player.md)和[资源与数据](DataResources.md)。
6. MapCanvas 调用 `FactoryUISystem.BindScene(ItemParent)`；该方法要求有效的场景引用，缺失即抛错。有效绑定时创建当前场景的三个池，再依次调用 `PlayerDetails.OnStart()`、`KnapsackControl.OnStart()`。玩家详情注册刷新事件并立即展示；背包从 PlayerModel 的 GoodsDict 创建初始格子。当前 Scene 序列化绑定已补齐，用户已确认第 3 阶段初始化正常；验收边界见本页末节。
7. 上述调用完成后才设置 MapCanvas 的初始化标记，Update 据此接受按键。绑定或面板初始化抛出异常时，MapCanvas 清理本场景池并重新抛出原异常；单格失败由背包条目边界记录和隔离。

这是已核实的入口链，不声称它是所有运行场景中首次访问 Game.Interface 的唯一来源。

MapCanvas.OnDestroy 使用缓存的 FactoryUISystem 和 ItemParent 调用 ClearScene，不在销毁阶段重新获取架构。ClearScene 比较绑定来源的引用身份，重复清理无副作用；旧场景的延迟清理不会清掉新场景绑定。池内与尚未归还对象的清理规则见[资源与数据](DataResources.md)。

## 【CURRENT STRATEGY】输入与 UI 控制

| 输入入口 | 现有调用 |
|---|---|
| MapCanvasControl.Update：E | 通过缓存的 FactoryUISystem 实例取野猪对象，localPosition 设为零 |
| MapCanvasControl.Update：I | 通过缓存的 FactoryUISystem 实例取活力苹果对象，localPosition 设为 (300, 0, 0) |
| MapCanvasControl.Update：B | 切换背包对象激活状态 |
| MapCanvasControl.Update：Tab | 切换玩家详情对象激活状态 |
| TestController.Start：enemyBtn / activeBtn | 分别注册 CreateEnemy / CreateItem；点击时获取 Game 中已注册的 FactoryUISystem 实例，创建位置与上述按键相同 |

Map 中存在 TestController 的脚本引用（GUID `42f2add30349522408099dbf10fc3742`）。按钮对应的完整层级与运行点击结果不由脚本字段名推断。

`UIBase.Show/Hide` 走同文件的 `Framework.UI.UIManager`。它维护 CurUI 和历史栈；ShowUI 激活新对象并保存上一对象，没有主动关闭上一对象；HideUI 关闭当前对象再取栈中对象。Map 的 B/Tab 分支直接切换激活状态，未调用这套栈 API。

`YMonoBehaviour` 只定义自定义生命周期方法，未提供 Unity Start 到 OnStart 的自动桥接。当前两个面板的 OnStart 来源是 MapCanvas 的显式调用；不能把“继承 YMonoBehaviour”写成所有对象会自动初始化。

## 【FACT】其他已核实脚本边界

- [MapSceneControl.cs](../../Assets/Scripts/Controller/UIController/Map/MapSceneControl.cs)只声明 playerControl 字段，Map 文本未检出该脚本 GUID；不作为当前启动控制器。
- [PlayerControl.cs](../../Assets/Scripts/Features/Player/Control/PlayerControl.cs)持有 PlayerBodyControl，Start 为空；[PlayerBodyControl.cs](../../Assets/Scripts/Features/Player/Control/PlayerBodyControl.cs)的 Awake 无有效执行逻辑。
- [NavMeshControl.cs](../../Assets/Scripts/AI/NavMeshControl.cs)读取主相机，鼠标左键时输出屏幕点转换后的世界坐标；未执行导航目标设置。
- [Unit.cs](../../Assets/Scripts/Features/Unit/Unit.cs)、[UnitData.cs](../../Assets/Scripts/Features/Unit/UnitData.cs)、[IUnit.cs](../../Assets/Scripts/Features/Unit/Rules/IUnit.cs)包含数据持有、赋值方法和接口。在 Scripts / Framework / Test 的检索范围未发现 Unit / UnitData 构建或继承接入。
- [ShowTime.cs](../../Assets/Scripts/ShowTime.cs)在 Update 中写入格式为 `hh:HH:mm:ss` 的当前时间；字段绑定和使用场景未验收。

## 未知项与验收状态

## 【FACT】第 1 阶段战斗切片入口

新增 `Assets/Scripts/CombatPrototype/` 独立切片脚本：`CombatPrototypePlayerController` 使用 Input System 读取 WASD 并按相机水平朝向移动；`CombatPrototypeOrbitCamera` 提供本地鼠标右键自由旋转和跟随目标；`CombatPrototypeMeleeAttack` 提供鼠标左键/空格触发的前摇、命中、后摇链；`CombatPrototypeHealth` 提供基础生命、受击和死亡停用。脚本不接入 MapCanvas、现有 UGUI 敌人池、ECS 或网络同步。

新增独立场景 `Assets/Scenes/CombatPrototypeScene.unity`：Player 挂载 CharacterController、PlayerController、MeleeAttack、Health；Enemy 位于 Layer 6，挂载 Health 与 CapsuleCollider；Player 的 targetMask 序列化为 Layer 6 位掩码。场景包含 Main Camera/CinemachineBrain、Cinemachine 3.1.7 `CinemachineCamera` 与 `CinemachineThirdPersonFollow`，其 `Target.TrackingTarget` 和 `Target.LookAtTarget` 均绑定 Player Transform；OrbitCameraInput 的 `followTarget` 绑定 Player，`cameraTransform` 绑定 CinemachineCamera Transform。

## 【KNOWN ISSUES】第 1 阶段切片

独立场景绑定已写入 YAML，用户已完成第 1 阶段人工 GamePlayer 验收并确认正常；当前未修改正式 Map 场景、Prefab 或 Animator。该场景未加入正式构建列表。

- `UNKNOWN`：正式启动体验、输入设备与平台要求、调试按键是否属于产品功能。

## 【FACT】第 2B / 第 3A 阶段 NetCode 运行入口

`CombatPrototypeNetCodeBootstrap.Initialize` 通过官方 `DiscoverAutomaticNetcodeBootstrap` 读取启动场景标记。标记启用时，Editor 从启动配置文件创建明确的 Client/Server World 并显式监听、连接，具体规则见本页“Editor 启动配置”；非 Editor 保留端口 `7979`、后台运行及原 `ClientServerBootstrap` 创建链。未启用标记时仍调用 `CreateLocalWorld`，不读取原型启动配置。场景标记判断沿用包对启动时场景尚未有效的处理，不直接依赖早期 `GetActiveScene().name`。

独立主场景为 `Assets/Scenes/CombatPrototypeNetCode.unity`，保留 Main Camera、Directional Light 与自动加载的 `CombatPrototypeNetCodeSubScene`。子场景路径为 `Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity`，唯一 `CombatPrototypeNetworkRoot` 挂载原 `CombatPrototypePlayerSpawnerAuthoring` 和 `CombatPrototypeMapAuthoring`，显式引用玩家、敌人两个 Ghost Prefab 及地图资源；地图配置和资源归[战斗地图](Map.md)。Root 自身没有 Ghost，场景中没有额外玩家或敌人 Ghost 实例。

运行调用链为：`SubScene 地图/Spawner 数据加载 → 客户端读取第 4D 固定 ID 并发送 GoInGame RPC → 服务端唯一握手入口验证身份与存档 → 生成玩家并恢复金币/经验/背包 → GhostOwner / AutoCommandTarget / CommandTarget 绑定 → 玩家加入连接 LinkedEntityGroup → NetworkStreamInGame`。重复或失效 RPC 不重复生成玩家；连接销毁时由 NetCode 的 LinkedEntityGroup 销毁对应玩家。服务端的 `CombatPrototypeEnemySpawnSystem` 从同一个 Spawner 批量生成 `32` 个现有敌人 Ghost，地图提供总量与首点，当前以 `(0, 1, 16)` 为首个网格位置，在 X/Z 平面按 `8` 列、`4` 行、间距 `3` 排列；玩家加入和复活共用地图位置计算，当前仍为 `(NetworkId * 2, 1, 0)`。批量生成只尝试一次，逐项记录和隔离实例化/初始化失败，清理当前项半成品，日志分别记录成功与失败数量。

`Assets/Prefabs/CombatPrototype/` 中的玩家 Ghost 使用 `HasOwner`、`OwnerPredicted`、`SupportedGhostModes=All` 和自动输入目标；敌人 Ghost 使用 `Interpolated`。两者通过官方 `GhostPresentationGameObjectAuthoring.ClientPrefab` 绑定对应 View，并由官方桥接同步 Transform，服务端表现引用为空。敌人根 MeshFilter/MeshRenderer 保留但 Renderer 关闭，EnemyView 绑定腐化荒猪模型；动画与死亡隐藏归[敌人美术](../EnemyArt.md)。

## 【CURRENT STRATEGY】第 3A 阶段观测入口

原型保留日志入口：`CombatPrototypeNetCodeLogSystem` 在 Client/Server World 中记录玩家数量及敌人存活/死亡数量变化，每 2 秒记录玩家位置、旋转、攻击阶段/序号、生命/上限、受击序号及死亡标记，以及各敌人 Ghost ID、位置、HP、受击序号、死亡标记；服务端同时记录各敌人目标 NetworkId。服务端另记录握手、批量生成结果、攻击开始、空间查询命中目标数和事件扣血日志。WASD 按当前本地镜头水平角转换后写入世界 X/Z Move，空格或鼠标左键发送基础攻击事件；第 6B 的 R 键发送手动复活请求，第 7A 的 E 键发送物品使用请求，独立服务端系统记录接受、拒绝和处理失败。状态含义见[玩家](Player.md)、[战斗](Combat.md)与[背包与道具](Inventory.md)。

## 【KNOWN ISSUES】第 2B 阶段验收边界

- 代码编译、Unity 资源绑定与 Editor 配置的 SubScene 烘焙产物已核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。
- 验收范围仅当前独立网络原型：两个玩家加入/退出、跨客户端移动与朝向同步、本地输入预测、基础近战和单敌人生命/受击/死亡状态及 Mono 表现；不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收。
- UI 保留 TODO；房间/匹配、Relay、寻路避障、正式 Map、正式属性奖励与正式存档保持原边界；独立网络奖励与开发固定 ID 存档的当前入口见本页第 4B/4C/4D 节，敌人反击与玩家受伤见第 6A 节。第 3A 已接入群体生成、最近在线玩家追踪及空间查询伤害链，验收边界见下节。
- 当前渲染管线为 URP，玩家与敌人使用官方 GameObject 表现桥接；第 3B-2 原占位显示已由用户确认人工 GamePlayer 验收通过，当前敌人美术/动画人工验收仍 UNKNOWN，范围见[敌人美术](../EnemyArt.md)。Console 中的既有 No SRP、PEListener 序列化与 DOTween 弃用记录不能作为本阶段运行验收通过的依据，诊断记录见 ChangeLog。
- AI 未执行本阶段逻辑单元测试、PlayMode、命令行构建或平台发布；人工 GamePlayer 通过结论来自用户明确反馈。
- `UNKNOWN`：第 3 阶段清单之外的面板交互与显示效果、生命周期调用组合，以及全部场景组件的完备性。第 4 阶段已完成两个面板和详情文本监听的静态生命周期接入，人工交互验收仍待主线程确认。
- 第 2 阶段玩家状态与存储改动已有用户“实际行为和日志均已核对正确”的反馈，并由主线程结合代码、文档、资源静态检查判定该阶段通过；不扩展为全部场景或平台验收。
- 第 3 阶段人工 GamePlayer 清单（初始化、生成/回收复用、失败项隔离、退出后重新进入 Map，以及实际行为和日志）已获用户“已确认正常”的反馈；主线程结合代码、资源、文档及引用留存静态验收，已判定该阶段通过。第 4 阶段 UI 刷新与释放代码已完成静态落地，详情使用效果和平台验证仍未覆盖。

相关模块：[玩家](Player.md)、[背包与道具](Inventory.md)、[战斗](Combat.md)、[框架与工具](Framework.md)。

## 【FACT】第 3A 阶段群体运行入口

CombatPrototypeEnemySpawnSystem 在服务端网络接收之后、握手系统之前从既有 Spawner 生成敌人。Spawner 仍是 SubScene 唯一 Root 的组件，保留玩家/敌人 Prefab 显式引用及原组件挂载；敌人 Prefab 在既有 Authoring 上保存速度 2 和停止距离 1.5。主场景结构、Bootstrap、连接生命周期和玩家输入接入不变。

## 【CURRENT STRATEGY】第 3A 阶段运行与观测

群体调用链为：批量生成 → 最近在线玩家选择与移动 → 空间索引/近战筛选 → 伤害事件统一结算 → Ghost 状态同步 → 客户端敌人实体渲染与死亡隐藏；玩家保留 Mono 表现。目标选择、移动和伤害只在服务端执行；无在线玩家时敌人停止，死亡后停止移动和受击。敌人总数保留死亡实体，alive + dead = total；客户端统计基于各自已接收的 Ghost 状态，目标 NetworkId 仅服务端记录。

## 【KNOWN ISSUES】第 3A 阶段验收边界

第 3A 代码已经 Unity 编译，Editor 保存的 Prefab/SubScene 参数和实际烘焙产物已读取核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。验收仅覆盖当前独立网络原型的双客户端 32 敌人生成、最近在线玩家选择与玩家加入/退出后的目标切换、停止距离与无在线玩家时停止追踪、群体近战每次攻击每目标去重及伤害 25、死亡后停止移动和受击，以及双端 HP、死亡状态与存活/死亡统计一致性。第 2B 的通过结论只覆盖此前单敌人原型范围；本次第 3A 通过结论不扩展为正式 Map、平台构建、大规模性能或线上联调验收。AI 未执行逻辑单元测试、PlayMode、命令行构建或平台发布，也未读取图片。当前使用 URP，玩家保留 Mono 表现；敌人 Entities Graphics 接入的验收边界见第 3B-2 节，敌人反击/玩家受伤见第 6A 节，寻路避障仍未接入。

## 【FACT】第 3B-1 阶段渲染配置与材质

`Packages` 中 URP、Universal Config、Shader Graph 与 Render Pipelines Core 均为 `17.5.0`；Searcher `4.9.4` 是 Shader Graph 的解析依赖。URP 全局设置为 `Assets/UniversalRenderPipelineGlobalSettings.asset`，其默认 Volume Profile 为 `Assets/DefaultVolumeProfile.asset`，当前 components 为空；包级配置还包括 `ProjectSettings/URPProjectSettings.asset` 与 `ShaderGraphSettings.asset`。

玩家与敌人两份 View 的原根 MeshRenderer 仍引用 `Assets/Materials/CombatPrototype/CombatPrototypeNetworkView.mat`，Shader 为 `Universal Render Pipeline/Lit`，白色、不透明、Metallic=0、Smoothness=0.5、无贴图；两者当前均关闭。实际角色由各自 VisualRoot 的独立模型和材质显示，分别归[玩家美术](../PlayerArt.md)与[敌人美术](../EnemyArt.md)。两份 Ghost 的 ClientPrefab 引用对应 View，ServerPrefab 均为空；敌人 Ghost 根的 Capsule 网格与原材质仍保留，但根 Renderer 关闭。

管线 MSAA 为 `1`（关闭多重采样），Render Scale 为 `1`；URP 将当前 Ultra 的 `QualitySettings.antiAliasing` 同步为 `0`。`lightsUseLinearIntensity` 与 `lightsUseColorTemperature` 均为 true，由 URP 依据当前 Linear 配置设置。Graphics/Quality 文件使用当前 Unity 序列化版本及其默认字段。

## 【CURRENT STRATEGY】第 3B-1 阶段渲染接入边界

URP 与 Linear 是全项目配置，影响所有场景；玩家保留原 Mono 表现桥接，敌人当前表现见第 3B-2 节。群体生成、追踪、近战、伤害、Ghost 同步及 32 敌人配置保持第 3A 已验收逻辑；场景结构、构建列表和自定义 UI Shader 保持原状。第 3B-1 的验收仅覆盖管线前置配置，不包含第 3B-2 实体渲染。

## 【KNOWN ISSUES】第 3B-1 阶段验收边界

主线程已核对包版本、磁盘与编辑器的 Forward+/Linear/SRP Batcher 配置、六档管线引用、两份材质绑定和实际 SubScene 烘焙产物，静态验收通过。用户已确认本阶段人工 GamePlayer 验证通过，主线程结合静态检查与用户反馈判定第 3B-1 阶段通过。人工验收范围仅覆盖网络场景外观、亮度与边缘效果、Map UI/TMP/自定义 Outline Shader 兼容，以及切换 URP/Linear 后双端 32 敌人群体行为回归；不扩展为平台构建、大规模性能或线上联调验收。第 3A 人工通过结论保持原验收范围。AI 未运行 PlayMode、逻辑单元测试、命令行构建、平台发布或图片检查；第 3B-1 人工通过结论不扩展为第 3B-2 敌人实体渲染与死亡隐藏验收。

## 【FACT】第 3B-2 阶段敌人实体渲染

`CombatPrototypeNetworkEnemy.prefab` 保留既有根节点及 MeshFilter/MeshRenderer，原占位 Renderer 关闭。GhostPresentationGameObjectAuthoring 的 ClientPrefab 绑定既有 EnemyView，ServerPrefab 为空；根层级、Ghost 参数、生命与移动参数保持。EnemyView 的 VisualRoot 绑定腐化荒猪模型与 Animator，资源与当前显示策略归[敌人美术](../EnemyArt.md)。玩家仍使用原 PlayerView 桥接。

当前 SubScene 隔离 Editor 烘焙已读回：Spawner 为 1 个，敌人配置为 32 个、8 列、间距 3；实际出生位置来自地图配置，见[战斗地图](Map.md)。敌人 Prefab 根含 Prefab、LocalTransform、原生命/攻击数据、新表现状态与 GhostPresentationGameObjectPrefabReference，LinkedEntityGroup 含根和表现资源引用两实体；根不含 MaterialMeshInfo，ClientPrefab 已核对为 EnemyView，ServerPrefab 为空。没有执行游戏 World 或 PlayMode。

## 【CURRENT STRATEGY】第 3B-2 阶段显示调用链

`CombatPrototypeEnemyRenderSystem` 仅在 ClientSimulation World 的 PresentationSystemGroup、EntitiesGraphicsSystem 之前执行：无官方 GameObject 表现引用的敌人仍按 IsDead 控制 MaterialMeshInfo，带此引用的占位渲染保持关闭，两类查询都包含已禁用的渲染组件。当前敌人显示由官方 GameObject 桥接到 EnemyView，动画读取同步阶段、剩余时间与生命；侧倒后隐藏，晚加入对已死亡敌人直接隐藏。不写生命、世界根位置、伤害或奖励，不销毁死亡实体，完整规则归[敌人美术](../EnemyArt.md)。

## 【KNOWN ISSUES】第 3B-2 阶段验收边界

原占位渲染系统、Prefab 与烘焙产物已静态核对；用户已确认第 3B-2 人工 GamePlayer 验证通过，主线程结合既有静态检查与用户反馈判定该阶段通过。验收仅覆盖当时独立网络原型的双端占位敌人显示与移动、死亡隐藏无重复显示或残影、重新加入后的死亡状态，以及 HP/存活/死亡统计一致性。第 2B、第 3A 与第 3B-1 通过结论保持各自原验收范围；不包含规模性能、平台构建或线上联调结论。该人工通过结论不覆盖当前腐化荒猪与 Animator 显示，当前资源静态落地已通过、人工 GamePlayer 仍 UNKNOWN，归[敌人美术](../EnemyArt.md)。原人工结论来自用户反馈，AI 未运行逻辑单元测试、PlayMode、命令行构建或平台发布。

## 【FACT】第 4A 阶段网络玩家资源与烘焙

`Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkPlayer.prefab` 的既有玩家 Authoring 保存 `InitialPower=100`、`UpperPower=100`、`AttackPowerCost=10`。Prefab 仍为原单根节点与原 5 个组件，玩家 Mono 表现引用和 Ghost 参数保持原值；新增的是烘焙后的 `CombatPrototypePlayerResource` ECS 数据，没有增加 Mono 组件挂载或修改 Scene/SubScene 层级。

现有 SubScene 的 Editor 配置已重新烘焙并只读反序列化核对：玩家含体力组件，当前值/上限为 100/100，成本为 10，初始攻击阶段 Ready、序号 0；移动速度 5 与原近战参数保持。Spawner 仍为 1 个，敌人 32 个、8 列、间距 3、首位置 `(0, 1, 2)`；敌人 HP100、速度2、停止距离1.5、伤害缓冲和根实体渲染组件保持，玩家仍引用原 PlayerView。读取用临时 World，systems=0，读取后释放，没有执行游戏系统。

## 【CURRENT STRATEGY】第 4A 阶段观测入口

`CombatPrototypeNetCodeLogSystem` 在原每 2 秒玩家快照中增加 `power=CurrentPower/UpperPower`，两端分别记录各自服务端状态或已收到的 Ghost 状态。服务端近战日志增加攻击接受/拒绝原因：`ReadyAndPowerAvailable`、`InsufficientPower`、`AttackInProgress`，并包含玩家 NetworkId、体力和攻击序号；规则由[战斗](Combat.md)维护。输入、移动预测、握手、敌人群体和显示调用链沿用原入口，没有新增 UI。

## 【KNOWN ISSUES】第 4A 阶段运行验收

代码编译、新组件类型加载、玩家 Prefab 参数及实际烘焙数据已完成静态核对；用户已确认第 4A 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4A 阶段通过。验收仅覆盖当前独立网络原型的双玩家独立体力及双端同步、成功启动攻击一次扣 10（含空挥）、忙碌输入拒绝且阶段继续推进、10 次成功启动后耗尽并拒绝后续攻击且序号不增加、无自动恢复、断线重新加入恢复 100，以及原移动、群体伤害和死亡显示回归。既有第 2B、第 3A 与第 3B 人工通过结论保持原范围，本阶段不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、平台发布或图片读取。

## 【FACT】第 4B 阶段奖励资源与烘焙

`Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkEnemy.prefab` 在既有 Enemy Authoring 保存 RewardCoin=1、RewardExperience=10；相对执行前副本仅新增这两个参数，根节点、7 个组件、Ghost 与渲染引用不变。玩家 Authoring 只在 Baker 中添加本局奖励组件初值 0/0，玩家 Prefab 文本未修改；三份新增脚本的 meta 由 Unity 生成。

现有 SubScene 的 Editor 配置已定点重新烘焙并只读反序列化：玩家 Coin/Experience=0/0；敌人奖励配置=1/10，奖励与伤害事件缓冲长度均为 0。玩家体力 100/100、成本 10、Ready/序号 0、速度 5、近战 25/2/100/0.18/0.08/0.3 保持；Spawner=1、敌人 32、8 列、间距 3、首位置 (0,1,2)、HP100/速度2/停止1.5 保持，玩家 Mono 表现及敌人根实体渲染引用保持。临时读取 World 的 systems=0，读取后释放，没有执行游戏系统；四份网络 Prefab 均无缺失脚本。

## 【CURRENT STRATEGY】第 4B 阶段奖励观测入口

原每 2 秒玩家快照记录 coin 与 experience，当前还包含第 4C 背包日志，两端分别记录自身已持有的 Ghost 状态。服务端在首次死亡后记录 reward queued，在成功同次写回后记录 reward granted、当次奖励和 totalCoin/totalExperience；攻击者离线记录 reward skipped 与 AttackerOffline，数据缺失或结算异常输出带敌人、NetworkId、攻击序号和原因的错误日志。日志入口保持 `CombatPrototypeNetCodeLogSystem`，没有新增 UI；具体规则见[战斗](Combat.md)和[玩家](Player.md)。

## 【KNOWN ISSUES】第 4B 阶段运行验收

新增组件与系统已由 Unity 编译并加载，奖励 Ghost Serializer/Snapshot、Prefab 参数与实际烘焙已完成静态核对；用户已确认第 4B 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4B 阶段通过。验收仅覆盖当前独立网络原型的非致命无奖、致命一击归属并获得金币 1/经验 10、每敌人一次奖励和多目标分别结算、双玩家累计独立与双端同步、离线不补发/重入归零，以及原体力、伤害和死亡显示回归。既有阶段通过范围保持，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片读取。

## 【FACT】第 4C 阶段本局背包资源与烘焙

现有 `CombatPrototypeNetworkEnemy.prefab` 的 Enemy Authoring 保存 RewardItemName=小块肉、RewardItemQuantity=1，Coin=1、Experience=10 保持；根节点、7 个既有组件、Ghost 与渲染引用保持。玩家 Baker 新增空背包缓冲，玩家 Prefab 没有新增序列化参数或组件挂载；新增缓冲脚本的 meta 由 Unity 生成。

现有 SubScene 的 Editor 配置实际产物已重新烘焙并只读核对：玩家背包长度 0、Coin/Experience=0/0、体力 100/100、成本 10、Ready/序号 0；敌人奖励小块肉 1、金币 1、经验 10，两类事件缓冲均为空。玩家速度与近战、Spawner 的 32 敌人/8 列/间距 3/原点 (0,1,2)、敌人 HP100/速度2/停止1.5、根实体渲染及原 PlayerView 引用保持。读取时临时 World 的 systems=0，读取后释放；原 Editor/Streaming World 数量前后均为 6，没有创建或运行游戏系统。

## 【CURRENT STRATEGY】第 4C 阶段背包观测入口

`CombatPrototypeNetCodeLogSystem` 以只读 BufferLookup 访问背包，原每 2 秒玩家快照增加 `inventoryEntries`；每个条目另记录同一 World/玩家 NetworkId 下的 `inventoryItem` 和 `quantity`。空背包通过 inventoryEntries=0 表示；客户端记录已接收的 Ghost 状态，不发奖或自行改库存。服务端 reward queued/skipped/granted 日志包含物品名与当次数量，成功日志另含 totalItemQuantity；金币/经验原日志字段保持。

## 【KNOWN ISSUES】第 4C 阶段运行验收

Unity 编译、背包生成的 Ghost Serializer/Snapshot、Enemy Prefab 参数与实际产物已静态核对；用户已确认第 4C 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定第 4C 阶段通过。验收仅覆盖当前独立网络原型的双玩家空初始背包、独立累计与双端同步、非致命无奖、致命一击金币/经验/小块肉三项奖励一致、同名合并、每敌人一次和多目标分别奖励、离线不补发与重新加入空背包，以及原体力、移动、伤害和死亡显示回归。第 4B 及此前验收范围保持，不扩展为规模性能、平台构建或线上联调验收。人工通过结论来自用户反馈，AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片读取。

## 【FACT】第 4D 阶段资源与观测入口

现有 Networking 新增 `CombatPrototypeDevelopmentIdentity`、`CombatPrototypePlayerIdentity`、`CombatPrototypePlayerSaveData`/`CombatPrototypePlayerSaveItem`、`CombatPrototypePlayerSaveStore` 四份脚本，meta 由 Unity 正常导入生成。既有 NetCodeLifecycle、RewardSystem、NetCodeLogSystem 分别接入准入恢复、保存后结算与身份日志；Scene、Prefab、Animator、既有 meta、正式玩家链及包/构建设置保持本阶段执行前状态。

配置入口是 [CombatPrototypeDevelopmentIdentity.json](../../UserSettings/CombatPrototypeDevelopmentIdentity.json)，当前明确配置 `ClientWorld → player-a`；完整配置规则和存档格式见[资源与数据](DataResources.md)。

服务端接受日志包含 NetworkId、PlayerId、restored、金币/经验、库存条目数与实际路径；重复 ID 记录 `PlayerIdAlreadyOnline`，准入/读取错误包含身份、路径和原异常。成功奖励日志为 `reward granted and saved`，失败日志为 `reward settlement or save failed`，原每 2 秒快照另记录服务端 `persistentPlayerId` 与 NetworkId 的映射。客户端只记录所声明的 ID 和已收到的 Ghost 状态，不读取或写入服务器存档。

## 【KNOWN ISSUES】第 4D 阶段人工运行验收

新增类型和包含 PlayerId 的 GoInGame RPC Serializer 已由 Unity 编译并加载，四份网络 Prefab 无缺失脚本，实际 persistentDataPath 为 `C:/Users/91611/AppData/LocalLow/DefaultCompany/Code_01`。静态检查覆盖准入先校验、同更新重复身份隔离、候选存档先写后提交与文件差异；AI 未运行游戏系统、PlayMode、逻辑单元测试、命令行构建、平台发布或图片检查。

用户已确认第 4D 人工 GamePlayer 验收通过，主线程结合既有静态验收与用户反馈判定该阶段通过。以下为本阶段人工通过范围，客户端与服务端使用同一第 4D 代码版本：

- 用不同固定 ID 加入；无档玩家为金币/经验 0/0、空库存和体力 100，两人的奖励与 JSON 独立且双端状态一致。
- 每次击杀金币 1、经验 10、小块肉 1 一起到账；同名累计；同 ID 断线重入及服务端重启后恢复这三项，体力仍重置为 100。
- 同 ID 已在线时新连接被拒绝，原玩家继续运行；配置缺失/非法 ID 被明确拒绝。
- 离线状态下损坏 JSON、改变 Version 或构造非法库存后，该 ID 加入失败且原文件保留；其他合法 ID 仍能加入和结算。
- 人工使保存失败后，该次三项均不增加、已有正式文件保留；失败事件不补发，其他独立事件继续。存储恢复后，后续成功奖励可继续保存。
- 原移动、体力扣费、非致命无奖、每敌人一次、多目标分别结算及伤害、死亡显示回归。

第 4B/4C 当时重入归零的通过结论保持原范围，第 4D 恢复通过来自本阶段用户反馈。当前只覆盖开发固定 ID 与服务端本地同步文件写入；正式认证、跨服务器并发存档、存档迁移、规模性能、平台构建和线上联调仍未验收。人工通过结论不表示 AI 执行过游戏系统、逻辑单元测试、PlayMode、构建、发布或图片检查。

## 【FACT】第 5A 阶段性能入口

首轮已确认规模为 2 玩家/32 敌人，沿用当前网络场景和人工 GamePlayer；存档读写 Profiler 标记已编译并注册。用户恢复该阶段后明确要求“不启动，静态检测”，本轮以第 6A 已验收版本核对配置、编译后的结算顺序、标记边界和临时助手覆盖缺口；Editor 未进入 PlayMode，Profiler 关闭，采集助手未注册回调。尚无运行采样，运行结果与最终性能验收仍为 UNKNOWN。采集口径、静态发现、环境、指标与限制统一归[性能基线](Performance.md)，本页不复制性能报告。

## 【FACT】第 5B 阶段采集助手静态验收

项目外助手 5B-1 按用户确认方案补齐 World/Tick、玩家生命/体力、敌人数量/阶段、连接 RTT、预测误差、Frame Timing 与负载事件记录；采集控制、World 只读查询与清理、原生结果检查分别由启动模板及两个新增辅助文本负责，状态查询继续独立。组合代码与状态入口已通过 C#6 内存编译；启动返回 static-compiled-not-armed、armed=false，状态为 not-armed 且无停止回调，原 6 个 Editor/Loading World 保持。主线程静态验收通过，仅覆盖实现、API、默认关闭和改动边界；运行数据有效性与性能验收仍为 UNKNOWN，完整覆盖与限制归[性能基线](Performance.md)。

## 【FACT】第 6A 阶段资源与实际烘焙

现有 CombatPrototypeNetworkPlayer.prefab 的原 Authoring 增加 InitialHealth=100、MaxHealth=100；现有 CombatPrototypeNetworkEnemy.prefab 的原 Authoring 增加 AttackDamage=10、AttackRange=1.75、AttackStartupSeconds=0.5、AttackRecoverySeconds=1。仅写入上述序列化参数，玩家仍为 5 组件/0 子节点，敌人仍为 7 组件/0 子节点；原挂载、两份 View、Scene/SubScene 结构及既有 meta/GUID 保持本轮执行前状态。

原 SubScene 的 Editor 配置已定点重新烘焙，实际产物只读核对：玩家生命 100/100、HitSequence=0、IsDead=0、玩家伤害缓冲长度 0；敌人反击参数与上述 Prefab 一致，Ready、计时/序号 0、空锁定目标。玩家体力 100/100、成本 10、原近战参数、Coin/Experience=0/0 与空库存保持；Spawner=1、敌人 32/8 列/间距 3/原点 (0,1,2)、敌人 HP100/速度2/停止1.5、原伤害/奖励空缓冲与根实体渲染组件保持。

五份新脚本及两个系统已由正常 Unity 导入编译，生命 Ghost Serializer/Snapshot 包含 CurrentHealth、MaxHealth、HitSequence、IsDead。烘焙读取用临时 World，systems=0，读取后释放，原 Editor/Loading World 前后均为 6；没有执行游戏系统。新脚本 meta 由 Unity 自动生成，仅原 Editor 烘焙缓存随重新导入更新。

## 【CURRENT STRATEGY】第 6A 阶段运行观测

每 2 秒玩家日志在原字段上增加 `HP=当前/上限`、hit、dead；服务端反击日志记录敌人实体、锁定玩家/NetworkId、攻击序号、Startup/Recovery，以及 TargetOfflineOrDead、TargetOutOfRange 空击原因。事件结算日志记录攻击来源、玩家 HP/受击/死亡；死亡后攻击输入记录 PlayerDead。玩家死亡继续使用日志验收，原 Mono 玩家显示保留。

完整服务端顺序、锁定目标与一次命中规则归[战斗](Combat.md)，玩家死亡门槛及重入生命归[玩家](Player.md)，生命不入存档的契约归[资源与数据](DataResources.md)。新增 Ghost 字段要求双客户端与服务端使用同一第 6A 版本。

## 【KNOWN ISSUES】第 6A 阶段人工运行验收

主线程已判定实现与静态验收通过；用户明确反馈“验收已通过，继续下一阶段”，主线程结合既有静态验收与用户反馈判定第 6A 阶段通过。以下为本阶段人工通过范围，客户端与服务端使用同一第 6A 版本：

1. 两个不同固定 ID 玩家以同一版本加入，初始生命各为 100/100，受击序号/死亡独立且双端一致；敌人距离内前摇后每序号只命中一次，伤害 10，前摇/后摇停止移动。
2. 锁定玩家在命中前移出 X/Z 距离 1.75、断线或死亡时本次空击并进入后摇，进行中的挥击不切换目标；下一轮可选择其他最近在线存活玩家。
3. 玩家与敌人同 tick 到达命中时，已被玩家击杀的敌人无反击伤害；事件消费后不重复扣血，原敌人伤害、死亡显示与首次击杀奖励不重复。
4. 玩家生命归零后移动/朝向和攻击停止，新的攻击输入不再扣体力或增加攻击序号，敌人排除该玩家；死亡实体仍保留原显示。重新加入恢复生命 100/100、受击序号 0、未死亡，并按固定 ID 恢复金币/经验/背包。
5. 原体力、近战伤害、多目标结算、金币/经验/小块肉保存、重连恢复及失败隔离回归。32 敌人的伤害允许叠加；该人工通过范围不包含规模性能，尚未取得运行性能数值。

第 4D 及以前的人工通过结论保持原范围，第 5A 已由用户要求恢复后改为本轮静态检测，尚无运行采样，性能通过仍为 UNKNOWN。本次第 6A 人工通过来自用户反馈，不扩展为规模性能、平台构建或线上联调；AI 未运行逻辑单元测试、GamePlayer/PlayMode、游戏系统、命令行构建、发布或图片检查。

## 【FACT】第 6B 阶段编译与资源边界

现有 CombatPrototypePlayerInput 增加 Respawn 输入事件；新增独立 CombatPrototypePlayerRespawnSystem，原玩家 Baker 已添加该输入组件，未改 Authoring、Scene/SubScene、Prefab、Animator、View 或原组件挂载。新脚本通过 Unity 定点导入，唯一新增 meta 由 Unity 自动生成，GUID 为 6d3fa6f03e7768847bf7faaf4e24e604；既有 meta/GUID 和资源文本与本轮开始前一致。

Unity 已编译并加载包含 Move、Attack、Respawn 的输入类型，以及生成的 InputEventHelper、输入缓冲 Serializer、Send/Receive/CompareCommandSystem。复活系统含 ISystemCompilerGenerated，编译后的特性为 ServerSimulation、PredictedSimulationSystemGroup、UpdateAfter(CombatPrototypePlayerDamageSystem)；与原八项特性连成既有战斗之后的复活链。未创建服务端/客户端 Game World、调用游戏系统或进行 SubScene 定点烘焙读取。

## 【CURRENT STRATEGY】第 6B 阶段观测与版本

复活日志记录 NetworkId、玩家实体、生命/体力、受击与攻击序号、位置、取消的旧前摇数及解除的旧锁定数；存活和归属不一致请求分别记录 PlayerAlive、CommandTargetOwnerMismatch，失败记录连接、玩家、阶段与原始异常。既有每 2 秒状态日志继续读取复活后的状态。生命与复活条件归[玩家](Player.md)，旧敌人锁定和执行顺序归[战斗](Combat.md)。

第 6B 引入 Respawn 时要求服务端与客户端命令布局一致；当前第 7A 版本约束见本页第 7A 节。原第 4D JSON 契约未改，复活不读写存档。

## 【KNOWN ISSUES】第 6B 阶段验收

主线程已核对源码、正常 Unity 编译、生成输入类型、编译后的顺序特性及文件改动边界，判定第 6B 静态验收通过；用户随后明确反馈“我已验收通过，接下来下一阶段”，主线程结合既有静态核对与用户反馈判定第 6B 阶段通过。静态落地仅修改原输入脚本、新复活脚本及其 meta、对应五份文档；既有资源、其他脚本、项目设置及用户设置保持当时执行前状态。

AI 执行第 6B 静态落地时 Editor 未进入 PlayMode，Profiler 关闭，采集会话及停止回调均不存在；正常编译触发 Domain Reload 后，仅有 6 个 Editor/Loading World。当时 Console 为 0 条 Error、6 条 Warning：5 条既有 Server Tick Batching 记录，以及本轮编译报告的既有 PEListener.cs:17 字段 args 序列化分析警告 UAC1001；该脚本未修改，未清空 Console。

用户人工通过范围覆盖同一第 6B 版本的双玩家死亡后 R 键复活、满生命/满体力、原加入位置及双端同步、存活拒绝、重复输入、独立状态、旧前摇取消与旧后摇保持、攻击/受击序号及奖励/库存保持、原移动/攻击/存档回归。复活仍没有保护时间，32 敌人可在后续 tick 再次造成伤害。

人工通过结论来自用户明确反馈；AI 未新增或运行逻辑单元测试、GamePlayer/PlayMode、游戏系统、命令行构建、发布、性能采样或图片读取。本结论不扩展到规模性能、平台构建或线上联调，第 5A/5B 与第 6A 既有结论保持原范围。

## 【FACT】第 7A 阶段编译与资源边界

现有 CombatPrototypePlayerInput 增加 UseItem，输入类型含 Move、Attack、Respawn、UseItem；NetCode 的输入事件辅助、输入缓冲 Serializer 与 Send/Receive/CompareCommandSystem 已加载。新增 CombatPrototypeItemUseSystem 含 ISystemCompilerGenerated，编译后特性为 ServerSimulation、PredictedSimulationSystemGroup、UpdateAfter(CombatPrototypeEnemySpatialSystem)、UpdateBefore(CombatPrototypeMeleeServerSystem)，与既有链连成十项顺序。现有存储类增加消费候选投影，原加载、奖励投影、SavePrepared 和 JSON v1 保持。

唯一新增脚本 meta 由 Unity 自动生成，GUID 为 a227d368e6f698741adef1dd18595c6a；玩家/敌人 Baker、Scene/SubScene、Prefab、Animator、旧 meta/GUID、View 和组件挂载均与本轮基线一致。没有新增玩家组件、RPC 或存档字段。

## 【CURRENT STRATEGY】第 7A 阶段观测与版本

客户端 E 键按下当帧发送使用请求，服务端成功日志记录 NetworkId、PlayerId、玩家、物品、消耗/剩余数量、实际恢复量和最终体力；拒绝原因包含 CommandTargetOwnerMismatch、PlayerDead、AttackInProgress、PowerFull、InsufficientItem。处理失败日志保留连接、玩家、NetworkId、阶段与原异常，既有每 2 秒状态日志继续读取库存和体力。规则归[背包与道具](Inventory.md)，体力归[玩家](Player.md)，顺序归[战斗](Combat.md)。

UseItem 已改变输入命令布局，服务端与所有客户端必须使用同一第 7A 版本。原 v1 存档仍兼容，消费后的库存持久化，体力不入 JSON；第 6B 复活保持原规则。

## 【KNOWN ISSUES】第 7A 阶段验收

主线程已完成正常 Unity 编译、源码及编译后类型/顺序特性核对，判定第 7A 静态验收通过；用户明确反馈“验收通过，继续下一阶段”，主线程结合既有静态核对与用户反馈判定第 7A 阶段通过。

第 7A 静态落地时 Editor 未进入 PlayMode，Profiler/录制关闭，采集会话及停止回调为空，仅有 6 个 Editor/Loading World；当时 Console 查询为 0 条 Error、1 条 MCP WebSocket 未初始化 Warning，未清空 Console。以下为本阶段人工通过范围，服务端与客户端使用同一第 7A 版本：

1. 两名不同固定 ID 玩家使用同一第 7A 版本；存活、Ready、有小块肉且体力不足时按 E，库存扣 1、体力最多增加 30 并截到上限，最后一份消耗后移除条目。按住 E 不按帧连续消耗。
2. 死亡、攻击阶段非 Ready、满体力和无物品分别拒绝，使用操作不改库存/体力；存活且 Ready 的 E+攻击先恢复再沿原成本扣费，满体力 E 先拒绝。死亡时 E+R 只在末尾按原规则复活，不重放 E。
3. 双玩家独立消费，服务端与两客户端库存/体力一致；使用操作保持金币/经验、生命与攻击/受击序号，原近战、敌人伤害、奖励和复活回归。
4. 人工制造保存失败时，该使用操作两项均保持、旧正式档保留，错误有阶段和原异常，其他玩家仍可处理；没有自动重复扣除。
5. 同 ID 重连及服务端重启恢复消费后的库存，体力仍为烘焙 100/100；v1 档内无零数量条目，空 Items 仍可加载，后续击杀可继续按原规则入包。

本阶段人工通过结论来自用户反馈；AI 未新增或运行逻辑单元测试、GamePlayer/PlayMode、游戏系统、命令行构建、发布、性能采样或图片读取。第 6A/6B 及以前的人工通过范围保持；第 5A/5B 尚无运行样本，同步写盘成本、规模性能、平台和线上联调仍为 UNKNOWN。

## 【FACT】Editor 启动配置入口

菜单 `Tools/CombatPrototype/启动配置` 打开 [CombatPrototypeStartupSettingsWindow.cs](../../Assets/Scripts/Editor/CombatPrototypeStartupSettingsWindow.cs) 定义的 EditorWindow。窗口提供单机/联机、Host/Client/Server、客户端 IPv4、联机端口和后台运行配置；“保存并校验”写入已验证文件，“重新读取”舍弃窗口候选值并读取磁盘。未保存值不会参与启动；PlayMode 或即将切换 PlayMode 时禁用启动字段和保存/重读操作，并展示本次成功启动的不可变快照。

`CombatPrototypeNetCodeBootstrap.Initialize` 在场景标记启用的 Editor 入口读取一次配置。每次 Initialize 先清空旧快照，将 AutoConnectPort 设为 0，重设默认地址及官方传输构造器，再按配置创建所需 World。新入口不调用原自动 World/ThinClient 创建链，NetCode PlayMode Tools 的角色、自动连接地址和端口不决定本次 World 数量或连接端点；联机 Host 的本机客户端传输仍沿用官方驱动选择及网络模拟设置。

| 模式 | 本次创建 | 监听/连接 |
|---|---|---|
| SinglePlayer | ServerWorld + ClientWorld | 两端只注册 IPC；监听与连接为 127.0.0.1:7979，7979 为进程内通道编号，不开放 UDP 接入 |
| Online / Host | ServerWorld + ClientWorld | 服务端沿官方默认驱动监听 0.0.0.0:Port；本机客户端连接 127.0.0.1:Port |
| Online / Client | ClientWorld | 明确使用 UDP 驱动，连接 ServerAddress:Port |
| Online / Server | ServerWorld | 沿官方默认驱动监听 0.0.0.0:Port，没有本地客户端 |

启动先创建并监听服务端，再创建并连接客户端；使用官方 CreateServerWorld/CreateClientWorld、NetworkStreamDriver.Listen/Connect。成功发起启动后记录配置快照和模式/角色/端点/后台运行日志；该日志不表示网络握手或玩家准入已经完成。配置读取/校验、监听或启动调用失败时，记录路径和原异常，清理本次新增的客户端/服务端 World 并恢复此前默认注入 World、后台运行和官方传输构造器，不回退为其他模式。

玩家生成继续走原固定 ID、GoInGame RPC、存档恢复和连接绑定链；战斗、奖励、复活、物品使用及 Ghost 字段保持。单机仍使用服务端逻辑和原存档；同一存档根目录中的同一 ID 对应同一文件。新配置链仅在 UNITY_EDITOR 编译，非 Editor 启动行为未接入该文件。

窗口显示现有开发身份及来源，提供身份文件定位和原 SubScene/玩家/敌人 Prefab 的资源选择入口，不保存身份或覆写玩法参数。生命、体力、移动、攻击及奖励数值仍由所属 Authoring Inspector 和既有烘焙链负责；地图原点与敌人总量由[地图默认配置](Map.md)提供，列数/间距保留原 Spawner 值，不写入启动 JSON。Editor 启动配置本身未改变 Scene/Prefab 层级或旧组件挂载。

## 【KNOWN ISSUES】Editor 启动配置验收边界

主线程已核对实现符合确认方案、Unity 编译完成、窗口正确读取实际配置、文档同步及修改边界，判定代码与文档静态验收通过；新增模式的人工 GamePlayer 行为、连接结果、失败清理及连续模式切换仍为 `UNKNOWN`。既有第 2B～第 7A 人工通过范围不扩展为本次启动配置通过；AI 未启动 PlayMode、执行逻辑单元测试、构建、发布、性能采样或读取图片。

人工验收范围：单机一名本地玩家及原战斗/奖励/复活/物品使用；单机 IPC 驱动与外部进程无法加入；联机 Host/Client 使用不同固定 ID 的加入、同步和存档；Client/Server World 数量；缺失/非法配置及监听失败的暴露与清理；单机→联机→单机重复进入；Map 和第 1 阶段入口回归。窗口当前场景提示仅用于使用说明，真正的启动范围仍由现有官方场景标记判断。

## 【FACT】网络原型本地跟随镜头

[CombatPrototypeNetCode.unity](../../Assets/Scenes/CombatPrototypeNetCode.unity) 的现有 Main Camera 挂载 [CombatPrototypeFollowCamera](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeFollowCamera.cs)，controlledCamera 显式引用同对象的 Camera。当前透视 FOV=35、固定俯角 40°、跟随显示根节点的 Y 偏移 0.5、默认距离 18、范围 12～26、缩放步长 1、位置与缩放平滑时间 0.12s、水平旋转步长 45°、旋转过渡 0.18s；原 Camera Transform、物体层级及其余组件保持。

[CombatPrototypeCameraBindingSystem](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeCameraBindingSystem.cs) 仅在 ClientSimulation 的 PresentationSystemGroup 执行。相机 Start 向官方 ClientServerBootstrap.ClientWorld 中的该系统注册；系统从本 World 的 CombatPrototypePlayerNetCode + GhostOwnerIsLocal 以 SystemAPI.Query 枚举 GhostOwnerIsLocal 启用匹配并确认唯一拥有者，经 GhostPresentationGameObjectSystem.GetGameObjectForEntity 获取既有 PlayerView，并在完成官方 Transform 桥接作业后读取显示位置。没有本地玩家属于准入/断线等待；多个本地玩家、必需相机或 PlayerView 缺失显式失败。Server-only 没有本地客户端注册。

## 【CURRENT STRATEGY】本地镜头与输入

Z/X 单次按下改变水平目标角，鼠标滚轮改变观察距离；角色转身不驱动镜头旋转。当前水平角在原 GhostInputSystemGroup 输入采集时每个渲染帧推进一次，同一角度用于 WASD 转换和表现阶段镜头姿态。移动输入仍通过原 Move/预测/服务端链，具体边界归[玩家](Player.md)；空格/鼠标左键攻击、R 复活和 E 物品使用保持。

首次绑定或重连新玩家在输入转换前重置默认方向与距离，首次显示直接对齐并清除跟随速度。死亡仍观察本人；同步生命从死亡转为存活时直接对齐复活位置、清除位置平滑速度，保留当前旋转和缩放。无本地玩家时解除目标并保持最后相机姿态；SubScene 停止、Scene 释放或 World 销毁时清理绑定。

## 【KNOWN ISSUES】本地跟随镜头验收边界

源码、Unity 编译、类型加载和 Main Camera 的实际序列化绑定已静态核对；用户明确反馈尚未进行人工验收，人工 GamePlayer 的视野比例、跟随手感、输入方向及生命周期回归仍为 UNKNOWN。既有网络战斗人工通过结论不扩展为本次镜头通过。当前未新增地图边界、遮挡处理或角色表现修正；未改 Ghost Prefab、SubScene、Animator、旧 meta、包或构建配置。

人工验收范围为直行/斜行/急停/转身、持续按 W 时 Z/X 旋转、滚轮缩放上下限、双客户端只跟随本人且视角独立、死亡/复活/断线重连，以及原攻击/E 物品/R 复活回归。AI 未启动 GamePlayer/PlayMode、执行游戏系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【FACT】网络原型 PlayerView 美术接入

原 PlayerView 根节点增加 CombatPrototypePlayerNetCodeView，新增一个 VisualRoot 嵌套角色实例，位置 (0,-1,0)，关闭原胶囊 Renderer；原 Owner、根 Transform 与 Ghost ClientPrefab 引用保持。VisualRoot 使用两层 Animator 与实际序列化绑定的 CombatPrototypePlayerAnimation，完整资源映射和动画规格归[玩家美术](../PlayerArt.md)。Scene/SubScene、玩家 Ghost Authoring、敌人显示与项目设置保持本轮开始时内容。

## 【KNOWN ISSUES】玩家美术与动画人工验收

主线程代码/资源静态验收通过，人工 GamePlayer 尚未确认。检查范围：双端玩家均显示灰衣修士且只有一份模型；WASD 待机/移动切换及移动中挥刀；攻击前摇/命中动作/后摇与原扣费/伤害回归；受击、死亡倒地保持、R 复活恢复站立；断线重连与新观察者不重放旧受击或死亡；镜头旋转/缩放与原 E 物品使用回归。角色细节、实际光照、动画衔接与运行性能仍为 UNKNOWN。

Clip 图片由隔离 Editor 预览场景采样生成，仅用于本次获授权的新资源视觉检查。AI 未启动 GamePlayer/PlayMode、执行业务系统、逻辑单元测试、命令行构建、平台发布或性能采样；既有网络阶段人工通过结论不扩展为本次角色运行通过。

## 【FACT】战斗地图入口

当前网络 SubScene 选择 SourceMode=Json、Preset=Forest，五份 JSON 已显式绑定；可在 PlayMode 前保存 Preset 切换到 Grassland，或显式选择 BuiltIn。配置经校验与 LayoutBuilder 烘焙地图单例及缓冲，修改 JSON 须完成正常导入/烘焙后再进入。当前配置契约为 schemaVersion=5、configRevision=7，树木和 gather_apple 按生态密度依次布置。客户端在 Presentation 阶段、Entities Graphics 前创建分块地表和草丛/碎石；砍伐开启时树木和采集物由服务端在准入前生成插值 Ghost，关闭砍伐时保留原静态树。客户端按同步状态控制树木/采集物显示。H 成功砍倒生成 wood ×3，默认 600 秒后在原点空闲时恢复 Standing/显示，阻挡按砍倒/再生历史重建，再次砍伐须新 H。默认 gather_apple 保存成功后耗尽，服务端模拟计时 600 秒后在原点恢复 Available，客户端恢复显示；再生不发物品，再次采集须新 F 请求。玩家 Client/Server 预测移动及服务端敌人移动读取烘焙阻挡缓冲，经过共享扫掠/滑动工具；服务端沿原 Spawner 生成敌人，GoInGame 和复活使用共用出生位置函数。服务端独立生成敌人首次死亡的额外苹果，驱动飞行/落地、G 拾取保存及到期清理，客户端接收插值 Ghost；地面掉落不写盘。配置文件、参数、所有权与清理职责由[战斗地图](Map.md)及[掉落与拾取](MapDrops.md)维护。

## 【KNOWN ISSUES】战斗地图第一阶段验收

主线程已核对 Unity 编译、两种模板的隔离 Editor 烘焙、网格数据、资源绑定和授权修改边界，代码与文档静态验收通过。用户已明确确认第一阶段人工 GamePlayer 验收通过；主线程结合静态结果与用户反馈判定第一阶段通过。验收限下述人工清单，性能、平台构建和线上联调仍为 UNKNOWN。AI 未启动 PlayMode、执行游戏模拟系统、逻辑单元测试、命令行构建、发布或读取图片。

已通过的人工验收范围：

1. 分别在 PlayMode 前保存 Forest、Grassland，确认 96×96 米地图的三类地表、草丛和碎石显示，区块无裂缝/重叠；出生区、十字道路、战斗空地及敌人初始排列区没有装饰侵入。静态装饰不阻挡原移动。
2. 确认初始 32 个敌人、8 列、间距 3，首点 (0,1,16)；玩家加入仍按 NetworkId×2 排列、脚部与地表对齐，R 复活回到与加入相同的地图位置。
3. 双客户端检查地表与静态装饰一致、镜头旋转/缩放和本人跟随；回归原移动、攻击、受击/死亡/复活、E 物品使用、奖励保存及断线重连。
4. 停止并再次进入、重复切换两种模板，以及 SubScene/World 释放时确认没有重复地表、装饰或遗留对象；Console 无地图生成、资源绑定和清理错误。运行性能另有验收口径，本清单不视为性能通过。

## 【KNOWN ISSUES】战斗地图第二阶段 JSON 验收

JSON 读取、五份 TextAsset 引用及两种模板的隔离 Editor 烘焙已静态核对，主线程代码、资源与文档静态验收通过；第二阶段人工 GamePlayer 的配置修改生效、异常配置反馈、来源/模板切换与原玩法回归仍为 UNKNOWN。以下清单保留第六阶段 schemaVersion=4、configRevision=5 的参数示例；当前入口与验收恢复值以第八阶段 v5/revision=7 清单为准；第一、第三阶段人工通过不替代本节独立的 JSON 验收清单。

人工验收范围：

1. 在 PlayMode 前修改 battle_forest_01.json 的 defaultSeed（12345 改为 23456）及 configRevision（5 改为 6），等待成功导入/烘焙后进入；确认装饰布置改变、地表与保护区域仍符合配置。将 biomes.json 中 forest 的 decorationDensityPer100m2 从 8 改为 4，重新进入并核对草丛数量变化；树木密度已由第三阶段启用，采集密度已由第四阶段启用。
2. 将选中地图 population.initialEnemyCount 从 32 改为 16，重新烘焙并进入，确认实际敌人总量和装饰排除区域使用新配置；Spawner 的旧 EnemyCount 序列化值不控制该总量。加入/复活位置、镜头、双客户端一致性及原移动/攻击/受击/死亡/R 复活/E 物品/奖励存档/重连仍须回归。
3. 分别核对缺少 JSON 引用、缺字段/重复字段、错误类型、不支持 schemaVersion、地图 ID 不匹配及无效资源键的明确错误；修复后须重新成功烘焙，SourceMode 不应自行切回 BuiltIn。
4. 在 PlayMode 前保存并切换 Json/BuiltIn 来源及 Forest/Grassland 模板，停止重进后检查地表/装饰和清理。验收结束恢复 SourceMode=Json、Preset=Forest 及五份 JSON 初始值（种子 12345、修订号 5、forest 草丛密度 8、敌人 32、采集物启用再生/600 秒及 drops 默认值）。

运行热重载、联网配置一致性校验、性能、平台构建和线上联调未覆盖。AI 未启动 GamePlayer/PlayMode、执行游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或读取图片。

## 【KNOWN ISSUES】战斗地图第三阶段树木与移动阻挡验收

Unity 编译、树木显式资源绑定、两种模板的隔离 Editor 烘焙、阻挡记录对应关系、占地/同类间距、道路/安全区及敌人出生避让已静态核对，主线程静态验收通过。用户明确反馈“我已验收通过，接下来下一阶段”，主线程结合既有静态核对与用户反馈判定第三阶段通过；人工通过范围限下述七项清单，来自当时 v2 版本；当前恢复值跟随 v4/revision=5 默认配置，采集、再生与掉落行为另见第四至第六阶段。AI 未运行游戏模拟/显示系统、PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查；结论不扩展为第二阶段独立 JSON 清单、规模性能、平台构建或线上联调通过。

人工验收范围：

1. 分别保存 Forest、Grassland 后进入，核对普通树木显示、底部高度、生态分布及道路/出生区/战斗空地/敌人初始区域避让。树冠与逻辑占地分别判断；草丛、碎石仍不阻挡。
2. 玩家从四个方向接近树干，核对停止位置、斜向与切向滑动、不穿树、保持地面高度及原速度/朝向。静止、连续移动与反复靠近时的卡顿、抖动仍须人工检查；初始重叠允许向外移动，不主动传送或挤出。
3. 让敌人朝树木另一侧的存活玩家追踪，核对阻挡、滑动或停止及双端同步；当前没有全局绕路，不能要求绕过成片障碍。最近在线目标切换、前后摇停动、距离 1.5 停止及原近战/反击仍须回归；攻击遮挡未接入。
4. 两个客户端使用相同配置，核对树木位置一致、本地预测与远端位置表现；回归死亡/R 复活、镜头、E 物品使用、奖励保存、断线重连及原玩家/敌人生成。
5. PlayMode 前把三种生态的 treeDensityPer100m2 全部设为 0，完成导入/烘焙后重新进入，核对树木和阻挡记录为空、移动恢复无树状态；treeObjectId 与 Prefab 绑定仍必填。恢复密度后，将 tree_normal 的 blocksMovement=false，重新进入核对树仍显示但不阻挡；再恢复 true 并把占地半径 0.5 改为 0.75，核对停止距离与布置避让变化。
6. 核对缺失树木绑定、无效 treeObjectId、缺少 movement 段、阻挡物零占地、非法角色半径/留缝/滑动次数及旧 schemaVersion 的明确错误；修复后重新成功烘焙，不补默认值或回退来源。
7. 停止重进、切换 Forest/Grassland 和 Json/BuiltIn，核对没有重复树木、地图或遗留对象。验收结束恢复 Json/Forest、schemaVersion=4、configRevision=5、种子 12345、32 敌人、树木密度 0.4/1.5/0.1、树木占地 0.5、间距 3、blocksMovement=true，以及 movement=0.4/0.45/0.01/3。

第三阶段通过范围仅覆盖静态物体移动阻挡；第四阶段采集另列下节。地图边界、地表通行/速度倍率、寻路、攻击遮挡、砍树、树木再生、通用动态对象、地图状态存档和联网配置一致性协议未接入，性能与平台验收仍为 UNKNOWN。

## 【KNOWN ISSUES】战斗地图第四阶段采集验收

正常 Unity 编译、Gather 输入、新系统顺序、状态 Ghost Serializer 和服务端配置/进度特性已静态核对；两种模板从保存 SubScene 克隆至临时 Editor 场景隔离烘焙，通过资源引用、占地/间距及保护区域检查。默认 Forest 为 89 树木/阻挡记录、36 采集点，Grassland 为 53 树木/阻挡记录、38 采集点；静态结果不表示交互或跨端显示通过。用户明确反馈“我已验收通过，接下来下一阶段”，主线程结合既有静态核对与用户反馈判定第四阶段通过，人工通过范围限下述八项清单，来自第四阶段 schemaVersion=3、configRevision=3、regrowEnabled=false 的版本；再生验收见第五阶段，当前默认及掉落验收见第六阶段。第一/第三阶段既有通过与第二阶段独立 JSON UNKNOWN 保持；结论不扩展为规模性能、平台构建或线上联调通过。人工结论来自用户反馈，AI 未运行游戏模拟/显示系统、GamePlayer/PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

已通过的人工验收清单（第四阶段再生关闭，客户端与服务端使用同一 v3/revision=3 配置及含 Gather 的输入版本）：

1. 分别保存 Forest、Grassland 后进入，核对 gather_apple 显示、底部高度和生态分布，避开道路/安全区/战斗区/敌人初始区域，不与树木或碎石占地重叠；采集物不阻挡玩家/敌人移动。树木阻挡、草丛/碎石及地表继续回归。
2. 存活且静止、近战 Ready 的玩家在采集中心 X/Z 2 米内单次按 F，核对 Gather started 日志、1 秒后 Gather granted and saved、活力苹果增加 1，并在双端隐藏耗尽点。背包沿每 2 秒日志核对，金币/经验保持；同名累加，不生成新 UI。范围内多个点取最近 Available，精确同距取较小 placement；范围外、全耗尽时明确拒绝，持续按住 F 不连续发放。
3. 采集中分别输入移动、攻击，遭受命中、死亡、离开范围或断线，核对取消日志、不入包且预约释放；移动因树木受阻仍应取消。攻击已在阶段中、死亡、非零移动输入时 F 不开始；R 复活不补发死亡前预约或同 tick 的 F。原移动/攻击/受击/死亡/R 回归。
4. 两名玩家在同一可用点范围内争抢：只允许一名预约；同次服务端请求按 NetworkId 升序，另一人可选择其他 Available 点或被拒绝。重复 F 不重置预约时长，不多发物品；预约者取消后另一人须有新 F 请求才能开始。实际同次请求、同距目标与跨端同步单独记录结果，未触发的用例保持 UNKNOWN。
5. 人工制造既有玩家存档保存失败条件，核对 settlement or save failed 日志、活力苹果数量不增加、点未耗尽、预约释放及原正式档保留；恢复存储后不会自动补发，重新按 F 才可成功。其他玩家/采集点和原独立击杀奖励仍可继续；保存失败条件及实际覆盖需记录。
6. 在同一服务端局中断线重连、晚加入，核对当前 Available/Collecting/Depleted 状态一致，耗尽点不重新出现，库存按原固定 ID 恢复。停止服务端再进入则重新生成全部点，已保存苹果仍保留；E 仍只使用小块肉，苹果不触发新使用效果。
7. PlayMode 前把三种生态 gatherableDensityPer100m2 设为 0，重新导入/烘焙并进入，确认无采集点且 F 拒绝，gatherObjectId 和绑定仍必填。恢复密度后调整采集距离/时长/产出数量及种子/revision，重新烘焙核对生效；缺失 gatherObjectId/绑定、引用非采集物、未知 yieldItemId、非正交互/时长/数量、开启任一阻挡/再生或旧版本须明确报错，无默认补全或来源回退。
8. 停止重进、切换 Forest/Grassland 及 Json/BuiltIn，核对无重复地表、静态物体或采集 Ghost，SubScene/World 释放无遗留。回归镜头、32 敌人/8 列/间距 3/首点 (0,1,16)、加入/复活位置、近战反击、E 小块肉、奖励与保存/恢复。

第四阶段验收时恢复值为 Json/Forest、schemaVersion=3、configRevision=3、seed=12345、32 敌人、树木密度 0.4/1.5/0.1 和原 movement 参数；采集密度 0.6/0.5/0.2、gatherObjectId=gather_apple、占地 0.3、间距 1.5、距离 2、时长 1、vitality_apple ×1，三类阻挡/再生关闭。当时再生未接入；再生验收见第五阶段，当前默认见第六阶段。地图状态存档、砍树、攻击遮挡、资源 UI、运行配置热重载和联网配置一致性协议仍未接入。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】战斗地图第五阶段采集物再生验收

第五阶段 schemaVersion=3、默认 configRevision=4，gather_apple 为 regrowEnabled=true、regrowSeconds=600。正常 Unity 编译、仅服务端配置/期限字段及共享 Ghost 仍为原三字段已静态核对；保存 SubScene 克隆至临时 Editor 场景的两个模板隔离烘焙均与 BuiltIn 值一致，Forest/Grassland 仍为 36/38 采集点、89/53 树木及阻挡记录，占地/同类间距/保护区域无违规。主线程静态验收通过；用户明确反馈“我已验收通过，接下来下一阶段”，主线程结合既有静态核对与用户反馈判定第五阶段通过，人工范围限本节八项清单。人工结论来自用户反馈，AI 未运行游戏系统或 GamePlayer/PlayMode；不扩展为第二阶段独立 JSON、规模性能、平台构建或线上联调通过。

已通过的人工验收清单（客户端与服务端使用同一第五阶段配置及代码）：

1. PlayMode 前将 objects.json 的 gather_apple.regrowSeconds 临时设为 5，regrowEnabled=true；选中地图 configRevision 临时改为 5，完成正常导入/烘焙再进入。记录本人苹果初始数量，在 2 米内静止单次 F，确认 1 秒后只加 1、保存成功且点隐藏。以服务端模拟时间从成功耗尽起计，5 秒前保持 Depleted，到期一次 Gather regrown 日志、原位置恢复显示；计时不从按 F 时起算。
2. 针对同一 placement 连续完成至少两个“采集→耗尽→再生”周期。每次再生保持同一服务端 point/布置索引与位置，没有新增或重复 Ghost；再生本身不改变苹果/金币/经验或写盘。须新 F 才能再采集，持续按住 F、耗尽期间反复 F 不累发物品、不延长期限。两个成功周期合计只增加 2 个苹果，并可沿既有每 2 秒背包日志核对。
3. 双客户端核对耗尽隐藏、到期原点恢复和再次采集同步。在计时期间晚加入/断线重连仍接收 Depleted，到期后晚加入接收 Available；不能以客户端登录时间重启期限。新一轮两人争抢仍只有一人预约，重复 F、中断及原固定 ID 库存恢复规则回归，未实际覆盖的精确同 tick/同距用例保持 UNKNOWN。
4. PlayMode 前改为 regrowEnabled=false、regrowSeconds=5 并重新烘焙/进入；成功采集后超过 5 秒模拟时间仍耗尽、没有再生日志。恢复 true 后重新进入，再完成两个周期；树木/草丛/碎石没有获得再生或采集能力。
5. 临时 5 秒配置下分别取消预约（移动/攻击/受击/死亡/超距/断线），以及制造既有存档保存失败条件。点恢复 Available 且库存不增加，随后超过间隔没有本次请求的再生日志；恢复存储不会自动发奖或安排再生，须新 F 成功并保存后才开始期限。记录实际失败条件，其他玩家/点及击杀奖励继续处理。
6. 在 Depleted 等待期间停止服务端再进入，确认全部采集点从 Available 重建、旧期限不恢复，已保存苹果按原固定 ID 保留。新局首次成功后才重新计时；当前期限不写入玩家存档。停止重进、释放 SubScene/World 时无采集实体或地图遗留。
7. PlayMode 前修改再生间隔，重新正常导入/烘焙核对生效；启用时 0、负数及非有限数须明确报错，关闭时负数/非有限数仍报错。缺字段/错误类型/三类阻挡、非法产出和旧 schema 错误规则保持，不补默认值、不回退来源。修改 runtime JSON 不形成热重载。
8. 分别回归 Forest/Grassland 与 Json/BuiltIn；BuiltIn 默认间隔为 600 秒，临时 5 秒仅来自修改后的 Json。回归原地表/树木阻挡/移动滑动、镜头、32 敌人及出生/复活、近战反击、E 小块肉、奖励保存/恢复；苹果仍仅入包，无新 UI 或使用效果。

第五阶段验收时恢复值为 SourceMode=Json、Preset=Forest、schemaVersion=3、两份地图 configRevision=4、seed=12345、32 敌人及原空间/movement 参数；采集密度 0.6/0.5/0.2、占地 0.3、间距 1.5、距离 2、时长 1、vitality_apple ×1、三类阻挡关闭、regrowEnabled=true、regrowSeconds=600。当前配置恢复值见第六阶段。第五阶段通过结论来自用户反馈，范围限本节清单；未实际覆盖的精确同 tick/同距用例仍为 UNKNOWN。第二阶段独立 JSON 清单仍为 UNKNOWN，第一/第三/第四阶段既有通过范围保持。性能、平台构建与线上联调未验收；AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】战斗地图第六阶段动态掉落与拾取验收

正常 Unity 编译、Pickup 输入及生成命令类型、DropState 四项 Ghost 字段和生成序列化器、仅服务端 Progress 特性及系统顺序已静态核对。保存 SubScene 克隆至临时 Editor 场景的 Forest/Grassland 隔离烘焙均与 BuiltIn 一致，为 schemaVersion=4、configRevision=5；drops 配置及独立插值掉落 Prefab 有效。原 36/38 采集点、89/53 树木/阻挡记录、地表/草丛/碎石、96×96 米和出生配置保持，空间违规为 0。原主场景干净，烘焙前后 Console 为 0 Error/2 个既有源码 Warning。主线程静态验收通过；用户明确反馈“我已验收通过，接下来下一阶段”，主线程结合既有静态核对与用户反馈判定第六阶段通过，范围限本节九项人工清单，人工结论来自用户反馈。未实际触发的临界距离、精确同距与同 tick 用例仍为 UNKNOWN；第一/第三/第四/第五阶段既有通过与第二阶段独立 JSON UNKNOWN 保持。AI 未运行游戏系统、GamePlayer/PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

人工入口为 CombatPrototypeNetCode，按既有启动配置进入 SinglePlayer 或 Online。所有端须使用相同代码、Ghost 资源和 v4 地图配置；下述为本阶段获用户确认的人工清单，未实际触发的独立用例仍为 UNKNOWN，静态烘焙不替代运行验收：

1. 分别核对非致命命中无掉落、敌人首次死亡额外出现一份活力苹果 ×1；反复攻击尸体不再生成，多敌人分别死亡各生成一次且 DropId 不重复。原攻击者仍沿既有统一链领取金币 1/经验 10/小块肉 1；地面苹果是额外产出，不直接入包，不因原奖励保存失败或攻击者离线被取消。
2. 核对默认 0.4 秒飞行、0.6 米散落半径及相对线性轨迹 0.6 米弧高，终点位于地表基准加 0.05 米，统一缩放 0.5。只存在空中掉落时 G 拒绝；落地后本人 X/Z 2 米内单次 G 可拾取。范围外、死亡、移动输入、攻击请求或近战非 Ready 均拒绝；移动被树木挡住仍按输入拒绝，R 不补发死亡时的 G。
3. 多份已落地掉落取最近目标，精确同距取较小 DropId；只提交一份数量，连续按住 G 不连续拾取，重复请求不再消费同一掉落。临界距离、精确同距与同 tick 用例须记录实际结果，未触发的保持 UNKNOWN。
4. 两名玩家争抢同一掉落，仅一人入包，任意合格玩家可拾取，无击杀者专属。同次服务端请求按 NetworkId 升序处理，另一人可取其他符合条件的目标或被拒绝；成功后两端移除掉落。按原每 2 秒背包日志核对苹果增量、原金币/经验及小块肉奖励归属。
5. 人工制造既有玩家存档保存失败条件，核对 Pickup settlement or save failed 日志、库存数量不增、未到期掉落仍为 Landed、旧正式档保留；恢复存储后不自动补发，须新 G。未到期前允许其他合格玩家重新请求；到期仍可清理。记录失败条件及实际覆盖，确认其他玩家/掉落与原独立奖励继续处理。
6. 双端观察飞行/落地，晚加入及断线重连接收当前剩余掉落；已消耗的掉落不重放。成功拾取后重连和服务端重启按原固定 ID 恢复苹果库存；未拾取地面掉落随服务端重启清空，不恢复旧 DropId、位置或期限。
7. PlayMode 前将 lifetimeSeconds 临时设为 3、选中地图 revision 改为 6，正常导入/烘焙后核对从生成模拟时间起到期清理，期间没有请求不入包；改为 0 后不自动到期。enabled=false 后无新掉落，原统一奖励/F 采集保持。核对缺少 drops/字段、旧 v3、未知 itemId/资源键、非正数量/距离/飞行时长/缩放，以及负数/非有限散落/弧高/贴地偏移/寿命的明确错误，不补字段或回退来源；修复后重新成功烘焙。
8. 停止重进、切换 Forest/Grassland 与 Json/BuiltIn，释放 SubScene/World，确认无重复掉落、遗留实体或销毁错误；飞行中、落地等待中及拾取/到期清理后分别覆盖。旧共享网格、材质、采集 Prefab 保持可用。
9. 回归地表/树木阻挡、镜头、32 敌人/8 列/间距 3/首点 (0,1,16)、加入/复活点、移动/近战/反击/受击/死亡/R/E 小块肉、原奖励保存/恢复、F 预约/中断/保存失败以及至少两轮采集物原点再生。G 不替代 F，苹果没有新增使用效果或 UI。

验收结束恢复 SourceMode=Json、Preset=Forest、两份地图 schemaVersion=4/configRevision=5、seed=12345、32 敌人及原空间/movement/生态配置；gather_apple 的 2 米/1 秒/vitality_apple ×1/三类阻挡关闭/启用再生 600 秒保持。drops 恢复 enabled=true、itemId=vitality_apple、quantity=1、visualResourceKey=drop_apple、pickupDistanceMeters=2、flightDurationSeconds=0.4、scatterRadiusMeters=0.6、arcHeightMeters=0.6、groundOffsetMeters=0.05、visualScale=0.5、lifetimeSeconds=600。详细规则归[掉落与拾取](MapDrops.md)。第六阶段人工通过仅限上述九项清单，未实际覆盖的独立用例仍为 UNKNOWN；性能、平台构建与线上联调未验收，不扩展既有阶段结论。

## 【KNOWN ISSUES】战斗地图第七阶段树木砍伐与资源掉落验收

本节保留第七阶段 v5/revision=6、原 H 操作的验收口径；当前操作和跨类型选择见本页“地图资源统一 F 的人工验收”。

正常 Unity 编译、H 的 HarvestTree 输入/生成命令类型、TreeState 四项 Ghost 字段/Serializer、系统顺序及仅服务端 Progress 已静态核对。Forest/Grassland 的隔离 Editor 烘焙均与 BuiltIn 完整值一致，为 schemaVersion=5/configRevision=6；默认砍伐 2 米/2 秒/wood ×3 和新树木/木材 Ghost 有效，四类地图 GhostType 互不重复。原布局、89/53 初始树木阻挡、36/38 采集点、96×96 米、32 敌人/8 列/间距 3/首点 (0,1,16) 保持，空间违规为 0。木材占位网格 18 顶点/32 三角形、非退化且朝外，旧资源散列保持。主线程静态验收通过；用户明确反馈“我已验收通过，接下来下一阶段”，主线程结合既有静态核对与用户反馈判定第七阶段通过，范围限本节十项人工清单及 schemaVersion=5/configRevision=6 版本。人工结论来自用户反馈；未实际触发的临界距离、精确同距、同 tick、延迟/预测回放及创建/清理/回滚失败分支仍为 UNKNOWN。

人工入口为 CombatPrototypeNetCode，按既有启动配置进入 SinglePlayer 或 Online。所有端须使用同一代码、输入布局、Ghost 和 v5 配置；以下为该阶段人工验收范围，结论来自用户反馈，静态烘焙不替代运行验收：

1. 分别在 Forest/Grassland 核对树木位置/数量及初始阻挡；本人 X/Z 2 米内单次 H 预约最近 Standing 树，2 秒后仅生成一份木材堆 ×3，树木隐藏并解除阻挡。木材经过原 0.4 秒飞行/落地，空中 G 拒绝，落地 G 才入包；砍伐完成时库存、金币/经验不直接变化。
2. 核对范围外、死亡、非零移动输入、攻击请求或近战非 Ready 时 H 拒绝；树挡住移动仍按输入取消。预约中移动/攻击/受击/死亡/超距/断线均取消，不产木材、不解除阻挡；恢复后新 H 可重新开始。重复 H 不重置期限，持续按住 H 不自动连续砍树，完成/取消当次不自动预约下一棵，R 不补发死亡时请求。
3. 同时存在多树时选择最近 Standing，精确同距取较小 PlacementIndex。两名玩家争抢同树仅一人预约，服务端同次请求按 NetworkId 升序；已预约树不被另一个请求覆盖，另一玩家可以选择其他合格树。F/H 同次 F 优先；F 正在采集中拒绝 H，砍伐中按 F 取消砍伐并沿原采集链处理。精确同距、临界距离与同 tick 用例未触发时保持 UNKNOWN。
4. 核对砍倒后本人/远端玩家和服务端敌人可通过原阻挡圆；相邻未砍树仍阻挡。树木状态与隐藏在双端一致；晚加入及断线重连接收本局 Felled，不重建静态树副本。观察本地预测回放/纠正及同 tick 边界，记录可见抖动与实际覆盖；未覆盖的回放/延迟场景保持 UNKNOWN。
5. 再次 H 或攻击已砍树不重复产出；多树分别完成各产一堆 ×3。同时制造敌人苹果与树木木材掉落，核对所有 DropId 唯一且目标按原最近规则；G 后苹果/木材分别入对应库存，同名累计，无击杀者/砍伐者专属。多人争抢同一木材堆只入包一次，原金币/经验/小块肉归属保持。
6. 按既有方法制造玩家保存失败，核对 G 错误日志中的实际 wood/DropId、木材库存不增、未到期掉落仍 Landed、旧正式档保留；恢复后须新 G，不自动补发。独立玩家和苹果/F/原奖励继续处理。完成后以固定 ID 重连及服务端重启恢复已入包木材，未拾取掉落清空，树木按原布局恢复 Standing。
7. 在可控条件下覆盖木材创建/树木提交失败，记录 Tree completion failed 及原异常、地图/布置/玩家/物品/资源。核对没有有效半成品或重复掉落，树木恢复可预约、原阻挡保持，需新 H；其他树/玩家继续。不能实际触发的创建、清理或回滚失败保持 UNKNOWN，不以配置烘焙错误替代运行失败。
8. PlayMode 前分别修改采集距离（objects.tree_normal.interactionDistanceMeters）、砍伐时长、木材数量及 map revision，核对正常导入/烘焙后生效。treeHarvest.enabled=false 时恢复原静态树/阻挡、H 不产木材，F/G/战斗保持；drops.enabled=false 只关闭敌人额外掉落，砍伐木材仍正常飞行/拾取/到期。缺少 treeHarvest/字段、旧 v4、非正/非有限时长、未知物品/资源/树木 ID、非正数量或所选物体不是有效阻挡树木均明确报错，不补字段、不回退 BuiltIn。修复后重新成功烘焙。
9. 按第六阶段方法临时设 drops.lifetimeSeconds=3，再核对木材生成起计时到期、不自动入包；为 0 时不自动到期。飞行中、等待中、砍倒后及取消/完成后停止重进、切换 Json/BuiltIn 和 Forest/Grassland、释放 SubScene/World，确认无重复树木、掉落或销毁错误；旧共享网格/材质/Prefab 保持。
10. 回归地表/草丛/碎石、镜头、出生/复活点、移动/近战/敌人反击/受击/死亡/R/E 小块肉、原奖励保存/恢复、敌人额外苹果/G、F 预约/中断/保存失败/至少两轮采集物原点再生（可沿第五阶段方法临时改为 5 秒，验收后恢复 600 秒）。H 只接逻辑砍伐；没有斧头要求、动画、树桩、木材使用效果或新增 UI。

验收结束恢复 SourceMode=Json、Preset=Forest、两份地图 schemaVersion=5/configRevision=6、seed=12345、32 敌人及原空间/movement/生态/出生配置；treeHarvest 恢复 enabled=true、treeObjectId=tree_normal、visualResourceKey=tree_harvest、harvestDurationSeconds=2、dropItemId=wood、dropQuantity=3、dropVisualResourceKey=drop_wood，树木交互距离恢复 2。drops 恢复第六阶段的 enabled=true、vitality_apple ×1、drop_apple、距离 2、飞行 0.4、散落/弧高 0.6、贴地 0.05、缩放 0.5、寿命 600；gather_apple 原 2 米/1 秒/苹果 ×1/关闭阻挡/启用再生 600 秒保持。

详细规则归[树木砍伐](MapTreeHarvest.md)与[掉落与拾取](MapDrops.md)。原第一/第三/第四/第五/第六阶段通过范围保持，第二阶段独立 JSON 人工清单仍为 UNKNOWN；第七阶段人工通过仅限本节清单，未实际触发的独立用例和规模性能、平台构建与线上联调仍为 UNKNOWN。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】战斗地图第八阶段树木原点再生验收

本节保留第八阶段 v5/revision=7、原 H 操作的验收口径；当前操作和跨类型选择见本页“地图资源统一 F 的人工验收”。

正常 Unity 编译当前无 C# Error，TreeBlockingEvent 的 TransitionTick/Disabled 两项 Ghost 字段与 Serializer/Snapshot 已生成，TreeProgress.RegrowAt 仅服务端，TreeRegrow 在 TreeHarvest 之后、PlayerRespawn 之前已静态核对。Forest/Grassland 隔离 Editor 烘焙均与 BuiltIn 完整值一致，为 schemaVersion=5/configRevision=7；tree_normal 的 regrowEnabled=true/regrowSeconds=600 已进入物体定义及 TreeSettings，原树 Prefab 烘焙历史为空、RegrowAt=0。原 2 米/2 秒/wood ×3、89/53 初始树木及启用阻挡、36/38 采集点、9 块/2304 格/96×96 米、32 敌人/8 列/间距 3/首点 (0,1,16)、NetworkId=1 玩家点 (2,1,0) 保持，空间违规为 0。烘焙前后 Console 均为 [0 Error,8 Warning,106 Log]，未清空日志，未新增烘焙错误/警告；原主场景干净、未进入 PlayMode，无临时烘焙 World 遗留。原 Scene/Prefab/Animator/资源/旧 meta 保持。

主线程静态验收通过；用户明确反馈“我已验收通过，接下来下个阶段”，主线程结合既有静态核对与用户反馈判定第八阶段通过，范围限本节八项人工清单及 schemaVersion=5/configRevision=7。人工结论来自用户反馈，未实际触发的精确边界、同 tick、延迟/预测回放与创建/清理/回滚失败仍为 UNKNOWN。入口为 CombatPrototypeNetCode，沿原启动配置使用 SinglePlayer 或 Online，所有端须使用同一代码、Ghost、输入布局及配置。以下八项为已获用户确认的本阶段人工范围，静态烘焙不替代运行验收：

1. PlayMode 前临时将 objects 中 tree_normal.regrowSeconds 改为 5，regrowEnabled=true，等待正常导入/烘焙。分别在 Forest/Grassland 成功 H 后检查从砍倒提交起模拟计时，原点空闲时原实体/位置/Yaw/PlacementIndex 恢复 Standing/显示及阻挡，至少完成两轮 H → Felled → Standing。再生本身库存/地面木材/金币/经验不增加，新 H 完成才再次产出一份 wood ×3；持续按住 H、重复 H、已砍树不自动连砍。
2. 预约期间移动/攻击/受击/死亡/超距/断线或 F 优先取消时无木材和再生期限，原树保持 Standing/阻挡；恢复后须新 H。成功砍倒后 G 是否拾取、木材是否到期均不改变树木期限。可控条件下核对木材创建/提交失败无有效半成品、不安排再生，记录原异常；不能触发的创建/清理/回滚失败仍为 UNKNOWN。
3. 分别让存活玩家/敌人在砍倒的原阻挡圆内等待到期，核对树不恢复、不推开角色；原期限保留，移出后才恢复，默认中心距离阈值为玩家 0.91/敌人 0.96 米（含边界）。存活网络玩家不以 Simulate 为占位条件，死亡角色不占位。恢复后玩家/敌人按原扫掠/滑动被树阻挡，相邻树保持独立；精确边界、恢复失败及回滚日志未触发时保持 UNKNOWN。
4. 双客户端至少两轮核对砍倒/再生显示与阻挡一致，分别在 Felled 和再次 Standing 时晚加入、固定 ID 重连。检查跨端历史能覆盖先砍倒/再生/再砍倒，预测回放到转换之前及两次转换之间重建对应阻挡；转换在该 tick 移动后提交，从下一模拟 tick 生效。记录实际覆盖的延迟/纠正/回放条件，未触发的时序用例保持 UNKNOWN，不用显示一致替代阻挡验证。
5. PlayMode 前设 tree_normal.regrowEnabled=false，成功砍倒后本局保持 Felled、不自动产出；恢复 true 后以新局复测。treeHarvest.enabled=false 时使用原静态树/阻挡，不产生树木 Ghost/砍伐木材；drops.enabled=false 只禁敌人额外掉落，H 及木材的 G/到期链保持。原 gather_apple 再生开关与期限独立。
6. 临时调整树木 regrowSeconds、开关及 configRevision，核对重新导入/烘焙后生效，JSON 来源失败不回退 BuiltIn。非有限/负间隔、启用时为 0、缺失或错误类型字段须明确报错，关闭时 0 仍合法；schemaVersion 保持 5，无新增字段或旧配置自动补齐，旧 v4 仍拒绝。修复后重新成功烘焙；不同端使用相同配置，本阶段不提供联网配置校验协议。
7. 在等待再生、被占位、多轮恢复后分别停止重进；切换 Forest/Grassland、Json/BuiltIn，释放 SubScene/World，核对树木及历史随原实例释放、无重复树/掉落或清理错误。新 Server World 从原布局 Standing/空历史/零期限开始；已入包木材按原 v1 恢复，树木期限/历史和未拾取掉落不写盘。共享资源保持。
8. 回归 H/F 中断与争抢、G 苹果/木材分别入包和保存失败保留、新 G 重试/固定 ID 恢复、drops 到期及清理；F 采集物至少两轮原点再生（可沿第五阶段方法临时 5 秒）。回归地表/装饰、镜头、出生/复活点、玩家/敌人移动、近战/反击/受击/死亡/R、E 小块肉及原奖励保存。未实际触发的同 tick、保存失败和运行失败分支仍为 UNKNOWN。

验收结束恢复 SourceMode=Json、Preset=Forest、两份地图 schemaVersion=5/configRevision=7、seed=12345、32 敌人及原空间/movement/生态/出生参数；tree_normal 恢复 interactionDistanceMeters=2、footprintRadiusMeters=0.5、minimumSameTypeSpacingMeters=3、blocksMovement=true、regrowEnabled=true、regrowSeconds=600。treeHarvest 恢复 enabled=true、treeObjectId=tree_normal、visualResourceKey=tree_harvest、harvestDurationSeconds=2、dropItemId=wood、dropQuantity=3、dropVisualResourceKey=drop_wood；drops 恢复 enabled=true、vitality_apple ×1、drop_apple、距离 2、飞行 0.4、散落/弧高 0.6、贴地 0.05、缩放 0.5、寿命 600；gather_apple 恢复原 2 米/1 秒/苹果 ×1/关闭阻挡/启用再生 600 秒。

实现边界归[树木砍伐](MapTreeHarvest.md)，木材运动/拾取与库存归[掉落与拾取](MapDrops.md)/[背包与道具](Inventory.md)。第七阶段用户通过仍限 v5/revision=6 及原十项清单，其他历史通过与第二阶段独立 JSON UNKNOWN 保持。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查；第八阶段人工通过仅限本节八项清单及原版本；未实际触发的独立用例、历史内存与网络开销、规模性能、平台构建和线上联调仍为 UNKNOWN。


## 【KNOWN ISSUES】战斗地图第九阶段矿点采集与资源掉落验收

本节保留第九阶段 v6/revision=8、原 J 操作的验收口径；当前操作和跨类型选择见本页“地图资源统一 F 的人工验收”。

正常 Unity 编译当前无 C# Error，J 的 Mine 输入/命令类型、MineState 四字段 Serializer/Snapshot、服务端配置/进度标注及系统顺序已静态核对。Forest/Grassland 的 Json/BuiltIn 四次隔离 Editor 烘焙均与内置完整值一致，为 schemaVersion=6/configRevision=8；另两次 Json 关闭采矿时矿点为 0、原四类布局保持。默认森林/草原矿点 20/18、启用阻挡 109/71，树木 89/53、采集点 36/38 的位置与朝向保持，空间违规为 0；9 块/2304 格/96×96 米、32 敌人/8 列/间距 3/首点 (0,1,16)、NetworkId=1 玩家点 (2,1,0) 保持。六类地图交互/掉落 GhostType 互不重复，新网格 38 顶点/72 三角形、1.2 米高/0.75 米最大半径，非退化且朝外。烘焙前后 Console 均为 [0 Error,2 Warning,7 Log]，未清空日志、未新增烘焙错误/警告；主场景干净、无临时 World 遗留。旧资源/旧 meta 与主 Scene 保持，SubScene 仅追加 mine_rock/drop_stone 两个引用。

主线程代码/资源静态验收通过；第九阶段人工 GamePlayer 为 UNKNOWN，不能以编译/隔离烘焙替代。人工入口为 CombatPrototypeNetCode，沿既有启动配置进入 SinglePlayer 或 Online；所有端须同步代码、输入布局、Ghost、配置与资源。以下为本阶段人工清单，未能实际触发的独立用例保留 UNKNOWN：

1. 分别在 Forest/Grassland 核对新增 mine_rock 及初始阻挡、树木/采集点保持；本人 X/Z 2 米内单次 J 预约最近 Available 矿点，3 秒后仅生成一堆 stone ×3，矿点隐藏/解除对应阻挡。完成时库存、金币/经验不直接增加；石材沿原 0.4 秒飞行，空中 G 拒绝，落地 G 后按日志进入“石材”库存。
2. 核对范围外、死亡、非零 Move（含被障碍挡住）、Attack 请求、近战非 Ready 或无有效在线/归属/Simulate 时拒绝。预约中移动/攻击/受击/死亡/超距/断线均取消，不产石材、不耗尽/解除阻挡；恢复后须新 J。重复 J 不重置期限，持续按住不自动连续采矿，完成/取消当 tick 不再预约下一点，R 不补发死亡时 J。
3. 核对最近目标、精确同距取小 PlacementIndex；两玩家同 tick 争抢按 NetworkId 升序，仅一人预约同点，另一人可取其他合格点，已预约目标不被覆盖。不能触发的精确同距、临界 2 米和同 tick 竞争保持 UNKNOWN。
4. 分别覆盖 F/J、H/J 同帧请求、F/H 已在交互中按 J、J 预约中开始 F/H；核对 F/H 请求或活跃状态优先，J 被拒绝/取消，原 F/H 链保持。G/E/R 仍按原规则独立处理；同步按键不产生重复石材或双重预约。
5. 耗尽后本人/远端玩家和服务端敌人可通过该矿点原阻挡圆，相邻矿点/树木保持独立；双端隐藏/状态一致。晚加入、固定 ID 重连保留本局 Depleted；按原预测环境观察回放到 MinedTick 之前的阻挡恢复、从下一模拟 tick 解除，记录延迟/纠正条件。未覆盖的预测/时序分支仍为 UNKNOWN。
6. 已耗尽矿点的新 J 或攻击不重复产出，不自动再生；多点各完成一堆 ×3。并存敌人苹果、树木木材和石材时核对共享 DropId 唯一/不复用、G 最近规则及三个实际 ItemId 分别入包，同名累计，无采矿者专属；多人争抢同堆只提交一次，原金币/经验/小块肉归属保持。
7. 按既有方法制造玩家保存失败，核对 stone/DropId/阶段及原异常、石材库存不增、未到期掉落保持 Landed、旧正式档保留；恢复后须新 G，无自动补发，其他玩家/掉落/F/奖励继续。成功 G 后固定 ID 重连/服务端重启恢复已入包石材；未拾取掉落清空，矿点按原布局 Available 重建。
8. 可控条件下覆盖石材创建、矿点提交、清理/回滚失败，记录 Mine completion failed 及具体地图/布置/玩家/DropId/物品/资源/原异常；成功回滚时无有效半成品、矿点可重新预约且原阻挡保持，需新 J，其他点/玩家继续。清理/回滚错误必须单独可见；不能触发的运行失败保持 UNKNOWN，不用配置错误替代。
9. PlayMode 前修改 objects.mine_rock.interactionDistanceMeters、mining 时长/数量、生态 mineDensityPer100m2 与 map revision，等待正常导入/烘焙后核对。mining.enabled=false 时无矿点/采矿掉落、原四类布局恢复；drops.enabled=false 只禁敌人额外掉落，H/J 和已有掉落 G/到期仍沿原链；treeHarvest 开关与采矿独立。
10. 核对旧 v5、缺 mining/生态矿点字段、未知物品/资源/对象、非正时长/数量/交互/占地、负数或非有限密度、错误类型及矿点再生字段非 false/0 的明确错误，不补字段、不回退 BuiltIn；修复后重新成功烘焙。各端配置一致，本阶段没有新增联网一致性协议。
11. 沿原方法临时设 drops.lifetimeSeconds=3 或 0，核对石材生成起模拟计时到期/不自动到期，不自动入包。预约中、飞行中、落地等待、耗尽/取消后分别停止重进、切换 Forest/Grassland 与 Json/BuiltIn、释放 SubScene/World，核对矿点/石材无重复或销毁错误，共享资源保持。
12. 回归地表/草丛/碎石、镜头、出生/R 复活、玩家/敌人移动、近战/反击/受击/死亡、E 小块肉、原奖励/库存保存恢复、敌人苹果/G、H 木材及至少两轮树木/采集物再生（沿原方法临时 5 秒，结束恢复 600 秒）。J 没有工具要求、动画、石材使用效果或新 UI。

验收结束恢复 SourceMode=Json、Preset=Forest、两份地图 schemaVersion=6/configRevision=8、seed=12345、32 敌人及原空间/movement/出生；mining 恢复 enabled=true、mineObjectId/visualResourceKey=mine_rock、harvestDurationSeconds=3、dropItemId=stone、dropQuantity=3、dropVisualResourceKey=drop_stone，grassland/forest/rocky 的矿点密度恢复 0.1/0.2/1。mine_rock 恢复占地 0.75、同类间距 2.5、交互 2 米、只阻挡移动、非 gatherable、regrowEnabled=false/regrowSeconds=0。tree_normal/gather_apple 再生恢复 true/600 秒；treeHarvest 恢复 2 米/2 秒/wood ×3、drop_wood，drops 恢复 enabled=true、苹果 ×1、drop_apple、距离 2、飞行 0.4、散落/弧高 0.6、贴地 0.05、缩放 0.5、寿命 600，其余原值保持。

完整规则归[采矿](MapMining.md)，掉落/库存归[掉落](MapDrops.md)/[背包](Inventory.md)。第八阶段通过仍限 v5/revision=7 八项清单，第七阶段仍限 v5/revision=6 十项清单；其余既有通过及第二阶段独立 JSON UNKNOWN 保持。第九阶段人工 GamePlayer、同步写盘耗时、规模性能、平台构建与线上联调为 UNKNOWN。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】地图资源统一 F 的人工验收

本节保留统一 F 的 v6/revision=8 原版本验收口径，入口为 CombatPrototypeNetCode；矿点再生 v6/revision=9 清单见下节，当前 v7/revision=10 的 HUD 清单见后节。客户端 F 单次按下提交已有 Gather，H/J 不再触发或消费，G/E/R 保持；所有端须同步当前代码、输入布局、配置及资源。正常 Unity 编译及两个新脚本、三类 TryBegin/CancelBegin、保留输入字段和顺序特性已静态核对，未运行游戏系统或 PlayMode。用户已确认统一 F 人工 GamePlayer 验收通过，主线程结合既有静态核对与用户反馈判定该阶段通过，范围限下列八项清单。人工结论来自用户反馈，未实际触发的临界距离、精确同距、同 tick、独立失败及延迟/预测回放时序仍为 UNKNOWN：

1. 分别在采集点、树木、矿点旁单次 F：默认 2 米内开始，分别 1/2/3 秒完成；苹果 ×1 保存成功直接入包，木材/石材各 ×3 生成地面掉落并沿原 G 保存入包。H/J 单独按下无对应资源请求，原 G/E/R 和攻击保持。
2. 交互范围相交时跨三类选择 X/Z 最近有效目标，无类型优先；用 Interaction selected 的 type/placement/distance 核对。最近目标已占用/耗尽或功能关闭时排除，可选择另一有效目标；不同类型配置距离分别生效，范围外拒绝。精确同距取小 PlacementIndex，未触发边界用例保持 UNKNOWN。
3. 一名玩家只持有一种资源预约。操作期间重复 F 不重置、不预约第二个目标，其他更近资源变为可用也不切换；按住不自动连续。完成/取消当 tick 不启动另一交互，之后须重新按 F；无目标或启动失败不自动补发/转选。
4. 三类分别覆盖非零 Move（含被阻挡）、攻击、受击、死亡、超距、断线/归属失效的拒绝与中断：不提前产出或耗尽，树木/矿点阻挡保持；R 不补发死亡时 F。旧 F/H 类型优先不再参与，同一 F 不被树木/采矿系统误取消。
5. 两玩家同 tick 争抢同目标，按 NetworkId 升序立即预约，只有一个持有者，另一人可取其他有效目标；覆盖不同类型目标并存、已经预约玩家再次 F。未触发精确同 tick 竞争保持 UNKNOWN。
6. 回归采集 SavePrepared 失败不入包/耗尽、释放预约并须新 F；木材/石材创建或提交失败只清理当前项/回滚，记录原异常及清理失败，其他玩家继续；地上掉落的 G 保存失败保持未到期掉落及旧库存/正式档。未触发独立失败用例保持 UNKNOWN。
7. 回归采集物和树木至少两轮原点再生、占位等待及新 F 再次产出，取消/失败不安排再生；矿点仍不再生。核对跨端显示、下一 tick 阻挡、预测/晚加入/重连及重启后资源重建和固定 ID 库存恢复，未覆盖时序保持 UNKNOWN。
8. 回归 Forest/Grassland、Json/BuiltIn、treeHarvest/mining 独立开关、drops.enabled 仅控制敌人掉落，以及原镜头/出生/移动/近战/反击/奖励/库存/清理。修改距离/耗时/数量须在 PlayMode 前完成并各端一致，沿原严格 JSON 校验及烘焙生效；结束恢复默认 2 米、1/2/3 秒、苹果 ×1/木材 ×3/石材 ×3、再生 true/600 秒、矿点 false/0 和其余原配置。

统一 F 通过依据为本阶段用户反馈；旧阶段人工通过仍限各自原版本/清单，第九阶段原 J 人工结果仍为 UNKNOWN，未触发用例不由其他阶段替代。完整规则归[地图](Map.md)，计时/结算归[背包](Inventory.md)、[树木](MapTreeHarvest.md)、[采矿](MapMining.md)。AI 未新增/运行逻辑单元测试、GamePlayer/PlayMode、游戏模拟/显示系统、命令行构建、发布、性能采样或图片检查；运行性能、平台及线上结果仍为 UNKNOWN。

## 【KNOWN ISSUES】矿点原点再生的人工验收

本节保留矿点原点再生的 v6/revision=9 原版本验收口径，当前 v7/revision=10 的 HUD 清单见后节。入口为 CombatPrototypeNetCode；原 Json 与 BuiltIn 为 schemaVersion=6/configRevision=9，mine_rock 默认 regrowEnabled=true/regrowSeconds=600。正常 Unity 编译、历史 Ghost Serializer、仅服务端期限及系统顺序、八次隔离 Editor 烘焙已静态核对，主线程静态验收通过；未运行游戏模拟/显示系统或 PlayMode。用户已确认本阶段人工 GamePlayer 验收通过；主线程结合既有静态核对与用户反馈判定通过，范围限下列八项清单及 v6/revision=9。人工结论来自用户反馈，未实际触发的精确距离/特殊 Simulate、同 tick/延迟/预测回放、晚加入及独立失败分支仍为 UNKNOWN；既有统一 F/v6/revision=8 通过保持原口径：

1. PlayMode 前临时将 mine_rock.regrowSeconds=5，完成导入/烘焙且各端一致；2 米内新 F 仍 3 秒采矿，成功才生成 stone ×3 并开始 5 秒服务端模拟计时。矿点耗尽隐藏/解除阻挡，原点空闲后同一 Ghost、PlacementIndex、位置/朝向恢复 Available/显示/阻挡；暂停不按系统墙钟推进，恢复本身不发石材或保存。
2. 矿点耗尽后让存活玩家占原点至期限到达，X/Z 中心距离<=1.16 米时保持耗尽/隐藏/可通行；包含未启用 Simulate 的存活玩家，不推开、不换点、不重置期限。离开占位范围后恢复；死亡玩家不占位。精确边界或特殊 Simulate 条件未触发时保持 UNKNOWN。
3. 用存活敌人覆盖原点占位<=1.21 米、离开后恢复、死亡敌人不占位，以及多人/多敌人同时占位必须全部退出。原移动/攻击/反击/受击/死亡和树木占位链保持；不能触发的边界用例保持 UNKNOWN。
4. 至少两轮“新 F → stone ×3 → 原点再生”，核对每次只生成一堆新 DropId 的石材，重复 F 不重置/切换、按住不自动连续，再生或完成当 tick 不自动预约。再生不受石材已拾取、飞行/落地或到期影响；G 沿原 SavePrepared 成功才提交库存/Consumed，库存保存/恢复及苹果/木材回归保持。
5. 双端核对耗尽/恢复显示及转换后下一模拟 tick 的阻挡；至少两轮后按权威历史重建转换前/转换间状态，晚加入/重连接收当前状态与完整历史，无重复矿点。精确同 tick、延迟/预测回放时序不能实际触发时保持 UNKNOWN；所有端使用同版新 Ghost 布局与重新烘焙的数据。
6. 分别中断采矿（移动、攻击、受击、死亡、超距、断线/归属失效），保持原阻挡、零再生期限及既往历史，须新 F。可控条件下覆盖石材创建/提交和再生恢复失败：成功回滚无有效半成品或假期限，采矿回到 Available、再生仍为原 Depleted/到期时间，历史长度/障碍恢复；错误保留地图/布置/资源/阶段/原异常，清理/回滚失败另记，其他条目继续。未触发独立失败保持 UNKNOWN。
7. PlayMode 前覆盖 Forest/Grassland、Json/BuiltIn，关闭矿点再生后成功采矿保持耗尽；mining.enabled=false 无矿点并恢复原四类布局，drops.enabled 仅控制敌人额外掉落。修改间隔后严格 JSON 校验/烘焙生效，字段仍为原 regrowEnabled/regrowSeconds，无热重载；树木/采集物 true/600 的独立再生及统一 F 最近目标回归保持。
8. 耗尽计时中、占位等待、再生后、再次采矿/石材飞行或等待 G 时分别停止重进、切换模板/来源、释放 SubScene/World：矿点/历史/石材只由原所有权链清理，共享资源保持。新 Server World 从原布局 Available、空历史/零期限重建，固定 ID 库存仍按 v1 恢复；回归镜头/出生/R、移动/近战/反击/奖励/E、原保存和实例清理。

验收结束恢复 SourceMode=Json、Preset=Forest、两份地图 schemaVersion=6/configRevision=9、seed=12345、32 敌人及原空间/movement/出生；mine_rock 再生恢复 true/600 秒、占地 0.75/间距 2.5/交互 2 米、只阻挡移动/非 gatherable。mining 恢复 enabled=true、mine_rock、3 秒、stone ×3、drop_stone，生态矿点密度 0.1/0.2/1；树木/采集物 true/600 秒、treeHarvest 2 秒/wood ×3、drops enabled=true 与原距离/运动/寿命及其他配置保持。

规则归[采矿](MapMining.md)，F 入口归[地图](Map.md)，掉落/库存归[掉落](MapDrops.md)/[背包](Inventory.md)。用户已确认矿点再生人工 GamePlayer 通过，范围限本节八项清单及 v6/revision=9；未实际触发的边界/独立失败/预测时序/晚加入仍为 UNKNOWN，历史内存/网络开销、规模性能、平台构建和线上联调未验收。第九阶段原 J 和第二阶段独立 JSON 人工 UNKNOWN 保持；统一 F 与第七/第八及其他通过仍限各自原版本/清单。AI 未新增/运行逻辑单元测试、GamePlayer/PlayMode、游戏模拟/显示系统、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】资源交互提示与进度显示的人工验收

本节保留 HUD 已验收的 v7/revision=10 口径；当前工具 v8/revision=11 的新清单见后节。入口为 CombatPrototypeNetCode，原两份地图 Json/BuiltIn 为 schemaVersion=7/configRevision=10、interactionHud.enabled=true。Main Camera 已追加一个 HUD 组件，原三根对象和其他组件保持；Player Baker 新增所属玩家 HUD Ghost 数据，各端须使用同版代码、输入、配置和重新烘焙的数据。正常 Unity 编译、SendToOwner 与四字段 Serializer、系统顺序、十次隔离烘焙及显式挂载已静态核对，主线程静态验收通过。用户已确认本阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定通过，范围限本节八项清单及 v7/revision=10。人工结论来自用户反馈，未实际触发的独立用例仍为 UNKNOWN：

1. 在森林/草地靠近采集点、树木、矿点并静止，2 米内分别显示 F Gather Apple/F Chop Tree/F Mine Rock；无可用目标、移动、攻击或死亡时收起。跨三类重叠区域核对当前最近有效目标、较近目标被他人预约后改选、同距按原 PlacementIndex；提示不提前预约，真正开始由 F 服务端日志/状态确认。未触发的精确同距/临界距离保持 UNKNOWN。
2. 新 F 后三类分别显示当前目标、递增百分比和进度条，原耗时仍 1/2/3 秒。交互中另一类型更近不切换目标；重复/按住 F 不重置计时、切换或连续自动采集。完成收到权威状态后清掉进度，再显示下一有效提示或隐藏；采集仍保存后入包，砍树/采矿仍只生成一堆新 DropId 的木材/石材。
3. 覆盖移动、攻击、受击、死亡、超距、断线/归属失效中断，收到取消状态后进度清掉，重新开始须新 F。可控条件下覆盖采集保存失败和树木/矿点生成/提交失败：HUD 不显示额外成功/发奖，不自行完成或改变原资源/库存；独立失败和回滚若未实际触发仍为 UNKNOWN。
4. 两个客户端同时在不同目标交互，只显示各自本地所属玩家进度；争抢同一目标仅原赢家 Working，输家不展示他人进度，按原规则改选或隐藏。双端/晚加入/重连核对初始 Hidden 与新状态、取消/完成一致性；网络延迟/预测纠正时进度来自最新权威快照，不能用本地秒表提前完成。未覆盖时序保持 UNKNOWN。
5. 在 1920×1080 及其他窗口尺寸/长宽比核对底部居中、等比缩放、文字清晰、面板/进度布局和英文文案；HUD 不截获鼠标或 F/G/E/R、WASD、镜头操作。此项由人工观察，AI 未查看图片或调用 HUD 绘制进行验收；中文字体/字形覆盖仍未确认。
6. PlayMode 前修改 interactionHud 尺寸、底距、字号、条高及三类文案，正常导入/烘焙后生效；关闭 enabled 后 HUD 隐藏，原 F 三类行为及 G/存档保持。Json/BuiltIn、两种模板和关闭采矿/再生回归。缺段/未知或重复键/错误类型、尺寸不合法、空白/控制字符/超 61 UTF-8 字节文案、旧 v6 明确报错且不回退来源；无热重载。不能触发的运行错误保持 UNKNOWN。
7. 准入未完成、无本地 Ghost、玩家死亡/R、断线重连、停止重进、模板/来源切换及 World/SubScene/主场景释放时，不残留上一局提示/进度；没有客户端的 Server 启动保持隐藏。Main Camera 不增加第二个 HUD，原共享资源和实例释放保持，原 PlayerView/跟随镜头正常。
8. 回归至少两轮采集物/树木/矿点原点再生与占位等待、新 F 再产出，原移动/阻挡、玩家与敌人战斗/奖励、G 苹果/木材/石材、E 小块肉、R、v1 固定库存保存恢复和清理。未实际触发的保存/清理/预测失败、边界及规模性能保持 UNKNOWN。

验收结束恢复 SourceMode=Json、Preset=Forest、schemaVersion=7/configRevision=10、HUD enabled=true、320×76/底距48/字号20/条高10、英文 Gather Apple/Chop Tree/Mine Rock；原 seed=12345、32 敌人、空间/出生/movement、drops、treeHarvest、mining 及三类 true/600 秒再生保持。

规则与静态产物归[交互显示](MapInteractionHud.md)。用户已确认 HUD 人工 GamePlayer 通过，范围限本节八项清单及 v7/revision=10；未实际触发的精确距离/同距、同 tick、延迟/预测回放、晚加入和独立保存/创建/提交/清理/回滚失败仍为 UNKNOWN，中文字体/字形覆盖未确认。HUD 与历史内存/网络开销、规模性能、平台构建和线上联调未验收。矿点再生 v6/revision=9、统一 F v6/revision=8 及其他阶段用户通过保持原版本/清单；第九阶段原 J、第二阶段独立 JSON 旧 UNKNOWN 保持。AI 未新增/运行逻辑单元测试、GamePlayer/PlayMode、游戏模拟/显示系统、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】采集工具与耐久的人工验收

入口为 CombatPrototypeNetCode，当前两份地图 Json/BuiltIn 为 schemaVersion=8/configRevision=11、gatherTools.enabled=true，玩家存档写 v2/读取 v1 迁移。数字 1/2 分别制作 Axe/Pickaxe，沿原 F 自动使用；各端须同版代码、输入、配置及重新烘焙数据。本阶段正常编译、新 Serializer/所属同步、系统声明顺序和十次隔离 Editor 烘焙已静态核对，主线程静态验收通过；用户已确认本阶段人工 GamePlayer 通过，主线程结合既有静态核对与用户反馈判定通过，范围限本入口、v8/revision=11 和以下十二项清单；未实际触发的独立用例仍为 UNKNOWN：

1. 使用无档的新固定 ID 准入，工具为空；不用工具分别新 F 完成植物1秒、树木2秒、矿点3秒，仍是苹果×1直接保存入包和 wood/stone×3地面掉落，G/E/R及原最近目标规则保持。
2. 用原 F/G 获取材料后静止，数字1单次扣木材3/石材2并获得 Axe 60/60，数字2扣木材2/石材3并获得 Pickaxe 40/40。数量归零移除该材料项，其他库存/金币/经验保持；按住不连续制作。材料不足、仍有可用工具均拒绝，不扣材料或补满。
3. 进入树木/矿点新 F，分别锁定斧头1.5秒/镐子2.25秒，成功只产一堆新 DropId 的原数量资源，耐久恰扣1并保存；植物仍1秒且两类工具不扣耐久。普通攻击不扣工具，G 拾取保持原库存提交。
4. 分别用移动、攻击、受击、死亡、超距、断线/归属失效中断两类工具交互；取消、未赢预约、重复F均不扣耐久/发资源，须新F。交互中工具/目标/实际耗时不换；新近目标不抢占。不能触发的边界条件保持 UNKNOWN。
5. 用测试固定 ID 的有效档案或自然使用将工具耐久降至1，最后一次正常完成1→0，损坏记录保留；下一F徒手，数字1/2重新制作才扣完整材料并恢复对应满耐久。配置单次成本>1时，低于成本也不可用/允许重制，不能用不足成本完成。
6. 移动、攻击、死亡或任意三类预约期间制作均拒绝；同输入tick按F+1/2时只执行F、不扣制作材料，同时1+2只处理斧头。非法CommandTarget归属不修改真正所属玩家工具/反馈，R同tick不补办被拒绝制作。
7. 可控条件下使制作保存或工具完成保存失败，旧正式档、材料/耐久保持，完成失败只清理本次掉落并取消、不安排再生，恢复后须新按键。分别覆盖创建/准备/清理/回滚错误并核对其他条目继续、原异常/阶段被暴露；未触发独立失败保持 UNKNOWN。文件替换成功后的意外ECS故障不以旧档补偿，不宣称跨文件系统事务完整回滚。
8. 两个固定 ID、双端核对独立材料与工具，仅本地所属客户端接收工具/制作反馈；重连和服务端重启恢复各自耐久/损坏记录，死亡/R不补满。奖励、E、植物F、各物品G之后重新准入仍保存工具，验证全部旧保存候选不会覆盖Tools。
9. 在人工准备的原v1测试档核对金币/经验/库存原样、Tools空、读取不立即改盘；下一次正常保存写v2。v2缺Tools、未知/重复工具ID、超过两条、额外/重复字段、错误类型/负值/超过当前最大耐久均拒绝该玩家并保留旧档，其他玩家可准入；不批量重写已有正式档。
10. HUD第一行与进度保留，第二行显示工具耐久/Hands/损坏制作提示；Working进度按实际1.5/2.25秒计算。制作成功/各类拒绝/失败显示2秒，没有F目标也可反馈；新绑定不重播旧Sequence，死亡/R、无本地Ghost、断线、停止、地图/World/Scene释放无旧反馈残留。人工核对320×104底部面板和不同窗口尺寸，中文字体/字形保持 UNKNOWN。
11. PlayMode前覆盖两模板/Json与BuiltIn、工具关闭、HUD关闭；工具关闭拒绝制作、F徒手且档中工具保留，HUD关闭不改变玩法。修改合法耐久/成本/配方/倍率/显示秒数后正常烘焙生效；旧地图v7、缺段/未知或重复字段、非整数/非法数值、错目标类型、空白/控制字符/超61字节名称明确失败、不回退来源或热重载。未触发运行错误保持 UNKNOWN。
12. 回归至少两轮三类原点再生/占位等待与新F、原G飞行/到期/保存、移动/阻挡、战斗/奖励/E/R、镜头/出生及停止重进的实例清理；世界资源和期限仍不保存。徒手与工具完成保持原产出和600秒再生，不补工具，不改变既有资源布局/绑定。

验收结束恢复 SourceMode=Json、Preset=Forest、schemaVersion=8/configRevision=11；gatherTools=true、反馈2秒、Axe60/成本1/倍率0.75/木3石2、Pickaxe40/成本1/倍率0.75/木2石3；HUD enabled=true、320×104/底距48/字号20/条高10、原英文文案。原seed=12345、32敌人、空间/出生/movement、drops、treeHarvest2秒/wood×3、mining3秒/stone×3和三类true/600秒再生保持。

工具规则归[采集工具](MapGatherTools.md)，显示归[交互HUD](MapInteractionHud.md)，档案归[资源与数据](DataResources.md)。本阶段人工通过来自用户反馈；未实际触发的精确边界、同tick、延迟/预测回放、晚加入及独立配置/创建/准备/保存/提交/清理/回滚失败仍为UNKNOWN。既有HUD v7/revision=10、矿点再生v6/revision=9、统一F v6/revision=8及其他通过保持各自版本/清单，未触发用例不替代。新增同步保存耗时/性能、带宽、平台与线上联调未验收。AI未执行PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】材料背包与制作面板的人工验收

入口CombatPrototypeNetCode，面板验收时Forest/Grassland的Json/BuiltIn为schemaVersion=9/configRevision=12。inventoryPanel=true/initiallyOpen=false、380×640/右24/上64/字号18/行32；原HUD仍true/320×104，工具参数及玩家v2保存/v1读取迁移保持。正常编译/类型导入/十四次隔离Editor烘焙已静态核对，主线程静态验收通过；用户已确认本阶段人工GamePlayer通过，主线程结合既有静态核对与用户反馈判定通过，范围限本入口、v9/revision12及以下十二项清单，未实际触发的独立用例仍为UNKNOWN：

1. 默认准入后面板关闭，B单次打开/再按关闭，按住不反复；底部关闭按钮有效。没有本地Ghost/未准入/死亡时不显示，打开不暂停世界、攻击/敌人/再生计时，WASD、空格、F/G/E/R及Z/X保持。
2. 新固定ID空库存显示Empty，两工具Not owned；通过植物F、木材/石材G和敌人奖励看到Apple/Wood/Stone/Meat及实际数量，保持原条目顺序，同名累加、归零移除，数量超过99不拆格。原1/2在面板关闭时也可制作。
3. 面板列出斧头木3/石2、镐子木2/石3的现有/需要及各材料缺少数量；不足时按钮禁用，足够且未持有/损坏时可点击。一次按钮操作沿原服务端链扣完整材料并生成满耐久工具，其余库存/金币/经验保持，成功反馈按原2秒且耐久区更新；UI不提前发工具或扣料。
4. 两工具仍可用时按钮禁用，不允许补满；F成功扣耐久后列表更新，取消不扣。最后一次到0显示损坏，之后按钮/原1或2可重新制作，缺少材料仍禁用，重制扣完整配方并补对应满耐久，另一槽保持。
5. 点击配方、关闭按钮、面板空白或滚动区时不触发左键攻击；面板外左键仍攻击，空格沿原规则。滚轮在面板内只滚动列表、面板外仍缩放镜头，Z/X和相机相对WASD保持；B关闭与面板内左键同帧无穿透。不同窗口尺寸、缩放及边界命中由人工核对，未触发精确边界保持UNKNOWN。
6. 移动/攻击/受击/任意资源忙碌期间点击可用按钮时服务器继续按原资格接受或拒绝并反馈；F与制作同输入tick仍F优先，键1/2与按钮合并、两类同时仍斧头优先，不部分扣料或重复赠工具。客户端快照滞后不绕过服务端资格，同tick/预测回放未触发保持UNKNOWN。
7. 可控条件下制作保存失败，按钮收到原失败反馈，旧材料/耐久/正式档保持，不自动重试；恢复后新按钮或数字键才重试。核对原G、植物F、奖励和E候选继续保留Tools，不能触发的独立保存/准备/提交/清理失败保持UNKNOWN，不宣称文件替换后意外ECS故障可完整回滚。
8. 双客户端/两个固定ID各面板只显示本地所属库存、工具、配方与反馈，任一端制作不会显示为另一玩家制作成功；断线/重连和服务端重启恢复原材料/工具且默认关闭面板。晚加入、非法归属、延迟及同帧绑定变化未触发保持UNKNOWN。
9. 面板打开或按钮尚未交给输入系统时关闭/死亡/断线/停止/地图或玩家更换，未提交请求与鼠标按下标记清掉，不由下一玩家或World继承。R/重连不补满工具，重新绑定不重播旧Sequence；已经提交到原输入命令的制作继续以原服务器结果为准。
10. PlayMode前分别关闭inventoryPanel、interactionHud、两者及gatherTools并正常烘焙：面板关闭保留原F HUD及1/2制作，F HUD关闭保留B/列表/按钮/反馈，两者关闭仍可1/2，工具关闭保留库存/已有耐久并禁用面板按钮，F徒手。两模板/Json/BuiltIn均核对，不运行热切配置。
11. 合法修改initiallyOpen、尺寸/边距/字号/行高/文案后正常导入/烘焙生效；可控条件下旧v8、缺段/字段、未知/重复字段、错类型、非有限/非法几何、空白/控制字符/超61UTF-8字节文案明确失败、不补默认或回退来源。确认字体之外的显示及滚动；中文字体/字形仍UNKNOWN。未触发配置/异常网络条目保持UNKNOWN。
12. 回归原采集/砍伐/采矿计时与取消、工具损坏/重制、掉落飞行/G/到期、原点再生/占位等待、移动阻挡/战斗/E/R/镜头，以及停止重进的实例释放；原产出、存档v2/v1边界和地图布局保持，世界资源/期限仍不保存，面板没有库存容量/99拆格或额外物品使用效果。

验收结束恢复SourceMode=Json/Preset=Forest/schema9/revision12、inventoryPanel=true/initiallyOpen=false及本节默认尺寸/英文文案；原seed12345、32敌人、HUD、工具、资源产出和600秒再生保持。新面板规则归[材料背包与制作面板](MapInventoryPanel.md)，原制作/耐久归[采集工具](MapGatherTools.md)。本阶段人工通过来自用户反馈；未实际触发的精确排版/命中、事件顺序、同tick、延迟/预测回放、晚加入及独立配置/网络条目/创建/准备/保存/提交/清理失败仍为UNKNOWN。工具v8/revision11、HUD v7/revision10及其他用户通过仍限旧版本/清单。字体、运行性能/带宽、平台与线上未验证，AI未执行PlayMode、游戏模拟/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 【KNOWN ISSUES】背包物品丢弃与地面掉落的人工验收

入口CombatPrototypeNetCode，丢弃验收时Forest/Grassland的Json/BuiltIn为schemaVersion=10/configRevision=13，inventoryDrop=true、singleDropQuantity=1、allowDropAll=true、feedbackSeconds=2；苹果/木材/石材各用原资源键。正常Unity编译、新输入/所属反馈序列化及16次隔离Editor烘焙静态通过，未改原地图布置/结构/资源绑定。用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限本入口、schemaVersion=10/configRevision=13及以下十二项清单；未实际触发的独立用例仍为UNKNOWN：

1. B打开面板，苹果/木材/石材正数量行显示Drop x1和All，小块肉及未配置库存显示Unavailable；工具两槽无丢弃按钮。默认空库存和材料不足单次数量时无非法操作，丢弃开关关闭保留原列表/制作/玩法。
2. 静止存活且近战Ready，分别单次丢弃三种物品：原库存各扣1，其余条目顺序/金币/经验/Tools保持，一份对应Ghost从玩家位置飞行、落地；客户端不提前扣包。库存归零移除条目，面板随后显示服务器结果。
3. All使用服务端执行时的实际总量；可控准备超过99数量，单份Ghost承载全部数量、不拆99、不地面合并；G拾取全量后按原同名checked累加。快照滞后和连续主动点击仍以服务器当前量/资格判定，不产生负数或补发。
4. Prepared不可见、不运动、不可G；激活后飞行期不可领，落地才可在原2米内G，保存成功才入包/Consumed。其他合格玩家也可拾取，两玩家争抢沿原一次提交/同距DropId规则；跨端/晚加入及预测回放未触发保持UNKNOWN。
5. 移动、Attack请求/近战非Ready、死亡、无所属Connected/InGame连接或任意植物/树木/矿点预约期间丢弃拒绝，不扣库存或留下可拾取物。本人收到Drop rejected，非法归属不覆盖真实玩家反馈；不能触发的分支保持UNKNOWN。
6. 同输入tick丢弃与F/G/E、制作或R均保留原操作，丢弃拒绝；不干扰原F最近目标、制作F优先/斧头优先、E小块肉及G/R资格。无自动重试，重新主动点击仍重新核定；精确同tick/事件顺序未触发保持UNKNOWN。
7. 可控准备/Prefab创建/保存失败时旧库存与正式档保持，只有本次Prepared释放；清理失败须保留日志且残留不可见/不可领。恢复后重新点击，其他玩家请求继续；独立失败无法触发保持UNKNOWN，不宣称保存成功后意外ECS故障可完整回滚。
8. 成功显示Dropped及对应物品/实际量，拒绝/失败分别显示配置文案，默认2秒，面板页脚优先于原制作提示，F进度和工具反馈不改。仅本人显示丢弃反馈，新绑定不重播旧Sequence；双客户端独立反馈/晚加入未触发保持UNKNOWN。
9. 面板内丢弃按钮/空白/滚轮不触发鼠标Attack或缩放镜头，面板外原鼠标规则保持；关闭、死亡、断线、源/玩家变化及World/Scene释放清掉未提交请求。已交给输入的请求仍由服务器核定，不由下一玩家或World继承待提交状态；缩放/精确命中未触发保持UNKNOWN。
10. PlayMode前正常导入/烘焙核对两模板/Json与BuiltIn，分别关闭inventoryDrop、All、面板、F HUD、敌人掉落、工具/砍树/采矿；各开关独立，已有地面物仍可原G。Single改2、合法物品列表缩减/文案/反馈秒数变化生效；关闭面板不再提交按钮，原1/2保持。
11. 可控旧v9、缺inventoryDrop/字段、未知/重复字段、错误类型、非正单次数量/非有限反馈、空/超3条/未知或重复itemId、非法或未绑定资源键、空白/控制字符/超61字节文案均明确失败，不补默认或回退来源。配置关闭仍校验全部字段；未触发独立异常保持UNKNOWN。
12. 丢弃后重连恢复已扣库存，未拾取地面物仍只属于本局；服务器重启清掉未拾取物，已保存扣减保持，G成功后物品随拾取者库存恢复。回归原三类F/工具耐久/制作、飞行/G/到期、再生/占位、战斗奖励/E/R/镜头/阻挡及停止释放；原空间/资源/产出/600秒再生保持。

丢弃清单对应SourceMode=Json/Preset=Forest/schema10/revision13、inventoryDrop=true/Single1/Alltrue/反馈2秒、三种默认映射和六个英文文案，原面板/工具/HUD/drops/空间/种子/出生及600秒再生默认保持。规则归[背包丢弃](MapInventoryDrop.md)，运动/拾取归[掉落](MapDrops.md)。本阶段人工通过来自用户反馈；地面物重启消失且不返还已保存库存保持当前边界，未触发的独立保存/创建/提交/清理、预测/网络/反馈/命中仍UNKNOWN。原面板v9/12十二项、工具v8/11十二项、HUD v7/10八项及其他用户通过保持各自原版本/清单，不覆盖新增丢弃。AI未执行GamePlayer/PlayMode、游戏模拟/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查；字体、运行性能/带宽、平台/线上及文件替换后意外ECS恢复未验证。

## 【KNOWN ISSUES】掉落物拾取提示与目标显示的人工验收

入口CombatPrototypeNetCode，G提示验收时Forest/Grassland的Json/BuiltIn为schemaVersion=11/configRevision=14，pickupHud=true/400×52/底168/字号20，四文案Pick up、Vitality Apple、Wood、Stone。原F HUD和背包面板默认开启，G距离仍为drops的2米。正常Unity编译、新所属四字段Serializer/SendToOwner/原13输入、十次隔离Editor烘焙静态通过；原地图布置/资源引用及F初值保持。用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限本入口、schemaVersion=11/configRevision=14及以下十项清单；未实际触发的独立用例仍UNKNOWN：

1. 按原敌人掉落、砍树、采矿与背包丢弃分别取得苹果/木材/石材，落地且具备资格时显示G、正确物品文案及目标实际数量；All生成的一份Ghost显示其实际总量，不显示固定1。默认G面板在F面板上方16像素，两者无覆盖；无目标时G隐藏。
2. 多份已落地物同时进入2米范围，仅显示X/Z最近一个；改变站位并停下后按最近目标切换，新G沿实际处理tick选择。精确同距取较小DropId、恰好边界距离及不可精确构造用例保持UNKNOWN，不凭目测推定通过。
3. Prepared、Airborne、Consumed、已到期和超范围物体没有有效G提示；飞行落地后可显示，成功消耗或到期后随权威快照隐藏或切换下一目标。关闭自动到期的原lifetime=0仍按原规则，未触发的准备态/期限独立用例保持UNKNOWN。
4. 移动输入（含被障碍挡住）、Attack请求或近战非Ready时隐藏；重新静止且Ready后恢复。死亡、无准入连接、断线及本地玩家失效不保留旧目标，R复活后重新采样；有限输入、特殊Simulate/归属失效等未触发用例保持UNKNOWN。
5. F Ready/Working与G目标同时存在时分别显示，F工具行/真实进度保持，G不创建资源预约或中断F。B背包的列表/滚动/制作/Drop与鼠标隔离保持，G面板没有点击按钮或输入事件拦截；同tick F/G及其他原操作规则不因提示改变。
6. 新G成功后仍先保存，再入包/Consumed，提示随快照隐藏或显示下一目标；不由提示预扣/预发。可控保存失败时原库存和未到期地面物保持、提示仍可显示、须新G重试；提示不是成功标志，实际未触发保存/提交失败保持UNKNOWN。
7. 多玩家各自只显示本人权威目标，远端玩家状态不覆盖本地；争抢沿原NetworkId顺序只入包一次。晚加入/重连不重播旧目标；延迟下可短暂显示过期快照，按G时仍由当前服务端选择，不锁定旧提示目标。未触发的同tick/预测回放/晚加入/争抢用例保持UNKNOWN。
8. PlayMode前分别关闭pickupHud、关闭interactionHud与inventoryPanel但保留pickupHud、三者全关并重新导入/烘焙：关闭G显示仍可G拾取，关闭其余两显示仍可独立G提示，三者全关仅收起显示。drops.enabled=false只关闭敌人新掉落，已有/树木/矿点/背包掉落的G显示与拾取保持。
9. PlayMode前调整尺寸/底距/字号和四文案、正常导入/烘焙后核对等比显示。旧v10/缺段/缺字段/未知或重复字段/错类型、非有限或非法几何、字号非正、F间隔不足16、空白/控制字符/超61UTF-8字节标签明确失败，不补默认或回退。关闭显示仍校验全部字段；中文/乘号字形与未触发的独立异常保持UNKNOWN。
10. 停止/释放和再次进入后不显示上一局目标，原宿主/绑定保留、没有新场景UI或世界标记。回归原G保存/数量/飞行/到期/释放，F采集/砍树/采矿及600秒再生、工具耐久/制作、背包Drop/All、战斗奖励/E/R/镜头/阻挡；原地面物本局不存档、重启清空且已保存扣减不返还边界保持。

本清单对应SourceMode=Json/Preset=Forest/schema11/revision14、pickupHud=true/400×52/底168/字号20及四个默认文案，原F HUD/面板/工具/丢弃/drops/空间/资源/600秒再生值保持。规则归[拾取提示](MapPickupHud.md)，实际结算归[掉落](MapDrops.md)。原147项验收编号/内容和各旧版本用户通过保持，本阶段不扩展旧通过。AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查；本阶段人工通过来自用户反馈，限上述版本与清单；未实际触发的独立配置/网络/保存/预测/字形与布局用例，以及性能/带宽/平台/线上仍UNKNOWN。

## 【KNOWN ISSUES】资源交互目标高亮的人工验收

入口CombatPrototypeNetCode，本段高亮验收版本为schemaVersion=12/configRevision=15，Forest/Grassland的Json/BuiltIn中interactionHighlight主/F/G开启、采集/树/矿/掉落半径0.65/0.9/0.9/0.45米、线宽3/48段、Ready黄#FFD166/Working绿#6ED88A/G蓝#6EC6FF、opacity=0.9、heightOffset=0.03。原F/G文字与背包面板默认开启。正常Unity编译及14次隔离Editor烘焙静态通过，14个新值/RGB、原输入13/F四/G四、F/G初值、原Prefab及全部布局保持；主线程静态验收通过。用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定阶段通过，范围限本入口、schemaVersion=12/configRevision=15及以下十项清单；未实际触发的独立用例仍UNKNOWN：

1. 默认Forest进入，静止接近可采集植物/树木/矿点：F文字和黄色圆环对应同一最近权威目标，三类半径分别0.65/0.9/0.9米，远端玩家目标不显示；无目标或资格不符时隐藏。
2. 开始F采集/砍树/采矿：Working圆环变绿色并跟随原锁定目标，附近更近物体不使其切换；原百分比、1/2/3秒徒手和1.5/2.25秒工具耗时保持，没有客户端提前完成。
3. 移动/攻击/受击/死亡/超距取消，以及正常完成/耗尽：收到当前权威状态后绿色圈清除，可恢复新有效黄圈或隐藏；资源和所属快照未对齐时允许暂不画，不残留旧圈。
4. 接近Landed苹果/木材/石材：G文字和蓝色圆环对应同一DropId，半径0.45米、实际数量仍正确；Prepared/Airborne/Consumed不画圈，拾取/到期或目标被他人拿走后随权威状态隐藏/切换。
5. F/G目标同时有效时最多各一圈，蓝圈先画、黄/绿圈后画；B面板、G/F文字盖在圆环之后。按F/G仍按原处理tick选择，不新增目标锁定、预约或自动拾取。
6. 旋转/缩放原Main Camera并改变Game视口尺寸：圆环跟随对应资源客户端位置，半径仍为世界米，线宽按1920×1080比例缩放，画面外和近/远裁剪边界无异常；接受圆环作为无真实深度遮挡的屏幕叠加。
7. PlayMode前分别修改三色、透明度、偏移、四半径、线宽和段数并正常导入/烘焙：值按合法配置生效，不能改变资源占地、阻挡、交互距离或工作耗时；结束恢复本段默认值。
8. PlayMode前分别关闭高亮主开关/F通道/G通道、关闭对应文字但保留高亮、三文字全关而高亮开启、所有显示全关并重新导入/烘焙：文字与圈独立，关闭文字仍有对应圈，全部显示关闭仅收起显示，原F/G及工具玩法保持。
9. 两个Client仅显示本人圈；死亡/R、断线/重连、退出/重进、玩家或地图源变化后不残留上一局实体/位置/圈。Ghost晚到或快照分批到达先隐藏再按同身份恢复；不能触发的多玩家/时序分支保持UNKNOWN。
10. 正常F采集入包、砍树/采矿掉落与600秒再生、G保存入包、工具制作/耗耐久、B/Drop/All以及原E/R/攻击继续；非法/缺失/未知/重复字段、旧v1～v11、非法颜色和边界数值在配置阶段明确失败且不补段/回退。不能触发的保存/解析/绘制独立失败保持UNKNOWN。

本清单默认SourceMode=Json/Preset=Forest/schema12/revision15，interactionHighlight14值与本段一致；原seed/空间/出生/32敌人、F/G HUD、面板/工具/丢弃/drops/树矿/600秒再生保持。规则归[目标高亮](MapInteractionHighlight.md)，实际F/G行为归原资源/掉落模块。原157项验收编号/内容和各旧版本用户通过保持，新高亮不扩展旧通过。AI未运行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查。本阶段人工结论来自用户反馈，限上述版本与清单；未实际触发的独立投影/缩放/身份时序/网络/生命周期及配置/解析/绘制/保存故障，以及性能/带宽/平台/线上仍UNKNOWN。

## 【KNOWN ISSUES】资源状态与再生提示的人工验收

入口CombatPrototypeNetCode，本段资源状态验收版本为Forest/Grassland的Json/BuiltIn schemaVersion=13/configRevision=16，resourceStatusHud=true/400×52/底236/字号20，六文案Available、Working、In use、Regrows in、Waiting to regrow、No regrowth。原F/G/B与高亮默认开启、三类原点再生600秒。正常编译/所属Serializer与14次隔离Editor烘焙静态通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定阶段通过，范围限本段版本与以下十项清单，人工结论来自用户反馈：

1. 默认Forest进入，静止接近可用植物/树木/矿点：G上方出现对应原名称与Available；与F Ready身份一致，没有有效附近资源时隐藏，远端玩家状态不显示。
2. 本人F采集/砍树/采矿时显示Working，跟随原F锁定身份；附近更近资源不使其切换。原百分比、工具耗时/耐久和黄/绿圆环保持，不新增客户端工作计时或提前发奖。
3. 另一玩家使用附近资源且本人没有F Ready/Working目标时显示In use；本人不能由提示获得预约权。相应资源恢复可用后随权威状态切换；单Client无法触发此分支时保持UNKNOWN。
4. 耗尽植物后，在原2米交互范围、没有其他F目标时显示Regrows in Ns，按服务端期限向上取整变化；原点位置/身份保持，倒计时不使用本地时间补算、不写库存或保存。
5. 砍倒树木/耗尽矿点后在原点附近核对再生剩余秒数；到期前不显示Available。需要缩短等待时可在PlayMode前临时配置三类regrowSeconds=2、正常导入/烘焙，验收后恢复600；这只改变原间隔接口。
6. 树/矿到期时原点被存活玩家/敌人占用，状态显示Waiting to regrow，仍保持原隐藏/解除阻挡状态；离开占位区但仍在交互距离内，实际恢复后才显示Available。植物沿原再生逻辑，不新增占位条件。
7. 关闭三类regrowEnabled后重新烘焙，耗尽且没有其他F目标时显示No regrowth，期限不补默认。关闭砍伐/采矿不选择对应资源。移动/攻击时仍可读状态，F/G资格和实际取消/结算规则保持；死亡隐藏全部显示。
8. 状态关闭时原F/G/B/高亮照常；关闭其他文字及高亮、仅状态开启时仍显示并保留原F身份优先；全部显示关闭才收起绑定。状态不显示G掉落TTL，不新增圆环、点击或按键消费。
9. 死亡/R、断线/重连、退出/重进、玩家或地图源及World/Scene变化清旧状态/秒数。两个Client只显示本人所属快照，晚加入读取当前权威秒数；未触发的独立时序/预测/网络分支保持UNKNOWN。
10. PlayMode前修改新面板尺寸/底距/字号/六文案并正常导入烘焙，合法值生效；旧v1～v12、缺失/未知/重复字段、错类型、非有限尺寸、非正字号、文字容纳或G间隔不足、空白/控制字符/超61 UTF-8字节标签明确失败，不补段/回退，disabled仍校验。回归原F/G/掉落/600秒再生、工具/制作/Drop/All及E/R/战斗/镜头/阻挡/保存；未触发的独立失败、字形与性能保持UNKNOWN。

本清单限SourceMode=Json/Preset=Forest/schema13/revision16及本段11默认值；原空间/seed/32敌人/出生、F/G/工具/面板/丢弃/高亮/产出与存储接口保持。规则归[资源状态](MapResourceStatusHud.md)，原再生/采集/掉落结算仍归原专题。全部177项验收编号/内容及旧用户通过保持各自版本/清单。AI未运行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查；未实际触发的独立倒计时/等待/距离/身份/输入时序、配置/快照/绘制/保存失败、多玩家/生命周期、字形、性能/带宽/平台/线上及旧保存成功后意外ECS恢复仍UNKNOWN。

## 【KNOWN ISSUES】采集工具修理与耐久恢复的人工验收

入口CombatPrototypeNetCode，本段修理验收版本为schemaVersion=14/configRevision=17，默认SourceMode=Json/Preset=Forest。gatherTools.repairEnabled=true、repairFeedbackSeconds=2，斧头/镐子恢复20/15，每次木1石1；原最大耐久60/40、制作配方/单次成本/倍率保持，面板新增Repair/Repair/Full durability三文案。正常Unity编译、所属Serializer与18次隔离Editor烘焙静态通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定阶段通过，范围限下列十二项；未实际触发的独立分支仍UNKNOWN：

1. 默认进入，用B查看制作区后的修理区：两工具名称/状态、当前→恢复后和实际增量、材料现有/需要、缺口与3/4按钮可滚动访问。未持有不显示负耐久，原380×640/字号18/行32及Drop/All/制作区保持。
2. 制作斧头并用F消耗耐久，有材料时按3；当前40时恢复60并扣木1石1。工具/材料快照、B与F第二行反馈更新，不生成地面物或增加产出/交互距离；保存成功日志包含身份/工具/新耐久。
3. 制作镐子并消耗耐久，有材料时按4；当前25时恢复40并扣木1石1。工具修理独立于植物、树木和矿点状态，不改变原资源/再生/掉落链。
4. 斧头59→60或镐子39→40仍扣完整配方，不超过最大值。满耐久时按钮不可用、键盘请求明确拒绝且不扣料；按住3/4不连续修理，没有自动重试。
5. 已持有耐久0的损坏工具允许修理，默认恢复20/15，之后F可沿原工具倍率使用；未持有时不创建新槽、不扣材料。可用工具修理与原1/2“仍可用拒绝、损坏可重新制作”分别核对；验收使用合法原Tools状态，不在运行中篡改ECS。
6. 任一材料不足时不部分扣料或恢复耐久；刚好木1石1时两材料归零按原规则移除，其他库存/金币/经验及另一工具保持。接近满耐久也不得减免成本；客户端预览不预扣或写盘。
7. 移动、攻击/非Ready近战、死亡和任意活动采集预约期间修理拒绝；存活静止且空闲时才允许。无所属连接/CommandTarget不符/Simulate失效不结算，非法归属不覆盖真正所属反馈；未实际触发的独立资格分支保持UNKNOWN。
8. 同tick有F/G/E/R或1/2请求时修理拒绝，原业务继续按自身资格处理；3/4同时只处理斧头；修理与Drop同时则Drop拒绝，即使修理也被拒绝，不部分处理或转为丢弃。未触发的同tick/延迟/预测分支保持UNKNOWN。
9. B按钮与3/4合并到原输入链，每次点击消费一次，只有当前有效绑定/可见面板内鼠标按下可提交。面板内不触发攻击或镜头滚轮缩放，外部鼠标行为保持；材料快照非法时禁用按钮，滞后预览由服务器重新判断。
10. PlayMode前分别关闭repairEnabled、工具总开关、面板、F文字及全部显示并正常导入/烘焙：前两项拒绝修理且保留原耐久，关闭面板仍可3/4，关闭F文字但B开启仍有结果，全部显示关闭只关闭显示。反馈默认2秒，初次绑定/晚加入不重播旧结果；页脚未到期丢弃优先于修理、修理优先于制作。
11. 可控保存失败时材料/耐久及原正式档保持，反馈失败，重新主动请求才重试。正常修理后同固定ID重连/重启恢复原v2材料和Tools，死亡/R不补满；两个Client只收到本人反馈、各自库存/工具独立。关闭面板、死亡/断线、源/玩家/World/Scene变化清未提交按钮/旧反馈。未实际触发的保存/提交、恢复、多人/晚加入和生命周期分支保持UNKNOWN。
12. PlayMode前修改新恢复量、材料成本、反馈秒数及三文案并正常导入/烘焙，合法配置生效；恢复量须正整数且<=最大值，成本非负且总数>0，反馈有限正数，文案非空白/无控制字符/≤61 UTF-8字节。旧v1～v13、缺失/未知/重复字段、错类型及非法值明确失败，无补默认/回退，关闭仍校验；各端同版重新烘焙。回归原F/G、工具完成扣耐久、制作/Drop/All、600秒再生、高亮/资源状态、E/R/战斗/镜头/阻挡/保存；未触发的配置故障、字形/排版及性能保持UNKNOWN。

本清单限本段v14/17与默认新字段，原空间/seed/32敌人/出生、工具槽、F/G与资源状态各四字段、原制作/丢弃反馈和写v2/读v1迁移保持；修理规则归[工具修理](MapToolRepair.md)。原177项验收编号/内容及旧用户通过保持原版本/清单；本阶段人工通过结论来自用户反馈，仅覆盖本段十二项。AI未运行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git；未实际触发的独立失败/时序/网络/生命周期、未覆盖字形与性能/带宽/平台/线上及文件替换后意外ECS恢复仍UNKNOWN。

## 【KNOWN ISSUES】地图资源状态存档的人工验收

入口CombatPrototypeNetCode，当前schemaVersion=15/configRevision=18，默认Json/Forest、resourcePersistence=true/default_world/10秒，世界资源格式v1。正常Unity编译、字段反射和18次隔离Editor烘焙静态通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限本入口、v15/18、世界v1及以下十二项清单；结论来自用户反馈，未实际触发的独立用例仍UNKNOWN。清单使用单一服务端写同一槽；测试改变资源布局/开关/再生规则时，使用新合法槽或匹配的原资源档，避免把签名拒绝误判成加载失败。

1. 首次无正式资源档进入，服务端生成原89树/36采集/20矿及109阻挡，保存空Resources的v1档后Ready再准入；首次写失败拒绝准入，不创建玩家。客户端不读写世界文件。Forest/Grassland同槽分文件，原15输入/F/G/B及玩家加载保持。
2. 完成植物采集、砍树和采矿，文件包含对应gather/tree/mine、PlacementIndex/ObjectId和剩余秒数；只在成功资源结果后保存。植物仍先玩家保存再入包，树/矿原工具保存后完成并只生成地面物，G仍独立入包。可用/工作中资源不存条目，世界恢复不额外发物品。
3. 有耗尽资源时观察10秒检查点和正常关闭日志，关闭期间等待后重启；从最近成功保存的剩余秒数继续，不扣离线时间。正常关闭使用最后完整采样；异常结束仅恢复最后成功检查点，不能要求尚未成功写盘的时间精度。
4. 三类到期后沿原再生处理；树/矿到期原点被存活玩家/敌人占用时仍等待，移开后恢复原实例/显示/阻挡。保存到期0秒并重启仍走原占位检查。客户端预测/晚加入读取本次基态及后续转换，不使用旧World绝对tick/全部历史；未触发独立时序保持UNKNOWN。
5. 采集/砍树/采矿工作中正常关闭再进入，资源可用、无旧预约/工具锁定/工作进度，须新F；工作中保存不提前耗尽、发奖或扣耐久。正常修理/制作材料和Tools仍沿原玩家v2恢复，死亡/R不补满工具。
6. PlayMode前对三类关闭再生并使用匹配新槽，完成后资源档记录耗尽且RemainingSeconds=0；重启仍耗尽、不自动恢复。读取恢复不生成补偿苹果/木材/石材；未拾取地面物重启清空，已入包库存保持。
7. 固定槽重启同地图/种子/签名恢复；换Forest/Grassland读取各自文件。改变种子、资源布局或再生规则后原档签名不符明确拒绝准入、保留旧档；换槽建立新世界。仅改槽名/保存间隔、工具修理量/文案不改变资源签名；configRevision只记录、不替代签名。
8. 使用可控坏资源档，分别核对不支持版本、缺失/未知/重复字段、错类型、根后额外内容、坏UTF-8、错槽/地图/seed/签名、重复/未知身份、非有限/负/超过再生间隔秒数及关闭再生的非零秒数明确失败。不得跳过坏项、回退初始图、重写坏档或让玩家进入部分恢复地图；遗留.tmp不作为恢复来源，未实际触发的独立坏档分支保持UNKNOWN。
9. 正常准入后制造世界写盘失败：原正式档保留、资源/库存/工具已成功结算不回滚，日志包含map/slot/path/阶段，恢复写权限后下一状态变化或检查点保存当前快照。玩家写失败仍按原规则拒绝结算。玩家/世界文件分别替换，异常中断不保证跨文件一致；未实际触发的I/O/文件替换/ECS故障保持UNKNOWN。
10. 两个Client及晚加入读取服务端恢复的耗尽/剩余秒数/阻挡，各自玩家库存/Tools独立；F争抢、G拾取、修理/Drop优先及反馈保持。所有端同版代码/JSON重新烘焙，客户端不读取服务端世界文件；同槽多服务端并发不属于通过范围。
11. PlayMode前修改enabled、saveSlotId和saveIntervalSeconds后正常导入/烘焙，合法值生效。disabled不读写世界档、按原布局重置且保留已有世界文件，仍严格校验新段。空/过长/非法字符/保留名槽、非有限/非正间隔、旧v1～v14、缺失/未知/重复字段和错类型明确失败，无默认补段/回退；未实际触发独立配置分支保持UNKNOWN。
12. 正常关闭/重启、World/Scene退出重进和源更换无旧缓存/实体残留，最后保存只使用完整缓存，不读取已释放资源，不重复关闭写入。回归原F/G、600秒再生/占位/阻挡、高亮/资源状态、工具/修理、制作/Drop/All、E/R/战斗/镜头与玩家保存；未实际触发的清理/网络/时序、字形和性能保持UNKNOWN。

本清单限本段v15/18、世界v1与默认三字段；完整路径/字段/恢复/失败边界归[资源存档](MapResourcePersistence.md)。原189项编号/内容及修理v14/17、资源状态v13/16等旧用户通过保持各自版本/清单；新增后共201项。本阶段人工通过结论来自用户反馈，仅覆盖本段十二项；未实际触发的独立文件I/O/坏档/替换/关闭与重启/占位/预测、多玩家时序/生命周期，以及跨文件原子一致/意外ECS故障恢复、同槽多服务端并发、未覆盖字形、性能/带宽/平台/线上仍UNKNOWN。AI未执行真实存档读写方法、游戏/显示系统、GUI回调、逻辑单元测试、GamePlayer/PlayMode、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 【KNOWN ISSUES】地面掉落物存档与恢复的人工验收

入口CombatPrototypeNetCode，Json/Forest默认schemaVersion=16/configRevision=19、resourcePersistence=true/default_world/10秒/saveGroundDrops=true，世界档写v2、读v1迁移。正常Unity编译、字段反射及24次隔离Editor烘焙静态通过；用户已确认人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限以下十二项清单；未实际触发的独立用例UNKNOWN。单一服务端写同槽，各端同版代码/配置重新烘焙，沿原SinglePlayer或Online启动。

1. 首次无世界档进入，先保存九字段v2且Resources/Drops为空、LastDropId=0，再Ready准入。默认资源/布局、15输入、Drop四Ghost字段、F/G/资源状态四字段及玩家v2加载保持；客户端不读写世界文件。初始写失败不准入。
2. 分别触发敌人苹果、砍树木材、采矿石材及背包Single/All丢弃；快照保留ItemId、实际数量、唯一DropId、落点及剩余寿命，LastDropId包含已分配空号。正常关闭/重启后，未领取物在原保存落点恢复Landed；不直接入包、不重新扣材料/耐久、不补偿发奖。
3. 在0.4秒飞行窗口正常关闭并有成功快照，重启直接恢复原EndPosition的Landed，无再次散落或飞行动画，Quantity/ID保持。Landed保存实际位置；落点可能在地图外，不擅自钳制或避让；不能触发窗口时保持UNKNOWN。
4. 默认600秒寿命检查点/关闭后恢复最近成功余时，离线等待不扣时间，恢复不重置成600秒；到期掉落不进入快照。HasExpiry=true且余时0的合法条目不生成，false/0永久条目继续永久保留。新Lifetime只作用于新生成物，旧物按保存的是否到期/余时恢复。
5. 恢复后G仍按最近Landed目标、同距小DropId选择，先保存玩家候选再库存/Consumed；成功后的世界完整快照排除该物。多人争抢只入包一次，目标文字/蓝圈沿原身份显示；玩家保存失败且物品未到期时保留掉落，恢复后须新G，不自动重试。
6. 背包丢弃准备态、失败/回滚对象、已Consumed/排队销毁对象均不进入Drops。可控丢弃保存失败不扣库存、不把Prepared恢复成可领取物；成功丢弃保存后的物品才在重启恢复。空号保留在LastDropId，未来新ID严格递增，0/负/重复或超过上限编号不能加载。
7. 使用匹配布局/seed的合法世界v1档，严格读取原七字段/资源身份与余时，在内存补Drops=[]、LastDropId=0，不因读取立刻写迁移文件；下一正常状态变化/检查点/关闭保存写v2。耗尽/阻挡基态与离线暂停保持，不凭旧v1补造已丢失掉落。玩家档仍写v2/读v1，地图旧v1～v15配置拒绝。
8. 可控坏v2分别覆盖缺失/未知/重复字段、错类型、根后内容、坏UTF-8、未知版本/槽/地图/seed/签名、负LastDropId、坏DropId/ItemId/Quantity、非有限/超float范围位置、非布尔HasExpiry、非法秒数及永久物非零余时。开启掉落恢复时缺ItemId显式绑定/坏Prefab也失败；整体拒绝准入、坏档保留，不跳过坏项或回退.tmp。未触发的独立分支UNKNOWN。
9. 正常准入后制造世界写失败，旧正式档保留，成功G/丢弃/采集/工具结算不回滚，日志带map/slot/path/reason及原异常，下一变化/检查点重试。捕获失败时资源与掉落均保留上一完整缓存，不保存半批；可控实例恢复失败清理本批掉落并Failed，不部分Ready，清理失败另记。未触发独立失败UNKNOWN。
10. PlayMode前关闭saveGroundDrops并重新烘焙：资源耗尽/再生仍恢复，地面物沿旧规则重启清空，下一成功世界快照Drops=[]/LastDropId=0。resourcePersistence.enabled=false不读写任何世界档、保留原文件，两个开关关闭仍校验全部配置字段，单独关闭saveGroundDrops仍完整校验已读取的v2档；saveGroundDrops缺失/未知/错类型及旧schema15明确拒绝，不补段/回退。
11. 两Client、晚加入/重连读取服务端恢复的同一物品ID/数量/位置，G提示/高亮和拾取结算正常；恢复编号上限后新敌人/木材/石材/丢弃物不撞号，World释放仍由原DropSpawn/ECB拥有和清理。玩家/世界分别保存，G已入包后世界更新失败并中断可能重现旧物，本阶段不承诺跨文件防重复或同槽多服务端并发。
12. 森林/草地、槽名/seed/资源签名隔离沿原规则；正常退出/重启、源更换/Scene/World退出重进只保存最后完整缓存，不读取已释放实体、无重复恢复/销毁。回归三类F采集/600秒再生/占位阻挡、G、工具制作/修理、Drop/All、B显示/高亮/资源状态、E/R/战斗/镜头及玩家保存。未实际触发的时序/清理/字形/性能用例UNKNOWN。

本清单限v16/19、世界v2及本段默认四配置字段，完整契约归[掉落存档](MapDropPersistence.md)/[资源存档](MapResourcePersistence.md)。原201项编号/内容与v15/18资源存档十二项及更早用户通过保持原版本/清单，新增后共213项。本阶段人工结论来自用户反馈；未实际触发的独立I/O/迁移/重启/飞行窗口/到期与永久物、解析/绑定/捕获/实例化/保存/清理失败、多玩家/生命周期与时序仍UNKNOWN；跨文件原子一致与防重复、意外ECS故障恢复、同槽并发、未覆盖字形及性能/带宽/平台/线上不属于通过范围，仍UNKNOWN。AI未调用真实世界/玩家存档读写或运行游戏/显示系统、GUI回调、逻辑单元测试、GamePlayer/PlayMode、构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 【KNOWN ISSUES】掉落物寿命提示与到期预警的人工验收

入口CombatPrototypeNetCode，Json/Forest默认schemaVersion=17/configRevision=20，各端同版重新烘焙。pickupHud新增寿命/预警两个true、阈值30秒、Expires in/Permanent/Expiring soon/s四文案及#FFB454；G400×84/底168/字号20，资源状态底268。正常Unity编译、六字段所属Serializer/Snapshot反射、17配置/Settings和26次隔离Editor烘焙静态通过；本阶段人工GamePlayer为UNKNOWN，以下十二项待用户运行。

1. 森林/草地Json与BuiltIn默认生效，17字段完整必填、G所属六字段Hidden/None/0初值正确。关闭仍校验阈值/文案/颜色/尺寸；旧v1～v16、缺失/重复/未知字段、错布尔/数值/字符串、非有限或非正阈值、非法#RRGGBB和双行高度/G-F/资源状态间隔错误明确失败，无补默认/回退。独立失败未触发则UNKNOWN。
2. 静止合格玩家靠近Landed苹果/木材/石材，第一行仍G Pick up物品×数量，第二行Expires in剩余秒数；只显示原最近G目标，2米及同距小DropId规则保持。Prepared/Airborne/Consumed、过期及范围外不显示；切换同种/异种目标和不同数量/余时无旧缓存残留。
3. 剩余秒数由目标实际ExpiresAt减服务端模拟时间向上取整；30秒以上白字，实际余时≤30秒切橙色Expiring soon，取整不提前触发预警。有效余时不足1秒仍显示1s，实际到期后原选择/清理收起或切下一目标。检查默认及自定义5.5秒阈值，精确边界未触发保持UNKNOWN。
4. PlayMode前设drops.lifetimeSeconds=0并正常重新烘焙，新永久物显示白色Permanent且余时字段0，不切预警、不倒数或自动过期。世界v2恢复的HasExpiry=false/0条目同样永久；Timed与Permanent切换不保留颜色/旧秒数。
5. 存档开启并有成功快照后正常关闭/重启，原掉落ID/数量与余时恢复；新显示使用恢复后的ExpiresAt，不把旧物重置为当前600秒或新Lifetime，离线暂停计时。飞行物仍按原定落点恢复Landed再显示。世界仍写v2/合法v1内存迁移、玩家仍写v2/读v1，UI字段不写入存档或资源签名。
6. lifetimeEnabled=false重新烘焙，仅原G一行及蓝圈/拾取保持；默认配置G高度仍84，可显式设52并将资源状态底距设236。expiryWarningEnabled=false仍显示剩余秒数，临期继续白色Expires in；原到期时刻保持。全部17字段即使关闭仍须合法。
7. pickupHud.enabled=false、G高亮开启时只显示原蓝圈，G所属目标仍采样但寿命None/0；G文字与G圈同时关闭时六字段Hidden/零，原G仍可拾取。单独关闭F HUD/背包或资源状态仍可显示G寿命；全部显示关闭正确释放绑定。
8. 改阈值、四文案、秒单位和颜色#12ABEF，经正常导入/烘焙生效；两行居中、间隔8、字号/宽度/双行最小高度校验正确。F320×104/底48、G400×84/底168、资源状态400×52/底268各相隔16，B及F同时显示、不同分辨率/缩放下不重叠；未覆盖排版/字形UNKNOWN。
9. 移动/攻击/非Ready近战/死亡沿原G资格隐藏目标及寿命，恢复合格后重新采样；断线、重连/新玩家、地图/World/Scene源变化清可见状态与缓存。Hidden或关闭寿命时新字段必须None/0；非法寿命枚举/非有限/非正/非整数秒或Permanent非零余时在显示边界记录DropId/itemId/原异常并保持G隐藏。
10. 按G仍由处理tick重新选目标、先保存玩家候选才库存/Consumed；成功后寿命行随目标消失或切换，保存失败且未到期时保留物和实际余时，不显示保存成功。仅文字绘制不发命令、不自动拾取或修改期限/数量/库存/工具。世界写失败及跨文件风险沿原边界。
11. 两Client、晚加入/重连只收本人六字段快照，不使用客户端墙钟推演或读取其他玩家目标/服务端DropProgress；同一目标仍按原服务端时刻到期。网络延迟可能使文字滞后，实际拾取/到期依原服务端判断，未触发多玩家/延迟/预测时序保持UNKNOWN。
12. 回归F三类采集/600秒再生/阻挡、G文字与蓝圈、B/工具制作与修理、Drop/All、E/R/战斗/镜头及玩家/资源/掉落保存。15输入、F/资源状态各4、Drop Ghost4和原反馈保持；关闭/重启及生命周期无上一局文字或预警颜色残留。独立绑定/捕获/绘制/保存/清理、性能/带宽/平台/线上UNKNOWN。

本清单限v17/revision20寿命显示；原213项编号/内容与v16/19掉落存档十二项及更早用户通过保持旧版本/清单，新增后共225项。完整规则归[拾取提示](MapPickupHud.md)。新增人工、真实倒计时/小数阈值/永久物/恢复余时、排版/颜色/字形、独立失败、多玩家/晚加入/延迟与生命周期仍UNKNOWN；跨文件原子一致/防重复、意外ECS故障恢复、同槽并发及性能/带宽/平台/线上不属于通过范围。AI未调用真实玩家/世界存档读写或运行游戏/显示系统/GUI回调、GamePlayer/PlayMode、逻辑单元测试、构建、发布、采样或图片检查，未创建子Agent或提交Git。
