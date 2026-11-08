"""Full content checks, with one preflight before any native Aseprite launch."""
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT))
from Tools.Aseprite.cli import AsepriteEnvironmentError, require_batch_access


def main():
    if Path('/Applications/Aseprite.app/Contents/MacOS/aseprite').exists():
        try:
            require_batch_access()
        except AsepriteEnvironmentError as error:
            print(error, file=sys.stderr)
            return 2
    suite = unittest.defaultTestLoader.discover(str(ROOT/'Tools/Integration'), pattern='test_*.py')
    return 0 if unittest.TextTestRunner(verbosity=1).run(suite).wasSuccessful() else 1


if __name__ == '__main__':
    sys.exit(main())
