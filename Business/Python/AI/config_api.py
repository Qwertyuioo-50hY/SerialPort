"""JSON configuration -> OpenAI Python SDK (Responses / Chat Completions)."""
import json
import os
from pathlib import Path
from urllib.parse import urlsplit


def read_config(path):
    config = json.loads(Path(path).read_text(encoding="utf-8-sig"))
    if isinstance(config, dict) and "AI" in config:
        config = config["AI"]
        if isinstance(config, dict):
            config.setdefault("UseCcSwitchConfig", True)
    if not isinstance(config, dict):
        raise ValueError("AI 配置必须是 JSON 对象。")
    if config.get("UseCcSwitchConfig"):
        from .ccswitch_config import apply_provider
        config = apply_provider(config)
    base = config.get("BaseUrl", "").strip().rstrip("/")
    url = urlsplit(base)
    if url.scheme not in ("http", "https") or not url.hostname or url.username or url.password or url.query or url.fragment:
        raise ValueError("API Base URL 无效。")
    if base.endswith(("/responses", "/chat/completions", "/messages")):
        raise ValueError("请填写 Base URL（例如以 /v1 结尾），不要包含接口路径。")
    config["BaseUrl"] = base
    if not isinstance(config.get("Model"), str) or not config["Model"].strip():
        raise ValueError("请配置模型名称。")
    for key, choices, default in (
        ("ApiMode", ("responses", "chat_completions", "anthropic_messages"), "responses"),
        ("ReasoningMode", ("standard", "pro"), "standard"),
        ("ReasoningEffort", ("default", "low", "medium", "high", "xhigh", "max"), "medium"),
        ("ServiceTier", ("auto", "default", "priority", "ultrafast"), "default"),
        ("Verbosity", ("default", "low", "medium", "high"), "default"),
        ("OutputFormat", ("text", "markdown", "json_object"), "text"),
    ):
        config.setdefault(key, default)
        if config[key] not in choices:
            raise ValueError("无效的 AI 配置项：" + key)
    for key, minimum, maximum, default in (("MaxOutputTokens", 0, 2147483647, 0), ("TimeoutSeconds", 1, 3600, 120)):
        value = config.setdefault(key, default)
        if type(value) is not int or not minimum <= value <= maximum:
            raise ValueError("无效的 AI 数值配置：" + key)
    if config["ApiMode"] == "chat_completions" and (config["ReasoningMode"] == "pro" or config["ServiceTier"] == "ultrafast"):
        raise ValueError("Pro 模式和 Ultrafast 速度需要 Responses 接口。")
    return config


def build_request(config, messages):
    instructions = []
    for key in ("Instructions", "OutputInstructions"):
        value = config.get(key, "").strip()
        if value:
            instructions.append(value)
    tone = config.get("Tone", "默认").strip()
    language = config.get("Language", "跟随用户").strip()
    if tone and tone != "默认":
        instructions.append("回复语气：" + tone)
    if language and language != "跟随用户":
        instructions.append("回复语言：" + language)
    if config["OutputFormat"] == "markdown":
        instructions.append("使用 Markdown 格式输出。")
    elif config["OutputFormat"] == "json_object":
        instructions.append("输出有效的 JSON 对象（JSON），不要添加格式之外的文字。")
    if config["ApiMode"] == "anthropic_messages":
        options = {"model": config["Model"], "messages": [dict(item) for item in messages],
                   "max_tokens": config["MaxOutputTokens"] or 4096}
        if instructions:
            options["system"] = "\n".join(instructions)
        return options
    options = {"model": config["Model"], "store": config.get("StoreResponses", False)}
    if not config.get("UseCcSwitchConfig"):
        options["service_tier"] = config["ServiceTier"]
    effort = config["ReasoningEffort"]
    if config["ApiMode"] == "responses":
        options["input"] = [dict(item) for item in messages]
        if instructions:
            options["instructions"] = "\n".join(instructions)
        options["reasoning"] = {} if config.get("UseCcSwitchConfig") else {"mode": config["ReasoningMode"]}
        if effort != "default":
            options["reasoning"]["effort"] = effort
        text = {}
        if config["Verbosity"] != "default":
            text["verbosity"] = config["Verbosity"]
        if config["OutputFormat"] == "json_object":
            text["format"] = {"type": "json_object"}
        if text:
            options["text"] = text
        if config["MaxOutputTokens"]:
            options["max_output_tokens"] = config["MaxOutputTokens"]
    else:
        options["messages"] = ([{"role": "system", "content": "\n".join(instructions)}] if instructions else []) + messages
        if effort != "default":
            options["reasoning_effort"] = effort
        if config["Verbosity"] != "default":
            options["verbosity"] = config["Verbosity"]
        if config["OutputFormat"] == "json_object":
            options["response_format"] = {"type": "json_object"}
        if config["MaxOutputTokens"]:
            options["max_completion_tokens"] = config["MaxOutputTokens"]
    return options


def run_from_file(config_path, request_json, api_key="", on_event=None):
    from openai import OpenAI, APIError
    from .response_reader import read_response
    config = read_config(config_path)
    key = (config.get("_ProviderApiKey", "") if config.get("UseCcSwitchConfig") else api_key) or os.environ.get(config.get("ApiKeyEnvironmentVariable", "OPENAI_API_KEY"), "")
    if not key or not key.isascii() or any(character.isspace() for character in key):
        raise ValueError("API Key 为空或格式无效。")
    request = json.loads(request_json)
    messages = request["messages"]
    if not isinstance(messages, list) or any(not isinstance(item, dict) or item.get("role") not in ("user", "assistant") or not isinstance(item.get("content"), str) for item in messages):
        raise ValueError("聊天记录格式无效。")
    options = build_request(config, messages)
    if config["ApiMode"] == "anthropic_messages":
        from .anthropic_api import send_request
        return json.dumps(send_request(config, key, options, on_event), ensure_ascii=False)
    try:
        with OpenAI(api_key=key, base_url=config["BaseUrl"], timeout=config["TimeoutSeconds"], max_retries=0) as client:
            if config["ApiMode"] == "responses":
                if on_event:
                    options["reasoning"]["summary"] = "auto"
                response = client.responses.create(**options, stream=on_event is not None)
            else:
                response = client.chat.completions.create(**options, stream=on_event is not None)
            result = read_response(response, on_event)
    except APIError as error:
        status = getattr(error, "status_code", None)
        raise RuntimeError("API 请求失败" + ("（HTTP %s）" % status if status else "，请检查网络与接口配置") + "。") from None
    if not result["reply"]:
        raise ValueError("API 未返回文本，请检查模型、输出上限或接口协议。")
    return json.dumps(result, ensure_ascii=False)
