using UnityEngine;

namespace MidnightCastleAset.Showcase
{
    // Memutar frame efek visual satu kali lalu menghapus dirinya (debu, lingkaran suara, asap roh).
    public class FxOnce : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 12f;
        SpriteRenderer sr;
        float t;

        public static FxOnce Spawn(Sprite[] frames, Vector3 pos, float fps, int order, Color color, Material mat)
        {
            if (frames == null || frames.Length == 0) return null;
            var go = new GameObject("fx");
            go.transform.position = pos;
            var r = go.AddComponent<SpriteRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            r.sprite = frames[0];
            r.sortingOrder = order;
            r.color = color;
            var f = go.AddComponent<FxOnce>();
            f.frames = frames;
            f.fps = fps;
            return f;
        }

        void Awake() { sr = GetComponent<SpriteRenderer>(); }

        void Update()
        {
            t += Time.deltaTime;
            int i = (int)(t * fps);
            if (frames == null || i >= frames.Length) { Destroy(gameObject); return; }
            sr.sprite = frames[i];
        }
    }
}
