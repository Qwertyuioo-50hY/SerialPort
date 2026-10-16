"""Normalize SDK / relay JSON and SSE without assuming output_text exists."""
import json


def snapshot(value):
    if hasattr(value, "model_dump"):
        value = value.model_dump()
    for _ in range(3):
        if not isinstance(value, str):
            break
        text = value.strip().lstrip("\ufeff")
        if text.startswith(("data:", "event:")):
            return collect_sse(text)
        if text.startswith("<"):
            raise ValueError("中转站返回了网页，请检查 Base URL 和接口协议。")
        try:
            decoded = json.loads(text)
        except json.JSONDecodeError:
            return {"reply": value, "reasoning_summary": ""}
        if decoded == value:
            break
        value = decoded
    if isinstance(value, str):
        return {"reply": value, "reasoning_summary": ""}
    if not isinstance(value, dict) or value.get("error"):
        raise ValueError("中转站返回了无效响应或 API 错误。")
    if value.get("status") in ("incomplete", "failed", "cancelled"):
        raise ValueError("模型响应未完成，请检查输出上限或重试。")
    if value.get("stop_reason") in ("max_tokens", "pause_turn"):
        raise ValueError("模型响应未完成，请调整输出上限后重试。")
    if value.get("type") == "message" and isinstance(value.get("content"), list):
        return {"reply": "".join(part.get("text", "") for part in value["content"] if part.get("type") == "text"), "reasoning_summary": ""}
    reply = value.get("output_text")
    reply = reply if isinstance(reply, str) else ""
    summaries = []
    texts = []
    for item in value.get("output") or []:
        if not isinstance(item, dict):
            continue
        if item.get("type") == "reasoning":
            summaries.extend(part["text"] for part in (item.get("summary") or []) if isinstance(part, dict) and isinstance(part.get("text"), str))
        if item.get("type") == "message":
            for part in (item.get("content") or []):
                if isinstance(part, dict):
                    text = part.get("text") or part.get("refusal")
                    if isinstance(text, str):
                        texts.append(text)
    if not reply:
        reply = "".join(texts)
    if not reply and value.get("choices"):
        message = value["choices"][0].get("message", {})
        content = message.get("content")
        if isinstance(content, str):
            reply = content
        summary = message.get("reasoning_summary")
        if isinstance(summary, str):
            summaries.append(summary)
    return {"reply": reply, "reasoning_summary": "\n".join(summaries)}


def collect_events(events, on_event=None):
    result = {"reply": "", "reasoning_summary": ""}
    complete = False
    def append(key, text):
        if isinstance(text, str) and text:
            result[key] += text
            if on_event:
                on_event(key, text)
    for event in events:
        if hasattr(event, "model_dump"):
            event = event.model_dump()
        if event == "[DONE]":
            complete = True
            continue
        if not isinstance(event, dict):
            continue
        kind = event.get("type")
        if kind in ("error", "response.failed", "response.incomplete"):
            raise ValueError("模型响应未完成，请检查接口配置、输出上限后重试。")
        if kind == "content_block_delta":
            delta = event.get("delta") or {}
            if delta.get("type") == "text_delta":
                append("reply", delta.get("text"))
        elif kind == "content_block_start":
            block = event.get("content_block") or {}
            if block.get("type") == "text":
                append("reply", block.get("text"))
        elif kind == "message_delta":
            if (event.get("delta") or {}).get("stop_reason") in ("max_tokens", "pause_turn"):
                raise ValueError("模型响应未完成，请调整输出上限后重试。")
        elif kind == "message_stop":
            complete = True
        elif kind == "response.output_text.delta":
            append("reply", event.get("delta"))
        elif kind == "response.reasoning_summary_text.delta":
            append("reasoning_summary", event.get("delta"))
        elif kind == "response.completed":
            complete = True
            final = snapshot(event.get("response", {}))
            for key in result:
                if final[key]:
                    if final[key].startswith(result[key]):
                        append(key, final[key][len(result[key]):])
                    result[key] = final[key]
        elif event.get("choices"):
            choice = event["choices"][0]
            delta = choice.get("delta") or {}
            append("reply", delta.get("content"))
            append("reasoning_summary", delta.get("reasoning_summary"))
            if choice.get("finish_reason") == "length":
                raise ValueError("输出达到 Token 上限，请调整后重试。")
            complete = complete or choice.get("finish_reason") is not None
    if not complete:
        raise ValueError("响应流提前结束，未收到完成标记，请重试。")
    return result


def collect_sse(text, on_event=None):
    events = []
    data = []
    for line in text.splitlines() + [""]:
        if line.startswith("data:"):
            data.append(line[5:].lstrip())
        elif not line and data:
            raw = "\n".join(data); data = []
            events.append(raw if raw == "[DONE]" else json.loads(raw))
    return collect_events(events, on_event)


def read_response(response, on_event=None):
    # Some relays ignore stream=true or send JSON with text/plain content type.
    raw = getattr(response, "response", None)
    if raw is not None and hasattr(response, "close"):
        try:
            if "text/event-stream" in raw.headers.get("content-type", ""):
                return collect_events(response, on_event)
            value = raw.read().decode("utf-8-sig")
            result = snapshot(value)
        finally:
            response.close()
    else:
        result = snapshot(response)
    if on_event:
        for key in ("reasoning_summary", "reply"):
            if result[key]:
                on_event(key, result[key])
    return result
