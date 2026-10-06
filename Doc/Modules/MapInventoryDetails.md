# 背包材料详情与用途提示

返回[地图](Map.md)、[B面板](MapInventoryPanel.md)、[背包](Inventory.md)、[排序筛选](MapInventoryListView.md)、[搜索](MapInventorySearch.md)、[偏好](MapInventoryPreferences.md)、[配置与存档](DataResources.md)及[运行验收](Runtime.md)。入口CombatPrototypeNetCode，原B面板；Forest/Grassland Json/BuiltIn当前v39/revision42，[收藏](MapInventoryFavorites.md)人工已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN。主线程按已确认方案完成代码、配置和静态核对，用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v32/revision35及运行入口十六项清单，人工结论来自用户反馈；重置用户通过仍限v31/revision34十六项，保存/搜索与其他旧通过保持各自原版本/清单。

## 【FACT】入口与职责

| 文件 | 职责 |
|---|---|
| [Details](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelDetails.cs) | 原Panel持有的普通C#类，排队Name/关闭、详情缓存、配方用途及测量/绘制 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 原Configure/Show/GUI/输入隔离、滚动及生命周期接入 |
| [Snapshot](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSnapshot.cs) | 新增DisplayMaximum，只读当前等级受管上限；容量关闭或未受管返回0 |
| [ListView](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelListView.cs) | 新增CategoryLabel，复用原TypeRank/类别文案，不改变排序筛选 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)/[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原inventoryPanel追加十四字段 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)/[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)/[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 全量校验、建议默认值及原根Settings映射 |

v32详情阶段仅八现有脚本、一新普通Details类及正常Unity生成meta、两地图JSON接入；新GUID已注册，没有新MonoBehaviour/组件或挂载。Scene/SubScene/Prefab/Animator/旧meta/资源/图片/字体/包/构建配置、HUD/Binding/PlayerInput、服务器及保存代码保持。

## 【FACT】配置与建议默认值

| inventoryPanel字段 | 两地图Json/BuiltIn值 |
|---|---|
| detailsEnabled | true |
| detailsButtonLabel | Details |
| detailsTitleLabel | Material details |
| detailsCloseLabel | Close details |
| detailsQuantityLabel | Quantity |
| detailsDescriptionLabel | Description |
| detailsUsageLabel | Uses |
| detailsNoUsageLabel | No listed uses |
| detailsUnknownDescriptionLabel | No description configured |
| woodDescriptionLabel | Material from trees |
| stoneDescriptionLabel | Material from ore nodes |
| appleDescriptionLabel | Supply from vegetation |
| meatDescriptionLabel | Supply from enemy rewards |
| meatUsageLabel | E: Restore power |

原详情十四字段保留，另有[收藏](MapInventoryFavorites.md)六字段、[收藏筛选](MapInventoryFavoritesFilter.md)六字段、[收藏计数](MapInventoryFavoritesCount.md)两字段、[收藏保护](MapInventoryFavoritesDropProtection.md)两字段、[收藏提示](MapInventoryFavoritesConsumptionHint.md)两字段与[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)四字段及[配方筛选](MapInventoryRecipeFilter.md)七字段；当前完整DTO十八bool、六float、三int、三模式string、69文案string及一文件ID，共100；Settings十八byte、六float、三int、三byte枚举及70 FixedString64Bytes，共100，零GhostField/无GhostComponent。新开关必填严格bool→byte，十三文案必填非空白、无控制字符、最多61 UTF-8字节→FixedString64Bytes；关闭详情/面板/原能力仍完整验证。地图schema39/revision42，各端同版，旧v1～v38拒绝；无补默认、来源回退或热重载，正常导入/烘焙生效。

类别复用原Resources/Supplies/Other文案，类别标题复用TypeOrderLabel；容量关闭或未受管材料复用Unlimited。默认英文，中文配置接口保留，实际字形/分辨率覆盖UNKNOWN。

## 【CURRENT STRATEGY】选择、刷新与布局

原HUD Binding→HUD.Configure/Show→Panel.Configure/Show传入完整Settings及原工具/容量定义；Details不持有跨帧DynamicBuffer，只通过原Require方法复制合法定义值，必需定义缺失沿原异常暴露，不补配置。Snapshot仍验证完整库存，ListView仍只过滤/排序可见行。

材料名称行左侧使用原Text（收藏开启且已收藏时追加配置标记），右侧30%宽度Details按钮，4像素间隔；关闭detailsEnabled时名称恢复整宽及原行高。按钮沿原_rowMousePressAccepted只排队真实Name，不在GUI内遍历/修改库存或写文件。下一有效Show在完整Snapshot/可见行刷新后消费一次，按当前可见Name查找；排序保留同一材料，不以行索引/显示名绑定；已不可见或消失时清选择。最多展开一个材料，重复选择同一材料不另建详情。

展开区位于所选材料原Drop/All及启用的收藏控制行之后，显示标题、全宽Close details行、显示名、类别、当前Quantity/单种容量、来源简介与用途。GUI关闭同样只排队，下一有效Show清详情。正文与标题复制原labelStyle后开启wordWrap，CalcHeight按真实宽度测量，至少一原行高；内容/宽度/行高变化时重算，实际展开高度加入原滚动区。原搜索控件位置和命中公式保持，固定标题/页脚及Drop/All行几何保持。

详情文本按原Snapshot.Revision、工具拥有状态/等级及容量等级缓存，不重复建立可变库存。数量/容量/工具等级变化刷新；材料归零从可见行移除后关闭。展开/关闭/刷新改变详情几何时取消旧面板及行按下许可，避免点击沿旧位置作用于后续按钮；已排队的真实Name丢弃/制作/修理/升级或已提交服务器动作保持。

显示重置、B/关闭按钮、原无效ReadInput及绑定Reset清详情选择/请求，逐帧Clear仍仅隐藏，正常绑定不因此丢选择。死亡/断线、源或玩家/World/Scene变化沿原绑定释放处理；详情选择不保存，关闭再开或重绑不自动恢复。搜索/焦点与原面板指针隔离代码保持，无额外快捷键或弹窗。

## 【CURRENT STRATEGY】用途的真实来源

木材/石材按当前启用功能列用途，只显示所选材料的实际正消耗数量，不存第二套配方，不判断本次服务器可执行性：

- 工具总开关启用：基础斧头/镐子Lv1制作，读取原CraftWoodQuantity/CraftStoneQuantity；仍显示基础制作用途，已持有工具的重制资格归原面板/服务器。
- 工具与修理开关启用且持有工具：读取原Show传入当前有效等级的RepairWoodQuantity/RepairStoneQuantity；满耐久或缺料仍可显示用途，原资格保持。
- 工具与升级开关启用且持有未满级工具：按ToolId+Level读下一档UpgradeDefinition木/石需求；未持有或Lv3不列升级项，定义数组顺序不影响匹配。
- 容量与升级开关启用且未满级：读取下一档InventoryCapacityUpgradeDefinition需求；Lv3不列扩容项。

小块肉只显示可配置E用途说明，不触发E或修改体力。苹果没有在本阶段新增使用动作，显示No listed uses；其他合法材料显示配置的通用简介/无用途记录。原配方按钮、业务资格、扣料/发物/工具耐久、输入/Ghost及玩家v4/世界v2保存保持；当前偏好v3七字段兼容v1五字段/v2六字段，详情选择仍不保存。

## 【CURRENT STRATEGY】配方分类关联

当前v39/revision42的[配方筛选](MapInventoryRecipeFilter.md)只控制B配方显示、切换取消及行数；材料与工具状态、原输入/事务/存档链保持。分类不写偏好v3，完整契约与本阶段待人工范围归专题；既有通过限原版本/清单。

## 【KNOWN ISSUES】静态证据与人工边界

以下编译/烘焙及人工结论仅限v32/revision35详情阶段。该阶段正常Unity编译/重载完成；71配置/Settings、零GhostField、普通类注册/加载及原协议元数据核对通过。2414份非法配置全部拒绝（每地图1207），128组合法读取通过（每地图64），覆盖完整71字段、十四新增项、标量/字段/重复键/非有限/文案界限、关闭仍校验、旧v1～v31/未来版本及原规则。

两地图各64次，共128次隔离Editor Bake通过：保留原56变体，增加详情关闭、详情/面板关闭、原偏好/显示控件关闭而详情开启、十三文案ASCII/UTF-8 61字节/中文、用途功能关闭、工具与容量升级定义换序。全部71Settings与原Settings/零反馈/Prefab引用、布置/资源兼容签名匹配；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。只读源SubScene，临时副本/TextAsset/Scene/World/BlobAssetStore释放，主场景干净、3根对象、未Play。后续仅详情标题/关闭行高度适配，正常编译完成；配置/烘焙契约未改变。

执行前Console[1 Error,2 Warning,0 Log]，Error为已有UnityConnect Token Exchange失败，两条既有PEListener/DOTween源码Warning；编译/重载后出现MCP WebSocket未初始化Warning。隔离Bake前后[1 Error,3 Warning,0 Log]一致，没有新增烘焙条目；最终正常重载后Console[1 Error,2 Warning,0 Log]，保留UnityConnect Error及两条源码Warning。未清空Console，未宣称0 Error。

原451项人工内容/编号保留，新增十六项后467项，归[运行入口](Runtime.md)，本阶段人工十六项已获用户通过反馈，范围限上述版本/清单；旧用户通过仅限各自原版本/清单。未实际触发的独立详情按钮/Name选择/数量与配方刷新、滚动/换行/字形/输入焦点、关闭/重绑/多人/延迟/预测/故障用例仍UNKNOWN，性能/平台/线上未获单独验收结论，仍UNKNOWN。AI未执行Details/Panel/Snapshot/ListView/GUI业务或偏好/游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片，未创建子Agent或提交Git。

## 【CURRENT STRATEGY】收藏后的详情选择

v33/revision36收藏阶段只将收藏标记后的显示文字传入DrawItemLabel，详情身份仍按真实Name匹配；收藏重排保持同名详情，分类/搜索隐藏或数量归零仍关闭，收藏状态本身继续保留。展开位置跟随所选材料收藏行，v33阶段77配置/Settings及146次隔离Bake静态通过，人工收藏十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN，范围归[收藏](MapInventoryFavorites.md)；详情既有通过仍限v32/revision35十六项。

## 【CURRENT STRATEGY】收藏筛选后的详情

当前v39/revision42复用原Details.Capture：切到仅看收藏后所选真实Name不再可见即关闭详情；仅看收藏中取消所选收藏，在同一次有效刷新先移除行，再关闭详情，不恢复隐藏选择。详情代码、选择身份及高度测量沿原；新控制行高度由Panel/ListView统一计入。完整交集/鼠标许可与人工边界归[收藏筛选](MapInventoryFavoritesFilter.md)，当前阶段十六项已获用户通过反馈，限v34/revision37及运行入口清单；未触发独立用例仍UNKNOWN；旧详情/收藏通过保持各自版本/清单。

## 【FACT】收藏计数与详情布局

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)只在容量后增加可选固定一行，Panel内容高度计入CountRowCount，原详情选择/测量/展开代码保持；计数文字变化不增减详情高度。原收藏变化/身份变化的鼠标许可规则保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；详情及筛选旧通过保持各自版本/清单。

## 【FACT】收藏保护与详情

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：Details代码、选择Name、实际高度与详情文案保持；只读保护提示使用原丢弃操作行固定高度，收藏按钮和详情按钮仍沿原许可。取消收藏或Reset view实际清集合后的下一有效Show恢复原丢弃行。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

v37/revision40提示阶段的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v37/revision40及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。

## 【CURRENT STRATEGY】收藏材料消耗确认

当前v39/revision42的[收藏材料消耗确认](MapInventoryFavoritesConsumptionConfirm.md)仅处理原B七个制作/修理/容量与工具升级按钮。首次有效按钮请求命中已应用收藏木石的正成本时暂存一个操作，在配方内显示数量提示，以Confirm/Cancel替换原按钮行；确认按最新已捕获候选和本地Revision复核后沿原请求提交一次，取消/关闭B/相关数量、等级、耐久、配方或已应用收藏变化/绑定失效清待确认。逐帧Clear仅隐藏，确认目标或高度变化清旧面板与行鼠标许可。确认独立于原消耗提示开关；数字1～7保持原直达链，偏好v3七字段、输入19、Ghost/服务器与保存入口保持。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v38/revision41及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN，已验收提示仍限v37/revision40，全部旧通过保持原版本/清单。
