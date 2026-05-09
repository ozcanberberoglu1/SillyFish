using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [SerializeField] private Image sliderFillImage;
    [SerializeField] private float loadingDuration = 4f;

    private void Start()
    {
        if (sliderFillImage != null)
            sliderFillImage.fillAmount = 0f;

        StartCoroutine(AnimateLoading());
    }

    private IEnumerator AnimateLoading()
    {
        float elapsed = 0f;

        while (elapsed < loadingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / loadingDuration);

            if (sliderFillImage != null)
                sliderFillImage.fillAmount = progress;

            yield return null;
        }

        if (sliderFillImage != null)
            sliderFillImage.fillAmount = 1f;

        SceneManager.LoadScene("MainmenuScene");
    }
}
