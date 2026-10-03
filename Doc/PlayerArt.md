# 网络原型 Player 美术与动画

[返回总导航](AI_Understanding.md)。本页负责当前网络原型的角色外观、资源绑定、动画表现及其验收边界；生命、复活归[玩家](Modules/Player.md)，权威攻击时序归[战斗](Modules/Combat.md)。

## 【FACT】角色与资源

角色为已确认的灰衣短刀修士，采用末世玄幻与风格化手绘设定方向，固定特征为旧皮护肩、破损符袋、短刀及少量暗朱红。实装采用分部件低面数 Transform 关节模型，设定图的衣料与面部细节已简化；设定图本身只作美术参考，不是运行时角色贴图。

| 资源 | 当前用途 |
|---|---|
| [GrayRobeCultivatorConcept.png](../Assets/Art/Player/CombatPrototype/GrayRobeCultivatorConcept.png) | 正面、侧面、背面的角色设定图 |
| [GrayRobeCultivator.prefab](../Assets/Art/Player/CombatPrototype/GrayRobeCultivator.prefab) | 角色显示模型、Animator 与动画驱动组件 |
| [Meshes](../Assets/Art/Player/CombatPrototype/Meshes) / [Materials](../Assets/Art/Player/CombatPrototype/Materials) | 23 份网格、11 份 URP/Lit 材质；模型实例有 79 个 MeshRenderer、2584 个三角形 |
| [PlayerCombat.controller](../Assets/Art/Player/CombatPrototype/Animations/PlayerCombat.controller) | Base Layer 与 Upper Body 两层表现控制器 |
| [PlayerUpperBody.mask](../Assets/Art/Player/CombatPrototype/Animations/PlayerUpperBody.mask) | 只启用 Rig/Hips/Torso 及其后代的 62 个 Transform 路径 |
| [PlayerAnimationPreview.png](../Assets/Art/Player/CombatPrototype/PlayerAnimationPreview.png) | 静态 Clip 采样预览；不是 GamePlayer 运行截图 |

[CombatPrototypeNetworkPlayerView.prefab](../Assets/Prefabs/CombatPrototype/CombatPrototypeNetworkPlayerView.prefab) 保留根 Transform、原 MeshFilter/MeshRenderer 和 GhostPresentationGameObjectEntityOwner，根 MeshRenderer 当前关闭。新增 VisualRoot 为上述角色 Prefab 的嵌套实例，局部位置为 (0,-1,0)，用于对齐原网络根节点 Y=1 的站立位置。显示根仍由官方 GhostPresentationGameObjectTransformSystem 写位置与朝向，动画只写角色内部关节。

玩家 Ghost 的原 ClientPrefab 仍引用同一 PlayerView，GUID 为 4622076924452ab429a8cbb99799bb0e，ServerPrefab 仍为空。Animator 显式绑定 PlayerCombat.controller，Apply Root Motion=false，Culling Mode=AlwaysAnimate；模型动画组件的 characterAnimator 与 View 组件的 playerAnimation 均有实际序列化引用。

## 【FACT】动画资源与参数

所有 Clip 均为 60 fps 采样资源，含 161 条 Transform 曲线；没有 Animation Event。

| 状态 | Clip | 时长 | 循环 |
|---|---|---|---|
| 待机 | Player_Idle.anim | 1.6 秒 | 是 |
| 移动 | Player_Move.anim | 0.7 秒 | 是 |
| 攻击前摇 | Player_Startup.anim | 0.18 秒 | 否 |
| 命中动作 | Player_Active.anim | 0.08 秒 | 否 |
| 攻击后摇 | Player_Recovery.anim | 0.3 秒 | 否 |
| 受击 | Player_Hit.anim | 0.16 秒 | 否 |
| 死亡倒地 | Player_Death.anim | 0.75 秒 | 否 |

Base Layer 含 Idle、Move、Death；Upper Body 含 Empty、Startup、Active、Recovery、Hit，使用 Override 与上身遮罩，默认权重为 0。控制器没有参数或自动状态过渡，状态与上身权重由已绑定的表现组件写入。走路关键帧按脚部网格采样调整 Hips 高度；待机不通过 Hips 升降移动脚底。

## 【CURRENT STRATEGY】表现调用链

[CombatPrototypePlayerNetCodeView](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeView.cs) 挂在 PlayerView 根节点。Start 从既有 Owner 取得实际 World/Entity 和该 World 的官方 Transform 桥接系统；LateUpdate 读取当前玩家的 CombatPrototypeMeleeState、CombatPrototypePlayerHealth，完成桥接作业后读取显示根位置，再调用 [CombatPrototypePlayerAnimation](../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerAnimation.cs)。该链只读 ECS 状态，作用于当前 View 所属玩家，也覆盖其他玩家的客户端显示。

动画组件按水平显示位移切换 Idle/Move，变化门槛为 0.02 单位/秒，切换混合为 0.1 秒。首次采样及死亡到存活的复活跳转不作为走路步进。上身仅按收到的 Startup/Active/Recovery 播放挥刀；攻击序号变化允许重新开始对应动作，Ready 将上身权重降为 0。下肢移动持续由 Base Layer 表现，攻击动画不锁定移动。

受击序号发生变化时播放一次 0.16 秒上身受击；死亡优先于受击和攻击。死亡时播放倒地并保持最后姿态，原实体及显示根保留；新观察者首次看到已死亡玩家时直接显示最终倒地姿态。复活时清除受击表现、关闭上身动作并恢复 Idle/Move，不重放旧受击或攻击；首次绑定也只建立当前序号基线。

实体销毁或 World 释放后的 View 清理间隙停止读取。必需组件、Animator 和序列化引用不通过动态节点、默认配置或递归搜索补齐。表现链不写生命、体力、位置、伤害、奖励、库存或存档；被拒绝的攻击输入不会单独触发挥刀，伤害仍由原服务端阶段查询生成。

## 【KNOWN ISSUES】验收与边界

- 主线程已确认脚本编译/类型加载、资源引用、两层状态、上身遮罩、全部曲线目标存在及缺失脚本为 0，判定代码和资源静态验收通过。
- 已按用户一次性授权查看本次新生成的角色与 Clip 预览；没有读取项目原有图片。预览是隔离 Editor 预览场景的资源采样，不执行 GamePlayer、业务系统或存档操作。
- `UNKNOWN`：人工 GamePlayer 下的双端动作、实际光照和视野比例、走路脚部接触与动作衔接、死亡/复活/断线重连回归，尚无用户人工通过反馈。
- PhaseTimer 不属于现有 GhostField；客户端动作从观察到对应阶段时开始，精确跨端时间对齐尚未验收。一次显示帧收到多个受击序号增量时只播放一次受击表现。
- 角色为分部件低面数首版；79 个 MeshRenderer 的运行开销尚未采样，不构成规模性能通过结论。正式 Map 玩家、美术生产规格、平台构建与发布仍不在本原型验收范围内。

## 设定图生成规格

- 模板版本：1.0，图片编号 Player-Concept-20261003-01；生成日期 2026-10-03（Asia/Shanghai）。
- 生成工具：内置 image_gen；transparent_background=false，请求与实际源图均为 1536x1024，浅灰非透明背景。
- 设定图与预览导入均设置 NPOT=None，保留源图比例；预览尺寸为 1600x1100。
- 输入参考图片：无。对应项目对象为 CombatPrototypeNetworkPlayerView 的灰衣修士造型参考，运行资源为上表中的独立 3D 模型。
- 固定体型、衣装、装备、三个辨识特征、光照与色彩；三个视图只改变观察朝向。禁止额外角色、文字、水印、裁切及遮挡角色的特效。
- 项目通用模板见 [CombatImagePromptTemplate.md](CombatImagePromptTemplate.md)。本角色资源规格只适用于当前网络原型。

### 提交的完整提示词

Use case: stylized-concept. Asset type: review-only Player character design sheet for the existing Code_01 3D melee game prototype. This is a concept reference, not a finished rigged 3D asset. Create one clean landscape character turnaround sheet, requested 1536x1024, with three evenly spaced full-body views of the SAME survivor cultivator: front, right profile, back. Plain warm light-grey parchment-like background, subtle grounding shadow, no scene. All views same scale and neutral relaxed standing pose, entire head and feet visible, ample gaps and margins. Character: a lean adult survivor cultivator wearing repaired ash-grey coarse-cloth tunic and separated trouser legs, old leather shoulder guard on the right shoulder, worn talisman pouch at the waist, rusty short knife held in the right hand, sparse dark vermilion cloth accents. These three signature features — old leather shoulder guard, damaged talisman pouch, rusty short knife — must remain consistent across all views. Post-apocalyptic Chinese fantasy and wilderness survival theme, stylized hand-painted materials with simple angular low-poly-friendly shapes, readable silhouette suitable for an elevated 3/4 game camera. Simple joint-friendly limb separation, about 6 heads tall, practical clothing, charcoal boots, hair tied back. Palette: muted ash-grey, charcoal, bone-white and aged brown leather, small dark vermilion accent, restrained weathering, no bright glow. Subject/background value separation, broad painted shading with limited tiny detail, face understated. One character design shown in the three prescribed views, no additional characters, no captions, no lettering, no UI, no watermarks, no crop, no fog, no photorealism, no sprawling robes obscuring the legs, no exaggerated giant weapon. Reference images: none. 本次已确认外观为灰衣短刀修士、旧皮护肩、破损符袋、暗朱红点缀；请保持造型与装备一致。
