# Membuat sprite sheet (PNG) + file .meta Unity (slicing otomatis) untuk aset Midnight Castle.
import os, sys, json, uuid, random, math
import numpy as np
from PIL import Image, ImageDraw
ART='/home/claude/work/art/'; OUT=sys.argv[1]
random.seed(42)
def L(p): return Image.open(ART+p+'.png').convert('RGBA')
def new(w,h): return Image.new('RGBA',(w,h),(0,0,0,0))
def rect(im,x0,y0,x1,y1,c): ImageDraw.Draw(im).rectangle([x0,y0,x1,y1],fill=tuple(c)+((255,) if len(c)==3 else ()))
def line(im,x0,y0,x1,y1,c): ImageDraw.Draw(im).line([x0,y0,x1,y1],fill=tuple(c)+((255,) if len(c)==3 else ()))
def px(im,x,y,c):
    if 0<=x<im.size[0] and 0<=y<im.size[1]: im.putpixel((x,y),tuple(c)+((255,) if len(c)==3 else ()))
def outline(im,col=(14,12,20)):
    a=np.array(im); al=a[:,:,3]>0; o=np.zeros_like(al)
    o[1:,:]|=al[:-1,:]; o[:-1,:]|=al[1:,:]; o[:,1:]|=al[:,:-1]; o[:,:-1]|=al[:,1:]
    o&=~al; a[o]=list(col)+[255]; return Image.fromarray(a)
T=32
# ---------- tile tambahan ----------
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
def wall_top_edge(side):
    im=L('Tiles/wall_top'); rim=(52,46,66); hi=(70,64,86)
    if side=='s': rect(im,0,T-3,T-1,T-1,rim); line(im,0,T-3,T-1,T-3,hi)
    if side=='n': rect(im,0,0,T-1,2,rim); line(im,0,2,T-1,2,hi)
    if side=='w': rect(im,0,0,2,T-1,rim); line(im,2,0,2,T-1,hi)
    if side=='e': rect(im,T-3,0,T-1,T-1,rim); line(im,T-3,0,T-3,T-1,hi)
    return im
def shadowed(img):
    a=np.array(img).astype(float)
    for y in range(12):
        k=0.45*(1-y/12)
        a[y,:,:3]*= (1-k)
    return Image.fromarray(a.astype('uint8'))
def puddle():
    im=L('Tiles/floor_dungeon_0'); d=ImageDraw.Draw(im)
    d.ellipse([6,10,25,22],fill=(36,46,62,255)); d.ellipse([9,12,22,19],fill=(48,62,84,255)); px(im,12,13,(120,140,170)); px(im,13,13,(90,110,140))
    return im
# ---------- FX tambahan ----------
def ring_frames(n=6,S=32):
    fr=[]
    for i in range(n):
        im=new(S,S); d=ImageDraw.Draw(im); r=3+i*2.4; a=int(255*(1-i/n))
        d.ellipse([S/2-r,S/2-r*0.6,S/2+r,S/2+r*0.6],outline=(255,170,110,a),width=2 if i<3 else 1)
        fr.append(im)
    return fr
def dust_frames(n=4,S=16):
    fr=[]; rnd=random.Random(5)
    for i in range(n):
        im=new(S,S); d=ImageDraw.Draw(im)
        for k in range(4):
            x=4+k*2.5+i*0.5*(k-1.5); y=12-i*1.6-(k%2); r=1.5+i*0.8
            c=(190,180,170,int(220*(1-i/n)))
            d.ellipse([x-r,y-r,x+r,y+r],fill=c)
        fr.append(im)
    return fr
def bell_wave(n=5,S=48):
    fr=[]
    for i in range(n):
        im=new(S,S); d=ImageDraw.Draw(im); r=4+i*4.5; a=int(230*(1-i/n))
        d.ellipse([S/2-r,S/2-r,S/2+r,S/2+r],outline=(170,190,255,a),width=2)
        if r>8: d.ellipse([S/2-r+4,S/2-r+4,S/2+r-4,S/2+r-4],outline=(220,230,255,a//2),width=1)
        fr.append(im)
    return fr
def wisp_frames(n=4,S=16):
    fr=[]
    for i in range(n):
        im=new(S,S); d=ImageDraw.Draw(im); y=12-i*2.5; a=int(200*(1-i/(n+1)))
        d.ellipse([5+math.sin(i)*2,y-3,11+math.sin(i)*2,y+3],fill=(150,200,255,a)); px(im,8,int(y),(230,245,255))
        fr.append(im)
    return fr
# ---------- UI tambahan ----------
def button(sel):
    im=new(48,16); rect(im,0,0,47,15,(196,156,76) if sel else (12,10,18)); rect(im,1,1,46,14,(78,62,40) if sel else (52,48,66)); rect(im,2,2,45,13,(104,82,48) if sel else (34,31,46))
    line(im,2,2,45,2,(140,112,64) if sel else (70,64,86)); return im
def bar_frame():
    im=new(16,8); rect(im,0,0,15,7,(12,10,18)); rect(im,1,1,14,6,(70,64,86)); rect(im,2,2,13,5,(20,18,28)); return im
def bar_fill():
    im=new(8,4); rect(im,0,0,7,3,(214,170,72)); line(im,0,0,7,0,(255,230,140)); return im
def arrow():
    im=new(8,8); d=ImageDraw.Draw(im); d.polygon([(1,0),(6,4),(1,7)],fill=(240,205,110,255)); return outline(im)
def heart():
    im=new(12,11); d=ImageDraw.Draw(im); d.ellipse([0,0,6,6],fill=(200,40,50,255)); d.ellipse([5,0,11,6],fill=(200,40,50,255)); d.polygon([(0,4),(11,4),(6,10)],fill=(200,40,50,255)); px(im,3,2,(255,140,140)); return outline(im)

# ================= definisi sheet =================
def grid_sheet(items, cell_w, cell_h, cols, pivot='center', tight=False):
    rows=(len(items)+cols-1)//cols
    W,H=cols*cell_w, rows*cell_h
    sheet=new(W,H); sprites=[]
    for i,(name,im,border) in enumerate(items):
        cx,cy=(i%cols)*cell_w,(i//cols)*cell_h
        w,h=im.size
        if pivot=='bottom':
            ox=cx+(cell_w-w)//2; oy=cy+cell_h-h
        else:
            ox=cx+(cell_w-w)//2; oy=cy+(cell_h-h)//2
        sheet.alpha_composite(im,(ox,oy))
        if tight: rx,ry,rw,rh=ox,oy,w,h
        else: rx,ry,rw,rh=cx,cy,cell_w,cell_h
        # Unity: y dari bawah
        sprites.append(dict(name=name,x=rx,y=H-(ry+rh),w=rw,h=rh,pivot=pivot,border=border))
    return sheet,sprites
def shelf_pack(items, width, pad=2, pivot='center'):
    x=y=0; rowh=0; place=[]
    for name,im,border in items:
        w,h=im.size
        if x+w>width: x=0; y+=rowh+pad; rowh=0
        place.append((name,im,border,x,y)); x+=w+pad; rowh=max(rowh,h)
    H=y+rowh; H=int(2**math.ceil(math.log2(max(H,1)))) if H<=1024 else H
    sheet=new(width,H); sprites=[]
    for name,im,border,x,y in place:
        sheet.alpha_composite(im,(x,y)); w,h=im.size
        sprites.append(dict(name=name,x=x,y=H-(y+h),w=w,h=h,pivot=pivot,border=border))
    return sheet,sprites

META_HEAD='''fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{idtable}
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: {filter}
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites:
{sprites}
    outline: []
    customData: 
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{nametable}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
'''
SPR='''    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {w}
        height: {h}
      alignment: {align}
      pivot: {{x: {px}, y: {py}}}
      border: {{x: {bl}, y: {bb}, z: {br}, w: {bt}}}
      customData: 
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sid}
      internalID: {iid}
      vertices: []
      indices: 
      edges: []
      weights: []'''
INDEX={}
def write_sheet(path, sheet, sprites, ppu=32, smooth=False):
    full=os.path.join(OUT,path); os.makedirs(os.path.dirname(full),exist_ok=True)
    sheet.save(full)
    guid=uuid.uuid4().hex
    ids=[]; sp=[]
    for s in sprites:
        iid=random.randint(10**17, 9*10**18); ids.append((s['name'],iid))
        if s['pivot']=='bottom': align,pxv,pyv=7,0.5,0
        else: align,pxv,pyv=0,0.5,0.5
        b=s['border'] or (0,0,0,0)
        sp.append(SPR.format(name=s['name'],x=s['x'],y=s['y'],w=s['w'],h=s['h'],align=align,px=pxv,py=pyv,bl=b[0],bb=b[1],br=b[2],bt=b[3],sid=uuid.uuid4().hex,iid=iid))
    idtable="\n".join(f"  - first:\n      213: {iid}\n    second: {n}" for n,iid in ids)
    nametable="\n".join(f"      {n}: {iid}" for n,iid in ids)
    meta=META_HEAD.format(guid=guid,idtable=idtable,filter=1 if smooth else 0,ppu=ppu,sprites="\n".join(sp),nametable=nametable)
    open(full+'.meta','w').write(meta)
    INDEX[path]=dict(size=sheet.size,count=len(sprites),sprites=[(s['name'],s['w'],s['h']) for s in sprites],guid=guid)
    print(path, sheet.size, len(sprites))

# ---------- 1. TILESET ----------
tiles=[
 ('lantai_batu_0',L('Tiles/floor_stone_0')),('lantai_batu_1',L('Tiles/floor_stone_1')),('lantai_batu_2',L('Tiles/floor_stone_2')),
 ('lantai_kayu_0',L('Tiles/floor_wood_0')),('lantai_kayu_1',L('Tiles/floor_wood_1')),
 ('lantai_bawahtanah_0',L('Tiles/floor_dungeon_0')),('lantai_bawahtanah_1',L('Tiles/floor_dungeon_1')),('lantai_bawahtanah_2',L('Tiles/floor_dungeon_2')),
 ('karpet_tengah',carpet('')),('karpet_atas',carpet('n')),('karpet_bawah',carpet('s')),('karpet_kiri',carpet('w')),('karpet_kanan',carpet('e')),
 ('karpet_kiri_atas',carpet('nw')),('karpet_kanan_atas',carpet('ne')),('karpet_kiri_bawah',carpet('sw')),('karpet_kanan_bawah',carpet('se')),
 ('dinding_depan_0',L('Tiles/wall_front_0')),('dinding_depan_1',L('Tiles/wall_front_1')),('dinding_jendela',L('Tiles/wall_window')),
 ('dinding_panji',L('Tiles/wall_banner')),('dinding_obor',L('Tiles/wall_torch')),
 ('dinding_atas',L('Tiles/wall_top')),('dinding_atas_tepi_bawah',wall_top_edge('s')),('dinding_atas_tepi_atas',wall_top_edge('n')),
 ('dinding_atas_tepi_kiri',wall_top_edge('w')),('dinding_atas_tepi_kanan',wall_top_edge('e')),
 ('lantai_batu_bayangan',shadowed(L('Tiles/floor_stone_0'))),('lantai_kayu_bayangan',shadowed(L('Tiles/floor_wood_0'))),
 ('lantai_bawahtanah_bayangan',shadowed(L('Tiles/floor_dungeon_0'))),('lantai_bawahtanah_genangan',puddle()),
 ('tangga_turun',L('Objects/stairs_open')),
]
assert len(tiles)==32
s,sp=grid_sheet([(n,i,None) for n,i in tiles],32,32,8)
write_sheet('Tileset/Tileset_Kastil.png',s,sp)
TILE_NAMES=[n for n,_ in tiles]

# ---------- 2. OBJEK ----------
objs=[('lemari_tertutup','wardrobe_closed'),('lemari_terbuka','wardrobe_open'),('lemari_berisi','wardrobe_hidden'),
 ('rak_buku_0','bookshelf_0'),('rak_buku_1','bookshelf_1'),('rak_buku_2','bookshelf_2'),('tirai','curtain'),('baju_zirah','armor'),
 ('meja','table'),('meja_tulis','desk'),('peti_kayu','crate'),('tong','barrel'),('tiang_batu','pedestal'),('tulang','bones'),('sarang_laba','cobweb'),('karpet_bundar','rug'),
 ('lilin_0','candle_0'),('lilin_1','candle_1'),('lilin_2','candle_2'),('jam_besar_0','clock_0'),('jam_besar_1','clock_1'),('jam_besar_2','clock_2'),('jam_besar_3','clock_3'),('tangga_terkunci','stairs_locked'),
 ('pintu_terkunci','door_locked'),('pintu_sandi','door_code'),('pintu_terbuka','door_open'),('gerbang_tertutup','gate_closed'),('gerbang_terbuka','gate_open'),('peti_harta_tertutup','chest_closed'),('peti_harta_terbuka','chest_open'),('tangga_terbuka','stairs_open')]
s,sp=grid_sheet([(n,L('Objects/'+f),None) for n,f in objs],64,80,8,pivot='bottom',tight=True)
write_sheet('Benda/Objek_Kastil.png',s,sp)

# ---------- 3. BENDA TEMUAN ----------
items=[('senter','senter'),('kunci_aula','kunci'),('surat','surat'),('buku_merah','buku_merah'),('buku_biru','buku_biru'),('catatan','catatan'),
 ('bongkahan_bulan','bongkahan_bulan'),('bongkahan_mahkota','bongkahan_mahkota'),('bongkahan_mawar','bongkahan_mawar'),('bongkahan_gagak','bongkahan_gagak'),('medali_ravenhold','medali')]
s,sp=grid_sheet([(n,L('Items/'+f),None) for n,f in items],32,32,6)
write_sheet('Benda/Benda_Temuan.png',s,sp)

# ---------- 4. UI ----------
ui=[('panel_batu',L('UI/panel_stone'),(12,12,12,12)),('panel_perkamen',L('UI/panel_parchment'),(12,12,12,12)),('bulan',L('UI/moon'),None),
 ('tombol',button(False),(6,6,6,6)),('tombol_dipilih',button(True),(6,6,6,6)),
 ('slot',L('UI/slot'),(6,6,6,6)),('slot_dipilih',L('UI/slot_sel'),(6,6,6,6)),('ikon_jam',L('UI/icon_clock'),None),
 ('ikon_mata_terbuka',L('UI/icon_eye_open'),None),('ikon_mata_tertutup',L('UI/icon_eye_closed'),None),('ikon_jalan',L('UI/icon_walk'),None),('ikon_lari',L('UI/icon_run'),None),
 ('tombol_keyboard',L('UI/keycap'),(4,4,4,4)),('bar_bingkai',bar_frame(),(3,3,3,3)),('bar_isi',bar_fill(),(1,1,1,1)),('panah_pilih',arrow(),None),('ikon_hati',heart(),None),('putih',L('UI/white'),None)]
s,sp=shelf_pack(ui,256)
write_sheet('UI/UI_Kastil.png',s,sp)
for n,f in (('UI_Judul_Midnight','title_midnight'),('UI_Judul_Castle','title_castle'),('UI_Siluet_Kastil','castle_silhouette')):
    im=L('UI/'+f); write_sheet(f'UI/{n}.png',im,[dict(name=n,x=0,y=0,w=im.size[0],h=im.size[1],pivot='center',border=None)])
im=L('UI/vignette'); write_sheet('UI/UI_Vignette.png',im,[dict(name='vignette',x=0,y=0,w=128,h=128,pivot='center',border=None)],smooth=True)

# ---------- 5. EFEK VISUAL (animasi per baris) ----------
def frames(base,n,S):
    out=[]
    for i in range(n):
        im=L(base+f'_{i}'); c=new(S,S); c.alpha_composite(im,((S-im.size[0])//2,(S-im.size[1])//2)); out.append(c)
    return out
rows=[('kilau',frames('FX/sparkle',4,16),16),('api_obor',frames('FX/flame',3,16),16),('debu_langkah',dust_frames(),16),('roh_asap',wisp_frames(),16)]
fx_items=[]
# susun sheet FX: tiap baris 1 animasi, sel 16x16 (lingkaran 32, gelombang 48 di sheet sendiri)
W=8*16; Hs=len(rows)*16; sheet=new(W,Hs+16*2); sps=[]
y=0
for name,fr,S in rows:
    for i,im in enumerate(fr):
        sheet.alpha_composite(im,(i*16,y)); sps.append(dict(name=f'{name}_{i}',x=i*16,y=sheet.size[1]-(y+16),w=16,h=16,pivot='center',border=None))
    y+=16
for i,(n,f) in enumerate((('tanda_seru','alert'),('tanda_tanya','question'),('bayangan','shadow'))):
    im=L('FX/'+f); c=new(16,16); c.alpha_composite(im,((16-im.size[0])//2,(16-im.size[1])//2))
    sheet.alpha_composite(c,(i*16,y)); sps.append(dict(name=n,x=i*16,y=sheet.size[1]-(y+16),w=16,h=16,pivot='center',border=None))
write_sheet('EfekVisual/FX_Efek_16px.png',sheet,sps)
s,sp=grid_sheet([(f'lingkaran_suara_{i}',im,None) for i,im in enumerate(ring_frames())],32,32,6)
write_sheet('EfekVisual/FX_LingkaranSuara_32px.png',s,sp)
s,sp=grid_sheet([(f'gelombang_dentang_{i}',im,None) for i,im in enumerate(bell_wave())],48,48,5)
write_sheet('EfekVisual/FX_GelombangDentang_48px.png',s,sp)
glow=L('FX/glow'); dot=L('FX/soft_dot'); cone=L('FX/cone')
sh=new(128,64); sh.alpha_composite(glow,(0,0)); sh.alpha_composite(dot,(64,0)); sh.alpha_composite(cone,(64,40))
write_sheet('EfekVisual/FX_Cahaya.png',sh,[dict(name='cahaya',x=0,y=0,w=64,h=64,pivot='center',border=None),dict(name='titik_lembut',x=64,y=32,w=32,h=32,pivot='center',border=None),dict(name='kerucut_pandang',x=64,y=16,w=64,h=8,pivot='center',border=None)],smooth=True)
json.dump({k:dict(v,size=list(v['size'])) for k,v in INDEX.items()},open('index.json','w'),indent=1)
