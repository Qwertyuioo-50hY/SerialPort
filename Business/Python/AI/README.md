# AI 配置与本地项目

C# 设置页保存 `%LOCALAPPDATA%/Tomography/ai-settings.json`，Python 的 `config_api.run_from_file` 每次读取该文件，通过 OpenAI Python SDK 发起请求。配置示例见 `ai-settings.example.json`。在所选 Python 运行时环境中安装 `requirements.txt`。

API Key 由 Windows 凭据管理器保存并在调用时注入；也可使用 JSON 中 `ApiKeyEnvironmentVariable` 指定的环境变量。JSON 和项目聊天记录不保存密钥。

- `BaseUrl` 支持中转站，填写基础地址（通常包含 `/v1`），不要附加 `/responses` 或 `/chat/completions`。
- `ApiMode`：`responses` 或 `chat_completions`。GPT-6.1 Sol 的完整模式和速度配置使用 Responses。
- `Model` 为当前模型；`Models` 为设置页及聊天输入区可选的模型列表。
- `ReasoningMode`：`standard`、`pro`；`ReasoningEffort`：`default`、`low`、`medium`、`high`、`xhigh`、`max`。`default` 表示不发送强度参数。
- `ServiceTier`：`auto`、`default`、`priority`（Fast）、`ultrafast`。实际可用档位由模型及中转服务支持情况决定。
- `Verbosity`：`default`、`low`、`medium`、`high`；`MaxOutputTokens=0` 表示不指定输出上限（上限包含推理 Token）。
- `OutputFormat`：`text`、`markdown`、`json_object`。Markdown、`Tone`、`Language`、`Instructions`、`OutputInstructions` 合并为模型指令；语气和语言不是独立 SDK 参数。
- `TimeoutSeconds`：请求超时秒数。界面配置的参数会真实传入 Python；服务端不支持时返回错误，不静默降级。请求使用 `store=false`，本地保存全部文本历史。

全局配置统一保存在 `%LOCALAPPDATA%/Tomography/setting.json`，其中 `AI` 为 AI 配置、`Theme` 为现有主题选择。C# 负责读写，Python 读取同一文件中的 `AI`（兼容原平铺 JSON）。手动配置的 API Key 继续由 Windows 凭据管理器保存。首次读取自动迁移旧 `ai-settings.json`，原文件保留。

CC Switch：AI 设置默认启用“由 CC Switch 管理模型配置”，默认读取 `%USERPROFILE%/.codex/config.toml`，可在设置中更改路径并保存。每次发送重新读取文件，识别 `model_provider`、`model`、`model_reasoning_effort`、`disable_response_storage` 和对应服务商的 `base_url`、`wire_api`、`experimental_bearer_token` 或 `env_key`。TOML 中的密钥只在请求期间使用，不复制进 setting.json；`mcp_servers` 等其他内容不会执行。`requires_openai_auth = false` 不会阻止使用服务商提供的 Bearer Token。无密钥示例见 `config.example.toml`。

新建项目：输入项目名并选择父目录，生成 `project.json`、`data/` 和 `chats/`。设备及实验输入配置保存至项目根目录的 `project.json`；聊天以 `chats/<对话ID>/conversation.json` 保存。旧 AppData 项目配置和聊天、项目内旧中文聊天目录均兼容迁移，原文件保留。项目导航仅显示新建项目、打开文件夹和新建对话入口。设置分类顺序为通用、外观、AI、快捷键、语言和地区；新增分类仅提供页面占位。

官方依据（2026-10-14 核对）：
- [GPT-6.1 Sol](https://developers.openai.com/api/docs/models/gpt-6.1-sol)
- [推理模式与强度](https://developers.openai.com/api/docs/guides/reasoning)
- [Responses Python SDK 参数](https://developers.openai.com/api/reference/python/resources/responses/methods/create)

聊天默认使用紧凑单行输入区，Enter 发送、Shift+Enter 换行；中文输入法选词期间不触发发送。强度面板采用小圆点刻度的五档滑块，并保留恢复默认入口；模型在设置页选择。

界面请求流式返回，Python 将服务实际返回的 `response.reasoning_summary_text.delta` 和 `response.output_text.delta` 分别传给 C#。等待时显示闪烁圆点；没有摘要时不显示摘要区域。摘要随对话存入项目文件，但不作为后续用户消息重新发送。解析兼容 SDK 对象、JSON、字符串和 SSE；未收到流完成标记时报错，不将截断内容当成完整答案。

流式事件依据：[OpenAI 官方流式文档](https://developers.openai.com/api/docs/guides/streaming-responses)。本地端到端测试：`dotnet run --project Tests/DeviceTests/DeviceTests.csproj -- --python-chat`（使用本机 CPython 与回环模拟接口）。


Anthropic 中转配置：Base URL 填 `https://api.mxwell.ai`（也兼容末尾 `/v1`），接口协议选 `Anthropic Messages（Claude）`，模型填 `claude-sonnet-5` 或 `claude-opus-5`，密钥在设置页填写。请求固定使用所选模型，不进行自动路由。协议使用 `/v1/messages`，不附加 OpenAI 专有参数；输出上限为 0 时使用 4096。该模式目前使用模型默认思考设置，不应用 OpenAI 思考强度、速度或详细程度参数。
