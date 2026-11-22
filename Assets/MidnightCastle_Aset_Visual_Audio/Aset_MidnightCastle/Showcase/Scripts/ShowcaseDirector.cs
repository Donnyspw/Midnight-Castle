using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MidnightCastleAset.Showcase
{
    // Mengatur jalannya ruang pamer:
    //  - kamera menyusuri tiap ruangan sambil menampilkan keterangan aset,
    //  - jam di UI berdetak menuju 00:00, lalu berdentang 12 kali,
    //  - saat dentang: lampu berkedip, layar berkilat, kawanan kelelawar lewat.
    public class ShowcaseDirector : MonoBehaviour
    {
        [Header("Kamera")]
        public Camera cam;
        public Vector2[] stops;
        [TextArea] public string[] captions;
        public float holdTime = 7f;
        public float moveTime = 2.5f;

        [Header("UI")]
        public CanvasGroup titleGroup;
        public CanvasGroup captionGroup;
        public Text captionText;
        public Text clockText;
        public Text clockLabel;
        public Image barFill;
        public Image flash;

        [Header("Cahaya")]
        public Light2D globalLight;

        [Header("Audio")]
        public AudioSource sfx;
        public AudioClip tick, tock, bell, batsClip, whoosh, warning;

        [Header("Tengah malam")]
        public int startSecond = 30;
        public Sprite[] batFrames;
        public Material unlitMaterial;
        public GameObject bellWave;

        int stop;
        float holdT, moveT;
        bool moving;
        Vector3 camFrom, camTo, camBase;
        float sec;
        int lastSec = -1;
        bool striking;
        int bells;
        float bellT, afterT;
        float titleT;
        float globalBase = 0.3f;
        float shakeT, shakeAmt;
        bool tockNext;
        readonly Color gold = new Color(0.96f, 0.8f, 0.42f);
        readonly Color blood = new Color(0.9f, 0.22f, 0.22f);

        class Bat { public Transform t; public SpriteRenderer sr; public Vector2 vel; public float phase; }
        readonly List<Bat> bats = new List<Bat>();

        void Start()
        {
            if (cam == null) cam = Camera.main;
            if (globalLight != null) globalBase = globalLight.intensity;
            sec = startSecond;
            if (stops != null && stops.Length > 0) camBase = new Vector3(stops[0].x, stops[0].y, -10f);
            else camBase = cam.transform.position;
            ShowCaption(0);
            if (bellWave != null) bellWave.SetActive(false);
            FlickerLight.Midnight = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateTitle(dt);
            UpdateCamera(dt);
            UpdateClock(dt);
            UpdateBats(dt);
            if (flash != null)
            {
                var c = flash.color; c.a = Mathf.MoveTowards(c.a, 0f, dt * 1.2f); flash.color = c;
            }
        }

        // ---------------- Judul pembuka ----------------
        void UpdateTitle(float dt)
        {
            if (titleGroup == null) return;
            titleT += dt;
            titleGroup.alpha = titleT < 3.5f ? 1f : Mathf.Clamp01(1f - (titleT - 3.5f) / 1.5f);
            if (titleT > 5.5f) titleGroup.gameObject.SetActive(false);
        }

        // ---------------- Kamera ----------------
        void UpdateCamera(float dt)
        {
            if (cam == null || stops == null || stops.Length == 0) return;
            if (!moving)
            {
                holdT += dt;
                if (captionGroup != null) captionGroup.alpha = Mathf.MoveTowards(captionGroup.alpha, 1f, dt * 2f);
                if (holdT >= holdTime && stops.Length > 1)
                {
                    moving = true; moveT = 0f; holdT = 0f;
                    camFrom = camBase;
                    stop = (stop + 1) % stops.Length;
                    camTo = new Vector3(stops[stop].x, stops[stop].y, -10f);
                }
            }
            else
            {
                moveT += dt;
                if (captionGroup != null) captionGroup.alpha = Mathf.MoveTowards(captionGroup.alpha, 0f, dt * 3f);
                float k = Mathf.Clamp01(moveT / moveTime);
                k = k * k * (3f - 2f * k);
                camBase = Vector3.Lerp(camFrom, camTo, k);
                if (moveT >= moveTime) { moving = false; ShowCaption(stop); }
            }
            Vector3 shake = Vector3.zero;
            if (shakeT > 0f) { shakeT -= dt; shake = (Vector3)(Random.insideUnitCircle * shakeAmt); }
            cam.transform.position = camBase + shake;
        }

        void ShowCaption(int i)
        {
            if (captionText != null && captions != null && i < captions.Length) captionText.text = captions[i];
        }

        // ---------------- Jam & dentang ----------------
        void UpdateClock(float dt)
        {
            if (!striking)
            {
                sec += dt;
                int s = Mathf.FloorToInt(sec);
                if (s != lastSec)
                {
                    lastSec = s;
                    float warn = Mathf.Clamp01((s - 48f) / 12f);
                    if (sfx != null) sfx.PlayOneShot(tockNext ? tock : tick, Mathf.Lerp(0.25f, 1f, warn));
                    tockNext = !tockNext;
                    if (s == 48 && warning != null && sfx != null) sfx.PlayOneShot(warning, 0.7f);
                }
                float w = Mathf.Clamp01((sec - 48f) / 12f);
                FlickerLight.Midnight = w;
                if (globalLight != null) globalLight.intensity = globalBase * (1f - 0.4f * w);
                if (clockText != null)
                {
                    clockText.text = "23:59:" + Mathf.Min(59, s).ToString("00");
                    clockText.color = w > 0f ? Color.Lerp(gold, blood, 0.5f + 0.5f * Mathf.Sin(Time.time * 10f)) : gold;
                }
                if (clockLabel != null) clockLabel.text = w > 0f ? "jam akan berdentang!" : "menuju tengah malam";
                if (barFill != null) { barFill.fillAmount = Mathf.Clamp01((sec - startSecond) / (60f - startSecond)); barFill.color = w > 0f ? blood : gold; }
                if (sec >= 60f) { striking = true; bells = 0; bellT = 0f; afterT = 0f; if (clockText != null) clockText.text = "00:00:00"; }
            }
            else
            {
                FlickerLight.Midnight = 1f;
                if (globalLight != null) globalLight.intensity = globalBase * 0.5f;
                if (bells < 12)
                {
                    bellT -= dt;
                    if (bellT <= 0f)
                    {
                        bellT = 0.6f; bells++;
                        if (sfx != null && bell != null) sfx.PlayOneShot(bell, 0.9f);
                        if (flash != null) flash.color = new Color(0.65f, 0.75f, 1f, 0.22f);
                        shakeT = 0.25f; shakeAmt = 0.08f;
                        if (bellWave != null) { bellWave.SetActive(false); bellWave.SetActive(true); }
                        if (bells == 2) SpawnBats();
                        if (clockLabel != null) clockLabel.text = "DENTANG " + bells + " / 12";
                    }
                }
                else
                {
                    afterT += dt;
                    if (afterT > 3f && bats.Count == 0)
                    {
                        striking = false; sec = startSecond; lastSec = -1;
                        FlickerLight.Midnight = 0f;
                        if (globalLight != null) globalLight.intensity = globalBase;
                        if (bellWave != null) bellWave.SetActive(false);
                    }
                }
                if (clockText != null) clockText.color = blood;
            }
        }

        // ---------------- Kelelawar hantu ----------------
        void SpawnBats()
        {
            if (batFrames == null || batFrames.Length == 0 || cam == null) return;
            if (sfx != null && batsClip != null) sfx.PlayOneShot(batsClip, 0.9f);
            float h = cam.orthographicSize, w = h * cam.aspect;
            for (int i = 0; i < 26; i++)
            {
                var go = new GameObject("kelelawar");
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                if (unlitMaterial != null) r.sharedMaterial = unlitMaterial;
                r.sprite = batFrames[0];
                r.sortingOrder = 2000;
                bool fromLeft = Random.value < 0.6f;
                float x = fromLeft ? camBase.x - w - Random.Range(0.5f, 9f) : camBase.x + w + Random.Range(0.5f, 9f);
                go.transform.position = new Vector3(x, camBase.y + Random.Range(-h * 0.9f, h * 0.9f), 0f);
                float s = Random.Range(0.8f, 1.4f);
                go.transform.localScale = new Vector3(s, s, 1f);
                bats.Add(new Bat { t = go.transform, sr = r, vel = new Vector2(fromLeft ? 1f : -1f, Random.Range(-0.15f, 0.15f)) * Random.Range(6f, 11f), phase = Random.value * 6f });
            }
        }

        void UpdateBats(float dt)
        {
            if (bats.Count == 0) return;
            float h = cam.orthographicSize, w = h * cam.aspect;
            for (int i = bats.Count - 1; i >= 0; i--)
            {
                var b = bats[i];
                Vector3 p = b.t.position;
                p += (Vector3)(b.vel * dt) + new Vector3(0f, Mathf.Sin(Time.time * 8f + b.phase) * dt * 1.5f, 0f);
                b.t.position = p;
                b.sr.flipX = b.vel.x < 0f;
                b.sr.sprite = batFrames[(int)((Time.time + b.phase) * 14f) % batFrames.Length];
                bool gone = Mathf.Abs(p.x - camBase.x) > w + 10f;
                bool leaving = (p.x - camBase.x) * b.vel.x > 0f && Mathf.Abs(p.x - camBase.x) > w + 1f;
                if (gone || leaving)
                {
                    Destroy(b.t.gameObject);
                    bats.RemoveAt(i);
                }
                else if (Random.value < dt * 0.15f && sfx != null && whoosh != null) sfx.PlayOneShot(whoosh, 0.25f);
            }
        }
    }
}
