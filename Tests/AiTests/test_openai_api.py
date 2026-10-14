import json
import sys
import threading
import unittest
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "Business" / "Python"))
from AI.ai_bridge import run
from AI.openai_api import chat


class ApiTests(unittest.TestCase):
    def setUp(self):
        self.requests = []
        self.code = 200
        self.body = {"choices": [{"message": {"content": "回复成功"}}]}
        owner = self

        class Handler(BaseHTTPRequestHandler):
            def do_POST(self):
                owner.requests.append((self.path, self.headers.get("Authorization"),
                    json.loads(self.rfile.read(int(self.headers["Content-Length"])))))
                self.send_response(owner.code)
                self.send_header("Content-Type", getattr(owner, "content_type", "application/json"))
                if owner.code == 302:
                    self.send_header("Location", "/redirected")
                self.end_headers()
                self.wfile.write((owner.body if isinstance(owner.body, str) else json.dumps(owner.body)).encode())

            def log_message(self, *args):
                pass

        self.server = HTTPServer(("127.0.0.1", 0), Handler)
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.base = "http://127.0.0.1:%s/proxy/v1" % self.server.server_port

    def tearDown(self):
        self.server.shutdown()
        self.server.server_close()
        self.thread.join()

    def send(self, base=None):
        return chat([{"role": "user", "content": "你好"}], "custom-model", "test-only-key", base or self.base)

    def test_proxy_model_and_messages(self):
        self.assertEqual(self.send(self.base + "/"), "回复成功")
        path, authorization, payload = self.requests[0]
        self.assertEqual(path, "/proxy/v1/chat/completions")
        self.assertEqual(authorization, "Bearer test-only-key")
        self.assertEqual(payload, {"model": "custom-model", "messages": [{"role": "user", "content": "你好"}], "stream": False})

    def test_full_endpoint(self):
        self.send(self.base + "/chat/completions")
        self.assertEqual(self.requests[0][0], "/proxy/v1/chat/completions")

    def test_bridge(self):
        result = json.loads(run(json.dumps({"provider": "openai", "base_url": self.base,
            "model": "custom-model", "api_key": "test-only-key", "messages": []})))
        self.assertEqual(result["reply"], "回复成功")

    def test_http_error_does_not_echo_credentials(self):
        self.code = 401
        self.body = {"error": "test-only-key"}
        with self.assertRaisesRegex(RuntimeError, "HTTP 401") as caught:
            self.send()
        self.assertNotIn("test-only-key", str(caught.exception))

    def test_redirect_does_not_forward_key(self):
        self.code = 302
        with self.assertRaisesRegex(RuntimeError, "HTTP 302"):
            self.send()
        self.assertEqual(len(self.requests), 1)

    def test_invalid_response(self):
        self.body = {"choices": []}
        with self.assertRaises(ValueError):
            self.send()

    def test_invalid_url(self):
        for base in ("file:///secret", "https://key@example.test/v1", "https://example.test/v1?key=x"):
            with self.assertRaises(ValueError):
                self.send(base)
        self.assertFalse(self.requests)


if __name__ == "__main__":
    unittest.main()
