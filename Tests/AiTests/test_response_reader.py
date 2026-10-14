import json
import unittest
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "Business" / "Python"))
from AI.response_reader import snapshot, collect_sse


class ResponseReaderTests(unittest.TestCase):
    def test_json_string_and_plain_string(self):
        for value in ('{"output_text":"reply"}', json.dumps(json.dumps({"output_text": "reply"})), "reply"):
            self.assertEqual(snapshot(value)["reply"], "reply")

    def test_response_output_and_summary(self):
        result = snapshot({"output": [{"type": "reasoning", "summary": [{"text": "summary"}]},
            {"type": "message", "content": [{"type": "output_text", "text": "reply"}]}]})
        self.assertEqual(result, {"reply": "reply", "reasoning_summary": "summary"})

    def test_chat_json(self):
        self.assertEqual(snapshot('{"choices":[{"message":{"content":"relay reply"}}]}')["reply"], "relay reply")

    def test_chat_stream(self):
        text = 'data: {"choices":[{"delta":{"content":"reply"},"finish_reason":null}]}\n\ndata: [DONE]\n\n'
        self.assertEqual(collect_sse(text)["reply"], "reply")

    def test_html_and_error_are_not_answers(self):
        for value in ("<html>login</html>", {"error": {"message": "key"}}):
            with self.assertRaises(ValueError):
                snapshot(value)

    def test_no_fake_reasoning(self):
        self.assertEqual(snapshot({"output_text": "reply"})["reasoning_summary"], "")

    def test_incomplete_response_is_not_success(self):
        with self.assertRaisesRegex(ValueError, "未完成"):
            snapshot({"status": "incomplete", "output_text": "partial"})
