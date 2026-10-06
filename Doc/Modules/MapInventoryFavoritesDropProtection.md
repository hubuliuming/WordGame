# 收藏材料丢弃保护

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[丢弃](MapInventoryDrop.md)、[收藏](MapInventoryFavorites.md)、[仅看收藏](MapInventoryFavoritesFilter.md)、[收藏计数](MapInventoryFavoritesCount.md)、[本机偏好](MapInventoryPreferences.md)、[配置与存档](DataResources.md)及[运行验收](Runtime.md)。入口CombatPrototypeNetCode，原B面板；两地图Json/BuiltIn当前v36/revision39。主线程按用户已确认方案完成接入与静态范围核对，本阶段十六项人工GamePlayer待验收。收藏计数用户通过仍限v35/revision38十六项，筛选限v34/revision37、收藏限v33/revision36及其他旧阶段各自原版本/清单。

## 【FACT】入口与修改文件

| 文件 | 本阶段职责 |
|---|---|
| [MapInventoryPanelConfig](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs) | 原DTO追加保护开关与保护文案两字段 |
| [PanelData](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) | 原Settings追加byte/FixedString64Bytes，无新组件 |
| [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs) | 地图schema36；沿原HudLabel校验新文案 |
| [BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs) | 两地图schema36/revision39与显式保护默认值 |
| [Map Baker](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) | 原Settings中烘焙两字段 |
| [Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) | DrawBody/ReadInput向原DropClient传递同一Favorites实例 |
| [DropClient](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryDropClient.cs) | 缓存配置文案/开关，绘制只读保护行，消费未提交请求前再检查 |

共七现有C#与两JSON，没有新增脚本/辅助类/组件/挂载。DropClient仍普通C#类，GUID=6038f8bbb4c46bc48bc521329f32745d；Favorites与Details代码未修改，GUID分别保持fce8d766a3a6f334286842eb7f4d86a8、769e4ceeb5330f64f9a933190640a9a0。Scene/SubScene/Prefab/Animator/全部meta/资源绑定/字体/包/构建结构保持。

## 【FACT】JSON契约与建议值

[Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json)与BuiltIn一致为schemaVersion=36/configRevision=39，原inventoryPanel85字段原顺序/值保留，尾部追加两必填字段。

| inventoryPanel字段 | 当前建议值 | 契约 |
|---|---|---|
| favoritesDropProtectionEnabled | true | 严格bool，Baker写原Settings的FavoritesDropProtectionEnabled byte |
| favoritesProtectedLabel | Favorite protected | 非空白、无控制字符、最多61 UTF-8字节，Baker写FavoritesProtectedLabel FixedString64Bytes |

当前DTO/Settings各87字段：DTO十五bool、六float、三int、两模式string、60文案string及一偏好文件ID；Settings十五byte、六float、三int、两byte枚举及61 FixedString64Bytes。Settings零GhostField/无GhostComponent。新开关、favoritesEnabled、面板或原能力关闭时仍校验全部字段/文案。沿原严格UTF-8、完整对象/类型、缺失/null/未知/重复键与原语义校验；旧地图v1～v35和未来版本明确失败，不补默认、不回退来源。正常导入/烘焙后生效，无运行热重载，各端同版。

实际保护条件为favoritesDropProtectionEnabled启用且原Favorites.IsFavorite(Name)为true；原IsFavorite包含favoritesEnabled门槛并查询已应用真实Name集合。计数、仅看收藏、排序/分类/搜索、详情、偏好I/O、重置按钮开关不构成保护条件。

## 【CURRENT STRATEGY】原操作行的保护提示

原有效Panel.Show捕获完整库存，经ListView.Capture应用原待显示/收藏请求并缓存可见行；Panel.DrawBody将行及同一Favorites传给DropClient.DrawRow。绘制仍先按inventoryDrop开关及原定义检查支持资格，丢弃关闭/不支持条目优先显示原Unavailable；仅其余支持条目按真实Row.Name检查收藏。

命中保护时，原Drop/All操作行绘制全宽只读favoritesProtectedLabel，不绘制丢弃按钮、不排队Kind/Mode。固定行高/材料行数和原滚动几何保持，不增加保护行。未收藏或保护关闭沿原单次数量、All开关、库存有效性及鼠标许可处理。苹果/木材/石材仍由原定义支持；小块肉、工具槽与未配置条目没有新丢弃能力。

保护不按本地显示文案、别名、行序号或库存索引关联。分类/搜索/仅看收藏交集与置顶只改变可见行；隐藏、数量归零或暂不存在的已应用Name不因本阶段删除，原恢复/再次获得同名材料时仍由当前集合决定保护。收藏按钮、详情、计数和焦点/IME/指针隔离沿原代码。

取消收藏或Reset view实际清启用集合后，下一有效Show恢复原操作行。GUI收藏按钮仅排队原Name，在原ApplyPending实际应用前仍读取旧集合；本阶段没有提前应用显示请求或新增立即同步路径，未实际触发的GUI/Show/输入采样时序为UNKNOWN。

## 【CURRENT STRATEGY】未消费请求的二次检查

原输入链为Binding.ReadPanelInput→HUD.ReadPanelInput→Panel.ReadInput，重新验证当前地图、World、所属存活Ghost/Connected/InGame连接后读取按钮请求。Panel调用原DropClient.ReadRequest并传入同一Favorites。方法先取出单个_pending并ClearPending；保护启用且请求Mode非None时，按稳定Kind匹配当前Definition.ItemName，再检查当前已应用收藏。

命中返回default空请求一次，原待请求已清，无自动重试、服务器调用、库存扣减、地面创建或新失败/成功反馈。没有命中时返回原Kind/Mode请求，由原PlayerInput和服务端资格/保存事务处理。DrawRow九参数、ReadRequest一个Favorites参数；Request仍Kind/Mode两字段，原稳定Kind/Mode枚举不变。

边界是尚未消费到PlayerInput的本机请求。已经写入PlayerInput的请求不会因之后收藏、关闭面板或显示重置而撤回；服务端不读取本机收藏，没有服务器收藏同步/新RPC/新输入或Ghost条件。原服务端忙碌/同tick优先级、候选保存、Prepared创建/提交/回滚及所属丢弃反馈保持。

## 【CURRENT STRATEGY】配置、偏好与生命周期

DropClient.Configure沿原Reset后缓存保护bool和文案string；普通GUI读取缓存，不逐次转换配置文字。ReadRequest/Close/原无效输入沿ClearPending清未消费请求，Reset同时清保护开关/文案及原定义/显示/序号缓存。逐帧Clear只隐藏原展示，当前绑定已应用收藏沿原保持；死亡/断线/源或玩家变化及World/Scene失效沿原Reset，合法新绑定重新配置/恢复。

原本机偏好仍CurrentVersion=3/Data七字段，兼容严格v1/v2；没有保护专用偏好字段、保存入口或额外文件写入。合法Restore后保护读取实际集合，读取不新增写盘；关闭偏好或I/O暂停时仍读取本绑定临时已应用集合。原关闭收藏使IsFavorite返回false，关闭保护直接使用原丢弃规则；关闭计数或仅看收藏不解除保护。Reset view沿原清启用收藏与重置显示，再由原偏好观察/提交，不调用业务库存保存。

收藏材料仍计完整库存数量/容量，可供原制作、工具修理/升级、背包升级和E使用；数量归零仍保留原收藏Name。保护不改原输入19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3及全部反馈，玩家v4根7/工具项3、世界v2根9/掉落项8和本机偏好v3七字段保持。

## 【KNOWN ISSUES】静态证据与人工边界

正常Unity编译/重载完成。87字段/零GhostField、普通类身份/GUID、DrawRow九参数/ReadRequest一个Favorites参数/请求两字段、偏好v3七字段及原协议元数据核对通过。3182份非法配置全部拒绝（每地图1591），234组合法读取通过（每地图117），覆盖两地图Json/BuiltIn等价、87字段完整/形状/类型/重复键、保护严格bool/61 UTF-8字节文案与关闭仍验证、旧v1～v35/未来版本及全部原规则。仅读取配置/验证语义和反射元数据，没有执行保护/收藏/面板/GUI业务。

两地图各117次、共234次隔离Editor Bake通过；原101变体保留，追加保护关闭、收藏/面板关闭、保护文案ASCII61/UTF-8 61/中文、计数/收藏筛选/排序/分类/搜索/详情/偏好/重置关闭仍配置保护、保护与收藏同时关闭、原inventoryDrop关闭共16变体。全部87Settings、原Settings/零反馈/Prefab引用、资源布置与兼容签名匹配；Forest89树/36采集/20矿/109阻挡，Grassland53/38/18/71。源SubScene只读；临时克隆/TextAssets/Scene/World/BlobAssetStore释放，主场景CombatPrototypeNetCode干净、3根对象、单场景，未Play。

执行前Console[0 Error,5 Warning,27 Log]；正常编译后/Bake前后/最终均[0 Error,6 Warning,27 Log]。新增一条MCP WebSocket未初始化工具警告，原三条NetCode Tick Batching及PEListener UAC1001、DOTween CS0618警告保留，没有新增项目编译错误/警告；未清空Console，不从既有运行日志推导本阶段性能结论。

原515项人工内容/编号逐字保留，追加十六项后共531项，归[运行入口](Runtime.md)“收藏材料丢弃保护”章节。本阶段人工GamePlayer待验收；收藏计数v35/revision38与所有旧阶段用户通过仍限各自原版本/清单，不覆盖新保护。未实际触发的独立保护/取消/重置/请求消费时序、关闭/重绑、GUI命中/滚动/字形/裁切/分辨率、偏好真实读写/故障、多人/预测用例及性能/平台/线上为UNKNOWN。

AI未执行DropClient/Favorites/ListView/Panel/Preferences/GUI业务、实际偏好或游戏存档I/O、GamePlayer/PlayMode、逻辑单元测试、命令行构建/发布、性能采样或图片读取；未创建子Agent、暂存或提交Git。
