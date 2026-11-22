using System.Collections.Generic;
using System.IO;
using System.Linq;
using MidnightCastleAset.Showcase;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace MidnightCastleAset
{
    // Membangun scene "Ruang Pamer" yang menampilkan semua aset visual & audio Midnight Castle:
    // tileset, objek, benda temuan, UI, efek visual, musik, dan efek suara.
    [InitializeOnLoad]
    public static class ShowcaseBuilder
    {
        const string Root = "Assets/Aset_MidnightCastle";
        const string Dir = Root + "/Showcase";
        public const string ScenePath = Dir + "/RuangPamer_Aset.unity";
        const string CtrlDir = Dir + "/Animator";
        const string MatPath = Dir + "/Material_SpriteUnlit.mat";
        const string AnimDir = Root + "/EfekVisual/Animasi";
        const string DocDir = Root + "/Dokumentasi~";

        static readonly string[] MAP =
        {
            "#t#w#n##t##n#w#t##t###n#K#n###t##t######S######t#",
            "#L.A....3....A.L#B.B.B.B.B.B.B.B#x...b.....b...x#",
            "#...............#...............#..~....p.......#",
            "#..c..,,,,,..c..#.BBB..d4..BBB..#..7....X....8..#",
            "#T....,,,,,....T#...............#...............#",
            "#1....,,,,,....k#.BBB.......BBB.#.C..c.....c..C.#",
            "#.....,,,,,......................~.............~#",
            "#H....,,,,,....C..BBB.......BBB.................#",
            "#.....,,,,,.....#...............#..9....~....0..#",
            "#..c..,,,,,..c..#H..T.5...T...H.#b......x......b#",
            "#b..............#...............#...C.......L...#",
            "#L..2.......r..L#L.c....6....c.L#x..L...c.......#",
            "########G###############################O########"
        };

        static Dictionary<string, Sprite> sp;
        static Material unlit;
        static int W, H;

        static ShowcaseBuilder()
        {
            EditorApplication.delayCall += AutoBuild;
        }

        static void AutoBuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(ScenePath)) return;
            if (!File.Exists(Root + "/Tileset/Tileset_Kastil.png")) return;
            if (AssetDatabase.LoadAllAssetsAtPath(Root + "/Tileset/Tileset_Kastil.png").OfType<Sprite>().Count() == 0) return;
            if (!AssetDatabase.IsValidFolder(Root + "/Tileset/Tiles") || !AssetDatabase.IsValidFolder(AnimDir)) AsetTools.BuatSemua();
            Build();
        }

        [MenuItem("Midnight Castle/Buat Ulang Scene Ruang Pamer (Showcase)", false, 0)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            LoadSprites();
            EnsureMaterial();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            H = MAP.Length; W = MAP[0].Length;

            // ---------- kamera & cahaya global ----------
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6.9f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.015f, 0.04f);
            camGo.transform.position = new Vector3(8.5f, 6.5f, -10f);
            camGo.AddComponent<AudioListener>();

            var glGo = new GameObject("Cahaya Global (malam)");
            var gl = glGo.AddComponent<Light2D>();
            gl.lightType = Light2D.LightType.Global;
            gl.color = new Color(0.58f, 0.64f, 1f);
            gl.intensity = 0.3f;

            // ---------- tilemap ----------
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();
            var floor = NewTilemap(gridGo, "Lantai", -200);
            var walls = NewTilemap(gridGo, "Dinding", -150);

            var objRoot = new GameObject("Objek").transform;
            var itemRoot = new GameObject("Benda Temuan").transform;
            var lightRoot = new GameObject("Cahaya").transform;
            var fxRoot = new GameObject("Efek Visual").transform;
            GameObject bellWave = null;

            for (int r = 0; r < H; r++)
            {
                for (int x = 0; x < W; x++)
                {
                    char c = MAP[r][x];
                    int y = H - 1 - r;
                    var cell = new Vector3Int(x, y, 0);
                    Vector2 bottom = new Vector2(x + 0.5f, y);
                    Vector2 center = new Vector2(x + 0.5f, y + 0.5f);

                    if (IsWall(c) || IsDoor(c))
                    {
                        bool front = r + 1 < H && !IsWallLike(At(x, r + 1));
                        if (IsDoor(c))
                        {
                            walls.SetTile(cell, T("dinding_atas"));
                            if (c == 'S') { walls.SetTile(cell, T("tangga_turun")); AddLight(lightRoot, "cahaya tangga", center, new Color(1f, 0.8f, 0.5f), 0.6f, 2.5f, 0.05f, true); }
                            else
                            {
                                string o = c == 'K' ? "pintu_sandi" : c == 'D' ? "pintu_terkunci" : c == 'G' ? "gerbang_tertutup" : "gerbang_terbuka";
                                Obj(objRoot, o, bottom, Order(y) + 5);
                                if (c == 'O') AddLight(lightRoot, "cahaya bulan (gerbang)", new Vector2(x + 0.5f, y + 1.2f), new Color(0.5f, 0.6f, 1f), 1.0f, 3.5f, 0.02f, false);
                            }
                            continue;
                        }
                        if (front)
                        {
                            string t = c == 't' ? "dinding_obor" : c == 'n' ? "dinding_panji" : c == 'w' ? "dinding_jendela" : ((x * 7 + r * 3) % 5 == 0 ? "dinding_depan_1" : "dinding_depan_0");
                            walls.SetTile(cell, T(t));
                            if (c == 't')
                            {
                                var flame = Obj(fxRoot, "api_obor_0", new Vector2(x + 0.5f, y + 0.62f), Order(y) + 1, true);
                                Animate(flame.gameObject, "Anim_ApiObor", 0f);
                                AddLight(lightRoot, "cahaya obor", new Vector2(x + 0.5f, y + 0.4f), new Color(1f, 0.62f, 0.3f), 1.25f, 4.6f, 0.14f, true);
                            }
                            if (c == 'w') AddLight(lightRoot, "cahaya jendela", new Vector2(x + 0.5f, y - 0.5f), new Color(0.45f, 0.55f, 1f), 0.75f, 3.4f, 0.02f, false);
                        }
                        else
                        {
                            string t = "dinding_atas";
                            if (r > 0 && !IsWallLike(At(x, r - 1))) t = "dinding_atas_tepi_atas";
                            else if (!IsWallLike(At(x + 1, r))) t = "dinding_atas_tepi_kanan";
                            else if (!IsWallLike(At(x - 1, r))) t = "dinding_atas_tepi_kiri";
                            walls.SetTile(cell, T(t));
                        }
                        continue;
                    }

                    // ---------- lantai ----------
                    string reg = x < 16 ? "batu" : x < 32 ? "kayu" : "bawahtanah";
                    if (c == ',') floor.SetTile(cell, T(CarpetTile(x, r)));
                    else
                    {
                        bool shadow = r > 0 && IsWallLike(At(x, r - 1)) && At(x, r - 1) != 'S';
                        string t;
                        if (c == '~') t = "lantai_bawahtanah_genangan";
                        else if (shadow) t = "lantai_" + reg + "_bayangan";
                        else
                        {
                            int v = (x * 31 + r * 17) % 10;
                            int n = reg == "kayu" ? 2 : 3;
                            t = "lantai_" + reg + "_" + (v < 6 ? 0 : (v % n));
                        }
                        floor.SetTile(cell, T(t));
                    }

                    // ---------- objek ----------
                    switch (c)
                    {
                        case 'L': Obj(objRoot, "lemari_tertutup", bottom, Order(y)); break;
                        case 'B': Obj(objRoot, "rak_buku_" + ((x + r) % 3), bottom, Order(y)); break;
                        case 'T': Obj(objRoot, "meja", bottom, Order(y)); break;
                        case 'd': Obj(objRoot, "meja_tulis", bottom, Order(y)); break;
                        case 'A': Obj(objRoot, "baju_zirah", bottom, Order(y)); break;
                        case 'b': Obj(objRoot, "tong", bottom, Order(y)); break;
                        case 'C': Obj(objRoot, "peti_kayu", bottom, Order(y)); break;
                        case 'H': Obj(objRoot, "tirai", bottom, Order(y)); break;
                        case 'p': Obj(objRoot, "tiang_batu", bottom, Order(y)); break;
                        case 'x': Obj(objRoot, "tulang", bottom + new Vector2(0f, 0.15f), -100); break;
                        case 'r': Obj(objRoot, "karpet_bundar", bottom + new Vector2(0.5f, 0.1f), -100); break;
                        case 'c':
                            {
                                var cnd = Obj(objRoot, "lilin_0", bottom, Order(y));
                                Animate(cnd.gameObject, "Anim_Lilin", 0f);
                                AddLight(lightRoot, "cahaya lilin", new Vector2(x + 0.5f, y + 1.15f), new Color(1f, 0.72f, 0.42f), 1.0f, 3.2f, 0.12f, true);
                                break;
                            }
                        case 'k':
                            {
                                var clk = Obj(objRoot, "jam_besar_0", bottom, Order(y));
                                Animate(clk.gameObject, "Anim_JamBesar_Bandul", 0f);
                                var wave = Obj(fxRoot, "gelombang_dentang_0", new Vector2(x + 0.5f, y + 1.8f), Order(y) + 2, true);
                                wave.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                                Animate(wave.gameObject, "Anim_GelombangDentang", 0.2f);
                                wave.gameObject.name = "Gelombang Dentang (aktif saat tengah malam)";
                                bellWave = wave.gameObject;
                                break;
                            }
                        case 'X':
                            {
                                Obj(objRoot, "peti_harta_terbuka", bottom + new Vector2(0f, 0.1f), Order(y));
                                Item(itemRoot, lightRoot, "medali_ravenhold", new Vector2(x + 0.5f, y + 1.1f), Order(y) + 3, 2.8f, 1.3f);
                                break;
                            }
                    }
                    string item = ItemFor(c);
                    if (item != null) Item(itemRoot, lightRoot, item, center, Order(y + 0.2f), 1.5f, 0.85f);
                }
            }
            AddCobwebs(objRoot);

            // ---------- audio ----------
            var audioGo = new GameObject("Audio");
            var music = audioGo.AddComponent<AudioSource>();
            music.clip = Clip("Musik/BGM_Jelajah_Kastil");
            music.loop = true; music.playOnAwake = true; music.volume = 0.55f;
            var sfx = audioGo.AddComponent<AudioSource>();
            sfx.playOnAwake = false;

            // ---------- karakter contoh ----------
            var charRoot = new GameObject("Karakter (contoh skala)").transform;
            var player = Character(charRoot, "Pemain (berlari)", "pemain_lari_", false, new Vector2(3.5f, 2.08f), new Vector2(13.5f, 2.08f), 4.2f, 14f, 0f, true);
            AddLight(player.transform, "senter pemain", new Vector2(3.5f, 2.7f), new Color(1f, 0.82f, 0.55f), 1.15f, 4.2f, 0.04f, false);
            var pp = player.GetComponent<Patrol>();
            pp.audioSrc = player.AddComponent<AudioSource>();
            pp.stepClips = new[] { Clip("SFX/Lari/SFX_Lari_01"), Clip("SFX/Lari/SFX_Lari_02"), Clip("SFX/Lari/SFX_Lari_03") };
            pp.stepVolume = 0.55f; pp.stepInterval = 0.27f;
            pp.stepFx = Frames("debu_langkah_", 4); pp.stepFxColor = new Color(1f, 1f, 1f, 0.85f);
            pp.stepFx2 = Frames("lingkaran_suara_", 6); pp.stepFx2Color = new Color(1f, 0.7f, 0.5f, 0.9f);
            pp.fxMaterial = unlit;

            var ghost = Character(charRoot, "Hantu Penjaga", "hantu_jalan_", true, new Vector2(17.5f, 4.1f), new Vector2(30.5f, 4.1f), 1.6f, 9f, 0.08f, false);
            var gp = ghost.GetComponent<Patrol>();
            gp.sr.sharedMaterial = unlit;
            gp.sr.color = new Color(0.85f, 0.95f, 1f, 0.9f);
            gp.stepFx = Frames("roh_asap_", 4); gp.stepFxColor = new Color(0.8f, 0.9f, 1f, 0.8f);
            gp.stepInterval = 0.35f; gp.fxMaterial = unlit;
            AddLight(ghost.transform, "cahaya roh", new Vector2(17.5f, 4.9f), new Color(0.5f, 0.8f, 1f), 1.0f, 3.0f, 0.1f, false);
            var bubble = Obj(ghost.transform, "tanda_tanya", new Vector2(17.5f, 6.25f), 1500, true);
            bubble.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            bubble.gameObject.AddComponent<Bob2D>().amp = 0.08f;

            // ---------- UI ----------
            var ui = BuildUI();

            // ---------- sutradara ----------
            var dirGo = new GameObject("Showcase Director");
            var d = dirGo.AddComponent<ShowcaseDirector>();
            d.cam = cam;
            d.stops = new[] { new Vector2(8.5f, 6.5f), new Vector2(24.5f, 6.5f), new Vector2(40.5f, 6.5f) };
            d.captions = new[]
            {
                "AULA  ·  Lantai batu, karpet bertepi emas, dinding dengan jendela, panji & obor, lemari, baju zirah, jam besar",
                "PERPUSTAKAAN  ·  Lantai kayu, rak buku, tirai, meja tulis, lilin  ·  hantu dengan efek asap roh",
                "RUANG BAWAH TANAH  ·  Genangan, tulang, tong, peti kayu  ·  bongkahan medali & peti harta",
            };
            d.titleGroup = ui.title; d.captionGroup = ui.captionGroup; d.captionText = ui.caption;
            d.clockText = ui.clock; d.clockLabel = ui.clockLabel; d.barFill = ui.bar; d.flash = ui.flash;
            d.globalLight = gl;
            d.sfx = sfx;
            d.tick = Clip("SFX/Jam/SFX_Jam_Detak_Tik"); d.tock = Clip("SFX/Jam/SFX_Jam_Detak_Tok");
            d.bell = Clip("SFX/Jam/SFX_Jam_Dentang"); d.warning = Clip("SFX/Jam/SFX_Jam_Peringatan_TengahMalam");
            d.batsClip = Clip("SFX/Tambahan/SFX_Kelelawar"); d.whoosh = Clip("SFX/Tambahan/SFX_Kelelawar_Lewat");
            d.batFrames = Frames("kelelawar_", 4);
            d.unlitMaterial = unlit;
            d.bellWave = bellWave;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.Refresh();
            SchedulePreviews();
            Debug.Log("[Midnight Castle] Scene Ruang Pamer selesai dibuat: " + ScenePath + ". Tekan Play untuk melihat.");
        }

        // ================= helper =================
        static char At(int x, int r)
        {
            if (r < 0 || r >= MAP.Length || x < 0 || x >= MAP[0].Length) return '#';
            return MAP[r][x];
        }
        static bool IsWall(char c) { return c == '#' || c == 't' || c == 'n' || c == 'w'; }
        static bool IsDoor(char c) { return c == 'D' || c == 'K' || c == 'S' || c == 'G' || c == 'O'; }
        static bool IsWallLike(char c) { return IsWall(c) || IsDoor(c); }
        static int Order(float y) { return 1000 - Mathf.RoundToInt(y * 10f); }

        static string ItemFor(char c)
        {
            switch (c)
            {
                case '1': return "senter";
                case '2': return "kunci_aula";
                case '3': return "surat";
                case '4': return "buku_merah";
                case '5': return "buku_biru";
                case '6': return "catatan";
                case '7': return "bongkahan_bulan";
                case '8': return "bongkahan_mahkota";
                case '9': return "bongkahan_mawar";
                case '0': return "bongkahan_gagak";
            }
            return null;
        }

        static string CarpetTile(int x, int r)
        {
            bool n = At(x, r - 1) != ',', s = At(x, r + 1) != ',', w = At(x - 1, r) != ',', e = At(x + 1, r) != ',';
            if (n && w) return "karpet_kiri_atas";
            if (n && e) return "karpet_kanan_atas";
            if (s && w) return "karpet_kiri_bawah";
            if (s && e) return "karpet_kanan_bawah";
            if (n) return "karpet_atas";
            if (s) return "karpet_bawah";
            if (w) return "karpet_kiri";
            if (e) return "karpet_kanan";
            return "karpet_tengah";
        }

        static void LoadSprites()
        {
            sp = new Dictionary<string, Sprite>();
            foreach (var p in new[] { "Tileset/Tileset_Kastil.png", "Benda/Objek_Kastil.png", "Benda/Benda_Temuan.png", "UI/UI_Kastil.png",
                "UI/UI_Judul_Midnight.png", "UI/UI_Judul_Castle.png", "UI/UI_Siluet_Kastil.png", "UI/UI_Vignette.png",
                "EfekVisual/FX_Efek_16px.png", "EfekVisual/FX_LingkaranSuara_32px.png", "EfekVisual/FX_GelombangDentang_48px.png",
                "EfekVisual/FX_Cahaya.png", "Showcase/Karakter_Contoh.png" })
            {
                foreach (var s in AssetDatabase.LoadAllAssetsAtPath(Root + "/" + p).OfType<Sprite>()) sp[s.name] = s;
            }
        }

        static Sprite S(string name)
        {
            Sprite s;
            if (sp.TryGetValue(name, out s)) return s;
            Debug.LogWarning("[Midnight Castle] Sprite tidak ditemukan: " + name);
            return null;
        }

        static Sprite[] Frames(string prefix, int n)
        {
            var list = new List<Sprite>();
            for (int i = 0; i < n; i++) { var s = S(prefix + i); if (s != null) list.Add(s); }
            return list.ToArray();
        }

        static TileBase T(string name) { return AssetDatabase.LoadAssetAtPath<Tile>(Root + "/Tileset/Tiles/" + name + ".asset"); }

        static AudioClip Clip(string rel) { return AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/" + rel + ".wav"); }

        static void EnsureMaterial()
        {
            unlit = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (unlit != null) return;
            var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) return;
            unlit = new Material(sh);
            AssetDatabase.CreateAsset(unlit, MatPath);
        }

        static Tilemap NewTilemap(GameObject grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tm = go.AddComponent<Tilemap>();
            var tr = go.AddComponent<TilemapRenderer>();
            tr.sortingOrder = order;
            return tm;
        }

        static SpriteRenderer Obj(Transform parent, string sprite, Vector2 pos, int order, bool unlitMat = false)
        {
            var go = new GameObject(sprite);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = S(sprite);
            r.sortingOrder = order;
            if (unlitMat && unlit != null) r.sharedMaterial = unlit;
            return r;
        }

        static Light2D AddLight(Transform parent, string name, Vector2 pos, Color color, float intensity, float radius, float flicker, bool midnight)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.08f;
            l.falloffIntensity = 0.6f;
            if (flicker > 0f)
            {
                var f = go.AddComponent<FlickerLight>();
                f.flicker = flicker;
                f.affectedByMidnight = midnight;
            }
            return l;
        }

        static void Item(Transform root, Transform lights, string sprite, Vector2 pos, int order, float lightRadius, float lightIntensity)
        {
            var holder = new GameObject("benda_" + sprite).transform;
            holder.SetParent(root, false);
            holder.position = new Vector3(pos.x, pos.y, 0f);
            var vis = Obj(holder, sprite, pos, order);
            vis.gameObject.AddComponent<Bob2D>();
            var spk = Obj(holder, "kilau_0", pos + new Vector2(0.3f, 0.32f), order + 1, true);
            Animate(spk.gameObject, "Anim_Kilau", 0f);
            AddLight(holder, "cahaya benda", pos, new Color(1f, 0.85f, 0.5f), lightIntensity, lightRadius, 0.15f, false);
        }

        static GameObject Character(Transform root, string name, string prefix, bool nativeRight, Vector2 a, Vector2 b, float speed, float fps, float bob, bool lit)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(a.x, a.y, 0f);
            var frames = Frames(prefix, 8);
            var r = Obj(go.transform, prefix + "0", a, Order(a.y));
            r.gameObject.name = "sprite";
            var p = go.AddComponent<Patrol>();
            p.a = a; p.b = b; p.speed = speed; p.fps = fps; p.frames = frames; p.sr = r; p.nativeFacesRight = nativeRight; p.bobAmp = bob;
            return go;
        }

        // Animator dari klip animasi efek (dibuat sekali per klip).
        static void Animate(GameObject go, string clipName, float loopGap)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AnimDir + "/" + clipName + ".anim");
            if (clip == null) { Debug.LogWarning("[Midnight Castle] Klip tidak ditemukan: " + clipName); return; }
            if (!AssetDatabase.IsValidFolder(CtrlDir)) AssetDatabase.CreateFolder(Dir, "Animator");
            string path = CtrlDir + "/" + clipName + ".controller";
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPathWithClip(path, clip);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (!settings.loopTime)
                {
                    var st = ctrl.layers[0].stateMachine.defaultState;
                    var tr = st.AddTransition(st);
                    tr.hasExitTime = true; tr.exitTime = 1f + loopGap; tr.duration = 0f;
                }
            }
            var anim = go.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.speed = Random.Range(0.85f, 1.15f);
        }

        static void AddCobwebs(Transform root)
        {
            for (int r = 1; r < MAP.Length - 1; r++)
                for (int x = 17; x < MAP[0].Length - 1; x++)
                {
                    if (IsWallLike(MAP[r][x]) || MAP[r][x] != '.') continue;
                    if (IsWall(MAP[r - 1][x]) && (IsWall(MAP[r][x - 1]) || IsWall(MAP[r][x + 1])))
                    {
                        var cw = Obj(root, "sarang_laba", new Vector2(x + 0.5f, H - 1 - r), Order(H - 1 - r + 0.9f));
                        if (IsWall(MAP[r][x + 1]) && !IsWall(MAP[r][x - 1])) cw.flipX = true;
                    }
                }
        }

        static void AddToBuildSettings()
        {
            var list = EditorBuildSettings.scenes.ToList();
            if (list.Any(s => s.path == ScenePath)) return;
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // Cahaya 2D baru siap setelah beberapa frame editor, jadi pengambilan gambar ditunda sebentar.
        static int previewFrames;

        [MenuItem("Midnight Castle/Simpan Gambar Pratinjau Ruang Pamer", false, 1)]
        public static void SchedulePreviews()
        {
            previewFrames = 12;
            EditorApplication.update -= PreviewTick;
            EditorApplication.update += PreviewTick;
        }

        static void PreviewTick()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            if (--previewFrames > 0) return;
            EditorApplication.update -= PreviewTick;
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Midnight Castle] Buka scene Ruang Pamer dulu untuk menyimpan gambar."); return; }
            RenderPreviews(cam);
            Debug.Log("[Midnight Castle] Gambar pratinjau disimpan di " + DocDir);
        }

        // Simpan gambar tiap ruangan ke folder Dokumentasi~ (untuk laporan).
        static void RenderPreviews(Camera cam)
        {
            try
            {
                Directory.CreateDirectory(DocDir);
                var xs = new[] { 8.5f, 24.5f, 40.5f };
                var names = new[] { "Showcase_1_Aula.png", "Showcase_2_Perpustakaan.png", "Showcase_3_RuangBawahTanah.png" };
                var old = cam.transform.position;
                var rt = new RenderTexture(1600, 900, 24);
                for (int i = 0; i < 3; i++)
                {
                    cam.transform.position = new Vector3(xs[i], 6.5f, -10f);
                    var req = new RenderPipeline.StandardRequest();
                    if (RenderPipeline.SupportsRenderRequest(cam, req)) { req.destination = rt; RenderPipeline.SubmitRenderRequest(cam, req); }
                    else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                    RenderTexture.active = rt;
                    var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                    tex.Apply();
                    File.WriteAllBytes(DocDir + "/" + names[i], tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                }
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);
                cam.transform.position = old;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Midnight Castle] Gagal menyimpan gambar pratinjau: " + e.Message);
            }
        }

        // ================= UI =================
        struct UIRefs
        {
            public CanvasGroup title, captionGroup;
            public Text caption, clock, clockLabel;
            public Image bar, flash;
        }

        static Font F(string file) { return AssetDatabase.LoadAssetAtPath<Font>(Root + "/UI/Font/" + file); }

        static RectTransform RT(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image Img(RectTransform rt, string sprite, Color c, bool sliced = false, bool keepAspect = true)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? S(sprite) : null;
            img.color = c;
            img.raycastTarget = false;
            if (sliced) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.2f; }
            else img.preserveAspect = keepAspect && sprite != null;
            return img;
        }

        static Text Txt(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string s, Font f, int fs, Color c, TextAnchor a, bool shadow = true)
        {
            var rt = RT(name, parent, anchor, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = f; t.fontSize = fs; t.color = c; t.alignment = a; t.text = s;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (shadow) { var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0f, 0f, 0f, 0.85f); sh.effectDistance = new Vector2(2f, -2f); }
            return t;
        }

        static UIRefs BuildUI()
        {
            var refs = new UIRefs();
            Font reg = F("CrimsonText-Regular.ttf"), bold = F("CrimsonText-Bold.ttf"), mono = F("AnonymousPro-Bold.ttf");
            Color gold = new Color(0.96f, 0.8f, 0.42f), pale = new Color(0.9f, 0.88f, 0.94f), ink = new Color(0.25f, 0.14f, 0.08f);
            Vector2 C = new Vector2(0.5f, 0.5f);

            var cgo = new GameObject("UI (Canvas)");
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            var root = cgo.transform;

            Img(Stretch("Vignette", root), "vignette", new Color(0f, 0f, 0f, 0.72f), false, false);

            // Jam & timeline
            var clock = RT("Panel Jam", root, new Vector2(0f, 1f), new Vector2(178, -64), new Vector2(320, 108));
            Img(clock, "panel_batu", Color.white, true);
            Img(RT("ikon_jam", clock, C, new Vector2(-114, 2), new Vector2(54, 54)), "ikon_jam", Color.white);
            refs.clock = Txt(clock, "waktu", C, new Vector2(18, 20), new Vector2(210, 44), "23:59:30", mono, 34, gold, TextAnchor.MiddleLeft);
            Img(RT("bar_bingkai", clock, C, new Vector2(38, -9), new Vector2(190, 16)), "bar_bingkai", Color.white, true);
            refs.bar = Img(RT("bar_isi", clock, C, new Vector2(38, -9), new Vector2(182, 8)), "putih", gold, false, false);
            refs.bar.type = Image.Type.Filled; refs.bar.fillMethod = Image.FillMethod.Horizontal; refs.bar.fillAmount = 0f;
            refs.clockLabel = Txt(clock, "label", C, new Vector2(38, -27), new Vector2(220, 20), "menuju tengah malam", reg, 14, new Color(0.72f, 0.7f, 0.8f), TextAnchor.MiddleCenter);

            // Tujuan
            var obj = RT("Panel Tujuan", root, new Vector2(1f, 1f), new Vector2(-210, -56), new Vector2(390, 88));
            Img(obj, "panel_batu", Color.white, true);
            Txt(obj, "judul", C, new Vector2(0, 22), new Vector2(330, 22), "TUJUAN", bold, 16, gold, TextAnchor.MiddleLeft);
            Txt(obj, "isi", C, new Vector2(0, -8), new Vector2(330, 44), "Kumpulkan 4 bongkahan Medali Ravenhold sebelum tengah malam.", reg, 18, pale, TextAnchor.UpperLeft);

            // Status
            var st = RT("Panel Status", root, new Vector2(0f, 0f), new Vector2(150, 60), new Vector2(270, 88));
            Img(st, "panel_batu", Color.white, true);
            Img(RT("ikon_mata", st, C, new Vector2(-98, 17), new Vector2(42, 28)), "ikon_mata_terbuka", Color.white);
            Txt(st, "status", C, new Vector2(30, 17), new Vector2(190, 26), "Tidak terlihat", bold, 19, pale, TextAnchor.MiddleLeft);
            Img(RT("ikon_lari", st, C, new Vector2(-98, -16), new Vector2(34, 34)), "ikon_lari", Color.white);
            Txt(st, "gerak", C, new Vector2(30, -16), new Vector2(190, 26), "Berlari (berisik!)", reg, 18, new Color(1f, 0.6f, 0.45f), TextAnchor.MiddleLeft);

            // Hotbar inventori
            var bar = RT("Inventori", root, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(470, 80));
            string[] icons = { "senter", "kunci_aula", "surat", "buku_merah", "catatan", "bongkahan_bulan" };
            for (int i = 0; i < icons.Length; i++)
            {
                var slot = RT("slot" + i, bar, C, new Vector2(-185 + i * 74, 0), new Vector2(66, 66));
                Img(slot, i == 0 ? "slot_dipilih" : "slot", Color.white, true);
                Img(RT("ikon", slot, C, Vector2.zero, new Vector2(48, 48)), icons[i], Color.white);
            }
            var key = RT("tombol_I", bar, C, new Vector2(262, 0), new Vector2(34, 34));
            Img(key, "tombol_keyboard", Color.white, true);
            Txt(key, "huruf", C, new Vector2(0, 2), new Vector2(34, 34), "I", bold, 22, ink, TextAnchor.MiddleCenter, false);

            // Keterangan (caption)
            var cap = RT("Keterangan", root, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(980, 62));
            var capImg = Img(cap, "panel_perkamen", Color.white, true);
            refs.captionGroup = cap.gameObject.AddComponent<CanvasGroup>();
            refs.caption = Txt(cap, "teks", C, new Vector2(0, 1), new Vector2(920, 50), "", bold, 19, ink, TextAnchor.MiddleCenter, false);

            // Kilatan
            refs.flash = Img(Stretch("Kilatan", root), "putih", new Color(1f, 1f, 1f, 0f), false, false);

            // Judul pembuka
            var title = Stretch("Judul Pembuka", root);
            refs.title = title.gameObject.AddComponent<CanvasGroup>();
            Img(Stretch("gelap", title), "putih", new Color(0.02f, 0.015f, 0.05f, 0.82f), false, false);
            Img(RT("bulan", title, C, new Vector2(330, 200), new Vector2(110, 110)), "bulan", Color.white);
            Img(RT("MIDNIGHT", title, C, new Vector2(0, 150), new Vector2(620, 100)), "UI_Judul_Midnight", Color.white);
            Img(RT("CASTLE", title, C, new Vector2(0, 52), new Vector2(440, 102)), "UI_Judul_Castle", Color.white);
            Txt(title, "sub1", C, new Vector2(0, -40), new Vector2(1000, 36), "Ruang Pamer Aset Visual & Audio", bold, 30, gold, TextAnchor.MiddleCenter);
            Txt(title, "sub2", C, new Vector2(0, -80), new Vector2(1000, 30), "Tileset  ·  Benda  ·  UI  ·  Efek Visual  ·  Musik & Efek Suara", reg, 22, pale, TextAnchor.MiddleCenter);
            Txt(title, "nama", C, new Vector2(0, -130), new Vector2(1000, 26), "Kelompok 7  —  Pengembangan GIM", reg, 18, new Color(0.7f, 0.68f, 0.8f), TextAnchor.MiddleCenter);
            return refs;
        }
    }
}
