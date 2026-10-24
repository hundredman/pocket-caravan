"""Exercise a local stdio MCP and real Aseprite; writes only technical fixtures."""
import argparse
import base64
import json
from pathlib import Path
import queue
import struct
import subprocess
import threading
import tempfile


def check(binary, workspace, script):
    process = subprocess.Popen(
        [str(binary), '--aseprite', '/Applications/Aseprite.app/Contents/MacOS/aseprite',
         '--workspace', str(workspace), '--timeout', '30s'],
        stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True,
    )
    messages = queue.Queue()
    def read_messages():
        for line in process.stdout:
            messages.put(json.loads(line))
        messages.put(None)
    threading.Thread(target=read_messages, daemon=True).start()
    def send(message):
        process.stdin.write(json.dumps(message) + '\n')
        process.stdin.flush()
    def request(number, method, params):
        send(dict(jsonrpc='2.0', id=number, method=method, params=params))
        while True:
            response = messages.get(timeout=45)
            if response is None:
                raise RuntimeError('MCP exited before response')
            if response.get('id') == number:
                assert 'error' not in response, response
                return response['result']
    try:
        init = request(1, 'initialize', dict(protocolVersion='2024-11-05',
            capabilities={}, clientInfo=dict(name='PocketCaravan-A6-check', version='1')))
        send(dict(jsonrpc='2.0', method='notifications/initialized'))
        names = sorted(t['name'] for t in request(2, 'tools/list', {})['tools'])
        assert names == ['execute_lua', 'inspect_export'], names
        execution = request(3, 'tools/call', dict(name='execute_lua', arguments=dict(script=script)))
        assert not execution.get('isError'), execution
        assert 'A6_SMOKE_OK' in json.dumps(execution), execution
        inspection = request(4, 'tools/call', dict(name='inspect_export', arguments=dict(path='a6-smoke.png')))
        assert not inspection.get('isError'), inspection
        images = [c for c in inspection['content'] if c['type'] == 'image']
        assert len(images) == 1 and images[0]['mimeType'] == 'image/png'
        png = base64.b64decode(images[0]['data'])
        assert png[:8] == b'\x89PNG\r\n\x1a\n'
        assert struct.unpack('>II', png[16:24]) == (16, 16)
        assert (workspace/'a6-smoke.aseprite').is_file()
        with tempfile.TemporaryDirectory(prefix='a6-outside-') as folder:
            outside = Path(folder)/'outside.png'
            outside.write_bytes(png)
            rejected = request(5, 'tools/call', dict(name='inspect_export', arguments=dict(path=str(outside))))
            assert rejected.get('isError') and 'outside workspace' in json.dumps(rejected), rejected
        return dict(protocolVersion=init['protocolVersion'], tools=names,
                    execute_lua='passed', inspect_export='passed: 16x16 image content',
                    outside_path='rejected', aseprite_export='exists')
    finally:
        process.stdin.close()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.terminate()
            process.wait(timeout=5)
        process.stdout.close()
        process.stderr.close()


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--binary', type=Path, required=True)
    parser.add_argument('--workspace', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    args = parser.parse_args()
    args.workspace.mkdir(parents=True, exist_ok=True)
    result = check(args.binary.resolve(), args.workspace.resolve(),
                   Path(__file__).with_name('smoke.lua').read_text())
    args.report.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps(result, ensure_ascii=False))
