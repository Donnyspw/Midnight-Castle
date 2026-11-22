import sys, glob, os, json
import numpy as np
from PIL import Image
SRC=sys.argv[1]; OUT=sys.argv[2]
# target content height in px (PPU 64 in Unity)
TARGET={'player':92,'ghost':100,'bat':40}
# manual multipliers for poses whose bbox is not a full standing height
MULT={'player_run':1.0,'ghost_run':0.85,'player_hide':1.0}
def bbox(a):
    ys,xs=np.nonzero(a[:,:,3]>8); return xs.min(),ys.min(),xs.max()+1,ys.max()+1
meta={}
anims=sorted(d for d in os.listdir(SRC) if os.path.isdir(os.path.join(SRC,d)))
for an in anims:
    who=an.split('_')[0]
    fs=sorted(glob.glob(os.path.join(SRC,an,'*.png')))
    ims=[Image.open(f).convert('RGBA') for f in fs]
    arrs=[np.array(i) for i in ims]
    bbs=[bbox(a) for a in arrs]
    hs=[b[3]-b[1] for b in bbs]
    ref=hs[0] if an=='player_hide' else float(np.median(hs))
    if who=='bat': ref=float(np.median([b[2]-b[0] for b in bbs]))*144/243*1.0
    s=TARGET[who]/ref*MULT.get(an,1.0)
    if who=='bat': s=80/float(np.median([b[2]-b[0] for b in bbs]))
    crops=[]
    for im,b,a in zip(ims,bbs,arrs):
        c=im.crop(b)
        nw,nh=max(1,round(c.size[0]*s)),max(1,round(c.size[1]*s))
        c=c.resize((nw,nh),Image.LANCZOS)
        ca=np.array(c); 
        # head centre: opaque pixels in top 35%
        top=ca[:max(1,int(nh*0.35)),:,3]>40
        xs=np.nonzero(top.any(axis=0))[0]
        hc=(xs.min()+xs.max()+1)/2 if len(xs) else nw/2
        crops.append((c,hc))
    # canvas: wide enough for all frames centred on head
    left=max(hc for c,hc in crops); right=max(c.size[0]-hc for c,hc in crops)
    half=int(np.ceil(max(left,right)))+1
    W=half*2; H=max(c.size[1] for c,_ in crops)+1
    os.makedirs(os.path.join(OUT,an),exist_ok=True)
    for i,(c,hc) in enumerate(crops):
        cv=Image.new('RGBA',(W,H),(0,0,0,0))
        cv.alpha_composite(c,(int(round(half-hc)),H-c.size[1]))
        # clean faint alpha
        a=np.array(cv); a[a[:,:,3]<10]=0; cv=Image.fromarray(a)
        cv.save(os.path.join(OUT,an,f'{an}_{i+1:02d}.png'))
    meta[an]={'frames':len(crops),'w':W,'h':H,'scale':round(s,4),'srcH':hs}
    print(an,len(crops),W,H,round(s,3),'heights',min(hs),max(hs))
