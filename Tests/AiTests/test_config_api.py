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

    def send_config(self, key="test-only-key"):
        self.path.write_text(json.dumps(self.config, ensure_ascii=False), encoding="utf-8")
        return json.loads(run_from_file(str(self.path), json.dumps({"messages": [
            {"role": "user", "content": "你好"}, {"role": "assistant", "content": "你好！"},
            {"role": "user", "content": "继续"}]}), key))

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
