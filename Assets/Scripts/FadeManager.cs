using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeManager : MonoBehaviour
{
    public Image fadeImage;
    public float fadeTime = 0.7f;

    public IEnumerator FadeOut()
    {
        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            float alpha = time / fadeTime;

            fadeImage.color = new Color(0f, 0f, 0f, alpha);

            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 1f);
    }

    public IEnumerator FadeIn()
    {
        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            float alpha = 1f - (time / fadeTime);

            fadeImage.color = new Color(0f, 0f, 0f, alpha);

            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, 0f);
    }
}