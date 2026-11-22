# Daftar Aset Visual & Audio: Midnight Castle

**Cakupan:** tileset, benda, UI, dan efek visual bergaya pixel art (aset visual, sprite sheet, UI), serta musik latar dan efek suara (langkah, lari, detak jam, dentang jam).
**Gim:** Midnight Castle (Kelompok 7, Pengembangan GIM)

## Spesifikasi umum

- Gaya: pixel art, palet gelap kastil (abu-biru batu, merah tua, kayu cokelat, aksen emas).
- Ukuran dasar: petak 32×32 px, **Pixels Per Unit 32** (1 petak = 1 unit Unity).
- Import Unity: Sprite (Multiple), Filter **Point**, tanpa kompresi, sudah di-slice otomatis (file `.meta`).
- Pivot: tile, benda temuan, UI, dan efek di **tengah**; objek dunia di **bawah-tengah** (agar mudah diurutkan berdasarkan posisi Y).

## 1. Tileset: `Tileset/Tileset_Kastil.png` (256×128 px, 32 petak)

| Kelompok | Petak |
| --- | --- |
| Lantai | lantai_batu_0–2, lantai_kayu_0–1, lantai_bawahtanah_0–2, versi bayangan (batu, kayu, bawah tanah), genangan air |
| Karpet | tengah + 8 tepi/sudut (atas, bawah, kiri, kanan, 4 sudut) |
| Dinding | dinding_depan_0–1, jendela, panji, obor, dinding_atas + 4 tepi |
| Lain | tangga_turun |

- Tile asset: `Tileset/Tiles/` (dinding sudah memakai collider Grid).
- **Tile Palette:** `Tileset/Palette/Palette_Kastil.prefab`, dibuka lewat **Window → 2D → Tile Palette**.

## 2. Benda

### Objek dunia: `Benda/Objek_Kastil.png` (512×320 px, 32 sprite)

| Objek | Frame/varian | Keterangan |
| --- | --- | --- |
| Lemari | tertutup, terbuka, berisi (pemain bersembunyi) | Tempat sembunyi |
| Rak buku | 3 varian | Penghalang pandangan hantu |
| Tirai, peti kayu | 1 | Tempat sembunyi "di balik benda" |
| Baju zirah, meja, meja tulis, tong, tiang batu | 1 | Dekorasi/penghalang |
| Tulang, sarang laba-laba, karpet bundar | 1 | Dekorasi lantai/sudut |
| Lilin | 3 frame (animasi) | Sumber cahaya |
| Jam besar | 4 frame (bandul berayun) | Jam terkutuk |
| Pintu | terkunci, panel sandi, terbuka | Transisi level 1 dan 2 |
| Tangga | terkunci (jeruji), terbuka | Transisi level 3 |
| Gerbang | tertutup, terbuka | Pintu keluar akhir |
| Peti harta | tertutup, terbuka | Teka-teki akhir |

### Benda temuan: `Benda/Benda_Temuan.png` (192×64 px, 11 ikon 24×24)

senter, kunci_aula, surat, buku_merah, buku_biru, catatan, bongkahan_bulan, bongkahan_mahkota, bongkahan_mawar, bongkahan_gagak, medali_ravenhold.
Ikon yang sama dipakai di dunia gim dan di inventori.

## 3. UI

| File | Isi |
| --- | --- |
| `UI/UI_Kastil.png` (18 sprite) | panel_batu, panel_perkamen, tombol, tombol_dipilih, slot, slot_dipilih, tombol_keyboard, bar_bingkai, bar_isi (semua 9-slice), ikon_jam, ikon_mata_terbuka/tertutup, ikon_jalan, ikon_lari, ikon_hati, panah_pilih, bulan |
| `UI/UI_Judul_Midnight.png`, `UI/UI_Judul_Castle.png` | Logo judul untuk menu utama |
| `UI/UI_Siluet_Kastil.png` | Latar menu utama |
| `UI/UI_Vignette.png` | Bingkai gelap layar (filter halus) |

Sprite 9-slice sudah diberi border, jadi bisa diregangkan di komponen **Image** (Image Type: Sliced) tanpa merusak bingkai.

## 4. Efek visual

| File | Animasi | Frame | Klip animasi |
| --- | --- | --- | --- |
| `EfekVisual/FX_Efek_16px.png` | kilau (benda temuan) | 4 | Anim_Kilau |
| | api_obor | 3 | Anim_ApiObor |
| | debu_langkah (saat berlari) | 4 | Anim_DebuLangkah |
| | roh_asap (jejak hantu) | 4 | Anim_RohAsap |
| | tanda_seru, tanda_tanya (hantu waspada/curiga), bayangan | 1 | - |
| `EfekVisual/FX_LingkaranSuara_32px.png` | lingkaran suara langkah lari | 6 | Anim_LingkaranSuara |
| `EfekVisual/FX_GelombangDentang_48px.png` | gelombang dentang jam | 5 | Anim_GelombangDentang |
| `EfekVisual/FX_Cahaya.png` | cahaya, titik lembut, kerucut pandang (filter halus) | 1 | - |
| `Benda/Objek_Kastil.png` | lilin, bandul jam besar | 3 / 4 | Anim_Lilin, Anim_JamBesar_Bandul |

Klip animasi ada di `EfekVisual/Animasi/`. Cara pakai: seret klip ke objek yang punya **SpriteRenderer** di Scene.

## 5. Audio (WAV 44,1 kHz, mono)

### Musik latar: `Audio/Musik/`

| File | Durasi | Dipakai saat |
| --- | --- | --- |
| BGM_Menu | 37,9 s (loop) | Menu utama: kotak musik suram, D minor |
| BGM_Jelajah_Kastil | 64 s (loop) | Menjelajah kastil: dengung gelap + angin + lonceng jauh |
| BGM_Dikejar_Hantu | 14,6 s (loop) | Dikejar hantu: ostinato cepat 132 BPM |
| BGM_Ending | 26,6 s | Berhasil keluar |

### Efek suara utama

| File | Durasi | Keterangan |
| --- | --- | --- |
| SFX/Langkah/SFX_Langkah_01–03 | 0,12 s | Langkah pelan (3 variasi agar tidak monoton) |
| SFX/Lari/SFX_Lari_01–03 | 0,20 s | Langkah lari, lebih keras (memancing hantu) |
| SFX/Jam/SFX_Jam_Detak_Tik, _Tok | 0,08 s | Detak jam bergantian, makin cepat menjelang pukul 12 |
| SFX/Jam/SFX_Jam_Dentang | 5 s | Dentang jam besar (diputar 12 kali) |
| SFX/Jam/SFX_Jam_Peringatan_TengahMalam | 12 s | Dengung yang makin naik sebelum dentang |

### Efek suara tambahan: `Audio/SFX/Tambahan/` (22 file)

Kelelawar, kelelawar lewat, detak jantung, rintihan hantu, hantu melihat, ambil benda, kertas, masuk/keluar lemari, kunci terbuka, pintu terkunci, pintu berderit, peti terbuka, gerbang terbuka, teka-teki benar/salah, tombol sandi, tertangkap, menang, UI pindah/pilih/kembali.

**Cara pembuatan audio:** disintesis secara digital (prosedural) dengan Python/NumPy: osilator, noise yang difilter, envelope, dan reverb. Bebas hak cipta. Skrip pembuatnya ada di `Assets/Aset_MidnightCastle/Tools~/gen_audio.py` (gambar: `gen_art.py` dan `make_sheets.py`).

## 6. Scene Ruang Pamer (Showcase)

`Showcase/RuangPamer_Aset.unity`: scene siap tampil yang menerapkan semua aset di atas. Buka scene ini lalu tekan **Play**.

- **Tiga ruangan bersambung:** Aula (lantai batu + karpet), Perpustakaan (lantai kayu), Ruang Bawah Tanah (lantai lembap, genangan). Semua dilukis dengan Tile Palette.
- **Pencahayaan URP 2D:** malam kebiruan, obor dan lilin berkedip hangat, cahaya bulan dari jendela dan gerbang, senter pemain, aura roh hantu, kilau benda.
- **Kamera otomatis** menyusuri tiap ruangan dan menampilkan keterangan aset di panel perkamen.
- **Karakter contoh:** pemain berlari (suara langkah lari, debu, lingkaran suara) dan hantu berpatroli (asap roh, tanda "?").
- **UI diterapkan:** panel jam + timeline, panel tujuan, status, slot inventori berisi benda temuan, tombol keyboard, vignette, judul pembuka.
- **Siklus tengah malam (tiap ~30 detik):** detak jam makin cepat dan keras, lampu padam-nyala, lalu 12 dentang dengan kilatan layar, gelombang dentang, dan kawanan kelelawar.
- **Musik latar** BGM_Jelajah_Kastil diputar berulang.
- Gambar tiap ruangan otomatis disimpan ke `Dokumentasi~/Showcase_*.png` untuk laporan.
- Menu **Midnight Castle > Buat Ulang Scene Ruang Pamer** membuat ulang scene ini kapan saja.

Karakter (pemain, hantu, kelelawar) di scene ini dipakai sebagai pembanding skala.

## Dokumentasi

Folder `Dokumentasi~/` (tidak di-import Unity) berisi gambar pratinjau berlabel untuk laporan: tileset, objek, benda temuan, UI, efek visual, dan contoh penerapan di Level 1.
