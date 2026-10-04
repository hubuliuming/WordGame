# 网络原型性能基线

[返回总导航](../AI_Understanding.md)。游戏入口与既有人工验收归[运行入口](Runtime.md)，文件契约归[资源与数据](DataResources.md)。本页只记录独立 CombatPrototype 网络原型的性能测量口径、环境、数据与限制。

## 【FACT】第 5A 阶段范围与当前状态

用户已确认首轮规模为 **2 玩家、32 敌人**，沿用现有 GamePlayer 双客户端和不同开发固定 ID。32 是既有 Spawner 的生成数量；实际在线人数及存活/死亡数量必须随采集记录，不能把生成数量当作持续存活数量。

当前存档标记已由 Unity 正常编译并注册。用户恢复第 5A 后要求“不启动，静态检测”，随后确认第 5B 补齐项目外采集助手；助手 5B-1 已实现并通过主线程静态验收，默认禁止启动采集，运行覆盖及数据有效性仍未验证。尚无运行样本，性能数值和本阶段 GamePlayer 回归结果均为 `UNKNOWN`，本阶段未通过最终性能验收。第 6A/6B/7A 及以前的人工通过结论保留各自范围，不能用作性能通过依据。

当前原型含第 6A 反击/死亡、第 6B 手动复活及第 7A 物品使用，行为归[战斗](Combat.md)、[玩家](Player.md)与[背包与道具](Inventory.md)。样本口径须标明实际源码/输入/Ghost 版本、生命变化及复活/使用请求；不同阶段不作为同一负载直接比较，仍没有任一版本的运行性能样本。

## 【FACT】存档观察入口

[CombatPrototypePlayerSaveStore.cs](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerSaveStore.cs) 提供以下 `Unity.Profiling.ProfilerMarker`，已核实注册类别为 Scripts、单位为 TimeNanoseconds：

| 标记 | 计量范围 | 范围外工作 |
|---|---|---|
| CombatPrototype.PlayerSave.Load | Load 的整个调用，包括打开文件、无档初值、严格读取/校验与返回；异常路径也结束计时 | 握手验证、玩家生成与恢复后的组件写入 |
| CombatPrototype.PlayerSave.SavePrepared | SavePrepared 的路径准备、JSON 序列化、临时文件写入、Flush(true)、Replace/Move；异常路径也结束计时 | PrepareReward/PrepareItemConsumption 候选投影、ECS 奖励或库存/体力提交与调用方日志 |

标记本身沿用原存储实现，当前调用来自服务端准入、奖励及第 7A 物品使用；物品使用与奖励保存共用 SavePrepared 标记，调用次数不等同于击杀奖励成功数。ProfilerRecorder 的按帧累加值可能包含多个调用；调用次数须另记，按帧均值/P95 不能写成单次存档耗时，无事件不能写成“耗时为 0”。

## 【FACT】已核实的 Editor 环境

| 项目 | 当前值 |
|---|---|
| Unity | 6000.5.6f1 |
| Netcode for Entities | 6.5.0（当前安装包） |
| 场景 | Assets/Scenes/CombatPrototypeNetCode.unity |
| 操作系统 | Windows 10，10.0.19045，64bit |
| CPU / 逻辑处理器 | 11th Gen Intel Core i5-11400 @ 2.60GHz / 12 |
| 系统内存 | SystemInfo.systemMemorySize 返回 32649 MB；不是进程使用量 |
| GPU / 显存 | NVIDIA GeForce RTX 3060 / SystemInfo.graphicsMemorySize 返回 12115 MB |
| Frame Timing 能力 | FrameTimingManager.IsFeatureEnabled() 为 true；有效帧样本仍为 UNKNOWN |
| 运行拓扑、第二客户端进程指标 | UNKNOWN；本轮按用户要求仅静态检测，未启动双客户端 |

上述硬件字段来自当前 Unity Editor 的 SystemInfo，不代表目标发布设备。核对时 Editor 未进入 PlayMode，无 Server/Client 游戏 World；该状态的帧耗时与内存不能记入游戏运行基线。

## 【FACT】第 5A 静态核对结果

源码、资源文本、已加载程序集与现有采集助手已有静态核对；当前编译后的顺序与存档调用包含第 6B/7A 接入。静态结果只证明配置、调用边界和观察覆盖情况，不提供耗时、GC、网络质量或承载能力结论。

| 核对对象 | 已核实状态 |
|---|---|
| 规模与引用 | 原 SubScene 的 Spawner 引用现有玩家/敌人 Prefab，EnemyCount=32、8 列、间距 3；EnemyPosition=(0,1,2) 是兼容字段，实际位置由[地图](Map.md)配置提供；2 玩家仍是已确认采集目标，实际在线人数 UNKNOWN |
| 第 6A 参数与门槛 | 玩家生命 100/100；敌人伤害 10、范围 1.75、前摇 0.5 秒、后摇 1 秒；死亡玩家排除追踪并停止移动/攻击，死亡敌人停止反击 |
| 编译后的结算顺序 | PlayerMovement → EnemyMovement → EnemySpatial → ItemUse → MeleeServer → Damage → Reward/SavePrepared → EnemyAttack → PlayerDamage → PlayerRespawn；UpdateAfter/UpdateBefore 与源码一致 |
| 存档入口与标记 | 服务端准入调用 Load；奖励/物品使用先准备候选、SavePrepared 再提交 ECS。两个标记仍为 Scripts/TimeNanoseconds，两种候选投影不在保存标记内 |
| 当前观察状态 | Editor 未进入 PlayMode、未处于编译中，6 个 World 仅为 Editor/Loading；Profiler.enabled=false，采集会话未启动且无停止回调；未启用记录器或录制 |

源码可见的成本入口包括：敌人移动按在线存活玩家逐个搜索最近目标；空间表复用 Persistent 容器并按更新清空重建存活敌人条目，容量仅在敌人数超过现值时扩充；近战只查询范围所覆盖的网格。移动、反击和奖励收集使用原生临时容器，这不能直接记作托管 GC。PrepareReward/PrepareItemConsumption 创建存档候选数组/字符串，SavePrepared 同步 JSON 序列化、UTF-8 字节分配、写盘和 Flush(true)；原每 2 秒状态/库存/逐敌人日志及战斗事件日志保留。上述调用的实际次数、耗时和分配量均未测量。

## 【FACT】当前敌人动画表现成本边界

当前网络敌人使用官方 GameObject 桥接与单层 Animator，每个腐化荒猪模型有 11 个 MeshRenderer；根占位 Renderer 关闭，不再以其 MaterialMeshInfo 显示。服务端只复制新增表现状态，客户端逐敌人读取同步阶段/剩余时间与生命并驱动姿态，资源与字段归[敌人美术](../EnemyArt.md)。32 敌人的实际可见数量、Animator/显示与网络字段成本均未采样，不能沿用旧占位显示的人工通过范围作为新表现的性能结论。

## 【CURRENT STRATEGY】已确认的采集口径

每组均为 **预热 10 秒、正式采集 30 秒**，由人工启动、移动、攻击及重连，AI 通过现有 Unity MCP/Profiler 读取文本指标。采集不改变 Tick、业务参数、日志策略或资源结构。

| 负载 | 测量对象 | 必须同时记录 |
|---|---|---|
| 双玩家站立、敌人追踪 | 持续模拟与同步、主线程、内存/GC、RTT/快照 | 在线人数、各 World、玩家与敌人存活/死亡、反击与停动变化 |
| 移动与攻击、击杀/保存 | 预测/模拟、同步、GC 与保存标记 | 实际击杀/保存事件、体力、玩家 HP 和双方存活/死亡变化 |
| 断线与重连 | 连接变化、重新准入/加载、状态恢复 | 固定 ID、断线/重连时间、人数/生命变化及加载事件 |

原生 ProfilerRecorder 保留正式窗口的逐帧数值、调用次数、单位、样本数与缓冲回绕状态；均值、P95 和最大值只基于有效样本。预热和正式窗口时长分开记录，缺失/失效计数器、未触发事件、采集中断或缓冲回绕必须明确标注，不能伪装为完整 30 秒测量。

临时采集助手位于项目外，通过 Unity MCP 内存编译核对；当前未注册采集回调或开启录制。助手使用有时限的 Editor 观察回调与原生记录器，采集结束或 PlayMode/程序集结束时释放观察资源，结果写入临时目录，不新增项目脚本或游戏实体。

助手 5B-1 按原生类别/完整名称保留逐帧值、Count、单位、数据类型及回绕状态，并按已存在 World 名称前缀匹配 ECS 系统标记。共享标记与进程计数器保留进程范围；同名 World 的歧义显式记录，父子标记不累加。新增读取入口、事件来源与缺失状态见下节；内存编译通过仍不等于运行数据有效或性能通过。

## 【FACT】第 5B 助手实现与静态验收

四份 UTF-8 助手文本位于项目外目录 `C:/Users/91611/AppData/Local/Temp/codex-phase5a-1b19c36e6865434281c99cf8d7145aeb/`：原 capture-start.cs.txt 为控制模板，原 capture-status.cs.txt 为只读状态入口；新增 capture-world-read.cs.txt 负责 World 查询与清理，capture-result-review.cs.txt 负责原生样本检查与统计。启动模板的两个 include 注释分别替换为相应辅助文本后才是完整 Unity MCP 方法体。默认 armCapture=false，直接返回 static-compiled-not-armed，声明后的采集逻辑与回调注册不会执行；状态入口独立编译，不启停采集。

| 数据 | 当前代码读取方式与边界 |
|---|---|
| World 与 Tick | 只观察现有 GameServer/GameClient World，保存名称、SequenceNumber、Flags、World 时间及实际观察帧/时间；记录显式 ClientServerTickRate 原始字段和 NetworkTime 的 Tick、有效标记、fraction、batch size。无配置组件时 unavailable，不套用默认 Hz；Tick 快照不等于逐次执行数量或实际频率 |
| 玩家、敌人及连接 | World 状态每 0.1 秒观察一次；按 Ghost 记录 HP/上限/受击/死亡、体力/攻击阶段/序号，服务端读取固定 ID；记录敌人总数/存活/死亡及服务端攻击阶段。在线连接数与玩家 Ghost 数分别记录，死亡玩家仍可在线；客户端固定 ID 及服务端专有字段不伪造 |
| RTT 与预测误差 | 按连接读取 NetworkSnapshotAck.EstimatedRTT/DeviationRTT，单位毫秒，后者为平滑平均偏差而非标准差；无组件或尚无接收消息时 unavailable。PredictionErrorNames/PredictionErrorMetrics 同实体、等长对应并保留名称；缺少/空缓冲 unavailable，冲突记录失败，不启用额外统计系统 |
| 原生耗时/内存/快照 | 沿用按帧 ProfilerRecorder；保留完整名称、类别、单位、Int64/Double 数据类型、有效样本和 Count，明确 World/共享/进程或歧义范围；缺少名称、无效记录器、无样本及回绕分别标记 |
| Frame Timing | 读取平台实际返回的 CPU/GPU 帧数据与时间戳，排除预热旧帧并去重；CPU 无效样本另计，未提供的 GPU 数据不补 0；实际有效样本数保留 |
| 击杀、保存与准入 | 监听正式窗口中新出现的原服务端日志，区分击杀奖励入队、保存成功、奖励或保存失败、准入成功/拒绝/准入或加载失败及受伤等，保留原文和帧/时间；不导入旧 Console。Load/SavePrepared 的调用次数来自独立标记，日志成功数不能代替调用数；组合失败日志不强行拆成纯保存或纯加载失败 |
| 断线/重连观察 | 保存连接实体/NetworkId/状态/固定 ID，区分首次观察、状态变化与不再出现；注明快照观察时点，无法确认的精确断线时间、原因和两次观察之间的变化不补全 |

原 10 秒预热/30 秒目标窗口与 32768 缓冲上限保留。结束、人工停止、退出 PlayMode、程序集重载、Profiler 被关闭或观察失败均有停止路径；释放本助手记录器、日志/Editor 回调与自有查询，已销毁 World 的查询由所属 World 生命周期释放。单个 World/连接/玩家/敌人或记录器失败分别记录上下文并隔离；缺失指标、部分失败、事件丢弃、回绕与中断不会写成完整测量。均值/P95/最大值仅基于有效样本，时间单位转换明确；存档按有调用的帧累加时间统计，仍不提供单次调用 P95，无调用记 no-events。

主线程已核对组合代码与只读状态代码内存编译成功，启动结果 armed=false、isPlaying=false、profilerEnabled=false，无采集会话或停止回调；只读核对原 6 个 Editor/Loading World、两个 Scripts/TimeNanoseconds 存档标记和 Frame Timing 能力。Console 为 0 条 Error、5 条既有 Warning；Profiler 未录制且原区域设置保持。静态验收通过的范围仅为实现、API、默认关闭与文件边界，World 数据准确性、回调释放的运行效果、实际开销及完整性能验收均为 UNKNOWN。

| 指标 | 数据口径 | 当前实测结果 |
|---|---|---|
| Simulation / Network Tick | 运行 World 的实际配置及采集窗口内 Tick；包默认值不能代替实测 | UNKNOWN |
| 模拟与同步耗时 | 按实际注册的 World/系统标记分别记录；父子组耗时不直接相加 | UNKNOWN |
| 主线程帧耗时 | 原生主线程记录与可用 Frame Timing，明确单位和有效样本数 | UNKNOWN |
| 内存与 GC | 进程/Editor 计数器原生值、单位及采集开销；不等同于单个 World 内存 | UNKNOWN |
| RTT / Jitter | 实际可用客户端/连接指标；必须注明来源与单位 | UNKNOWN |
| 快照量 | 原生 Snapshot Size 计数器及有效时间窗口；不直接等同于完整网络流量 | UNKNOWN |
| 预测误差 | 实际可用名称与值一一对应；缺少指标不等于零误差 | UNKNOWN |
| Load / SavePrepared | 标记有效样本、按帧累加时间与调用次数 | UNKNOWN |

## 【KNOWN ISSUES】测量限制与验收边界

- 第 5A 恢复后按用户“不启动，静态检测”完成本轮核对，尚无运行样本。奖励/存档/重连的本轮性能窗口回归仍为 UNKNOWN，静态核对完成不等于本阶段最终性能通过。
- 第 5B 已补齐上述读取/归属/事件入口并通过静态验收；运行时组件及计数器是否提供、数据对应与有效性、停止路径实际效果仍为 UNKNOWN，缺失状态不代表指标为 0。
- Console 只读返回 0 条 Error、5 条 Warning，保留此前运行留下的 Server Tick Batching 警告（短窗口平均 1.25、1.5、1.75 ticks/frame）。这些消息不是本轮采样；发生频率、窗口负载、成因及与保存/日志的关联为 UNKNOWN，不能据此填写 30 秒基线或判定性能不合格。本轮未清空 Console 或关闭警告。
- 第 6A 的 32 敌人伤害可叠加，站立玩家可能快速死亡，死亡后追踪、移动与攻击负载改变；样本须记录实际变化，不能把死亡后的停动窗口写成持续存活追踪/攻击基线。
- Editor、Profiler、原生记录缓冲及 MCP 观察均有开销；现有 NetCodeLogSystem 保留每 2 秒玩家、库存及逐敌人日志，这些开销属于当前基线，不作静默关闭。
- 第 5B 的 ECS 查询可能等待依赖，状态对象、临时容器、日志监听、Frame Timing 请求和结果序列化也有开销。0.1 秒快照可能漏掉间隔内变化，不等于逐 tick 跟踪；助手位于项目外临时目录，换机或清理后可用性为 UNKNOWN。原生缓冲、状态/帧/事件列表有容量上限，超限或回绕明确标记。
- 同一 Editor 中的服务端与本地客户端共享进程。帧时间和内存不能直接拆成独立服务端/客户端成本；其他进程未连接 Profiler 时，其独立指标为 UNKNOWN。
- 快照统计不证明 UDP/IP、命令/RPC、重传等完整传输流量；没有相应证据时，完整网络带宽为 UNKNOWN。
- 本阶段没有已确认的性能合格阈值或产品承载上限，首轮结果仅建立当前规模的测量基线。发布平台、独立服务器/客户端性能、大规模上限与线上网络结果为 UNKNOWN。
