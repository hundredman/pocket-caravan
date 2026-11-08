"""A4 review only: source-certified walking surfaces and material-specific CC0 foley."""
from pathlib import Path
import array,collections,json,math,random,re,shutil,subprocess,wave
import sys
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'WorldLayout'))
from t3_history import map_at
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A4-v3-20261105'
KINDS=('dirt','grass','stone','wood','splash');RATE=22050
def floor_kind(name):
 m=re.search(r'floor[:_](\d+)[:_](\d+)',name)
 if not m:return None
 row=int(m[2]);return 'D'if 7<=row<=10 else 'G'if row>=11 else 'S'
def surfaces(historical=False):
 cert=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text())['tiles'];maps=[]
 ids=['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan','transit_woodtown','transit_ferry','transit_southport','transit_crossroads','transit_wetroad','transit_pass','transit_central_approach','transit_south_inland']
 for mid in ids:
  d=(map_at((ROOT/'Assets/PocketCaravan/Maps'/(mid+'.json'))) if historical else json.loads((ROOT/'Assets/PocketCaravan/Maps'/(mid+'.json')).read_text()));w,h=d['w'],d['h'];data=['O']*(w*h);examples={}
  for i,t in enumerate(d['ground']):
   if not t or d['blocked'][i]:continue
   name=d['palette'][t];kind=floor_kind(name)
   if kind is None:
    parts=cert.get(name,{}).get('parts',[]);tags=[floor_kind(p[0])for p in parts];kind='D'if'D'in tags else'S'if'S'in tags else'G'
   deco=d['palette'][d['deco'][i]]if d['deco'][i]else''
   if d['road'][i]>0 and any(str(p[0]).startswith('bridge:')for p in cert.get(deco,{}).get('parts',[])):kind='W'
   x,y=i%w,i//w;data[(h-y-1)*w+x]=kind;examples.setdefault(kind,[]).append([x,y])
  maps.append(dict(id=mid,w=w,h=h,data=''.join(data),counts=dict(collections.Counter(data)),examples={k:v[len(v)//2]for k,v in examples.items()}))
 return maps
def samples(kind):
 source=OUT/'Sources'/(kind+('.wav'if kind=='splash'else'.ogg'))
 raw=subprocess.run([shutil.which('ffmpeg')or'ffmpeg','-hide_banner','-loglevel','error','-i',str(source),'-ac','1','-ar',str(RATE),'-f','f32le','-'],check=True,capture_output=True).stdout
 base=array.array('f');base.frombytes(raw)
 def lp(values,hz):
  a=1-math.exp(-2*math.pi*hz/RATE);state=0;result=[]
  for x in values:state+=a*(x-state);result.append(state)
  return result
 cutoff={'dirt':2200,'grass':3000,'stone':2600,'wood':2000,'splash':3200}[kind]
 body=lp(lp(base,cutoff),cutoff);dc=lp(body,25);duration=len(body)/RATE;values=[]
 for i,(x,low)in enumerate(zip(body,dc)):
  t=i/RATE;values.append((x-low)*min(1,t/.004)*min(1,(duration-t)/.025))
 rms=math.sqrt(sum(v*v for v in values)/len(values));gain=.036/max(rms,1e-8)
 values=[.20*math.tanh(v*gain/.20)for v in values];values[0]=values[-1]=0;return values
def wav(path,values):
 pcm=array.array('h',(round(max(-1,min(1,v))*32767)for v in values))
 with wave.open(str(path),'wb')as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(pcm.tobytes())
def build():
 OUT.mkdir(parents=True,exist_ok=True);native=OUT/'Native';native.mkdir(exist_ok=True);audio=[];timeline=[];all_demo=[]
 for kind in KINDS:
  values=samples(kind);wav(native/(kind+'.wav'),values);clip=[]
  interval=round(.52*RATE);clip=[0]*(interval*4+len(values))
  for j in range(5):
   for i,v in enumerate(values):clip[j*interval+i]+=v
  clip.extend([0]*round(.4*RATE));timeline.append(dict(kind=kind,startSeconds=len(all_demo)/RATE,durationSeconds=len(clip)/RATE));all_demo.extend(clip)
  wav(OUT/(kind+'-demo.wav'),clip);audio.append(dict(kind=kind,path=str((native/(kind+'.wav')).relative_to(ROOT)),sampleRate=RATE,samples=len(values),peak=max(abs(v)for v in values)))
 wav(OUT/'footsteps-review.wav',all_demo)
 for name in ['footsteps-review']+[k+'-demo'for k in KINDS]:subprocess.run([shutil.which('ffmpeg')or'ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(OUT/(name+'.wav')),'-codec:a','libmp3lame','-q:a','3',str(OUT/(name+'.mp3'))],check=True)
 document=dict(status='review',maps=surfaces(),audio=audio,timeline=timeline,generatedAudio='Distinct whole material-source clips: Kenney dirt/grass/concrete/wood and rubberduck water splash, gently filtered and levelled',audioSource='Sources/provenance.json',license='CC0; see Sources/CREDITS.md',revisionReason='v2 natural but material differences weak; replace shared base with separate surface recordings',stoneNote='Current owned map ground has no stone paving if S count is zero; sample is prepared, not forced onto dirt.')
 (OUT/'source.json').write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n');print([(m['id'],m['counts'])for m in document['maps']])
if __name__=='__main__':build()
