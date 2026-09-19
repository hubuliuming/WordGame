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
