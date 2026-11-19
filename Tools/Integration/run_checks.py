"""Full content checks, with one preflight before any native Aseprite launch.

11/19 윤채: Windows에서도 그대로 돌게 했다. Windows 파이썬은 파일을 기본으로 cp949로 읽어 한글이 든 파일에서
테스트 수백 개가 틀렸다. Windows에서는 이 파일이 스스로 UTF-8 모드(PYTHONUTF8=1)로 한 번 다시 켜진다.
테스트가 부르는 다른 도구(자식 프로세스)도 같은 설정을 물려받는다. 같이 compat/sitecustomize.py를 먼저 읽게 해서
Pillow 12보다 낮은 컴퓨터(파이썬 3.9는 12를 못 깐다)에서도 그림 테스트가 쓰는 함수 하나를 잇는다. 맥·리눅스는 그대로다.
"""
from pathlib import Path
import os
import subprocess
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


def relaunch_utf8():
    """Windows: UTF-8 모드 + compat를 켠 파이썬으로 이 파일을 한 번 다시 돌리고 그 결과를 돌려준다 (이미 다시 켠 것이면 None)"""
    if os.name != 'nt' or os.environ.get('POCKET_CHECKS_UTF8') == '1':
        return None
    paths = [str(ROOT / 'Tools/Integration/compat')] + ([os.environ['PYTHONPATH']] if os.environ.get('PYTHONPATH') else [])
    env = dict(os.environ, PYTHONUTF8='1', PYTHONPATH=os.pathsep.join(paths), POCKET_CHECKS_UTF8='1')
    return subprocess.call([sys.executable, '-X', 'utf8', str(Path(__file__).resolve()), *sys.argv[1:]], env=env)


if __name__ == '__main__':
    code = relaunch_utf8()
    sys.exit(main() if code is None else code)
