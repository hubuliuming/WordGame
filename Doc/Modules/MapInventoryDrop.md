# 背包物品丢弃与地面掉落

返回[地图](Map.md)、[背包](Inventory.md)及[材料面板](MapInventoryPanel.md)。本专题负责 CombatPrototypeNetCode 的丢弃配置、按钮请求、服务端库存扣减与准备态掉落事务；运动、G拾取和实例释放归[掉落](MapDrops.md)，玩家存档归[资源与数据](DataResources.md)，人工清单归[运行入口](Runtime.md)。

## 【FACT】入口与范围

| 文件 | 当前职责 |
|---|---|
| [MapInventoryDropConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryDropConfig.cs) | 必填 inventoryDrop DTO及物品/资源键定义 |
| [InventoryDropData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryDropData.cs) | Settings、Definition、稳定命令枚举和所属反馈 |
| [InventoryDropSystem](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryDropSystem.cs) | 在线资格、消费候选、准备态创建、保存和提交 |
| [InventoryDropClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryDropClient.cs) | 普通C#类，按钮绘制、单个未提交请求与反馈缓存 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原根新增Settings/定义缓冲，沿原显式资源键取得Prefab |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) / [绑定](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs) | 原面板接入丢弃行，复用本地玩家/连接检查及鼠标隔离 |
| [PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs) / [Player Baker](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerNetCodeAuthoring.cs) | 丢弃事件/物品/模式三字段及玩家所属反馈初值 |
| [Drop Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropAuthoring.cs) / [Spawn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnSystem.cs) / [Utility](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropSpawnUtility.cs) / [Render](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapDropRenderSystem.cs) | Prepared初值、阶段参数、统一所有权和准备态隐藏 |

四个新脚本及正常Unity导入生成的meta已落地。没有新增MonoBehaviour挂载、Scene/SubScene/Prefab/Animator结构、图片/字体/材质/网格或资源绑定，也未修改旧meta、包或构建设置。正式Map背包、99拆格、道具效果及网络小块肉E消费保持各自原边界。

当前只支持 vitality_apple→drop_apple、wood→drop_wood、stone→drop_stone，分别映射原活力苹果/木材/石材名称；复用 DroppedApple、DroppedWood、DroppedStone。小块肉和工具槽不能丢弃；未配置的库存条目显示Unavailable，工具仍是独立Tools缓冲。

## 【FACT】JSON契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)一致为schemaVersion=34/configRevision=37。inventoryDrop全部字段必填，沿原严格UTF-8/缺失/未知/重复字段及类型检查；旧v1～v33明确失败，不补段或回退来源。disabled仍校验全部字段与资源绑定；正常导入/烘焙后生效，没有运行热重载。

| 字段 | 默认值 | 契约 |
|---|---|---|
| enabled | true | 独立控制丢弃；不关闭F/G/E、制作或已有掉落 |
| singleDropQuantity | 1 | 正整数，单次按服务端配置取量，数量不足拒绝 |
| allowDropAll | true | All取服务器执行时的实际库存总量 |
| feedbackSeconds | 2 | 有限正数，仅本地反馈显示期限 |
| dropLabel / dropAllLabel / unavailableLabel | Drop / All / Unavailable | 非空白、无控制字符、最多61个UTF-8字节 |
| successLabel / rejectedLabel / failureLabel | Dropped / Drop rejected / Drop failed | 同上 |
| items | 苹果/木材/石材三条 | 1～3条非null、已知且唯一itemId；资源键须符合原ID格式及显式绑定 |

每条items含itemId和visualResourceKey，客户端不传资源键。Settings和定义由原地图根烘焙取得；禁用或缩减列表不自动替补物品。原drops独占运动参数：2米拾取、0.4秒飞行、散落半径/弧高各0.6米、贴地0.05米、缩放0.5、600秒到期，未复制另一套参数。drops.enabled只控制敌人额外掉落；关闭敌人掉落、工具、砍伐或采矿不关闭inventoryDrop及其配置绑定。

## 【CURRENT STRATEGY】按钮、命令与反馈

B沿原规则打开380×640面板；每个经[排序筛选](MapInventoryListView.md)与[搜索](MapInventorySearch.md)后的正数量可见行下面仍有原操作行。配置支持的条目显示Drop x1（数量随配置）和All，单次数量不足或All关闭时对应按钮禁用；不支持或丢弃关闭时显示Unavailable。库存快照异常时禁用所有可丢弃按钮，原工具制作按钮资格保持。单个All创建一份承载全部数量的Ghost，不按99拆分；落地后参与[地面合并](MapDropMerge.md)，超合并上限的大堆仍独立保留。

客户端仅缓存一个未提交Kind/Mode请求，不扣库存或创建掉落。按钮沿原鼠标按下来源检查，使用可见Row.Name解析原稳定物品码；按下至抬起间可见行身份/顺序/数目变化取消该次行点击，已经排队的Kind/Mode沿原消费，不撤回已提交请求。面板内左键/滚轮仍隔离攻击/镜头；下一输入采样重新核对World、地图、当前所属存活玩家和Connected/InGame连接，再消费并清空请求。原IInputComponentData新增DropInventory InputEvent、InventoryDropItem byte、InventoryDropMode byte，稳定物品码None/Apple/Wood/Stone=0/1/2/3，模式None/Single/All=0/1/2；不传库存索引、数量、位置或客户端目标。没有新增RPC。

新增玩家反馈Sequence/Kind/Quantity/Result四个GhostField，仅SendToOwner，初值全零；Result为None/Success/Rejected/Failed。服务端每次本人的有效处理结果更新序号，非法所有权请求不覆盖真正所属者反馈，未知物品码在拒绝反馈中归None。面板页脚依次显示未到期丢弃、修理、制作反馈；成功包含物品和实际数量，拒绝/失败使用配置文案。新绑定只观察现有Sequence，不重播旧结果；F提示/进度和工具制作反馈组件保持。

关闭面板清掉未提交丢弃请求；死亡、断线、无本地Ghost、源/玩家变化及World/Scene停止或释放沿原Reset清掉请求、显示和序号观察。已消费进输入命令的请求由服务器最终资格决定，不通过关闭面板撤回。inventoryPanel、interactionHud和inventoryDrop开关独立；关闭面板不再提交按钮请求，数字1/2/3/4和F/G/E/R仍沿原链。

输入与玩家Ghost布局已变化，所有参与端必须使用同版代码/JSON及重新烘焙数据；未增加版本协商或配置一致性协议。

## 【CURRENT STRATEGY】服务端资格与事务

InventoryDropSystem仅在ServerSimulation的PredictedSimulationSystemGroup执行，位于DropSpawn、制作和修理之后、统一F及R复活之前，间接位于玩家伤害/掉落清理之后。从Connected/InGame且无断线请求的连接CommandTarget取启用Simulate的当前玩家；复用原交互服务核对GhostOwner、存活、有限零Move、没有Attack请求且近战Ready。任何植物/树木/矿点预约期间拒绝。同tick包含F/G/E、制作、Respawn或RepairAxe/RepairPickaxe时保留原操作并拒绝丢弃，不部分扣料；不自动重试。

服务端再校验启用开关、已配置物品、合法模式、All开关和实际库存。Single取当前配置数量；All取该项当前总量；缺项/非正或不足数量拒绝。同名重复条目、必需组件或服务缺失明确暴露错误，不创建兜底资源或修正库存。独立连接请求复制后逐项处理、记录阶段和原异常，一个失败不阻止其他请求。

1. 用现有PrepareItemConsumption生成扣减后的完整v3候选，保留其余库存顺序、金币/经验、当前Tools与Level。
2. 验证既有掉落Ghost/Transform/State/Progress及Prepared初值，经SpawnOwnedDrop创建并登记本次Prepared实例；地图定义先复制，避免批次中的结构变更使引用失效。
3. Instantiate之后重新取得库存、掉落状态和本人反馈引用，再调用SavePrepared。
4. 保存成功才写原库存缓冲（零数量移除），把本次状态改为Airborne并报告成功；提交阶段只有非结构性写入。

DropPhase追加Prepared=3，原Airborne/Landed/Consumed值0/1/2保持。Prefab烘焙初值为Prepared；原敌人、砍伐和采矿创建完成时默认Airborne，行为保持。Prepared不运动，Render隐藏，G只选择Landed因而无法拾取。激活后沿原飞行/落地/G/到期/Server World释放，无专属拾取者；任意满足原资格的玩家均可领取。

保存前失败保持该操作的库存不变，释放仅本次Prepared实例；清理失败保留原错误及InventoryDropRollback阶段，残留实例仍不可见、不可拾取。没有自动重放或用旧档补偿。保存成功后意外ECS提交/激活失败按saved/committed/activated明确记录，不宣称磁盘与ECS可完整回滚或自动恢复。

[掉落存档](MapDropPersistence.md)开启时恢复已提交且未到期的丢弃物；Prepared不保存，关闭时沿旧规则重启清空。丢弃扣减仍先保存，恢复不返还原库存；G继续独立保存入包。没有新增容量/堆叠、物品使用或工具丢弃。

## 【KNOWN ISSUES】验证与人工边界

v10/13丢弃阶段正常Unity编译通过，新增输入共13字段及所属反馈Serializer已生成；当前配置、PrefabPrepared初值和玩家反馈零初值已静态核对。Forest/Grassland各八次隔离Editor烘焙，共16次：Json/BuiltIn默认、丢弃关闭、面板和F HUD均关闭、敌人掉落关闭、工具/砍树/采矿关闭、Single=2且All关闭、仅木材列表；两来源一致，设置/定义/原Prefab引用符合契约，默认地图布置保持，临时World/Scene释放且主场景干净。原SubScene正常重新导入已执行，未运行游戏模拟或GUI回调。

首次写入新JSON期间旧程序集导入产生两条inventoryDrop未知字段异常；完成编译后的严格读取和16次烘焙均成功，Console前后均[2 Error,2 Warning,0 Log]，无新增烘焙错误/警告。两条编译警告来自未修改的PEListener和DOTweenPreviewManager。保留原日志，没有清空Console。

用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，范围限CombatPrototypeNetCode、schemaVersion=10/configRevision=13及[运行入口](Runtime.md)丢弃十二项清单。人工结论来自用户反馈；未实际触发的按钮布局/命中、事件顺序、同tick、延迟/预测回放、多玩家/晚加入、独立保存/准备创建/提交/清理及文件替换后意外ECS恢复仍为UNKNOWN。用户此前面板v9/12、工具v8/11、HUD v7/10等人工通过仍限各自原版本/清单，不覆盖新增丢弃。字体、运行性能/带宽、平台/线上未验收；AI未执行PlayMode、逻辑单元测试、命令行构建、发布、性能采样或图片检查，未创建子Agent或提交Git。

v11/14阶段新增[G提示](MapPickupHud.md)，与实际拾取共用资格/最近目标并显示实际数量；inventoryDrop列表不控制提示标签。未改变丢弃事务、请求/反馈、原13输入字段或v2存档；G新显示静态及用户人工通过，限v11/14十项，未触发用例UNKNOWN，丢弃用户通过仍限v10/13十二项。

v12/15的[资源高亮](MapInteractionHighlight.md)只读取原F/G目标快照和对应客户端资源/掉落位置，不改变Drop/All请求、反馈、保存/扣料或地面生命周期。原13输入与玩家Ghost字段保持；高亮静态及用户人工通过限v12/15十项，未触发独立用例UNKNOWN，旧丢弃/G文字通过仍限原版本/清单。

v13/16的[资源状态](MapResourceStatusHud.md)新增玩家所属四字段，只读采集物/树/矿，不读取掉落到期或改Drop/All请求、反馈、保存/扣料。原13输入与F/G各四字段保持，玩家烘焙布局新增状态组件，各端须同版重新烘焙。新显示静态及用户人工通过，限v13/16十项，未触发用例UNKNOWN，旧通过保持原范围。

当前v23/26的[修理](MapToolRepair.md)两个请求加入原HasPriorOperation：同tick有修理时Drop拒绝，即使修理被拒绝也不改为丢弃。RepairSystem位于Craft之后、Drop之前；原F/G/E/R/1/2互相规则、Drop/All事务、输入物品/模式及所属反馈保持，当前总输入19字段。新链静态及用户人工通过，限v14/17十二项，未触发用例UNKNOWN。

## 【FACT】地图资源存档接入边界

世界v2档可包含地面掉落、DropId/剩余寿命，详见[掉落存档](MapDropPersistence.md)；原Drop/All先保存后扣库存并激活、G保存入包和修理优先保持。资源档写失败不回滚丢弃，资源存档人工通过限v15/18十二项，未触发用例UNKNOWN，见[资源存档](MapResourcePersistence.md)。

寿命提示v17/20归[G提示](MapPickupHud.md)：所属G六字段，原目标/拾取/期限/保存保持；静态及用户人工通过限十二项，未触发独立用例UNKNOWN；旧用户通过仍限原版本/清单。

地图v18/21的[F5/保存提示](MapWorldSaveHud.md)已接入：该阶段17输入、新增所属3字段；原F/G、工具及世界/玩家存档格式保持。静态及用户人工通过限v18/revision21十二项，未触发独立用例UNKNOWN；旧通过限原版本/清单。

## 【FACT】容量等级接入边界

[升级](MapInventoryCapacityUpgrade.md)：升级请求不改变原丢弃资格；同tick丢弃优先、升级拒绝，当前19输入，丢弃候选携带实际Level。新链静态及用户人工通过限v20/revision23升级十六项，未触发用例UNKNOWN；旧通过保持原版本/清单。

## 【FACT】落地后合并边界

v26/revision29的[地面合并](MapDropMerge.md)只处理已登记Landed物，原Prepared半成品不参与，丢弃仍先保存扣料/激活及原Drop/All数量。落地后同ItemId整份合入较小编号的最近合格堆，默认上限99，不拆分原All大堆或追加玩家保存；有限寿命取最早期限，有限/永久不混合。丢弃开关不关闭已有地面物合并，合并开关不改变丢弃按钮/输入/所属反馈。新链静态及用户人工通过限v26/revision29十六项，未触发独立用例UNKNOWN；旧丢弃用户通过保持原版本/清单。

## 【FACT】丢弃堆的按容量领取

当前v34/revision37的[部分拾取](MapDropPartialPickup.md)适用于原Drop/All落地物；默认G只领取当前总量/单种余量可容纳的部分，原大堆不按99钳制或新建拆分实体，剩余量保留原身份/期限。丢弃数量、保存/激活/回滚与反馈保持；partialPickupEnabled独立于丢弃及敌人掉落开关，静态及用户人工通过限v27/revision30十六项，未触发独立用例UNKNOWN。

搜索仅改变可见行，原DrawRow继续按真实Row.Name解析Kind/Mode；搜索变化引起身份/顺序/数目变化时沿原许可取消未完成行点击，已排队/已消费请求保持。搜索静态及用户人工GamePlayer通过，限v29/revision32及运行入口十六项，结论来自用户反馈；未实际触发的独立点击/丢弃用例UNKNOWN，服务端丢弃事务保持。

当前[本机偏好](MapInventoryPreferences.md)恢复显示模式/查询时，仍在首次Snapshot前完成，原Drop/All按真实Name与稳定Kind/Mode处理。偏好不保存未提交业务按钮请求，也不调用服务端丢弃/库存保存；新恢复行为人工待验收，旧丢弃与搜索通过保持原范围。
