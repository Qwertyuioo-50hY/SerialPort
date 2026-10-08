# Python 模块

| 分类 | 文件 | 功能 |
| --- | --- | --- |
| 算法 | 算法/lbp.py | 线性反投影重建 |
| 算法 | 算法/tikhonov.py | Tikhonov 正则化重建 |
| 算法 | 算法/landweber.py | Landweber 迭代重建 |
| 算法 | 算法/tv.py | TV 全变分重建 |
| 算法辅助 | 算法/custom_algorithm.py | 加载自定义 reconstruct 函数 |
| 算法辅助 | 算法/measurement.py | 64 通道测量值校验、空场参考和绝对/相对差分 |
| 算法辅助 | 算法/sensitivity_io.py | 加载 MAT/CSV 灵敏度矩阵及坐标 |
| 算法辅助 | 算法/image_mapping.py | 节点值归一化、二维图像插值 |
| 算法入口 | 算法/emt_bridge.py | JSON 重建请求、算法选择和图像结果校验 |
| 其余功能 | emt_bridge.py | 保留现有 C# Python.NET 调用入口，仅转发到算法包 |
| 其余功能 | acquisition_export.py | 将空场/物场采集帧和空场均值导出为 MAT |
| AI | AI/openai_api.py | OpenAI Chat Completions 和兼容中转站 |
| AI | AI/anthropic_api.py | Anthropic Messages 和兼容中转站 |
| AI 入口 | AI/ai_bridge.py、AI/__main__.py | JSON 调用接口及独立 Python 进程入口 |

本次仅整理当前 SerialPort 项目，不移动工作区其他旧版 Python 项目。
算法依赖现有运行环境中的 numpy、scipy。AI SDK 安装：

```powershell
python -m pip install -r Business/Python/AI/requirements.txt
```

从 `Business/Python` 目录调用（model 替换为服务实际提供的模型）：

```python
import json
from AI import run

reply = run(json.dumps({
    "provider": "openai",
    "api_key": "你的密钥",
    "model": "你的模型名称",
    "base_url": "https://api.openai.com/v1",
    "messages": [{"role": "user", "content": "你好"}]
}))
print(json.loads(reply)["reply"])
```

Anthropic 将 provider 改为 `anthropic`，base_url 使用 `https://api.anthropic.com`。
省略 base_url 使用官方默认地址；中转站填写其地址，并按它支持的协议选择 provider。
OpenAI 兼容站通常需要在 base_url 中保留 `/v1`，以服务商说明为准。
messages 保存文本对话历史；Anthropic 的 system 消息会转换为顶层 system 参数。
可选 max_tokens 默认 1024，仅用于 Anthropic。

独立进程调用：`python -m AI`，标准输入传入一个 UTF-8 JSON 请求，输入结束后标准输出返回 `{"reply":"..."}`。
当前 C# 重建仍沿用现有进程内 Python.NET；底部聊天 UI 不调用 API。

实现依据：[OpenAI 官方文档](https://developers.openai.com/api/reference/python/resources/chat/subresources/completions/methods/create)、[Anthropic 官方文档](https://platform.claude.com/docs/en/cli-sdks-libraries/sdks/python)。
