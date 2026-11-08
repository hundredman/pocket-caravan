"""Do not launch AppKit in the known denied environment or weaken native checks."""
import os
import subprocess
import unittest
from unittest.mock import patch
from Tools.Aseprite import cli


class AsepriteExecution(unittest.TestCase):
    def test_mac_seatbelt_rejected_before_subprocess_starts(self):
        with patch.object(cli.sys, 'platform', 'darwin'), patch.dict(os.environ, {'CODEX_SANDBOX': 'seatbelt'}), patch.object(cli.subprocess, 'run') as launch:
            with self.assertRaisesRegex(cli.AsepriteEnvironmentError, 'NOT started'):
                cli.run(['aseprite', '-b', 'source.aseprite'])
            launch.assert_not_called()

    def test_normal_mac_execution_preserves_exact_arguments_and_options(self):
        command=['/Applications/Aseprite.app/Contents/MacOS/aseprite','-b','source.aseprite','--save-as','output.png']
        with patch.object(cli.sys, 'platform', 'darwin'), patch.dict(os.environ, {}, clear=True), patch.object(cli.subprocess, 'run') as launch:
            result=cli.run(command, check=True, capture_output=True, timeout=60)
            launch.assert_called_once_with(command, check=True, capture_output=True, timeout=60)
            self.assertIs(result, launch.return_value)

    def test_nonmac_platform_is_not_blocked_by_mac_environment_marker(self):
        with patch.object(cli.sys, 'platform', 'linux'), patch.dict(os.environ, {'CODEX_SANDBOX': 'seatbelt'}), patch.object(cli.subprocess, 'run') as launch:
            cli.run(['aseprite','-b'])
            launch.assert_called_once()

    def test_real_export_failure_is_not_swallowed_or_skipped(self):
        failure=subprocess.CalledProcessError(1,['aseprite','-b'])
        with patch.object(cli.sys, 'platform', 'darwin'), patch.dict(os.environ, {}, clear=True), patch.object(cli.subprocess, 'run', side_effect=failure):
            with self.assertRaises(subprocess.CalledProcessError):
                cli.run(['aseprite','-b'],check=True)

    def test_full_runner_stops_before_native_tests_in_denied_environment(self):
        from Tools.Integration import run_checks
        with patch.object(cli.sys, 'platform', 'darwin'), patch.dict(os.environ, {'CODEX_SANDBOX': 'seatbelt'}), patch.object(run_checks.Path,'exists',return_value=True), patch.object(run_checks.unittest.defaultTestLoader,'discover') as discover, patch('sys.stderr'):
            self.assertEqual(run_checks.main(),2)
            discover.assert_not_called()

if __name__=='__main__':unittest.main()
