using UnityEngine;
using UnityEngine.UI;

public class DarkDeepOverlay : MonoBehaviour
{
    [Tooltip("Overlay Image (noise shader materyali atanmış)")]
    [SerializeField] private Image overlayImage;
    [Tooltip("Karanlığa geçiş yumuşaklığı (düşük = daha yumuşak)")]
    [SerializeField] private float fadeSpeed = 1.5f;

    private Material overlayMat;
    private float targetOpacity;
    private float currentOpacity;
    private float depthMultiplier;
    private static readonly int OpacityID = Shader.PropertyToID("_Opacity");
    private static readonly int DarkMinID = Shader.PropertyToID("_DarkMin");
    private static readonly int DarkMaxID = Shader.PropertyToID("_DarkMax");

    private float baseDarkMin;
    private float baseDarkMax;

    private void Start()
    {
        if (overlayImage != null)
        {
            overlayMat = new Material(overlayImage.material);
            overlayImage.material = overlayMat;
            overlayMat.SetFloat(OpacityID, 0f);
            baseDarkMin = overlayMat.GetFloat(DarkMinID);
            baseDarkMax = overlayMat.GetFloat(DarkMaxID);
            overlayImage.gameObject.SetActive(false);
        }
    }

    public void SetDark(bool dark, float opacity = 0f, float depth = 0f)
    {
        targetOpacity = dark ? Mathf.Clamp01(opacity) : 0f;
        depthMultiplier = Mathf.Clamp01(depth);
        if (dark && overlayImage != null && !overlayImage.gameObject.activeSelf)
            overlayImage.gameObject.SetActive(true);
    }

    public float GetDarkness() => currentOpacity;

    private void Update()
    {
        if (overlayMat == null) return;

        currentOpacity = Mathf.Lerp(currentOpacity, targetOpacity, fadeSpeed * Time.deltaTime);
        overlayMat.SetFloat(OpacityID, currentOpacity);

        float baseRange = baseDarkMax - baseDarkMin;
        float newMin = baseDarkMin + depthMultiplier * (1f - baseDarkMin - baseRange);
        float newMax = newMin + baseRange;
        overlayMat.SetFloat(DarkMinID, Mathf.Clamp01(newMin));
        overlayMat.SetFloat(DarkMaxID, Mathf.Clamp01(newMax));

        if (currentOpacity < 0.01f && targetOpacity <= 0f && overlayImage != null)
            overlayImage.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlayMat != null)
            Destroy(overlayMat);
    }
}
