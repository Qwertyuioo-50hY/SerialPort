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

新建项目：输入项目名并选择父目录。聊天以 `项目目录/聊天/<对话ID>/conversation.json` 保存，导航顶部“聊天”文件夹可展开并恢复对话。旧 AppData 项目聊天在首次读取时复制到项目目录；原文件保留。

官方依据（2026-10-14 核对）：
- [GPT-6.1 Sol](https://developers.openai.com/api/docs/models/gpt-6.1-sol)
- [推理模式与强度](https://developers.openai.com/api/docs/guides/reasoning)
- [Responses Python SDK 参数](https://developers.openai.com/api/reference/python/resources/responses/methods/create)
