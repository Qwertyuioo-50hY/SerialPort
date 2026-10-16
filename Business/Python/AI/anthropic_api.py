from anthropic import Anthropic


def chat(messages, model, api_key, base_url=None, max_tokens=1024):
    system = "\n".join(message["content"] for message in messages if message["role"] == "system")
    conversation = [message for message in messages if message["role"] != "system"]
    with Anthropic(api_key=api_key, base_url=base_url) as client:
        result = client.messages.create(
            model=model, messages=conversation, system=system, max_tokens=max_tokens)
        return "".join(block.text for block in result.content if block.type == "text")


def send_request(config, api_key, options, on_event=None):
    from anthropic import APIError
    from .response_reader import read_response
    # The SDK appends /v1/messages; accept both a host root and a /v1 base.
    base = config["BaseUrl"].rstrip("/")
    if base.endswith("/v1"):
        base = base[:-3]
    try:
        with Anthropic(api_key=api_key, base_url=base, timeout=config["TimeoutSeconds"], max_retries=0) as client:
            response = client.messages.create(**options, stream=on_event is not None)
            result = read_response(response, on_event)
    except APIError as error:
        status = getattr(error, "status_code", None)
        raise RuntimeError("API 请求失败" + ("（HTTP %s）" % status if status else "，请检查网络与接口配置") + "。") from None
    if not result["reply"]:
        raise ValueError("API 未返回文本，请检查模型、输出上限或接口协议。")
    return result
