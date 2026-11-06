"""R3 흥정·R4 평판 회복 규칙: 기록된 진단 결과와, dotnet이 있으면 원본으로 다시 실행한 결과를 확인한다."""
import json,shutil,subprocess,unittest,tempfile
import xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/Validation/R3-R4-2026-10-27/probe.json'
PROJECT=ROOT/'Tools/Integration/StandingProbe/StandingProbe.csproj'
REQUIRED=('haggling never changes reputation','hard failure cancels the deal and bans trade for a few hours',
          'normal failure locks haggling with that merchant for the day','reloading cannot reroll a failed haggle',
          'from the floor (적대 -6000) chores alone reach neutral and never pass it','chores raise and stop exactly at neutral',
          'donation charges only what is needed','central village A recovery leaves village B unchanged',
          'reloaded chore still completes only once','old trade activity save loads with empty haggle and recovery records')

class R3R4Rules(unittest.TestCase):
    def check(self,data):
        for name in REQUIRED:self.assertIn(name,data['passed'])
        self.assertEqual(data['count'],len(data['passed']))

    def test_recorded_probe(self):self.check(json.loads(OUT.read_text(encoding='utf-8')))

    @unittest.skipUnless(shutil.which('dotnet'),'dotnet 없음')
    def test_live_probe(self):
        sdks=subprocess.check_output(['dotnet','--list-sdks'],text=True)
        majors=[int(line.split('.')[0]) for line in sdks.splitlines() if line and line[0].isdigit()]
        self.assertTrue(majors,'installed dotnet SDK missing')
        target='net%d.0'%max(majors)
        # Preserve the net8 reference project; compile the same sources against an installed SDK in isolation.
        with tempfile.TemporaryDirectory(prefix='goryeo-standing-') as tmp:
            tmp=Path(tmp)
            tree=ET.parse(PROJECT)
            tree.find('.//TargetFramework').text=target
            for compile in tree.findall('.//Compile'):
                compile.set('Include',str((PROJECT.parent/compile.get('Include')).resolve()))
            for source in ('Program.cs','Stubs.cs','nuget.config'):
                shutil.copy2(PROJECT.parent/source,tmp/source)
            tree.write(tmp/PROJECT.name)
            r=subprocess.run(['dotnet','run','--project',str(tmp/PROJECT.name),'--verbosity','quiet'],capture_output=True,text=True,cwd=ROOT,timeout=600)
        if r.returncode!=0 and 'NU1301' in (r.stdout+r.stderr):self.skipTest('NuGet 접근 불가')
        self.assertEqual(r.returncode,0,r.stdout[-2000:]+r.stderr[-2000:])
        self.check(json.loads(r.stdout[r.stdout.index('{'):]))

if __name__=='__main__':unittest.main()
