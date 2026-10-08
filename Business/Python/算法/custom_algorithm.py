from pathlib import Path
import sys


def reconstruct(path, u, sensitivity, xy, parameters):
    source_path = Path(path)
    namespace = {"__file__": str(source_path), "__name__": "emt_custom"}
    sys.path.insert(0, str(source_path.parent))
    try:
        exec(compile(source_path.read_text(encoding="utf-8-sig"), str(source_path), "exec"), namespace)
        function = namespace.get("reconstruct")
        if not callable(function):
            raise ValueError("Custom Python file must define reconstruct(u, sensitivity, xy, parameters)")
        return function(u, sensitivity, xy, parameters)
    finally:
        sys.path.pop(0)
