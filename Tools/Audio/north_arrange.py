# 북유럽 시간대 배경 음악 편곡: naru.ogg를 basic-pitch로 받아 적은 음표(north_naru_notes.csv) → 북유럽 악기로 다시 연주 → north_bgm_*.ogg
# 사용: python3 north_arrange.py <출력폴더>   (numpy, scipy, soundfile, ffmpeg 필요)
import os, csv, numpy as np, soundfile as sf, subprocess, sys
from scipy.signal import lfilter, fftconvolve, butter, sosfilt
R=44100; S=0.1875; BARS=12
rows=[r for r in csv.reader(open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'north_naru_notes.csv')))][1:]
notes=[]
for r in rows:
    k=round(float(r[0])/S); d=max(1,round((float(r[1])-float(r[0]))/S)); p=int(r[2]); v=float(r[3])
    if k<0 or k>=BARS*16: continue
    if v<50: continue                     # 약한 것은 배음 잡음
    notes.append([k,d,p,v])
# 같은 음 겹침 정리
notes.sort(); clean=[]
for n in notes:
    if any(c[2]==n[2] and c[0]<=n[0]<c[0]+c[1] for c in clean[-12:]): continue
    clean.append(n)
notes=clean
def hz(p): return 440*2**((p-69)/12)
def env_adsr(n, a, rel, sus=1.0):
    e=np.ones(n)*sus; ai=min(n,int(a*R)); e[:ai]=np.linspace(0,1,ai)*sus if ai>0 else e[:ai]
    ri=min(n,int(rel*R)); e[n-ri:]*=np.linspace(1,0,ri); return e
def bowed(f,dur,bright=1.0,vibd=0.005,noise=0.02,seed=0):
    n=int((dur+0.25)*R); t=np.arange(n)/R
    vib=1+vibd*np.sin(2*np.pi*5.5*t)*np.clip((t-0.15)/0.3,0,1)
    ph=np.cumsum(2*np.pi*f*vib/R); v=np.zeros(n)
    for h in range(1,12):
        if f*h>9000: break
        v+=np.sin(ph*h)/h**(1.0+0.6/bright)
    v+=np.random.default_rng(seed).normal(0,noise,n)
    e=env_adsr(n,0.06,0.25); return v*e*0.5
def flute(f,dur,seed=0):
    n=int((dur+0.2)*R); t=np.arange(n)/R
    vib=1+0.006*np.sin(2*np.pi*5*t)*np.clip((t-0.12)/0.3,0,1)
    ph=np.cumsum(2*np.pi*f*vib/R)
    v=np.sin(ph)+0.25*np.sin(2*ph)+0.08*np.sin(3*ph)
    br=lfilter([1],[1,-0.9],np.random.default_rng(seed).normal(0,0.04,n))
    return (v+br)*env_adsr(n,0.04,0.18)*0.55
def pluck(f,dur,decay=0.996,seed=0,bright=0.5):
    n=int((min(dur,1.5)+0.9)*R); P=max(2,int(R/f))
    buf=np.random.default_rng(seed).uniform(-1,1,P); buf=lfilter([bright,1-bright],[1],buf)
    out=np.zeros(n); b=buf.copy()
    # 벡터화된 KS
    reps=n//P+1; out_list=[]
    for i in range(reps):
        out_list.append(b.copy()); b=decay*0.5*(b+np.roll(b,-1))
    out=np.concatenate(out_list)[:n]
    return out*np.exp(-np.arange(n)/R*1.2)*0.6
def bell(f,dur,ice=False):
    n=int(2.4*R); t=np.arange(n)/R; a=np.clip(np.arange(n)/60,0,1)
    if ice: v=np.sin(2*np.pi*f*t)*np.exp(-t*1.4)+.5*np.sin(2*np.pi*f*1.003*t)*np.exp(-t*1.6)+.2*np.sin(2*np.pi*f*2.76*t)*np.exp(-t*4)+.08*np.sin(2*np.pi*f*5.4*t)*np.exp(-t*9)
    else: v=np.sin(2*np.pi*f*t)*np.exp(-t*2.2)+.3*np.sin(2*np.pi*f*3.01*t)*np.exp(-t*7)+.12*np.sin(2*np.pi*f*5.4*t)*np.exp(-t*16)
    return a*v*0.45
def drum(strength):
    n=int(0.7*R); t=np.arange(n)/R; f=60+55*np.exp(-t*18); ph=np.cumsum(2*np.pi*f/R)
    return strength*(np.sin(ph)*np.exp(-t*6)*0.9+np.random.default_rng(3).uniform(-1,1,n)*np.exp(-t*45)*0.2)
def lowpass(x,fc):
    return sosfilt(butter(2,fc,fs=R,output='sos'),x)
def reverb(x,decay=1.6,wet=0.22,seed=1):
    n=int(decay*R); t=np.arange(n)/R
    ir=np.random.default_rng(seed).normal(0,1,n)*np.exp(-t*6.9/decay); ir=lowpass(ir,5000); ir/=np.sqrt(np.sum(ir**2))
    return x*(1-wet)+fftconvolve(x,ir)[:len(x)]*wet*1.4
def render(slot, out):
    tempo={3:1.12}.get(slot,1.0)   # 밤은 조금 느리게
    st=S*tempo; L=int(BARS*16*st*R); tail=int(3*R)
    mel=np.zeros(L+tail); mid=np.zeros(L+tail); bas=np.zeros(L+tail); per=np.zeros(L+tail)
    for i,(k,d,p,v) in enumerate(notes):
        f=hz(p); dur=d*st; at=int(k*st*R); vel=(v/100)**1.2
        if p>=67:
            if slot==0: c=flute(f,dur,i)
            elif slot==1: c=bowed(f,dur,1.0,0.006,0.015,i)
            elif slot==2: c=bowed(f,dur,0.7,0.007,0.02,i)*0.9 + bowed(f*2.0,dur,0.5,0.007,0,i)*0.12   # 니켈하르파: 공명 현 반짝임
            else: c=bell(f*2 if f<520 else f,dur,ice=True)
            mel[at:at+len(c)]+=c[:len(mel)-at]*vel
        elif p>=52:
            if slot==3: c=bowed(f,dur,0.4,0.003,0,i)*0.35
            else: c=pluck(f,dur,0.997,i,0.5 if slot!=2 else 0.3)
            mid[at:at+len(c)]+=c[:len(mid)-at]*vel
        else:
            c=bowed(f,dur,0.5,0.003,0.01,i)*0.7 if slot in (1,2) else pluck(f,dur,0.998,i,0.3)
            bas[at:at+len(c)]+=c[:len(bas)-at]*vel
    if slot in (0,1):
        for b in range(BARS):
            for beat,sv in ((0,1.0),(2,0.55)) if slot==1 else ((0,0.5),):
                at=int((b*16+beat*4)*st*R); c=drum(sv); per[at:at+len(c)]+=c
    gains={0:(0.55,0.42,0.38,0.25),1:(0.5,0.42,0.42,0.35),2:(0.5,0.32,0.4,0),3:(0.6,0.3,0.25,0)}[slot]
    mix=mel*gains[0]+mid*gains[1]+bas*gains[2]+per*gains[3]
    mix=lowpass(mix,7000 if slot<2 else 5000)
    mix=reverb(mix,1.4 if slot<2 else 2.2,0.2 if slot<2 else 0.32)
    # 루프 이음: 꼬리를 앞으로 감기
    loop=mix[:L].copy(); loop[:tail]+=mix[L:L+tail]
    loop=loop/np.sqrt(np.mean(loop**2))*0.085; loop=np.clip(loop,-0.95,0.95)
    print("peak",round(float(np.max(np.abs(loop))),2))
    sf.write('/tmp/north_bgm_tmp.wav',loop,R)
    subprocess.run(['ffmpeg','-y','-loglevel','error','-i','/tmp/north_bgm_tmp.wav','-c:a','libvorbis','-q:a','4',out])
    print(out, round(L/R,2),'s')
if __name__=='__main__':
    import os; od=sys.argv[1]; os.makedirs(od,exist_ok=True)
    print('notes',len(notes))
    for s,nm in enumerate(['morning','day','evening','night']): render(s, f'{od}/north_bgm_{nm}.ogg')
