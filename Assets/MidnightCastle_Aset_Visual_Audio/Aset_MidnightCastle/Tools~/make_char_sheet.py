import sys, glob
sys.argv=[sys.argv[0],'Aset_MidnightCastle']
exec(open('/home/claude/scripts/make_sheets.py').read().split("# ---------- 1. TILESET ----------")[0])
from PIL import Image
C='/home/claude/work/chars/'
items=[]
for src,dst in (('player_run','pemain_lari'),('ghost_walk','hantu_jalan'),('player_idle','pemain_diam'),('bat','kelelawar')):
    for i,f in enumerate(sorted(glob.glob(C+src+'/*.png'))):
        items.append((f'{dst}_{i}',Image.open(f).convert('RGBA'),'center' if src=='bat' else 'bottom'))
# shelf pack per baris
x=y=0; rowh=0; place=[]; width=1024
for n,im,pv in items:
    w,h=im.size
    if x+w>width: x=0; y+=rowh+2; rowh=0
    place.append((n,im,pv,x,y)); x+=w+2; rowh=max(rowh,h)
H=y+rowh
sheet=new(width,H); sprites=[]
for n,im,pv,x,y in place:
    sheet.alpha_composite(im,(x,y)); w,h=im.size
    sprites.append(dict(name=n,x=x,y=H-(y+h),w=w,h=h,pivot=pv,border=None))
write_sheet('Showcase/Karakter_Contoh.png',sheet,sprites,ppu=64,smooth=False)
