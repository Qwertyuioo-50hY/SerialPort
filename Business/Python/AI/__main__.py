import sys
from .ai_bridge import run

sys.stdin.reconfigure(encoding="utf-8")
sys.stdout.reconfigure(encoding="utf-8")
print(run(sys.stdin.read()))
