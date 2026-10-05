# 2026-10 变更记录

## 2026-10-01：第 2B 阶段独立 NetCode 原型落地

- 按前轮已确认的网络 World、SubScene/Ghost、Baker/Input、Mono 表现桥接及独立测试资源范围执行；继续日志入口，未创建 UI。
- 修改 `Assets/Scripts/CombatPrototype/Networking/CombatPrototypeNetCodeBootstrap.cs`：通过官方场景标记限定网络启动，未启用标记时创建本地 World；网络端口 7979。
- 修改 `CombatPrototypeNetCodeLifecycle.cs`：统一 GoInGame RPC 与玩家生成，过滤重复/失效请求，设置 GhostOwner、AutoCommandTarget、CommandTarget，并加入连接 LinkedEntityGroup；服务端生成唯一敌人。
- 将 `CombatPrototypePlayerNetCode.cs` 中未序列化绑定的 Authoring/输入/移动职责拆入文件名匹配脚本：`CombatPrototypePlayerNetCodeAuthoring.cs`、`CombatPrototypePlayerSpawnerAuthoring.cs`、`CombatPrototypePlayerInput.cs`、`CombatPrototypePlayerMovementSystem.cs`；保留数据与攻击阶段定义。使用 InputEvent 攻击、Simulate 过滤、直接朝向赋值和外部移动输入校验。
- 新增 `CombatPrototypeEnemyNetCodeAuthoring.cs`、`CombatPrototypeMeleeServerSystem.cs`、`CombatPrototypeNetCodeLogSystem.cs`、`CombatPrototypeEnemyNetCodeView.cs`。服务端按前摇/命中/后摇处理单敌人，同步攻击阶段/序号及 HP/受击序号/死亡标记；日志按 World 输出实体数量与状态；敌人 Mono 表现仅读死亡标记控制已绑定 Renderer。
- 参数沿用已读取的第 1 阶段序列化值：移动速度 5、伤害 25、距离 2、角度 100、前摇 0.18 秒、命中阶段 0.08 秒、后摇 0.3 秒、敌人初始 HP 100。
- 通过 Unity Editor PrefabUtility 保存 `Assets/Prefabs/CombatPrototype/` 下玩家 Ghost、敌人 Ghost、玩家 View、敌人 View。玩家启用 HasOwner/OwnerPredicted/All/AutoCommand；敌人使用 Interpolated；ClientPrefab 分别显式绑定 Mono 表现 Prefab，ServerPrefab 为空。EnemyView.bodyRenderer 已绑定。
- 通过 Unity Editor 保存 `Assets/Scenes/CombatPrototypeNetCode.unity` 与对应 SubScene：主场景添加官方 OverrideAutomaticNetcodeBootstrap 标记；SubScene 仅保留一个 Transform + Spawner 的 NetworkRoot，移除临时玩家和 Root Ghost，绑定两个 Ghost Prefab。保留主场景 Camera/Light，SubScene 保存后关闭。
- 新脚本/Prefab 的 meta 与 Ghost prefabId 均由 Unity 生成；再经 PrefabUtility.SavePrefabAsset 持久化并复读磁盘：玩家 `5d400bfd20b5bf44e8a400f981e2754a`、敌人 `6b69b054cdcf4a5479db0e7fb263497c`，与对应 meta 一致。未手写 Ghost/SubScene YAML 或烘焙数据。
- Unity 刷新和脚本编译完成，新 Authoring 类型在已加载 Assembly-CSharp 中可反射读取。修复编译时 `Code_01.System` 对 `System.InvalidOperationException` 的遮蔽，使用 `global::System.InvalidOperationException`。
- 使用已核实包 API `SubSceneInspectorUtility.ForceReimport` 和 `EntityScenesPaths.GetSubSceneArtifactHash` 请求 Editor 配置 `dd3fd3638f8b63e7392371e56e19b70a` 的真实 SubScene 烘焙。Unity 更新 `Assets/SceneDependencyCache/3dbc4e16b72a3700e3a52b53099600c0.sceneWithBuildSettings` 及配套 meta；AssetImportWorker 日志记录导入成功。
- 最终保存后的 Import Result ID 为 `21d8f9d78a4da6354fa39dd592aa23d9`；产物包括 `VirtualArtifacts/Extra/21/21d8f9d78a4da6354fa39dd592aa23d9.0.entities`、`.entityheader`、`.0.asset`、`.exportedtypes` 等。
- 在不创建或更新游戏系统的临时 World 中，调用官方 `EditorEntityScenes.Read` 反序列化最终产物，读取后立即 Dispose：Spawner 数量为 1；玩家/敌人引用均带 Prefab；玩家包含业务输入、生成的 InputBufferData、GhostOwner、AutoCommandTarget.Enabled=true、PredictedGhost、Simulate；移动/近战参数正确；敌人 HP=100、HitSequence=0、IsDead=0；两个 ClientPrefab 和 EnemyView Renderer 引用均有效。这是烘焙资源检查，不是逻辑单元测试或多人运行验收。
- 收尾 Editor 状态：活动场景为独立网络主场景，场景未脏、未处于编译中、非 PlayMode，SubScene 关闭且 AutoLoadScene=true。Console 保留既有 PEListener 序列化、DOTween 弃用诊断，以及 5 条 Exception 类型的 Built-in 相关异常诊断（No SRP，Entities Graphics/Deformation 未启用）；未变更渲染管线，未将 Console 记为清零。
- 同步 `Doc/Modules/Runtime.md`、`Combat.md`、`Player.md`、`Framework.md` 受影响段落，移除“尚无 Ghost/输入同步”等旧原型描述；Runtime 同步说明用户预先安装的 Addressables 2.9.1 与设置资源，本次没有修改包或迁移加载链。
- 未执行逻辑单元测试、命令行构建、平台发布、人工 GamePlayer 或图片读取；未修改正式 Map、第 1 阶段场景、Animator、Packages、构建配置、正式 UI/属性奖励/存档。两个实际玩家加入退出、移动/旋转同步、预测修正、攻击/受击/死亡同步及 Mono 显示仍待人工 GamePlayer 和主线程最终验收。
- 主线程只读复查确认：主场景未脏、非 PlayMode、未处于编译中，SubScene 关闭且自动加载，官方 Bootstrap 标记启用；4 个 Prefab 的缺失组件数量均为 0；本轮代码与文档定向 diff 检查通过；Map、本地切片、Packages、GraphicsSettings、BuildSettings、配置及 9 月 ChangeLog 的 hash 与本轮基线一致。主线程判定本次代码、资源与文档静态验收通过；整个第 2B 阶段仍待人工 GamePlayer 验收，尚未判定阶段通过。

## 2026-10-01：第 2B 阶段人工验收通过

- 用户明确反馈“我已经验收通过，接下来下一阶段”；主线程结合已完成的代码、资源和文档静态验收，判定第 2B 阶段通过。
- 本次人工 GamePlayer 验收仅覆盖当前独立网络原型：两个玩家加入退出、移动朝向与本地输入预测、基础近战、单敌人生命/受击/死亡同步及 Mono 表现；不扩展为群体 ECS、正式 Map、平台构建、大规模性能或线上联调验收。
- 更新 Runtime、Combat、Player、Framework 的第 2B 当前验收状态，保留此前落地记录、Built-in 相关真实异常诊断和未接入功能。本次只更新验收文档；AI 未运行逻辑单元测试、PlayMode 或构建，未实施下一阶段代码或资源。

## 2026-10-01：第 3A 阶段 ECS 群体逻辑落地

- 按已确认第 3A 方案及既有资源参数权限执行：32 敌人、8×4 网格、间距 3；复用现有敌人 Ghost，以原 EnemyPosition (0, 1, 2) 为首格位置。用户已授权子 Agent 按步骤执行，当前记录不构成人工验收通过。
- 新增 CombatPrototypeEnemySpawnSystem.cs：从服务端握手系统中移出敌人生成职责，保留玩家握手原链；批量入口检查共享 Prefab 组件，逐项隔离实例化/初始化异常并清理半成品，只统计成功实例，最终输出成功/失败数量。
- 新增 CombatPrototypeEnemyMovementSystem.cs：从当前 Connected/InGame 连接的有效 CommandTarget 选取 X/Z 最近玩家，同距离按 NetworkId；速度 2，距离 1.5 停止并限制当 tick 步长；无玩家或死亡停止。新增服务端移动参数与目标状态。
- 新增 CombatPrototypeEnemySpatialSystem.cs：敌人移动后重建格宽 2 的 X/Z 空间索引，每个存活敌人只加入一个格子；复用 NativeParallelMultiHashMap，World 销毁时释放。
- 修改 CombatPrototypeMeleeServerSystem.cs：移除单敌人 GetSingleton，保留攻击时序，Startup 结束一次空间格筛选并执行原距离/角度判断，每攻击每目标最多生成一条伤害事件。新增 CombatPrototypeDamageSystem.cs 统一消费事件，写入 HP/受击/死亡并清空缓冲，死亡目标不再扣血。
- 修改 CombatPrototypePlayerSpawnerAuthoring.cs 与 CombatPrototypeEnemyNetCodeAuthoring.cs：烘焙群体配置和移动/目标/伤害缓冲；移动、目标、伤害事件仅服务端保留。修改 CombatPrototypeNetCodeLogSystem.cs：增加敌人总数、存活/死亡数、Ghost ID/位置和服务端目标 NetworkId；保留玩家状态及敌人生命日志。
- 通过 Unity Editor PrefabUtility 保存敌人 Prefab 的 MoveSpeed=2、StopDistance=1.5；通过 EditorSceneManager 保存现有 SubScene 的 EnemyCount=32、EnemyColumns=8、EnemySpacing=3。Root 数量仍为 1，组件仍为 Transform+Spawner；玩家/敌人 Prefab 引用不变；保存后关闭 SubScene，主场景未脏。未手写 Ghost/SubScene YAML 或烘焙数据，新脚本 meta 由 Unity 生成。
- 初次仅请求脚本编译时新增文件尚未导入，产生缺少新类型的 CS0246；随后完整 AssetDatabase 刷新导入新增脚本，Unity 编译成功，新空间系统可从 Assembly-CSharp 读取；移除迁出生成职责后遗留的 _enemySpawned 字段，再次编译并读取最终烘焙产物。最终检查没有本轮 C# 编译错误；Console 仍保留 Built-in 下 No SRP / Entities Graphics / Deformation 的既有类别诊断，未声称清零。
- 使用官方包 API 强制重导入并读取 Editor 配置 dd3fd3638f8b63e7392371e56e19b70a 的 SubScene。最终 Import Result ID 为 2932685b2538f41ae0ba11e649053753；产物为 VirtualArtifacts/Extra/29/2932685b2538f41ae0ba11e649053753.0.entities、.0.asset、.entityheader 等，AssetImportWorker 记录导入成功。
- 在仅反序列化的临时 World 中经 EditorEntityScenes.Read 核对该产物：Spawner=1，数量32/列8/间距3/原点(0,1,2)；玩家和敌人均为 Prefab；敌人 HP100、HitSequence0、IsDead0、速度2、停止距离1.5、目标Entity.Null/NetworkId0、伤害缓冲长度0；原近战参数25/2/100/0.18/0.08/0.3未变。临时 World 的 Systems.Count=0，读取后立即释放。
- 同步 Runtime、Combat、Player、Framework 的受影响事实、当前策略及验收边界。未执行逻辑单元测试、PlayMode、命令行构建、发布或图片读取；未接入敌人反击、寻路避障、Entities Graphics 群体表现或 URP。第 3A 人工 GamePlayer 群体验收尚未完成；主线程静态验收结论见下条。
- 主线程复查批量生成、在线目标选择、空间查询去重和伤害事件清空路径；四个 Prefab 缺失脚本数量均为 0，最终程序集无 _enemySpawned；主场景未脏、非 PlayMode、非编译中，SubScene 关闭且 AutoLoad 开启。正式 Map、本地切片、网络主场景、Packages、Build/Graphics/Quality 配置、其他本轮基线配置及 9 月 ChangeLog 的哈希均与本轮基线一致；最终烘焙 Import Result ID 为 2932685b2538f41ae0ba11e649053753。主线程判定第 3A 代码、资源和文档静态验收通过；整阶段仍待用户人工 GamePlayer 验收，尚未判定第 3A 阶段通过。

## 2026-10-01：第 3A 阶段人工验收通过

- 用户明确反馈“我已经验收通过，接下来下一阶段”；主线程结合已完成的第 3A 代码、资源、编译、烘焙和文档静态验收，判定第 3A 阶段通过。
- 本次人工 GamePlayer 验收仅覆盖当前独立网络原型：双客户端 32 敌人生成、最近在线玩家选择与玩家加入/退出后的目标切换、停止距离与无在线玩家时停止追踪、群体近战每次攻击每目标去重及伤害 25、死亡后停止移动和受击，以及双端 HP、死亡状态与存活/死亡统计一致性。
- 更新 Runtime、Combat、Player、Framework 的第 3A 当前验收状态，保留第 2B 通过结论和既往执行记录；本次通过结论不扩展为正式 Map、平台构建、大规模性能或线上联调验收，Entities Graphics 群体表现、敌人反击及寻路避障仍未接入。
- 本次仅同步验收文档；AI 未执行逻辑单元测试、PlayMode、命令行构建或平台发布，未读取图片，未修改代码或资源。

## 2026-10-01：第 3B-1 阶段渲染前置迁移

- 用户确认 3B-1 方案及全项目渲染设置、新渲染资产、两份网络 View 材质引用的修改范围；子 Agent 完成主体迁移，因服务错误中断后由主线程继续核验和文档收尾。
- 通过 Unity Package Manager 安装 URP 17.5.0 及官方依赖；新增 Universal Config/Shader Graph 17.5.0 和 Searcher 4.9.4，既有 Render Pipelines Core 17.5.0 仅依赖深度变化。没有升级其他既有业务包。
- Unity 创建 Assets/Settings/Rendering/CombatPrototypeUniversalRenderer.asset（ForwardPlus=2）与 CombatPrototypeUniversalRenderPipeline.asset（SRP Batcher=true，MSAA=1，Render Scale=1），Graphics 默认及六个 Quality 档全部绑定同一管线，当前档保留 Ultra/5；Player 色彩空间由 Gamma 改为 Linear。
- Unity 创建共用 Assets/Materials/CombatPrototype/CombatPrototypeNetworkView.mat，绑定两份网络 View；Shader 为 Universal Render Pipeline/Lit，白色/不透明/Metallic0/Smoothness0.5/无贴图。两份 Prefab 相对本轮基线各只有一条材质引用变化，组件、网格、层级及原 Ghost 引用保留。
- URP/Shader Graph 初始化自动创建 UniversalRenderPipelineGlobalSettings.asset、空 DefaultVolumeProfile.asset、相应 meta，以及 ProjectSettings/URPProjectSettings.asset、ShaderGraphSettings.asset。没有批量转换工程材质或改写自定义 UI Shader。
- Graphics/Quality 经 Unity 序列化为当前版本，补入引擎默认字段。URP 源码 UniversalRenderPipeline.cs:374、577/578 确认它会同步 MSAA 和光照设置：当前 Ultra antiAliasing 从 2 变为 0，管线 MSAA=1；lightsUseLinearIntensity/ColorTemperature 从 false 变为 true。未额外调节抗锯齿或光照风格，外观差异留待人工回归。
- 保存 ProjectSettings 时，SaveAssetIfDirty 未使设置落盘；随后调用定点 SaveToSerializedFileAndForget，Console 留有一条“objects already persistent”工具诊断。主线程独立确认三份磁盘设置及编辑器实时值一致，未继续调用该内部保存路径；未清空 Console。烘焙后 World.All 的 LINQ 统计表达式曾报告 NoAllocReadOnlyCollection 异常，之前提交的烘焙已成功，后续读取改用直接 Count。以上不记作业务 C# 编译错误。
- 最终 Editor 配置 dd3fd3638f8b63e7392371e56e19b70a 的 SubScene Import Result ID 为 ecfffbba9c56208432e86454a90ebadd，Entities.Hash128 文本为 ceffbfabc9650248238e46459ae0abdd，二者经官方隐式转换对应。产物位于 VirtualArtifacts/Extra/ec/，AssetImportWorkerHW2.log 记录导入成功；Unity 更新对应 sceneWithBuildSettings 缓存。
- 主线程读回最终烘焙产物：临时 World 的 systems=0；Spawner1/count32/columns8/spacing3/origin(0,1,2)；玩家速度5、近战25/2/100/0.18/0.08/0.3；敌人速度2/停止1.5/HP100及伤害缓冲存在。两个 Ghost 的 ClientPrefab 均引用现有 View 和新 URP/Lit 材质，ServerPrefab 均为空。读取后临时 World 释放，未创建或运行游戏系统。
- 主线程确认实时 URP 实例、ForwardPlus、Linear、SRP Batcher（资产和运行值）均有效，六档引用一致、当前档5，Lit Shader 在当前 DX11/支持 Compute Shader 的编辑器报告支持，未处于 PlayMode 或编译中；Console 的 error CS 查询为0。Console 仍保留既有 No SRP 诊断和上述设置保存工具诊断，不记作清零。
- 主线程对照本轮基线确认代码、Scene、Ghost Prefab、构建场景列表、Outline/TMP 资产保持原状；更新 Runtime、Framework 当前事实和验收边界，保留第 3A 人工通过结论。第 3B-1 代码/资源/配置/文档静态验收通过，阶段仍待用户人工 GamePlayer 网络场景及 Map UI 回归；未执行单元测试、PlayMode、命令行构建、发布、图片读取或第 3B-2 实体渲染接入。

## 2026-10-01：第 3B-1 阶段人工验收通过

- 用户明确反馈“我已验证通过，接下来下一阶段”；主线程结合已完成的第 3B-1 包版本、渲染配置、材质绑定、实际 SubScene 烘焙和文档静态验收，判定第 3B-1 阶段通过。
- 本次人工 GamePlayer 验收仅覆盖网络场景外观、亮度与边缘效果、Map UI/TMP/自定义 Outline Shader 兼容，以及切换 URP/Linear 后双端 32 敌人群体行为回归；不扩展为平台构建、大规模性能或线上联调验收。
- 更新 Runtime、Framework 的第 3B-1 当前验收状态，保留第 3A 人工通过结论及既往执行记录；网络玩家和敌人仍使用原 Mono 表现，未接入第 3B-2 敌人实体渲染与死亡隐藏系统。
- 本次仅同步验收文档；AI 未执行逻辑单元测试、PlayMode、命令行构建或平台发布，未读取图片，未修改代码或资源。

## 2026-10-02：第 3B-2 阶段敌人实体渲染与死亡隐藏落地

- 用户确认第 3B-2 方案与本次资源权限，按既有授权由子 Agent 执行、主线程验收。仅调整现有敌人 Ghost 根组件与表现引用，新增客户端显示脚本，重烘焙原 SubScene；保留既有层级、旧 EnemyView、玩家 Mono 表现及全部权威逻辑。
- 新增 `CombatPrototypeEnemyRenderSystem.cs`：ClientSimulation、PresentationSystemGroup、EntitiesGraphicsSystem 之前执行；只读同步 IsDead 并写 MaterialMeshInfo 启用位，使用 IgnoreComponentEnabledState 保留对已隐藏敌人的查询。Unity 自动生成脚本 meta（GUID `3a4e7f2dee6c3864ea5fac39b5e34a27`），已通过 typeof 确认新系统类型进入 Unity 当前程序集。
- 通过 PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset 保存 `CombatPrototypeNetworkEnemy.prefab`：根新增 MeshFilter/MeshRenderer，复制旧 EnemyView 的 Capsule 单 submesh 网格、现有 URP/Lit 材质及 Renderer 配置，原 GhostPresentationGameObjectAuthoring.ClientPrefab 清空，ServerPrefab 仍为空。保存前后文本对照只涉及上述组件与引用；原根 Transform、既有组件 fileID、Ghost 和生命/移动参数保留。
- 使用 Unity 官方 SubScene 缓存脏标记与同步产物接口重烘焙现有 Editor 配置 `dd3fd3638f8b63e7392371e56e19b70a`；Import Result ID 为 `9ada5c2850bcd090d0898eda7a27042d`，Entities.Hash128 文本为 `a9adc58205cb0d090d98e8ada77240d2`，产物位于 `VirtualArtifacts/Extra/9a/`。Unity 更新相应 sceneWithBuildSettings 缓存，未改主场景或 SubScene 层级。
- 通过 EditorEntityScenes.Read 读回真实 `.entities` 与 `.asset` 产物，临时 World 的 systems=0，读取完成后释放。敌人根同时含 EnemyState、启用的 MaterialMeshInfo、RenderMeshArray、RenderBounds/WorldRenderBounds、LocalToWorld，LinkedEntityGroup=1，无 GhostPresentationGameObjectPrefabReference；Capsule 和现有 URP/Lit 材质引用一致。玩家仍指向原 PlayerView，ServerPrefab 为空，玩家根无 MaterialMeshInfo。
- 烘焙中 Spawner=1，count32/columns8/spacing3/origin(0,1,2)，敌人 HP100/速度2/停止距离1.5/伤害事件缓冲仍在。当前 Editor 非 PlayMode、非编译；新增系统类型已加载，Console error 查询为 0，未清空 Console。
- 局部同步 Runtime、Framework、Combat 的当前表现事实、调用链与验收边界，保留第 2B/3A/3B-1 人工通过范围；第 3B-2 人工 GamePlayer 显示、移动、死亡隐藏、重复显示或残影、重新加入与统计一致性尚未验收。AI 未运行逻辑单元测试、PlayMode、命令行构建、平台发布或图片检查，本阶段不形成规模性能结论。
- 主线程独立读回同一最终烘焙产物，确认敌人根渲染组件、Capsule/URP Lit 引用、无 Mono 桥接、LinkedEntityGroup=1 及 Spawner/敌人参数一致；玩家速度5、伤害25/范围2/角度100/前摇0.18/命中0.08/后摇0.3 和原 PlayerView 保留。四份网络 Prefab 缺失脚本均为0，临时 World systems=0、读取前后 World 数均为6，当前场景无脏标记且非 PlayMode/非编译；该核验只确认静态事实，人工验收仍未完成。
- 主线程完成本轮最终范围与文档差异审核，判定第 3B-2 静态验收通过：相对执行前基线，仅敌人 Prefab、三份模块文档与本 ChangeLog 改变，新增显示系统及 Unity 生成的 meta；其余纳入核对的原型代码、场景、玩家/旧 View 资产、材质、渲染/包/构建设置保持原值。最终 Console error 查询为0，未清空 Console。第 3B-2 阶段仍待用户人工 GamePlayer 验收通过，不进入后续阶段。

## 2026-10-02：第 3B-2 阶段人工验收通过

- 用户明确反馈“我已验证通过，接下来下一阶段”；主线程结合上一轮已完成的代码、Prefab、实际 SubScene 烘焙及文档静态验收，判定第 3B-2 阶段通过。
- 人工 GamePlayer 验收仅覆盖当前独立网络原型的双端敌人显示与移动、死亡隐藏无重复显示或残影、重新加入后的死亡状态，以及 HP/存活/死亡统计一致性；不扩展为规模性能、平台构建或线上联调验收。
- 局部更新 Runtime、Framework、Combat 的第 3B-2 当前验收状态，保留第 2B、第 3A 与第 3B-1 的原验收范围及既往变更条目。
- 子 Agent 两次受服务速率限制未能执行本次文档同步，由主线程完成验收记录收尾。本次仅修改验收文档，未改代码或资源，未执行逻辑单元测试、PlayMode、命令行构建、发布或图片读取。

## 2026-10-02：第 4A 阶段网络玩家体力

- 按用户已确认的第 4A 方案和一次性玩家 Prefab 参数授权，由子 Agent 执行；新增 `CombatPrototypePlayerResource.cs`，其整数 CurrentPower/UpperPower 均为 GhostField，Unity 生成对应 meta。
- 原玩家 Authoring/Baker 新增 InitialPower100、UpperPower100、AttackPowerCost10，统一检查初值处于 0 至上限之间、成本非负；近战配置保存整数成本。原服务端近战系统仅在 Ready 收到 Attack 且可支付时扣费一次，然后启动攻击并递增序号；空挥同样扣费。体力不足拒绝，不扣费或递增序号；攻击进行中输入记录拒绝且继续原计时推进。没有自动恢复，重新加入沿原握手生成初始体力 100 的新玩家。
- 原每 2 秒玩家日志增加体力/上限；服务端记录 ReadyAndPowerAvailable、InsufficientPower、AttackInProgress 接受/拒绝原因。未接 UI、正式 Game/PlayerModel/PlayerDataStore、账号、奖励、背包、存档或玩家受击链。
- 通过 Unity PrefabUtility 保存 `CombatPrototypeNetworkPlayer.prefab`，与本轮执行前副本比较仅新增 InitialPower、UpperPower、AttackPowerCost 三个参数；原 5 个组件、单根零子节点、Ghost 参数与玩家表现引用保留。移动速度 5、近战 25/2/100/0.18/0.08/0.3、32 敌人和敌人实体表现不变。
- 初轮编译出现 CS0234：项目 `Code_01.System` 命名空间遮蔽 `System.InvalidOperationException`；改用 `global::System.InvalidOperationException` 后编译完成，类型反射确认新组件、两个 Int32 GhostField 和成本字段已加载。后续 Console error 查询为 0，未清空 Console；仍有此前 Tick Batching 记录及 PEListener 序列化、DOTween 弃用警告，本次未处于 PlayMode，未作性能或旧警告消除结论。
- 现有 SubScene 的 Editor 配置 `dd3fd3638f8b63e7392371e56e19b70a` 定点重新烘焙，Import Result ID 为 `3ec6a8af3ee1c865154747b08f70a395`，Entities.Hash128 文本为 `e36c8afae31e8c565174740bf8073a59`，产物位于 `VirtualArtifacts/Extra/3e/`；Unity 更新对应 sceneWithBuildSettings 缓存，未改主场景/SubScene 文本结构。
- 只读反序列化本轮真实 `.entities` 与 `.asset`：临时 World 的 systems=0，读前/读后 World 数均为6，退出 using 后释放。玩家 CurrentPower100/UpperPower100/成本10、Ready/序号0，原移动/近战参数和 PlayerView 引用正确；Spawner1/count32/columns8/spacing3/origin(0,1,2)，敌人 HP100/速度2/停止1.5/伤害缓冲保留，MaterialMeshInfo 启用、Capsule/URP Lit 引用保留且无敌人 Mono 表现桥接。
- 局部同步 Player、Combat、Runtime、Framework 的当前事实、策略和 UNKNOWN 边界；第 4A 人工 GamePlayer 尚未验收，最终验收由主线程处理。第 2B/3A/3B 既有人工通过范围不扩展到第 4A。AI 未运行逻辑单元测试、PlayMode、命令行构建、发布或图片检查，未提交 Git。

## 2026-10-02：第 4A 主线程静态验收

- 主线程按本轮执行前副本逐项复核新增体力组件、4 份既有脚本与玩家 Prefab：扣费仅位于服务端 Ready 且可支付分支，扣费与 Startup、序号递增同次发生；不足体力拒绝不改状态，忙碌输入日志后仍推进原 PhaseTimer。输入、连接生命周期、伤害查询和正式玩家存档链未改。
- 独立只读反序列化本轮 Import Result ID `3ec6a8af3ee1c865154747b08f70a395` 的实际产物：确认玩家体力 100/100、成本 10、Ready/序号 0，原移动与近战数值、Spawner 和敌人参数、敌人实体渲染引用及玩家 Mono 表现均保持。临时 World 的 systems=0，读取前后 World 数均为 6，未执行游戏系统。
- Unity 已加载两个 Int32 GhostField 及新资源的 GhostComponentSerializer/Snapshot；四份网络 Prefab 全层级 missingScripts 均为 0。验收时 Console error=0，网络主场景 dirty=false，Editor playing=false、compiling=false。
- 本轮 152 个基线文件核对结果：仅授权的 10 个既有文件改变，另新增体力组件脚本与 Unity meta，无文件删除；Scene/SubScene、其余 Prefab、玩家存档、包和项目配置未发生本轮改动。已检查 Git 差异及本轮副本差异，中文正常，未发现空白错误；执行证据保存于本轮临时目录的 main-artifact-review.json、main-baseline-review.json、main-diff-check.json。
- 主线程判定第 4A 实现与静态验收通过；人工 GamePlayer 的双玩家独立体力、双端同步、空挥扣费、忙碌拒绝且计时继续、耗尽拒绝、不自动恢复、重新加入初值及原战斗回归仍为 UNKNOWN，尚不作本阶段最终通过结论。AI 未运行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 2026-10-02：第 4A 阶段人工验收通过

- 用户明确反馈“我已确认验收通过，继续下一阶段”；主线程结合上一轮第 4A 代码、资源、编译、实际烘焙及文档静态验收与本轮用户反馈，判定第 4A 阶段通过。
- 人工 GamePlayer 验收仅覆盖当前独立网络原型：双玩家独立体力及双端同步、成功启动攻击一次扣 10（含空挥）、忙碌输入拒绝且阶段继续推进、10 次成功启动后体力耗尽并拒绝后续攻击且序号不增加、无自动恢复、断线重新加入恢复 100，以及原移动、群体伤害和死亡显示回归。
- 局部更新 Player、Combat、Runtime、Framework 的第 4A 当前验收状态；保留其他阶段原验收范围和既往记录，不扩展为规模性能、平台构建或线上联调验收，不新增下一阶段事实。
- 本次仅同步验收文档，未修改代码或资源，未调用 Unity，AI 未执行逻辑单元测试、PlayMode、命令行构建、平台发布或图片读取。

## 2026-10-02：第 4B 阶段服务端击杀归属与本局奖励

- 按用户已确认方案和子 Agent 执行授权落地；新增 CombatPrototypePlayerReward.cs（整数 Coin/Experience GhostField）、CombatPrototypeKillReward.cs（仅服务端奖励配置/事件）和 CombatPrototypeRewardSystem.cs，meta 由 Unity 生成。四份既有 Networking 脚本仅补充玩家 0/0 烘焙、敌人奖励配置/空缓冲、首次死亡产奖及原日志累计值。
- 原伤害系统按已有事件处理顺序，把首次使敌人 IsDead=1 的致命一击归属写入一次奖励事件；独立服务端奖励系统在伤害系统之后，从 NetworkStreamInGame、Connected、NetworkId、CommandTarget 确认有效接收玩家，每敌人统一提交 1 金币和 10 经验。完整候选值计算后一次写回；必需组件缺失和异常逐事件记录隔离，离线记录 AttackerOffline，不补发，处理后清空缓冲；重入新玩家累计归零。
- 通过 PrefabUtility 保存现有 CombatPrototypeNetworkEnemy.prefab，相对本次 before 副本仅新增 RewardCoin=1、RewardExperience=10；根节点和 7 个既有组件保持。玩家 Prefab、其他 View、Scene/SubScene 结构均未修改；正式 PlayerModel/PlayerDataStore、账号、背包、实体掉落、存档和 UI 未接入。
- Unity 编译完成，反射确认 Coin/Experience 两个 Int32 GhostField、生成的 GhostComponentSerializer/Snapshot 与奖励系统类型已加载，配置/事件的 GhostPrefabType 均为 Server。四份网络 Prefab 全层级 missingScripts 均为 0。
- 通过官方缓存 DirtyFile 与 Synchronous artifact API 定点重烘焙现有 SubScene Editor 配置 dd3fd3638f8b63e7392371e56e19b70a；Import Result ID 为 9fd20ae92b05ea6812d26d7e704c74df，Entities.Hash128 为 f92da09eb250ae86212dd6e707c447fd，产物位于 VirtualArtifacts/Extra/9f/。只读反序列化确认玩家奖励 0/0、敌人奖励 1/10、两类事件缓冲均空；临时 World systems=0，读前/读后 World 数均为 0，未执行游戏系统。
- 实际产物的体力 100/100、成本 10、Ready/序号0、速度5、伤害25/范围2/角100/时序0.18/0.08/0.3 保持；Spawner1/count32/columns8/spacing3/origin(0,1,2)，敌人 HP100/速度2/停止1.5、根实体渲染和玩家原 PlayerView 引用保持。
- Editor 核对时仍在用户原 Map，dirty=false、playing=false、compiling=false。Console 保留 1 条已知正式 PlayerModel 旧存档 Defence8/UpperDefence0 错误，以及 VSCode 包弃用、PEListener 序列化警告；没有本轮编译错误，未清空 Console，未修改这些既有问题。
- 局部同步 Player、Combat、Runtime、Framework 的当前奖励事实、策略与 UNKNOWN 验收边界，保留第 4A 人工通过状态及既往 ChangeLog。第 4B 人工击杀归属、一次奖励、独立累计与同步、离线不补发/重入归零和原战斗回归尚未验收，最终结论由主线程处理；AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查，未提交 Git。
- 本轮 154 文件基线核对仅 10 个既有文件发生内容变化（4 脚本、敌人 Prefab、4 模块文档和本 ChangeLog），无删除；新增 3 脚本及 3 份 Unity meta。授权烘焙另更新 Assets/SceneDependencyCache/3dbc4e16b72a3700e3a52b53099600c0.sceneWithBuildSettings 缓存；基线中的 Map、其余 Prefab/meta 和 ProjectSettings 字节哈希均保持，不把 Git 的换行提示视为实际改动。执行证据保存于本轮临时目录的 agent-bake.json、agent-artifact-read.json、agent-final-editor.json、agent-final-console.json 和 agent-baseline-review.json。

## 2026-10-02：第 4B 主线程静态验收

- 主线程逐项复核本轮副本差异与调用链，确认原伤害顺序保持、首次死亡才生成致命一击奖励事件；独立服务端奖励系统在伤害系统之后按 Connected 连接及 CommandTarget 查找接收玩家。金币/经验完整计算后同次写回，离线或失败事件消费后不重放，没有接入正式玩家存档。
- 独立只读反序列化 Import Result ID `9fd20ae92b05ea6812d26d7e704c74df`，确认玩家奖励 0/0、敌人奖励 1/10、奖励与伤害缓冲均为空；体力、成本、近战、Spawner、敌人参数及两端表现引用保持。临时 World systems=0，读取前后 World 数均为 0，没有执行游戏系统。
- Unity 已加载 Coin/Experience 两个 Int32 GhostField 与奖励 Serializer/Snapshot，四份网络 Prefab 全层级 missingScripts=0；验收时 Map dirty=false、playing=false、compiling=false。Console 仍为 1 条已知 PlayerModel 旧存档 Defence=8/UpperDefence=0 错误，未发现本轮编译错误，未清空或修正既有记录。
- 本轮 154 文件基线中仅授权的 10 个既有文件发生变化，新增 3 个奖励脚本和 3 个 Unity meta，无删除；授权的 SubScene 烘焙缓存另有生成变更。Map/其他场景、玩家和其他 Prefab、既有 meta、包、正式玩家代码与存档、项目设置的基线哈希保持。已核对中文文档、Git 差异与未跟踪脚本的空白检查。
- 主线程判定第 4B 实现与静态验收通过；人工 GamePlayer 的非致命无奖、致命一击归属、每敌人仅奖励一次、多目标分别奖励、独立累计与双端同步、离线不补发/重入归零及原体力战斗回归仍为 UNKNOWN，尚不作第 4B 最终通过结论。第 4A 及以前的通过范围保持。AI 未执行逻辑单元测试、PlayMode、命令行构建、发布或图片检查。

## 2026-10-02：第 4B 阶段人工验收通过

- 用户明确反馈“验收通过，接下来下一阶段”；主线程结合前轮第 4B 代码、资源、编译、实际烘焙及文档静态验收与本轮用户反馈，判定第 4B 阶段通过。
- 人工 GamePlayer 验收仅覆盖当前独立网络原型：非致命不发奖、致命一击归属并获得金币 1/经验 10、每敌人只奖励一次和多目标分别结算、双玩家累计独立与双端同步、离线不补发/重新加入从 0 开始，以及原体力、伤害和死亡显示回归。
- 局部更新 Player、Combat、Runtime、Framework 的第 4B 当前验收状态，保留其他阶段原验收范围和既往记录，不扩展为规模性能、平台构建或线上联调验收。
- 本次由子 Agent 仅同步五份验收文档，保留既有未提交改动；未修改代码或资源，未调用 Unity，AI 未执行逻辑单元测试、PlayMode、命令行构建、平台发布或图片读取。

## 2026-10-02：第 4C 阶段本局背包与击杀物品奖励

- 按已确认方案及资源权限，由子 Agent 落地、主线程独立验收。新增 CombatPrototypeInventoryItem.cs，ItemName 为 FixedString64Bytes、Quantity 为 int，均标注 GhostField；现有玩家 Baker 添加空缓冲，每名玩家独立持有。名称复用 Code_01.Msg.ItemName.小块肉，Unity 生成新脚本 meta。
- 既有 Enemy Authoring、仅服务端奖励配置/事件及伤害产奖点增加物品名和数量；每敌人固定小块肉 1，原金币 1/经验 10 保持。原致命一击归属、每敌人一次、有效在线连接/CommandTarget 校验、离线不补发及事件消费保持。
- 既有奖励系统先取得金币/经验可写引用，checked 准备三项最终数值，新物品先完成容量分配，再无分配写入背包条目和已取得的奖励引用；同名合并，不复制整个背包。缺少依赖、非法奖励、溢出或准备异常拒绝整个事件，后续事件继续。重入新玩家仍为金币/经验 0/0、体力 100/100，背包为空。
- 现有 NetCodeLogSystem 使用只读 BufferLookup，原每 2 秒快照增加 inventoryEntries，并按玩家记录 inventoryItem/quantity；服务端奖励日志补充物品名、当次及累计数量。没有接入正式 PlayerModel/存档/账号、背包 UI、物品使用、世界掉落或拾取链。
- PrefabUtility 保存既有 CombatPrototypeNetworkEnemy.prefab，相对本轮 before 副本仅新增 RewardItemName=小块肉、RewardItemQuantity=1；根节点、7 组件、渲染与 Ghost 绑定保持。玩家 Prefab 与 Scene/SubScene 层级未改，既有所有未提交修改保留。
- 初次 if_dirty 编译未导入新脚本，出现临时未找到 Inventory 类型错误；随后 force/all 刷新导入并完成 Unity 编译，反射确认两字段 GhostField 和生成的 Serializer/Snapshot 已加载，当前 Console 编译错误为 0。Prefab 保存后附带的 World 列表 LINQ 检查因 NoAllocReadOnlyCollection 拒绝 IEnumerable 枚举而失败，保存已完成；随后的只读 foreach 检查与磁盘差异确认参数正确，未重复修改 Prefab。
- 通过官方 DirtyFile 与 Synchronous artifact API 定点重烘焙现有 SubScene Editor 配置 dd3fd3638f8b63e7392371e56e19b70a，Import Result ID 为 4efdea377a2f16d972f77702047f8e33、Entities.Hash128 为 e4dfae73a7f2619d277f772040f7e833；实际读取玩家空背包、奖励 0/0，敌人小块肉 1/金币 1/经验 10，奖励和伤害事件缓冲均为空。
- 实际产物保留玩家体力 100/100、成本 10、Ready/序号0、速度5、伤害25/范围2/角100/时序0.18/0.08/0.3，Spawner1/count32/columns8/spacing3/origin(0,1,2)，敌人 HP100/速度2/停止1.5、根实体渲染和原 PlayerView。检查用临时 World systems=0，读取前后现有 Editor/Streaming World 数均为 6，未创建或运行游戏系统。
- 已局部同步 Inventory、Player、Combat、Runtime、Framework 的本局背包事实、策略与 UNKNOWN 人工验收边界，保持第 4B 和更早人工通过范围。执行证据在本轮临时目录的 agent-types.json、agent-prefab-save.json、agent-prefab-read.json、agent-bake.json、agent-artifact-read.json；Editor 已交回主线程进行独立核对。
- 本轮 166 文件基线中仅 14 个既有文件改变：6 份 Networking 脚本、敌人 Prefab、5 份模块文档、当前 ChangeLog 及授权烘焙缓存；无删除，新增背包脚本及其 Unity meta。Scene/SubScene、玩家和其他 Prefab、既有 meta、正式玩家代码/消息/数据、包与设置的基线哈希保持。中文文档严格 UTF-8 解码通过，Git 差异中文正常，既有与新增文件空白检查无错误。
- 第 4C 人工 GamePlayer 的双玩家空初始库存/独立累计/双端同步、致命一击三项一致、同名合并、非致命无奖、每敌人一次/多目标分别结算、离线不补发/重入清空及原体力战斗回归仍为 UNKNOWN。AI 未执行逻辑单元测试、PlayMode、命令行构建、平台发布或图片检查，未提交 Git。

## 2026-10-02：第 4C 阶段主线程静态验收

- 主线程逐项对比本阶段执行前副本，确认空背包初始化、同名数量合并、物品奖励事件传递及客户端只读日志符合已确认方案。三项奖励在 checked 候选值和新条目容量准备后提交；写入背包后直接写入预先取得的金币/经验引用，无中途分配或组件查找。原致命归属、在线校验、事件一次消费与离线不补发分支保持。
- 主线程独立读取新烘焙产物 `4efdea377a2f16d972f77702047f8e33`，确认玩家背包长度 0、金币/经验 0/0，敌人奖励金币 1/经验 10/小块肉 1，两类事件缓冲为空；原玩家体力、移动、近战、32 敌人生成参数及两端表现引用保持。检查 World systems=0，读取前后 World 数量为 6→6，临时 World 已释放。
- 背包两个 GhostField 和生成的 Serializer/Snapshot 已加载；敌人 Prefab 仍为 7 组件、0 子节点，四份网络 Prefab 均无缺失脚本。核对时网络场景 dirty=false、playing=false、compiling=false；Console 为 0 条错误，保留 5 条原 Tick Batching 警告、PEListener/DOTween 编译警告与 1 条 MCP WebSocket 警告，未清空或处理无关警告。
- 主线程独立比较 166 文件基线：14 个授权既有文件变化，仅新增背包脚本及 Unity meta，无删除；场景、其他 Prefab、既有 meta、正式玩家/消息/数据、包与设置保持本阶段执行前哈希。六份文档的 UTF-8 严格解码、中文差异和 Git 空白检查通过，既有未提交改动保留。
- 主线程判定第 4C 实现与静态验收通过。人工 GamePlayer 的双玩家空初始库存、独立累计与双端同步、非致命无奖/致命三项一致、同名合并、每敌人一次/多目标分别结算、离线不补发/重入清空及原体力战斗回归仍为 UNKNOWN，尚不作第 4C 最终通过结论。AI 未运行逻辑单元测试、PlayMode、命令行构建、平台发布或图片检查。

## 2026-10-02：第 4C 阶段人工验收通过

- 用户明确反馈“已验证通过，接下来下一阶段”；主线程结合上一轮第 4C 代码、资源、编译、实际烘焙及文档静态验收与本轮用户反馈，判定第 4C 阶段通过。
- 人工 GamePlayer 验收仅覆盖当前独立网络原型：双玩家空初始库存、独立累计与双端同步、非致命无奖、致命一击金币 1/经验 10/小块肉 1 三项奖励一致、同名合并、每敌人一次和多目标分别结算、离线不补发/重新加入清空，以及原体力、移动、伤害和死亡显示回归。
- 局部更新 Inventory、Player、Combat、Runtime、Framework 的第 4C 当前验收状态，保留第 4B 及更早通过范围和既往 ChangeLog 中的当时 UNKNOWN 记录，不扩展为规模性能、平台构建或线上联调验收。
- 本次由子 Agent 仅同步六份验收文档，保留既有未提交改动；未修改代码或资源，未调用 Unity，AI 未执行逻辑单元测试、PlayMode、命令行构建、平台发布或图片读取。

## 2026-10-02：第 4D 阶段开发固定 ID 与独立网络存档

- 用户选择“开发用固定玩家 ID”并明确确认第 4D 执行方案；本阶段由主线程落地和静态验收，保留此前第 4B/4C 未提交改动与人工通过范围。
- 新增 CombatPrototypeDevelopmentIdentity、CombatPrototypePlayerIdentity、CombatPrototypePlayerSaveData、CombatPrototypePlayerSaveStore 四份脚本及 Unity 正常导入生成的 meta，新增 UserSettings/CombatPrototypeDevelopmentIdentity.json；仅修改既有 NetCodeLifecycle、RewardSystem、NetCodeLogSystem 三份脚本。开发配置明确为 ClientWorld → player-a；进程参数 -combatPrototypePlayerId 可显式覆盖，缺失或非法身份拒绝加入，无随机身份或 NetworkId 存档键。
- GoInGame RPC 携带 PlayerId，服务端在原握手入口验证固定 ID 与在线占用，并完整读取、校验存档后排入玩家生成。服务端运行玩家添加身份组件，未修改玩家 Baker 或 Ghost 字段。相同更新内的身份竞争与重复连接请求均隔离；重复在线 ID 拒绝新连接，原玩家保留。仅不存在文件时采用金币/经验 0/0 与空库存，恢复或新加入均沿烘焙体力 100/100 与原连接、输入、位置、LinkedEntityGroup 绑定。
- 服务端存档为 Application.persistentDataPath/CombatPrototype/Players/<PlayerId>.json，Version=1，保存 PlayerId、Coin、Experience 与 Items 的 ItemName/Quantity。完整校验版本、身份匹配、字段类型与数量、非负金币/经验、正库存数量和唯一非空名称；名称在读取边界显式校验严格 UTF-8 字节数不超过 FixedString64Bytes 容量，不依赖 Unity 调试检查。坏档或读取异常拒绝当前玩家，保留原文件并记录身份、路径与原异常。
- 奖励系统沿原致命一击归属与在线 CommandTarget 查找接收玩家，先完成三项 checked 候选值、所需背包容量及序列化数据，再写同目录 .json.tmp、Flush(true) 并替换正式文件；保存成功后才同次写回背包与金币/经验。保存失败不发本次三项奖励，失败及离线事件消费后不重放，后续独立事件继续。成功日志增加身份和路径，原每 2 秒状态日志增加服务端固定 ID 映射；客户端仍只接收 Ghost 状态。
- 普通 Unity Editor 导入编译完成，九个相关类型及包含 PlayerId 的 RPC Serializer/请求系统已加载，四份网络 Prefab 无缺失脚本，组件、GUID 与层级保持。最终只读核对为 CombatPrototypeNetCode 场景 dirty=false、playing=false、compiling=false、updating=false，现有 Editor/Loading World 共 6 个；实际 persistentDataPath 为 C:/Users/91611/AppData/LocalLow/DefaultCompany/Code_01。当前 Console 为 0 条错误，保留 VSCode 包弃用、Input Manager 弃用及既有 PEListener UAC1001 三条警告，未清空或修正无关记录。
- 局部同步 AI_Understanding、Player、Combat、Inventory、Runtime、Framework、DataResources 七份当前文档及本 ChangeLog，修正原型未持久化与重入归零的旧当前规则，保留既往阶段验收的历史范围。严格 UTF-8 解码、中文差异、本地链接与 Git 空白检查通过；局部文档审计仍报告 Runtime 31.8 KiB 超过默认预算和 DataResources 第 112 行既有长段落，本阶段未扩展为文档拆分。
- 本阶段 231 文件基线中仅 11 个既有文件变化（三份脚本、七份当前文档及本 ChangeLog），新增九个文件（四份脚本、四份 meta 与开发配置），无删除。基线内的 Scene/SubScene、Prefab、既有 meta、正式玩家/消息/数据脚本、包、项目/用户设置及既有烘焙缓存保持执行前哈希；未修改 Animator 或正式存档。
- 主线程判定第 4D 实现与静态验收通过；人工 GamePlayer 的双 ID 独立保存与同步、同 ID 重连/服务端重启恢复、体力重置、重复 ID/坏档隔离、保存失败三项均不发且旧档保留及原战斗回归仍为 UNKNOWN，完整清单维护于 Runtime。AI 未执行逻辑单元测试、游戏系统、PlayMode、命令行构建、平台发布或图片读取，未提交 Git；本阶段尚不作人工最终通过结论。

## 2026-10-02：第 4D 阶段人工验收通过

- 用户明确反馈“验收已通过，接下来下一阶段”；主线程结合上一轮第 4D 代码、准入与奖励调用链、Unity 编译、资源边界及文档静态验收和本轮用户反馈，判定第 4D 阶段通过。
- 人工 GamePlayer 通过范围仅覆盖当前独立网络原型：不同固定 ID 的玩家与存档独立、三项奖励一起到账且双端一致、同 ID 重连/服务端重启恢复金币/经验/背包且体力重置为 100、重复 ID 和缺失/非法配置拒绝、坏档/版本或库存错误保留原文件并隔离、保存失败三项均不发且旧档保留、失败事件不补发且后续独立结算继续，以及原移动、体力、伤害和死亡显示回归。
- 局部更新 AI_Understanding、Player、Combat、Inventory、Runtime、Framework、DataResources 的第 4D 当前验收状态及本 ChangeLog，共八份文档；保留既往第 4B/4C 当时归零与空背包的验收范围、历史 UNKNOWN 记录，以及正式认证、跨服务器存档、迁移、规模性能、平台构建和线上联调的未验收边界。
- 本轮仅由主线程同步验收文档，未修改代码、配置或资源，未调用 Unity；AI 未运行逻辑单元测试、GamePlayer/PlayMode、构建、发布或图片读取，未提交 Git。严格 UTF-8 解码、中文差异与 Git 空白检查通过；本轮 238 个非视觉文件基线中仅上述八份文档变化，无新增或删除，代码与资源保持执行前哈希。

## 2026-10-02：第 5A 阶段基线观察准备

- 用户选择现有 2 玩家/32 敌人规模并确认第 5A 方案；本轮由主线程按方案执行，保留第 4D 及以前的人工通过范围和全部既有未提交改动。
- CombatPrototypePlayerSaveStore.cs 仅增加 Unity.Profiling 引用、两个静态 ProfilerMarker 及 Load/SavePrepared 各一处 Auto 作用域，共五行。Load 覆盖整个读取/校验调用，SavePrepared 覆盖序列化、临时文件写入、Flush(true) 和替换；候选投影、ECS 提交与调用方日志不计入保存标记。移除这五行后与执行前副本完全一致。
- 普通 Unity Editor 脚本刷新编译完成，已加载并注册 CombatPrototype.PlayerSave.Load 与 CombatPrototype.PlayerSave.SavePrepared，类别 Scripts、单位 TimeNanoseconds；编译核对时 Console 错误为 0，网络场景 dirty=false、playing=false、compiling=false、updating=false。元数据核对没有调用 Load/SavePrepared 业务方法。
- 通过当前 Unity MCP/Profiler API 核实环境和计数器接口，在项目外临时目录准备有时限的原生记录采集助手；内存编译后因未进入 PlayMode 返回 awaiting-human-GamePlayer，状态 not-armed，没有开启 Profiler 录制或注册采集回调。已确认每组预热 10 秒、正式 30 秒，运行指标尚无样本。
- 新增 Performance.md 作为唯一性能细节入口，局部同步 AI_Understanding、Runtime、Framework、DataResources 和本 ChangeLog。环境硬件信息仅来自当前 Editor 的 SystemInfo；运行 Tick、帧耗时、内存/GC、RTT/快照、预测误差、存档耗时及本轮奖励/存档/重连回归均保持 UNKNOWN。
- 用户反馈双客户端“尚未启动”，第 5A 当前仅完成观察准备与静态落地，尚不作性能或人工最终通过结论。AI 未启动 GamePlayer/PlayMode、注入游戏输入、运行游戏系统、逻辑单元测试、命令行构建、平台发布或图片读取，未提交 Git。
- 主线程核对本轮 255 文件基线，仅存档脚本和上述五份既有文档发生变化，新增 Performance.md，无删除；基线内 Scene/SubScene、Prefab、meta、配置、包、设置及既有烘焙缓存哈希保持。中文 UTF-8、差异、链接与空白检查通过；局部审计仅保留 Runtime 原有体量超限和 DataResources 第 112 行既有长段落，本轮未扩大为文档拆分。Profiler 仍关闭且配置保持，当前 Console 错误为 0。

## 2026-10-02：第 5A 运行验收暂缓

- 用户明确要求“暂时不运行验收，继续下一阶段”，主线程保留第 5A 静态观察准备完成的状态，不运行 GamePlayer/Profiler 采集，不将该阶段标记为最终通过；全部性能数值与本轮运行回归继续为 UNKNOWN。
- 局部同步 AI_Understanding、Performance、Runtime 和本 ChangeLog 的当前验收状态，保留既有采集口径、存档标记和第 4D 及以前的通过范围。
- 原任务路线止于第 5 阶段规模验证。用户已选择“敌人反击与玩家受伤”作为下一阶段方向；本轮仅核对文档、现有敌人目标、玩家攻击/敌人伤害及 Prefab 文本，具体执行方案尚未确认，未开展该方向的代码或资源修改。

## 2026-10-02：第 6A 阶段敌人反击与玩家受伤

- 用户明确确认第 6A 执行方案及本次资源授权，主线程按确认方案执行；人工运行验收继续保留待完成状态，第 5A 暂缓与以前阶段的人工通过范围保持。
- 在原 Networking 目录新增 CombatPrototypePlayerHealth、CombatPrototypePlayerDamageEvent、CombatPrototypeEnemyAttack、CombatPrototypeEnemyAttackSystem、CombatPrototypePlayerDamageSystem 五份脚本及 Unity 自动生成的 meta；仅修改原玩家/敌人 Authoring、玩家/敌人 Movement、MeleeServer 与 NetCodeLog 六份脚本。
- 玩家生命与上限为 100/100，受击序号/死亡标记初始为 0，四项生命状态经 Ghost 同步。服务端玩家伤害缓冲保存来源敌人、攻击序号及伤害，统一扣血至不小于 0、递增有效受击序号、归零标记死亡，死亡后剩余事件也消费清空。死亡玩家停止移动/旋转并取消未完成近战阶段，新攻击输入不扣体力、不增序号、不产伤害；实体和原显示保留，未接入正式 PlayerModel 或新表现系统。
- 敌人攻击配置为伤害 10、X/Z 距离 1.75、前摇 0.5 秒、后摇 1 秒，复用最近在线存活目标并锁定实体与 NetworkId。前摇结束重新核对在线、存活与距离，仅命中一次；离线/死亡/出范围空击，命中或失败均进入后摇，不重试同次挥击；前后摇停止移动/旋转。目标依赖缺失或事件写入异常记录具体条目并隔离，其他敌人继续。
- 原玩家攻击、敌人伤害、击杀奖励/存档先执行，再执行敌人攻击与玩家伤害；同 tick 被击杀的敌人清空攻击阶段与锁定目标，不再反击。32 敌人伤害各自叠加，没有无敌时间、防御、治疗、击退或动画接入。生命不入存档，再次加入从烘焙生命 100/100 开始，原金币/经验/背包固定 ID 恢复不变；存档与奖励脚本未修改。
- 通过 Unity Prefab API 各保存现有玩家与敌人 Prefab 一次，仅新增确认的两个生命参数和四个敌人攻击参数；根组件分别仍为 5/7 个、子节点均为 0，原挂载与资源引用保持。未改变 Scene/SubScene、Animator、View、既有 meta/GUID 或构建配置；新脚本 meta 正常生成，原 Editor 场景依赖缓存经定点重新烘焙更新。
- 正常 Unity Editor 导入编译完成，新增系统及生命 Ghost Serializer/Snapshot 已加载，Snapshot 四字段齐全。实际 Editor SubScene 产物为 VirtualArtifacts/Extra/ca/cadbac4f6c98d6a686179bbfbcfd3fbe.0.entities，只读核对生命 100/100/受击 0/未死亡、空玩家伤害缓冲及敌人攻击参数/Ready 初态；原体力、近战、0/0 奖励与空库存、32 敌人网格、敌人生命/移动、奖励和根实体渲染组件保持。临时读取 World 的 systems=0，读取后释放，原 Editor/Loading World 前后均为 6，未运行游戏系统。
- 原每 2 秒玩家状态日志增加 HP/上限、受击序号与死亡标记；服务端日志补充反击开始、空击/事件生成、玩家扣血及死亡输入拒绝。Console 错误为 0，保留既有包/Input Manager 弃用及 PEListener、DOTween 警告，未清空或处理无关警告。
- 局部同步 AI_Understanding、Player、Combat、Runtime、Framework、DataResources、Performance 七份当前文档及本 ChangeLog；明确当前生命/反击职责、人工待验收清单、生命不入档和第 6A 样本版本/死亡负载边界，没有填写运行性能数值。
- 主线程比较本轮 260 个非视觉文件基线，仅 17 个授权既有文件变化（六份脚本、两份 Prefab、一个原 Editor 烘焙缓存、七份当前文档及本 ChangeLog），新增五份脚本和五份 Unity meta，无删除或范围外变化；原场景及既有未提交改动保留。中文严格 UTF-8、八份文档本地链接和本轮代码/文档空白检查通过；Runtime 既有体量超限与 DataResources 既有长段落保持，本轮未进行文档拆分。
- 主线程判定第 6A 实现与静态验收通过，人工 GamePlayer 与最终验收为 UNKNOWN。AI 未执行逻辑单元测试、GamePlayer/PlayMode、命令行构建、发布或图片检查，未提交 Git；完整人工清单保留在 Runtime。

## 2026-10-02：第 6A 人工验收通过与第 5A 恢复

- 用户明确反馈“验收已通过，继续下一阶段”；主线程结合上一轮第 6A 代码、权限边界、编译、Ghost 与实际烘焙静态验收和本轮用户反馈，判定第 6A 阶段通过。
- 人工 GamePlayer 通过范围仅覆盖当前独立网络原型的双玩家生命独立与同步、敌人前摇单次命中及前后摇停动、锁定目标离线/死亡/出范围空击、同 tick 被击杀敌人不再反击、事件消费不重复扣血、玩家死亡后停止移动/攻击、重新加入生命重置且固定 ID 奖励/背包恢复，以及原体力、伤害、敌人死亡显示与奖励存档/失败隔离回归。
- 用户随后明确选择“恢复第 5A：规模性能验证”；原 2 玩家/32 敌人、预热 10 秒/采集 30 秒及三组负载口径保持，以第 6A 已验收版本为当前基线。第 6A 及以前的人工通过范围保持，不等同于规模性能通过。
- 局部同步 AI_Understanding、Player、Combat、Runtime、Framework、Performance 六份当前文档及本 ChangeLog，更新第 6A 当前验收状态和第 5A 恢复状态；代码、资源与历史记录未修改。
- 当前 Unity Editor 未进入 PlayMode，尚无运行采样；已请求人工按既有方式启动双客户端，性能数值和第 5A 最终验收保持 UNKNOWN。AI 本轮未启动 GamePlayer、注入输入、运行游戏系统、逻辑单元测试、构建、发布或图片读取，未提交 Git。

## 2026-10-02：第 5A 按用户要求改为静态检测

- 用户回复“不启动，静态检测”，本轮停止等待双客户端启动，保持第 6A 已验收基线和原 2 玩家/32 敌人、预热 10 秒/采集 30 秒的运行口径，仅执行只读静态核对；没有运行采样或性能通过结论。
- 核对现有 SubScene/Prefab 序列化配置、玩家死亡门槛、敌人反击参数及准入/奖励存档调用；只读查询已加载的八个系统 UpdateAfter，确认移动、空间索引、近战、敌人伤害、奖励保存、反击与玩家伤害的既有顺序，两个存档标记仍为 Scripts/TimeNanoseconds。
- 源码核对记录同步序列化/写盘与 Flush(true)、候选数组/字符串、原生临时容器、空间表复用和原日志等成本入口，具体耗时与 GC 数值保持 UNKNOWN。没有调整业务参数、Tick、日志策略或存档实现。
- 核对项目外临时采集助手：原生记录器值/Count/单位/窗口清理已有实现，但实际 Tick、World 归属、人数/生命/事件/重连状态、按连接 RTT 和预测误差名称/值对应尚未覆盖；本轮没有修改或启动助手。
- Editor 只读状态为未进入 PlayMode、未处于编译中，6 个 World 仅为 Editor/Loading，Profiler 关闭，采集会话未启动且无停止回调。Console 返回 0 条 Error、5 条既有 Tick Batching Warning；警告短窗口值不写作本轮性能样本，频率、负载与成因保持 UNKNOWN，未清空或静默关闭警告。
- 局部同步 AI_Understanding、Runtime、Framework、Performance 当前状态及本 ChangeLog；连同本轮第 6A 人工通过同步，共涉及七份 Doc 文档。代码、资源与采集助手保持本轮开始时内容；未执行 GamePlayer/PlayMode、游戏系统、逻辑单元测试、命令行构建、发布或图片读取，未提交 Git。
- 主线程比较本轮 270 个非视觉文件基线，仅上述七份 Doc 文档变化，无新增、删除或范围外变化；严格 UTF-8 读写、git diff 与空白检查、七份文档的局部链接审计通过。Runtime 既有体量超过 24 KiB，本轮未拆分或进行无关文档整理。
- 主线程判定本轮第 5A 静态检测完成；完整运行采集覆盖尚未就绪，运行性能数值与最终性能验收保持 UNKNOWN，第 6A 人工通过范围保持。

## 2026-10-02：第 5B 性能采集助手补齐与静态验收

- 用户选择补齐性能采集助手并明确确认调整方案；本轮由主线程执行，保持 GamePlayer、Profiler 采样及运行回调关闭，没有创建子 Agent。
- 项目外原 capture-start.cs.txt/capture-status.cs.txt 扩展为 5B-1；新增 capture-world-read.cs.txt 与 capture-result-review.cs.txt，分别承担 World 只读查询/清理与原生样本检查/统计，启动模板以两个 include 注释组合片段。默认 armCapture=false，状态查询独立且不启停采集。
- 补齐现有 World 身份与显式 Tick/NetworkTime、连接 RTT、预测误差名称/值对应、玩家生命/体力、敌人数量/攻击阶段、Frame Timing、当前窗口日志及连接变化的读取代码。World 状态观察周期 0.1 秒，原生指标仍按帧保留；缺失组件不动态补齐或启用统计系统，字段与指标保持来源/单位/不可用说明。
- 原 10 秒预热/30 秒目标采集与 32768 上限保持；补齐完整标记名称和 World/进程归属、Int64/Double 解读、有效样本统计、预热帧排除/去重、no-events 与调用/成功数量区分，以及中断/失败/溢出/回绕、记录器/查询/回调释放路径。观察与序列化成本明确记录，未修改游戏 Tick、日志、存档、Ghost 或资源。
- 四份文本组合后的完整方法体与状态入口由 C#6 CodeDom 内存编译成功；默认启动结果 static-compiled-not-armed、armed=false、isPlaying=false、profilerEnabled=false，无旧会话或停止回调，状态查询为 not-armed。只读核对仍为 6 个原 Editor/Loading World，两个存档标记与 Frame Timing 能力保留；没有创建查询、记录器、游戏 World 或运行回调。
- Console 0 条 Error、5 条原有 Tick Batching Warning；Profiler.enabled=false、recording=false，原区域设置保持，未清空或关闭警告。AI 未执行逻辑单元测试、GamePlayer/PlayMode、游戏系统、命令行构建、发布或图片读取。
- 同步 AI_Understanding、Runtime、Performance 当前事实与本 ChangeLog；主线程静态验收仅覆盖实现、API、默认关闭和修改边界，运行数据有效性、释放的实际效果、观察开销及第 5A 最终性能验收保持 UNKNOWN。
- 主线程对比 270 个项目非视觉文件基线，仅上述四份 Doc 文档变化，无项目文件新增、删除或范围外改动；项目外助手仅两份原文本修改、两份辅助文本新增，原编译证据等其他文件保持。严格 UTF-8、git diff/空白与局部文档链接审计通过，导航 7738 字节；Runtime 原有体量超限保留，未拆分或整理无关文档，未提交 Git。

## 2026-10-02：第 6B 局内手动复活静态落地

- 用户确认第 6B 执行方案后由主线程完成，未创建子Agent。
- 现有 CombatPrototypePlayerInput 增加 InputEvent Respawn，GhostOwnerIsLocal 的 R 键当帧事件沿原 NetCode 命令链发送。
- 新增 CombatPrototypePlayerRespawnSystem，限定服务端预测模拟组、玩家伤害结算之后；沿已入游戏的在线连接及当前 Simulate 玩家确认归属和死亡，存活请求拒绝。
- 服务端沿用玩家实体，恢复 CurrentHealth=MaxHealth、CurrentPower=UpperPower、IsDead=0；按原加入公式恢复位置，保留旋转/缩放、受击/攻击序号、金币/经验/库存和连接身份，不新增存档读写。
- 清空玩家伤害与近战阶段计时，解除该玩家对应敌人目标/攻击锁定；旧 Startup 取消并进入完整 Recovery，旧 Recovery 剩余计时保留，不影响其他玩家锁定。
- 新脚本由 Unity 定点导入并自动生成唯一新 meta；编译后反射确认新系统、输入字段、生成的辅助/命令类型及九项系统特性链。最后脚本修改后的 Assembly-CSharp 已更新，Console 为 0 条 Error、6 条 Warning（5 条旧 Tick Batching 和既有 PEListener 的 UAC1001），未修改告警来源脚本。
- 同步 Player、Combat、Runtime、AI_Understanding 的当前事实、策略和验收边界；既有第 6A 人工通过范围不扩展为第 6B 运行通过。
- 本轮保持 GamePlayer、PlayMode、Profiler、采集会话与停止回调关闭；未运行游戏系统、逻辑单元测试、构建、发布、图片读取或性能采样。主线程静态验收通过，双端复活、旧攻击取消及原链回归的人工结果仍为 UNKNOWN。
- 主线程复核 270 项非视觉项目基线，当前 272 项，仅现有输入脚本与五份 Doc 文档修改、新脚本及其 meta 新增，无删除或范围外变化；严格 UTF-8、git diff/空白和四份受影响事实文档的链接检查通过，导航 7982 字节、Combat 24250 字节。Runtime 原有超限边界保留，未拆分或整理无关文档，未提交 Git。

## 2026-10-02：第 6B 用户人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”；主线程结合第 6B 已有静态核对与该反馈，判定第 6B 阶段通过。
- 本阶段通过范围覆盖双玩家手动复活与同步、满生命/满体力和原加入位置、存活拒绝/重复输入、旧攻击及锁定清理、序号与奖励库存保持及原操作/存档回归；没有把反馈扩展为性能、平台或线上联调通过。
- 同步 AI_Understanding、Player、Combat、Runtime 的验收状态，并将第 6B 静态落地时的环境证据与用户人工通过来源区分记录；代码与资源未修改，第 5A/5B 尚无性能运行样本的边界保持。
- 本轮仅读取文件并同步文档，未启动 GamePlayer、调用游戏系统、执行逻辑单元测试/构建/发布/性能采样或读取图片。

## 2026-10-02：第 7A 网络物品使用与体力恢复静态落地

- 用户确认第 7A 执行方案后由主线程完成，未创建子Agent；继续采用编译与静态核对，未启动 GamePlayer。
- 现有 CombatPrototypePlayerInput 增加 UseItem，E 键按下当帧只写本地拥有者事件，沿原 NetCode 输入命令发送。新增独立 CombatPrototypeItemUseSystem，仅在服务端预测模拟组、敌人空间索引之后且玩家近战之前更新。
- 复用当前连接、CommandTarget、GhostOwner、Simulate、身份、生命、近战、库存与体力；死亡、非 Ready、满体力或小块肉不足时拒绝。接受时消耗 1 份小块肉，恢复最多 30 点并截到 UpperPower，同 tick 的后续近战可使用恢复后的体力。
- 现有存储类增加 PrepareItemConsumption，数量归零时从候选及 ECS 缓冲移除，其他库存顺序、金币/经验保持；先 SavePrepared 成功返回，再用已取得的引用同次提交库存与体力。保存失败时该使用操作两项均保持，旧正式档保留，独立玩家失败隔离并记录阶段/原异常；没有自动重试。
- 原 Load、PrepareReward、SavePrepared、JSON v1 字段及严格校验保持，体力仍不持久化；重新加入恢复消费后的库存，生命/体力沿烘焙初值，原奖励与第 6B 复活规则保持。
- 新脚本由 Unity 定点导入，唯一新 meta 自动生成，GUID 为 a227d368e6f698741adef1dd18595c6a；没有修改既有 Scene/SubScene、Prefab、Animator、Baker 组件挂载、旧 meta 或项目设置。
- 正常 Unity 编译已加载新系统和四项输入字段；只读反射确认生成辅助/Serializer/Send/Receive/Compare 类型，UseItem 在生成的事件增减、序列化/反序列化及变化掩码方法中被引用，恢复常量为 30。新系统含 ISystemCompilerGenerated，十项系统顺序特性与源码一致；未调用游戏或存档业务方法。
- Console 查询为 0 条 Error、1 条 MCP WebSocket 未初始化 Warning；Editor 未进入 PlayMode，Profiler/录制关闭，采集会话和停止回调为空，仅 6 个 Editor/Loading World，未清空 Console。
- 增量同步 AI_Understanding、Inventory、Player、Combat、DataResources、Framework、Runtime、Performance 与本 ChangeLog。物品规则归 Inventory，Runtime 保留人工清单；性能文档同步新的保存调用与计时范围，项目外采集助手保持关闭且未修改。
- 主线程源码、编译元数据、UTF-8、git diff/空白和八份事实文档局部链接检查完成；导航 8079 字节，Player 24559、Framework 24548、Combat 24324 字节。Runtime 既有大文档仅同步第 7A 内容，未拆分或整理无关部分；DataResources 既有长行保持。
- 人工 GamePlayer 的消费/恢复、拒绝、同 tick 输入、双玩家同步、保存失败与重连恢复仍为 UNKNOWN；第 6A/6B 已有人工通过范围保持，第 5A/5B 运行性能仍未验收。AI 未新增或运行逻辑单元测试、PlayMode、游戏系统、命令行构建、发布、性能采样或图片读取，未提交 Git。
- 主线程最终复核 272 项非视觉项目基线，当前 274 项，仅两份原脚本及九份 Doc 文档修改、新系统及其 meta 新增，无删除或范围外变化；判定第 7A 实现与静态验收通过，人工运行验收保持 UNKNOWN。

## 2026-10-02：第 7A 用户人工验收通过

- 用户明确反馈“验收通过，继续下一阶段”；主线程结合第 7A 已有静态核对与该反馈，判定第 7A 阶段通过。
- 人工通过范围沿原清单，覆盖小块肉消费/体力恢复、归零移除、拒绝条件及同 tick 输入、双玩家独立与双端同步、保存失败保持、固定 ID 重连/重启恢复及原操作/奖励/复活回归；没有扩展为规模性能、平台或线上联调通过。
- 增量同步 AI_Understanding、Inventory、Player、Runtime、DataResources、Framework、Performance 的验收状态及本 ChangeLog；Runtime 将原静态环境证据与用户人工通过来源区分，第 5A/5B 无运行采样的边界保持。
- 用户在下一阶段方向选择中回复“暂不执行”；本轮未启动后续开发，没有新增阶段实现或修改业务方案。
- 本轮仅进行文件读取与文档同步，代码、资源、项目设置及采集助手保持本轮开始时内容；未调用 Unity 工具、启动 GamePlayer、运行逻辑单元测试/构建/发布/性能采样或读取图片。
- 主线程复核本轮 274 项非视觉文件基线，仅上述八份 Doc 文档变化，无新增、删除或范围外变化；严格 UTF-8、git diff/空白及七份事实文档局部链接检查通过。导航 8082 字节，Runtime 既有体量超限与 DataResources 既有长行保留，未进行无关拆分或整理，未提交 Git。

## 2026-10-02：Editor 启动配置与单机 IPC 静态落地

- 用户确认启动配置方案后由主线程执行，未创建子Agent。
- 新增 StartupSettings、StartupSettingsStore、StartupDriverConstructor 和 StartupSettingsWindow 四份脚本；现有 NetCodeBootstrap 接入 Editor 配置、明确 World 创建/监听/连接和本次失败清理，非 Editor 保留原分支。
- 新增 Tools/CombatPrototype/启动配置 菜单，提供单机/联机、Host/Client/Server、客户端 IPv4、联机端口、后台运行、保存校验和重读；显示现有身份来源并定位原参数资源，PlayMode 期间锁定启动编辑并显示成功启动快照。
- 创建本地 UserSettings/CombatPrototypeStartupSettings.json，实际值为 Version=1、Online/Host、127.0.0.1、Port=7979、RunInBackground=true；.gitignore 仅增加该文件的忽略项。
- 单机明确只注册双端 IPC，固定通道 7979；联机沿用服务端官方驱动及原战斗、身份、奖励和存档链。新入口将 AutoConnectPort 设为 0，不由 PlayMode Tools 创建角色或覆盖端点，不自动创建 ThinClient。
- 配置采用严格 UTF-8、六字段/版本/类型/枚举/IP/端口/后台运行校验，保存采用同目录临时文件、Flush(true) 和替换；缺失/坏配置显式失败，不补默认配置。
- 四份新脚本由 Unity 定点导入并生成新 meta；Unity 编译和菜单注册已核对，无新增脚本编译错误。Console 保留既有连接、UnityConnect、序列化引用记录及 PEListener/DOTween 警告，未清空 Console 或修改告警来源。
- 原网络主场景/SubScene、玩家/敌人 Prefab、开发身份文件及任务开始已有场景/渲染配置/EditorUserSettings 改动的内容散列保持；原 Map.meta 的删除状态保持，未修改 Scene/Prefab/Animator、旧 meta、构建设置或存档。
- 局部同步 Runtime、Framework、DataResources 当前事实、策略和待验收项；本条只记录实际改动。AI 未启动 GamePlayer/PlayMode、执行逻辑单元测试/构建/发布/性能采样或读取图片，人工运行验收和最终通过结论保持 UNKNOWN。
- 主线程静态验收通过：最终 Assembly-CSharp/Editor 程序集更新时间晚于五份脚本，新增配置属性不可变、Editor 启动入口和窗口类型已加载；窗口实际打开并读取 Online/Host、127.0.0.1、7979、后台运行 true、现有 player-a，无配置/身份读取错误，isPlaying=false。
- 复核 11 份本轮文本的严格 UTF-8、三份模块文档链接和四个新脚本 GUID 唯一性；git diff/空白检查通过，九项已有资源/配置基线散列保持。静态通过不等同人工 GamePlayer 或最终运行验收通过，未提交 Git。

## 2026-10-03：末世玄幻战斗模式 AI 出图模板文档落地

- 用户确认模板草案后由主线程执行，新增 Doc/CombatImagePromptTemplate.md；模板版本为 1.0，采用末世玄幻、荒野求生、风格化手绘质感和清晰战斗轮廓的已确认出图方向。
- 文档包含统一美术方向、必填信息、通用提示词、六类图片补充要求、三个完整示例及批量生成与验收标准；示例涵盖战斗场景、敌人前摇设定和武器图标。
- Doc/AI_Understanding.md 的按任务阅读表增加模板入口；未改写既有模块文档。正式镜头、分辨率、角色比例、资源映射和生产规格保持 UNKNOWN，示例名称与视觉设计不作为现有功能或资源绑定事实。
- 本轮仅修改上述模板、导航和本 ChangeLog，未创建子Agent，未修改代码、Scene、Prefab、Animator、meta、包或构建设置；未生成或读取图片，未运行逻辑单元测试、GamePlayer PlayMode、构建或发布。

## 2026-10-03：网络战斗原型角色视野与本地跟随相机落地

- 用户确认调整方案及 Main Camera 组件挂载、新脚本 meta 的一次性授权；由主线程执行，未创建子Agent。
- 新增 CombatPrototypeFollowCamera 与 CombatPrototypeCameraBindingSystem；前者负责镜头姿态/缩放/平滑和水平方向转换，后者仅在客户端处理本地拥有者、官方 PlayerView、表现同步完成、复活对齐与释放。
- 原 CombatPrototypePlayerInputSystem 接入当前本地镜头水平角到世界 X/Z Move 的转换，保留原四个输入字段、移动速度 5、预测/服务端移动与战斗/体力/物品/奖励/存档链。Z/X 每次旋转 45°，滚轮缩放；原空格/鼠标左键攻击、R 复活、E 物品使用保持。
- 仅在 CombatPrototypeNetCode.unity 的现有 Main Camera 新增一个 FollowCamera 组件并显式绑定同对象 Camera，FOV 从 60 改为 35；保存俯角 40°、锚点 Y 偏移 0.5、默认距离 18、上下限 12/26、缩放步长 1、跟随/缩放平滑 0.12s 与旋转过渡 0.18s。没有新增场景物体或修改原 Transform。
- 两份新脚本由 Unity 定点导入并生成 meta。编译中发现旧 CompleteDependency 调用的受保护访问错误，已按安装包的公开 CheckedStateRef.CompleteDependency 修正；最新程序集已编译、两个类型已加载，无新增脚本编译错误，未手动清空 Console。
- Main Camera 在干净 Edit Mode 中完成一次挂载与保存，实际组件由 3 个变为 4 个，Camera 引用与参数已读回；局部同步 Runtime、Player、Framework 当前事实/策略/待验收项及本 ChangeLog。
- 用户明确反馈尚未进行人工验收；人工 GamePlayer 的视野、跟随、持续移动时旋转、缩放、双客户端归属、死亡/复活/重连与原操作回归仍为 UNKNOWN。AI 未启动 PlayMode、执行游戏系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查；未提交 Git。
- 最终 Console 检查定位到相机绑定查询的两条 GetSingletonEntity 启用型组件异常，调用栈分别来自输入与表现阶段；已将本地玩家查询改为 SystemAPI.Query 的启用匹配枚举，保留唯一性检查，不分配临时数组。修正已重新编译，Console 保留修正前异常记录；修正后人工运行复测仍为 UNKNOWN。
- 主线程静态验收通过：修正后 Unity 编译与类型加载、相机绑定及参数、十份任务文件严格 UTF-8、模块文档局部链接和 git diff/空白检查已核对；十八项旧资源/配置基线中只有获授权的网络主场景变化，其余十七项（含任务开始已有 AutoSaveSettings.asset 修改）散列保持。人工 GamePlayer 与修正后运行复测仍待完成，未判定整体验收通过。

## 2026-10-03：网络 Player 灰衣修士美术与战斗动画静态实装

- 用户确认灰衣短刀修士外观，随后确认执行方案并一次性授权 PlayerView 层级/组件、Animator/Clip、角色资源导入及本次新设定/角色预览的视觉检查；主线程完成落地，未创建子Agent。
- 内置 image_gen 生成角色三视图设定稿，已复制到 Assets/Art/Player/CombatPrototype/GrayRobeCultivatorConcept.png；Doc/PlayerArt.md 保存完整提示词、工具、参数与实际消费边界。未读取原有 Player.jpg 或其他旧图片。
- 新增分部件低面数 GrayRobeCultivator.prefab、23 份网格、11 份 URP/Lit 材质；79 个 Renderer、2584 个三角形。模型按灰衣、旧皮护肩、符袋、短刀和暗朱红特征制作，设定稿细节简化。
- 新增 Idle/Move/Startup/Active/Recovery/Hit/Death 七份 60 fps Clip，时长为 1.6/0.7/0.18/0.08/0.3/0.16/0.75 秒，每份 161 条曲线，无动画事件。新建两层 PlayerCombat.controller 与只启用 Torso 后代的 PlayerUpperBody.mask，关闭 Root Motion。
- 新增 CombatPrototypePlayerNetCodeView 与 CombatPrototypePlayerAnimation，分别负责真实 Owner/World/Entity 状态读取和 Animator 表现；移动读取官方桥接完成后的显示位移，上身攻击不锁定下肢移动，死亡优先于受击/攻击，复活清除旧表现，首次绑定只建立序号基线。
- 仅修改原 CombatPrototypeNetworkPlayerView：增加一个 View 脚本和 VisualRoot 嵌套角色，局部 Y=-1，关闭原根胶囊 Renderer；原组件、根 Transform、GUID 和 Ghost ClientPrefab 引用保持。Scene/SubScene、玩家/敌人 Ghost Prefab、旧 meta、包/构建设置、伤害/体力/奖励/存档链未修改，新 meta 均由 Unity 生成。
- Unity 编译与新增类型已加载；只读资源核对确认必需引用正确、缺失脚本 0、所有曲线目标存在、两层状态与遮罩符合范围。通过隔离 Editor 预览场景采样核对本次新模型与动作，调整走路关键帧的脚部高度并导出 PlayerAnimationPreview.png；两份新图设置 NPOT=None 保留 1536x1024/1600x1100 的源图比例。
- 局部同步 AI_Understanding、Player、Combat、Runtime、DataResources，新增 PlayerArt 专题；移除与当前玩家动画冲突的“尚未接入受击动画”描述，保留正式 Map、敌人和既有验收边界。
- 主线程代码与资源静态验收通过；人工 GamePlayer 的双端角色显示、移动中攻击、受击/死亡/复活、断线重连、镜头和原战斗/物品回归仍为 UNKNOWN，性能未采样。AI 未启动 PlayMode、执行业务系统、逻辑单元测试、命令行构建或平台发布，未提交 Git。

## 2026-10-03：战斗地图配置建议模板导出

- 按用户指定路径新增 E:/UnityProjects/MyGame/Code_01策划/Config/战斗地图配置建议模板.md，整理已讨论的空间参数、生态密度、物体参数、配置数据职责、来源接口草案及地图 JSON 示例。
- 模板明确标注建议值、待确认与尚未实装状态，保留实际资源引用、物品定义和运行接入中的 UNKNOWN；未将建议模板写为已生效业务配置。
- 本次仅导出 Markdown 并追加本记录，未修改代码或资源，未运行逻辑单元测试、GamePlayer PlayMode、构建或发布，未读取图片。

## 2026-10-03：战斗地图第一阶段落地

- 用户确认第一阶段方案并授权新地图材质/网格/装饰 Prefab、原网络 SubScene 根节点新增 MapAuthoring 与 Unity 生成新 meta；由主线程执行，未创建子 Agent。
- 新增 Assets/Scripts/CombatPrototype/Map/：四类普通配置与集合、来源接口、草地/森林默认来源、边界校验、布局烘焙、地图 ECS 数据、共用玩家位置函数、分块网格与客户端显示。默认 3×3 块、每块 16×16 格、格长 2 米、地图 96×96 米、地面 Y=0、种子 12345；三类地表、静态草丛和碎石已登记，道路/安全区/空地/边缘/敌人初始网格排除装饰。
- 原 GoInGame 与 R 复活等待地图并共用出生计算，当前玩家仍为 (NetworkId×2,1,0)。Spawner Baker 从地图提供的原点/总量生成 ECS 数据，敌人仍为 32 个、8 列、间距 3，首点改为 (0,1,16)；旧 EnemyPosition/EnemyCount 序列化字段保留且不再控制当前原点/总量。原敌人生成增加地图就绪前提，其实例化与失败隔离保持。
- 新增 CombatPrototypeMapAssetBuilder Editor 入口，创建 4 材质、2 网格及 2 个单根装饰 Prefab；原 CombatPrototypeNetworkRoot 仅新增地图组件，默认 Forest，显式绑定 3 材质与 2 Prefab。主场景、镜头、玩家/敌人原 Prefab、Animator、旧 meta、包与构建设置经基线哈希核对保持。
- Unity 编译完成且最终 Console 无编译/地图错误。仅执行隔离 Editor 烘焙与网格数据检查，没有运行游戏模拟或显示系统；所有隔离 World/场景及临时生成网格均已释放。
- 两种模板实际烘焙均为 1 地图、9 块、2304 格，其中 452 格为禁止装饰区域；地图原点 (-48,-48)、尺寸 (96,96)，NetworkId=1 玩家位置 (2,1,0)。地表资源引用有效，两个装饰 Prefab 均有 Prefab、LocalTransform、MaterialMeshInfo。

| 隔离烘焙模板 | 草地格 | 林地格 | 岩地格 | 草丛组 | 碎石 | 生成块网格 | 非空地表子网格 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Forest | 512 | 1588 | 204 | 601 | 20 | 9 | 19 |
| Grassland | 1478 | 636 | 190 | 743 | 21 | 9 | 16 |

- 新增 Doc/Modules/Map.md；局部同步 AI_Understanding、Runtime、Player、Combat、DataResources，以及项目外战斗地图配置建议模板的实装状态和 JSON 字段形状。本记录保留此前建议模板导出记录。
- 主线程代码/资源/文档静态验收通过；人工 GamePlayer 的地图显示、跨端一致性、原玩法回归与生命周期清理仍为 UNKNOWN。实际 JSON 读取、树木、采集、碰撞/寻路/攻击遮挡、动态对象、再生和地图状态存档未接入。AI 未启动 PlayMode、执行逻辑单元测试、命令行构建、发布、性能采样或读取图片，未提交 Git。

## 2026-10-03：战斗地图第一阶段人工验收通过

- 用户明确反馈“我已验收通过，继续第二阶段”；主线程结合既有代码、资源与文档静态核对，判定战斗地图第一阶段通过。
- 人工验收范围限 Runtime.md 已列清单：两种模板的地表/装饰与保护区域、初始敌人和加入/复活位置、双客户端一致性及原玩法回归、停止重进/模板切换与 SubScene/World 清理。性能、平台构建和线上联调仍为 UNKNOWN。
- 局部同步 Map.md、Runtime.md 和项目外战斗地图配置建议模板的当前验收状态，并追加本记录；本轮未修改代码或资源，未创建子Agent。AI 未启动 PlayMode、执行逻辑单元测试、命令行构建、发布、性能采样或读取图片。

## 2026-10-03：战斗地图第二阶段 JSON 配置接入

- 用户确认执行方案并授权新增 JSON/脚本与 Unity 生成新 meta，以及原 SubScene MapAuthoring 的来源与文件引用字段；由主线程执行，未创建子Agent。
- 新增 Assets/Config/CombatPrototype/Map/ 下 battle_grassland_01.json、battle_forest_01.json 两份地图对象及 biomes.json、grounds.json、objects.json 三份共享数组。五份文件从第一阶段默认来源导出为 UTF-8 无 BOM，地图尺寸、生态/装饰/出生数值、schemaVersion/configRevision=1 和种子 12345 保持。
- 新增 CombatPrototypeMapJsonReader、CombatPrototypeJsonMapConfigSource、CombatPrototypeMapJsonBinding；读取 TextAsset.bytes，严格 UTF-8（支持 BOM）解码，检查必填/未知/重复字段、类型与数值表示范围，再沿原校验器检查版本/ID/引用/区域。JSON 模板 ID 必须与 Preset 匹配，错误保留文件/字段或语义原因；未绑定材质/Prefab 键的错误补齐对应 JSON 文件、条目 ID 和 visualResourceKey 字段，无自动默认来源回退。
- 原 MapAuthoring 增加 SourceMode 与五份显式 TextAsset 引用；当前 SubScene 保存 Json/Forest，原资源键、材质/装饰绑定、根节点与组件列表保持。Editor 绑定菜单先校验两种配置及敌人网格，只保存该 SubScene；Map Baker 和 Spawner Baker 在读取前登记所用四份 JSON 的依赖，运行仍使用现有烘焙 ECS 数据。
- Unity 编译与三个新增类型已加载；两种 JSON 配置与内置 DTO 逐字段一致。隔离 Editor 烘焙结果与第一阶段一致：两种均为 1 地图、9 块、2304 格、452 禁止装饰格，敌人 32/8 列/间距 3/首点 (0,1,16)，NetworkId=1 玩家点 (2,1,0)。Forest 地表格 512/1588/204、装饰 601/20；Grassland 地表格 1478/636/190、装饰 743/21，材质、装饰 Prefab 与玩家/敌人 Ghost 引用有效。
- 隔离核对脚本初次遗漏 AddEntityGUID，产生一条 Ghost 烘焙异常记录；补齐核对参数并按读取前依赖顺序重新编译、重烘焙后 Console 计数无新增，旧记录保留。全部隔离 World/BlobAssetStore/临时场景已释放，原主场景保持干净 EditMode；未改游戏系统以规避该核对参数错误。
- 局部同步 Map、Runtime、DataResources、AI_Understanding 及项目外策划模板的当前事实/读取时机/来源切换和第二阶段人工清单；第一阶段人工通过保持，第二阶段人工 GamePlayer 仍为 UNKNOWN。AI 未启动 PlayMode、执行游戏模拟或显示系统、逻辑单元测试、命令行构建、发布、性能采样或读取图片，未提交 Git。
- 主线程最终静态验收通过：28 份任务文本严格 UTF-8、五份 JSON 与示例语法、文档链接、11 个新 meta 的 GUID 唯一性及 git diff/空白检查已核对。2,924 份既有文本资源基线中仅九份授权代码/Scene/文档发生变化，旧资源、旧 meta、包与构建设置保持；导航 8,183 字节，地图模块文档低于 20 KiB。最终 Unity 编译无脚本错误，人工 GamePlayer/性能/平台与线上验证仍未执行。

## 2026-10-03 战斗地图第三阶段：树木生成与基础移动阻挡

- 用户确认并授权第三阶段方案，由主线程执行。范围为普通树木静态生成、JSON v2 配置、移动扫掠/滑动接入、新占位资源及原 MapAuthoring 的一次树木绑定；未新增采集、砍树、攻击遮挡、寻路、再生、动态对象或地图状态存档。
- MapDefinitionConfig 增加 movement 子段，BiomeDefinitionConfig 增加 treeObjectId；新增 MapMovementConfig，内置来源与两份地图 JSON 升级为 schemaVersion=2、configRevision=2。生态和物体 JSON 接入 tree_normal，grounds.json、地图尺寸、种子 12345、生态密度、出生位置及 32 敌人总量保持。旧 v1、缺段、无效引用及非法移动参数明确失败，无补字段或来源回退。
- 树木占地半径 0.5 米、同类间距 3 米、blocksMovement=true；草原/森林/岩地密度 0.4/1.5/0.1 每 100㎡。先完成全图树木布置，再补草丛/碎石；空间桶检查同类间距及非零占地互斥，阻挡物避让额外计入角色半径和留缝。Baker 增加 CombatPrototypeMapObstacle 缓冲，保留原布置/资源缓冲和两个 Baker 的 JSON 内容依赖登记。
- 新增 CombatPrototypeMapMovementUtility，共享 X/Z 圆形扫掠及有限次数滑动。玩家原 Client/Server 预测移动与敌人原服务端移动接入只读阻挡缓冲，角色 Y、输入归一化、死亡停动、速度/朝向、敌人目标/前后摇/停止距离和系统先后关系保持。默认 playerRadiusMeters=0.4、enemyRadiusMeters=0.45、collisionSkinMeters=0.01、maxSlideIterations=3；次数校验 1～8，留缝为正且小于两类半径，最小道路宽度须容纳最大角色直径与留缝。未增加输入/Ghost 字段或玩家/敌人 Prefab 组件。
- 新增 Editor 树木资源生成与绑定脚本。新建 TreeNormal.mat、TreeNormal.asset、TreeNormal.prefab，原资源目录保持；占位树高 4 米、最大视觉半径 1.25 米，单根对象、单网格/材质、46 顶点、64 三角形，仅 Transform/MeshFilter/MeshRenderer，无 Collider/Animator/Ghost。原 SubScene 只追加 tree_normal Prefab 引用，原层级、组件列表、五份 JSON 引用、Preset=Forest、SourceMode=Json、材质和装饰引用保持。
- 执行时首次只请求脚本编译，尚未完成新脚本资产导入便调用菜单，Unity 记录“菜单不存在”；该次未生成树木资源或改 Scene。完成全资源导入并确认新类型加载后，调用正式 Editor 入口完成资源生成与一次绑定；生成/绑定和有效烘焙未新增错误。Console 保留先前第二阶段检查器的 EntityGuid 异常和本次提前调用菜单的错误，未清空。
- Unity 6000.5.6f1 正常编译并加载新类型；最终输入字段仍为 Move、Attack、Respawn、UseItem。两个模板从原保存 SubScene 克隆至临时 Editor 场景，使用 SkipCreatingCompanions|AddEntityGUID 的隔离烘焙；没有更新游戏模拟/显示系统。Json 与当前 BuiltIn 配置逐字段一致，两种模板均为 1 地图、9 块、2304 格、452 禁止布置格、96×96 米，revision=2、seed=12345，32 敌人/8 列/间距 3、首点 (0,1,16)，NetworkId=1 玩家位置 (2,1,0)。
- 默认 Forest 静态产物为草丛 601、碎石 23、树木/阻挡记录 89；Grassland 为 744、21、53。最小树木中心间距分别为约 3.0768/3.0634 米；全部同类间距违规、占地重叠、保护区域违规和敌人出生重叠计数均为 0。阻挡索引/位置/半径、移动参数、地表材质、三个可渲染 Prefab 及原玩家/敌人 Ghost 引用有效，树木不含 GhostType。烘焙前后 Console 计数均为 [2,1,2]。
- 完成后 Editor 未进入 PlayMode，只保留未脏的原主场景，无临时烘焙 World。Map、Player、Combat、Runtime、DataResources 和策划模板按受影响条目更新，策划模板的“实际 JSON 尚未创建”残留已修正；第二阶段人工验收清单改用当前 v2 默认值，第三阶段人工清单已记录，第二/第三阶段人工结果仍为 UNKNOWN，第一阶段人工通过范围保持。
- 主线程资源/文档静态验收通过：3139 个既有非视觉文件的原始散列基线中，21 个文件在授权范围内变化，无范围外变化；新增 14 个文件及 7 个 Unity 生成的唯一 GUID。35 个本阶段文本文件严格 UTF-8、文档链接及森林 JSON 示例一致性、Scene 仅追加树木引用检查和 git diff --check 通过。AI 导航保持 8183 字节，Map 为 19183 字节；其他已有较大模块只修改关联段落，没有全量拆分或重写。
- 未新增/运行逻辑单元测试、人工 GamePlayer、PlayMode、命令行构建、平台发布、性能采样或图片检查。实际阻挡/滑动、显示/清理、双端预测同步、配置调整生效和原战斗/存档回归仍待用户人工验收；静态烘焙不代表这些运行结果通过。

## 2026-10-03：战斗地图第三阶段人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”；主线程结合既有代码、资源与文档静态验收，判定战斗地图第三阶段通过。
- 人工通过范围限 Runtime.md 第三阶段已有七项清单：树木显示/布置与保护区域、玩家和敌人阻挡/滑动/停止、双端预测与位置同步、第三阶段配置修改和错误反馈、清理及原移动/战斗/复活/物品/奖励存档/重连回归。
- 第二阶段独立 JSON 人工清单未单独获确认，仍为 UNKNOWN；性能、平台构建、线上联调及此前各模块超出本清单的验收边界保持。
- 局部同步 Map.md、Runtime.md、Player.md、Combat.md 和项目外战斗地图配置建议模板的当前验收状态，并追加本记录；未修改代码、JSON、Scene、Prefab、Animator、meta、包或构建设置，未创建子Agent。
- AI 未启动 PlayMode、执行 GamePlayer 或游戏系统、逻辑单元测试、命令行构建、发布、性能采样或读取图片。第四阶段代码与资源尚未执行。

## 2026-10-03：战斗地图第四阶段种植采集

- 用户确认并授权第四阶段方案，由主线程执行，未创建子Agent。范围为 gather_apple 初始布置、F 输入、服务端预约/计时/中断与入包保存、耗尽 Ghost 显示、新占位资源及原 MapAuthoring 的一次采集物引用追加。
- BiomeDefinitionConfig 新增 gatherObjectId；内置来源和两份地图 JSON 升级为 schemaVersion=3、configRevision=3，三种生态引用 gather_apple，objects.json 追加对应定义。旧 v1/v2、缺字段、引用非采集物及未知产出 ID 明确失败，不补默认值或回退来源；grounds.json、旧物体、地图尺寸、种子 12345、出生参数及 32 敌人总量保持。采集物要求正距离/时长/产出，禁止开启移动/近战/投射物阻挡及再生。
- gather_apple 占地半径 0.3 米、同类间距 1.5 米、交互中心 X/Z 距离 2 米、时长 1 秒；草原/森林/岩地密度 0.6/0.5/0.2 每 100㎡。全图依次布置树木、采集物、草丛/碎石，共用占地/间距/保护区域检查；树木保持，后两类装饰布局发生变化。vitality_apple 显式映射既有 Msg.ItemName.活力苹果 ×1，仅入包和保存；E 仍只使用小块肉。
- 新增 MapGatherData、MapGatherAuthoring、MapYieldItemResolver、MapGatherSpawnSystem、MapGatherSystem、MapGatherRenderSystem 六份运行脚本，职责分别为状态/配置/计时、烘焙、稳定 ID 映射、服务端生成/清理、交互结算及客户端耗尽显示。现有 PlayerInput 增加 Gather，仅本地拥有者 F 单次按下发事件；MapObject 烘焙配置新增采集参数，客户端静态地图显示跳过采集 Ghost，原移动和战斗脚本未改。
- 服务端采集系统在玩家伤害之后、复活之前处理已有预约及新请求。沿在线 CommandTarget 核对当前玩家/归属/存活/近战 Ready/零移动输入，选择范围内最近 Available 点，精确同距取较小布置索引；同次请求按 NetworkId 升序。移动/攻击/受击序号变化/死亡/超距/断线取消并释放预约，不锁定玩家操作；重复 F 不重置计时，同次完成或取消后不重新启动。
- 完成前取得必需状态/库存引用并预留缓冲容量，PrepareReward 投影保持当前金币/经验的完整库存，SavePrepared 成功后同次提交库存和 Depleted。准备/保存失败不发物品、不耗尽，释放预约、记录阶段及原异常并继续其他点，无自动重试。原存储类和奖励/消费系统未改，玩家存档仍为 v1；地图状态只属于本局，服务端重启重新生成点，已保存库存按原固定 ID 恢复。
- 服务端实例化插值采集 Ghost，同步 PlacementIndex、Phase、CollectorNetworkId；配置与进度组件标注仅服务端保留。客户端按 Depleted 禁用 MaterialMeshInfo，保留耗尽 Ghost 至地图释放供晚加入接收当前状态；根更换/失效、系统停止及 World 销毁清理本系统拥有的实体。实际同步/清理与晚加入仍待人工运行验收。
- 新增 Editor 采集资源生成/绑定脚本；生成 GatherApple.mat、GatherApple.asset、GatherApple.prefab，单网格/材质、36 顶点/48 三角形，高 0.9 米、最大视觉半径 0.45 米。单根 Prefab 无子对象/Collider/Animator，含 Transform、MeshFilter、MeshRenderer、GatherAuthoring、GhostAuthoringComponent 及 Ghost 自动要求的 LinkedEntityGroupAuthoring，无 Owner/AutoCommandTarget，仅插值。原 SubScene 仅追加 gather_apple 两行引用；原根节点、组件、五份 JSON 引用、Json/Forest 和旧材质/Prefab 引用保持。新增 meta 均由 Unity 导入生成。
- Unity 正常编译，新输入字段、系统特性及 GatherState Ghost Serializer/Snapshot 已加载，Snapshot 含三项同步字段。两种模板从原保存 SubScene 克隆至临时 Editor 场景，以 SkipCreatingCompanions|AddEntityGUID 隔离烘焙，不调用游戏模拟或显示系统；Json 与当前 BuiltIn 逐字段一致。两种均为 1 地图、9 块、2304 格、452 禁止布置格、96×96 米，32 敌人/8 列/间距 3/首点 (0,1,16)，NetworkId=1 玩家点 (2,1,0)。

| 静态烘焙模板 | 草地/林地/岩地格 | 草丛 | 碎石 | 树木/阻挡 | 采集点 | 最小采集同类间距 |
|---|---|---:|---:|---:|---:|---:|
| Forest | 512/1588/204 | 598 | 17 | 89 | 36 | 约 2.1460 米 |
| Grassland | 1478/636/190 | 746 | 22 | 53 | 38 | 约 2.0394 米 |

- 全部同类间距违规、非零占地重叠、树木/采集保护区域违规与敌人出生重叠为 0，采集配置/Prefab、材质/渲染引用、原玩家/敌人 Ghost 和移动参数有效。树木最小同类间距约 3.0768/3.0634 米。有效烘焙前后 Console 均为 [0,8,52]，无新增错误；最终仍为 0 Error/8 Warning，未清空 Console。Editor 保持干净的原主场景、未进入 PlayMode，仅有原 6 个 Editor/Loading World，无临时烘焙 World。
- 局部同步 Map、Inventory、Runtime、Player、Combat、DataResources、AI_Understanding 和项目外战斗地图配置建议模板；配置例与真实森林 JSON 一致，第四阶段人工清单含交互/中断/争抢/保存失败/耗尽与晚加入/重启/配置错误/清理回归。修正 Combat 中与已验收第三阶段不一致的“未接移动碰撞”残留。第一/第三阶段人工通过保持，第二阶段独立 JSON 和第四阶段人工 GamePlayer 仍为 UNKNOWN。
- 主线程代码/资源/文档静态验收通过：3153 个既有非视觉文件基线中仅 22 个授权文件变化，无范围外变化；新增 22 个文件及 11 个唯一 GUID。44 份任务文本严格 UTF-8，五份 JSON、300 个本地文档链接、策划示例一致性、Scene 仅追加引用及 git diff/空白检查通过。导航 8191 字节，Map 22156 字节；采集详细提交规则归 Inventory，人工清单归 Runtime，没有全量拆分或重写既有模块。
- 未新增/运行逻辑单元测试、人工 GamePlayer、PlayMode、命令行构建、平台发布、性能采样或图片检查，未提交 Git。静态验收不代表交互、写盘失败、跨端显示或性能通过；砍树、再生、地图状态持久化、攻击遮挡、寻路、新 UI、热重载及联网配置一致性协议未接入。

## 2026-10-03：战斗地图第四阶段人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”；主线程结合既有代码、资源与文档静态核对，判定战斗地图第四阶段通过。
- 人工通过范围限 Runtime.md 第四阶段既有八项清单：采集显示/布置，F 目标选择/计时及产出，预约中断/争抢，保存失败不入包/不耗尽并释放预约，耗尽跨端同步/晚加入，同局重连与服务端重启库存恢复，采集配置修改/错误反馈，以及清理和原移动/战斗/复活/E 物品/奖励存档回归。未触发的独立用例仍按原清单保留 UNKNOWN。
- 第二阶段独立 JSON 人工清单未单独获确认，仍为 UNKNOWN；第一/第三阶段既有通过、性能/平台构建/线上联调及超出当前清单的未知边界保持。
- 局部同步 Map、Inventory、Runtime、Player、Combat、DataResources 和项目外战斗地图配置建议模板的验收状态，并追加本记录；只修改上述八份文档，不改代码、JSON、Scene、Prefab、Animator、meta、包或构建设置，未创建子Agent。AI 导航的地图/采集路由保持。
- AI 未启动 GamePlayer/PlayMode、执行游戏系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。人工通过结论来自用户反馈，未执行第五阶段开发。

## 2026-10-03：战斗地图第五阶段采集物原点再生

- 用户确认第五阶段方案，由主线程按确认范围执行，未创建子Agent。仅接入现有采集物的“成功采集并耗尽→服务端模拟计时→原实体/位置恢复 Available→新 F 再次采集”。
- gather_apple 默认 regrowEnabled=true、regrowSeconds=600；沿用现有 JSON 字段形状和 schemaVersion=3，内置来源与两份地图 JSON 的 configRevision 统一为 4。objects.json 仅修改该物体的再生开关/间隔，其他对象、biomes.json、grounds.json、布局/种子/密度/出生/产出与移动参数保持。
- 修改七份既有 Map 脚本：DefaultMapConfigSource、MapConfigValidator、MapData、MapAuthoring、MapGatherData、MapGatherSpawnSystem、MapGatherSystem。校验器解除采集物的再生禁用限制，保留有限非负间隔及启用时正间隔校验；三类阻挡仍禁止。MapObject 和仅服务端 GatherConfig 新增 RegrowEnabled/RegrowSeconds，Baker 与服务端生成链传递配置；仅服务端 GatherProgress 增加 double RegrowAt，共享 GatherState Ghost 字段仍为 PlacementIndex/Phase/CollectorNetworkId。
- Complete 在准备阶段计算候选期限，只有 SavePrepared 成功后同次提交库存、耗尽与期限；关闭时期限为 0。取消或准备/保存失败清空进度、释放预约，不发物品、不耗尽、不安排再生，无自动重试。原玩家存档类、v1 格式、奖励/消费系统与输入字段保持。
- 新增独立 CombatPrototypeMapGatherRegrowSystem.cs 及 Unity 导入生成的 meta。在 ServerSimulation 的 PredictedSimulationSystemGroup，GatherSystem 之后、PlayerRespawnSystem 之前恢复启用再生且到期的 Depleted 点，清空进度/采集者、设置 Available；保持原实体/位置/索引，不创建新 Ghost、不发物品、不写盘。逐项记录再生或带条目标识/阶段/原异常的失败，其他点继续处理。客户端复用原状态同步和 MaterialMeshInfo 显示链，未改渲染系统。
- 期限采用当前 Server World 的 SystemAPI.Time.ElapsedTime，不依赖客户端或系统墙钟；耗尽/期限只保留本局，不写玩家或地图存档。关闭再生则保持本局耗尽，服务端重启从 Available 重建，既有苹果库存仍按原固定 ID 恢复。再次采集须新 F，原预约/资格/中断与 SavePrepared 顺序保持。
- 正常 Unity 导入/编译后新再生类型及生成代码已加载，开关/间隔/期限字段类型为 byte/float/double，配置/进度仍标注仅服务端，共享 Ghost 同步仍为三字段。两个模板从保存 SubScene 克隆至临时 Editor 场景隔离烘焙，与当前 BuiltIn 逐字段一致，烘焙配置为 v3/revision=4、启用再生/600 秒。Forest/Grassland 仍为 36/38 采集点、89/53 树木和阻挡记录，草丛 598/746、碎石 17/22；1 地图/9 块/2304 格/452 禁布格、96×96 米及 32 敌人/8 列/间距 3/首点 (0,1,16)、NetworkId=1 玩家点 (2,1,0) 保持，占地/间距/保护区域/敌人出生重叠违规均为 0。有效烘焙前后 Console 为 [0,7,49]，未清空 Console，Editor 未进入 PlayMode、原主场景保持干净。
- 局部同步 Map、Inventory、Runtime、Player、Combat、DataResources 及项目外战斗地图配置建议模板，追加本月记录；AI_Understanding 原地图/采集导航保持。Runtime 新增临时 5 秒、至少两轮、同 Ghost 原点、双端/晚加入、关闭、取消/保存失败、重启和原玩法回归的人工清单，要求结束恢复 Json/Forest、v3/revision=4 及启用再生/600 秒。明确第四阶段既有通过来自再生关闭的 v3/revision=3 版本，保留用户验收结论。
- 主线程代码、资源边界和文档静态验收通过；第五阶段人工 GamePlayer 为 UNKNOWN，第一/第三/第四阶段既有通过及第二阶段独立 JSON UNKNOWN 保持。AI 未新增/运行逻辑单元测试、游戏模拟/显示系统、GamePlayer/PlayMode、命令行构建、平台发布、性能采样或图片检查，未提交 Git。循环再生、跨端恢复、失败/取消与关闭分支不能以静态烘焙替代；树木再生、砍树、攻击遮挡、寻路、地图状态持久化、通用动态物体、新 UI、热重载及联网配置一致性协议未接入。
- 最终范围核对：3175 个既有非视觉文件中仅 18 个授权文件变化（含项目外模板），新增再生脚本及 meta 两个文件、1 个唯一 GUID；既有 Scene/Prefab/Animator/资源/旧 meta/包与构建设置无变化。20 份任务文本严格 UTF-8、5 份 JSON、259 个本地文档链接、策划森林示例与真实 JSON 一致性及 git diff/空白检查通过。导航保持 8191 字节，Map 为 23513 字节。最终仍仅有原 6 个 Editor/Loading World，无临时烘焙 World，原主场景干净、未进入 PlayMode，Console 为 0 Error/7 Warning，未清空日志。

## 2026-10-03：战斗地图第五阶段人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”；主线程结合既有代码、资源边界与文档静态核对，判定战斗地图第五阶段通过。
- 人工通过范围限 Runtime.md 第五阶段既有八项清单：至少两轮同 Ghost/原点再生、新 F 再采集且不重复发奖/写盘、跨端恢复与晚加入/重连、关闭再生、取消/保存失败不安排再生、服务端重启按既有规则重建及库存恢复、再生配置修改/错误反馈，以及清理和原玩法回归。未触发的精确同 tick/同距等独立用例仍按原清单保留 UNKNOWN。
- 第一/第三/第四阶段既有通过、第二阶段独立 JSON UNKNOWN 及性能/平台构建/线上联调等超出清单的边界保持。
- 局部同步 Map、Inventory、Runtime、Player、Combat、DataResources 和项目外战斗地图配置建议模板的验收状态，并追加本记录；只修改上述八份文档，代码、JSON、Scene、Prefab、Animator、meta、包与构建设置保持，AI_Understanding 原地图/采集导航保持，未创建子Agent。
- 人工结论来自用户反馈，AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。本轮未执行第六阶段开发。

## 2026-10-03：战斗地图第六阶段动态掉落与拾取

- 用户确认并授权第六阶段完整方案，由主线程执行，未创建子Agent。每敌人本局首次死亡额外生成活力苹果 ×1，服务端控制飞行/落地、G 单次拾取、保存入包及到期/地图释放清理；原金币/经验/小块肉统一奖励、F 种植采集及 600 秒原点再生保持。
- 两份地图 JSON 和 BuiltIn 升级 schemaVersion=4、默认 configRevision=5，增加必填 drops 段：enabled=true、itemId=vitality_apple、quantity=1、visualResourceKey=drop_apple、pickupDistanceMeters=2、flightDurationSeconds=0.4、scatterRadiusMeters=0.6、arcHeightMeters=0.6、groundOffsetMeters=0.05、visualScale=0.5、lifetimeSeconds=600（0 关闭自动到期）。原空间/移动/出生/生态值、共享 biomes/grounds/objects JSON 保持；旧版本或缺字段明确失败。
- 仅修改五份既有 C#：MapDefinitionConfig、DefaultMapConfigSource、MapConfigValidator、MapAuthoring、Networking/PlayerInput。增加 drops DTO/数值及白名单校验、显式掉落 Prefab 烘焙配置与 G 的 InputEvent Pickup；所有端须使用相同输入布局/资源/配置，没有修改原奖励、存储、伤害或采集系统。
- 新增十份脚本：MapDropConfig，DropData/Authoring/Spawn/Motion/Pickup/Cleanup/Render，Editor DropAssetBuilder/DropBinding；新脚本及 Prefab meta 由 Unity 导入生成。DropState 同步 DropId/ItemId/Quantity/Phase 四字段，Progress 标注仅服务端；服务端驱动 LocalTransform 弧线位置，客户端按插值 Ghost 和 Consumed 控制显示。
- 在线当前玩家须存活、静止、没有攻击请求且近战 Ready；G 选 X/Z 范围内最近 Landed 且未到期的掉落，同距取较小 DropId，同次更新按 NetworkId 升序，任意合格玩家均可拾取。SavePrepared 成功后同次提交库存及 Consumed；保存失败保留库存及未到期掉落，不自动补发，须新 G。原 v1 格式/存储类保持，地面 DropId/位置/状态/期限不写盘，重启清空；已拾取库存随固定 ID 恢复。
- 按一次性资源授权创建 DroppedApple.prefab，复用既有 GatherApple.asset/.mat，单根仅插值 Ghost、无 Owner/AutoCommandTarget；原 SubScene 的 DecorationPrefabs 仅追加 drop_apple 引用。新 Prefab GUID 为 3c92d1d189fc4af44b3957ed7ba2784c。原网格/材质、主场景、玩家/敌人及采集 Prefab、Animator、旧 meta、包与构建设置保持；清理 Unity 新序列化的空字段尾部空白。
- 首轮正常 Unity 编译发现 NativeList 的 using 变量不可写错误，改为 try/finally 释放后编译无 C# 错误，新掉落系统、状态序列化器与 Pickup 输入命令类型已加载。清理增加 CleanupQueued，避免多 tick 重复排队；拥有者清理保留已排队项给 EndSimulation ECB，避免记录与回放之间提前重复销毁。
- 保存 SubScene 克隆至临时 Editor 场景，Forest/Grassland 两种模板隔离烘焙均与 BuiltIn 逐字段一致，schema=4/revision=5、全部 drops 默认值及独立 Ghost Prefab 有效。仍为 1 地图/9 块/2304 格/452 禁布格、96×96 米；森林/草地分别为草丛 598/746、碎石 17/22、树木与阻挡 89/53、采集点 36/38，占地/间距/保护区域及敌人出生重叠违规为 0。原 32 敌人/8 列/间距 3/首点 (0,1,16) 和 NetworkId=1 玩家点 (2,1,0) 保持。烘焙前后 Console 为 [0,2,2]，两个 Warning 来自既有 PEListener 序列化与 DOTween 过时 API；未清空 Console，原主场景干净、未进入 PlayMode。
- 新增 MapDrops 专题并局部同步 Map、Inventory、Runtime、Player、Combat、DataResources、AI_Understanding 与项目外建议模板，当前模板 JSON 示例同步真实森林配置；Runtime 新增九项第六阶段人工清单。保留第一/第三/第四/第五阶段用户人工通过与第二阶段独立 JSON UNKNOWN；历史第四/第五阶段默认值明确归当时版本，当前恢复值为 v4/revision=5。
- 主线程代码/资源/文档静态验收通过；第六阶段人工 GamePlayer 为 UNKNOWN。AI 未新增/运行逻辑单元测试、游戏模拟/显示系统、GamePlayer/PlayMode、命令行构建、发布、性能采样或图片检查，未提交 Git。不增加物理、UI、世界存档、砍树、通用动态对象框架、热重载或联网配置一致性协议；同步写盘性能/平台/线上验收仍为 UNKNOWN。
- 最终范围核对：3177 个既有非视觉文件中仅 17 个授权文件变化，新增十份脚本及其 meta、一个 Prefab 及其 meta 和一个掉落专题，共 23 个文件、11 个唯一 GUID。SubScene 仅追加两行 drop_apple 引用；旧资源/meta/场景结构保持，新生成 meta 仅清理尾部空白。40 份任务文本严格 UTF-8、5 份 JSON、344 个本地文档链接、策划森林示例与真实 JSON 一致性及 git diff/空白检查通过；导航为 8188 字节，Map 为 23969 字节。最终新类型和 Prefab 引用仍已加载，仅有原 6 个 Editor/Loading World，无临时烘焙 World，主场景干净、未进入 PlayMode。最后一次导入后的 Console 为 0 Error/1 Warning，Warning 来自 MCP 域重载时 WebSocket 未初始化，工具已恢复可调用；未清空日志。

## 2026-10-03：战斗地图第六阶段人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”。主线程结合既有代码/资源/文档静态核对与用户人工反馈，判定战斗地图第六阶段通过；人工结论来自用户反馈，范围仅限 Runtime 第六阶段九项清单。
- 通过范围为敌人首次死亡一次额外苹果掉落、飞行/落地及 G 资格/最近选择、多人争抢一次提交、保存失败保留与新 G 重试、跨端/晚加入/重连及库存恢复、配置/到期/释放和原战斗/奖励/F 再生回归。未实际触发的临界距离、精确同距、同 tick 与其他独立用例仍为 UNKNOWN。
- 第一/第三/第四/第五阶段既有通过范围保持，第二阶段独立 JSON 人工清单仍为 UNKNOWN；同步写盘耗时、规模性能、平台构建与线上联调未验收。本次反馈不扩展为通用动态对象、物理、UI、世界存档或树木砍伐通过。
- 局部同步 Map、MapDrops、Inventory、Player、Combat、DataResources、Runtime、本月 ChangeLog 和项目外战斗地图配置建议模板，共九份既有文档；当前 v4/revision=5 默认配置保持，AI_Understanding 原导航保持。代码、JSON、Scene、Prefab、Animator、meta、包与构建设置未修改，未创建子Agent。
- AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未提交 Git。本轮未执行第七阶段开发。

## 2026-10-03：战斗地图第七阶段树木砍伐与资源掉落

- 用户确认并授权第七阶段完整方案及四项新资源/两个显式引用的一次性边界，由主线程执行，未创建子Agent。新增 H 单次服务端砍伐预约、2 秒完成木材 ×3、砍倒隐藏及移动阻挡同步，木材复用飞行/G 拾取/保存和清理链。
- 两份地图 JSON 与 BuiltIn 升级 schemaVersion=5/configRevision=6，增加必填 treeHarvest：enabled=true、treeObjectId=tree_normal、visualResourceKey=tree_harvest、harvestDurationSeconds=2、dropItemId=wood、dropQuantity=3、dropVisualResourceKey=drop_wood。距离复用对象定义的 2 米；原空间、种子、生态、出生、movement/drops 及共享三份 JSON 保持，旧 v4 不补段/迁移。
- 修改十二份既有 C#：MapDefinitionConfig、DefaultMapConfigSource、MapConfigValidator、MapAuthoring、MapData、MapPresentationSystem、MapMovementUtility、MapYieldItemResolver、DropSpawnSystem、DropPickupSystem、Networking/PlayerInput、Msg/Msg。MapObject 增加 Harvestable，开启时路由新 Ghost，关闭时沿原静态树；MapObstacle 增加 Disabled，由原移动工具跳过。
- 新增十份脚本：MapTreeHarvestConfig，TreeData/Authoring/Spawn/Harvest/Obstacle/Render，DropSpawnUtility，Editor TreeHarvestAssetBuilder/Binding。TreeState 同步 PlacementIndex/Phase/CollectorNetworkId/FelledTick 四字段，Progress 标注仅服务端；创建/计时/阻挡/显示职责分别拆分。
- H 验证当前在线归属/Simulate、存活、静止及近战 Ready，最近 Standing/同距较小 PlacementIndex，同次请求按 NetworkId 排序。移动、攻击、受击、死亡、超距/断线取消；F 请求/既有采集优先。先初始化并登记木材掉落，再重新取得结构变更失效访问提交 Felled/解除阻挡；失败清理当前掉落并恢复可预约，错误逐项隔离。
- 苹果/木材共用 DropId 与有效所有权，G 改按目标实际 ItemId 白名单解析，WoodId 映射 Msg.ItemName.木材；SavePrepared 成功后才入包/Consumed，v1 格式与存储类保持。树木/未拾取木材不写世界存档，重启按原布局恢复树木；已入包木材随固定 ID 库存恢复。drops.enabled 仍控制敌人额外掉落，砍伐开关独立，木材共用其运动/拾取/寿命参数。
- 阻挡系统在玩家/敌人移动前按权威 FelledTick 重建当前预测 tick；完成发生在本 tick 移动之后，严格晚于砍倒 tick 才移除阻挡，回放此前 tick 恢复原阻挡。客户端显示按 Felled 隐藏，保留本局树木 Ghost 供晚加入；原 F 再生、战斗奖励、角色资源与架构保持。
- 按授权创建 HarvestableTree.prefab（GUID 3207c51e470cd2244b3b934436fd83ec）与 DroppedWood.prefab（4d858381c32935942851ccbba8a14fe4）；新树复用 TreeNormal.asset/.mat。新木材网格/棕色材质由程序生成，网格 18 顶点/32 三角形，体积为正、三角形无退化；生成前校正朝外绕序。原 SubScene 仅追加 tree_harvest/drop_wood 四行引用，旧 Scene/Prefab/Animator/meta/网格/材质/包与构建设置保持；新 meta 由 Unity 导入生成。
- 首轮编译出现两处项目 Code_01.System 遮蔽 System.IO 的 CS0234，改为 global::System.IO 后正常 Unity 编译无 C# 错误。HarvestTree 输入辅助/命令序列化类型与 TreeState Ghost Serializer/Snapshot 已生成并加载，系统顺序与仅服务端 Progress 特性已静态核对。
- 保存 SubScene 克隆至临时 Editor 场景，Forest/Grassland 隔离烘焙均与 BuiltIn 完整值一致，为 v5/revision=6。仍为 1 地图/9 块/2304 格/452 禁布格、96×96 米；森林/草地草丛 598/746、碎石 17/22、树木与初始启用阻挡 89/53、采集点 36/38，占地/间距/保护区/敌人出生重叠违规为 0。四类地图 GhostType 互异，原 32 敌人/8 列/间距 3/首点 (0,1,16) 与 NetworkId=1 玩家点 (2,1,0) 保持。烘焙前后 Console 为 [0,1,5]，Warning 为既有 DOTween 过时 API，未清空 Console；主场景干净、未进入 PlayMode，无临时烘焙 World 遗留。
- 新增 MapTreeHarvest 专题，局部同步 Map、MapDrops、Inventory、Player、Combat、DataResources、Runtime、AI_Understanding、本月 ChangeLog 与项目外建议模板；配置示例同步真实森林 JSON，Runtime 增加十项第七阶段人工清单。第一/第三/第四/第五/第六阶段通过仅限原版本/原清单，第二阶段独立 JSON 人工仍为 UNKNOWN。
- 最终范围核对以本阶段开始前 3200 份文件散列为基线：25 份既有文件受影响（含项目外模板），29 份新增文件（含 14 份 Unity 生成 meta）；未删除文件，旧资源仅 SubScene 两处引用改变。Unity 保存引入的原组件空名称尾空格已恢复原序列化格式。54 份文本严格 UTF-8/新增行空白检查、14 个新 GUID 唯一性与旧 GUID 无冲突、5 份 JSON、模板示例一致性、388 个文档本地链接和 git diff --check 均通过；导航 8191 字节、地图主文档 23331 字节、新砍伐专题 12387 字节均在约定上限内。
- 主线程代码/资源静态验收通过；第七阶段人工 GamePlayer 为 UNKNOWN，取消/争抢、动态阻挡/预测回放、失败回滚、木材拾取保存/恢复与原玩法回归需人工确认。未接斧头、动画/树桩、树木再生、物理、世界存档、新 UI 或使用效果。AI 未运行游戏模拟/显示系统、GamePlayer/PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未提交 Git；性能、平台与线上联调仍为 UNKNOWN。

## 2026-10-03：战斗地图第七阶段人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”，主线程结合第七阶段既有静态核对与用户反馈判定该阶段通过；范围限 Runtime 第七阶段十项清单及 schemaVersion=5/configRevision=6 版本。
- 人工通过覆盖范围按原清单记录：H 最近树木/计时/木材产出、中断/F 优先/争抢、隐藏及动态阻挡/跨端/晚加入、苹果与木材分别拾取/保存/恢复、配置/开关/到期/清理和原玩法回归。未实际触发的临界距离、精确同距、同 tick、延迟/预测回放与创建/清理/回滚失败分支仍为 UNKNOWN，不扩展为规模性能、平台构建或线上联调通过；第二阶段独立 JSON UNKNOWN 与此前阶段边界保持。
- 局部同步 Map、MapTreeHarvest、MapDrops、Inventory、Player、Combat、DataResources、Runtime、本月 ChangeLog 及项目外建议模板，共十份既有文档；AI_Understanding 当前路由和 v5/revision=6 默认配置保持。用户已选择第八阶段方向“树木原点再生”；本轮只同步第七阶段验收状态，未执行第八阶段开发。
- 代码、JSON、Scene、Prefab、Animator、meta、包与构建设置未修改，未创建子Agent。人工结论来自用户反馈；AI 未运行游戏模拟/显示系统、GamePlayer/PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未提交 Git。

## 2026-10-04：战斗地图第八阶段树木原点再生

- 用户确认第八阶段完整方案，由主线程执行，未创建子Agent。入口为 CombatPrototypeNetCode，成功砍倒后按原对象字段计时并在原树 Ghost 恢复 Standing/显示/阻挡，再次产出须新 H。
- 两份地图 JSON 与 BuiltIn 的 configRevision 从 6 改为 7，schemaVersion=5 保持；objects.tree_normal 的 regrowEnabled=true、regrowSeconds=600。没有新增 JSON 字段或修改原 DTO/校验器；原空间、种子、生态、出生、砍伐/drops/gather_apple 数值和 biomes/grounds 保持。
- 修改七份既有脚本：DefaultMapConfigSource、MapAuthoring、TreeData、TreeAuthoring、TreeSpawnSystem、TreeHarvestSystem、TreeObstacleSystem。Map Baker 复制所选树木再生配置，TreeProgress 增加仅服务端 RegrowAt；TreeAuthoring 烘焙空阻挡历史，TreeSpawn 初始化时清空。
- 新增 TreeRegrowSystem，仅服务端预测组、TreeHarvest 之后及 PlayerRespawn 之前执行。计时只从完整木材生成/登记及砍倒提交成功起安排；取消/失败不安排。到期原点无占位时先预留缓冲容量，再追加启用历史、清空进度并恢复原实体/阻挡；单树失败回滚状态、进度、历史长度及阻挡，原异常可定位，其他条目继续，原期限保留供后续检查。
- 占位采用原阻挡位置/半径，加对应角色半径及留缝；全部存活服务端网络玩家参与（包含未启用 Simulate 的玩家），存活敌人参与，死亡角色跳过，默认阈值分别为 0.91/0.96 米（含边界）。占位时等待、清空后恢复，不推开角色/换树位/重置期限；非有限角色位置明确终止本批恢复。
- TreeState 保持四个 Ghost 字段，新增 CombatPrototypeMapTreeBlockingEvent 动态缓冲，两字段 TransitionTick/Disabled 同步，内部容量 4，每完整轮次增加两条。砍倒/再生均在移动之后记录权威 tick，下一模拟 tick 起转换；TreeObstacle 对所有树木阶段逆向查找历史重建当前预测阻挡，取消/新 H 保留既往轮次，不只依赖最新 FelledTick。历史保留当前 World 全部轮次并随原树实体释放，不写世界存档。
- 再生不创建新树/掉落、不发物品、不写盘；重复砍伐继续共用原 DropId/有效所有权/木材 G 保存与到期清理。原输入、玩家/敌人、F 再生、战斗/奖励、存储类与架构保持。原 Scene/SubScene、Prefab、Animator、网格/材质/资源/旧 meta、包与构建配置未修改；仅新增系统脚本的 meta 由 Unity 导入生成。
- 首轮正常编译发现新增系统两处动态缓冲临时返回值写入的 CS1612，改为局部缓冲变量后正常 Unity 编译无 C# 错误；静态复核移除占位查询对 Simulate 的限制，覆盖全部存活网络玩家，随后正常编译当前 0 Error。两项阻挡历史 Ghost Serializer/Snapshot 已生成，RegrowAt 的服务端标记和系统顺序已核对；未调用游戏系统。
- 原 SubScene 根克隆到临时 Editor 场景，Forest/Grassland 隔离烘焙均与 BuiltIn 完整值一致，为 v5/revision=7；物体定义/TreeSettings 均为 true/600 秒，树 Prefab 历史为空且 RegrowAt=0。仍为 1 地图/9 块/2304 格/452 禁布格、96×96 米；森林/草地树木及初始阻挡 89/53、采集点 36/38、草丛 598/746、碎石 17/22，占地/间距/保护区与出生重叠违规为 0。四类 GhostType 互异，原 32 敌人/8 列/间距 3/首点 (0,1,16) 和 NetworkId=1 玩家点 (2,1,0) 保持。
- 烘焙前后 Console 均为 [0 Error,8 Warning,106 Log]，未清空日志、未新增烘焙警告；原主场景干净/未进入 PlayMode，临时烘焙 World 已释放，保留原六个 Editor/Loading World。代码/资源静态验收通过；多轮再生/占位/预测回放与第八阶段人工 GamePlayer 为 UNKNOWN。
- 局部同步 Map、MapTreeHarvest、MapDrops、Inventory、Player、Combat、DataResources、Runtime、本月 ChangeLog 与项目外建议模板，共十份文档；Runtime 增加八项人工清单，模板示例同步当前森林 JSON。修正地图/模板中仍称树木无产出或无再生及旧当前配置版本的受影响描述；AI_Understanding 已有专题路由保持。第七阶段用户通过仍限 v5/revision=6 原十项清单，其他既有通过与第二阶段独立 JSON UNKNOWN 保持。
- AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未提交 Git。阻挡历史的内存/网络开销、规模性能、平台构建与线上联调未验收。
- 最终范围核对以本阶段开始前 3290 份既有非视觉文件散列为基线：仅 20 份授权既有文件变化（七份脚本、三份 JSON、十份文档），新增系统脚本及其 meta 两份，无删除；原 SubScene/Scene/Prefab/Animator/资源/旧 meta 字节保持。22 份任务文本严格 UTF-8/新增行空白、5 份 JSON、策划森林示例一致性、496 个文档本地链接及 git diff --check 均通过。新 meta GUID fcaaed41f0c51154a9700bef2f28f400 唯一且与旧 GUID 无冲突；导航 8191 字节、地图主文档 23981 字节、树木专题 16699 字节在上限内。最终 Editor 为 idle、未播放/未编译，Console 当前 0 Error；人工 GamePlayer 与性能覆盖仍为 UNKNOWN。

## 2026-10-04：战斗地图第八阶段人工验收通过

- 用户明确反馈“我已验收通过，接下来下个阶段”。主线程结合第八阶段既有代码/资源/文档静态核对与用户人工反馈判定该阶段通过；范围限 Runtime 第八阶段八项清单及 schemaVersion=5/configRevision=7，结论来自用户反馈。
- 人工通过按原清单限定：至少两轮原树再生/新 H 产出、取消/失败不安排、存活角色占位等待/阻挡恢复、双端/晚加入、开关/配置、重启/释放及原库存/战斗回归。未实际触发的精确边界、同 tick、延迟/预测回放、创建/清理/回滚或保存失败用例仍为 UNKNOWN；历史内存/网络开销、规模性能、平台构建与线上联调未验收，不扩大此前阶段通过范围，第二阶段独立 JSON UNKNOWN 保持。
- 局部同步 Map、MapTreeHarvest、MapDrops、Inventory、Player、Combat、DataResources、Runtime、本月 ChangeLog 与项目外配置建议模板，共十份既有文档。当前两份地图 v5/revision=7、tree_normal/gather_apple 均启用再生/600 秒、SourceMode=Json/Preset=Forest 保持，AI_Understanding 原导航保持。
- 用户已选择第九阶段方向“石头／矿点采集与资源掉落”；本轮同步第八阶段验收状态并只读准备后续方案，未执行第九阶段开发。代码、JSON、Scene/Prefab/Animator/资源/meta、包与构建配置未修改，未创建子Agent、未提交 Git。
- AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查。第八阶段人工结论完全来自用户反馈。
- 最终核对：3292 份既有非视觉文件基线中仅上述十份文档变化，无新增/删除，代码/JSON/原资源与结构字节保持。十份文档严格 UTF-8、407 个局部文档本地链接及 git diff --check 通过，原第八阶段八项清单正文保持。导航为 8191 字节、地图入口 24133 字节、树木专题 16975 字节，均在本次约定上限内；未运行编译、逻辑单元测试或人工 GamePlayer。


## 2026-10-04：战斗地图第九阶段矿点采集与石材掉落

- 用户选择“石头／矿点采集与资源掉落”，并在完整第九阶段执行方案和一次性资源/Scene 权限说明后明确“确认并授权”；主线程按方案执行，未创建子Agent。
- 两份地图与 BuiltIn 更新为 schemaVersion=6/configRevision=8，新增必填 mining，默认启用 mine_rock、3 秒、stone ×3、drop_stone。biomes 新增 mineObjectId/mineDensityPer100m2，grassland/forest/rocky 为 0.1/0.2/1；objects 在原四项后追加矿点，占地 0.75、间距 2.5、交互 2 米、只阻挡移动、非 gatherable、无再生。grounds 及原空间/种子/出生/movement/drops/treeHarvest 数值保持，旧 v5 明确拒绝。
- 原 PlayerInput 增加本地 J/Mine 单次事件；新增独立 Mine 数据、Authoring、生成/采矿/预测阻挡/显示系统。服务端在 F/H 与伤害后、R 前处理资格/最近预约，F/H 请求或活跃状态优先，移动/攻击/受击/死亡/超距/在线或归属失效取消；重复/长按不重置或连续，完成/取消本 tick 不再预约。石材完全初始化并登记后才耗尽矿点/解除阻挡，失败仅清理当前掉落并尝试回滚，原异常及清理错误保留。本局 Depleted 不再生，MinedTick 四字段 Ghost 同步供预测 tick 重建阻挡。
- stone 显式映射新增 Msg.ItemName.石材。石材沿原共享 DropId、飞行/落地/G/SavePrepared/到期/清理链，保存成功后才提交库存/Consumed；原 v1 存储类及格式保持，矿点/未拾取掉落不保存。drops.enabled 只控制敌人额外掉落，mining 开关独立。
- 新增 MineableRock.asset/.mat、MineableRock.prefab、DroppedStone.prefab 四个授权资源及 Unity 生成 meta，两个 Prefab 为单根插值动态 Ghost、带 LinkedEntityGroupAuthoring，共用新灰色占位网格/材质，无 Owner/AutoCommandTarget/Collider/Animator。新 Editor 创建/绑定入口只处理明确路径、拒绝覆盖/重复绑定；原网络 SubScene 仅追加 mine_rock/drop_stone 两个引用。旧 Scene 根/挂载关系、主 Scene、玩家/敌人 Prefab、Animator、旧资源/旧 meta、包与构建设置保持。
- 正常 Unity 编译当前 0 C# Error；MineState 四字段 Serializer/Snapshot、J 命令类型、Server 标注及系统顺序通过。Forest/Grassland 的 Json/BuiltIn 各一次隔离烘焙，另各一次 Json mining=false，共六次；森林/草原默认矿点 20/18、阻挡 109/71，树木 89/53、采集点 36/38 的位置与朝向保持，空间违规为 0。默认装饰草丛/碎石变为 601/19 与 744/19；关闭采矿后旧四类 [598,17,89,36]/[746,22,53,38] 与第八阶段一致。新网格 38 顶点/72 三角形、高 1.2 米/最大半径 0.75 米，非退化且朝外。烘焙前后 Console 均 [0 Error,2 Warning,7 Log]，未清空或新增烘焙警告，原主场景干净，无临时 World 遗留。
- 局部同步 Map/树木/掉落/背包/玩家/战斗/资源/Runtime、AI_Understanding、项目外建议模板及本月 ChangeLog；新增 MapMining 规则专题，Runtime 新增十二项人工清单，策划森林示例同步真实 JSON。第九阶段代码/资源静态验收通过，人工 GamePlayer 为 UNKNOWN；第八阶段仍限用户通过的 v5/revision=7 八项清单，第七阶段仍限 v5/revision=6 十项清单，其余通过与第二阶段独立 JSON UNKNOWN 保持。
- AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未提交 Git；同步写盘耗时、规模性能、平台构建与线上联调未验收。
- 最终第九阶段范围核对：以开始前 3292 份既有非视觉文件散列为基线，仅 27 份授权既有文件变化（11 脚本、4 JSON、1 SubScene、11 文档），新增 27 份（9 脚本及 9 meta、4 资源及 4 meta、1 采矿专题），无删除/越界。54 份任务文本严格 UTF-8、新增行空白、五份 JSON/策划示例一致性、12 份文档的 460 个本地链接及 git diff --check 通过；13 个新 meta GUID 唯一且与旧 GUID 无冲突。SubScene 差异严格限两项绑定，旧主场景/资源/meta 散列保持。导航 8188 字节、地图入口 24193 字节、树木专题 17247 字节、采矿专题 14253 字节均在各自上限内。最终 Editor 空闲、未播放/未编译，Console 当前 0 Error；第九阶段人工 GamePlayer 为 UNKNOWN。

## 2026-10-04 地图资源交互统一 F

- 用户确认统一 F 调整方案后，由主线程执行。新增 CombatPrototypeMapInteractionSystem 与 CombatPrototypeMapInteractionTargetSelector，按 NetworkId 升序跨三类筛选最近有效目标，精确同距取小 PlacementIndex，选中后立即预约；本 tick 已交互玩家不能再启动。
- PlayerInput 保留 Gather/HarvestTree/Mine 等原字段布局，F 复用 Gather，停止 H/J 触发与消费。Gather/TreeHarvest/MineHarvest 提供指定目标 TryBegin 与部分预约 CancelBegin，移除独立按键选择及旧 F/H 优先；原计时/中断、采集保存、木材/石材完成事务、再生/阻挡保持。
- 两个新脚本 meta 由 Unity 正常导入生成。正常 Unity 编译无 C# Error，已反射核对两个脚本、三类启动/清理入口、顺序特性及八个输入字段；未执行游戏回调或进入 PlayMode。
- 增量同步地图/树木/采矿/掉落/库存/玩家/战斗/资源文档、导航和项目外策划模板；运行入口新增八项统一 F 人工清单，旧 H/J 阶段清单注明原版本范围，人工 GamePlayer 保持 UNKNOWN。
- 本轮未修改地图 JSON、Scene/Prefab/Animator、既有 meta、包/构建设置或存储类；未新增/运行逻辑单元测试、GamePlayer/PlayMode、游戏系统、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交 Git。
- 相对本轮开始快照，15 个既有文本文件修改、4 个新脚本/meta 及策划模板变化均在范围内，无其他资源变化。三类 Complete/Cancel/FindCollector 方法逐字保持，RejectPlayer 仅删除旧类型优先，八个输入字段/顺序保持；UTF-8、相对文件链接、GUID 唯一性及受影响文件 git diff --check 通过。导航 8190 字节，Map 24574 字节；新交互人工仍为 UNKNOWN。

## 2026-10-04 地图资源统一 F 人工验收反馈

- 用户明确反馈“我已验收通过，接下来下一个阶段”；主线程结合统一 F 阶段已有静态核对与用户反馈判定该阶段通过，范围限 Runtime 统一 F 八项人工清单及 v6/revision=8。
- 人工结论来自用户反馈；未实际触发的临界距离、精确同距、同 tick、创建/清理/回滚或保存等独立失败、延迟/预测回放时序仍为 UNKNOWN。第九阶段原 J 清单及第二阶段独立 JSON 人工结果仍为 UNKNOWN，既有通过仍限各自原版本和清单；性能、平台构建与线上联调未验收。
- 增量同步地图、树木、采矿、掉落、背包、玩家、战斗、资源与 Runtime 的验收状态，以及项目外策划模板；导航和全部代码、JSON、资源及 Scene/Prefab/Animator 保持。本次仅记录已收到的人工反馈，未运行 Unity 编译、GamePlayer/PlayMode、游戏系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交 Git。

## 2026-10-04 矿点原点再生

- 用户选择矿点原点再生并确认完整执行方案；主线程按范围落地，未创建子Agent。沿原 objects.mine_rock 再生字段接通 true/600 秒，Json 与 BuiltIn 同步为 schemaVersion=6/configRevision=9；矿点/生态/空间/种子、2 米/3 秒/stone ×3 和原其他配置保持，关闭再生仍耗尽。
- MineSettings/Progress 增加仅服务端再生配置/期限，成功石材创建与耗尽提交才保留 RegrowAt；取消或提交失败清空期限并回滚历史长度/障碍，既往轮次历史保留。矿点 Baker/Spawn 初始化空阻挡历史，新增 MineBlockingEvent 的 TransitionTick/Disabled Ghost 缓冲、内部容量 4；MineObstacle 按预测 tick 严格之前最近转换重建阻挡，耗尽/恢复从下一 tick 生效，本局全部历史不截断。
- 新增独立 MineRegrowSystem，服务端在 MineHarvest 后/R 复活前按模拟时间处理到期矿点，检查全部存活玩家/敌人（含未启用 Simulate 的存活玩家），默认占位阈值 1.16/1.21 米且包含边界，死亡实体不占位。占位保持耗尽/原期限等待；空闲恢复同一矿点/Ghost/布置/位置/朝向、Available/显示/阻挡，失败尝试还原原状态/期限/历史/障碍并逐项隔离。再生不生成石材、改库存或写盘，须新 F 再次采矿，G 保存与本局生命周期沿原链。
- 正常 Unity 编译无 C# Error，新系统/仅服务端字段/顺序及 MineState、MineBlockingEvent Ghost Serializer 已加载核对。Forest/Grassland 的 Json/BuiltIn 各一次、另各一次 Json mining=false 和 regrowEnabled=false（600 秒），共八次隔离 Editor 烘焙通过；全部初始布置位置/朝向一致，森林/草原仍为矿点 20/18、阻挡 109/71、树木 89/53、采集点 36/38，关闭采矿为 0 矿点和 89/53 阻挡。空间违规为 0，Prefab 空历史/零期限、配置/资源键一致；烘焙 Console 前后均 [0 Error,3 Warning,7 Log]，无新增烘焙警告，原场景干净、未播放，无临时 World 遗留。
- 增量同步 MapMining 当前规则、地图/树木/掉落/背包/玩家/战斗/资源文档、导航及策划模板；Runtime 新增矿点再生八项人工清单，统一 F 的原 v6/revision=8 已通过清单保留原口径，第九阶段原 J 与第二阶段独立 JSON 人工 UNKNOWN 保持。当前矿点再生主线程代码/烘焙静态验收通过，人工 GamePlayer、未触发独立时序/失败、历史内存/网络开销、规模性能、平台构建与线上联调为 UNKNOWN。
- 只修改方案内矿点链/配置和受影响文档；新系统 meta 由 Unity 导入生成，GUID=9da8de47253e52f4ba3b80983082bcc3。Scene/Prefab/Animator 层级、旧 meta/资源、统一 F/G/E/R 输入链、树木/采集物系统、存储类、包与构建设置保持。AI 未新增/运行逻辑单元测试、GamePlayer/PlayMode、游戏模拟/显示系统、命令行构建、发布、性能采样或图片检查，未提交 Git。
- 最终范围核对：相对本阶段开始快照，22 个既有文件变化（8 脚本、3 JSON、11 文档），新增 1 脚本及 Unity 生成的 meta，另同步策划模板，无删除/越界。旧 Scene/Prefab/Animator/资源/meta 及原 F/G 输入与树木/采集系统散列保持；MineHarvest 的 TryBegin/CancelBegin/RejectPlayer/FindCollector/Cancel 原方法及统一 F 原八项人工清单逐字保持。25 份任务文本 UTF-8、496 个本地链接、JSON/策划示例一致性、GUID 唯一性及受影响文件 git diff --check 通过；导航 8192 字节、地图 24500 字节，矿点专题在 24 KiB 内。加载元数据确认状态 Snapshot 四字段及历史 Snapshot 两字段，临时烘焙 World 已释放；矿点再生人工 GamePlayer 仍为 UNKNOWN。

## 2026-10-04 矿点原点再生人工验收反馈

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有静态核对与该人工反馈判定矿点原点再生阶段通过，范围限 CombatPrototypeNetCode、schemaVersion=6/configRevision=9 及 Runtime 矿点再生八项清单。人工结果来自用户反馈，AI 未运行 GamePlayer/PlayMode 或游戏模拟/显示系统。
- 同步地图、采矿、掉落、背包、玩家、战斗、资源与运行入口的当前验收状态及外部配置模板；保留原八项清单和恢复默认配置要求。未实际触发的精确距离/特殊 Simulate、同 tick/延迟/预测回放、晚加入与独立创建/提交/清理/回滚失败分支仍为 UNKNOWN；历史内存/网络开销、规模性能、平台构建和线上联调未验收。
- 第二阶段独立 JSON、第九阶段原 J 等旧人工 UNKNOWN 及其他阶段原版本/清单保持。此次仅同步验收文档，未改脚本、JSON、Scene、Prefab、Animator、meta 或导航；未新增/运行逻辑单元测试、命令行构建、发布、性能采样或图片检查。

## 2026-10-04 资源交互提示与进度显示

- 用户确认并授权后由主线程按方案执行。新增 HUD JSON 配置、所属玩家快照、服务端只读状态采样、客户端绑定与 OnGUI 显示五个独立脚本及 Unity 生成的 meta；原 Player Baker 初始 Hidden，Map Baker 烘焙 Settings，统一 F 仅开放原 RejectPlayer 的只读接口。HUD 共用 Select，读取原 FinishAt/模拟时间和 1/2/3 秒耗时，SendToOwner 同步 Mode/Kind/PlacementIndex/ProgressPermille；不提交原玩法、库存、掉落或存档。
- 两份地图 JSON 与 BuiltIn 更新为 schemaVersion=7/configRevision=10，新增必填 interactionHud 九字段；默认 true、320×76、底距48、字号20、条高10及三类英文文案，严格尺寸/标签校验，旧 v1～v6 明确失败。其他 JSON 数值与共享数组保持，关闭 HUD 只清显示。
- 在授权主场景 Main Camera 一次追加一个 HUD 组件并保存，fileID=329441056、脚本 GUID=55daeba957df67347a415a043d00bd70；三根对象、原四个 Camera 组件和 Prefab/Animator/旧 meta/其他资源保持。只清掉保存产生的两个受影响 m_Name 行尾空格。
- 正常 Unity 编译无 C# Error，所属 Serializer 四字段和顺序已核对。森林/草地各覆盖 Json/BuiltIn 默认与 Json 关闭采矿/矿点再生/HUD，共十次隔离 Editor 烘焙，初始 HUD、引用、全部布局及空间约束通过；默认矿点20/18、阻挡109/71，Console 前后 [0 Error,9 Warning,53 Log]，无新增烘焙警告。临时烘焙 Scene/World 已释放，主场景干净。
- 增量同步导航、地图与相关模块，新增交互显示专题、Runtime 八项人工清单及外部配置模板；先前矿点再生人工通过记录保持原 v6/revision=9 范围。HUD 实际显示/字形/分辨率、跨端/预测/取消/生命周期人工 GamePlayer 与性能/带宽、平台构建、线上联调为 UNKNOWN；未运行游戏系统、PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交 Git。

## 2026-10-04 资源交互提示与进度显示人工验收反馈

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有静态核对与该人工反馈判定资源交互提示与进度显示阶段通过，范围限 CombatPrototypeNetCode、schemaVersion=7/configRevision=10 及 Runtime HUD 八项清单。人工结论来自用户反馈，AI 未运行 GamePlayer/PlayMode 或游戏模拟/显示系统。
- 同步交互显示专题、地图、玩家、战斗、掉落、背包、资源与运行入口的当前验收状态及外部配置模板；保留 HUD 八项清单、恢复默认配置要求和各旧阶段的原版本/清单。未实际触发的精确距离/同距、同 tick、延迟/预测回放、晚加入和独立保存/创建/提交/清理/回滚失败仍为 UNKNOWN，中文字体/字形覆盖未确认；性能、带宽、平台构建和线上联调未验收。
- 此次仅同步九份项目文档与一份外部模板，未改脚本、JSON、Scene、Prefab、Animator、meta 或导航；未新增/运行逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交 Git。

## 2026-10-04 采集工具与耐久按确认方案落地

- 在CombatPrototypeNetCode接入石斧/石镐，数字1/2单次制作、F按目标自动使用；默认耐久60/40、成本1、耗时倍率0.75、配方木3石2/木2石3，无工具仍徒手1/2/3秒。制作先保存扣料/满耐久候选，工具完成先保存耐久；取消不扣、损坏记录保留、R/重连不补满。
- 新增五个职责脚本及Unity生成meta；地图Json/BuiltIn改为v8/revision=11和必填gatherTools，玩家工具/反馈SendToOwner，实际耗时锁定仅服务端。沿原Main Camera HUD增加第二行和2秒制作反馈，面板320×104；未改Scene/Prefab/Animator/旧meta/其他资源或包/构建设置。
- PlayerSave写v2/Tools，严格读取v1并只在内存迁移为Tools空，不赠工具、不立即或批量重写；奖励/E/F植物/G候选保留Tools，固定路径与正式档替换保持。保存前完成失败清理当前掉落；保存成功后的意外ECS异常明确暴露，不以旧档补偿。
- 正常Unity编译无C# Error，新Serializer/输入/仅服务端字段/系统声明顺序及五脚本导入已静态核对；十次隔离Editor烘焙覆盖两模板Json/BuiltIn及关闭采矿/工具/HUD，v8/11、工具/初值/HUD参数一致，布局/旧资源绑定保持、空间违规0，Console前后[0 Error,2 Warning,0 Log]，无新增烘焙警告，原场景干净且临时World释放。
- 增量同步[工具专题](../Modules/MapGatherTools.md)、受影响模块、导航、外部配置建议模板与Runtime新12项人工清单。主线程静态验收通过；本阶段GamePlayer/PlayMode人工结果为UNKNOWN，既有通过仅保留各自旧版本/清单。未运行游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git；同步存档耗时/性能、带宽、字体、平台、线上及文件替换后意外ECS故障仍未验证。

## 2026-10-04 用户确认采集工具与耐久阶段人工验收通过

- 用户明确反馈“我已验收通过”。主线程结合此前代码/编译/烘焙静态核对与本次反馈判定阶段通过，范围限 CombatPrototypeNetCode、schemaVersion=8/configRevision=11、玩家存档写v2/读取v1迁移及运行入口工具十二项清单。
- 增量同步采集工具、运行入口、地图、交互HUD、树木、采矿、掉落、背包、玩家、战斗、资源与数据及项目外配置建议模板的验收状态；保持原事实、配置、验收编号和旧阶段通过范围。
- 人工结论来自用户反馈；未实际触发的精确边界、同tick、延迟/预测回放、晚加入及独立配置/创建/准备/保存/提交/清理/回滚失败仍为UNKNOWN。同步保存耗时/性能、带宽、字体、平台构建、线上联调及文件替换成功后的意外ECS故障恢复仍未验证。
- 本次仅同步文档，未修改脚本、JSON、Scene、Prefab、Animator、资源/meta、导航或包/构建配置；AI未执行GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent、未提交Git。

## 2026-10-04 材料背包与制作面板落地

- 用户选择该方向并确认执行；主线程按确认范围落地，未创建子Agent。四个新脚本分别负责inventoryPanel DTO、地图固定Settings、库存只读投影、GUI绘制/按钮；对应新meta由正常Unity导入生成，原Main Camera HUD复用原挂载。
- 两份地图Json与BuiltIn为schema9/revision12、必填25个inventoryPanel字段；默认开启/初始关闭、380×640/右24/上64/字号18/行32及17个英文文案，严格完整字段/类型、几何和文案校验，旧v1～v8失败、不补默认/回退或热重载。原gatherTools、interactionHud及布局数值保持。
- B本地开关，原库存正数量/原顺序滚动列表、两工具耐久和原配方/缺少材料预览；按钮条件只读原材料/工具/开关，沿原1/2事件、服务端资格、SavePrepared与反馈。面板内左键/镜头滚轮隔离，面板外和键盘操作保持，打开不暂停；显示开关独立，生命周期/关闭清掉未提交请求与旧显示。
- 正常Unity编译无C# Error，四脚本导入、普通展示类、烘焙25字段及原输入生成类型已静态核对；十四次隔离Editor烘焙覆盖两地图Json/BuiltIn及关闭采矿/工具/F HUD/面板/两种显示，schema9/revision12、配置/玩家初值一致，原布局与引用保持、空间违规0，Console前后[0 Error,7 Warning,67 Log]，无新增烘焙警告，主场景干净、临时World/Scene释放。
- 增量同步面板专题、相关模块、导航、配置建议模板与Runtime新十二项清单。主线程静态验收通过；本阶段GamePlayer人工结果UNKNOWN，旧工具v8/11及HUD v7/10等通过保持各自原范围。未修改Scene/SubScene、Prefab、Animator、旧meta、资源绑定、全部服务端玩法/存储链、原Ghost/输入字段、图片/字体、包或构建配置；AI未运行PlayMode、游戏模拟/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未提交Git。字体、运行性能/带宽、平台/线上及独立失败/预测用例保持UNKNOWN。

## 2026-10-04 用户确认材料背包与制作面板阶段人工验收通过

- 用户明确反馈“我已验收通过”。主线程结合此前代码/编译/十四次隔离Editor烘焙静态核对与本次反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=9/configRevision=12及运行入口面板十二项清单；玩家存档写v2/读取v1迁移边界保持。
- 增量同步材料面板专题、运行入口、地图、交互HUD、背包、玩家、战斗、资源与数据、掉落、树木、采矿及项目外配置建议模板的验收状态；原配置、事实、全部验收编号和旧阶段通过范围保持。
- 人工结论来自用户反馈；未实际触发的精确排版/命中、事件顺序、同tick、延迟/预测回放、晚加入及独立配置/网络条目/创建/准备/保存/提交/清理失败仍UNKNOWN。字体、运行性能/带宽、平台/线上及保存成功后意外ECS故障恢复仍未验证。
- 本次只同步文档，未修改脚本、JSON、Scene、Prefab、Animator、资源/meta、导航或包/构建配置；AI未执行GamePlayer/PlayMode、游戏模拟/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 背包物品丢弃与地面掉落落地

- 用户确认后由主线程按方案执行；新增inventoryDrop DTO、Settings/定义/所属反馈、服务端事务及普通客户端帮助类四个职责脚本，正常Unity导入生成meta。两份地图JSON和BuiltIn升为schema10/revision13，必填单次1/All开启/反馈2秒、苹果/木材/石材原资源映射和六文案，关闭仍严格校验，无热重载。
- 原B面板材料行接入Drop/All、单个待提交请求和所属结果；原指针隔离、连接/本地玩家复核和关闭/死亡/断线/源/World释放清理保持。PlayerInput增加丢弃事件/稳定物品码/模式三字段，Player Baker增加所属Sequence/Kind/Quantity/Result。原制作、F/G/E/R及正式Map链保持。
- 服务端复用原资格/预约互斥，同tick原操作优先；PrepareItemConsumption后创建Prepared，重取引用、SavePrepared成功才扣原库存并激活。DropPhase追加Prepared=3、Prefab初值Prepared/隐藏/不可拾取；原敌人/树木/矿点成功生成默认Airborne，复用原DropId/运动/G/到期/所有权。保存前失败仅释放当前准备态；文件替换后意外ECS故障不宣称完整回滚。世界掉落不保存，未拾取物重启消失且扣减已保存。
- 正常Unity编译、13输入字段/新反馈Serializer、两模板/两来源及六种配置变体共16次隔离Editor烘焙静态通过，设置/原Prefab/玩家初值/默认布局符合契约；主场景干净、临时World/Scene已释放，原SubScene正常重新导入。导入过渡有两条旧程序集未知inventoryDrop字段异常，完成编译后严格读取/烘焙成功，Console前后[2 Error,2 Warning,0 Log]未新增错误/警告，保留原日志；两条编译警告为未修改第三方/工具文件。
- 增量同步丢弃专题、相关模块、导航、项目外配置建议模板和Runtime十二项人工清单；主线程静态验收通过，GamePlayer人工UNKNOWN。原面板v9/12、工具v8/11、HUD v7/10及其他用户通过保持原版本/清单，全部旧验收编号保持。未修改Scene/SubScene、Prefab、Animator结构、旧meta、资源绑定/图片/字体、存储类、包/构建配置；AI未运行PlayMode、游戏模拟/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 用户确认背包物品丢弃与地面掉落人工验收通过

- 用户明确反馈“我已验收通过”。主线程结合此前代码/编译/16次隔离Editor烘焙静态核对与本次反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=10/configRevision=13及运行入口丢弃十二项清单；玩家存档v2/v1迁移及本局地面掉落边界保持。
- 增量同步丢弃专题、运行入口、地图、背包、材料面板、工具、交互HUD、掉落、树木、采矿、玩家、战斗、资源与数据及项目外配置建议模板的验收状态；原参数、配置示例、全部人工验收编号与旧阶段通过范围保持。
- 人工结论来自用户反馈；未实际触发的布局/命中、事件顺序、同tick、延迟/预测回放、多玩家/晚加入及独立配置/创建/准备/保存/提交/清理失败仍UNKNOWN。未拾取地面物重启消失、已保存扣减不返还保持；文件替换后意外ECS故障恢复、字体、运行性能/带宽、平台/线上未验证。
- 本次只同步文档，未修改脚本、JSON、Scene、Prefab、Animator、旧meta/资源绑定、导航、包或构建配置；AI未执行GamePlayer/PlayMode、游戏模拟/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 掉落物拾取提示与目标显示落地

- 按用户确认范围在CombatPrototypeNetCode接入G物品名/实际数量文字提示；五新职责脚本及Unity正常导入meta，原Main Camera HUD/绑定委托独立帮助类，不新增MonoBehaviour挂载或Scene/SubScene/Prefab/Animator结构。
- Forest/Grassland Json与BuiltIn升级schemaVersion=11/configRevision=14，必填pickupHud九字段，默认true/400×52/底168/字号20及Pick up、Vitality Apple、Wood、Stone；严格字段/类型/几何/F间隔16与61UTF-8字节文案校验，关闭仍验证、旧v1～v10不迁移或补段/回退。
- 原G资格及最近Landed/未到期/XZ距离/同距DropId选择抽为共用只读入口，结算、请求排序/次序及SavePrepared先于库存/Consumed保持；服务端在PlayerRespawn后采样，仅写Mode/DropId/ItemId/Quantity所属快照。原13输入、F四字段、丢弃反馈与v2/v1存储保持；三显示开关独立，生命周期失效清缓存，新提示不控制输入、产出、运动/到期或保存。
- 正常Unity编译无C# Error，新Serializer/SendToOwner与类型、十次隔离Editor烘焙静态通过；两模板/两来源、G关闭、F/面板关闭但保留G、三者全关的设置/玩家初值/原Prefab及布置一致。编译前Console[0 Error,5 Warning,48 Log]，新增两条未修改第三方/工具代码警告；烘焙前后[0 Error,7 Warning,48 Log]，无新增烘焙错误/警告，旧运行警告保留且未清Console，主场景干净、临时World/Scene/TextAsset释放。
- 增量同步新专题、相关模块、导航、项目外配置模板与Runtime十项人工清单，原147项人工编号/内容及旧版本用户通过范围保持。主线程静态验收通过，新显示GamePlayer人工UNKNOWN；未修改旧meta/资源绑定/图片/字体/网格/材质、包/构建设置或存储类。AI未运行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 用户确认掉落物拾取提示与目标显示人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”。主线程结合此前代码/编译/十次隔离Editor烘焙静态核对与本次反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=11/configRevision=14及运行入口拾取提示十项清单；人工结论来自用户反馈。
- 增量同步拾取提示专题、运行入口、地图、背包、掉落、丢弃、材料面板、工具、F交互HUD、树木、采矿、玩家、战斗、资源与数据及项目外配置建议模板的验收状态。参数、全部人工验收编号/内容与旧阶段通过范围保持；未实际触发的独立配置/快照/保存/网络/同tick/预测/字形与布局用例仍UNKNOWN。
- 本次只同步文档，未修改脚本、JSON、Scene、Prefab、Animator、meta/资源绑定、导航、包或构建配置。字体覆盖、运行性能/带宽、平台/线上及原保存成功后意外ECS恢复未验证；地面物本局不存档、重启消失且不返还已保存扣减边界保持。
- AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 资源交互目标高亮落地

- 按用户确认范围在CombatPrototypeNetCode复用原F/G所属四字段目标接入Ready黄圈、Working锁定绿圈和G蓝圈，最多各一圈；读取对应客户端LocalToWorld，经原Main Camera投影到屏幕，G先F后、原面板随后，无真实深度遮挡。
- Forest/Grassland Json与BuiltIn升级schemaVersion=12/configRevision=15，必填interactionHighlight14字段；默认主/F/G开启、半径0.65/0.9/0.9/0.45米、线宽3/48段、#FFD166/#6ED88A/#6EC6FF、透明度0.9/偏移0.03米。沿原严格字段/类型与有限数值/范围/#RRGGBB校验，关闭仍验证，旧v1～v11拒绝，不补段或回退。
- 五新职责脚本及Unity正常生成meta，原Map Baker追加固定Settings；原HUD/绑定委托普通解析/投影/客户端帮助类，Awake读取同对象Camera。只调整原F/G显示采样开关，文字关闭而对应高亮开启仍采样；三文字全关且高亮无启用通道才收起绑定，沿原所属连接/玩家及生命周期清缓存。
- 正常Unity编译无C# Error，五类型/14配置字段、原13输入与F/G各四字段静态核对；两模板/两来源及主/F/G开关、三文字关/高亮开、全部显示关共14次隔离Editor烘焙通过。新Settings/RGB、原初值/反馈/掉落Prefab/Prepared及全部布置位置/朝向一致，Console前后[0 Error,6 Warning,47 Log]无新增错误/警告，保留原日志，主场景干净、临时World/Scene/TextAsset释放。
- 增量同步新专题、相关模块、导航、项目外配置建议模板与Runtime十项人工清单；保留全部原157项编号/内容及旧版本用户通过范围。主线程静态验收通过，新高亮GamePlayer人工UNKNOWN；未改Player Baker、输入/Ghost/RPC、玩法选择/结算/存储、Scene/SubScene/Prefab/Animator结构、旧meta/资源绑定/网格/材质/图片/字体、包或构建配置。AI未执行PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 用户确认资源交互目标高亮人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”。主线程结合此前代码/正常编译/14次隔离Editor烘焙静态核对与本次反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=12/configRevision=15及运行入口高亮十项清单；人工结论来自用户反馈。
- 增量同步高亮专题、运行入口、地图、F/G提示、材料面板、背包丢弃、工具、树木、采矿、掉落、玩家、战斗、资源与数据及项目外配置建议模板的验收状态；参数、配置示例、全部167项人工验收编号/内容及旧阶段通过范围保持。
- 未实际触发的独立排版/投影/缩放、身份/阶段时序、同tick、延迟/预测回放、多玩家/晚加入、生命周期及配置/解析/绘制/保存失败仍UNKNOWN；原屏幕叠加无真实深度遮挡边界保持，运行性能/带宽、平台/线上及旧保存成功后意外ECS恢复未验证。
- 本次只同步文档，未修改脚本、JSON、Scene、Prefab、Animator、meta/资源绑定、导航、包或构建配置；AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 资源状态与再生提示接入

- 用户明确确认资源状态与再生提示方案，主线程按CombatPrototypeNetCode范围实施；新增五个职责脚本和正常Unity生成meta，原地图根追加11字段Settings、原Player Baker追加仅SendToOwner的Mode/Kind/PlacementIndex/RemainingSeconds四字段，Hidden为0/0/-1/0，RemainingSeconds不量化；原13输入及F/G各四字段保持。
- 服务端在原F HUD采样后读取三类资源真实阶段/Collector/RegrowAt，F Ready/Working身份优先，无F目标时按原各类交互距离/XZ最近/同距PlacementIndex选择，包括占用与耗尽资源。显示可用、本人采集中、他人使用中、权威整秒倒计时、到期等待、不再生；允许移动/攻击时只读显示，不改原F/G资格、采集/工具/掉落/保存/再生/占位与阻挡历史。
- 新resourceStatusHud必填true/400×52/底236/字号20及六文案，严格形状/类型、有限值、容纳/G间隔16与61 UTF-8字节校验，disabled仍校验。Json/BuiltIn/草地/森林统一schema13/revision16，旧v1～v12拒绝，无字段兜底/来源回退或热重载。原宿主/绑定接入独立状态行并沿Clear/Reset释放；F采样增加状态开关，状态关闭且其余显示全关才收起绑定。
- 正常Unity编译无C# Error，所属Serializer/四字段/SendToOwner及原F/G/输入布局静态核对通过；Forest/Grassland各7种隔离Editor烘焙共14次，配置等价、新Settings/Hidden初值、原Prefab/反馈与完整布局一致，烘焙Console前后[0 Error,7 Warning,47 Log]无新增错误/警告，主场景干净、临时World/Scene/TextAsset释放。主线程静态验收通过，新增GamePlayer人工仍UNKNOWN。
- 增量同步资源状态专题、受影响地图/显示/工具/树矿/掉落/玩家/战斗/数据/背包文档、导航、运行入口新增十项清单及项目外配置模板；原167项人工内容、旧验收版本/清单与UNKNOWN边界保持。未修改Scene/SubScene/Prefab/Animator结构、旧meta/资源绑定、包或构建配置；AI未运行PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-04 用户确认资源状态与再生提示人工验收通过

- 用户明确反馈“我已验收通过，接下来下一阶段”。主线程结合此前正常Unity编译、所属Serializer与14次隔离Editor烘焙静态核对及本次反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=13/configRevision=16及运行入口资源状态十项清单；人工结论来自用户反馈。
- 增量同步资源状态专题、运行入口及相关地图/显示/工具/树矿/掉落/玩家/战斗/资源与数据文档和项目外配置建议模板的验收状态；修正地图当前参数表残留的schema12/revision15为已确认13/16。全部177项人工验收编号/内容、配置默认值、原导航及旧阶段通过范围保持。
- 未实际触发的独立倒计时/到期等待/再生关闭精确边界、距离/身份/输入时序、移动攻击显示、多玩家/晚加入/生命周期及配置/快照/绘制/保存失败仍UNKNOWN；字形、运行性能/带宽、平台/线上及旧保存成功后意外ECS恢复未验证。
- 本次只同步文档，未修改脚本、JSON、Scene/SubScene、Prefab、Animator、meta/资源绑定、包或构建配置；AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 采集工具修理与耐久恢复接入

- 用户明确确认修理方案，主线程在CombatPrototypeNetCode新增四个职责脚本及正常Unity生成meta；原工具配置/烘焙数据接入repairEnabled=true/repairFeedbackSeconds=2及斧头恢复20、镐子15、每次木1石1；inventoryPanel新增Repair/Repair/Full durability三文案，共28字段。Json/BuiltIn/两模板统一schema14/revision17，旧v1～v13严格拒绝，关闭仍校验，无补默认/来源回退或热重载。
- 新服务端RepairSystem位于Craft后、Drop/F/Respawn前，沿原所属/存活/静止/无攻击/非预约资格；F/G/E/R/1/2请求优先于修理，3/4同时斧头优先，Drop将两修理请求加入原优先拒绝判断。只修理已持有未满工具，0可修复、封顶恢复且近满仍扣完整材料；复用原PrepareToolCraft候选与SavePrepared，先保存再提交材料/耐久，失败不自动重试、保存前不改原状态。
- 原PlayerInput新增RepairAxe/RepairPickaxe，共15实例字段；原Player Baker追加零Sequence/Kind/Result修理反馈，仅SendToOwner。新增普通反馈/面板类委托原Main Camera宿主与滚动面板，3/4与B按钮消费一次，预览材料缺口与封顶恢复；结果默认2秒，页脚丢弃/修理/制作优先，原F第一行/进度保持，生命周期清缓存/未提交请求。
- 正常Unity编译无C# Error，所属Serializer及三反馈/15输入/F/G/资源状态各4/工具定义11/Settings4/面板28字段静态核对通过；Forest/Grassland各9种隔离Editor烘焙共18次，新Settings/定义/全28面板值/玩家零反馈和原Prefab/初值/完整布局一致，Console前后[0 Error,8 Warning,47 Log]相同、原主场景干净、临时World/Scene/TextAsset释放。主线程静态验收通过，新GamePlayer人工UNKNOWN。
- 增量同步修理专题、相关模块/导航、项目外配置模板及Runtime新增十二项；原177项人工编号/内容、旧通过版本/清单和UNKNOWN边界保持。原SaveStore/存档格式、制作结算、F资源/工具完成、G/掉落生成/生命周期、Scene/SubScene/Prefab/Animator结构、旧meta/资源引用、包/构建配置均不在本阶段修改；执行期间敌人动画脚本/Prefab等并行差异保留且不计入本次验收。AI未执行PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 网络敌人腐化荒猪美术与战斗动画实装

- 按用户已确认方案与一次性资源权限，由主线程为 CombatPrototypeNetCode 现有敌人生成腐化荒猪设定图，并制作独立低面数关节模型。设定图于 2026-10-04 使用内置 image_gen 生成，1536x1024、非透明、无参考图片；运行模型位于 Assets/Art/Enemy/CombatPrototype/，含 11 份网格、8 份 URP/Lit 材质、11 个 MeshRenderer、3286 个三角形。
- 新增单层 EnemyCombat.controller 与七个 60 fps/84 曲线 Clip：待机 1.6、移动 0.7、蓄力 0.5、顶击 0.12、收势 0.88、受击 0.16、侧倒 0.75 秒。无 Animation Event、自动过渡或 Root Motion；新增新资源采样预览 2080x720，设定图/预览 NPOT=None，预览 Max Size=4096。
- 两份既有敌人 Prefab 保留根与原组件，Ghost ClientPrefab 绑定 EnemyView，新增 VisualRoot 嵌套模型于 (0,-1,0)，关闭原占位 Renderer 并序列化绑定 Animator/动画组件。既有 GUID、Scene/SubScene 结构、Bundle、包和构建设置未改。
- 新增表现状态、服务端状态复制系统与客户端动画组件，修改既有 Enemy Authoring/View/Render/Attack 脚本。权威攻击配置/状态继续仅服务端保留；正常前摇结束设置 SwingStarted，原复活取消前摇不设置该标记。复制系统在 PlayerRespawn 后同步阶段、序号、标记与剩余时间，客户端正常后摇前 12% 播放顶击，取消时只退回蓄力姿态；原前摇结束伤害、追踪、体力、奖励和存档链保持。
- 客户端受击由原 HitSequence 驱动，死亡播放侧倒后隐藏，晚加入直接隐藏已死敌人；保留原死亡实体与统计。表现只读取 Ghost 状态并写模型关节/可见性，完成官方 Transform 桥接依赖后再读取根位置；没有新增敌人复活或客户端伤害事件。
- Unity 编译、生成的表现 Ghost Serializer、七状态、全部曲线目标、缺失脚本为 0、无事件与静态接地采样已核对。现有 SubScene 的隔离 Editor 烘焙确认 32/8列/间距3、实际 EnemyPrefab 标签/LocalTransform/两实体 LinkedEntityGroup、新表现初态及 ClientPrefab，原 HP100、伤害10、范围1.75、前摇0.5/后摇1保持；主场景仍干净，临时 World/Scene 已释放。主线程判定代码和资源静态落地通过。
- 增量同步 EnemyArt、战斗、运行入口、资源与数据、性能边界、总导航与本月 ChangeLog，仅检查本次新设定图与新 Clip 预览。工作区工具修理任务的并行改动保留，不计入本次修改与验收。人工 GamePlayer 的双端动画/光照/脚步、攻击取消、死亡隐藏、晚加入/重连及原战斗回归仍 UNKNOWN；未执行逻辑单元测试、PlayMode、命令行构建、发布或性能采样，未创建子Agent或提交 Git。

## 2026-10-05 采集工具修理与耐久恢复人工验收通过

- 用户反馈“我已验收通过”，主线程结合既有Unity编译/Serializer及18次隔离Editor烘焙静态核对，判定修理阶段通过；范围限CombatPrototypeNetCode、schemaVersion=14/configRevision=17及Runtime十二项人工清单，结论来自用户反馈。
- 增量同步修理专题、运行清单、相关模块/导航和项目外配置模板的验收状态；189项人工编号/内容、旧通过版本/清单及未实际触发的独立失败/时序/网络/生命周期、字形、性能/带宽、平台/线上、跨文件/ECS故障恢复UNKNOWN保持。
- 本次只修改文档，不改代码、配置、Scene/Prefab/Animator或资源；敌人美术与动画等并行工作保留。AI未运行逻辑单元测试、GamePlayer/PlayMode、游戏模拟/GUI回调、构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 地图资源状态存档接入

- 按用户确认方案接入CombatPrototypeNetCode的独立世界资源v1存档；地图Json/BuiltIn升级schema15/revision18，新增必填resourcePersistence=true/default_world/10秒，旧v1～v14拒绝，原数值/布局保持。
- 七个新职责脚本及Unity生成meta、原地图根Settings/恢复状态与准入门落地；资源生成后整体校验/恢复，离线暂停、预约/进度清空，树/矿重建本次阻挡基态。状态变化/检查点/关闭独立保存，严格身份/签名/UTF-8/JSON与.tmp/Flush(true)/原子替换；坏档拒绝准入，写失败保留旧档并下个保存点重试，不回滚原结算。
- 原资源/工具/修理/玩家SaveStore/G及掉落脚本、15输入/原Ghost字段、Scene/SubScene/Prefab/Animator、旧meta/资源引用、包/构建配置保持。玩家写v2/读v1迁移，地面掉落不存档；跨玩家/世界文件事务、同槽多服务端协调未接入。
- 正常Unity编译/实际Assembly字段反射和Forest/Grassland各9种隔离Editor烘焙共18次通过，新四Settings/恢复初值/签名兼容及完整原布局/反馈/Prefab一致；Console前后[1 Error,4 Warning,4 Log]相同，既有Error为UnityConnect网络错误，无新编译/烘焙错误，主场景干净、临时对象释放。
- 增量同步资源存档专题、相关模块/导航、外部配置模板及Runtime新增十二项，原189项内容/编号与用户通过边界保持。主线程静态验收通过，新GamePlayer/I-O/坏档/关闭恢复/占位预测/跨文件ECS/多人并发生命周期/性能带宽平台线上UNKNOWN；AI未运行真实存档读写、游戏/显示系统、GUI回调、逻辑单元测试、PlayMode、构建、发布、采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 地图资源状态存档人工验收通过

- 用户反馈“我已验收通过”，主线程结合既有Unity编译/字段反射及18次隔离Editor烘焙静态核对，判定资源存档阶段通过；范围限CombatPrototypeNetCode、schemaVersion=15/configRevision=18、世界资源v1及Runtime十二项人工清单，人工结论来自用户反馈。
- 增量同步资源存档专题、运行清单、相关模块/导航和项目外配置模板；全部201项人工编号/内容、旧阶段通过版本/清单保持。未实际触发的独立I/O/坏档/替换/恢复/占位预测/多人时序/生命周期，以及跨文件/ECS、同槽并发、字形/性能/带宽/平台/线上UNKNOWN保持。
- 本次只修改文档；AI未运行真实存档读写、GamePlayer/PlayMode、游戏模拟/GUI回调、逻辑单元测试、构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 地面掉落物存档与恢复接入

- 按用户确认方案接入CombatPrototypeNetCode地面苹果/木材/石材及背包丢弃物持久化；Json/BuiltIn升级schema16/revision19，新增必填saveGroundDrops=true，原default_world/10秒及其他值保持，旧地图v1～v15拒绝。
- 五个新职责脚本及Unity正常生成meta、原配置/Baker、世界DTO/Store/Restore/Save及DropSpawn/SpawnUtility调整。世界同文件写v2九根字段/八字段掉落条目，合法v1严格读原字段并在内存补空掉落，下一正常保存升级；严格身份/位置/期限/绑定校验、原文件原子替换保持。
- 只保存已提交未到期物及编号上限，排除Prepared/Consumed/清理队列；飞行物保存原落点、恢复Landed，离线暂停TTL，永久物保持。玩家准入前完整恢复并登记原所有权，失败清理本批并Failed；资源/掉落双完整缓存同时交换，关闭只读缓存，世界写失败不回滚原结算，跨文件重复物品风险明确保留。
- 正常Unity编译与实际字段反射通过，Forest/Grassland各12种隔离Editor烘焙共24次通过；Settings5/配置4/世界根9/掉落8，原输入15/DropGhost4及原布局/Prefab/反馈保持。Console前后[0 Error,1 Warning,0 Log]相同，主场景干净、临时对象释放。Scene/SubScene/Prefab/Animator、旧meta/资源绑定/图片字体材质、包/构建配置与原G/结算/清理保持。
- 增量同步新掉落存档专题、相关模块/导航、项目外配置模板及Runtime十二项清单，原201项编号/内容及旧用户通过保持原范围。主线程静态验收通过，新GamePlayer/真实I-O/迁移重启/飞行TTL/多人生命周期/独立失败/跨文件ECS/性能平台线上UNKNOWN；AI未运行真实存档读写、游戏/显示系统、GUI回调、逻辑单元测试、PlayMode、构建、发布、采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 地面掉落物存档人工验收通过

- 用户确认地面掉落物存档与恢复人工GamePlayer验收通过；主线程结合既有代码/配置静态核对与用户反馈判定通过，范围限CombatPrototypeNetCode、地图v16/revision19、世界v2及Runtime对应十二项清单，结论来自用户反馈。
- 同步掉落/资源存档专题、地图/背包/数据入口、相关模块边界、AI导航和项目外配置建议模板的验收状态；保留原213项编号/内容及更早通过范围。未触发独立失败、跨文件防重复/原子一致、意外ECS恢复、同槽并发、未覆盖字形与性能/带宽/平台/线上保持UNKNOWN。
- 本次只更新文档及UTF-8/链接/体量/差异静态检查；未修改脚本、JSON、Scene/SubScene/Prefab/Animator、meta、资源或构建配置，未读取图片或真实存档，未执行PlayMode/游戏系统/GUI回调、逻辑单元测试、编译/烘焙、构建、发布、采样，未创建子Agent或提交Git。

## 2026-10-05 掉落物寿命提示与到期预警接入

- 按已确认范围接入CombatPrototypeNetCode G目标寿命第二行；两个普通C#助手负责实际ExpiresAt投影与文案/预警绘制，meta由Unity正常导入。原G选择/拾取、生成/运动/到期清理、玩家/世界存档、输入及HUD宿主/绑定保持。
- 两地图Json与BuiltIn升级schema17/revision20，pickupHud由9变17必填字段：寿命true、预警true、30秒、四文案/秒单位及#FFB454；关闭仍严格校验。G高度52改84，资源状态底距236改268，保持16像素间隔；新字段经原Baker写入17Settings。
- 所属G显示由4变6：LifetimeMode及无量化RemainingSeconds，生成Serializer/Snapshot已实际反射核对。基于目标服务端实际余时、向上取整秒数与未取整预警边界；永久为Permanent/0，Hidden或关闭为None/0。原15输入、F/资源状态各4、Drop Ghost4及存档v2格式/路径和合法v1读取保持，各端须同版重新烘焙。
- 正常Unity编译无C# Error；Forest/Grassland各13种隔离Editor烘焙共26次通过，配置/Settings/六字段初值、原布局/签名/三个掉落Prefab及反馈保持。隔离烘焙Console前后[0 Error,0 Warning,0 Log]一致，主场景干净、临时对象释放；同步相关专题/入口/策划模板，追加12项人工清单，原213项保留后共225项。
- 主线程代码/配置静态验收通过，新增人工GamePlayer及实际倒计时/边界/永久物/恢复余时、布局/字形/颜色、独立失败、多玩家/延迟/生命周期/性能/带宽/平台/线上UNKNOWN。原用户通过保持旧版本/清单；Scene/SubScene/Prefab/Animator、旧meta、资源/包/构建配置未改，未运行游戏/GUI回调、逻辑单元测试、PlayMode、构建、发布、采样或图片检查，未读取真实存档、创建子Agent或提交Git。


## 2026-10-05 掉落物寿命提示与到期预警人工验收通过

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有代码/配置静态核对、正常Unity编译、实际所属Serializer反射和26次隔离Editor烘焙，判定寿命显示阶段通过，限CombatPrototypeNetCode、schemaVersion=17/configRevision=20及Runtime对应十二项清单。人工结论来自用户反馈，AI未运行GamePlayer/PlayMode。
- 同步G提示主文档、Runtime、相关模块、导航和策划模板的通过状态；原225项人工清单编号/内容及旧阶段版本范围保持。未实际触发的独立边界、失败、多玩家/延迟/生命周期与性能/平台仍UNKNOWN；玩家与世界跨文件原子一致、防重复和意外ECS恢复不在通过范围。
- 本次仅修改验收文档；代码、JSON、Scene/SubScene/Prefab/Animator、meta/资源引用、包及构建配置保持。未执行逻辑单元测试、构建、发布、游戏/GUI回调、真实存档读写、采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 地图存档状态提示与手动保存接入

- 按已确认方案由主线程接入CombatPrototypeNetCode F5 SaveWorld，输入15→16；新普通请求助手执行所属资格、模拟时间全局冷却5秒与同tick请求合并。移动/攻击/采集预约允许，死亡/归属/连接/Simulate与Ready规则沿确认边界。
- 原SaveSystem接受请求后当前tick完整捕获，手动与原自动触发仅一次写入；捕获失败不能用旧缓存完成手动请求，真实SavePrepared成功返回才Saved/Success。原变化/10秒检查点/关闭缓存保存、世界v2路径/9根/4资源/8掉落条目、合法v1读取及玩家v2保存链保持。
- 两地图JSON及BuiltIn升级schema18/revision21，resourcePersistence配置4→6/Settings5→7，手动true/全局冷却5秒；新增worldSaveHud16必填字段，默认true/400×84/底336/字号20/反馈3秒/9文案/#FF6B6B。独立显示开关，关闭仍严格校验，资源状态上方间隔16；未增加热重载或新RPC。
- 五新职责脚本/meta由Unity正常导入，Player增加Mode/ManualSequence/ManualResult所属3字段/Hidden初值，原HUD宿主/绑定委托新普通缓存类绘制。第一行当前会话保存结果，第二行本人序号变化结果；初次绑定不重播。原F4/G6/资源状态4、DropGhost4与资源签名保持，各端同版重新烘焙。
- 正常Unity编译无C# Error，实际Assembly与生成Serializer/Snapshot字段核对通过；Forest/Grassland各13种隔离Editor烘焙共26次通过，两来源/新设置/初值/原布局/签名/Prefab/反馈一致。Console前后[2 Error,1 Warning,0 Log]不增加：2条为接入中提前刷新旧Forest缺manualSaveEnabled的历史错误，补齐升级后重新烘焙成功；1条MCP WebSocket警告。未清Console，主场景干净，临时对象释放。
- 同步F5主文档、模块/导航/策划模板和Runtime，原225项人工清单完整保留后追加12项，共237项。编译/字段/配置烘焙静态通过，新增人工GamePlayer及真实写盘、冷却/同tick/多人/延迟/晚加入、失败/关闭恢复、布局/字形/颜色/生命周期/性能/平台仍UNKNOWN；旧用户通过限原版本/清单。跨文件事务/防重复、同槽并发及意外ECS恢复未新增保证。
- Scene/SubScene/Prefab/Animator、旧meta、资源引用、图片/字体/材质、包及构建配置保持；原玩家存储与世界DTO/Store/Restore、资源/掉落结算及清理未改。未运行逻辑单元测试、GamePlayer/PlayMode、游戏/显示系统/GUI回调、真实存档读写、构建、发布、采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 地图存档状态提示与手动保存人工验收通过

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有代码/配置静态核对、正常Unity编译、实际所属Serializer反射与26次隔离Editor烘焙，判定本阶段通过，限CombatPrototypeNetCode、schemaVersion=18/configRevision=21及Runtime对应十二项清单。人工结论来自用户反馈，AI未运行GamePlayer/PlayMode。
- 同步F5专题、Runtime、相关模块、导航与策划模板的通过状态；原237项人工清单编号/内容及旧阶段版本范围保持。未实际触发的独立资格/冷却/同tick/多人/延迟/晚加入、捕获/保存/关闭恢复失败、布局/字形/颜色/生命周期与性能/平台仍UNKNOWN；跨文件原子一致、防重复、意外ECS恢复及同槽并发仍不在通过范围。
- 本次仅修改验收文档；代码、JSON、Scene/SubScene/Prefab/Animator、meta/资源引用、包及构建配置保持。未执行逻辑单元测试、构建、发布、游戏/GUI回调、真实存档读写、采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 材料背包容量与拾取限制

- 按用户已确认方案，由主线程在CombatPrototypeNetCode接入inventoryCapacity：默认总量300、vitality_apple/wood/stone各200，每件计1；保留原聚合库存及小块肉/Tools/金币/经验规则。新增四个配置/数据/判定脚本及Unity正常导入meta。
- 两地图JSON与BuiltIn升级schema19/revision22；容量根3字段/条目2字段，Map根Settings2字段/Definition3字段×3。F/G各新增noSpaceLabel，B新增capacityLabel/unlimitedLabel，配置/Settings分别10/18/30；全部严格必填，旧v1～v18拒绝，关闭仍校验。
- 统一F植物预约前与Gather完成前、G选定最近目标后检查实际库存；整批通过才沿原SavePrepared后提交。拒绝不部分入包/转选/消耗地面物，完成拒绝释放植物为Available；砍树/采矿满包仍产地面物。旧合法超限库存准入/保存保持完整，降低上限同样限制新入包，减少操作沿原规则。
- F四字段追加NoSpace=3、G六字段追加NoSpace=2，保留目标/寿命，适配原高亮/资源状态；B追加总量滚动行/单种上限，显示关闭不关闭权威容量。16输入、保存所属3字段、资源/掉落Ghost、存档格式/路径与原Scene/Prefab/Animator及旧meta保持。
- 正常Unity编译无C# Error；最终辅助方法、所属生成Snapshot4/6字段与SendToOwner已反射。Forest/Grassland各9种配置，共18次隔离Editor Bake：默认Json/BuiltIn、关闭容量、自定义上限/顺序/文案、F/G/B独立关闭及全显示关闭组合；新值/初值及旧参数符合，原区块/格子/布置/障碍/资源签名逐项保持。Bake Console前后[0,8,74]，无新增Bake警告；两条未修改PEListener/DOTween编译警告和六条既有运行警告按当时快照记录，末次反射Console[0,6,74]；临时World/Scene释放，主场景干净。
- 已增量同步地图/背包/显示/数据/玩家等受影响文档、导航与策划模板，新增MapInventoryCapacity主专题及Runtime十六项人工清单；旧237项保留，合计253项。本阶段静态通过，人工GamePlayer UNKNOWN；旧用户通过保持原版本/清单。未执行逻辑单元测试、GamePlayer/PlayMode、游戏/显示系统/GUI回调、真实存档I/O、命令行构建、发布、性能/图片检查、子Agent或Git提交。


## 2026-10-05 材料背包容量与拾取限制人工验收通过

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有正常Unity编译、所属Snapshot/SendToOwner反射及18次隔离Editor Bake静态核对，判定本阶段通过，限CombatPrototypeNetCode、schemaVersion=19/configRevision=22及Runtime材料容量十六项清单。人工结论来自用户反馈，AI未运行GamePlayer/PlayMode。
- 增量同步容量专题、Runtime、地图/背包/数据、导航与策划模板的验收状态；原253项编号/内容及旧阶段通过范围保持，模板F5通过版本校正为其已验收v18/revision21。未实际触发的独立边界/并发/保存失败、显示/字体/生命周期仍UNKNOWN，性能/平台及跨文件原子一致/意外ECS恢复不在通过范围。
- 本次只更新验收文档，代码、JSON、Scene/SubScene/Prefab/Animator、meta/资源引用、包和构建配置保持。未执行逻辑单元测试、构建、发布、游戏/GUI回调、真实存档读写、性能采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 背包容量扩展与升级

- 按用户确认范围接入个人Lv1/2/3、B/数字5一次请求、完整配方先保存后扣料/提交及所属反馈；F/G/B按实际等级选容量，关闭升级保留已有上限。
- 地图schema20/revision23新增必填inventoryCapacityUpgrade；玩家Version3新增InventoryCapacityLevel，合法v1/v2仅内存迁移Lv1，原九保存入口保留当前等级。六个普通脚本及Unity正常生成meta；原Scene/SubScene/Prefab/Animator、旧meta/资源/包/构建配置保持。
- Unity编译0 Error，新输入17/生成所属Serializer与配置元数据核对、两地图共96份非法配置拒绝、22次隔离Editor Bake通过；原布局/签名保持，Bake Console[0,8,113]前后一致、主场景干净。
- 同步容量升级专题、调用链文档、导航、运行清单及策划模板；旧253项逐字保留，新增16项后269项，升级人工GamePlayer UNKNOWN。AI未运行游戏/显示回调、PlayMode、逻辑单元测试、真实存档I/O、构建/发布、采样、图片、子Agent或Git提交。

## 2026-10-05 背包容量升级人工验收

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有编译、所属Serializer/配置静态核对、96份非法配置拒绝及22次隔离Editor Bake，判定通过，限CombatPrototypeNetCode、schemaVersion20/configRevision23与Runtime升级十六项；人工结论来自用户反馈，AI未运行GamePlayer/PlayMode。
- 同步升级专题、调用链摘要、导航和策划模板的验收状态，保留269项内容/编号、旧阶段通过范围及未实际触发用例UNKNOWN；按已确认的现行v3存储契约修正工具/修理专题旧v2文字，工具条目仍为ToolId/Durability两字段。
- 本次只改文档，代码、JSON及Scene/SubScene/Prefab/Animator/meta/资源/包/构建配置保持；未执行游戏/GUI回调、真实存档I/O、逻辑单元测试、构建/发布、性能采样或图片检查，未创建子Agent/提交Git。

## 2026-10-05 采集工具升级与效率提升

- 按已确认范围接入每把工具Lv1～Lv3、B/6/7一次请求、所属三字段反馈和有效最大耐久/耗时；升级保留绝对耐久，修理按本级封顶并保级，重做明确满Lv1，原F/G/资源产出与再生保持。旧操作优先，复用完整候选先保存后扣料/提交。
- 地图schema21/revision24新增必填gatherToolUpgrade根11字段/四条6字段定义；工具缓冲三字段、输入19，玩家Version4/根7/Tools项3。合法v2/v3工具只内存补Lv1，v3容量等级保持；所有候选保存两类等级，读取不写盘。六新普通脚本及Unity自动meta，原Scene/SubScene/Prefab/Animator、旧meta/资源/包/构建保持。
- Unity编译0 Error，生成所属Serializer/字段、136份非法配置拒绝及22次隔离Editor Bake通过；原布局/资源签名保持、Bake Console[0,8,113]前后一致、主场景干净，临时对象释放。
- 增量同步专题/调用链、导航、策划模板和Runtime22项人工清单；原269项逐字保留，共291项。静态核对通过，人工GamePlayer UNKNOWN；旧用户通过限原版本/清单。未执行逻辑单元测试、PlayMode、游戏/GUI回调、真实存档I/O、构建/发布、采样/图片、子Agent或Git提交。

## 2026-10-05 采集工具升级与效率提升人工验收

- 用户反馈“我已验收通过，接下来下一阶段”；主线程结合既有编译、所属Serializer/配置核对、136份非法配置拒绝及22次隔离Editor Bake，判定通过，限CombatPrototypeNetCode、schemaVersion21/configRevision24与Runtime二十二项清单；人工结论来自用户反馈，AI未执行GamePlayer/PlayMode。
- 增量同步工具升级专题、调用链摘要、导航和策划模板的验收状态；保留291项内容/编号、旧通过范围以及未实际触发的独立边界/并发/保存失败、延迟/预测及显示/生命周期用例UNKNOWN。
- 本次仅更新验收文档，代码、JSON与Scene/SubScene/Prefab/Animator/meta/资源/包/构建配置保持；未执行游戏/GUI回调、真实存档I/O、逻辑单元测试、构建/发布、采样或图片检查，未创建子Agent或提交Git。

## 2026-10-05 采集工具耐久预警与损坏提示

- 按已确认范围接入原F/B只读耐久投影：25% Low黄、10% Critical橙，不足一次成本优先Broken红；按实际工具等级上限计算，Durability/Level变化刷新。F工具状态着色与损坏3/4修理提示，B状态标签/颜色及两详情滚动行，Uses整除剩余次数；关闭修理损坏沿原1/2重做，关闭新显示或工具沿原显示/开关边界。
- 地图schema22/revision25新增必填gatherToolDurabilityHud根11字段，Json/BuiltIn一致；严格阈值/颜色/文案及关闭仍校验、旧v1～v21拒绝。新增三个普通脚本及Unity正常生成meta，原工具唯一状态/所属三字段、输入19、F4/G6/资源状态4/世界保存3、玩家v4与世界v2/保存链保持，原Scene/SubScene/Prefab/Animator/旧meta/资源/包/构建保持。
- 正常Unity编译0 Error，配置/Settings11及网络/存储元数据、240份非法配置拒绝和28次隔离Editor Bake静态通过；新11Settings/custom值正确，原布局/资源签名保持。Bake Console[0,7,53]前后一致，编译重报两条已有PEListener/DOTween警告，主场景干净、临时对象释放。
- 增量同步耐久预警专题、当前契约/调用链、导航与策划模板；保留前阶段验收状态及原291项逐字编号/内容，新增16项后307项，人工GamePlayer UNKNOWN。未执行游戏/显示或GUI回调、PlayMode、逻辑单元测试、真实存档业务I/O、构建/发布、采样/图片、子Agent或Git提交。
