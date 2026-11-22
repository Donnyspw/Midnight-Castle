using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace MidnightCastleAset
{
    // Membuat otomatis dari sprite sheet:
    //  - Tile asset untuk setiap petak di Tileset_Kastil.png
    //  - Tile Palette "Palette_Kastil" (Window > 2D > Tile Palette)
    //  - Klip animasi efek visual (api obor, lilin, jam, kilau, debu, lingkaran suara, dll.)
    [InitializeOnLoad]
    public static class AsetTools
    {
        const string Root = "Assets/Aset_MidnightCastle";
        const string TilesetPath = Root + "/Tileset/Tileset_Kastil.png";
        const string TilesDir = Root + "/Tileset/Tiles";
        const string PaletteDir = Root + "/Tileset/Palette";
        const string PaletteName = "Palette_Kastil";
        const string AnimDir = Root + "/EfekVisual/Animasi";
        const string FxSheet = Root + "/EfekVisual/FX_Efek_16px.png";
        const string RingSheet = Root + "/EfekVisual/FX_LingkaranSuara_32px.png";
        const string BellSheet = Root + "/EfekVisual/FX_GelombangDentang_48px.png";
        const string ObjSheet = Root + "/Benda/Objek_Kastil.png";

        static string PalettePath { get { return PaletteDir + "/" + PaletteName + ".prefab"; } }

        static AsetTools()
        {
            EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(TilesetPath)) return;
            if (SpritesOf(TilesetPath).Length == 0) return; // belum selesai di-import
            bool missing = !AssetDatabase.IsValidFolder(TilesDir) || !File.Exists(PalettePath) || !AssetDatabase.IsValidFolder(AnimDir);
            if (missing) BuatSemua();
        }

        [MenuItem("Midnight Castle/Buat Ulang Tiles, Palette & Animasi")]
        public static void BuatSemua()
        {
            try
            {
                var tiles = BuatTiles();
                BuatPalette(tiles);
                BuatAnimasi();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[Midnight Castle] Selesai: " + tiles.Count + " tile, palette '" + PaletteName + "', dan klip animasi efek di " + AnimDir);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Midnight Castle] Gagal membuat sebagian aset: " + e.Message);
            }
        }

        static Sprite[] SpritesOf(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------------- Tiles ----------------
        struct PlacedTile { public Tile tile; public Vector3Int pos; }

        static List<PlacedTile> BuatTiles()
        {
            EnsureFolder(TilesDir);
            var result = new List<PlacedTile>();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TilesetPath);
            int texH = tex != null ? tex.height : 128;
            foreach (var s in SpritesOf(TilesetPath))
            {
                string path = TilesDir + "/" + s.name + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, path);
                }
                tile.sprite = s;
                tile.color = Color.white;
                tile.colliderType = s.name.StartsWith("dinding") ? Tile.ColliderType.Grid : Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
                int col = Mathf.RoundToInt(s.rect.x / 32f);
                int row = Mathf.RoundToInt((texH - s.rect.y - s.rect.height) / 32f);
                result.Add(new PlacedTile { tile = tile, pos = new Vector3Int(col, -row, 0) });
            }
            return result;
        }

        // ---------------- Tile Palette ----------------
        static void BuatPalette(List<PlacedTile> tiles)
        {
            EnsureFolder(PaletteDir);
            if (!File.Exists(PalettePath))
            {
                if (!CreatePaletteWithUnityUtility()) CreatePaletteManually();
            }
            if (!File.Exists(PalettePath)) { Debug.LogWarning("[Midnight Castle] Palette tidak bisa dibuat otomatis. Buat manual di Window > 2D > Tile Palette, lalu seret folder Tiles ke dalamnya."); return; }

            var contents = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var tm = contents.GetComponentInChildren<Tilemap>();
                if (tm == null)
                {
                    var layer = new GameObject("Layer1");
                    layer.transform.SetParent(contents.transform, false);
                    tm = layer.AddComponent<Tilemap>();
                    layer.AddComponent<TilemapRenderer>();
                }
                tm.ClearAllTiles();
                foreach (var t in tiles) tm.SetTile(t.pos, t.tile);
                PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullName);
                if (t != null) return t;
            }
            return null;
        }

        // Memakai fungsi bawaan paket 2D Tilemap lewat reflection (aman walau versi paket berbeda).
        static bool CreatePaletteWithUnityUtility()
        {
            try
            {
                var util = FindType("UnityEditor.Tilemaps.GridPaletteUtility");
                if (util == null) return false;
                var method = util.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == "CreateNewPalette")
                    .OrderBy(m => m.GetParameters().Length).FirstOrDefault();
                if (method == null) return false;
                var ps = method.GetParameters();
                var args = new object[ps.Length];
                int strings = 0, vectors = 0;
                for (int i = 0; i < ps.Length; i++)
                {
                    var pt = ps[i].ParameterType;
                    if (pt == typeof(string)) args[i] = strings++ == 0 ? PaletteDir : PaletteName;
                    else if (pt == typeof(Vector3)) args[i] = vectors++ == 0 ? new Vector3(1f, 1f, 0f) : new Vector3(0f, 0f, 1f);
                    else if (pt == typeof(GridLayout.CellLayout)) args[i] = GridLayout.CellLayout.Rectangle;
                    else if (pt == typeof(GridLayout.CellSwizzle)) args[i] = GridLayout.CellSwizzle.XYZ;
                    else if (pt == typeof(TransparencySortMode)) args[i] = TransparencySortMode.Default;
                    else if (pt.IsEnum) args[i] = Enum.ToObject(pt, 0); // GridPalette.CellSizing.Automatic
                    else if (ps[i].HasDefaultValue) args[i] = ps[i].DefaultValue;
                    else return false;
                }
                method.Invoke(null, args);
                AssetDatabase.Refresh();
                return File.Exists(PalettePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Midnight Castle] GridPaletteUtility gagal, mencoba cara manual. " + e.Message);
                return false;
            }
        }

        static void CreatePaletteManually()
        {
            var go = new GameObject(PaletteName);
            try
            {
                var grid = go.AddComponent<Grid>();
                grid.cellSize = new Vector3(1f, 1f, 0f);
                var layer = new GameObject("Layer1");
                layer.transform.SetParent(go.transform, false);
                layer.AddComponent<Tilemap>();
                layer.AddComponent<TilemapRenderer>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PalettePath);
                var paletteType = FindType("UnityEditor.GridPalette");
                if (paletteType != null && prefab != null)
                {
                    var palette = ScriptableObject.CreateInstance(paletteType);
                    palette.name = "Palette Settings";
                    AssetDatabase.AddObjectToAsset(palette, prefab);
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ---------------- Animasi ----------------
        static void BuatAnimasi()
        {
            EnsureFolder(AnimDir);
            Clip(FxSheet, "api_obor_", "Anim_ApiObor", 8f, true);
            Clip(FxSheet, "kilau_", "Anim_Kilau", 10f, true);
            Clip(FxSheet, "debu_langkah_", "Anim_DebuLangkah", 12f, false);
            Clip(FxSheet, "roh_asap_", "Anim_RohAsap", 8f, true);
            Clip(RingSheet, "lingkaran_suara_", "Anim_LingkaranSuara", 14f, false);
            Clip(BellSheet, "gelombang_dentang_", "Anim_GelombangDentang", 12f, false);
            Clip(ObjSheet, "lilin_", "Anim_Lilin", 7f, true);
            Clip(ObjSheet, "jam_besar_", "Anim_JamBesar_Bandul", 2f, true);
        }

        static int FrameIndex(string name)
        {
            int u = name.LastIndexOf('_');
            int n;
            return u >= 0 && int.TryParse(name.Substring(u + 1), out n) ? n : 0;
        }

        static void Clip(string sheet, string prefix, string clipName, float fps, bool loop)
        {
            var frames = SpritesOf(sheet).Where(s => s.name.StartsWith(prefix)).OrderBy(s => FrameIndex(s.name)).ToArray();
            if (frames.Length == 0) return;
            string path = AnimDir + "/" + clipName + ".anim";
            if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            var clip = new AnimationClip();
            clip.frameRate = fps;
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[frames.Length - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
        }
    }
}
