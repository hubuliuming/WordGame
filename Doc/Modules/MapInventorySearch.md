# 背包材料搜索

返回[地图](Map.md)、[背包](Inventory.md)、[B面板](MapInventoryPanel.md)、[排序筛选](MapInventoryListView.md)、[容量](MapInventoryCapacity.md)、[丢弃](MapInventoryDrop.md)、[配置](DataResources.md)、[玩家输入](Player.md)与[运行入口](Runtime.md)。入口CombatPrototypeNetCode；Forest/Grassland Json/BuiltIn当前schemaVersion=37/configRevision=40；[本机偏好](MapInventoryPreferences.md)静态核对通过、人工已获用户通过反馈，限v30/revision33及运行入口偏好阶段十六项。主线程实现、正常编译、严格配置读取和隔离Editor Bake静态核对通过；用户已确认本阶段人工GamePlayer验收通过，主线程结合既有静态核对与用户反馈判定通过，限CombatPrototypeNetCode、v29/revision32及运行入口十六项清单；人工结论来自用户反馈。旧排序筛选用户通过限v28/revision31十六项，其他旧通过保持原版本/清单。

## 【FACT】文件与配置

| 职责 | 真实入口 |
|---|---|
| 必填字段与原显示Settings | [Config](../../Assets/Scripts/CombatPrototype/Map/MapInventoryPanelConfig.cs)、[Data](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelData.cs) |
| 版本、语义、默认值与烘焙 | [Validator](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapConfigValidator.cs)、[BuiltIn](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeDefaultMapConfigSource.cs)、[Authoring](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapAuthoring.cs) |
| 完整库存、可见行与GUI | [Snapshot](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSnapshot.cs)、[ListView](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelListView.cs)、[Panel](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanel.cs) |
| 文本编辑、匹配与焦点 | [Search](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInventoryPanelSearch.cs) |
| 客户端采样调用链 | [Binding](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHudBindingSystem.cs)→[HUD](../../Assets/Scripts/CombatPrototype/Map/CombatPrototypeMapInteractionHud.cs)→Panel；[PlayerInput](../../Assets/Scripts/CombatPrototype/Networking/CombatPrototypePlayerInput.cs)消费blocksKeyboard |
| 显式配置 | [Forest](../../Assets/Config/CombatPrototype/Map/battle_forest_01.json)、[Grassland](../../Assets/Config/CombatPrototype/Map/battle_grassland_01.json) |

v29搜索阶段十一现有脚本、一新普通Search类及Unity正常生成meta、两JSON接入；Search非MonoBehaviour，没有新组件类型/挂载或Scene/SubScene/Prefab/Animator/旧meta/资源/字体/包/构建结构变更。Input十九字段、Ghost及存档布局保持。

inventoryPanel原44字段保留，新增七项均必填：

| 字段 | 当前Json/BuiltIn值 | 校验与用途 |
|---|---|---|
| searchEnabled | true | 严格bool；关闭隐藏两搜索行并不施加搜索/键盘编辑隔离 |
| searchIgnoreCase | true | 严格bool；true为OrdinalIgnoreCase，false为Ordinal |
| searchMaxLength | 32 | 严格int，1～64，按C# UTF-16代码单元限制编辑文本 |
| searchLabel | Search | 标题文案 |
| searchPlaceholderLabel | Name keyword | 空文本、未编辑且无IME合成时的占位文案 |
| clearSearchLabel | Clear | 清空按钮文案 |
| noSearchResultsLabel | No search results | 全部材料模式下完整库存非空且有效搜索无可见行时显示 |

当前DTO共89字段：十六bool、六float、三int、两模式string、61文案string及一偏好文件ID；原Settings对应十六byte、六float、三int、两byte枚举、62 FixedString64Bytes，0 GhostField且无GhostComponent。四新文案沿非空白、无控制字符与最多61 UTF-8字节规则；即使关闭搜索、面板或全部显示仍校验所有字段。原Reader保留UTF-8/形状/缺失/null/错类型/未知或重复键检查，当前只接受schema37和正revision/seed，旧v1～v36拒绝，无补默认、来源回退或热重载。正常导入/烘焙后生效，各端同版代码/配置并重新烘焙。

## 【CURRENT STRATEGY】本地文本与列表投影

Snapshot仍先按原顺序校验、缓存全部合法正数量库存；Row增加DisplayName，原Name/OriginalName/Quantity/Text保持。搜索在原名或配置显示名上作子串匹配，不搜索行数量/容量字符串；未知合法物品显示名仍原名。仅匹配时Trim两端空白，文本框保留输入空白；空串/纯空白相当于无搜索。粘贴控制字符被去掉，超长截断不保留孤立代理项，合法代理对按两个UTF-16单元计。

原类别条件与搜索取交集，之后按当前original/type/quantity排序，分类/排序规则不变。完整库存/全局合法性、容量及配方/缺口、工具制作/修理/升级和服务器SavePrepared事务保持，隐藏材料继续参加原统计与资格。没有第二份可变游戏库存，搜索不修改原库存数量/顺序或世界/玩家存档。

GUI.TextField仅写draft并记录pending；下一Panel.Show在完整Snapshot之后，由ListView.Capture先ApplyPending更新applied/query与本地uint Revision，滚动归零，再按Snapshot/搜索Revision或模式变化重建可见行。除显式显示重置外，未变化不重复匹配/排序，GUI不进行业务库存或可见集合变更。同次绘制使用稳定列表。Clear清draft并排队空查询、释放焦点，下次Show应用；已有排序/分类保持。

搜索开启在材料标题/完整容量行、启用的收藏计数行及模式控制之后追加标题、文本框+Clear两行，沿原380×640滚动区、字号18/行高32和缩放，不改变面板矩形或固定页脚。文本框约占内容宽75%减4间距，Clear占25%；未配置新字体。空态优先级归[B面板](MapInventoryPanel.md)与[收藏筛选](MapInventoryFavoritesFilter.md)，收藏计数不参与空态判断。容量/工具/业务区一直可见。

Drop/All仍按真实Row.Name解析稳定Kind/Mode；搜索重建造成行身份/顺序/数目改变时只撤销尚未完成的行点击许可。搜索不清已排队请求；未消费请求另按[收藏保护](MapInventoryFavoritesDropProtection.md)复核，已写入PlayerInput的请求沿原服务端链，搜索不撤回服务器动作；同身份纯数量/文字刷新沿原资格。

## 【CURRENT STRATEGY】焦点、键盘与生命周期

面板不自动聚焦。每个Search实例有独立GUI控制名，点击搜索框后ReadInput先按与绘制共享的缩放/滚动几何记录焦点，并屏蔽获得焦点同帧键盘；GUI.SetNextControlName/TextField/FocusControl/GetNameOfFocusedControl沿原OnGUI执行。文本由原IMGUI处理，不手工把键码转字符。当前activeInputHandler为Both，满足旧IMGUI与Input System并用；没有修改项目输入设置。

编辑及获得/释放焦点同帧，Panel消费B为文本并经原Binding/HUD返回blocksKeyboard；PlayerInput的gameplayKeyboard为null，Move归零，空格、E/R/F/G/F5、1～7不写新键盘事件，Camera.ReadMove也不接收键盘Z/X。原十九字段/网络协议保持，已提交动作、服务器计时及镜头已有平滑继续，Time.timeScale不变。

Enter/小键盘Enter/Esc释放编辑焦点，活跃IME compositionString非空时保留候选处理键；之后B关闭。点击其他面板区域释放焦点，沿原面板内鼠标隔离；点击面板外释放焦点并消费该次左键，避免同时Attack，释放帧键盘仍屏蔽。其余原鼠标攻击/镜头滚轮及面板有效按钮请求保持。原键盘指针可空，不查找组件兜底。

同一有效绑定Close保留已应用查询，draft回退到applied并清pending、释放焦点和滚动，重开继续查询且不自动编辑。逐帧HUD.Clear/Panel.Clear只隐藏当次显示，避免清编辑；死亡/断线、无所属Ghost/有效连接、源或玩家变化、World/Scene停止释放沿Reset先提交待保存的已应用偏好，再清全部搜索文本/Revision/焦点/按下帧和GUI样式。GUI焦点只在Draw释放，隐藏也可清自己的控制；新绑定先按原initiallyOpen/默认模式与空查询初始化；[偏好](MapInventoryPreferences.md)启用、允许保存搜索且搜索能力开启时，再读取合法正式文件的文本，经当前CleanInput长度限制恢复且不聚焦。载入截断不触发写回；焦点/滚动/面板开关不保存，查询不入网络或玩家/世界档案。

## 【KNOWN ISSUES】静态证据与人工边界

以下静态证据限v29/revision32搜索阶段，当前偏好静态及人工边界归[偏好](MapInventoryPreferences.md)。该阶段正常Unity刷新/编译完成，C# Error为0；Search普通类、51配置/Settings、0 GhostField与两byte模式已在当前程序集核对。编译后Console保留原警告及PEListener、DOTween、MCP连接警告；隔离Bake前后均[0 Error,9 Warning,47 Log]，没有新增Bake警告，未清空Console。

1410份非法配置全部拒绝（每地图705），76组合法读取通过（每地图38）：完整51字段形状/类型/重复键，二开关错误标量，长度0/负数/65/int极值/浮点字面量，四文案空白/控制字符/62字节与中文超限，关闭仍必填/验证，旧v1～v28、未来版本以及全部原模式/部分拾取/合并/几何约束。合法含Json/BuiltIn等价、四开关组合、1/64长度边界、61字节/中文文案、原三排序×四分类、模式/面板/全部显示或其他功能关闭及定义/等级换序。仅执行配置Reader/Validator，没有调用搜索/排序/库存/焦点/GUI业务作为测试。

两地图各41次，共82次隔离Editor Bake通过：保留原32类，增加搜索关闭、大小写敏感、最大长度1/64、中文/61字节文案、数量排序+资源分类+初始打开、面板/搜索及三展示能力同时关闭。全部51Settings和原所有Settings/零反馈/Prefab引用、布置与兼容签名匹配。Forest仍89树/36采集/20矿/109阻挡，Grassland53/38/18/71；只读源SubScene，临时克隆/TextAsset/Scene/World/BlobAssetStore释放，原主场景干净、未Play。

Input19、DropGhost4、Tools3、F4/G7/资源状态4/世界保存3与原结果组件、玩家v4根7/工具项3、世界v2根9/掉落项8保持。服务器采集/拾取/丢弃/制作/修理/升级/保存、Ghost Serializer和Camera脚本保持，只有原客户端键盘采样链按本地编辑状态隔离。

原403项人工内容/编号保留，追加本阶段十六项，共419项，归[运行入口](Runtime.md)，用户已确认本阶段人工GamePlayer通过，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v29/revision32及十六项清单，结论来自用户反馈；未实际触发的独立IME输入/候选键、粘贴/代理边界、焦点切换帧、GUI事件/排版/字形/缩放/点击/滚动、多人/联网/预测/重绑及性能/平台/线上仍UNKNOWN。AI未调用Search匹配/清理/ApplyPending/焦点输入、ListView/库存Capture、面板/HUD/GUI、GamePlayer/PlayMode、逻辑单元测试、真实存档业务I/O、命令行构建/发布、性能采样或图片；未创建子Agent/提交Git。

v31/revision34重置阶段显示重置由Panel在Snapshot之后调用Search.ResetDisplay：已应用词非空才增加Revision，清草稿/查询/未应用编辑并释放焦点；原GUI控制名、文本框几何及键盘隔离保持。preferencesSaveSearch=false或搜索能力关闭时，仅清临时词、不覆盖文件原值；保存规则及v31/revision34十六项用户人工通过边界归[偏好](MapInventoryPreferences.md)。

## 【FACT】材料详情与搜索边界

v32/revision35详情阶段的[详情](MapInventoryDetails.md)位于搜索控件之后，原SearchField几何、焦点释放与键盘/面板指针隔离代码保持。查询隐藏选中Name时关闭详情，清查询不自动恢复详情；详情选择不作为搜索词或偏好保存，显示重置清两者。详情人工十六项已获用户通过反馈，限v32/revision35及运行入口清单，未触发独立用例仍UNKNOWN，原搜索用户通过仍限v29/revision32清单。

## 【FACT】收藏显示边界

v33/revision36收藏阶段的[收藏](MapInventoryFavorites.md)：分类/查询先产生可见行，再稳定分组置顶；隐藏收藏不强制出现，清词不清收藏，原SearchField位置/焦点及键盘隔离保持。人工收藏十六项已获用户通过反馈，限CombatPrototypeNetCode、v33/revision36及运行入口十六项；未触发独立用例仍UNKNOWN，旧通过仍限各自版本/清单。

## 【FACT】仅看收藏边界

v34/revision37筛选阶段的[收藏筛选](MapInventoryFavoritesFilter.md)：新增全宽收藏筛选位于分类之后、搜索之前，ControlRowCount同时用于绘制高度和ContainsSearchField，开启时文本框下移一行；分类/查询/收藏取交集，原焦点/IME/键盘及指针隔离保持。本阶段十六项人工GamePlayer已获用户通过反馈，限上述版本及运行入口清单；未触发独立用例仍UNKNOWN，旧通过范围保持。

## 【FACT】收藏计数与搜索命中

v35/revision38计数阶段的[收藏计数](MapInventoryFavoritesCount.md)位于模式控件之前，CountRowCount0～1与原ControlRowCount共用于DrawBody、内容高度和ContainsSearchField，启用时搜索框另下移一行；计数行不接受输入。搜索/IME/键盘/指针与Search代码保持，交集不改变收藏总数。本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v35/revision38及运行入口清单，人工结论来自用户反馈；未实际触发的独立用例仍UNKNOWN；旧搜索和筛选通过范围保持。

## 【FACT】收藏保护与搜索

v36/revision39保护阶段的[收藏保护](MapInventoryFavoritesDropProtection.md)：Search代码、焦点/IME/键盘/指针隔离及ContainsSearchField几何保持；保护文案占原操作行，不增加搜索行偏移。搜索和分类不删除已应用收藏，DropClient按真实Name保护当前可见材料，并在消费前另按Kind映射复核。 本阶段十六项人工GamePlayer已获用户通过反馈，主线程结合既有静态核对判定通过，限CombatPrototypeNetCode、v36/revision39及运行入口清单，人工结论来自用户反馈；完整静态证据与边界归保护专题及[运行入口](Runtime.md)；既有用户通过保持各自原版本/清单，未实际触发的独立用例仍UNKNOWN。

## 【CURRENT STRATEGY】收藏材料消耗提示

当前v37/revision40的[收藏材料消耗提示](MapInventoryFavoritesConsumptionHint.md)复用原B面板七份木石配方和已应用Favorites.IsFavorite真实Name；只在对应操作有有效配方且正成本材料已收藏时，于配方下加一行缓存只读文字，材料不足仍提示。隐藏/数量归零/仅看收藏/搜索不改变配方提示，取消或Reset实际应用后下一有效Show刷新。共0～7行计入原滚动高度，提示总高度变化清旧鼠标许可，不清已排队业务请求；原按钮资格、1～7/E/F/G、服务端扣料/保存及全部反馈保持。本阶段人工GamePlayer待验收，旧保护通过限v36/revision39十六项，其他旧阶段保持原版本/清单。
