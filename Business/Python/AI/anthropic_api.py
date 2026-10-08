from anthropic import Anthropic


def chat(messages, model, api_key, base_url=None, max_tokens=1024):
    system = "\n".join(message["content"] for message in messages if message["role"] == "system")
    conversation = [message for message in messages if message["role"] != "system"]
    with Anthropic(api_key=api_key, base_url=base_url) as client:
        result = client.messages.create(
            model=model, messages=conversation, system=system, max_tokens=max_tokens)
        return "".join(block.text for block in result.content if block.type == "text")
