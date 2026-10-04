# 网络原型敌人美术与动画

[返回总导航](AI_Understanding.md)。本页负责 CombatPrototypeNetCode 的敌人外观、显示资源、动画驱动及验收边界；权威攻击、伤害与奖励归[战斗](Modules/Combat.md)，入口与运行验收归[运行入口](Modules/Runtime.md)。

## 【FACT】造型与资源

敌人为已确认的腐化荒猪，固定辨识特征是弯曲骨白獠牙、破裂背部骨甲与少量暗朱红腐化裂纹。设定图采用末世玄幻、荒野求生、风格化手绘方向；实装为独立制作的分部件低面数 Transform 关节模型，兽皮纹理与骨甲破损细节已简化。设定图不作为运行贴图。

| 资源 | 当前用途 |
|---|---|
| [CorruptedBoarConcept.png](../Assets/Art/Enemy/CombatPrototype/CorruptedBoarConcept.png) | 正面、侧面、背面的造型参考 |
| [CorruptedBoar.prefab](../Assets/Art/Enemy/CombatPrototype/CorruptedBoar.prefab) | 运行显示模型、Animator 与动画组件 |
| [Meshes](../Assets/Art/Enemy/CombatPrototype/Meshes) / [Materials](../Assets/Art/Enemy/CombatPrototype/Materials) | 11 份网格、8 份 URP/Lit 材质；每个模型 11 个 MeshRenderer、3286 个三角形 |
| [EnemyCombat.controller](../Assets/Art/Enemy/CombatPrototype/Animations/EnemyCombat.controller) | 单层七状态控制器 |
| [Animations](../Assets/Art/Enemy/CombatPrototype/Animations) | 七个 Animation Clip |
| [EnemyAnimationPreview.png](../Assets/Art/Enemy/CombatPrototype/EnemyAnimationPreview.png) | 隔离 Editor 场景中采样新 Clip 的静态预览，不是 GamePlayer 截图 |

[CombatPrototypeNetworkEnemy.prefab](../Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkEnemy.prefab) 保留原根层级、组件、Ghost 配置及数值，ClientPrefab 绑定既有 [CombatPrototypeNetworkEnemyView.prefab](../Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkEnemyView.prefab)，ServerPrefab 为空。两份资源的原根 MeshRenderer 均关闭；原 MeshFilter 与材质引用保留。

EnemyView 保留根 Transform、GhostPresentationGameObjectEntityOwner 与原 View 组件；新增 VisualRoot 为腐化荒猪模型的嵌套实例，局部位置 (0,-1,0)，抵消现有地图角色根的 Y=1。View 的 enemyAnimation、模型组件的 characterAnimator 均有明确序列化引用。官方 GhostPresentationGameObjectTransformSystem 写显示根的位置与朝向，动画只写模型内部关节。Animator 的 Apply Root Motion=false，Culling Mode=AlwaysAnimate。

## 【FACT】动画资源

所有 Clip 的 frameRate=60，每个含 84 条 Transform 曲线，目标覆盖 Rig、Body、Head、四足上下关节与 Tail 共 12 个 Transform；没有 Animation Event。常量曲线保留首尾关键帧。

| 状态 | Clip | 时长 | 循环 |
|---|---|---|---|
| 待机 | Enemy_Idle.anim | 1.6 秒 | 是 |
| 四足移动 | Enemy_Move.anim | 0.7 秒 | 是 |
| 抬头蓄力 | Enemy_Startup.anim | 0.5 秒 | 否 |
| 獠牙顶击 | Enemy_Strike.anim | 0.12 秒 | 否 |
| 收势 | Enemy_Recovery.anim | 0.88 秒 | 否 |
| 受击 | Enemy_Hit.anim | 0.16 秒 | 否 |
| 侧倒死亡 | Enemy_Death.anim | 0.75 秒 | 否 |

Base Layer 含 Idle、Move、Startup、Strike、Recovery、Hit、Death，无控制器参数和自动过渡；状态由动画组件写入。移动时采用对角足步进。关键帧采样时校正模型最低点的高度；这项静态接地检查不代表运行中的脚步滑动或镜头效果已经通过。

## 【CURRENT STRATEGY】服务端表现状态

[CombatPrototypeEnemyAnimationState](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyAnimationState.cs) 保存同步的 Phase、AttackSequence、SwingStarted、PhaseRemaining、StartupSeconds、RecoverySeconds；三个浮点字段使用 Quantization=1000。Baker 从原攻击配置初始化 0.5 秒前摇、1 秒后摇，阶段与序号初始为 Ready/0，SwingStarted=0。

权威 CombatPrototypeEnemyAttackConfig 与 CombatPrototypeEnemyAttackState 继续只保留在服务端。AttackState 新增 SwingStarted 标记：开始前摇时为 0，正常前摇结束、进入后摇时为 1，敌人死亡取消时归零；正常空击也进入顶击表现。原玩家复活取消旧前摇的路径不会设置该标记，因此取消攻击与实际挥击可以区分。

[CombatPrototypeEnemyAnimationSyncSystem](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyAnimationSyncSystem.cs) 仅在服务端预测模拟组、PlayerRespawn 后复制当前阶段、序号、挥击标记与不小于 0 的剩余时间到表现组件。它包含原复活锁定清理结果，不改变攻击目标、命中条件、伤害、奖励、存档或权威阶段推进。

权威阶段仍为 Ready → Startup → Recovery → Ready。顶击 Clip 占用原 Recovery 的前 12%，其余 88% 播放收势；当前配置对应 0.12＋0.88 秒，没有新增权威命中阶段。伤害仍在原前摇结束点判定，不由顶击 Clip、Animator 或 Animation Event 触发。

## 【CURRENT STRATEGY】客户端表现

[CombatPrototypeEnemyNetCodeView](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyNetCodeView.cs) 在 Start 取得本 View 的 Owner、World/Entity 与官方 Transform 桥接系统。LateUpdate 读取该敌人的同步生命和表现状态，完成桥接依赖后，以显示根水平位移判定 Idle/Move，再调用 [CombatPrototypeEnemyAnimation](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyAnimation.cs)。首次位置采样不作为移动，阈值为 0.02 单位/秒，待机与移动切换混合 0.1 秒。

攻击姿态按同步剩余时间定位；同一采样之间只推进本地姿态时间，抵达阶段末后等待服务端新阶段。正常 Recovery 映射顶击与收势；被取消的 Recovery 从已观察到的蓄力姿态沿 Startup Clip 退回待机，不播放 Strike。首次绑定只建立受击序号基线，不补播旧受击。

死亡优先于受击与攻击，受击从 HitSequence 变化触发 0.16 秒表现，随后恢复当前同步阶段的姿态。存活到死亡的变化播放 0.75 秒侧倒，随后关闭 VisualRoot；显示根和死亡实体保留，不改变 HP、存活/死亡统计。新观察者首次绑定到已死亡敌人时直接隐藏，不重播死亡。没有新增敌人复活。

[CombatPrototypeEnemyRenderSystem](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypeEnemyRenderSystem.cs) 对带官方 GameObject 表现引用的敌人保持根实体渲染关闭，避免重复显示；没有此引用的旧实体渲染路径仍按 IsDead 控制。当前敌人根 Renderer 关闭后，隔离烘焙结果不含 MaterialMeshInfo，实际显示由 EnemyView 模型承担。

World 释放或实体销毁后的清理间隙停止读取；必需依赖没有默认配置、动态节点或递归搜索兜底。所有动画仅作用于显示资源，不写玩家/敌人的生命、世界根位置、体力、伤害、奖励、库存或存档。

## 【KNOWN ISSUES】验收边界

- 主线程已核对当前 Unity 编译与表现组件生成的 Ghost Serializer、资源绑定、七状态/Clip、全部曲线目标及缺失脚本为 0，判定代码与资源静态落地通过。
- 已在隔离 Editor 环境完成资源采样和现有 SubScene 静态烘焙。Spawner 的敌人 Prefab 标签、32 个生成配置、8 列/间距 3、LocalTransform、两实体 LinkedEntityGroup、ClientPrefab 及新表现组件均已核对；HP=100、反击伤害=10、范围=1.75、前摇=0.5 秒/后摇=1 秒保持。
- 只检查本次新设定图与新 Clip 预览。预览上排依次为待机、移动、蓄力、顶击，下排为收势、受击、死亡、背面待机；不包含游戏业务系统或 PlayMode 执行。
- UNKNOWN：人工 GamePlayer 下的双端造型与动作、实际场景光照/比例、脚步接触与滑动、攻击时序/出范围空击、复活取消旧攻击、致命受击/死亡隐藏、晚加入及断线重连回归。旧阶段的人工通过范围不自动覆盖本次新显示路径。
- 剩余时间量化到毫秒，并按客户端收到的快照定位姿态；不能据此宣称双端、敌人与玩家生命快照的时序精确一致。单次显示帧的多个 HitSequence 增量仅播放一次受击。
- 每敌人的 Animator/11 个 MeshRenderer、表现同步字段与 32 敌人的实际开销未采样。性能、正式 Map 美术、平台构建与发布保持 UNKNOWN；未运行逻辑单元测试、命令行构建或发布。

## 设定图生成规格

- 模板版本：1.0；编号 Enemy-Concept-20261004-01；生成日期 2026-10-04（Asia/Shanghai）。
- 工具：内置 image_gen；transparent_background=false；请求与实际源图为 1536x1024，浅灰非透明背景；参考图片：无。
- 项目对象：CombatPrototypeNetworkEnemyView 的腐化荒猪造型参考；功能状态为现有敌人的视觉设计。
- 固定体型、獠牙/背甲/裂纹、姿态、比例、光照与配色；三个视图仅改变朝向。禁止额外角色、文字、水印、裁切和遮挡特效。
- 设定图与预览导入为 NPOT=None；预览源图 2080x720，Max Size=4096。运行资源使用独立网格与材质。
- 通用出图模板见 [CombatImagePromptTemplate.md](CombatImagePromptTemplate.md)。

### 完整提示词

```text
Use case: stylized-concept.
Asset type: enemy character turnaround concept reference for Code_01 CombatPrototypeNetCode, image ID Enemy-Concept-20261004-01, template version 1.0. This is the approved new appearance for the existing network enemy, a concept reference for a separately made animated low-poly 3D model.
Primary request: create ONE clean landscape character design sheet of the SAME corrupted wild boar, "腐化荒猪", requested 1536x1024. Three evenly spaced full-body views: front, right profile, back; same scale and neutral quadruped standing pose, entire feet, tusks and back armor visible, generous panel gaps. No words or labels.
Subject: a compact stout wild boar with a strong barrel chest, four distinct short jointed legs and cloven hooves, triangular ears, small deep-set eyes, blunt flat snout, short tail and coarse dark mane. Three signature features fixed across every view: paired upward-curving bone-white tusks, layered cracked bone plates along the upper back and shoulders, and only a few dark vermilion corruption fissures in the hide beneath the plates. Skin is ash-brown and charcoal, armor weathered bone-white, details concentrated at head and dorsal silhouette.
Style/medium: post-apocalyptic Chinese fantasy and wilderness survival, stylized hand-painted material shading, angular low-poly-friendly forms, a readable combat silhouette for an elevated 3/4 game camera. Anatomically readable quadruped, the armor leaves head and limb joints free for animations.
Scene/backdrop: plain warm light-grey parchment-like opaque background with only subtle grounding shadows; no environment.
Composition/framing: front / side / back neutral turnaround, one boar design only; camera at body height, views consistent in scale. Only camera direction changes.
Lighting/mood: restrained diffuse upper-left studio light, clear subject/background value separation, limited fine detail.
Color palette: muted gray-brown, charcoal and aged bone-white, sparse dark vermilion accents; no neon glow.
Materials/textures: coarse painted hide, weathered cracked bone, dark hooves, coarse mane.
Input reference images: none.
Constraints: fixed body proportions, paired tusks, bone-plate layout and red marks across all three views. No humans, weapons, extra creatures, captions, text, UI, watermark, cropped feet, giant antlers, tentacles, fog, blood or gore, photorealism, oversized luminous effects. This sheet is a visual reference and does not itself provide a rigged or animated model.
```
