from openai import OpenAI


def chat(messages, model, api_key, base_url=None):
    with OpenAI(api_key=api_key, base_url=base_url) as client:
        result = client.chat.completions.create(model=model, messages=messages)
        return result.choices[0].message.content or ""
