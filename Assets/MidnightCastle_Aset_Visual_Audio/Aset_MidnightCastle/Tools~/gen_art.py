import sys, os, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
OUT=sys.argv[1]
random.seed(7); np.random.seed(7)
def save(img,path):
    p=os.path.join(OUT,path); os.makedirs(os.path.dirname(p),exist_ok=True); img.save(p)
def new(w,h): return Image.new('RGBA',(w,h),(0,0,0,0))
def px(im,x,y,c):
    if 0<=x<im.size[0] and 0<=y<im.size[1]: im.putpixel((x,y),tuple(c)+((255,) if len(c)==3 else ()))
def rect(im,x0,y0,x1,y1,c):  # inclusive
    d=ImageDraw.Draw(im); d.rectangle([x0,y0,x1,y1],fill=tuple(c)+((255,) if len(c)==3 else ()))
def line(im,x0,y0,x1,y1,c):
    d=ImageDraw.Draw(im); d.line([x0,y0,x1,y1],fill=tuple(c)+((255,) if len(c)==3 else ()))
def jit(c,a=6):
    k=random.randint(-a,a); return tuple(max(0,min(255,v+k)) for v in c[:3])
def outline(im,col=(14,12,20)):
    a=np.array(im); al=a[:,:,3]>0
    o=np.zeros_like(al)
    o[1:,:]|=al[:-1,:]; o[:-1,:]|=al[1:,:]; o[:,1:]|=al[:,:-1]; o[:,:-1]|=al[:,1:]
    o&=~al; a[o]=list(col)+[255]; return Image.fromarray(a)
def shade_noise(im,amt=8):
    a=np.array(im).astype(int); n=np.random.randint(-amt,amt+1,a.shape[:2])
    for k in range(3): a[:,:,k]=np.clip(a[:,:,k]+n*(a[:,:,3]>0),0,255)
    return Image.fromarray(a.astype('uint8'))
T=32
# ---------------- FLOORS ----------------
def floor_stone(base,dark,light,mortar,var):
    im=new(T,T); rect(im,0,0,T-1,T-1,mortar)
    random.seed(var*13+1)
    # 2x2 slabs with offsets
    slabs=[(0,0,15,15),(16,0,31,15),(0,16,15,31),(16,16,31,31)]
    if var==1: slabs=[(0,0,19,13),(20,0,31,13),(0,14,11,31),(12,14,31,31)]
    if var==2: slabs=[(0,0,31,9),(0,10,13,31),(14,10,31,21),(14,22,31,31)]
    for (x0,y0,x1,y1) in slabs:
        c=jit(base,5); rect(im,x0+1,y0+1,x1-1,y1-1,c)
        line(im,x0+1,y0+1,x1-1,y0+1,light); line(im,x0+1,y0+1,x0+1,y1-1,light)
        line(im,x0+1,y1-1,x1-1,y1-1,dark); line(im,x1-1,y0+1,x1-1,y1-1,dark)
    for _ in range(6):
        px(im,random.randint(2,29),random.randint(2,29),dark)
    if var==2:  # crack
        x,y=8,20
        for i in range(7): px(im,x,y,mortar); x+=1; y+=random.choice([-1,0,1])
    return im
STONE=((58,60,78),(42,43,58),(76,78,98),(28,28,40))
for v in range(3): save(floor_stone(*STONE,v),f'Tiles/floor_stone_{v}.png')
DUN=((50,52,54),(36,38,40),(66,68,70),(24,25,27))
for v in range(3):
    im=floor_stone(*DUN,v)
    random.seed(99+v)
    for _ in range(10):  # moss
        x,y=random.randint(1,30),random.randint(1,30); px(im,x,y,(58,82,56)); px(im,x+1,y,(48,70,48))
    save(im,f'Tiles/floor_dungeon_{v}.png')
def floor_wood(v):
    im=new(T,T); random.seed(200+v)
    base=(86,58,40); dark=(58,38,28); light=(112,78,52)
    for i in range(4):
        y0=i*8; c=jit(base,6); rect(im,0,y0,T-1,y0+7,c)
        line(im,0,y0,T-1,y0,light); line(im,0,y0+7,T-1,y0+7,dark)
        cut=(i*11+v*7)%28+2; line(im,cut,y0,cut,y0+7,dark)
        for _ in range(5): px(im,random.randint(0,31),y0+random.randint(2,5),jit(dark,4))
    return im
for v in range(2): save(floor_wood(v),f'Tiles/floor_wood_{v}.png')
def carpet(edge):
    im=new(T,T); red=(112,26,38); dk=(84,18,30); gold=(190,148,70)
    rect(im,0,0,T-1,T-1,red)
    for y in range(0,T,4):
        for x in range((y//4)%2*2,T,4): px(im,x,y,dk)
    for x in range(2,T,8):
        for y in range(2,T,8): px(im,x+1,y+1,(140,44,52))
    if 'n' in edge: rect(im,0,0,T-1,2,dk); line(im,0,3,T-1,3,gold)
    if 's' in edge: rect(im,0,T-3,T-1,T-1,dk); line(im,0,T-4,T-1,T-4,gold)
    if 'w' in edge: rect(im,0,0,2,T-1,dk); line(im,3,0,3,T-1,gold)
    if 'e' in edge: rect(im,T-3,0,T-1,T-1,dk); line(im,T-4,0,T-4,T-1,gold)
    return im
save(carpet(''),'Tiles/floor_carpet.png')
# ---------------- WALLS ----------------
def wall_front(v):
    im=new(T,T); mortar=(34,30,44); random.seed(300+v)
    rect(im,0,0,T-1,T-1,mortar)
    for r in range(4):
        y0=r*8; off=0 if r%2==0 else 8
        for bx in range(-off,T,16):
            c=jit((74,66,86),6)
            rect(im,max(0,bx+1),y0+1,min(T-1,bx+14),y0+6,c)
            line(im,max(0,bx+1),y0+1,min(T-1,bx+14),y0+1,(96,88,110))
            line(im,max(0,bx+1),y0+6,min(T-1,bx+14),y0+6,(56,50,66))
    # bottom shadow
    rect(im,0,T-3,T-1,T-1,(26,22,32))
    if v==1:  # moss / crack
        for _ in range(8): px(im,random.randint(0,31),random.randint(16,28),(60,84,58))
    return im
for v in range(2): save(wall_front(v),f'Tiles/wall_front_{v}.png')
def wall_top():
    im=new(T,T); rect(im,0,0,T-1,T-1,(22,20,30))
    for _ in range(14): px(im,random.randint(0,31),random.randint(0,31),(30,27,40))
    return im
save(wall_top(),'Tiles/wall_top.png')
# window on wall (moonlight)
im=wall_front(0); rect(im,9,3,22,22,(20,18,28)); rect(im,11,5,20,20,(60,80,130))
line(im,15,5,15,20,(20,18,28)); line(im,11,12,20,12,(20,18,28)); px(im,18,7,(200,210,240)); px(im,17,7,(150,170,220))
rect(im,10,1,21,3,(20,18,28)); save(im,'Tiles/wall_window.png')
# banner on wall
im=wall_front(0); rect(im,10,0,21,22,(110,24,36)); rect(im,10,0,21,1,(190,148,70))
for x in range(10,22): 
    if x%2==0: px(im,x,23,(110,24,36))
d=ImageDraw.Draw(im); d.polygon([(13,8),(16,5),(19,8),(16,15)],fill=(30,26,36,255)); px(im,15,7,(190,148,70))
save(im,'Tiles/wall_banner.png')
# torch sconce on wall (animated flame drawn separately)
im=wall_front(0); rect(im,14,14,17,22,(70,50,34)); rect(im,13,12,18,14,(110,80,46)); save(im,'Tiles/wall_torch.png')
# ---------------- OBJECTS ----------------
def wardrobe(state):
    im=new(32,56); W=(92,58,38); D=(60,36,24); L=(128,86,54); G=(196,156,76)
    rect(im,2,4,29,55,D); rect(im,3,5,28,54,W)
    rect(im,1,1,30,5,L); rect(im,1,5,30,6,D)   # crown
    line(im,15,7,15,52,D); line(im,16,7,16,52,D)
    for x0 in (5,18):
        rect(im,x0,9,x0+8,28,jit(W,4)); line(im,x0,9,x0+8,9,L); line(im,x0,28,x0+8,28,D)
        rect(im,x0,32,x0+8,50,jit(W,4)); line(im,x0,32,x0+8,32,L); line(im,x0,50,x0+8,50,D)
    px(im,13,30,G); px(im,18,30,G); px(im,13,31,G); px(im,18,31,G)
    rect(im,3,53,6,55,D); rect(im,25,53,28,55,D)
    if state=='open':
        rect(im,16,8,27,51,(14,10,16)); rect(im,27,8,30,51,L)
    if state=='hidden':  # door ajar with eyes
        rect(im,15,10,17,48,(10,8,12)); px(im,16,22,(250,240,200)); px(im,16,25,(250,240,200))
    return outline(im)
for s in ('closed','open','hidden'): save(wardrobe(s),f'Objects/wardrobe_{s}.png')
def bookshelf(v):
    im=new(32,48); random.seed(400+v); W=(84,54,36); D=(54,34,24)
    rect(im,1,1,30,47,D); rect(im,2,2,29,46,W)
    cols=[(120,30,40),(40,70,110),(50,96,60),(150,120,60),(90,50,110),(140,80,40),(60,60,70)]
    for sy in (4,18,32):
        rect(im,3,sy,28,sy+11,(30,20,16)); x=3
        while x<28:
            w=random.randint(2,3); h=random.randint(8,11); c=random.choice(cols)
            if random.random()<0.12: x+=2; continue
            rect(im,x,sy+11-h+1,min(28,x+w-1),sy+11,c); px(im,x,sy+11-h+2,(220,200,140)); x+=w
        line(im,2,sy+12,29,sy+12,(110,74,48))
    return outline(im)
for v in range(3): save(bookshelf(v),f'Objects/bookshelf_{v}.png')
def table():
    im=new(32,32); W=(104,68,44); D=(64,40,28); L=(136,92,58)
    rect(im,1,6,30,20,W); line(im,1,6,30,6,L); rect(im,1,20,30,23,D)
    rect(im,3,23,5,30,D); rect(im,26,23,28,30,D)
    return outline(im)
save(table(),'Objects/table.png')
def table_candle():
    im=table(); rect(im,14,1,17,9,(230,220,190)); px(im,15,0,(255,200,80)); return im
def crate():
    im=new(32,32); W=(120,84,50); D=(76,50,30); L=(150,108,66)
    rect(im,2,4,29,30,W); rect(im,2,4,29,6,L)
    for k in (2,29): line(im,k,4,k,30,D)
    line(im,2,30,29,30,D); line(im,2,17,29,17,D)
    line(im,4,8,27,28,D); line(im,4,28,27,8,D)
    return outline(im)
save(crate(),'Objects/crate.png')
def barrel():
    im=new(32,36); W=(110,72,44); D=(70,44,28); M=(80,80,92)
    d=ImageDraw.Draw(im); d.ellipse([4,2,27,12],fill=(130,90,56,255)); rect(im,4,7,27,31,W)
    d.ellipse([4,26,27,35],fill=W+(255,))
    for y in (11,25): rect(im,4,y,27,y+1,M)
    for x in (9,15,21): line(im,x,10,x,32,D)
    d.ellipse([7,4,24,10],fill=(96,62,38,255))
    return outline(im)
save(barrel(),'Objects/barrel.png')
def curtain():
    im=new(32,56); R=(110,24,36); D=(74,14,24); L=(150,44,56); G=(196,156,76)
    rect(im,0,0,31,3,G)
    for x in range(0,32):
        c=[R,L,R,D][x%4]; line(im,x,4,x,52+(x%3),c)
    rect(im,0,0,31,1,(120,90,40))
    return outline(im)
save(curtain(),'Objects/curtain.png')
def candle_stand(frame):
    im=new(16,40); I=(60,60,72); L=(110,110,128)
    rect(im,7,12,8,36,I); rect(im,4,36,11,38,I); rect(im,5,10,10,12,L)
    rect(im,6,4,9,10,(236,226,196)); px(im,6,4,(255,255,240))
    fl=[[(7,0),(8,0),(7,1),(8,1),(7,2),(8,2),(7,3)],[(8,0),(7,1),(8,1),(7,2),(8,2),(8,3)],[(7,0),(7,1),(8,1),(7,2),(8,2),(7,3),(8,3)]][frame]
    for (x,y) in fl: px(im,x,y,(255,200,80))
    px(im,7,2,(255,250,200)) 
    return outline(im)
for f in range(3): save(candle_stand(f),f'Objects/candle_{f}.png')
def torch_flame(frame):
    im=new(12,16); random.seed(500+frame)
    pts=[(6,15),(2,10),(4,4),(6,0+frame),(8,5),(10,10)]
    d=ImageDraw.Draw(im); d.polygon(pts,fill=(240,120,40,255))
    d.polygon([(6,15),(4,10),(6,5+frame),(8,10)],fill=(255,210,90,255)); px(im,6,12,(255,250,220))
    return im
for f in range(3): save(torch_flame(f),f'FX/flame_{f}.png')
def gclock(frame):
    im=new(32,72); W=(80,48,32); D=(50,30,20); L=(120,80,50); G=(200,160,80)
    rect(im,6,2,25,70,D); rect(im,7,3,24,69,W)
    d=ImageDraw.Draw(im); d.polygon([(5,6),(16,0),(26,6)],fill=L+(255,))
    d.ellipse([8,8,23,23],fill=G+(255,)); d.ellipse([10,10,21,21],fill=(236,226,200,255))
    for a in range(12):
        ang=a*math.pi/6; px(im,int(15.5+5*math.sin(ang)),int(15.5-5*math.cos(ang)),(40,30,30))
    line(im,15,15,15,11,(20,10,10)); line(im,16,15,16,11,(20,10,10))  # near 12
    rect(im,11,28,20,58,(30,18,14)); 
    sw=[-3,0,3,0][frame]; line(im,15,28,15+sw,48,G); d.ellipse([12+sw,46,19+sw,53],fill=G+(255,))
    rect(im,6,66,25,70,D)
    return outline(im)
for f in range(4): save(gclock(f),f'Objects/clock_{f}.png')
def door(state):
    im=new(32,40); W=(96,60,38); D=(56,34,22); I=(70,70,84); G=(200,160,80)
    rect(im,1,0,30,39,(40,36,50)); 
    if state=='open':
        rect(im,4,3,27,39,(10,8,14)); rect(im,4,3,7,39,W); return outline(im)
    rect(im,4,3,27,39,W)
    for x in (9,15,21): line(im,x,3,x,39,D)
    for y in (10,30): rect(im,4,y,27,y+1,I)
    if state=='locked':
        rect(im,20,18,24,25,G); px(im,22,20,(20,10,10)); px(im,22,21,(20,10,10)); px(im,22,22,(20,10,10))
    if state=='code':
        rect(im,17,15,26,27,(40,40,52)); 
        for yy in (17,20,23):
            for xx in (19,21,23): px(im,xx,yy,(150,200,230))
        px(im,25,16,(220,60,60))
    return outline(im)
for s in ('locked','code','open'): save(door(s),f'Objects/door_{s}.png')
def stairs(state):
    im=new(32,32); rect(im,0,0,31,31,(20,18,26))
    for i in range(6):
        y=i*5+1; c=(70-i*8,68-i*8,86-i*8); rect(im,2+i,y,29-i,y+3,c); line(im,2+i,y,29-i,y,(100-i*8,98-i*8,116-i*8))
    if state=='locked':
        for x in range(2,30,5): line(im,x,0,x,31,(90,90,100))
        line(im,0,8,31,8,(90,90,100)); line(im,0,22,31,22,(90,90,100))
    return im
for s in ('locked','open'): save(stairs(s),f'Objects/stairs_{s}.png')
def gate(state):
    im=new(32,48); rect(im,0,0,31,47,(30,28,38)); rect(im,3,4,28,47,(8,6,12) if state=='closed' else (40,60,90))
    if state=='open':
        for y in range(4,47,3): line(im,3,y,28,y,(50,70,100) if y%2 else (60,84,120))
        for x in range(4,28,4): line(im,x,4,x,10,(110,110,124))
        return outline(im)
    for x in range(5,28,5): line(im,x,4,x,47,(110,110,124)); px(im,x,47,(150,150,166))
    for y in (12,26,40): line(im,3,y,28,y,(90,90,104))
    return outline(im)
for s in ('closed','open'): save(gate(s),f'Objects/gate_{s}.png')
def chest(state):
    im=new(32,28); W=(120,70,36); D=(76,42,22); G=(214,170,72); L=(156,96,50)
    if state=='closed':
        rect(im,2,8,29,26,W); rect(im,2,2,29,10,L); line(im,2,10,29,10,D)
        for x in (6,25): rect(im,x,2,x+1,26,G)
        rect(im,14,9,17,14,G); px(im,15,11,(30,16,8)); px(im,16,11,(30,16,8))
        # 4 sockets
        for i,x in enumerate((6,11,20,25)): pass
    else:
        rect(im,2,12,29,26,W); rect(im,2,0,29,6,L); rect(im,3,6,28,12,(20,12,8))
        for x in range(4,28,3): px(im,x,10,G); px(im,x+1,9,(255,230,140))
        rect(im,6,0,7,26,G); rect(im,24,0,25,26,G)
    return outline(im)
for s in ('closed','open'): save(chest(s),f'Objects/chest_{s}.png')
def pedestal():
    im=new(24,28); S=(90,90,104); D=(60,60,72)
    rect(im,4,0,19,4,(120,120,136)); rect(im,7,4,16,22,S); line(im,7,4,7,22,D); rect(im,3,22,20,27,D)
    return outline(im)
save(pedestal(),'Objects/pedestal.png')
def armor():
    im=new(24,48); M=(130,134,150); D=(80,84,100); L=(180,184,200)
    d=ImageDraw.Draw(im); d.ellipse([7,1,16,11],fill=M+(255,)); line(im,8,6,15,6,(20,20,30))
    rect(im,5,12,18,28,M); line(im,5,12,18,12,L); rect(im,3,13,4,26,D); rect(im,19,13,20,26,D)
    rect(im,7,29,10,42,D); rect(im,13,29,16,42,D); rect(im,4,43,19,47,(60,60,72)); line(im,21,4,21,40,(160,160,170))
    return outline(im)
save(armor(),'Objects/armor.png')
def skull_pile():
    im=new(32,20); d=ImageDraw.Draw(im)
    for (x,y) in ((4,8),(14,6),(22,9)):
        d.ellipse([x,y,x+8,y+8],fill=(220,214,196,255)); px(im,x+2,y+4,(30,26,24)); px(im,x+5,y+4,(30,26,24))
    line(im,2,17,29,15,(200,194,176)); 
    return outline(im)
save(skull_pile(),'Objects/bones.png')
def cobweb():
    im=new(32,32); c=(200,200,210,150)
    d=ImageDraw.Draw(im)
    for a in range(0,91,22):
        r=math.radians(a); d.line([0,0,int(30*math.cos(r)),int(30*math.sin(r))],fill=c)
    for rr in (8,15,23): d.arc([-rr,-rr,rr,rr],0,90,fill=c)
    return im
save(cobweb(),'Objects/cobweb.png')
def rug_round():
    im=new(64,40); d=ImageDraw.Draw(im)
    d.ellipse([0,0,63,39],fill=(90,24,34,255)); d.ellipse([4,3,59,36],outline=(190,148,70,255)); d.ellipse([16,10,47,29],outline=(150,110,60,255))
    return im
save(rug_round(),'Objects/rug.png')
def desk():
    im=new(32,36); W=(96,62,40); D=(60,38,26); L=(130,88,56)
    rect(im,1,10,30,24,W); line(im,1,10,30,10,L); rect(im,1,24,30,34,D); rect(im,4,26,13,32,W); px(im,8,29,(200,160,80))
    rect(im,17,26,27,32,W); px(im,22,29,(200,160,80))
    rect(im,6,4,13,10,(236,226,196)); line(im,7,6,12,6,(120,110,100)); line(im,7,8,11,8,(120,110,100))
    rect(im,20,2,22,9,(40,40,50)); 
    return outline(im)
save(desk(),'Objects/desk.png')
# ---------------- ITEMS (24x24 icons used in world & UI) ----------------
G=(214,170,72); GD=(150,110,40); GL=(255,230,140)
def item_key():
    im=new(24,24); d=ImageDraw.Draw(im)
    d.ellipse([2,2,11,11],fill=G+(255,)); d.ellipse([5,5,8,8],fill=(0,0,0,0))
    rect(im,10,6,21,8,G); rect(im,17,9,18,12,G); rect(im,20,9,21,13,G); line(im,10,6,21,6,GL)
    px(im,4,4,GL); return outline(im)
def item_lantern():  # senter (brass flashlight)
    im=new(24,24); B=(170,130,60); BD=(110,80,36)
    rect(im,3,9,15,15,B); line(im,3,9,15,9,GL); line(im,3,15,15,15,BD)
    rect(im,15,7,19,17,(190,150,70)); rect(im,19,6,20,18,BD)
    rect(im,6,7,8,8,(80,80,90)); 
    d=ImageDraw.Draw(im); d.polygon([(21,8),(23,5),(23,19),(21,16)],fill=(255,240,170,200))
    return outline(im)
def item_letter():
    im=new(24,24); P=(232,220,190); PD=(190,176,140)
    rect(im,2,5,21,19,P); d=ImageDraw.Draw(im); d.line([2,5,11,13,21,5],fill=PD+(255,)); line(im,2,19,21,19,PD)
    d.ellipse([9,10,14,15],fill=(170,30,36,255)); px(im,11,12,(220,80,80))
    return outline(im)
def item_book(col=(110,30,40)):
    im=new(24,24); D=tuple(int(c*0.65) for c in col)
    rect(im,4,2,19,21,col); rect(im,4,2,6,21,D); rect(im,19,3,20,21,(232,220,190))
    rect(im,9,7,16,10,G); px(im,12,14,GL); px(im,12,15,G)
    return outline(im)
def item_note():
    im=new(24,24); P=(224,210,170); PD=(170,150,110)
    rect(im,4,3,19,20,P); rect(im,3,2,20,4,PD); rect(im,3,19,20,21,PD)
    for y in (7,10,13,16): line(im,6,y,17-(y%3),y,(90,70,60))
    return outline(im)
def frag(sym):
    im=new(24,24); d=ImageDraw.Draw(im)
    S=(80,84,110); SL=(130,136,170)
    d.polygon([(4,3),(19,2),(22,12),(17,21),(5,20),(2,11)],fill=S+(255,))
    d.line([(4,3),(19,2)],fill=SL+(255,)); d.line([(4,3),(2,11)],fill=SL+(255,))
    c=GL
    if sym=='bulan':
        d.ellipse([7,6,16,15],fill=c+(255,)); d.ellipse([10,5,18,13],fill=S+(255,))
    elif sym=='mahkota':
        d.polygon([(6,15),(6,8),(9,11),(12,6),(15,11),(18,8),(18,15)],fill=c+(255,)); px(im,12,12,(200,40,50))
    elif sym=='mawar':
        d.ellipse([8,5,16,13],fill=(200,40,56,255)); d.arc([9,6,15,12],0,300,fill=(120,20,30,255)); line(im,12,13,12,18,(60,120,60)); px(im,11,15,(60,120,60)); px(im,13,16,(60,120,60))
    elif sym=='gagak':
        d.polygon([(5,13),(10,9),(14,9),(18,6),(16,11),(19,15),(13,13),(9,15)],fill=(24,22,30,255)); px(im,13,10,(230,60,60)); d.line([(5,13),(10,9)],fill=c+(255,))
    return outline(im)
save(item_key(),'Items/kunci.png'); save(item_lantern(),'Items/senter.png'); save(item_letter(),'Items/surat.png')
save(item_book(),'Items/buku_merah.png'); save(item_book((40,70,110)),'Items/buku_biru.png'); save(item_note(),'Items/catatan.png')
for s in ('bulan','mahkota','mawar','gagak'): save(frag(s),f'Items/bongkahan_{s}.png')
def medallion():
    im=new(32,32); d=ImageDraw.Draw(im); d.ellipse([1,1,30,30],fill=G+(255,)); d.ellipse([4,4,27,27],fill=GD+(255,))
    d.ellipse([8,8,23,23],fill=(30,20,40,255)); px(im,15,15,(230,60,60)); px(im,16,15,(230,60,60)); px(im,8,6,GL)
    return outline(im)
save(medallion(),'Items/medali.png')
# ---------------- FX ----------------
def radial(size,col,power=2.0,maxa=255):
    a=np.zeros((size,size,4),'uint8'); c=(size-1)/2
    yy,xx=np.mgrid[0:size,0:size]; r=np.sqrt((xx-c)**2+(yy-c)**2)/c; v=np.clip(1-r,0,1)**power
    a[:,:,0],a[:,:,1],a[:,:,2]=col; a[:,:,3]=(v*maxa).astype('uint8'); return Image.fromarray(a)
save(radial(64,(255,190,110),1.6,200),'FX/glow.png')
save(radial(32,(255,255,255),1.0,255),'FX/soft_dot.png')
for f in range(4):
    im=new(9,9); c=(255,240,180); r=[1,3,4,2][f]
    for i in range(-r,r+1): px(im,4+i,4,c); px(im,4,4+i,c)
    px(im,4,4,(255,255,255)); save(im,f'FX/sparkle_{f}.png')
im=new(32,32); d=ImageDraw.Draw(im); d.ellipse([1,1,30,30],outline=(255,255,255,255),width=2); save(im,'FX/ring.png')
def bubble(ch,col):
    im=new(14,16); rect(im,1,0,12,11,(250,246,230)); px(im,6,12,(250,246,230)); px(im,7,12,(250,246,230)); px(im,6,13,(250,246,230))
    if ch=='!': rect(im,6,2,7,6,col); rect(im,6,8,7,9,col)
    else:
        line(im,5,3,8,3,col); px(im,9,4,col); px(im,8,5,col); px(im,7,6,col); px(im,7,7,col); px(im,7,9,col)
    return outline(im)
save(bubble('!',(200,30,40)),'FX/alert.png'); save(bubble('?',(200,150,30)),'FX/question.png')
im=new(16,10); d=ImageDraw.Draw(im); d.ellipse([0,0,15,9],fill=(0,0,0,110)); save(im,'FX/shadow.png')
im=new(4,4); rect(im,0,0,3,3,(255,255,255)); save(im,'FX/white.png')
# vision cone texture: horizontal gradient strip (used as mesh UV)
a=np.zeros((8,64,4),'uint8'); a[:,:,:3]=255; a[:,:,3]=np.linspace(170,0,64).astype('uint8')[None,:]
save(Image.fromarray(a),'FX/cone.png')
# ---------------- UI ----------------
def panel_stone():
    im=new(48,48); rect(im,0,0,47,47,(12,10,18)); rect(im,2,2,45,45,(70,64,86)); rect(im,4,4,43,43,(22,20,32))
    line(im,2,2,45,2,(110,104,130)); line(im,2,2,2,45,(110,104,130)); line(im,3,45,45,45,(40,36,52)); line(im,45,3,45,45,(40,36,52))
    for (x,y) in ((3,3),(44,3),(3,44),(44,44)): rect(im,x-1,y-1,x+1,y+1,(196,156,76))
    return im
save(panel_stone(),'UI/panel_stone.png')
def parchment():
    im=new(48,48); P=(214,196,156); random.seed(9)
    rect(im,0,0,47,47,(84,60,36)); rect(im,2,2,45,45,(150,120,80)); rect(im,4,4,43,43,P)
    for _ in range(60): px(im,random.randint(5,42),random.randint(5,42),jit(P,10))
    return im
save(parchment(),'UI/panel_parchment.png')
def slot(sel):
    im=new(24,24); rect(im,0,0,23,23,(196,156,76) if sel else (12,10,18)); rect(im,2,2,21,21,(40,36,54) if not sel else (70,58,40)); rect(im,3,3,20,20,(26,24,36) if not sel else (52,42,30))
    return im
save(slot(False),'UI/slot.png'); save(slot(True),'UI/slot_sel.png')
im=new(16,16); rect(im,0,0,15,15,(20,18,26)); rect(im,1,1,14,13,(220,214,196)); rect(im,1,13,14,14,(140,134,120)); save(im,'UI/keycap.png')
def pocketwatch():
    im=new(24,24); d=ImageDraw.Draw(im); d.ellipse([2,4,21,23],fill=G+(255,)); d.ellipse([4,6,19,21],fill=(236,226,200,255))
    rect(im,10,0,13,4,G); line(im,11,13,11,8,(30,20,20)); line(im,11,13,15,13,(30,20,20)); return outline(im)
save(pocketwatch(),'UI/icon_clock.png')
def eye(open_):
    im=new(24,16); d=ImageDraw.Draw(im)
    if open_:
        d.ellipse([1,3,22,12],fill=(236,230,214,255)); d.ellipse([8,3,15,12],fill=(180,40,40,255)); d.ellipse([10,6,13,9],fill=(10,10,10,255))
    else:
        d.arc([1,-2,22,12],20,160,fill=(160,170,200,255),width=2)
        for x in (5,11,17): line(im,x,10,x-1,13,(160,170,200))
    return outline(im)
save(eye(True),'UI/icon_eye_open.png'); save(eye(False),'UI/icon_eye_closed.png')
def foot(loud):
    im=new(24,24); d=ImageDraw.Draw(im); c=(220,214,196,255)
    d.ellipse([5,8,11,18],fill=c); d.ellipse([13,4,19,14],fill=c)
    if loud:
        for r in (2,): d.arc([0,0,23,23],300,60,fill=(230,80,60,255),width=2); d.arc([0,0,23,23],120,240,fill=(230,80,60,255),width=2)
    return outline(im)
save(foot(False),'UI/icon_walk.png'); save(foot(True),'UI/icon_run.png')
im=new(8,8); rect(im,0,0,7,7,(255,255,255)); save(im,'UI/white.png')
# vignette 128
a=np.zeros((128,128,4),'uint8'); yy,xx=np.mgrid[0:128,0:128]; r=np.sqrt(((xx-63.5)/63.5)**2+((yy-63.5)/63.5)**2)
a[:,:,3]=(np.clip((r-0.55)/0.6,0,1)**1.5*255).astype('uint8'); save(Image.fromarray(a),'UI/vignette.png')
# moon
im=new(64,64); d=ImageDraw.Draw(im); d.ellipse([4,4,59,59],fill=(230,226,200,255))
for (x,y,r) in ((20,22,6),(38,36,9),(28,44,4),(44,18,4)): d.ellipse([x-r,y-r,x+r,y+r],fill=(200,196,172,255))
save(im,'UI/moon.png')
# title logo (pixel font look): render small then upscale
def title(text,fontpath,size,scale,col,shadow):
    f=ImageFont.truetype(fontpath,size); bb=f.getbbox(text); w,h=bb[2]-bb[0]+6,bb[3]-bb[1]+6
    im=Image.new('L',(w,h),0); ImageDraw.Draw(im).text((3-bb[0],3-bb[1]),text,font=f,fill=255)
    m=np.array(im)>110
    a=np.zeros((h,w,4),'uint8'); a[m]=list(col)+[255]
    # gradient
    for y in range(h):
        t=y/h; a[y,:,0]=np.where(m[y],int(col[0]*(1-0.35*t)),0); a[y,:,1]=np.where(m[y],int(col[1]*(1-0.45*t)),0); a[y,:,2]=np.where(m[y],int(col[2]*(1-0.3*t)),0)
    img=outline(Image.fromarray(a),(10,6,14))
    sh=new(img.size[0]+2,img.size[1]+2); 
    sa=np.array(img); sa2=np.zeros_like(sa); sa2[sa[:,:,3]>0]=list(shadow)+[255]
    sh.alpha_composite(Image.fromarray(sa2),(2,2)); sh.alpha_composite(img,(0,0))
    return sh.resize((sh.size[0]*scale,sh.size[1]*scale),Image.NEAREST)
FB='/usr/share/fonts/truetype/google-fonts/ofl/cardo/Cardo-Bold.ttf'
save(title('MIDNIGHT',FB,30,4,(236,214,150),(90,20,30)),'UI/title_midnight.png')
save(title('CASTLE',FB,30,4,(236,214,150),(90,20,30)),'UI/title_castle.png')
# castle silhouette for menu
W,H=320,120; im=new(W,H); d=ImageDraw.Draw(im); C=(14,12,22,255)
d.rectangle([0,90,W,H],fill=C)
for (x,w,h) in ((40,30,70),(90,50,50),(150,26,95),(190,60,60),(260,34,80)):
    d.rectangle([x,H-h,x+w,H],fill=C)
    for k in range(x,x+w,8): d.rectangle([k,H-h-6,k+4,H-h],fill=C)
d.polygon([(150,H-95),(163,H-118),(176,H-95)],fill=C)
for (x,y) in ((52,70),(162,50),(206,80),(272,62),(105,85)): d.rectangle([x,y,x+3,y+6],fill=(240,190,90,255))
save(im,'UI/castle_silhouette.png')
print('art done')
