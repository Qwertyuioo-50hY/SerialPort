"""OpenAI-compatible Chat Completions transport; no third-party dependencies."""
import json
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import Request, build_opener, HTTPRedirectHandler


class _NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def chat(messages, model, api_key, base_url=None):
    base = (base_url or "").strip().rstrip("/")
    parsed = urlsplit(base)
    if (parsed.scheme not in ("https", "http") or not parsed.hostname
            or parsed.username or parsed.password or parsed.query or parsed.fragment):
        raise ValueError("API 地址必须是有效的 HTTP/HTTPS Base URL。")
    if not model.strip() or not api_key.strip():
        raise ValueError("请填写模型名称和 API 密钥。")
    endpoint = base if base.endswith("/chat/completions") else base + "/chat/completions"
    payload = json.dumps({"model": model, "messages": messages, "stream": False}, ensure_ascii=False).encode("utf-8")
    request = Request(endpoint, data=payload, headers={
        "Content-Type": "application/json", "Authorization": "Bearer " + api_key,
    }, method="POST")
    try:
        with build_opener(_NoRedirect()).open(request, timeout=60) as response:
            data = response.read(8 * 1024 * 1024 + 1)
            if len(data) > 8 * 1024 * 1024:
                raise ValueError("API 返回内容过大。")
            result = json.loads(data)
    except HTTPError as exc:
        code = exc.code
        exc.close()
        raise RuntimeError("API 请求失败（HTTP %s）。" % code) from None
    except (URLError, TimeoutError):
        raise RuntimeError("API 连接失败或请求超时，请检查地址与网络。") from None
    try:
        reply = result["choices"][0]["message"]["content"]
        if not isinstance(reply, str):
            raise ValueError()
        return reply
    except (KeyError, IndexError, TypeError, ValueError):
        raise ValueError("API 返回格式不符合 Chat Completions 协议。") from None
