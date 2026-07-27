using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIParticleEffect : MonoBehaviour
{
    public enum EffectType { Burst, Ripple, Sparkle }

    [Header("Effect Settings")]
    [SerializeField] private EffectType effectType = EffectType.Burst;
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool loop;

    [Header("Particles")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private int particleCount = 12;
    [SerializeField] private float duration = 1f;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color endColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float startSize = 30f;
    [SerializeField] private float endSize = 5f;

    [Header("Burst Settings")]
    [SerializeField] private float burstSpeed = 300f;
    [SerializeField] private float burstSpeedVariance = 100f;

    [Header("Ripple Settings")]
    [SerializeField] private int rippleCount = 3;
    [SerializeField] private float rippleMaxSize = 200f;
    [SerializeField] private float rippleDelay = 0.2f;
    [SerializeField] private float rippleWidth = 8f;

    [Header("Sparkle Settings")]
    [SerializeField] private float sparkleRadius = 150f;
    [SerializeField] private float sparkleMinSize = 10f;
    [SerializeField] private float sparkleMaxSize = 40f;

    private RectTransform rectTransform;
    private List<RectTransform> particles = new List<RectTransform>();
    private bool isPlaying;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (playOnEnable) Play();
    }

    public void Play()
    {
        if (isPlaying) StopAllCoroutines();
        StartCoroutine(RunEffect());
    }

    private IEnumerator RunEffect()
    {
        isPlaying = true;

        do
        {
            ClearParticles();

            switch (effectType)
            {
                case EffectType.Burst:
                    yield return StartCoroutine(BurstEffect());
                    break;
                case EffectType.Ripple:
                    yield return StartCoroutine(RippleEffect());
                    break;
                case EffectType.Sparkle:
                    yield return StartCoroutine(SparkleEffect());
                    break;
            }

            ClearParticles();
        }
        while (loop);

        isPlaying = false;
    }

    #region Burst - merkezden dışa doğru saçılan partiküller

    private IEnumerator BurstEffect()
    {
        var data = new List<BurstData>();

        for (int i = 0; i < particleCount; i++)
        {
            RectTransform p = CreateParticle();
            float angle = (360f / particleCount) * i + Random.Range(-15f, 15f);
            float rad = angle * Mathf.Deg2Rad;
            float speed = burstSpeed + Random.Range(-burstSpeedVariance, burstSpeedVariance);
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            data.Add(new BurstData { rt = p, direction = dir, speed = speed, image = p.GetComponent<Image>() });
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            foreach (var d in data)
            {
                if (d.rt == null) continue;
                d.rt.anchoredPosition += d.direction * d.speed * Time.deltaTime;
                d.rt.sizeDelta = Vector2.one * Mathf.Lerp(startSize, endSize, t);
                d.image.color = Color.Lerp(startColor, endColor, t);
                d.rt.localRotation = Quaternion.Euler(0, 0, t * 180f);
            }

            yield return null;
        }
    }

    private struct BurstData
    {
        public RectTransform rt;
        public Vector2 direction;
        public float speed;
        public Image image;
    }

    #endregion

    #region Ripple - merkezden büyüyen halkalar

    private IEnumerator RippleEffect()
    {
        var ripples = new List<RippleData>();

        for (int i = 0; i < rippleCount; i++)
        {
            if (i > 0)
                yield return new WaitForSeconds(rippleDelay);

            GameObject go = new GameObject("Ripple");
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            Image outline = go.AddComponent<Image>();
            outline.sprite = null;
            outline.color = startColor;
            outline.raycastTarget = false;

            var olineComp = go.AddComponent<Outline>();
            olineComp.effectColor = startColor;
            olineComp.effectDistance = new Vector2(rippleWidth, rippleWidth);

            Color c = startColor;
            c.a = 0.01f;
            outline.color = c;

            ripples.Add(new RippleData { rt = rt, image = outline, startTime = Time.time });
        }

        float totalDuration = duration + rippleDelay * (rippleCount - 1);
        float startT = Time.time;

        while (Time.time - startT < totalDuration)
        {
            foreach (var r in ripples)
            {
                if (r.rt == null) continue;
                float age = (Time.time - r.startTime) / duration;
                if (age > 1f) { r.rt.gameObject.SetActive(false); continue; }

                float size = Mathf.Lerp(0, rippleMaxSize, Mathf.SmoothStep(0, 1, age));
                r.rt.sizeDelta = Vector2.one * size;

                Color c = startColor;
                c.a = (1f - age) * startColor.a;
                c.a *= 0.01f;
                r.image.color = c;

                var outline = r.rt.GetComponent<Outline>();
                if (outline != null)
                {
                    Color oc = startColor;
                    oc.a = (1f - age) * startColor.a;
                    outline.effectColor = oc;
                }
            }
            yield return null;
        }
    }

    private struct RippleData
    {
        public RectTransform rt;
        public Image image;
        public float startTime;
    }

    #endregion

    #region Sparkle - rastgele konumlarda belirip kaybolan ışıltılar

    private IEnumerator SparkleEffect()
    {
        var sparkles = new List<SparkleData>();

        for (int i = 0; i < particleCount; i++)
        {
            RectTransform p = CreateParticle();
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(0f, sparkleRadius);
            p.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            float size = Random.Range(sparkleMinSize, sparkleMaxSize);
            p.sizeDelta = Vector2.one * size;

            float delay = Random.Range(0f, duration * 0.6f);

            sparkles.Add(new SparkleData
            {
                rt = p,
                image = p.GetComponent<Image>(),
                targetSize = size,
                delay = delay,
                lifetime = duration * Random.Range(0.3f, 0.7f)
            });
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            foreach (var s in sparkles)
            {
                if (s.rt == null) continue;
                float age = elapsed - s.delay;
                if (age < 0f) { s.image.color = new Color(1, 1, 1, 0); continue; }

                float t = age / s.lifetime;
                if (t > 1f) { s.rt.gameObject.SetActive(false); continue; }

                float pulse = Mathf.Sin(t * Mathf.PI);
                float scale = pulse * s.targetSize;
                s.rt.sizeDelta = Vector2.one * scale;
                s.rt.localRotation = Quaternion.Euler(0, 0, t * 90f);

                Color c = Color.Lerp(startColor, endColor, t);
                c.a = pulse * startColor.a;
                s.image.color = c;
            }

            yield return null;
        }
    }

    private struct SparkleData
    {
        public RectTransform rt;
        public Image image;
        public float targetSize;
        public float delay;
        public float lifetime;
    }

    #endregion

    #region Helpers

    private RectTransform CreateParticle()
    {
        GameObject go = new GameObject("Particle");
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.one * startSize;

        Image img = go.AddComponent<Image>();
        img.sprite = particleSprite;
        img.color = startColor;
        img.raycastTarget = false;

        particles.Add(rt);
        return rt;
    }

    private void ClearParticles()
    {
        foreach (var p in particles)
            if (p != null) Destroy(p.gameObject);
        particles.Clear();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        ClearParticles();
        isPlaying = false;
    }

    #endregion
}
