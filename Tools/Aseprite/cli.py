"""Aseprite subprocess boundary: fail before launching AppKit in a denied session."""
import os
import subprocess
import sys


class AsepriteEnvironmentError(RuntimeError):
    pass


def require_batch_access():
    # -b still calls AppOSX::init / NSApplication on this Mac. Do not probe by
    # starting Aseprite: a probe itself produces the crash dialog we prevent.
    if sys.platform == 'darwin' and os.environ.get('CODEX_SANDBOX') == 'seatbelt':
        raise AsepriteEnvironmentError(
            'Aseprite was NOT started: macOS AppKit initialization crashes in '
            'this Codex seatbelt session. Run this same command through the '
            'approved unsandboxed tool execution (require_escalated), or from '
            'a normal local Terminal. Do not unset CODEX_SANDBOX, retry in this '
            'session, suppress crash reports, or mark native checks as passed.'
        )


def run(*args, **kwargs):
    require_batch_access()
    return subprocess.run(*args, **kwargs)


if __name__ == '__main__':
    command = sys.argv[1:]
    if command[:1] == ['--']:
        command = command[1:]
    if not command:
        sys.exit('Usage: python Tools/Aseprite/cli.py -- /path/to/aseprite -b ...')
    try:
        result = run(command)
    except AsepriteEnvironmentError as error:
        print(error, file=sys.stderr)
        sys.exit(2)
    sys.exit(result.returncode)
