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

当前 `Assets/Scripts`、`Assets/Test` 和工程程序集未发现 NetCode World、`ClientServerBootstrap`、网络流请求或网络实体同步入口；`com.unity.multiplayer.center` 仅表示包已安装，不代表联机运行链已接入。第 0 阶段未新增网络启动脚本，也未修改 Scene/Prefab/Animator；可运行网络生命周期需要明确启动场景和 World 管理边界后再接入。

当前渲染配置仍为 Built-in：`ProjectSettings/GraphicsSettings.asset` 的 `m_CustomRenderPipeline` 为零引用，QualitySettings 各档 `customRenderPipeline` 也为零引用。项目未检出 Universal Render Pipeline 包或资源引用，URP 切换保留为后续独立步骤。

当前 Packages 清单未包含 Addressables，`Assets` 内也未发现 Addressables 设置资源或运行时调用；现有资源加载边界仍由既有 QFramework/Resources 链维护，迁移不属于本阶段。

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

独立场景绑定已写入 YAML，仍需主线程在 Unity 编辑器打开场景并进行人工 GamePlayer 验收；当前未修改正式 Map 场景、Prefab 或 Animator。该场景未加入正式构建列表。

- `UNKNOWN`：正式启动体验、输入设备与平台要求、调试按键是否属于产品功能。
- `UNKNOWN`：第 3 阶段清单之外的面板交互与显示效果、生命周期调用组合，以及全部场景组件的完备性。第 4 阶段已完成两个面板和详情文本监听的静态生命周期接入，人工交互验收仍待主线程确认。
- 第 2 阶段玩家状态与存储改动已有用户“实际行为和日志均已核对正确”的反馈，并由主线程结合代码、文档、资源静态检查判定该阶段通过；不扩展为全部场景或平台验收。
- 第 3 阶段人工 GamePlayer 清单（初始化、生成/回收复用、失败项隔离、退出后重新进入 Map，以及实际行为和日志）已获用户“已确认正常”的反馈；主线程结合代码、资源、文档及引用留存静态验收，已判定该阶段通过。第 4 阶段 UI 刷新与释放代码已完成静态落地，详情使用效果和平台验证仍未覆盖。

相关模块：[玩家](Player.md)、[背包与道具](Inventory.md)、[战斗](Combat.md)、[框架与工具](Framework.md)。
