using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MidnightCastleAset.Showcase
{
    // Membuat cahaya (Light2D) berkedip seperti api, dan padam-nyala menjelang tengah malam.
    [RequireComponent(typeof(Light2D))]
    public class FlickerLight : MonoBehaviour
    {
        public float flicker = 0.12f;
        public float speed = 3f;
        public bool affectedByMidnight = true;
        public static float Midnight; // 0..1, diatur ShowcaseDirector

        Light2D l;
        float baseIntensity, baseRadius, seed;

        void Awake()
        {
            l = GetComponent<Light2D>();
            baseIntensity = l.intensity;
            baseRadius = l.pointLightOuterRadius;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            float drop = 1f;
            if (affectedByMidnight && Midnight > 0f && Mathf.PerlinNoise(seed + 5f, Time.time * 10f) < Midnight * 0.6f) drop = 0.15f;
            l.intensity = baseIntensity * (1f + n * flicker * 2f) * drop;
            if (l.lightType == Light2D.LightType.Point) l.pointLightOuterRadius = baseRadius * (1f + n * flicker);
        }
    }
}
