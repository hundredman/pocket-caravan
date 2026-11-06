"""Run original R3/R4 rule sources in an isolated .NET probe, without importing game code."""
import hashlib
import argparse
import json
import os
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Docs/Validation/R3-R4-Merge-Review-2026-11-06'
SOURCE = 'claude/r3-r4'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--ref', default=SOURCE)
    args = parser.parse_args()
    def git(*args):
        return subprocess.check_output(['git', *args], cwd=ROOT)

    commit = git('rev-parse', '--verify', args.ref + '^{commit}').decode().strip()
    sdk = subprocess.check_output(['dotnet', '--list-sdks'], text=True)
    major = max(int(m.group(1)) for m in re.finditer(r'^(\d+)\.', sdk, re.M))
    framework = f'net{major}.0'
    files = [
        'Tools/Integration/StandingProbe/' + name
        for name in ('Program.cs', 'Stubs.cs', 'StandingProbe.csproj', 'nuget.config')
    ] + [
        'Assets/PocketCaravan/Scripts/' + name for name in (
            'Economy/Haggle.cs', 'Economy/StandingRecovery.cs', 'Economy/TradeActivity.cs',
            'Core/ReputationState.cs', 'Core/ReputationAwards.cs',
        )
    ]
    fingerprints = {}
    original_target = None
    original_target_log = ''
    env = dict(os.environ, DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1')
    with tempfile.TemporaryDirectory(prefix='pocket-r3-r4-review-') as folder:
        root = Path(folder)
        for file in files:
            data = git('show', f'{commit}:{file}')
            target = root / file
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(data)
            fingerprints[file] = hashlib.sha256(data).hexdigest()
        project = root / 'Tools/Integration/StandingProbe/StandingProbe.csproj'
        attempt = subprocess.run(
            ['dotnet', 'run', '--project', str(project), '--verbosity', 'quiet'],
            cwd=root, env=env, capture_output=True, text=True, timeout=180,
        )
        original_target = attempt.returncode
        original_target_log = attempt.stdout + attempt.stderr
        project.write_text(project.read_text().replace('net8.0', framework))

        def run():
            result = subprocess.run(
                ['dotnet', 'run', '--project', str(project), '--verbosity', 'quiet'],
                cwd=root, env=env, capture_output=True, text=True, timeout=180,
            )
            if result.returncode:
                raise RuntimeError(result.stdout[-3000:] + result.stderr[-3000:])
            return json.loads(result.stdout[result.stdout.index('{'):])

        original = run()
        (project.parent / 'Program.cs').write_text(r'''
using PocketCaravan;
using System.Text.Json;
const string town="naru", item="grain";
const int requested=100, actual=1;
float normal=HaggleRules.Chance(.03f,ReputationStanding.Neutral,HaggleStrength.Fair,town,actual);
float bulk=HaggleRules.Chance(.03f,ReputationStanding.Neutral,HaggleStrength.Fair,town,requested);
int day=Enumerable.Range(1,10000).First(d=>!HaggleRules.Roll(normal,d,town,item,false,HaggleStrength.Fair)
    && HaggleRules.Roll(bulk,d,town,item,false,HaggleStrength.Fair));
var one=Haggle.Try(new TradeActivity(),town,item,false,actual,ReputationStanding.Neutral,.03f,HaggleStrength.Fair,day,(day-1)*24);
var many=Haggle.Try(new TradeActivity(),town,item,false,requested,ReputationStanding.Neutral,.03f,HaggleStrength.Fair,day,(day-1)*24);
if(one.outcome==HaggleOutcome.Success || many.outcome!=HaggleOutcome.Success)throw new Exception("Counterexample not reproduced");
Console.WriteLine(JsonSerializer.Serialize(new{
    town,item,day,requested,actual,normalChance=normal,bulkChance=bulk,
    normalOutcome=one.outcome.ToString(),requestedOutcome=many.outcome.ToString(),
    dealAppliesToSameItemAfterQuantityChange=many.deal.AppliesTo(town,item,false,day),
    dealFields=typeof(HaggleDeal).GetFields().Select(f=>f.Name).ToArray(),
    limitation="Actual original C# rule source; quantity/affordability is supplied as a caller scenario. Unity UI/Market.Preview is not executed here."
}));
''')
        counterexample = run()
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'original-core-probe.json').write_text(json.dumps(original, ensure_ascii=False, indent=2) + '\n')
    (OUT / 'bulk-counterexample.json').write_text(json.dumps(counterexample, ensure_ascii=False, indent=2) + '\n')
    (OUT / 'source-manifest.json').write_text(json.dumps({
        'sourceCommit': commit, 'sourceRef': args.ref, 'originalFramework': 'net8.0',
        'originalTargetExitCode': original_target, 'reviewFramework': framework, 'sourceSha256': fingerprints,
        'note': 'Only temporary csproj framework and second probe Program.cs changed; game rule sources are unchanged. Unity dependencies use original probe stubs.',
    }, ensure_ascii=False, indent=2) + '\n')
    (OUT / 'original-target.log').write_text(original_target_log)
    print(json.dumps({'coreCount': original.get('count'), 'counterexample': counterexample}, ensure_ascii=False))


if __name__ == '__main__':
    main()
