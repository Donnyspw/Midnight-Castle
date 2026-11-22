using UnityEngine;

namespace MidnightCastleAset.Showcase
{
    // Karakter contoh yang bergerak bolak-balik: animasi frame, suara langkah, dan efek di kaki.
    public class Patrol : MonoBehaviour
    {
        public Vector2 a, b;
        public float speed = 2f;
        public float waitTime = 0.6f;
        public SpriteRenderer sr;
        public Sprite[] frames;
        public float fps = 10f;
        public bool nativeFacesRight = true;
        public float bobAmp;

        [Header("Langkah")]
        public AudioSource audioSrc;
        public AudioClip[] stepClips;
        public float stepVolume = 0.6f;
        public float stepInterval = 0.3f;

        [Header("Efek di kaki")]
        public Sprite[] stepFx;
        public Color stepFxColor = Color.white;
        public Sprite[] stepFx2;
        public Color stepFx2Color = Color.white;
        public Material fxMaterial;

        bool toB = true;
        float wait, animT, stepT;
        Vector3 visualBase;

        void Start()
        {
            transform.position = new Vector3(a.x, a.y, 0f);
            if (sr != null) visualBase = sr.transform.localPosition;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Vector2 target = toB ? b : a;
            Vector2 p = transform.position;
            bool moving = wait <= 0f;
            if (!moving) wait -= dt;
            else
            {
                p = Vector2.MoveTowards(p, target, speed * dt);
                transform.position = new Vector3(p.x, p.y, 0f);
                if ((p - target).sqrMagnitude < 0.0001f) { toB = !toB; wait = waitTime; }
                stepT -= dt;
                if (stepT <= 0f) { stepT = stepInterval; Step(); }
            }

            if (sr != null)
            {
                if (frames != null && frames.Length > 0)
                {
                    if (moving) animT += dt * fps;
                    sr.sprite = frames[(int)animT % frames.Length];
                }
                float dir = toB ? b.x - a.x : a.x - b.x;
                bool right = dir > 0f;
                sr.flipX = nativeFacesRight ? !right : right;
                sr.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 10f);
                if (bobAmp > 0f) sr.transform.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * 2.6f) * bobAmp);
            }
        }

        void Step()
        {
            int order = sr != null ? sr.sortingOrder + 1 : 1000;
            if (audioSrc != null && stepClips != null && stepClips.Length > 0)
            {
                float vol = stepVolume * Audible();
                if (vol > 0.01f) audioSrc.PlayOneShot(stepClips[Random.Range(0, stepClips.Length)], vol);
            }
            FxOnce.Spawn(stepFx, transform.position + new Vector3(0f, 0.05f, 0f), 12f, order, stepFxColor, fxMaterial);
            FxOnce.Spawn(stepFx2, transform.position + new Vector3(0f, 0.08f, 0f), 14f, order - 2, stepFx2Color, fxMaterial);
        }

        float Audible()
        {
            var cam = Camera.main;
            if (cam == null) return 1f;
            float d = Mathf.Abs(cam.transform.position.x - transform.position.x);
            return Mathf.Clamp01(1f - d / 14f);
        }
    }
}
