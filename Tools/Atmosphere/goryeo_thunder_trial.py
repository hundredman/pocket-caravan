"""Original quiet distant-thunder synthesis; review only, no downloaded samples."""
import json,wave,hashlib
from pathlib import Path
import numpy as np
from scipy.signal import butter,sosfilt
ROOT=Path(__file__).resolve().parents[2];OUT=ROOT/'ArtReview/Goryeo-StormLogs-20261105'
def build():
 sr=32000;duration=5.5;t=np.arange(int(sr*duration))/sr;rng=np.random.default_rng(20261105)
 noise=rng.standard_normal(len(t));low=sosfilt(butter(3,[32,380],btype='bandpass',fs=sr,output='sos'),noise)
 air=sosfilt(butter(2,[180,1000],btype='bandpass',fs=sr,output='sos'),rng.standard_normal(len(t)))
 env=(1-np.exp(-t/.25))*np.exp(-t/.95);roll=np.exp(-((t-1.45)/.5)**2)*.32+np.exp(-((t-2.6)/.7)**2)*.15
 y=(low*(env+roll)+air*env*.045);y*=np.minimum(1,t/.03)*np.minimum(1,(duration-t)/.6);y*=.24/np.max(np.abs(y));right=np.roll(y,96)*.96;right[:96]=0
 pcm=np.rint(np.column_stack([y,right])*32767).astype('<i2');OUT.mkdir(exist_ok=True,parents=True);(OUT/'Native').mkdir(exist_ok=True)
 p=OUT/'Native/distant-thunder.wav'
 with wave.open(str(p),'wb')as f:f.setnchannels(2);f.setsampwidth(2);f.setframerate(sr);f.writeframes(pcm.tobytes())
 (OUT/'source.json').write_text(json.dumps({'status':'review','provenance':'Original synthesized filtered noise, no external sample; not a field recording','sampleRate':sr,'seconds':duration,'seed':20261105,'peak':float(np.max(abs(y))),'rms':float(np.sqrt(np.mean(y*y))),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()},ensure_ascii=False,indent=2)+'\n')
if __name__=='__main__':build()
