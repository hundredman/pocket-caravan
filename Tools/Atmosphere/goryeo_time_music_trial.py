"""Original A2 review score and soft synthesized timbres. No external samples or production writes."""
from pathlib import Path
import json,math,wave,subprocess,tempfile,hashlib
import numpy as np
from scipy.signal import butter,sosfilt
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-Atmosphere-A2-20261105';RATE=32000
NAMES=('morning','day','evening','night');TEMPO=(88,104,80,68)
MOTIFS=[[(74,0,1),(76,1,1),(79,2,1.5)],[(81,0,1.5),(79,2,1),(76,3,.7)],[(79,0,1),(83,1,1),(81,2,1.5)],[(79,0,1.5),(76,2,.7),(74,3,.8)],[(76,0,1),(79,1,1),(81,2,.8),(79,3,.7)],[(83,0,1.5),(81,2,1.2)],[(79,0,1),(76,1.5,1),(74,3,.7)],[(76,0,1),(74,1.5,2)]]
CHORDS=((50,57,62),(43,50,59),(47,54,62),(45,52,59))
def low(x,hz):return sosfilt(butter(2,hz,fs=RATE,output='sos'),x,axis=0)
def tone(kind,midi,duration,seed):
 rng=np.random.default_rng(seed);tail=.5 if kind=='flute'else 1.2;n=int((duration+tail)*RATE);t=np.arange(n)/RATE;f=440*2**((midi-69)/12)
 if kind=='pluck':
  x=sum((.65/h**1.7)*np.sin(2*np.pi*f*h*t+.04*h)*np.exp(-t*(1.45+.55*h))for h in range(1,7));x+=.10*np.sin(2*np.pi*f*1.002*t)*np.exp(-t*2.4);attack=.012
 elif kind=='flute':
  phase=np.cumsum(2*np.pi*f*(1+.0023*np.sin(2*np.pi*4.3*t)*np.clip((t-.2)*2,0,1))/RATE);x=np.sin(phase)+.15*np.sin(2*phase)+.025*np.sin(3*phase);x+=low(rng.normal(0,.015,n),1800);attack=.10
 elif kind=='pad':
  x=.55*np.sin(2*np.pi*f*t)+.22*np.sin(2*np.pi*f*.998*t)+.08*np.sin(2*np.pi*f*2*t);attack=.35
 else:
  x=.65*np.sin(2*np.pi*f*t)*np.exp(-t*2.0)+.045*np.sin(2*np.pi*f*2.98*t)*np.exp(-t*8);attack=.025
 env=np.minimum(1,t/attack)*np.minimum(1,np.maximum(0,duration+tail-t)/(.35 if kind=='flute'else .65));x*=env
 if kind=='pad':x*=np.minimum(1,np.maximum(0,duration+.4-t)/.8)
 return low(x,4200 if kind=='flute'else 3000)
def score(slot):
 result=[];beat=60/TEMPO[slot]
 for bar in range(16):
  pattern=MOTIFS[bar%8]
  for i,(midi,at,dur)in enumerate(pattern):
   if slot==3 and i==1:continue
   kind=('flute','pluck','pluck','mallet')[slot];m=midi-(12 if slot in (2,3)else 0)
   result.append(dict(kind=kind,midi=m,beat=bar*4+at,durationBeats=dur,level=(.25,.34,.29,.25)[slot],pan=0 if slot==0 else -.12))
   if slot==1 and i==0 and bar%2==0:result.append(dict(kind='flute',midi=midi-12,beat=bar*4+at+.04,durationBeats=dur+.2,level=.10,pan=.15))
  chord=CHORDS[(bar//2)%4]
  for j,midi in enumerate(chord):
   if slot==3 and j==1:continue
   result.append(dict(kind='pad'if slot in (2,3)else'pluck',midi=midi,beat=bar*4+j*(.6 if slot<2 else .03),durationBeats=3.7-j*.4,level=(.13,.16,.105,.085)[slot],pan=(-.4,0,.4)[j]))
  if slot==1:
   for at in (0,2):result.append(dict(kind='drum',midi=0,beat=bar*4+at,durationBeats=.5,level=.07,pan=0))
 return result
def render(slot,events):
 duration=64*60/TEMPO[slot];length=round(duration*RATE);tail=RATE*3;mix=np.zeros((length+tail,2))
 for i,e in enumerate(events):
  if e['kind']=='drum':
   t=np.arange(int(RATE*.35))/RATE;x=np.sin(2*np.pi*(55*t+27*(1-np.exp(-t*22))/22))*np.exp(-t*16)*np.minimum(1,t/.012)
  else:x=tone(e['kind'],e['midi'],e['durationBeats']*60/TEMPO[slot],slot*1000+i)
  pos=round(e['beat']*60/TEMPO[slot]*RATE);n=min(len(x),len(mix)-pos);pan=e['pan'];mix[pos:pos+n,0]+=x[:n]*math.sqrt((1-pan)/2)*e['level'];mix[pos:pos+n,1]+=x[:n]*math.sqrt((1+pan)/2)*e['level']
 # Short quiet room reflections. Tail folds into next cycle, without a silent fade at the loop boundary.
 dry=mix.copy()
 for delay,gain in ((.047,.10),(.083,.07),(.127,.04)):
  k=round(delay*RATE);mix[k:,0]+=dry[:-k,1]*gain;mix[k:,1]+=dry[:-k,0]*gain
 mix=low(mix,4800 if slot<2 else 3500);loop=mix[:length].copy();loop[:tail]+=mix[length:length+tail];loop-=np.mean(loop,axis=0);loop*=.063/max(1e-9,float(np.sqrt(np.mean(loop**2))));loop=.40*np.tanh(loop/.40)
 return loop
def write(path,x):
 pcm=np.round(np.clip(x,-1,1)*32767).astype('<i2')
 with wave.open(str(path),'wb')as f:f.setparams((2,2,RATE,0,'NONE','not compressed'));f.writeframes(pcm.tobytes())
def build():
 (OUT/'Native').mkdir(parents=True,exist_ok=True);rows=[];combined=[];timeline=[]
 for slot,name in enumerate(NAMES):
  events=score(slot);x=render(slot,events)
  write(OUT/'Native'/f'{name}.wav',x)
  preview=x[:14*RATE].copy();fade=int(.25*RATE);preview[:fade]*=np.linspace(0,1,fade)[:,None];preview[-fade:]*=np.linspace(1,0,fade)[:,None]
  with tempfile.TemporaryDirectory(prefix='goryeo-music-review-')as tmp:
   wav=Path(tmp)/'preview.wav';write(wav,preview);subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-i',str(wav),'-codec:a','libmp3lame','-q:a','2',str(OUT/f'{name}-review.mp3')],check=True)
  start=sum(len(v)for v in combined)/RATE;timeline.append(dict(kind=name,startSeconds=start,durationSeconds=14));combined.extend([preview,np.zeros((int(.5*RATE),2))])
  rows.append(dict(name=name,tempo=TEMPO[slot],durationSeconds=len(x)/RATE,rms=float(np.sqrt(np.mean(x*x))),peak=float(np.max(np.abs(x))),loopJump=float(np.max(np.abs(x[0]-x[-1]))),path=str((OUT/'Native'/f'{name}.wav').relative_to(ROOT)),score=events))
 with tempfile.TemporaryDirectory(prefix='goryeo-music-demo-')as tmp:
  wav=Path(tmp)/'demo.wav';write(wav,np.concatenate(combined));subprocess.run(['/opt/homebrew/bin/ffmpeg','-v','error','-y','-i',str(wav),'-codec:a','libmp3lame','-q:a','2',str(OUT/'time-music-review.mp3')],check=True)
 (OUT/'source.json').write_text(json.dumps(dict(status='review',sampleRate=RATE,channels=2,tracks=rows,timeline=timeline,timeSlots=['05:00-10:00','10:00-17:00','17:00-20:00','20:00-05:00'],policy='Town maps: four original time arrangements. Four transit road maps keep existing road clip; danger keeps existing danger clip. Two-source fade, mute/music setting respected. No shared AudioManager edits.',provenance='Original score and deterministic synthesized instruments in Tools/Atmosphere/goryeo_time_music_trial.py; no external recordings, no copied regional code or music. Instrument descriptions are synthesized timbre intentions, not recordings of traditional instruments.'),ensure_ascii=False,indent=2)+'\n')
 print([(r['name'],round(r['durationSeconds'],2),round(r['rms'],4),round(r['peak'],3),round(r['loopJump'],5))for r in rows])
if __name__=='__main__':build()
