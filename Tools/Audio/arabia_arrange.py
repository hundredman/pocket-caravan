# 아라비아 시간대 배경 음악 편곡 (2026-11-07 기우): 북유럽과 같은 방식 — 원래 배경 음악(naru.ogg)의 음표(north_naru_notes.csv)를
# 아라비아 악기 느낌으로 다시 연주 → Resources/Arabia/Audio/arabia_bgm_*.wav. 귀엽게: 밝은 뜯는 현(카눈·우드)·작은 종(질)·가벼운 손북.
# 사용: python3 arabia_arrange.py <출력폴더>   (numpy만)
import os, csv, sys, wave, numpy as np
R=22050; S=0.1875; BARS=12
rows=[r for r in csv.reader(open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'north_naru_notes.csv')))][1:]
notes=[]
for r in rows:
    k=round(float(r[0])/S); d=max(1,round((float(r[1])-float(r[0]))/S)); p=int(r[2]); v=float(r[3])
    if k<0 or k>=BARS*16 or v<50: continue
    notes.append([k,d,p,v])
notes.sort(); clean=[]
for n in notes:
    if any(c[2]==n[2] and c[0]<=n[0]<c[0]+c[1] for c in clean[-12:]): continue
    clean.append(n)
notes=clean
def hz(p): return 440*2**((p-69)/12)
def env(n,a,rel,sus=1.0):
    A=max(1,int(a*R)); Rl=max(1,int(rel*R)); e=np.ones(n)*sus
    e[:A]=np.linspace(0,sus,min(A,n))[:min(A,n)] if A<=n else e[:A]
    if Rl<n: e[-Rl:]*=np.linspace(1,0,Rl)
    return e
def pluck(f,dur,decay=0.995,seed=0,bright=0.6,tail=0.4):
    rng=np.random.RandomState(seed); n=int((dur+tail)*R); N=max(2,int(R/f))
    buf=rng.uniform(-1,1,N); buf=buf-buf.mean()
    k=int(1+(1-bright)*6); buf=np.convolve(buf,np.ones(k)/k,mode='same')
    # 칼플러스-스트롱을 블록 단위로 (빠르게)
    out=np.zeros(n); b=buf.copy(); i=0
    while i<n:
        m=min(N,n-i); out[i:i+m]=b[:m]
        b=decay*0.5*(b+np.roll(b,-1)); i+=N
    return out
def qanun(f,dur,seed=0): return pluck(f,dur,0.996,seed,0.85)*0.7+pluck(f*1.004,dur,0.996,seed+7,0.85)*0.5
def oud(f,dur,seed=0): return pluck(f,dur,0.997,seed,0.45)
def ney(f,dur,seed=0):
    n=int((dur+0.15)*R); t=np.arange(n)/R; vib=1+0.006*np.sin(2*np.pi*5.2*t)
    s=np.sin(2*np.pi*f*vib*t)+0.3*np.sin(2*np.pi*2*f*vib*t)+0.08*np.sin(2*np.pi*3*f*vib*t)
    br=np.convolve(np.random.RandomState(seed).uniform(-1,1,n),np.ones(10)/10,mode='same')*0.05
    return (s+br)*env(n,0.06,0.15,0.8)
def zill(vol=1.0):
    n=int(0.5*R); t=np.arange(n)/R
    s=np.sin(2*np.pi*4200*t)*np.exp(-t*9)+0.6*np.sin(2*np.pi*6300*t)*np.exp(-t*12)+0.3*np.sin(2*np.pi*9100*t)*np.exp(-t*16)
    return s*vol*0.25
def dum(vol=1.0):
    n=int(0.2*R); t=np.arange(n)/R; f=110*np.exp(-t*20)+60
    return np.sin(2*np.pi*np.cumsum(f)/R)*np.exp(-t*16)*vol*0.7
def tek(vol=1.0):
    n=int(0.06*R); t=np.arange(n)/R; s=np.random.RandomState(3).uniform(-1,1,n)
    s=s-np.convolve(s,np.ones(5)/5,mode='same'); return s*np.exp(-t*70)*vol*0.35
def lowpass(x,fc):
    a=np.exp(-2*np.pi*fc/R); y=np.empty_like(x); z=0.0
    for i in range(len(x)): z=(1-a)*x[i]+a*z; y[i]=z
    return y
def reverb(x,decay=1.2,wet=0.18):
    out=x.copy()
    for d,g in ((0.031,0.5),(0.047,0.4),(0.071,0.33),(0.113,0.25)):
        k=int(d*R); y=np.zeros_like(x)
        for rep in range(1,5):
            gg=g**rep*(decay/1.2); kk=k*rep
            if kk<len(x): y[kk:]+=x[:-kk]*gg
        out+=y*wet
    return out
def add(buf,at,c):
    j=min(len(buf),at+len(c))
    if j>at: buf[at:j]+=c[:j-at]
def mixn(*parts):   # 길이 다른 소리 합치기
    n=max(len(x) for x,_ in parts); out=np.zeros(n)
    for x,g in parts: out[:len(x)]+=x*g
    return out
def render(slot,out):
    tempo={0:1.05,3:1.18}.get(slot,1.0); st=S*tempo; L=int(BARS*16*st*R); tail=int(2.5*R)
    mel=np.zeros(L+tail); mid=np.zeros(L+tail); bas=np.zeros(L+tail); per=np.zeros(L+tail)
    for i,(k,d,p,v) in enumerate(notes):
        f=hz(p); dur=d*st; at=int(k*st*R); vel=(v/100)**1.2
        if p>=67:
            if slot==0: c=mixn((ney(f,dur,i),0.8),(qanun(f*2,dur,i),0.25))
            elif slot==1: c=qanun(f,dur,i)
            elif slot==2: c=mixn((oud(f,dur,i),0.9),(ney(f,dur,i),0.35))
            else: c=mixn((oud(f,dur,i),0.6),(qanun(f*2,dur,i),0.15))
            add(mel,at,c*vel)
        elif p>=52:
            c=oud(f,dur,i) if slot!=1 else qanun(f,dur,i)*0.7
            add(mid,at,c*vel)
        else:
            add(bas,at,oud(f,dur,i)*vel)
    for b in range(BARS):
        for beat in range(4):
            at=int((b*16+beat*4)*st*R); half=int((b*16+beat*4+2)*st*R)
            if slot==1:
                add(per,at,dum(1.0) if beat in (0,2) else tek(0.8)); add(per,half,tek(0.6))
                if beat==3: add(per,half,zill(0.8))
            elif slot==0:
                if beat==0: add(per,at,zill(0.6))
                if beat==2: add(per,at,tek(0.4))
            elif slot==2:
                if beat==0: add(per,at,dum(0.7))
                if beat==2: add(per,at,zill(0.4))
    gains={0:(0.5,0.35,0.32,0.5),1:(0.55,0.35,0.4,0.4),2:(0.5,0.32,0.38,0.5),3:(0.5,0.28,0.28,0)}[slot]
    mix=mel*gains[0]+mid*gains[1]+bas*gains[2]+per*gains[3]
    mix=lowpass(mix,7500 if slot<2 else 5500)
    mix=reverb(mix,1.2 if slot<2 else 1.8,0.16 if slot<2 else 0.26)
    loop=mix[:L].copy(); loop[:tail]+=mix[L:L+tail]
    loop=loop/np.sqrt(np.mean(loop**2))*0.085; pk=float(np.max(np.abs(loop)));
    if pk>0.8: loop*=0.8/pk
    loop=np.clip(loop,-0.95,0.95)
    with wave.open(out,'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(R); w.writeframes((loop*32767).astype('<i2').tobytes())
    print(out,round(L/R,1),'s peak',round(float(np.max(np.abs(loop))),2),flush=True)
if __name__=='__main__':
    od=sys.argv[1]; os.makedirs(od,exist_ok=True); print('notes',len(notes),flush=True)
    for s,nm in enumerate(['morning','day','evening','night']): render(s,f'{od}/arabia_bgm_{nm}.wav')
