using UnityEngine;
using UnityEngine.UI;

public class ChainWaveEffect : MonoBehaviour
{
    [Tooltip("Dikey sallanma miktarı (piksel)")]
    [SerializeField] private float waveAmplitude = 8f;
    [Tooltip("Dalga hızı")]
    [SerializeField] private float waveSpeed = 1.5f;
    [Tooltip("Zincirler arası faz farkı")]
    [SerializeField] private float phaseOffset = 0.6f;
    [Tooltip("Hafif dönme miktarı (derece)")]
    [SerializeField] private float rotationAmount = 2f;

    private RectTransform[] children;
    private Vector3[] layoutPositions;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        int count = transform.childCount;
        children = new RectTransform[count];
        layoutPositions = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            children[i] = transform.GetChild(i) as RectTransform;
            layoutPositions[i] = children[i].localPosition;
        }

        var hlg = GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;

        var csf = GetComponent<ContentSizeFitter>();
        if (csf != null) csf.enabled = false;
    }

    private void Update()
    {
        float time = Time.time * waveSpeed;

        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null) continue;

            float phase = i * phaseOffset;
            float yOffset = Mathf.Sin(time + phase) * waveAmplitude;
            float rot = Mathf.Sin(time * 0.7f + phase * 1.3f) * rotationAmount;

            children[i].localPosition = layoutPositions[i] + new Vector3(0f, yOffset, 0f);
            children[i].localRotation = Quaternion.Euler(0f, 0f, rot);
        }
    }
}
