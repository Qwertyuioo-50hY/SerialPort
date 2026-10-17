"""Read CC Switch-managed Codex model settings without executing MCP entries."""
import os
from pathlib import Path
import tomllib


def apply_provider(config):
    path = Path(os.path.expandvars(config.get("CcSwitchConfigPath", ""))).expanduser()
    try:
        document = tomllib.loads(path.read_text(encoding="utf-8-sig"))
    except OSError:
        raise ValueError("无法读取 CC Switch config.toml，请检查文件路径和权限。") from None
    except (tomllib.TOMLDecodeError, UnicodeError):
        raise ValueError("CC Switch config.toml 格式无效，请保存为标准 TOML 文件。") from None
    provider_name = document.get("model_provider")
    providers = document.get("model_providers", {})
    provider = providers.get(provider_name) if isinstance(providers, dict) and isinstance(provider_name, str) else None
    if not isinstance(provider, dict):
        raise ValueError("config.toml 缺少 model_provider 对应的 model_providers 配置。")
    mode = {"responses": "responses", "chat": "chat_completions", "chat_completions": "chat_completions",
            "anthropic_messages": "anthropic_messages"}.get(provider.get("wire_api", "responses"))
    if mode is None:
        raise ValueError("config.toml 的 wire_api 协议不受支持。")
    model, base = document.get("model"), provider.get("base_url")
    if not isinstance(model, str) or not model.strip() or not isinstance(base, str) or not base.strip():
        raise ValueError("config.toml 必须填写 model 和服务商 base_url。")
    effort = document.get("model_reasoning_effort", "default")
    if not isinstance(effort, str):
        raise ValueError("config.toml 的 model_reasoning_effort 必须为字符串。")
    token = provider.get("experimental_bearer_token", "")
    env_key = provider.get("env_key", "")
    if not isinstance(token, str) or not isinstance(env_key, str):
        raise ValueError("config.toml 的认证配置格式无效。")
    disable_storage = document.get("disable_response_storage", True)
    if type(disable_storage) is not bool:
        raise ValueError("config.toml 的 disable_response_storage 必须为布尔值。")
    result = dict(config)
    result.update(Model=model.strip(), BaseUrl=base.strip(), ApiMode=mode, ReasoningEffort=effort,
                  ReasoningMode="standard", ServiceTier="default", Verbosity="default",
                  StoreResponses=not disable_storage, _ProviderApiKey=token or os.environ.get(env_key, ""))
    # All other TOML sections, including mcp_servers and its commands, are inert.
    return result
