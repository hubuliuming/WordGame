# 资源交互目标高亮

返回[地图](Map.md)、[F文字与进度](MapInteractionHud.md)、[G拾取提示](MapPickupHud.md)。本专题负责CombatPrototypeNetCode的本地目标圆环、配置、身份解析与投影；人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与职责

| 文件 | 当前职责 |
|---|---|
| [MapInteractionHighlightConfig](../../Assets/Scripts/CombatPrototype/Map/MapInteractionHighlightConfig.cs) | interactionHighlight的14个必填JSON字段 |
| [HighlightData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHighlightData.cs) | 原地图根固定Settings、颜色解码与本地帧；没有GhostField |
| [TargetResolver](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHighlightTargetResolver.cs) | 在原Client World按Kind/PlacementIndex、DropId解析并缓存对应目标实体 |
| [Projection](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHighlightProjection.cs) | 缓存单位圆点，投影/裁剪到Camera视口，绘制后恢复GUI状态 |
| [Client](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHighlightClient.cs) | 缓存两通道帧与Camera；Repaint先G后F，逐通道暴露绘制异常 |
| [原HUD宿主](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 原Main Camera读取同对象Camera，沿原所属连接/玩家绑定，委托解析和绘制 |
| [F采样](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudStateSystem.cs) / [G采样](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapPickupHudStateSystem.cs) | 只调整显示开关条件，原资格、目标与快照结构保持 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 在原地图根追加14项固定显示Settings |

五个新脚本及正常Unity生成的meta已导入；解析、投影、客户端均为普通C#帮助类，没有新MonoBehaviour挂载。原Main Camera已挂Camera/FollowCamera/HUD，宿主Awake读取同对象Camera，不查找或创建替代节点。未修改Scene/SubScene/Prefab/Animator结构、旧meta、资源绑定、网格/材质/图片/字体、包或构建配置。

当前玩家输入15字段，新增修理归[专题](MapToolRepair.md)，F仍Mode/Kind/PlacementIndex/ProgressPermille四字段，G仍Mode/DropId/ItemId/Quantity四字段、SendToOwner；高亮本身不改Player Baker、F/G/RPC或存档；新增所属状态归[资源状态](MapResourceStatusHud.md)。各端须使用同版代码/配置并重新烘焙；配置不参与新增联网版本协商。

## 【FACT】当前JSON契约与建议默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为schemaVersion=14/configRevision=17。interactionHighlight段及14字段必填，沿原严格UTF-8、缺失/未知/重复字段、对象形状及标量类型检查；旧v1～v13明确失败，不迁移、补段或回退来源。正常导入/烘焙后生效，没有运行热重载。

| 字段 | 当前默认值 | 契约/用途 |
|---|---|---|
| enabled | true | 高亮主开关，关闭不画F/G圆环 |
| fTargetsEnabled | true | F通道开关，覆盖采集/砍树/采矿 |
| gTargetsEnabled | true | G通道开关，覆盖原掉落拾取目标 |
| gatherRadiusMeters | 0.65 | 有限，(0,5]；采集黄/绿圈半径 |
| treeRadiusMeters | 0.9 | 同上；树木黄/绿圈半径 |
| mineRadiusMeters | 0.9 | 同上；矿点黄/绿圈半径 |
| dropRadiusMeters | 0.45 | 同上；掉落蓝圈半径 |
| lineWidthPixels | 3 | 有限，[1,8]；1920×1080参考像素 |
| segmentCount | 48 | 整数，[32,96]；闭合圆环线段数 |
| readyColorHex | #FFD166 | 精确#RRGGBB，ASCII十六进制大小写均支持 |
| workingColorHex | #6ED88A | 同上；Working工作中颜色 |
| pickupColorHex | #6EC6FF | 同上；G Ready颜色 |
| opacity | 0.9 | 有限，(0,1]；三颜色共用alpha |
| heightOffsetMeters | 0.03 | 有限，[0,1]；圆环平面相对地图BaseHeight的偏移 |

三个开关false时仍校验全部字段。显示半径不修改资源占地、移动阻挡或F/G交互距离。三色在烘焙时转RGB浮点；透明度单独配置。没有类型默认值兜底、运行客户端配置覆盖或新材质资源。

## 【CURRENT STRATEGY】复用权威目标、读取客户端位置

原服务端快照继续决定身份：F Ready选原最近可交互资源，Working锁定本人原Collector目标，G Ready选原最近可拾取掉落。文字与圆环读取同一份所属快照；解析器不按客户端距离重新选择，不上传目标或新增预约。按F/G仍在原实际处理tick使用原资格与选择/结算链，显示可能因网络延迟滞后。

F按Kind/PlacementIndex查询原GatherState/TreeState/MineState，排除Prefab并缓存实体；每次核对实体仍存在且身份一致。Ready仅接受原Available/Standing/Available，Working仅接受原Collecting/Chopping/Mining且CollectorNetworkId匹配本人。G按DropId解析原DropState，仅接受Landed。准备态/飞行/Consumed不画G圈；到期由原服务端处理和所属Hidden快照反映，客户端不读取仅服务端ExpiresAt。

原所属与资源Ghost可分批到达：目标暂未找到或阶段尚未与所属快照一致时当前帧不画，后续按相同身份继续解析；不猜位置、转选其他实体或保留上一局圈。Hidden/身份变更/实体失效重置对应缓存。已知非隐藏快照的非法模式/身份/Phase、重复身份或必要LocalToWorld缺失均暴露原异常，清空当前通道，不创建默认配置或替代实体。

圆环中心X/Z读取对应客户端目标LocalToWorld.Position，Y=map.BaseHeight+heightOffsetMeters；不读PlayerView、服务端工作计时器或服务器Transform。圆环每次Repaint用原Main Camera投影，跟随该相机旋转/缩放；官方Ghost Transform桥接不被替换。最多一个F圈、一个G圈，允许两者同时显示；G先绘制、F后绘制，原背包/G文字/F文字面板在圆环之后绘制。

## 【CURRENT STRATEGY】投影绘制、开关与释放

在XZ平面缓存segmentCount个单位圆点和闭合终点；每帧复用屏幕点数组。Camera.WorldToScreenPoint后翻转屏幕Y，线段裁剪到Camera.pixelRect并给线宽留边；非有限投影、穿越近/远裁剪面时不画该圈。线宽按min(屏幕宽/1920,屏幕高/1080)缩放。仅EventType.Repaint绘制Texture2D.whiteTexture，恢复GUI.matrix/color；不消费键鼠事件，不绘制倒计时、轮廓描边、发光或闪烁。

圆环属于屏幕投影叠加，没有真实深度遮挡，树木或其他物体前后关系不会裁去圆环；这不是世界网格/材质标记。人工通过范围见[运行入口](Runtime.md)十项清单，未触发的独立投影/缩放边界仍UNKNOWN。

| 条件 | 当前采样/显示行为 |
|---|---|
| interactionHud开启，F高亮关闭 | 原F文字/进度继续采样，F圈关闭 |
| interactionHud关闭，enabled且fTargetsEnabled开启 | 原F四字段继续采样，F文字隐藏、F圈可画；工具行/反馈遵循原文字开关，状态行独立 |
| F文字/F高亮/资源状态均关闭 | F快照清Hidden，原F玩法继续 |
| pickupHud开启，G高亮关闭 | 原G文字继续采样，G圈关闭 |
| pickupHud关闭，enabled且gTargetsEnabled开启 | 原G四字段继续采样，只可显示G圈 |
| G文字与G高亮都关闭 | G快照清Hidden，原G拾取继续 |
| 三文字关闭、无高亮通道且资源状态关闭 | 原整体绑定收起 |

绑定仍只取本地启用GhostOwnerIsLocal、存活玩家及所属Connected/InGame连接，不在该可启用查询上调用单例API。死亡、断线、没有本地玩家/地图、源或玩家变化、World/Scene停止与释放沿原Clear/Reset清圆环、实体缓存和Camera引用，不残留上一局目标。单通道关闭只清该通道；Camera未启用时不绘制。

ResolveF/ResolveG错误分别记录World、地图、NetworkId及Kind/PlacementIndex或DropId，DrawF/DrawG记录地图及目标身份，均保留原异常；出错通道当前帧隐藏，另一通道仍可显示。必需服务/Settings/HUD或Camera缺失明确报错，不自动找节点或补默认值。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译无C# Error，五脚本/meta与五个新类型已导入；配置14字段、原13输入及F/G各四字段静态核对。Forest/Grassland各覆盖Json默认、BuiltIn默认、Json关闭高亮主开关、三文字关闭但高亮开启、关闭F高亮、关闭G高亮、三文字及高亮全部关闭，共14次隔离Editor烘焙。schema12/revision15、全部新Settings/RGB、原G参数、F/G Hidden初值、丢弃反馈及原掉落Prefab/Prepared均一致，Json/BuiltIn等价。

原完整布置位置/朝向逐项一致；森林/草原草丛601/744、碎石19/19、树木89/53、采集点36/38、矿点20/18、阻挡109/71保持。烘焙Console前后均[0 Error,6 Warning,47 Log]，无新增错误/警告；原运行Tick Batching及TreeObstacle顺序警告保留，未清空Console。原主场景干净，临时World/Scene/TextAsset已释放。

主线程静态验收通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定阶段通过，范围限CombatPrototypeNetCode、schemaVersion=12/configRevision=15及[运行入口](Runtime.md)十项清单。人工结论来自用户反馈；未实际触发的身份/阶段到达时序、取消/完成、投影边界、旋转/缩放、开关/生命周期、多玩家/晚加入及独立配置/解析/绘制失败仍UNKNOWN。旧G文字v11/14十项、丢弃v10/13十二项、面板v9/12十二项、工具v8/11十二项、F HUD v7/10八项及更早用户通过保持原版本/清单，不扩展为本阶段或全平台通过；性能/带宽、平台/线上及旧保存成功后意外ECS恢复仍UNKNOWN。

AI未执行GamePlayer/PlayMode、游戏模拟/显示系统/GUI回调、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

v13/16的[资源状态](MapResourceStatusHud.md)独立显示文字、在原G面板上方绘制；F Ready/Working身份优先，无F目标才选择附近状态目标。状态目标不新增圆环，不改原圈的资格/身份/颜色/半径。状态开启可保留F采样及绑定，新增所属四字段静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN；高亮用户通过仍限v12/15十项。

当前v14/17的[工具修理](MapToolRepair.md)只恢复既有工具耐久，不预约资源或新增圆环；F/G四字段身份、颜色/投影及生命周期保持。新增输入与所属结果要求各端同版重新烘焙，修理静态通过、人工UNKNOWN。
