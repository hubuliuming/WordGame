# 制作配方分类与筛选

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[材料展示](MapInventoryListView.md)、[本机偏好](MapInventoryPreferences.md)、[收藏消耗确认](MapInventoryFavoritesConsumptionConfirm.md)、[修理](MapToolRepair.md)、[容量升级](MapInventoryCapacityUpgrade.md)、[工具升级](MapGatherToolUpgrade.md)、[配置](DataResources.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode，原B面板；两地图Json/BuiltIn当前v42/revision45。已按确认方案落地及完成静态范围核对；本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v39/revision42及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN。已验收确认仍限v38/revision41十六项，其他旧通过保持原版本/清单。

## 【FACT】文件与接入点

| 文件 | 当前职责 |
|---|---|
| [RecipeFilter](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryRecipeFilter.cs) | 普通C#辅助类；四模式byte枚举、严格模式Resolver/Restore、本地默认/已应用模式、待切换bool、缓存按钮文本和三组显隐 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | 持有分类实例；原Configure/Show/Draw/Close/Reset接入，类别切换清七请求/待确认/旧鼠标许可，按可见组计算行数 |
| [DTO](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Settings](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原结构尾部追加七必填显示字段 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | schema42/revision45、显式默认、严格模式/文案验证及原地图根七字段映射 |

分类实现范围为六现有C#、两JSON及一个普通C#辅助类/正常Unity导入meta；RecipeFilter GUID=b11ee8ac85c058e4884416ea3c754db6，无新ECS/MonoBehaviour、挂载或Scene/SubScene/Prefab/Animator/旧meta/资源/字体/包/构建结构变更。当前[配方搜索](MapInventoryRecipeSearch.md)复用Search并调整Panel及修理/工具升级Draw；当前新增类别Restore、Panel与Preferences/Data/Store的保存接入归[配方偏好](MapInventoryRecipePreferences.md)。三个预览Draw的配方收藏扩展归[配方收藏](MapInventoryRecipeFavorites.md)；Confirmation、Favorites、Snapshot/ListView/Details、DropClient、HUD/Binding、PlayerInput及服务端源码保持。

## 【FACT】JSON契约与默认值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与BuiltIn均为schemaVersion=42/configRevision=45，原inventoryPanel93字段及分类七字段保留，当前另有[配方搜索](MapInventoryRecipeSearch.md)七字段及[配方偏好](MapInventoryRecipePreferences.md)两字段及[配方收藏](MapInventoryRecipeFavorites.md)九字段，共118；分类字段为：

| 字段 | 默认值 | 校验与映射 |
|---|---|---|
| recipeFilterEnabled | true | 必填严格bool→RecipeFilterEnabled byte |
| defaultRecipeFilterMode | all | 必填严格模式string→DefaultRecipeFilterMode byte枚举 |
| recipeFilterLabel | Recipes | 必填文案→RecipeFilterLabel FixedString64Bytes |
| allRecipesLabel | All | 必填文案→AllRecipesLabel FixedString64Bytes |
| craftRecipesLabel | Craft | 必填文案→CraftRecipesLabel FixedString64Bytes |
| repairRecipesLabel | Repair | 必填文案→RepairRecipesLabel FixedString64Bytes |
| upgradeRecipesLabel | Upgrade | 必填文案→UpgradeRecipesLabel FixedString64Bytes |

模式仅逐字接受all/craft/repair/upgrade，不修剪或转换大小写；枚举All=0/Craft=1/Repair=2/Upgrade=3。五文案非空白、无控制字符、最多61 UTF-8字节。关闭分类/面板或其他显示能力仍完整验证默认模式和文案；Settings保留配置默认，客户端关闭分类时显示All并隐藏控制行。

DTO/Settings各118字段：DTO二十四bool、六float、五int、三模式string、79文案string及一偏好文件ID；Settings二十四byte、六float、五int、三byte枚举及80 FixedString64Bytes，零GhostField/无GhostComponent。原严格UTF-8、完整对象/类型、缺失/null/未知/重复键及语义验证保持；旧v1～v41和未来版本拒绝，不补默认或回退来源。正常导入/烘焙生效，无热重载，各端同版。

## 【CURRENT STRATEGY】四类配方与原资格

| 模式 | 可见配方 |
|---|---|
| All | 分类允许原七项制作/重做、修理、容量与工具升级，分类/关键词后按[配方收藏](MapInventoryRecipeFavorites.md)分区，区内保持原组顺序 |
| Craft | 原斧头/镐子制作或重做两项 |
| Repair | 原斧头/镐子修理两项 |
| Upgrade | 原容量升级、斧头升级、镐子升级三项；容量升级整个预览块跟随此类 |

分类按操作类型决定允许组，最终可见项同时与[配方搜索](MapInventoryRecipeSearch.md)关键词取交集，不按可制作性过滤；缺料、未持有、功能关闭、满耐久或满级仍沿原可见组文案与按钮资格。材料列表、完整容量摘要、工具标题及两个工具状态始终沿原绘制；排序/材料分类/搜索/收藏筛选只影响材料可见行，不影响配方类别或原完整Snapshot资格。

Panel.Show仍捕获全部七项候选与三个原预览，不因隐藏停止Capture，隐藏组的数量/耐久/等级/原所属反馈继续沿原缓存更新。数字1～7保持PlayerInput原直达链；搜索编辑及焦点切换同帧仍沿原blocksKeyboard规则。类别的本机显示记录归配方偏好，输入/RPC/Ghost、服务器收藏、材料事务与玩家/世界保存结构保持；SavePrepared及服务端资格保持原顺序。

## 【CURRENT STRATEGY】控制行、请求与生命周期

材料可见行之后、原容量升级块之前追加一个全宽Recipes: All按钮行，沿原滚动区与字体；点击按All→Craft→Repair→Upgrade→All循环。GUI只排一个本地待切换bool，下一有效Show在完整库存捕获后、原业务Capture前应用一次，更新缓存文本并归零滚动。

实际类别变化清原两制作、两修理、容量升级、两工具升级七个未消费B请求，调用原Confirmation.ClearPending清待确认和未消费决定，清_mousePressAccepted/_rowMousePressAccepted；不清Drop Kind/Mode、已应用收藏或材料显示选择，不回撤已消费输入/服务器事务。Reset view先丢弃分类待切换并恢复配置默认，实际类别改变也沿同一取消规则，不重复应用旧GUI切换。

Close/B关闭及无效ReadInput清未应用类别请求；同一绑定已应用类别保留。Reset/新绑定经Configure先恢复配置默认，再按[配方偏好](MapInventoryRecipePreferences.md)开关/正式记录恢复类别；原死亡/断线/源或玩家变化/World或Scene失效沿已有绑定Reset进入此路径。逐帧Clear仍只隐藏并保留类别及待切换，继续原容量/工具升级待请求清理。

类别接入偏好v4的recipeFilterMode；原偏好关闭/I/O暂停不影响本地分类，preferencesSaveRecipeFilter或分类能力关闭时保留文件原值，其他显示变化仍沿原保存规则。Reset view恢复配置默认类别，按开关参与原延迟保存；完整迁移/恢复/保存与失败规则归[配方偏好](MapInventoryRecipePreferences.md)，真实偏好和游戏保存I/O未由AI执行。

## 【CURRENT STRATEGY】内容高度与鼠标

以下为配方收藏关闭且关键词为空时的基础分类行数，收藏开启按[配方收藏](MapInventoryRecipeFavorites.md)两区合计，单项匹配及双搜索命中几何归[配方搜索](MapInventoryRecipeSearch.md)。原固定14行拆为常驻5行（材料/容量两行及工具标题/状态三行）与Craft9行；Repair原11行，Upgrade原容量10+工具14行。分类控制行0或1；只计可见组的原RowCount及ConsumptionHintRowCount，原待确认RowCount0或1、启用耐久两行、材料/收藏/排序/搜索/重置和详情实际展开高度继续计入。All原组顺序保持；关闭分类恢复All允许组，配方搜索条件仍独立生效，隐藏组不留空白。

可见收藏消耗提示总行数All最多7、Craft最多2、Repair最多2、Upgrade最多3，缓存仍按原完整候选更新。类别变化即使高度相同也清旧鼠标许可；原确认目标、提示高度、详情及行身份变化规则保持。搜索字段位于新控制行之前，其命中Y公式保持；GUI matrix/color/enabled恢复、固定标题/页脚、面板矩形、鼠标攻击/镜头隔离和字号/缩放保持。

## 【KNOWN ISSUES】静态证据与人工边界

本节静态及用户反馈保持分类阶段v39/revision42范围；当前搜索静态证据归[配方搜索](MapInventoryRecipeSearch.md)。

正常Unity编译/重载完成，新meta由正常导入生成；100字段类型构成、零GhostField、新普通类/四模式byte枚举/方法与属性/GUID核对通过。原确认GUID与候选/签名、三预览末两参Favorites/Confirmation、Favorites/DropClient/Details GUID、偏好v3七字段兼容v1/v2、输入19/Tools3/DropGhost4及请求2/F4/G7/资源状态4/世界保存3、各所属反馈、玩家v4/世界v2元数据保持。

4034份非法配置全部拒绝（每地图2017），418组合法读取通过（每地图209），覆盖完整100字段/形状/类型/重复键、分类严格bool、四合法默认/大小写和空白错误、五文案空白/控制/UTF-8限制、关闭仍验证、旧v1～v38/未来版本及原规则。两地图各209次、共418次隔离Editor Bake完成，保留原167变体并追加四默认/四关闭默认、面板关闭、五文案各ASCII61/UTF-8 61/中文、十二独立显示开关、四原能力/修理关闭及同时关闭显示共42变体。全部100Settings、原Settings/零反馈/Prefab引用、布局/兼容签名符合检查；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。源SubScene文本只读，临时克隆/TextAssets/Scene/World/BlobAssetStore释放，主场景干净、3根对象、单场景、未Play。

Console执行前[0 Error,0 Warning,0 Log]，正常编译后[3,2,0]，两JSON重新导入后/Bake前[3,2,0]，Bake后及源SubScene正常重新导入后[3,4,0]。初次自动导入中资源工作进程以旧DTO读取新JSON产生两条recipeFilterEnabled未知字段异常；脚本重载后新DTO/Settings均100字段，重新导入两JSON和源SubScene未增加Error，完整读取/418次Bake已核对通过。另有一条Unity账号Token Exchange异常；原PEListener/DOTween两编译警告及Bake两MCP连接工具警告保留，无C#编译Error。未清Console，不将保留三条历史Error写成零错误。Bake工具返回空失败状态，已核实完整418条落盘结果和资源释放，未重跑业务或据工具状态改生产代码。

原563项人工内容/编号逐字保留，追加十六项后共579项，归运行入口本阶段章节。主线程判定实现/静态范围核对通过；本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v39/revision42及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN。旧确认通过不覆盖本分类。真实按钮/循环/清旧请求/确认取消/按下抬起、几何/滚动/裁切/字形/分辨率、隐藏组更新/生命周期/搜索焦点、实际偏好与游戏保存I/O/故障/材料事务、数字直达/多人/预测/延迟及性能/平台/线上未实际触发为UNKNOWN。

AI未调用分类/确认/收藏/面板/预览/GUI业务，未执行真实偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片；未创建子Agent、暂存或提交Git。