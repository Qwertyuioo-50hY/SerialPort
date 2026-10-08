import json


def run(request_json):
    request = json.loads(request_json)
    provider = request["provider"].lower()
    arguments = {key: request[key] for key in ("messages", "model", "api_key")}
    arguments["base_url"] = request.get("base_url")
    if provider == "openai":
        from .openai_api import chat
    elif provider == "anthropic":
        from .anthropic_api import chat
        arguments["max_tokens"] = request.get("max_tokens", 1024)
    else:
        raise ValueError("provider must be openai or anthropic; use base_url for a proxy")
    return json.dumps({"reply": chat(**arguments)}, ensure_ascii=False)
