"""A4 review only: source-certified walking surfaces and original synthesized foley."""
from pathlib import Path
import array,collections,json,math,random,re,shutil,subprocess,wave
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A4-v2-20261105'
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
 # The original CC0 walking contact remains the body, rather than a pitched synthesized hit.
 source=ROOT/'Assets/PocketCaravan/Resources/Audio/Sfx/step.wav'
 raw=subprocess.run([shutil.which('ffmpeg')or'ffmpeg','-hide_banner','-loglevel','error','-i',str(source),'-ac','1','-ar',str(RATE),'-f','f32le','-'],check=True,capture_output=True).stdout
 base=array.array('f');base.frombytes(raw);rng=random.Random(271+KINDS.index(kind))
 def lp(values,hz):
  a=1-math.exp(-2*math.pi*hz/RATE);state=0;result=[]
  for x in values:state+=a*(x-state);result.append(state)
  return result
 cutoff={'dirt':1100,'grass':850,'stone':1500,'wood':700,'splash':1200}[kind]
 body=lp(lp(base,cutoff),cutoff);low=lp(body,260)
 noise=[rng.uniform(-1,1)for _ in body];noise=lp(lp(noise,1100),1100);nlow=lp(noise,140);values=[];duration=len(body)/RATE
 for i,x in enumerate(body):
  t=i/RATE;envelope=min(1,t/.015)*math.exp(-t/.12);texture=(noise[i]-nlow[i])*envelope
  if kind=='grass':v=x*.8+texture*.006
  elif kind=='wood':v=x*.75+low[i]*.35
  elif kind=='stone':v=x-low[i]*.15
  elif kind=='splash':v=x*.45+texture*.018
  else:v=x*.75+low[i]*.25+texture*.002
  values.append(v*min(1,t/.012)*min(1,(duration-t)/.035))
 rms=math.sqrt(sum(v*v for v in values)/len(values));gain=.032/max(rms,1e-8)
 values=[.20*math.tanh(v*gain/.20)for v in values];values[0]=values[-1]=0;return values
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
 document=dict(status='review',maps=surfaces(),audio=audio,timeline=timeline,generatedAudio='CC0 Ninja Adventure whole step.wav derivative: softened contact/EQ and low-level original texture, no pitched synthesized hits',audioSource='Assets/PocketCaravan/Resources/Audio/Sfx/step.wav',license='CC0; Assets/PocketCaravan/Art/NinjaAdventure/CREDITS.md',revisionReason='v1 too sharp and unnatural; lower transients and high frequencies, preserve existing walking contact',stoneNote='Current owned map ground has no stone paving if S count is zero; sample is prepared, not forced onto dirt.')
 (OUT/'source.json').write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n');print([(m['id'],m['counts'])for m in document['maps']])
if __name__=='__main__':build()
