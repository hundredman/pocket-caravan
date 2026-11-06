"""A4 review only: source-certified walking surfaces and original synthesized foley."""
from pathlib import Path
import array,collections,json,math,random,re,shutil,subprocess,wave
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A4-20261105'
KINDS=('dirt','grass','stone','wood','splash');RATE=22050
def floor_kind(name):
 m=re.search(r'floor[:_](\d+)[:_](\d+)',name)
 if not m:return None
 row=int(m[2]);return 'D'if 7<=row<=10 else 'G'if row>=11 else 'S'
def surfaces():
 cert=json.loads((ROOT/'Assets/PocketCaravan/Art/Baked/m4-composites.json').read_text())['tiles'];maps=[]
 ids=['Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan','transit_woodtown','transit_ferry','transit_southport','transit_crossroads','transit_wetroad','transit_pass','transit_central_approach','transit_south_inland']
 for mid in ids:
  d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/(mid+'.json')).read_text());w,h=d['w'],d['h'];data=['O']*(w*h);examples={}
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
 duration={'dirt':.20,'grass':.24,'stone':.15,'wood':.20,'splash':.28}[kind];rng=random.Random(271+KINDS.index(kind));lo=mid=hi=0;values=[]
 for i in range(round(duration*RATE)):
  t=i/RATE;n=rng.uniform(-1,1);lo+=.045*(n-lo);mid+=.25*(n-mid);hi+=.65*(n-hi);body=math.sin(2*math.pi*72*t)*math.exp(-t/.035)
  if kind=='dirt':v=.38*body+.9*(mid-lo)*math.exp(-t/.055)+.13*(hi-mid)*math.exp(-t/.045)
  elif kind=='grass':v=.2*body+.85*(mid-lo)*math.exp(-t/.085)*(1+.25*math.sin(t*130))
  elif kind=='wood':v=.4*math.sin(2*math.pi*145*t)*math.exp(-t/.04)+.16*math.sin(2*math.pi*325*t)*math.exp(-t/.028)+.5*(mid-lo)*math.exp(-t/.025)
  elif kind=='stone':v=.22*math.sin(2*math.pi*115*t)*math.exp(-t/.028)+.17*math.sin(2*math.pi*440*t)*math.exp(-t/.014)+.9*(hi-lo)*math.exp(-t/.018)
  else:v=1.1*(mid-lo)*math.exp(-t/.085)+.13*math.sin(2*math.pi*(340*t-180*t*t))*math.exp(-t/.09)
  values.append(v*min(1,t/.004)*min(1,(duration-t)/.025))
 peak=max(abs(v)for v in values);values=[v*.58/peak for v in values];values[0]=values[-1]=0;return values
def wav(path,values):
 pcm=array.array('h',(round(max(-1,min(1,v))*32767)for v in values))
 with wave.open(str(path),'wb')as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(RATE);f.writeframes(pcm.tobytes())
def build():
 OUT.mkdir(parents=True,exist_ok=True);native=OUT/'Native';native.mkdir(exist_ok=True);audio=[];timeline=[];all_demo=[]
 for kind in KINDS:
  values=samples(kind);wav(native/(kind+'.wav'),values);clip=[]
  for j in range(5):clip.extend(values+[0]*(round(.46*RATE)-len(values)))
  clip.extend([0]*round(.4*RATE));timeline.append(dict(kind=kind,startSeconds=len(all_demo)/RATE,durationSeconds=len(clip)/RATE));all_demo.extend(clip)
  wav(OUT/(kind+'-demo.wav'),clip);audio.append(dict(kind=kind,path=str((native/(kind+'.wav')).relative_to(ROOT)),sampleRate=RATE,samples=len(values),peak=max(abs(v)for v in values)))
 wav(OUT/'footsteps-review.wav',all_demo)
 for name in ['footsteps-review']+[k+'-demo'for k in KINDS]:subprocess.run([shutil.which('ffmpeg')or'ffmpeg','-hide_banner','-loglevel','error','-y','-i',str(OUT/(name+'.wav')),'-codec:a','libmp3lame','-q:a','3',str(OUT/(name+'.mp3'))],check=True)
 document=dict(status='review',maps=surfaces(),audio=audio,timeline=timeline,generatedAudio='Original deterministic procedural foley; no external recordings',stoneNote='Current owned map ground has no stone paving if S count is zero; sample is prepared, not forced onto dirt.')
 (OUT/'source.json').write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n');print([(m['id'],m['counts'])for m in document['maps']])
if __name__=='__main__':build()
