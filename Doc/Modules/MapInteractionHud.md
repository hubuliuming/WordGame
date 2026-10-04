# 资源交互提示与进度显示

返回[战斗地图](Map.md)。本专题负责 CombatPrototypeNetCode 的 F 资源提示、权威进度快照和屏幕 HUD；采集/砍伐/采矿、产出、G 拾取、存档及再生仍归原模块，人工清单归[运行入口](Runtime.md)。

## 【FACT】真实入口与职责

| 文件 | 职责 |
|---|---|
| [HUD 配置](../../Assets/Scripts/CombatPrototype/Map/MapInteractionHudConfig.cs) | interactionHud 的九个 JSON 字段 |
| [HUD 数据](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudData.cs) | 地图烘焙显示配置与玩家所属 Ghost 快照 |
| [服务端状态](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudStateSystem.cs) | 在原资源维护/再生与 PlayerRespawn 后读取目标和真实进度，只写 HUD |
| [客户端绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | Presentation 读取本地所属玩家快照并交给显示组件 |
| [HUD 显示](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) | Client World 注册/解绑，缓存文案与样式，OnGUI 绘制固定面板 |
| [F 入口](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionSystem.cs) | GetInteractionHintRejection 只读复用原 RejectPlayer；原 F 执行流程保持 |
| [目标选择](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionTargetSelector.cs) | F 与 HUD 共用同一 Select |
| [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 在原玩家实体追加初始 Hidden HUD 组件 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 在原地图根烘焙 HUD Settings |

[主场景](../../Assets/Scenes/CombatPrototypeNetCode.unity) 的 Main Camera 原四个组件保留，只追加 CombatPrototypeMapInteractionHud，序列化组件 fileID=329441056，脚本 GUID=55daeba957df67347a415a043d00bd70。主场景仍为三根对象；没有新 Canvas、EventSystem、场景根、UI Prefab、字体或图片资源。原网络 Player Prefab 文本/层级保持，Baker 新增 ECS 数据改变玩家烘焙后的 Ghost 布局，各端须使用同版代码、配置与重新烘焙的数据。

## 【FACT】当前 JSON 契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) 与 [BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) 为 schemaVersion=7/configRevision=10，新增必填 interactionHud；全部九个字段必填，沿原严格 UTF-8/字段/类型/重复键检查。旧 v1～v6 明确失败，不补默认段或回退来源；JSON 只在正常导入和烘焙后生效，无运行热重载。

| 字段 | 默认值 | 契约 |
|---|---|---|
| enabled | true | 仅控制显示；false 时服务器快照 Hidden，原 F/G 与资源链继续 |
| panelWidthPixels | 320 | 有限，32 < 宽度 <= 1920 |
| panelHeightPixels | 76 | 有限正数；高度 >= fontSize + progressBarHeightPixels + 40 |
| bottomMarginPixels | 48 | 有限非负；高度 + 底距 <= 1080 |
| fontSize | 20 | 正整数 |
| progressBarHeightPixels | 10 | 有限正数 |
| gatherLabel | Gather Apple | 非空白、无控制字符、至多 61 UTF-8 字节 |
| treeLabel | Chop Tree | 同上 |
| mineLabel | Mine Rock | 同上 |

即使 enabled=false，其余字段仍按上述契约校验。显示按 1920×1080 参考像素，以 min(屏幕宽/1920,屏幕高/1080) 等比缩放，底部居中；面板黑色背景 alpha=0.7，文字白色，进度背景白色 alpha=0.2，填充 RGB=(0.85,0.65,0.3)。使用内置 GUI 样式字体与 Texture2D.whiteTexture，不导入新的字体/纹理；当前英文文案，中文文案字段可配置，但中文字体与实际字形覆盖未确认。

## 【CURRENT STRATEGY】权威采样与本地显示

服务端 HUDStateSystem 在 PredictedSimulation、PlayerRespawn 后执行；原采集/砍伐/采矿及其再生均在复活前完成。每次读取原地图和三类资源，仅为 Connected、InGame、未请求断线、CommandTarget 指向当前启用 Simulate 玩家的连接采样。可交互条件调用原 RejectPlayer：所有权匹配、存活、有限零 Move、无攻击请求且近战 Ready。非法归属连接不覆盖真实所属玩家快照。

本人正在 Collecting/Chopping/Mining 时固定使用原 Collector 实体对应目标，不因其他资源更近而切换。进度由服务端模拟时间和原 FinishAt、原对应 duration 计算 round(clamp(1-(FinishAt-now)/duration,0,1)×1000)。默认三类耗时为 1/2/3 秒；不另起计时器、不提前完成、不改变原取消或完成行为。

空闲时复用同一 Select：各类型原交互距离筛选，只选 Available/Standing/Available，按 X/Z 中心距离最近、同距较小 PlacementIndex；关闭砍伐/采矿不选相应类型。提示本身不预约资源，按 F 时仍由原当 tick 选择/预约。移动、攻击、死亡等不可交互条件隐藏；收到取消/完成后的状态后清掉进度，可重新显示下一可用目标或隐藏。提示与进度仅代表最近收到的权威状态，网络延迟下可能滞后；Working 的百分比不是发奖或保存成功标志。

CombatPrototypeMapInteractionHudState 用 OwnerSendType=SendToOwner 同步给所属玩家：

| 字段 | 声明与含义 |
|---|---|
| Mode | byte 枚举：0 Hidden、1 Ready、2 Working |
| Kind | byte：0 None、1 Gather、2 Tree、3 Mine |
| PlacementIndex | int；Hidden 为 -1，其余是原布置索引 |
| ProgressPermille | ushort，0～1000；非 Working 为 0 |

状态仅含显示数据；FinishAt、RegrowAt、Collector 和玩法配置仍仅服务端。HUD 不写原资源 Phase/Collector/历史/障碍、玩家输入/属性/库存、掉落或存档。原三类资源 Ghost 字段和 F/G/E/R 保持。服务器缓存每 tick 重建，状态字段不变时不重复写组件；关闭 HUD、连接无效、未采样玩家或地图停止时清为 Hidden。

客户端枚举启用的 GhostOwnerIsLocal，不在含该可启用组件的查询上调用单例 API；只显示本地所属玩家且死亡时收起，不显示远端玩家进度。Presentation 不读取 PlayerView 或世界位置，不替代官方 Transform 显示桥接。Main Camera 组件随 Client World 变更注册/解绑；无客户端、准入未完成、没有本地 Ghost 或地图停止时隐藏。World/Scene 释放清掉显示和引用，不保留上一局目标/进度。

Ready 显示“F  文案”；Working 显示“文案  百分比%”和按千分比填充的进度条，整数百分比为 ProgressPermille/10。OnGUI 仅 Repaint 绘制，不接按钮、鼠标或按键；缓存文案和样式，绘制后恢复 GUI.matrix/color。HUD 不显示掉落拾取提示、库存面板、世界标记或再生倒计时。

资源采样错误按单项暴露地图、类型、布置索引、实体和原异常并继续；玩家帧错误按连接隔离，未生成有效快照者清为 Hidden。必需服务、地图 Settings、HUD 挂载或本地 HUD 数据缺失明确报错，不查找节点、不创建替代组件或默认配置；非法非隐藏网络快照明确报错并保持隐藏。

## 【KNOWN ISSUES】静态核对与人工边界

正常 Unity 编译无 C# Error；生成的 HUD Serializer/Snapshot 四字段、SendToOwner、系统顺序及五个脚本导入已静态核对。Forest/Grassland 各覆盖 Json 默认、BuiltIn 默认、Json 关闭采矿、Json 关闭矿点再生（仍 600 秒）、Json 关闭 HUD，共十次隔离 Editor 烘焙；v7/revision=10、HUD 参数、玩家 Prefab 初始 Hidden/Kind=0/PlacementIndex=-1/进度=0 均一致。默认森林/草原仍为矿点 20/18、阻挡 109/71、树木 89/53、采集点 36/38；HUD 关闭不改布局，采矿关闭恢复原四类布局。全部初始布置位置/朝向、资源引用、保护区/占地/间距及敌人初始重叠均通过。烘焙 Console 前后 [0 Error,9 Warning,53 Log]，无新增烘焙警告；主场景已保存，临时烘焙 World/Scene 已释放。

主线程静态验收通过；实际 HUD 画面/字体/分辨率、三类进度、双端所属显示、延迟/预测时序、输入/争抢/取消/生命周期与配置人工 GamePlayer 为 UNKNOWN，八项清单归[运行入口](Runtime.md)。既有矿点再生、统一 F 与各旧阶段用户通过保持原版本/清单，不覆盖 HUD。性能/带宽开销、平台构建和线上联调未测量。AI 未运行 GamePlayer/PlayMode、游戏模拟/显示系统、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交 Git。
