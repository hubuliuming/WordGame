# 制作配方搜索

返回[地图](Map.md)、[B面板](MapInventoryPanel.md)、[配方分类](MapInventoryRecipeFilter.md)、[材料搜索](MapInventorySearch.md)、[偏好](MapInventoryPreferences.md)、[消耗确认](MapInventoryFavoritesConsumptionConfirm.md)、[修理](MapToolRepair.md)、[工具升级](MapGatherToolUpgrade.md)、[配置](DataResources.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode，原B面板；当前Forest/Grassland Json及BuiltIn均为v42/revision45。用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v40/revision43及运行入口十六项清单；人工结论来自用户反馈，未触发独立用例仍UNKNOWN。分类已验收仍限v39/revision42十六项，各旧通过保持原版本/清单。

## 【FACT】职责与调用链

新增普通[RecipeSearch](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryRecipeSearch.cs)，GUID=cd0debd2968f78246bd4595564ced2a6；持有第二个原[Search](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSearch.cs)实例、七项配置文本和显隐数组，按搜索Revision/分类Mode变化匹配。Search新增ConfigureRecipeSearch及string Matches重载，材料Configure/Row匹配、清洗、焦点和IME规则保持。没有MonoBehaviour/ECS组件或挂载新增。

[Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs)在原Configure接入新实例；有效Show捕获完整库存后应用搜索/分类并更新显隐，文本或分类改变沿原ClearRecipeRequests清七个未消费B请求、待确认/决定与两种鼠标许可并滚动归零。原七项资格、候选和反馈仍全部Capture。Draw仅读缓存；原输入链为PlayerInput→InteractionHudBinding.ReadPanelInput→InteractionHud.ReadPanelInput→Panel.ReadInput，前三级源码/接口与十九字段布局保持。

搜索本体涉及Panel、Search、[RepairPanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolRepairPanel.cs)、[ToolUpgradePanel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapGatherToolUpgradePanel.cs)、[DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs)、[Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs)，两JSON以及新辅助类/正常Unity导入meta。RecipeFilter及Preferences/Data/Store的当前恢复/保存接入归[配方偏好](MapInventoryRecipePreferences.md)；CapacityUpgradePanel、Confirmation、Favorites、Snapshot/ListView/Details、DropClient、HUD/Binding、PlayerInput和服务端源码保持；Scene/SubScene/Prefab/Animator/旧meta、资源/字体/包/构建结构保持。

## 【FACT】JSON契约

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与BuiltIn一致schemaVersion=42/configRevision=45。inventoryPanel原100字段与搜索七字段保留，另有[配方偏好](MapInventoryRecipePreferences.md)两字段及[配方收藏](MapInventoryRecipeFavorites.md)九字段，当前118；搜索字段如下：

| 字段 | 默认值 | 映射与校验 |
|---|---|---|
| recipeSearchEnabled | true | 严格bool→RecipeSearchEnabled byte |
| recipeSearchIgnoreCase | true | 严格bool→RecipeSearchIgnoreCase byte |
| recipeSearchMaxLength | 32 | 严格int，1～64→RecipeSearchMaxLength int |
| recipeSearchLabel | Recipe search | 文案→RecipeSearchLabel FixedString64Bytes |
| recipeSearchPlaceholderLabel | Name or action | 文案→RecipeSearchPlaceholderLabel FixedString64Bytes |
| clearRecipeSearchLabel | Clear | 文案→ClearRecipeSearchLabel FixedString64Bytes |
| noRecipeSearchResultsLabel | No matching recipes | 文案→NoRecipeSearchResultsLabel FixedString64Bytes |

四文案非空白、无控制字符、最多61 UTF-8字节。长度按UTF-16单位限制，复用原粘贴控制字符/破损代理清洗；合法代理对占两个单位。关闭搜索/面板或原显示能力仍完整验证。DTO/Settings各118字段：DTO二十四bool、六float、五int、三模式string、79文案string及一偏好文件ID；Settings二十四byte、六float、五int、三byte枚举及80 FixedString64Bytes，零GhostField/无GhostComponent。

原严格UTF-8、对象完整、缺失/null/错类型/未知/重复键与语义校验保持；只接受schema42，旧v1～v41及未来版本拒绝，不补默认或回退来源。正常导入/烘焙后生效，无热重载，各端同版。原设置、玩家存档v4、世界存档v2保持；偏好已为v5十字段并严格兼容v1～v4，详见[配方偏好](MapInventoryRecipePreferences.md)。

## 【CURRENT STRATEGY】匹配与显示

搜索为独立已应用文本，Trim后以整个关键词作子串查询，不拆词；使用OrdinalIgnoreCase或Ordinal。配置缓存用换行分隔名称/操作文案：

| 原操作 | 参与匹配的配置文本 |
|---|---|
| CraftAxe / CraftPickaxe | 对应DisplayName，CraftLabel、CraftButtonLabel、RecraftLabel |
| RepairAxe / RepairPickaxe | 对应DisplayName，RepairLabel、RepairButtonLabel |
| CapacityUpgrade | 容量UpgradeLabel、UpgradeButtonLabel、面板CapacityLabel |
| UpgradeAxe / UpgradePickaxe | 对应DisplayName，工具UpgradeLabel、UpgradeButtonLabel |

最终显隐=分类允许组与关键词匹配的交集；空或全空白词显示当前分类全部项。默认Pickaxe匹配三项镐子操作，Repair匹配两项修理；Axe也包含于Pickaxe，按子串规则可匹配两种工具。匹配文本来自配置，不自动增加翻译/拼音/别名，材料数量、成本、缺少材料、资格和反馈状态不加入搜索。匹配到的未持有/缺料/满耐久/满级/功能关闭项仍沿原文案与按钮资格。

材料列表、完整容量摘要、工具标题与两工具状态保持。七项全不匹配时只增加一行No matching recipes；空组标题、工具升级反馈及隐藏配方提示不留空白，工具状态继续显示。材料排序/分类/搜索/收藏筛选、完整Snapshot资格与材料偏好沿原规则，与配方查询独立。

## 【CURRENT STRATEGY】几何、焦点与生命周期

材料搜索之后、Reset view之前追加标题及文本框/Clear两行；关闭搜索不占行。原材料字段命中Y保持，配方字段Y=(3+Favorites.CountRowCount+ListView.ControlRowCount+材料Search.RowCount)×RowHeight。二者共用缩放/滚动/viewport裁切及75%输入框/25%Clear布局；每实例有独立GUI控制名，不自动聚焦。

ReadInput每帧采样两实例，BlocksKeyboard/BlocksMouse取并集；Draw在原OnGUI先为两者FlushGUIFocus。编辑及获得/释放焦点同帧屏蔽B、WASD、空格、E/R/F/G/F5、1～7和镜头键盘；Enter/小键盘Enter/Esc仅在IME composition为空时释放。面板外释放焦点的左键沿原规则被消费，指针矩形/鼠标攻击与镜头隔离、GUI matrix/color/enabled恢复保持，原未编辑时数字直达链保持。

常驻五行和原材料/收藏/控制/详情高度保持；配方搜索0～2行。Craft空组0行、一个配方5行、两个9行；Repair空组0、一个6、两个11行；ToolUpgrade空组0、一个8、两个14行（标题及所属反馈共两行）；容量整块可见时原10行。只计可见项的收藏消耗提示和原确认0～1行，空结果另1行；Repair/ToolUpgrade的Draw与VisibleRowCount、VisibleConsumptionHintRowCount使用同一双显隐标志。工具耐久开启仍额外两行。

实际已应用文本改变或分类切换清原七请求/待确认并归零滚动，即使匹配结果/高度相同也清旧鼠标许可；不撤销已提交服务器事务，不清Drop请求、已应用收藏或材料选择。Reset view清配方草稿/查询/待编辑并恢复配置默认类别；Close丢弃未应用草稿、释放焦点，保留同绑定已应用关键词和类别。逐帧Clear仅隐藏，不清查询/编辑；无效ReadInput释放焦点并沿原清请求。Reset/死亡/断线/源、玩家、World或Scene变化沿原绑定Reset清旧关键词，新绑定先初始化空查询，再按[配方偏好](MapInventoryRecipePreferences.md)开关/正式记录恢复。

配方查询的已应用值传入Preferences，按PreferencesSaveRecipeSearch与RecipeSearchEnabled参与原延迟保存/恢复；原材料搜索仍由PreferencesSaveSearch独立控制。偏好v5十字段、严格v1～v4兼容及关闭项保留归[配方偏好](MapInventoryRecipePreferences.md)。服务端资格/材料投影/SavePrepared顺序、输入/RPC/Ghost及玩家/世界存档协议保持。

## 【CURRENT STRATEGY】配方收藏与分区

原分类/搜索交集保持，[配方收藏](MapInventoryRecipeFavorites.md)在其后按稳定操作ID分为收藏/其他两区，实际收藏变化沿原取消规则；原七项完整Capture保持，三个预览Draw追加独立收藏控制，偏好当前v5十字段。

## 【KNOWN ISSUES】静态核对与人工边界

本节静态证据及用户反馈仍限搜索阶段v40/revision43；当前v41/revision44偏好接入证据归[配方偏好](MapInventoryRecipePreferences.md)。

正常Unity编译/重载及107字段类型/零GhostField、新普通类GUID/显隐属性核对通过；原分类/确认/收藏/详情/丢弃GUID和确认/预览Capture契约保持，输入19/Tools3/DropGhost4、请求2/F4/G7/资源状态4/世界保存3与各所属反馈、偏好v3/玩家v4/世界v2元数据保持。

4336份非法配置全部拒绝（每地图2168），502组合法读取通过（每地图251），覆盖完整107字段/类型/形状/重复键、两个严格bool、1～64整数边界及浮点字面量、四文案空白/控制/UTF-8限制、关闭仍验证、旧v1～v39/未来版本及原规则。两地图各251次、共502次隔离Editor Bake完成，原209变体加42个配方搜索变体，覆盖两开关、长度1/64、四文案各ASCII61/UTF-8 61/中文、面板关闭、十三独立显示开关、两搜索同时关闭、四关闭分类组合、四原能力/修理关闭；全部107Settings、原Settings/零反馈/Prefab引用、布局及兼容签名符合检查。Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。

源SubScene只读，临时克隆/TextAssets/Scene/World/BlobAssetStore释放，主场景干净、3根对象、单场景、源SubScene未加载、未Play/未编译。Console执行前、Bake前后及结束均[3 Error,4 Warning,0 Log]，本次未新增Error；保留前阶段两条旧DTO未知字段异常、Unity账号Token Exchange异常、原两编译警告及两MCP连接警告，未清Console。Bake工具返回空失败状态，已核实完整502条落盘结果及结束状态，未重跑业务；结束核对脚本一次LINQ枚举不兼容，改为原生foreach完成核对，生产代码保持。

原579项人工内容/编号逐字保留，追加十六项后共595项，归运行入口本阶段章节；本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v40/revision43及运行入口清单；人工结论来自用户反馈，未实际触发的独立用例仍UNKNOWN。独立匹配/焦点/IME/粘贴、GUI事件/命中/滚动/裁切/字形/分辨率、请求取消时序/隐藏项更新、真实偏好/游戏保存I/O与故障、材料事务、多人/预测/延迟、性能、平台及线上未实际触发为UNKNOWN。

AI未调用配方搜索/材料搜索/确认/面板/预览/GUI业务或真实偏好与游戏存档I/O，未执行GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片；未创建子Agent、暂存或提交Git。
