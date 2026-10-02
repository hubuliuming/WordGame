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

`CombatPrototypeNetCodeBootstrap.Initialize` 通过官方 `DiscoverAutomaticNetcodeBootstrap` 读取启动场景标记；标记启用时以端口 `7979` 调用 `ClientServerBootstrap` 创建所选 Client/Server World，并启用后台运行。未启用标记时调用 `CreateLocalWorld`。该判断沿用包对启动时场景尚未有效的处理，不直接依赖早期 `GetActiveScene().name`。

独立主场景为 `Assets/Scenes/CombatPrototypeNetCode.unity`，保留 Main Camera、Directional Light 与自动加载的 `CombatPrototypeNetCodeSubScene`。子场景路径为 `Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity`，唯一 `CombatPrototypeNetworkRoot` 挂载 `CombatPrototypePlayerSpawnerAuthoring`，显式引用玩家、敌人两个 Ghost Prefab；Root 自身没有 Ghost，场景中没有额外玩家或敌人 Ghost 实例。

运行调用链为：`SubScene 数据加载 → 客户端发送 GoInGame RPC → 服务端唯一握手入口生成玩家 → GhostOwner / AutoCommandTarget / CommandTarget 绑定 → 玩家加入连接 LinkedEntityGroup → NetworkStreamInGame`。重复或失效 RPC 不重复生成玩家；连接销毁时由 NetCode 的 LinkedEntityGroup 销毁对应玩家。服务端的 `CombatPrototypeEnemySpawnSystem` 从同一个 Spawner 批量生成 `32` 个现有敌人 Ghost，以 `(0, 1, 2)` 为首个网格位置，在 X/Z 平面按 `8` 列、`4` 行、间距 `3` 排列；玩家生成位置沿用 `(NetworkId * 2, 1, 0)`。批量生成只尝试一次，逐项记录和隔离实例化/初始化失败，清理当前项半成品，日志分别记录成功与失败数量。

`Assets/Prefabs/CombatPrototype/` 中的玩家 Ghost 使用 `HasOwner`、`OwnerPredicted`、`SupportedGhostModes=All` 和自动输入目标；敌人 Ghost 使用 `Interpolated`。玩家通过官方 `GhostPresentationGameObjectAuthoring.ClientPrefab` 绑定原 Mono 表现 Prefab，并由官方桥接同步 Transform，服务端表现引用为空。敌人根节点已直接配置 MeshFilter/MeshRenderer，原 ClientPrefab 已清空；实体渲染与死亡隐藏见本页第 3B-2 节。

## 【CURRENT STRATEGY】第 3A 阶段观测入口

原型保留日志入口：`CombatPrototypeNetCodeLogSystem` 在 Client/Server World 中记录玩家数量及敌人存活/死亡数量变化，每 2 秒记录玩家位置、旋转、攻击阶段/序号，以及各敌人 Ghost ID、位置、HP、受击序号、死亡标记；服务端同时记录各敌人目标 NetworkId。服务端另记录握手、批量生成结果、攻击开始、空间查询命中目标数和事件扣血日志。WASD 写入世界 X/Z 平面移动，空格或鼠标左键发送基础攻击事件；状态含义见[玩家](Player.md)与[战斗](Combat.md)。

## 【KNOWN ISSUES】第 2B 阶段验收边界

- 代码编译、Unity 资源绑定与 Editor 配置的 SubScene 烘焙产物已核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。
- 验收范围仅当前独立网络原型：两个玩家加入/退出、跨客户端移动与朝向同步、本地输入预测、基础近战和单敌人生命/受击/死亡状态及 Mono 表现；不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收。
- UI 保留 TODO；房间/匹配、Relay、敌人反击、寻路避障、正式 Map、属性奖励和存档未接入本网络原型。第 3A 已接入群体生成、最近在线玩家追踪及空间查询伤害链，验收边界见下节。
- 当前渲染管线为 URP，玩家保留官方 Mono 表现桥接，敌人已接入 Entities Graphics；第 3B-2 已由用户确认人工 GamePlayer 验收通过，范围见本页对应验收节。Console 中的既有 No SRP、PEListener 序列化与 DOTween 弃用记录不能作为本阶段运行验收通过的依据，诊断记录见 ChangeLog。
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

第 3A 代码已经 Unity 编译，Editor 保存的 Prefab/SubScene 参数和实际烘焙产物已读取核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。验收仅覆盖当前独立网络原型的双客户端 32 敌人生成、最近在线玩家选择与玩家加入/退出后的目标切换、停止距离与无在线玩家时停止追踪、群体近战每次攻击每目标去重及伤害 25、死亡后停止移动和受击，以及双端 HP、死亡状态与存活/死亡统计一致性。第 2B 的通过结论只覆盖此前单敌人原型范围；本次第 3A 通过结论不扩展为正式 Map、平台构建、大规模性能或线上联调验收。AI 未执行逻辑单元测试、PlayMode、命令行构建或平台发布，也未读取图片。当前使用 URP，玩家保留 Mono 表现；敌人 Entities Graphics 接入的验收边界见第 3B-2 节，敌人反击与寻路避障未接入。

## 【FACT】第 3B-1 阶段渲染配置与材质

`Packages` 中 URP、Universal Config、Shader Graph 与 Render Pipelines Core 均为 `17.5.0`；Searcher `4.9.4` 是 Shader Graph 的解析依赖。URP 全局设置为 `Assets/UniversalRenderPipelineGlobalSettings.asset`，其默认 Volume Profile 为 `Assets/DefaultVolumeProfile.asset`，当前 components 为空；包级配置还包括 `ProjectSettings/URPProjectSettings.asset` 与 `ShaderGraphSettings.asset`。

玩家与敌人的 `CombatPrototypeNetworkPlayerView.prefab`、`CombatPrototypeNetworkEnemyView.prefab` 共用 `Assets/Materials/CombatPrototype/CombatPrototypeNetworkView.mat`，Shader 为 `Universal Render Pipeline/Lit`，白色、不透明、Metallic=0、Smoothness=0.5、无贴图。两份 View Prefab 保留原 Mesh、组件及层级；玩家 Ghost 仍通过原 ClientPrefab 引用 PlayerView，服务端表现引用为空。敌人 Ghost 复用 EnemyView 的网格与该材质，其当前实体渲染绑定见第 3B-2 节；旧 EnemyView 资产保留。

管线 MSAA 为 `1`（关闭多重采样），Render Scale 为 `1`；URP 将当前 Ultra 的 `QualitySettings.antiAliasing` 同步为 `0`。`lightsUseLinearIntensity` 与 `lightsUseColorTemperature` 均为 true，由 URP 依据当前 Linear 配置设置。Graphics/Quality 文件使用当前 Unity 序列化版本及其默认字段。

## 【CURRENT STRATEGY】第 3B-1 阶段渲染接入边界

URP 与 Linear 是全项目配置，影响所有场景；玩家保留原 Mono 表现桥接，敌人当前表现见第 3B-2 节。群体生成、追踪、近战、伤害、Ghost 同步及 32 敌人配置保持第 3A 已验收逻辑；场景结构、构建列表和自定义 UI Shader 保持原状。第 3B-1 的验收仅覆盖管线前置配置，不包含第 3B-2 实体渲染。

## 【KNOWN ISSUES】第 3B-1 阶段验收边界

主线程已核对包版本、磁盘与编辑器的 Forward+/Linear/SRP Batcher 配置、六档管线引用、两份材质绑定和实际 SubScene 烘焙产物，静态验收通过。用户已确认本阶段人工 GamePlayer 验证通过，主线程结合静态检查与用户反馈判定第 3B-1 阶段通过。人工验收范围仅覆盖网络场景外观、亮度与边缘效果、Map UI/TMP/自定义 Outline Shader 兼容，以及切换 URP/Linear 后双端 32 敌人群体行为回归；不扩展为平台构建、大规模性能或线上联调验收。第 3A 人工通过结论保持原验收范围。AI 未运行 PlayMode、逻辑单元测试、命令行构建、平台发布或图片检查；第 3B-1 人工通过结论不扩展为第 3B-2 敌人实体渲染与死亡隐藏验收。

## 【FACT】第 3B-2 阶段敌人实体渲染

`CombatPrototypeNetworkEnemy.prefab` 在既有根节点新增 MeshFilter 和 MeshRenderer，复用旧 EnemyView 的 Capsule 网格（单 submesh）、`CombatPrototypeNetworkView.mat` 的 URP/Lit 材质及 Renderer 配置。原 `GhostPresentationGameObjectAuthoring` 组件保留，ClientPrefab 与 ServerPrefab 均为空；根层级、Ghost 参数、生命与移动参数未变。旧 EnemyView Prefab 和脚本保留，玩家仍使用原 PlayerView 桥接。

现有 SubScene 的 Editor 配置已重新烘焙并读回：Spawner 仍为 1 个，敌人配置为 32 个、8 列、间距 3、首位置 `(0, 1, 2)`。敌人根实体同时具备 `CombatPrototypeEnemyState`、启用的 `MaterialMeshInfo`、`RenderMeshArray`、`RenderBounds`、`WorldRenderBounds` 与 `LocalToWorld`，LinkedEntityGroup 仅含根实体；网格与材质引用已核对，根实体不再含 `GhostPresentationGameObjectPrefabReference`。玩家仍有原 Mono 表现引用且没有 MaterialMeshInfo。

## 【CURRENT STRATEGY】第 3B-2 阶段显示调用链

`CombatPrototypeEnemyRenderSystem` 仅在 ClientSimulation World 的 PresentationSystemGroup 执行，并排在 EntitiesGraphicsSystem 之前。系统只读取 Ghost 同步的 IsDead，通过 `EnabledRefRW<MaterialMeshInfo>` 写启用状态：IsDead 为 0 时显示，否则隐藏。查询使用 `IgnoreComponentEnabledState`，已经隐藏的敌人仍参与显示状态更新；不写生命、位置、伤害或 Ghost 参数，不销毁死亡实体。

## 【KNOWN ISSUES】第 3B-2 阶段验收边界

新增系统已进入 Unity 加载程序集，Prefab 保存和实际烘焙产物已静态核对；用户已确认第 3B-2 人工 GamePlayer 验证通过，主线程结合既有静态检查与用户反馈判定第 3B-2 阶段通过。验收仅覆盖当前独立网络原型的双端敌人显示与移动、死亡隐藏无重复显示或残影、重新加入后的死亡状态，以及 HP/存活/死亡统计一致性。第 2B、第 3A 与第 3B-1 通过结论保持各自原验收范围；本阶段不包含规模性能、平台构建或线上联调结论。人工通过结论来自用户反馈，AI 未运行逻辑单元测试、PlayMode、命令行构建、平台发布或图片检查。

## 【FACT】第 4A 阶段网络玩家资源与烘焙

`Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkPlayer.prefab` 的既有玩家 Authoring 保存 `InitialPower=100`、`UpperPower=100`、`AttackPowerCost=10`。Prefab 仍为原单根节点与原 5 个组件，玩家 Mono 表现引用和 Ghost 参数保持原值；新增的是烘焙后的 `CombatPrototypePlayerResource` ECS 数据，没有增加 Mono 组件挂载或修改 Scene/SubScene 层级。

现有 SubScene 的 Editor 配置已重新烘焙并只读反序列化核对：玩家含体力组件，当前值/上限为 100/100，成本为 10，初始攻击阶段 Ready、序号 0；移动速度 5 与原近战参数保持。Spawner 仍为 1 个，敌人 32 个、8 列、间距 3、首位置 `(0, 1, 2)`；敌人 HP100、速度2、停止距离1.5、伤害缓冲和根实体渲染组件保持，玩家仍引用原 PlayerView。读取用临时 World，systems=0，读取后释放，没有执行游戏系统。

## 【CURRENT STRATEGY】第 4A 阶段观测入口

`CombatPrototypeNetCodeLogSystem` 在原每 2 秒玩家快照中增加 `power=CurrentPower/UpperPower`，两端分别记录各自服务端状态或已收到的 Ghost 状态。服务端近战日志增加攻击接受/拒绝原因：`ReadyAndPowerAvailable`、`InsufficientPower`、`AttackInProgress`，并包含玩家 NetworkId、体力和攻击序号；规则由[战斗](Combat.md)维护。输入、移动预测、握手、敌人群体和显示调用链沿用原入口，没有新增 UI。

## 【KNOWN ISSUES】第 4A 阶段运行验收

代码编译、新组件类型加载、玩家 Prefab 参数及实际烘焙数据已静态核对；第 4A 人工 GamePlayer 结果仍为 `UNKNOWN`，包括双玩家独立体力及双端日志一致性、消耗与拒绝分支、不自动恢复、重新加入初始体力和原移动/32 敌人战斗表现回归。既有第 2B、第 3A 与第 3B 人工通过结论保持原范围；AI 未执行逻辑单元测试、PlayMode、命令行构建、平台发布或图片读取。
