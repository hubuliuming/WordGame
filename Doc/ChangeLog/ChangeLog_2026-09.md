# 2026-09 变更记录

## 2026-09-19：初始化 AI 项目导航

- 按用户确认方案新建 Doc/AI_Understanding.md、Doc/Modules/ 下的 Runtime.md、Player.md、Combat.md、Inventory.md、DataResources.md、Framework.md，以及本月 ChangeLog，共 8 份中文 Markdown。
- 总入口建立项目身份、目录地图与按任务阅读路由；六份模块文档记录已核实的控制入口、调用链、数据值、资源文本引用、框架边界及相关源码链接。
- 区分当前源码实现、现有配置快照、编辑器重写值与尚未确认的业务意图；保留 UNKNOWN 和运行验收缺口。
- 按真实源码记录金币未写值、Hp 判死分支、体力标记、背包刷新/使用未完成、对象池警告等静态问题；未执行业务修复。
- 已有协议说明和笔记保留原位，由导航引用；没有迁移、重写或补造旧业务说明。
- 修改范围仅为 Doc 下新增文件，未修改代码、Scene、Prefab、Animator、meta、依赖或构建配置；未读取图片内容。
- 未运行逻辑单元测试、GamePlayer PlayMode、命令行构建或平台发布；无运行通过结论。
- 文档只读审计已完成：UTF-8、相对文件链接、入口可达性与体量检查未发现问题；逐份查看新增文件 Git diff，中文正常。该检查不代表工程运行验收。

## 2026-09-19：同步 Codex UnityMCP 连接端口

- 将项目级 `.codex/config.toml` 与用户级 Codex 配置中的 `unityMCP` 地址从 `http://127.0.0.1:8765/mcp` 同步为 `http://127.0.0.1:9321/mcp`。
- 已核实 `9321` 由当前 UnityMCP 服务监听，MCP `initialize` 返回成功，并可读取唯一 Unity 实例 `Code_01` 及工具列表。
- 当前任务的 MCP 工具目录不会在配置修改后热刷新；需要重启 Codex 或新建任务后确认 `read_console`、`find_gameobjects`、`find_in_file`、`set_active_instance` 已注入。
- 未修改代码、Scene、Prefab、Animator、meta、依赖或构建配置，未运行逻辑单元测试、GamePlayer PlayMode、命令行构建或平台发布。

## 2026-09-26：第 1 阶段执行基线与资源边界核对

- 核对记录时间：2026-09-26 10:20:29 +08:00。
- 读取根目录 AGENTS.md、CodeRule.md、Doc/AI_Understanding.md 及 Runtime、Player、Combat、Inventory、DataResources、Framework 六份模块文档，随后只读核对相关源码与 Scene / Prefab / meta 文本。
- 追加本记录前，`git status --porcelain=v1 --untracked-files=all` 输出为空；HEAD 为 `11499d78a50f3842ed00ade23f5f517cff7cfaec`。
- 玩家基线文件为 `Assets/streamingAssets/Data/PlayerData/Player.json`，SHA256 为 `9EE9068786648410256FC2977E2E6B623AC32E58184A2E2B574EB432782324BC`；原文件为 UTF-8 无 BOM、CRLF 换行、368 字节，最后一个字节是 `}`，文件末尾没有换行。下方代码块保留完整文本快照；这些值仅为本次读取到的文件状态，不是已确认初始角色数据。
- 核实 MapCanvasControl.Start 依次调用 PlayerDetails.OnStart 与 KnapsackControl.OnStart；PlayerDetails 经 Game.Interface 访问 PlayerModel，Game 注册模型与系统，QFramework 按模型、系统两个阶段初始化。
- 核实 Map 的 MapCanvasControl 组件 fileID 为 `271186376`，PlayerDetails 字段为 `236714923`，KnapsackControl 字段为 `1772732472`，contextRect 为 Content 的 RectTransform `1099784055`；ItemParent 为 MapCanvas 下的对象 `1747035443`，RectTransform 为 `1747035444`。
- 核实 Map 的 DetailInform 组件为 `1053136623`，其对象与 Knapsack 对象的序列化初始激活值均为 0；在 Scripts / Framework / Test 的 C# 检索范围内未发现 DetailInform.OnStart 的显式调用点。TestController 挂在 Test 对象上，enemyBtn 指向 BtnEnemy 的 Button `1962001503`，activeBtn 指向 BtnItem 的 Button `1792397324`；BtnItem 的序列化初始激活值为 0。
- 核实野猪 Prefab 根组件使用 EnemyBase，直接子节点 BtnAttack 挂有 Button；活力苹果 Prefab 根组件使用 ItemBase，直接子节点 Btn 挂有 Button；Goods Prefab 根节点挂有 Button，直接子节点 TxtName、TxtNum 均挂有 Text。
- 核实 ItemData 当前声明于 ItemBase 内，EnemyData 当前声明于 EnemyBase 内；DataSetting.cs 的公开 data 字段引用 EnemyBase.EnemyData。DataSetting.unity 的组件 `967421751` 使用 DataSetting 脚本 GUID `e050f320eee59c74ab7aff0e71c59fd7`，该组件当前序列化块未含 data 字段；野猪 Prefab 的 EnemyBase 组件序列化块则含 data 及 award 子字段。
- 核实玩家状态修改、保存与通知的现有入口，以及死亡、体力、属性上下限、库存、战斗、道具使用的现有分支；本阶段没有收到这些 UNKNOWN 业务规则的用户答案，也没有把当前源码行为视为已确认规则。发布平台、正式初始数据与旧档策略仍未确认。
- 本阶段仅追加本月 ChangeLog；未修改代码、JSON、Scene、Prefab、Animator、meta、脚本位置、Bundle 或构建配置，未创建数据/存储脚本，未执行编辑器菜单，未读取图片内容。
- 未运行逻辑单元测试、GamePlayer PlayMode、命令行构建或平台发布；本记录不包含运行通过结论。

玩家文件完整文本快照：

```json
{
  "property": {
    "Name": "小明",
    "Hp": 50,
    "Power": 60,
    "Level": 2,
    "Exp": 572,
    "Attack": 24,
    "Defence": 8,
    "Speed": 10,
    "UpperHp": 200,
    "UpperPower": 100,
    "UpperAttack": 24,
    "UpperDefence": 0,
    "UpperSpeed": 10
  },
  "goodsDict": {
    "Coin": 480,
    "馒头": 5,
    "小块肉": 40
  }
}
```

## 2026-09-26：第 1 阶段收到业务规则答复

- 记录时间：2026-09-26 10:29:03 +08:00。
- 收到用户针对死亡状态、属性上下限、体力与库存问题的原文答复：

> 1：不解除，2：不许超过下限，3：不许超过体力，拦截处理，4：控制台报错处理

- 本次已收到“死亡状态不解除、体力不足拦截、库存异常控制台报错”的答复；本记录不将答复写成已经实施的代码行为。
- 随后收到第 2 条的用户原文澄清：“上限不得低于下限”。比较方向已明确，不再列为未确认；当前 UpperDefence=0 的具体处理未在本次答复中指定。
- 库存扣减后恰好为 0 的行为仍未收到用户回复；本记录未将其判为成功或失败。
- 本次仅追加本月 ChangeLog，未进入第 2 阶段，未修改代码、JSON、资源或业务模块事实文档，未运行测试、GamePlayer PlayMode 或构建。

## 2026-09-26：第 1 阶段收到旧属性数据处理决定

- 记录时间：2026-09-26 10:31:31 +08:00。
- 针对当前存档 Defence=8、UpperDefence=0、下限为 4 的情况，收到用户选择：“控制台报错，暂不修正这组旧数据”。
- 原有数值暂不修改；控制台报错是已确认的处理规则，本次尚未实现该报错行为。
- 库存扣减后恰好为 0 的行为仍未收到用户选择，继续保留为未确认。
- 本次仅追加本月 ChangeLog，未进入第 2 阶段，未修改代码、JSON、资源或模块文档，未运行测试、GamePlayer PlayMode 或构建。

## 2026-09-26：第 1 阶段收到库存归零处理决定

- 记录时间：2026-09-26 10:35:00 +08:00。
- 针对库存合法扣减后恰好为 0 的情况，收到用户原文答复：“移除该库存”；已确认的规则为移除该库存条目，不再将这一点列为未确认。
- 原先负库存、扣减不存在物品的处理规则仍为控制台报错；旧属性数据仍按已收到的决定只报错、不改数值，死亡状态不解除，体力不足拦截。
- 第 1 阶段前述第 2 阶段相关规则问题均已收到用户答案；本条仅记录答案收齐的事实，不包含阶段验收通过或代码已实施结论。
- 后续阶段的战斗结算、道具消耗与效果、发布平台、正式初始数据及旧档策略仍保留 UNKNOWN。
- 本次仅追加本月 ChangeLog，未进入第 2 阶段，未修改代码、JSON、资源或模块文档，未运行测试、GamePlayer PlayMode 或构建。

## 2026-09-26：第 2 阶段玩家状态与存储职责落地

- 执行记录时间：2026-09-26 10:45:34 +08:00。主线程在移交前已判定第 1 阶段通过；本阶段由独立子 Agent 按已确认范围执行，以下记录不替代主线程第 2 阶段验收结论。
- 从 ItemBase 抽出全局 ItemData，从 EnemyBase 抽出 Code_01.Enemy.EnemyData，Award 保持嵌套。静态对比字段分别为 13 项、含 Award 共 10 项，字段名、类型、大小写、Serializable 与 struct 保持不变；现有 data / initData 字段及组件脚本位置不变。DataSetting、编辑器及 UI 调用方仅作必要类型适配。
- 新建 PlayerDataStore（IUtility）并由 Game 注册；复用 JsonUti 和 MsgPaths.Config.PlayerData 的 Load / Save。未修改原 JSON、共用 JsonUti 或平台路径；加载缺少必需数据时显式失败，保存异常记录路径与原始异常并返回 false。
- PlayerModel 公开属性只读，GoodsDict 通过 ReadOnlyDictionary 包装为 IReadOnlyDictionary。PlayerEventSystem 保留 Change* 入口并统一使用增量；先准备和校验全部最终值，再应用、保存一次，成功后按实际变更各发送一次 UpdateShowData / InventoryChanged。无实际变更不保存、不发事件。
- ChangeAll、LevelUp、库存变更与一次战斗结算使用同一提交点；校验失败不写入任何部分，保存失败回滚本次属性和涉及库存项，不复制整个库存。该回滚只覆盖内存；现有 JsonUti 直接写原文件，未增加磁盘原子替换或备份能力。
- 修复金币未赋值与 Hp 最终值被重复当增量判死的问题；死亡后普通 HP 恢复保持 0，其他属性按各自规则处理。死亡/体力耗尽状态分别从 Hp / Power 派生，体力不足拒绝扣除，成本等于存量允许扣至 0。
- 新上限低于对应 PlayerData 下限时报错拒绝；当前旧 Defence=8、UpperDefence=0 在加载时统一报错并保留，不自动修正、不阻止无关 HP/体力/金币/库存操作。普通库存负结果或扣减不存在物品记录物品和原因并拒绝，合法归零移除，零增量不变；Coin 使用专用规则并保留零金币键。
- AttackCommand 保留同步循环、先手比较、每轮双方扣血、胜负判定及失败敌人状态语义，仅将 CostPower 资格检查及体力/HP/经验/金币/掉落统一提交；提交成功后才回收胜利敌人。DropSystem 及 1 至 2 的掉落数量范围未改。
- UseItemCommand 改为 AbstractCommand<bool>；ItemBase 显式 SendCommand<bool> 并仅在成功后回收。未建立背包扣减/详情选择链路；InventoryChanged 已发事件，展示订阅/刷新尚未接入，UpdateGoods 空实现保留。
- 新文件及 GUID：Item/ItemData.cs（e4f95b75240b4d4e8761f9f9042aef29）、Features/Enemy/EnemyData.cs（ffdaaa14e999408fad72ab5bdf815951）、Features/Player/Data/PlayerDataStore.cs（cb77564d38bb468eb8b089763445a90e）；均仅新增对应脚本 meta，检索 Assets 内 GUID 各出现一次，未改现有 meta。
- 已按实际变化更新六份 Doc/Modules 文档及导航 Game 注册事实；保留旧防御数据、UI 生命周期、背包使用/刷新、战斗业务规则、平台存储等未完成或 UNKNOWN 项。
- 静态核对：Assets 下未检出旧 ItemBase.ItemData / EnemyBase.EnemyData / UpdateLocalData 引用，也未检出调用方对 PlayerModel 公开属性或 GoodsDict 直接写入；已核对 QFramework 同步命令返回值签名和变更方法调用链。已有文件 git diff --check 无空白错误，新增六文件逐个 no-index --check 无空白诊断；逐份 diff 中文正常。
- 玩家 JSON SHA256 仍为 9EE9068786648410256FC2977E2E6B623AC32E58184A2E2B574EB432782324BC；git diff 未包含 Scene、Prefab、Animator、既有 meta、JSON、Bundle 或构建配置变更。未读取图片，未运行逻辑单元测试、GamePlayer PlayMode、命令行构建、发布或编辑器菜单。
- 第 2 阶段交回主线程待验收。人工 GamePlayer 尚需核对加载旧值报错、体力边界、死亡后恢复、金币写入、库存拒绝/归零、完整操作一次保存与通知、保存失败不回收等行为；本记录不含这些运行检查已通过的结论。

## 2026-09-26：第 2 阶段主线程验收通过

- 用户在第 2 阶段静态验收后明确反馈“实际行为和日志均已核对正确”。主线程结合代码、文档、资源静态检查与该反馈，已判定第 2 阶段通过并授权进入第 3 阶段。
- 上述依据不表示 AI 执行过逻辑单元测试、人工 GamePlayer、命令行构建或完整平台验收；旧防御数据、后续 UI 生命周期/刷新与战斗规则边界继续保留。

## 2026-09-26：第 3 阶段场景编排与对象池生命周期落地及阻断

- 执行前保存工作区范围、关键文件 UTF-8 文本和 SHA256 基线，未覆盖或回退第 1/2 阶段成果。本阶段由独立子 Agent 按已确认范围执行，当前交回主线程处理阻断，不包含第 3 阶段通过结论。
- MapCanvasControl.Start 改为获取 Game 架构及已注册 FactoryUISystem → BindScene(ItemParent) → PlayerDetails.OnStart → KnapsackControl.OnStart → 启用按键处理。绑定/面板初始化抛错时清理本场景池并暴露原异常；OnDestroy 只使用缓存系统和绑定来源执行清理。
- FactoryUISystem 的池字典改为实例状态，补充全部实例与借出集合登记；OnInit 不访问场景。BindScene 接收明确父节点；创建时仅初始化成功的实例进入登记，借出敌人继续 InitData。归还按实例归属定位池，停用并恢复 ItemParent；成功归还不再打印失败警告。
- 增加销毁回调、失败借出对象 Discard 和场景清理。ClearScene 用 ReferenceEquals 校对绑定来源；ClearPools 清理池内对象及全部未归还对象，包含已改挂 Content 的 Goods。已由 Unity 先销毁的对象只清除登记；重复清理无副作用，旧场景延迟清理不影响新绑定。
- MapCanvasControl、TestController、KnapsackControl 改为使用已注册系统实例；保留的 GameObject.Release 扩展转到该实例，AttackCommand/ItemBase 业务代码未改变。背包只补单格异常隔离、半成品销毁和成功项计数；未接库存事件/刷新，未修改 99 拆格及布局公式，也未改详情使用链。
- 当前阻断：Map.unity 的 MapCanvasControl 组件（fileID 271186376，GUID 4d0569183ec2d064e8c075944eb82749）只尝试新增 ItemParent: {fileID: 1747035444}，未改变对象层级或组件身份。首次补丁后字段未留存；随后在主线程确认的原授权范围内单独补回一次，立即 diff 仅新增该一行，SHA256 为 7C573FE5D2EAE854D4A165C08A16D78E2161B12F0BE6093E17CE93B0CF3E8440。
- 2026-09-26 11:01:55 +08:00 再读时，该字段再次缺失，Map.unity SHA256 回到本阶段初始值 EA0A982A941B68943B77377808731F862A1FDE1DCCA02B780788DEB403F41E77。再次核对文件时间为 11:03:12，当前资源文件无文本差异。由于必需绑定缺失，MapCanvas.Start 进入 BindScene 时会显式失败，第 3 阶段未通过。覆盖来源 UNKNOWN；已停止代码和资源执行，未循环重写、修改 Unity 设置或触发构建，交主线程确认稳定写入时机/覆盖来源。
- 已同步导航与六份模块文档的实际脚本状态、第 2 阶段用户反馈范围及第 3 阶段绑定缺口；没有把尚未留存的引用记为已绑定。
- 当前静态核对：Assets 的 C# 文本中无旧 FactoryUISystem.Get/Release 静态调用；MapCanvas 无 GameObject.Find；池清理先处理池内对象，再处理剩余登记；成功创建/借出登记与失败清理顺序已核对。相对本阶段基线的代码差异仅为 FactoryUISystem、MapCanvasControl、TestController、KnapsackControl；其他第 1/2 阶段代码及六个未跟踪脚本/meta 哈希未变。git diff --check 未输出空白错误。
- 未新增脚本、组件或 meta，未改 Prefab、Animator、JSON、业务数值、Bundle、构建配置或第三方库；未读取图片，未新增/执行逻辑单元测试，未运行人工 GamePlayer、命令行构建/发布或编辑器菜单。
- 第 3 阶段未完成：必须先由主线程确认并解决场景绑定未留存的问题；此后人工 GamePlayer 仍需核对首次进入顺序、E/I 和按钮借出、敌人再次借出重置、成功归还无误报警、Goods 归还恢复父节点、失败项不残留且后续条目继续、池内与未归还对象退出清理、重复进入绑定新引用、重复清理及旧场景清理不影响新池。最终验收由主线程执行。

## 2026-09-26：第 3 阶段阻断相关自动保存机制核对

- 只读核实 XPAutoSave.cs：InitializeOnLoad 静态初始化注册 EditorApplication.update；自动保存开启且距上次保存超过配置间隔时执行 SaveScene；Application.isPlaying 时返回，编辑模式下调用 EditorSceneManager.SaveScene 保存当前活动编辑场景。
- 只读核实 AutoSaveSettings.asset 当前 autoSaveScene=1、intervalTime=30、showMessage=0；AutoSaveWindow.cs 对应菜单入口为 YFramework/AutoSaveScene，窗口中的“自动保存”开关和“时间间隔(秒)”写入相应设置。
- 已确认项目配置启用 30 秒场景自动保存，该机制与场景字段写入后未留存的现象吻合；尚未做关闭后的对照验证，不将其写成唯一覆盖来源已证实。本次只补充相关机制文档，未修改自动保存配置/代码，未操作菜单或恢复代码/资源写入。
- 第 3 阶段仍未通过：MapCanvasControl.ItemParent 场景绑定未留存，当前缺失必需引用会使 MapCanvas.Start → BindScene 显式失败；该阻断及人工 GamePlayer 待验收状态保持不变。

## 2026-09-27：第 3 阶段补齐 Map 场景父节点引用

- 恢复写入前，主线程通过 UnityMCP 核实活动场景为 `Assets/Test/Test.unity`，编辑器处于非 PlayMode 的 idle 状态，按 MapCanvasControl 组件查询（含 inactive）返回 0 个对象。Unity 本身仍运行；此状态不记录为整个工程或 Unity 已关闭。XPAutoSave 每 30 秒只保存当前活动编辑场景，因此当时 Map 已退出该自动保存目标；唯一历史覆盖来源仍为 UNKNOWN。
- 10:43:38 +08:00 刷新本次 33 个已有修改/未跟踪文件的 SHA256 与目标文本基线。Map 原值为 `ItemParent: {fileID: 0}`，原 SHA256 为 `FFFD87AFDF5DFBA1B922D4E230FE9EACA0EA7E80CCD555D5B4289CDF8D0DABF9`，原最后保存时间为 10:39:57 +08:00。已有 Test.unity 与 EditorUserSettings.asset 差异纳入保护范围。
- 10:44:17 +08:00 仅将 MapCanvasControl 组件（fileID `271186376`，脚本 GUID `4d0569183ec2d064e8c075944eb82749`）的 ItemParent 引用改为现有 RectTransform `1747035444`（所属对象 `1747035443`）。组件身份、对象层级及其他引用未变；立即读回与预期单项替换完全相同。
- 写入后 SHA256 为 `7C573FE5D2EAE854D4A165C08A16D78E2161B12F0BE6093E17CE93B0CF3E8440`。10:45:45 +08:00 首次延时复读（间隔约 88.7 秒）及 10:49:52 +08:00 再次复核均为同一哈希，文件修改时间保持 10:44:17 +08:00，引用留存；未循环重写或修改自动保存配置。
- 仅同步 AI_Understanding、Runtime、DataResources、Inventory、Combat 中受影响的绑定缺口与验收状态，移除当前缺失引用的已知问题，保留唯一历史覆盖来源 UNKNOWN、其他业务 UNKNOWN 及第 3 阶段人工 GamePlayer 待验收。第 2 阶段既有通过结论不扩大。
- 本次写入范围仅为上述 Map 单项引用、五份事实文档及本条记录。其余已有差异文件内容哈希保持基线；未新增或修改脚本、JSON、Prefab、Animator、meta、Bundle、构建配置、Test.unity 或 EditorUserSettings.asset。五份事实文档显式 UTF-8 检查有效，无替换字符和失效相对链接；逐文件 git diff 的中文正常，git diff --check 无空白错误。
- 本条只记录静态绑定补齐及留存证据，不代表第 3 阶段通过。首次进入顺序、E/I 和按钮借出、敌人再次借出重置、归还与父节点恢复、失败项隔离、池内与未归还对象退出清理、重复进入与清理仍待人工 GamePlayer 核对及主线程验收；未运行逻辑单元测试、GamePlayer PlayMode 或构建，未进入第 4 阶段，未读取图片内容。

## 2026-09-27：第 3 阶段用户确认与主线程验收通过

- 用户针对主线程提出的第 3 阶段人工 GamePlayer 清单（初始化、生成/回收复用、失败项隔离、退出后重新进入 Map，以及实际行为和日志）回复“已确认正常”。此反馈记录为用户人工核对结果，不记录为 AI 执行了 PlayMode 或测试。
- 主线程结合此前代码、资源、文档、场景引用留存的静态验收与本次用户反馈，明确判定第 3 阶段通过；本条同步该既有主线程结论，子 Agent 未自行替代最终验收。
- 同步 AI_Understanding、Runtime、DataResources、Inventory、Combat 中第 3 阶段待验收或结果 UNKNOWN 的过时状态。结论仅覆盖本阶段清单，UI 订阅/刷新、详情使用等后续工作及完整工程编译、平台构建、服务器联调仍保留原有未完成或 UNKNOWN 边界；唯一历史覆盖来源仍为 UNKNOWN。
- 本次仅修改上述五份事实文档并追加本条记录，未修改代码、Scene、JSON、settings 或其他资源；未运行逻辑单元测试、构建或 GamePlayer PlayMode，未进入第 4 阶段。

## 2026-09-27：第 4 阶段 UI 生命周期与库存刷新落地

- 按已确认范围核对并保留 MapCanvasControl、KnapsackControl、PlayerDetailsControl、DetailInform 的现有实现；未修改 Scene、Prefab、Animator、资源结构或详情物品效果链。
- KnapsackControl 缓存已注册 FactoryUISystem，订阅 InventoryChanged；库存变化时清理并归还旧格子，按 Coin/零值/负值规则和 99 上限重建格子，恢复或扩展 Content 高度；单格失败隔离并记录后继续。
- PlayerDetailsControl 订阅 UpdateShowData，重复 OnStart 只刷新不重复注册；DetailInform 维护 UseGoods 监听，当前回调仍不应用物品效果；三个面板及 MapCanvas 销毁路径均注销监听并释放对象池格子。
- 同步 Inventory、Player、Runtime、AI_Understanding 文档中的当前事实与已知边界。仅完成静态核对，未运行逻辑单元测试、命令行构建或人工 GamePlayer PlayMode；第 4 阶段交回主线程验收。
## 2026-09-27：第 0 阶段网络与运行时基础核对

- 按已确认的合作 PvE 第 0 阶段范围核对 Unity `6000.5.6f1` 及现有包：Input System `1.20.0`、Netcode for Entities/Transport `6.5.0`、Entities/Entities Graphics/Unity Physics `6.5.0`、Cinemachine `3.1.7`、AI Navigation `2.0.14`、Burst `1.8.29`、Collections `6.5.0`、Mathematics `1.4.0`。
- 核实当前未发现 NetCode World、`ClientServerBootstrap`、网络流请求或网络实体同步入口；未新增网络启动脚本。建立可运行网络生命周期仍需明确启动场景与 World 管理边界，不能在本阶段以未接入的空入口替代。
- 核实当前渲染配置为 Built-in（Graphics/Quality 的自定义渲染管线均为空）；Packages 未包含 Addressables，Assets 未发现 Addressables 设置或调用。未修改 Packages、Scene、Prefab、Animator、资源、meta 或构建配置。
- 本阶段仅更新事实文档，未运行逻辑单元测试、GamePlayer PlayMode、命令行构建或平台发布；网络联调、URP 切换、Addressables 迁移保留后续阶段。
## 2026-09-27

- 【FACT】新增独立 `CombatPrototype` 组件，覆盖相机跟随/旋转、相机相对移动、单次近战前摇/命中/后摇、基础生命与死亡停用。
- 【CURRENT STRATEGY】第 1 阶段仅用于独立测试场景，不接入正式 Map、UGUI 敌人、ECS、网络、奖励或存档。
- 【KNOWN ISSUES】测试场景中的 Inspector 绑定与 Cinemachine Follow/LookAt 仍需主线程在 Unity 编辑器中配置并进行人工 GamePlayer 验收。
- 【FACT】新增独立 `CombatPrototypeScene.unity`，完成玩家/敌人组件、Layer 6 targetMask、CinemachineBrain、CinemachineCamera Target 和 ThirdPersonFollow 的序列化绑定；场景未加入正式构建列表。
