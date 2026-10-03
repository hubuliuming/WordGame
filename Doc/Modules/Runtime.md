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

独立主场景为 `Assets/Scenes/CombatPrototypeNetCode.unity`，保留 Main Camera、Directional Light 与自动加载的 `CombatPrototypeNetCodeSubScene`。子场景路径为 `Assets/Scenes/CombatPrototypeNetCode/CombatPrototypeNetCodeSubScene.unity`，唯一 `CombatPrototypeNetworkRoot` 挂载 `CombatPrototypePlayerSpawnerAuthoring`，显式引用玩家、敌人两个 Ghost Prefab；Root 自身没有 Ghost，场景中没有额外玩家或敌人 Ghost 实例。

运行调用链为：`SubScene 数据加载 → 客户端读取第 4D 固定 ID 并发送 GoInGame RPC → 服务端唯一握手入口验证身份与存档 → 生成玩家并恢复金币/经验/背包 → GhostOwner / AutoCommandTarget / CommandTarget 绑定 → 玩家加入连接 LinkedEntityGroup → NetworkStreamInGame`。重复或失效 RPC 不重复生成玩家；连接销毁时由 NetCode 的 LinkedEntityGroup 销毁对应玩家。服务端的 `CombatPrototypeEnemySpawnSystem` 从同一个 Spawner 批量生成 `32` 个现有敌人 Ghost，以 `(0, 1, 2)` 为首个网格位置，在 X/Z 平面按 `8` 列、`4` 行、间距 `3` 排列；玩家生成位置沿用 `(NetworkId * 2, 1, 0)`。批量生成只尝试一次，逐项记录和隔离实例化/初始化失败，清理当前项半成品，日志分别记录成功与失败数量。

`Assets/Prefabs/CombatPrototype/` 中的玩家 Ghost 使用 `HasOwner`、`OwnerPredicted`、`SupportedGhostModes=All` 和自动输入目标；敌人 Ghost 使用 `Interpolated`。玩家通过官方 `GhostPresentationGameObjectAuthoring.ClientPrefab` 绑定原 Mono 表现 Prefab，并由官方桥接同步 Transform，服务端表现引用为空。敌人根节点已直接配置 MeshFilter/MeshRenderer，原 ClientPrefab 已清空；实体渲染与死亡隐藏见本页第 3B-2 节。

## 【CURRENT STRATEGY】第 3A 阶段观测入口

原型保留日志入口：`CombatPrototypeNetCodeLogSystem` 在 Client/Server World 中记录玩家数量及敌人存活/死亡数量变化，每 2 秒记录玩家位置、旋转、攻击阶段/序号、生命/上限、受击序号及死亡标记，以及各敌人 Ghost ID、位置、HP、受击序号、死亡标记；服务端同时记录各敌人目标 NetworkId。服务端另记录握手、批量生成结果、攻击开始、空间查询命中目标数和事件扣血日志。WASD 按当前本地镜头水平角转换后写入世界 X/Z Move，空格或鼠标左键发送基础攻击事件；第 6B 的 R 键发送手动复活请求，第 7A 的 E 键发送物品使用请求，独立服务端系统记录接受、拒绝和处理失败。状态含义见[玩家](Player.md)、[战斗](Combat.md)与[背包与道具](Inventory.md)。

## 【KNOWN ISSUES】第 2B 阶段验收边界

- 代码编译、Unity 资源绑定与 Editor 配置的 SubScene 烘焙产物已核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 2B 阶段通过。
- 验收范围仅当前独立网络原型：两个玩家加入/退出、跨客户端移动与朝向同步、本地输入预测、基础近战和单敌人生命/受击/死亡状态及 Mono 表现；不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收。
- UI 保留 TODO；房间/匹配、Relay、寻路避障、正式 Map、正式属性奖励与正式存档保持原边界；独立网络奖励与开发固定 ID 存档的当前入口见本页第 4B/4C/4D 节，敌人反击与玩家受伤见第 6A 节。第 3A 已接入群体生成、最近在线玩家追踪及空间查询伤害链，验收边界见下节。
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

第 3A 代码已经 Unity 编译，Editor 保存的 Prefab/SubScene 参数和实际烘焙产物已读取核对；用户已确认本阶段人工 GamePlayer 验收通过，主线程结合静态检查与用户反馈判定第 3A 阶段通过。验收仅覆盖当前独立网络原型的双客户端 32 敌人生成、最近在线玩家选择与玩家加入/退出后的目标切换、停止距离与无在线玩家时停止追踪、群体近战每次攻击每目标去重及伤害 25、死亡后停止移动和受击，以及双端 HP、死亡状态与存活/死亡统计一致性。第 2B 的通过结论只覆盖此前单敌人原型范围；本次第 3A 通过结论不扩展为正式 Map、平台构建、大规模性能或线上联调验收。AI 未执行逻辑单元测试、PlayMode、命令行构建或平台发布，也未读取图片。当前使用 URP，玩家保留 Mono 表现；敌人 Entities Graphics 接入的验收边界见第 3B-2 节，敌人反击/玩家受伤见第 6A 节，寻路避障仍未接入。

## 【FACT】第 3B-1 阶段渲染配置与材质

`Packages` 中 URP、Universal Config、Shader Graph 与 Render Pipelines Core 均为 `17.5.0`；Searcher `4.9.4` 是 Shader Graph 的解析依赖。URP 全局设置为 `Assets/UniversalRenderPipelineGlobalSettings.asset`，其默认 Volume Profile 为 `Assets/DefaultVolumeProfile.asset`，当前 components 为空；包级配置还包括 `ProjectSettings/URPProjectSettings.asset` 与 `ShaderGraphSettings.asset`。

玩家与敌人两份 View 的原根 MeshRenderer 仍引用 `Assets/Materials/CombatPrototype/CombatPrototypeNetworkView.mat`，Shader 为 `Universal Render Pipeline/Lit`，白色、不透明、Metallic=0、Smoothness=0.5、无贴图。PlayerView 原根 Renderer 当前关闭，实际角色由 VisualRoot 的灰衣修士模型和独立材质显示，资源与动画归[玩家美术](../PlayerArt.md)；玩家 Ghost 仍通过原 ClientPrefab 引用同一 PlayerView，服务端表现引用为空。敌人 Ghost 继续复用 EnemyView 的 Capsule 网格与原材质，其当前实体渲染绑定见第 3B-2 节；旧 EnemyView 资产保留。

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

窗口显示现有开发身份及来源，提供身份文件定位和原 SubScene/玩家/敌人 Prefab 的资源选择入口，不保存身份或覆写玩法参数。生成、生命、体力、移动、攻击及奖励数值仍由原 Spawner/Authoring Inspector 和既有烘焙链负责；Scene、Prefab 层级、原组件挂载及旧 meta 保持。

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
