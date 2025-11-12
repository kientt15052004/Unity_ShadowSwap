using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroSequenceController : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Image image;
        public TextMeshProUGUI text;
    }

    public Slide[] slides;
    public float fadeTime = 1f;
    public float displayTime = 6f;
    public string nextSceneName = "StartScreen"; //đổi tên màn ở đây

    bool _skipped = false;

    void Start()
    {
        // ẩn tất cả trước
        foreach (var s in slides)
        {
            SetAlpha(s.image, 0);
            SetAlpha(s.text, 0);
        }

        StartCoroutine(PlayIntro());
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            _skipped = true;
        }
    }

    IEnumerator PlayIntro()
    {
        for (int i = 0; i < slides.Length; i++)
        {
            yield return StartCoroutine(FadeIn(slides[i]));

            float t = 0;
            while (t < displayTime && !_skipped)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (_skipped) break;

            yield return StartCoroutine(FadeOut(slides[i]));
        }

        // chuyển scene
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeIn(Slide slide)
    {
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = t / fadeTime;
            SetAlpha(slide.image, a);
            SetAlpha(slide.text, a);
            yield return null;
        }
    }

    IEnumerator FadeOut(Slide slide)
    {
        float t = 0;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = 1 - t / fadeTime;
            SetAlpha(slide.image, a);
            SetAlpha(slide.text, a);
            yield return null;
        }
    }

    void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = a;
        g.color = c;
    }
}
