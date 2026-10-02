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
