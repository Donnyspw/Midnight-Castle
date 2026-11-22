import sys, os, wave
import numpy as np
from scipy.signal import fftconvolve, butter, lfilter
OUT=sys.argv[1]; SR=44100
rng=np.random.default_rng(3)
os.makedirs(OUT,exist_ok=True)
def t_(d): return np.arange(int(SR*d))/SR
def save(name,x,peak=0.9,stereo=False):
    x=np.asarray(x,float); m=np.max(np.abs(x))+1e-9; x=x/m*peak
    x=(x*32767).astype('<i2')
    with wave.open(os.path.join(OUT,name+'.wav'),'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(x.tobytes())
def lp(x,f,o=2): b,a=butter(o,f/(SR/2),'low'); return lfilter(b,a,x)
def hp(x,f,o=2): b,a=butter(o,f/(SR/2),'high'); return lfilter(b,a,x)
def bp(x,f1,f2,o=2): b,a=butter(o,[f1/(SR/2),f2/(SR/2)],'band'); return lfilter(b,a,x)
def env(n,a,d,s=0,r=0):
    e=np.ones(n); A=int(a*SR); 
    if A>0: e[:A]=np.linspace(0,1,A)
    e[A:]=np.exp(-np.arange(n-A)/(d*SR)) if d>0 else 1
    return e
def reverb(x,dur=2.5,mix=0.35,damp=3000):
    n=int(SR*dur); ir=rng.standard_normal(n)*np.exp(-np.arange(n)/(SR*dur/6)); ir=lp(ir,damp); ir/=np.sqrt(np.sum(ir**2))
    w=fftconvolve(x,ir)[:len(x)]; return x*(1-mix)+w*mix*2.2
def loop_reverb(x,dur=2.5,mix=0.35,damp=3000):
    # wrap tail around so loop is seamless
    n=int(SR*dur); ir=rng.standard_normal(n)*np.exp(-np.arange(n)/(SR*dur/6)); ir=lp(ir,damp); ir/=np.sqrt(np.sum(ir**2))
    w=fftconvolve(np.concatenate([x,x]),ir)[len(x):2*len(x)]; return x*(1-mix)+w*mix*2.2
def nf(m): return 440*2**((m-69)/12)
def mbox(f,d=1.6,amp=1.0):
    t=t_(d); e=np.exp(-t*3.2)*(1-np.exp(-t*400))
    return amp*e*(np.sin(2*np.pi*f*t)+0.35*np.sin(2*np.pi*f*2.01*t)*np.exp(-t*5)+0.15*np.sin(2*np.pi*f*4.2*t)*np.exp(-t*9))
def pad(f,d,bright=1200):
    t=t_(d); x=np.zeros_like(t)
    for det in (-0.12,0,0.11):
        ph=2*np.pi*f*(1+det/100)*t
        x+=2*(ph/(2*np.pi)%1)-1
    x=lp(x,bright); a=min(1.5,d/3); e=np.minimum(1,np.minimum(t/a,(d-t)/a)); return x*e
def place(buf,x,at):
    i=int(at*SR)
    if i>=len(buf): return
    j=min(len(buf),i+len(x)); buf[i:j]+=x[:j-i]
def wrap_place(buf,x,at):
    i=int(at*SR)%len(buf); n=len(x); k=0
    while k<n:
        j=min(len(buf)-i,n-k); buf[i:i+j]+=x[k:k+j]; k+=j; i=0
# ================= MUSIC =================
# --- menu: eerie music box in D minor, 8 bars of 3/4 at 84bpm... 
bpm=76; beat=60/bpm; L=beat*3*16
buf=np.zeros(int(L*SR))
mel=[(74,1),(77,1),(76,1),(74,2),(69,1),(70,1),(69,1),(67,1),(69,3),
     (74,1),(77,1),(81,1),(79,2),(77,1),(76,1),(74,1),(73,1),(74,3),
     (77,1),(76,1),(74,1),(72,2),(70,1),(69,1),(70,1),(72,1),(69,3),
     (74,1),(73,1),(74,1),(76,2),(73,1),(69,1),(73,1),(76,1),(74,3)]
tt=0
for m,dur in mel:
    place(buf,mbox(nf(m),2.2,0.55),tt); tt+=dur*beat
bass=[50,46,43,45, 50,46,41,45, 46,43,45,45, 50,45,43,50]
for i,m in enumerate(bass):
    for k in range(3): place(buf,mbox(nf(m),1.4,0.28 if k==0 else 0.12),(i*3+k)*beat)
buf=buf[:int(L*SR)]
drone=pad(nf(38),L,400)*0.18+pad(nf(45),L,500)*0.08
buf=loop_reverb(buf+drone,3.2,0.45,3500)
save('bgm_menu',buf,0.7)
# --- castle exploration ambient
L=64.0; buf=np.zeros(int(L*SR)); chords=[(38,50,53,57),(34,46,50,53),(31,43,46,50),(33,45,49,52)]
for i,ch in enumerate(chords):
    for m in ch:
        p=pad(nf(m),16.0+3,700 if m<45 else 1100)*(0.22 if m<45 else 0.07)
        wrap_place(buf,p,i*16.0-1.5)
wind=lp(rng.standard_normal(len(buf)),500)*(0.5+0.5*np.sin(2*np.pi*np.arange(len(buf))/SR/16.0))**2
buf+=wind*0.25
for at,m in ((6,74),(22,72),(38,77),(54,73),(30,69)):
    t=t_(5); b=np.exp(-t*0.9)*(np.sin(2*np.pi*nf(m)*t)+0.5*np.sin(2*np.pi*nf(m)*2.76*t)*np.exp(-t*2)+0.3*np.sin(2*np.pi*nf(m)*5.4*t)*np.exp(-t*4))
    wrap_place(buf,b*0.18,at)
# distant music-box fragments
for at,seq in ((10,[74,77,76]),(42,[69,70,69,67])):
    for k,m in enumerate(seq): wrap_place(buf,mbox(nf(m),2.0,0.12),at+k*0.8)
buf=loop_reverb(buf,4.0,0.5,2500)
save('bgm_castle',buf,0.6)
# --- chase loop: 132 bpm, 8 bars 4/4
bpm=132; beat=60/bpm; L=beat*32; buf=np.zeros(int(L*SR))
ost=[38,38,50,38,41,38,49,38]; roots=[0,0,-4,-4,-2,-2,-1,-1]
for bar in range(8):
    for k in range(8):
        m=ost[k]+roots[bar]; t=t_(beat/2*0.95); f=nf(m)
        x=(2*(f*t%1)-1)+0.6*(2*(f*1.005*t%1)-1); x=lp(x,900+600*(k%2))*np.exp(-t*6)
        place(buf,x*0.35,(bar*4+k*0.5)*beat)
    for k in (0,2.5):  # timpani-ish
        t=t_(0.6); f=55*np.exp(-t*2)+40; x=np.sin(2*np.pi*np.cumsum(f)/SR)*np.exp(-t*6)+lp(rng.standard_normal(len(t)),300)*np.exp(-t*20)*0.5
        place(buf,x*0.9,(bar*4+k)*beat)
    t=t_(beat*4); trem=(0.5+0.5*np.sign(np.sin(2*np.pi*t*beat**-1*4)))
    hi=np.sin(2*np.pi*nf(86+roots[bar])*t)+np.sin(2*np.pi*nf(87+roots[bar])*t)
    place(buf,hi*trem*0.05*np.minimum(1,t*4),bar*4*beat)
buf=loop_reverb(buf,1.5,0.25,4000); save('bgm_chase',buf,0.75)
# --- ending: gentle D major music box
bpm=70; beat=60/bpm; L=beat*31; buf=np.zeros(int(L*SR))
mel=[(74,1),(78,1),(81,2),(79,1),(78,1),(76,2),(74,1),(76,1),(78,1),(81,1),(79,4),(78,1),(76,1),(74,2),(73,1),(74,1),(76,2),(74,4)]
tt=0
for m,d in mel: place(buf,mbox(nf(m),2.5,0.5),tt); tt+=d*beat
for i,m in enumerate([50,47,43,45,50,45]):
    place(buf,pad(nf(m),beat*4+1,600)*0.12,i*4*beat)
buf=reverb(buf,3.0,0.45); save('bgm_ending',buf,0.7)
# ================= SFX =================
def noise(d): return rng.standard_normal(int(SR*d))
for i in range(3):
    t=t_(0.12); x=bp(noise(0.12),180+i*40,1800)*np.exp(-t*45)+np.sin(2*np.pi*(90+i*8)*t)*np.exp(-t*40)*0.6
    save(f'sfx_step_walk_{i}',x,0.35)
    t=t_(0.2); x=bp(noise(0.2),120+i*30,3500)*np.exp(-t*25)+np.sin(2*np.pi*(70+i*6)*t)*np.exp(-t*25)*1.2
    x+=hp(noise(0.2),2500)*np.exp(-((t-0.05)*30)**2)*0.4
    save(f'sfx_step_run_{i}',x,0.85)
t=t_(0.08); save('sfx_clock_tick',hp(noise(0.08),1500)*np.exp(-t*90)+np.sin(2*np.pi*2400*t)*np.exp(-t*120)*0.5,0.6)
save('sfx_clock_tock',hp(noise(0.08),900)*np.exp(-t*90)+np.sin(2*np.pi*1600*t)*np.exp(-t*120)*0.5,0.6)
# bell
t=t_(5.0); f0=nf(45); x=np.zeros_like(t)
for r,a,dc in ((0.5,0.6,0.5),(1,1,0.7),(1.19,0.5,1.0),(1.5,0.4,1.2),(2,0.5,1.5),(2.52,0.3,2.0),(3.0,0.25,2.6),(4.1,0.15,3.5)):
    x+=a*np.sin(2*np.pi*f0*r*t)*np.exp(-t*dc)
x+=lp(noise(5.0),2000)*np.exp(-t*30)*0.4
save('sfx_clock_bell',reverb(x,3,0.4),0.95)
# bats
d=3.0; t=t_(d); x=np.zeros_like(t)
for k in range(140):
    at=rng.uniform(0,d-0.1); tt2=t_(0.05); fl=bp(rng.standard_normal(len(tt2)),300,2500)*np.sin(np.pi*tt2/0.05)
    place(x,fl*rng.uniform(0.3,1),at)
for k in range(14):
    at=rng.uniform(0,d-0.2); tt2=t_(0.07); f=rng.uniform(3500,6000)
    ch=np.sin(2*np.pi*np.cumsum(f*(1-tt2*4))/SR)*np.sin(np.pi*tt2/0.07); place(x,ch*0.35,at)
x*=np.minimum(1,np.minimum(t/0.3,(d-t)/0.5)); save('sfx_bats',x,0.9)
# whoosh
t=t_(0.6); x=bp(noise(0.6),300,3000)*np.sin(np.pi*t/0.6)**2; save('sfx_whoosh',x,0.7)
# ghost alert shriek
t=t_(1.2); f=300+900*(1-np.exp(-t*6))+30*np.sin(2*np.pi*7*t)
x=np.sin(2*np.pi*np.cumsum(f)/SR)+0.4*np.sin(2*np.pi*np.cumsum(f*1.5)/SR)+bp(noise(1.2),800,4000)*0.5
x*=np.minimum(1,t*20)*np.exp(-t*2.2); save('sfx_ghost_alert',reverb(x,2,0.4),0.8)
# ghost moan (ambient)
t=t_(3.0); f=110+25*np.sin(2*np.pi*0.4*t)+8*np.sin(2*np.pi*5*t)
src=np.sin(2*np.pi*np.cumsum(f)/SR)
for k in range(2,8): src+=np.sin(2*np.pi*np.cumsum(f*k)/SR)/k
x=bp(src,250,900)+bp(noise(3.0),300,1200)*0.25; x*=np.sin(np.pi*t/3.0)**1.5
save('sfx_ghost_moan',reverb(x,2.5,0.5),0.6)
# pickup
x=np.zeros(int(SR*0.8))
for k,m in enumerate((79,83,86,91)): place(x,mbox(nf(m),0.6,0.6),k*0.06)
save('sfx_pickup',reverb(x,1.2,0.3),0.7)
# page / paper rustle
t=t_(0.45); x=np.zeros_like(t)
for k in range(9): place(x,hp(noise(0.05),2000)*np.hanning(int(SR*0.05))*rng.uniform(0.4,1),rng.uniform(0,0.38))
save('sfx_page',x,0.55)
# typing tick
t=t_(0.03); save('sfx_type',hp(noise(0.03),3000)*np.exp(-t*200),0.25)
# door unlock
x=np.zeros(int(SR*0.7)); t=t_(0.05)
place(x,hp(noise(0.05),2000)*np.exp(-t*150),0.0); place(x,hp(noise(0.05),1500)*np.exp(-t*120),0.12)
t=t_(0.4); place(x,(np.sin(2*np.pi*90*t)+lp(noise(0.4),600))*np.exp(-t*14),0.25); save('sfx_unlock',x,0.8)
# locked rattle
x=np.zeros(int(SR*0.5))
for k in range(5): t=t_(0.05); place(x,bp(noise(0.05),800,4000)*np.exp(-t*80),k*0.07)
save('sfx_locked',x,0.6)
# creak (door/wardrobe)
def creak(d,f0,f1):
    t=t_(d); f=np.linspace(f0,f1,len(t))+15*np.sin(2*np.pi*3*t)
    pulses=(np.sin(2*np.pi*np.cumsum(f)/SR)>0.97).astype(float)
    x=bp(pulses,300,3000)+0.3*bp(noise(d),500,1500); x*=np.sin(np.pi*t/d); return x
save('sfx_door_open',reverb(creak(1.0,40,22),1.5,0.3),0.7)
save('sfx_hide',creak(0.45,55,35),0.55); save('sfx_unhide',creak(0.35,35,55),0.5)
# keypad
t=t_(0.09); save('sfx_beep',np.sin(2*np.pi*1320*t)*np.minimum(1,np.minimum(t*400,(0.09-t)*200)),0.4)
t=t_(0.45); save('sfx_wrong',(2*((150*t)%1)-1)*np.minimum(1,(0.45-t)*20)*lp(np.ones_like(t),1000),0.5)
# solved chime
x=np.zeros(int(SR*2.0))
for k,m in enumerate((74,78,81,86)): place(x,mbox(nf(m),1.6,0.7),k*0.12)
save('sfx_solved',reverb(x,2,0.4),0.75)
# chest open
x=np.zeros(int(SR*2.2)); place(x,creak(0.7,30,18),0)
for k,m in enumerate((86,90,93,98,102)): place(x,mbox(nf(m),1.2,0.4),0.6+k*0.07)
save('sfx_chest_open',reverb(x,2,0.35),0.8)
# gate rumble
t=t_(3.0); x=lp(noise(3.0),180)*2+np.sin(2*np.pi*45*t)*0.6
ch=np.zeros_like(t)
for k in range(30): tt2=t_(0.04); place(ch,bp(rng.standard_normal(len(tt2)),1500,5000)*np.exp(-tt2*80),rng.uniform(0,2.8))
x=(x+ch*0.5)*np.minimum(1,np.minimum(t/0.2,(3-t)/0.6)); save('sfx_gate',reverb(x,2,0.3),0.85)
# heartbeat
x=np.zeros(int(SR*0.9))
for at,a in ((0,1),(0.2,0.7)):
    t=t_(0.18); place(x,np.sin(2*np.pi*(60*np.exp(-t*5)+30)*t)*np.exp(-t*22)*a,at)
save('sfx_heartbeat',lp(x,300),0.95)
# death stinger
t=t_(3.0); x=np.zeros_like(t)
for m in (62,63,68,69,74): x+=(2*((nf(m)*t)%1)-1)*0.3
x=lp(x,2500)*np.exp(-t*1.2)*np.minimum(1,t*30)
x+=np.sin(2*np.pi*np.cumsum(70*np.exp(-t*1.5)+25)/SR)*np.exp(-t*1.5)*1.5
x+=bp(noise(3.0),1000,6000)*np.exp(-t*4)*0.4
save('sfx_death',reverb(x,2.5,0.4),0.9)
# win fanfare
x=np.zeros(int(SR*3.5))
for k,(m,d) in enumerate(((62,0),(66,0.18),(69,0.36),(74,0.54))):
    place(x,mbox(nf(m),2.5,0.6)+mbox(nf(m+12),2.5,0.3),d)
place(x,pad(nf(50),3.0,900)*0.25,0.54); place(x,pad(nf(57),3.0,1200)*0.12,0.54)
save('sfx_win',reverb(x,2.5,0.4),0.8)
# UI
t=t_(0.06); save('sfx_ui_move',np.sin(2*np.pi*880*t)*np.exp(-t*60),0.35)
t=t_(0.18); save('sfx_ui_select',(np.sin(2*np.pi*660*t)+np.sin(2*np.pi*990*t*(t>0.06)))*np.exp(-t*18),0.45)
t=t_(0.12); save('sfx_ui_back',np.sin(2*np.pi*np.cumsum(np.linspace(700,350,len(t)))/SR)*np.exp(-t*20),0.4)
# midnight warning drone (rising tension, 12 s)
t=t_(12.0); f=55*(1+t/12*0.5); x=np.zeros_like(t)
for k in (1,1.5,2.02,3.01): x+=(2*((f*k*t)%1)-1)/k
x=(lp(x,350)*(1-t/12)+lp(x,1800)*(t/12))*(t/12)**1.5
save('sfx_midnight_rise',reverb(x,2,0.3),0.8)
print('audio done', len(os.listdir(OUT)))
