import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import test_openai_api as fixtures
from AI.config_api import run_from_file, read_config, build_request


class ConfigApiTests(unittest.TestCase):
    def setUp(self):
        fixtures.ApiTests.setUp(self)
        root = Path(__file__).parent / "obj"
        root.mkdir(exist_ok=True)
        self.folder = tempfile.TemporaryDirectory(dir=root)
        self.path = Path(self.folder.name) / "ai-settings.json"
        self.config = {"BaseUrl": self.base, "Model": "gpt-6.1-sol", "ApiMode": "responses",
            "ReasoningMode": "pro", "ReasoningEffort": "xhigh", "ServiceTier": "ultrafast",
            "Verbosity": "high", "OutputFormat": "json_object", "Tone": "专业", "Language": "中文",
            "Instructions": "精确回答", "OutputInstructions": "包括结论", "MaxOutputTokens": 4096}
        self.body = {"id": "resp_test", "object": "response", "created_at": 0, "status": "completed",
            "model": "gpt-6.1-sol", "output": [{"type": "message", "id": "msg_test", "role": "assistant",
            "status": "completed", "content": [{"type": "output_text", "text": "完成", "annotations": []}]}]}

    def tearDown(self):
        fixtures.ApiTests.tearDown(self)
        self.folder.cleanup()

    def send_config(self, key="test-only-key", on_event=None):
        self.path.write_text(json.dumps(self.config, ensure_ascii=False), encoding="utf-8")
        return json.loads(run_from_file(str(self.path), json.dumps({"messages": [
            {"role": "user", "content": "你好"}, {"role": "assistant", "content": "你好！"},
            {"role": "user", "content": "继续"}]}), key, on_event))

    def test_sdk_responses_parameters_and_history(self):
        self.assertEqual(self.send_config()["reply"], "完成")
        path, auth, body = self.requests[0]
        self.assertEqual(path, "/proxy/v1/responses")
        self.assertEqual(auth, "Bearer test-only-key")
        self.assertEqual(body["reasoning"], {"mode": "pro", "effort": "xhigh"})
        self.assertEqual(body["service_tier"], "ultrafast")
        self.assertEqual(body["text"], {"verbosity": "high", "format": {"type": "json_object"}})
        self.assertEqual(body["max_output_tokens"], 4096)
        self.assertFalse(body["store"])
        self.assertEqual([item["role"] for item in body["input"]], ["user", "assistant", "user"])
        for instruction in ("专业", "中文", "JSON", "包括结论", "精确回答"):
            self.assertIn(instruction, body["instructions"])

    def test_json_is_reread(self):
        self.send_config()
        self.config["Model"] = "custom-relay-model"
        self.send_config()
        self.assertEqual(self.requests[-1][2]["model"], "custom-relay-model")

    def test_global_setting_json(self):
        self.path.write_text(json.dumps({"AI": self.config, "Theme": "Dark"}), encoding="utf-8")
        config = read_config(self.path)
        self.assertEqual(config["Model"], self.config["Model"])
        self.assertEqual(build_request(config, [])["model"], self.config["Model"])

    def test_chat_compatibility(self):
        self.config.update(ApiMode="chat_completions", ReasoningMode="standard", ServiceTier="default")
        self.body = {"choices": [{"message": {"content": "兼容回复"}}]}
        self.assertEqual(self.send_config()["reply"], "兼容回复")
        body = self.requests[0][2]
        self.assertEqual(self.requests[0][0], "/proxy/v1/chat/completions")
        self.assertEqual(body["reasoning_effort"], "xhigh")
        self.assertEqual(body["response_format"], {"type": "json_object"})
        self.assertEqual(body["messages"][0]["role"], "system")

    def test_environment_key(self):
        self.config["ApiKeyEnvironmentVariable"] = "TEST_AI_KEY"
        with patch.dict("os.environ", {"TEST_AI_KEY": "env-only-key"}):
            self.send_config("")
        self.assertEqual(self.requests[0][1], "Bearer env-only-key")

    def test_incompatible_mode_is_not_downgraded(self):
        self.config["ApiMode"] = "chat_completions"
        with self.assertRaisesRegex(ValueError, "Responses"):
            self.send_config()
        self.assertFalse(self.requests)

    def test_sdk_error_hides_credentials(self):
        self.code = 401
        self.body = {"error": {"message": "test-only-key", "type": "invalid_request_error"}}
        with self.assertRaisesRegex(RuntimeError, "HTTP 401") as caught:
            self.send_config()
        self.assertNotIn("test-only-key", str(caught.exception))

    def test_defaults_and_markdown(self):
        self.config.update(ReasoningEffort="default", Verbosity="default", OutputFormat="markdown", MaxOutputTokens=0)
        self.path.write_text(json.dumps(self.config), encoding="utf-8")
        body = build_request(read_config(self.path), [])
        self.assertNotIn("effort", body["reasoning"])
        self.assertNotIn("max_output_tokens", body)
        self.assertNotIn("text", body)
        self.assertIn("Markdown", body["instructions"])

    def test_invalid_json_enum(self):
        self.config["ReasoningEffort"] = "none"
        with self.assertRaises(ValueError):
            self.send_config()
        self.assertFalse(self.requests)

    def test_mislabeled_string_response(self):
        self.content_type = "text/plain"
        self.assertEqual(self.send_config()["reply"], "完成")

    def test_proxy_ignores_stream_request(self):
        self.content_type = "text/plain"
        events = []
        self.assertEqual(self.send_config(on_event=lambda kind, text: events.append((kind, text)))["reply"], "完成")
        self.assertEqual(events, [("reply", "完成")])
        self.assertTrue(self.requests[0][2]["stream"])
        self.assertEqual(self.requests[0][2]["reasoning"]["summary"], "auto")

    def test_real_sdk_stream_summary_then_answer(self):
        self.content_type = "text/event-stream"
        final = self.body
        final["output"].insert(0, {"type": "reasoning", "id": "rs_test", "summary": [{"type": "summary_text", "text": "返回的摘要"}]})
        events = [
            {"type": "response.reasoning_summary_text.delta", "delta": "返回的摘要"},
            {"type": "response.output_text.delta", "delta": "完"},
            {"type": "response.output_text.delta", "delta": "成"},
            {"type": "response.completed", "response": final},
        ]
        self.body = "".join("data: " + json.dumps(event) + "\n\n" for event in events)
        received = []
        result = self.send_config(on_event=lambda kind, text: received.append((kind, text)))
        self.assertEqual(result, {"reply": "完成", "reasoning_summary": "返回的摘要"})
        self.assertEqual(received, [("reasoning_summary", "返回的摘要"), ("reply", "完"), ("reply", "成")])

    def test_truncated_stream_is_not_success(self):
        self.content_type = "text/event-stream"
        self.body = 'data: {"type":"response.output_text.delta","delta":"partial"}\n\n'
        with self.assertRaisesRegex(ValueError, "提前结束"):
            self.send_config(on_event=lambda *_: None)

    def test_anthropic_root_and_v1_paths(self):
        self.config.update(ApiMode="anthropic_messages", Model="claude-sonnet-5")
        self.body = {"id": "msg_test", "type": "message", "role": "assistant", "model": "claude-sonnet-5",
                     "content": [{"type": "text", "text": "完成"}], "stop_reason": "end_turn", "usage": {"input_tokens": 1, "output_tokens": 1}}
        for base in (self.base, self.base.removesuffix("/v1")):
            self.config["BaseUrl"] = base
            self.assertEqual(self.send_config()["reply"], "完成")
            path, _, body = self.requests[-1]
            self.assertEqual(path, "/proxy/v1/messages")
            self.assertEqual(body["max_tokens"], 4096)
            self.assertNotIn("reasoning", body)
            self.assertNotIn("service_tier", body)
            self.assertIn("精确回答", body["system"])

    def test_anthropic_stream_and_truncation(self):
        self.config.update(ApiMode="anthropic_messages", Model="claude-sonnet-5")
        self.content_type = "text/event-stream"
        events = [{"type": "content_block_delta", "index": 0, "delta": {"type": "text_delta", "text": "完成"}},
                  {"type": "message_delta", "delta": {"stop_reason": "end_turn"}, "usage": {"output_tokens": 1}},
                  {"type": "message_stop"}]
        self.body = "".join("event: " + event["type"] + "\n" + "data: " + json.dumps(event) + "\n\n" for event in events)
        received = []
        self.assertEqual(self.send_config(on_event=lambda *args: received.append(args))["reply"], "完成")
        self.assertEqual(received, [("reply", "完成")])
        self.body = 'event: content_block_delta\ndata: {"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"partial"}}\n\n'
        with self.assertRaisesRegex(ValueError, "提前结束"):
            self.send_config(on_event=lambda *_: None)
