using UnityEngine;

namespace MidnightCastleAset.Showcase
{
    // Membuat benda melayang naik-turun pelan.
    public class Bob2D : MonoBehaviour
    {
        public float amp = 0.06f;
        public float speed = 2.5f;
        Vector3 basePos;
        float seed;

        void Start() { basePos = transform.localPosition; seed = Random.value * 6f; }

        void Update() { transform.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * speed + seed) * amp); }
    }
}
