"""A3 review only: sourced living ambience and authored anchors, never writes production assets."""
from pathlib import Path
import json,subprocess,wave,math,array
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A3-20261105'
KINDS=('crowd','chicken','cow','dog','bird');RATE=22050
MAPS=('Naru','Gaegyeong','Dakmok','goryeo_songak','goryeo_jangdan','transit_woodtown','transit_ferry','transit_southport','transit_crossroads','transit_wetroad','transit_pass','transit_central_approach','transit_south_inland')
def maps():
 result=[]
 for id in MAPS:
  d=json.loads((ROOT/'Assets/PocketCaravan/Maps'/f'{id}.json').read_text());w,h=d['w'],d['h'];points=[]
  def point(kind,x,y,reason):
   if kind in ('chicken','dog','cow'):
    land=[(xx,yy)for yy in range(h)for xx in range(w)if d['reachable'][yy*w+xx]and not d['blocked'][yy*w+xx]and not d['water'][yy*w+xx]]
    x,y=min(land,key=lambda c:(c[0]-x)**2+(c[1]-y)**2)
   points.append(dict(kind=kind,x=x,y=h-y-1,radius=12 if kind=='crowd'else 14,level=.32 if kind=='crowd'else .45,startHour=8 if kind=='crowd'else 5,endHour=19 if kind!='dog'else 21,interval=0 if kind=='crowd'else dict(chicken=26,cow=40,dog=44,bird=22)[kind],reason=reason))
  markets=[o for o in d['objects']if o['kind']=='market']
  for m in markets:point('crowd',m['x'],m['y'],'existing market object, daytime only')
  crops=d.get('natureCrops',[])
  if crops:
   c=crops[0];point('bird',c['x']+c['w']//2,c['y']+c['h']//2,'existing rice field, occasional birds')
  else:
   # An existing reachable grass point, rather than inventing new land or structures.
   surface=next(m for m in json.loads((ROOT/'ArtReview/Goryeo-Atmosphere-A4-v3-20261105/source.json').read_text())['maps']if m['id']==id)['data']
   candidates=[(x,y)for y in range(4,h-4)for x in range(4,w-4)if d['reachable'][y*w+x]and not d['blocked'][y*w+x]and surface[(h-y-1)*w+x]=='G']
   if candidates:
    x,y=min(candidates,key=lambda c:(c[0]-w*.55)**2+(c[1]-h*.5)**2);point('bird',x,y,'existing reachable grass clearing')
  if markets:
   m=markets[0]
   kind='chicken'if id in ('Naru','goryeo_jangdan')else'dog'
   point(kind,max(0,m['x']-8),min(h-1,m['y']+5),'yard ambience near existing village; A7 animal anchor candidate')
  if id=='goryeo_jangdan'and crops:
   c=crops[0];point('cow',c['x'],c['y']+c['h'],'field edge, occasional distant cow; A7 anchor candidate')
  result.append(dict(id=id,w=w,h=h,points=points))
 return result

def decode(path):
 raw=subprocess.check_output(['/opt/homebrew/bin/ffmpeg','-v','error','-i',str(path),'-f','f32le','-ac','1','-ar',str(RATE),'-']);a=array.array('f');a.frombytes(raw);return a

def lp(x,hz):
 a=1-math.exp(-2*math.pi*hz/RATE);v=0.;out=[]
 for s in x:v+=a*(s-v);out.append(v)
 return out

def write(path,x):
 pcm=array.array('h',(int(max(-1,min(1,v))*32767)for v in x))
 with wave.open(str(path),'wb')as f:f.setparams((1,2,RATE,0,'NONE','not compressed'));f.writeframes(pcm.tobytes())

def build():
 (OUT/'Native').mkdir(parents=True,exist_ok=True);clips={};metadata=[]
 for kind in KINDS:
  raw=decode(OUT/'Sources'/f'{kind}.mp3')
  if kind=='crowd':
   n=RATE*16;offsets=(12,45,88);x=[sum(raw[(j+int(t*RATE))%len(raw)]for t in offsets)/3 for j in range(n)];x=lp(lp(x,1050),1050)
   low=lp(x,180);x=[a-b for a,b in zip(x,low)]
   # Circular blend: last half-second becomes the transition into the first.
   k=RATE//2;head=x[:k];tail=x[-k:];blend=[tail[i]*(1-i/(k-1))+head[i]*i/(k-1)for i in range(k)];x=blend+x[k:-k]
  else:
   x=list(raw[2*RATE:5*RATE])if kind=='chicken'else list(raw)
   x=lp(x,3400 if kind=='bird'else 2800);low=lp(x,45);x=[a-b for a,b in zip(x,low)]
  rms=math.sqrt(sum(v*v for v in x)/max(1,len(x)));scale=.045/max(rms,1e-8);x=[.22*math.tanh(v*scale/.22)for v in x]
  if kind!='crowd':
   fade=min(len(x)//3,int(RATE*.025))
   for i in range(fade):x[i]*=i/(fade-1);x[-1-i]*=i/(fade-1)
  write(OUT/'Native'/f'{kind}.wav',x);clips[kind]=x;metadata.append(dict(kind=kind,duration=len(x)/RATE,peak=max(abs(v)for v in x)))
 # Isolated sources, then a quiet market/yard mix. Listening level raised from runtime gain for comparison.
 demo=[];timeline=[]
 for kind in KINDS:
  x=clips[kind];start=len(demo)/RATE
  if kind=='crowd':
   x=x[:8*RATE];x=[v*(.85 if i<4*RATE else .3)for i,v in enumerate(x)]
  demo.extend(x);demo.extend([0.]*(RATE//2));timeline.append(dict(kind=kind,startSeconds=start,durationSeconds=len(x)/RATE))
 mixed=[clips['crowd'][i%len(clips['crowd'])]*.55 for i in range(RATE*10)]
 for kind,start in [('chicken',1),('bird',4),('dog',7)]:
  for i,v in enumerate(clips[kind]):
   j=start*RATE+i
   if j<len(mixed):mixed[j]+=v*.65
 timeline.append(dict(kind='mixed market yard',startSeconds=len(demo)/RATE,durationSeconds=10));demo+=mixed
 write(OUT/'life-review.wav',demo)
 subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-i',str(OUT/'life-review.wav'),'-codec:a','libmp3lame','-q:a','2',str(OUT/'life-review.mp3')],check=True)
 source=dict(status='review',maps=maps(),audio=metadata,timeline=timeline,sourceNotes='Sources/CREDITS.md; real CC0 recordings, gentle filtering/leveling. Crowd: French market, layered/filtered into indistinct walla; not Korean dialogue. Existing MapAmbience water unchanged.')
 (OUT/'source.json').write_text(json.dumps(source,ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':build()
