using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class OutroSequenceController : MonoBehaviour
{
    [System.Serializable]
    public class Slide
    {
        public Image image;
        public TextMeshProUGUI text;
        [Tooltip("Nếu bật, slide này sẽ không fade out (ảnh giữ nguyên). Nhưng text sẽ được ẩn trước khi slide kế tiếp hiển thị để tránh chồng chữ.")]
        public bool skipFadeOut = false;
    }

    public Slide[] slides;
    public float fadeTime = 1f;
    public float displayTime = 4f;
    public string nextSceneName = "StartScreen"; // đổi tên màn ở đây

    private bool _skipped = false;

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
            // trước khi FadeIn slide[i], đảm bảo text của slide trước (nếu giữ image) đã ẩn
            if (i > 0)
            {
                var prev = slides[i - 1];
                if (prev != null && prev.skipFadeOut)
                {
                    // ẩn text của slide trước ngay lập tức (không ảnh hưởng image)
                    if (prev.text != null)
                        SetAlpha(prev.text, 0f);
                }
            }

            yield return StartCoroutine(FadeIn(slides[i]));

            float t = 0;
            while (t < displayTime && !_skipped)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (_skipped) break;

            // Nếu slide này KHÔNG cần fade out thì chỉ ẩn text (giữ image)
            if (slides[i].skipFadeOut)
            {
                if (slides[i].text != null)
                    yield return StartCoroutine(FadeOutTextOnly(slides[i]));
                // image được giữ nguyên (alpha = 1)
            }
            else
            {
                yield return StartCoroutine(FadeOut(slides[i]));
            }
        }

        // chuyển scene
        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeIn(Slide slide)
    {
        float t = 0;
        // đảm bảo image/text khởi đầu là 0 (trong trường hợp bị can thiệp)
        if (slide.image != null) SetAlpha(slide.image, 0f);
        if (slide.text != null) SetAlpha(slide.text, 0f);

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = t / fadeTime;
            SetAlpha(slide.image, a);
            SetAlpha(slide.text, a);
            yield return null;
        }
        SetAlpha(slide.image, 1f);
        SetAlpha(slide.text, 1f);
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
        SetAlpha(slide.image, 0f);
        SetAlpha(slide.text, 0f);
    }

    // Chỉ fade out phần text, giữ nguyên image
    IEnumerator FadeOutTextOnly(Slide slide)
    {
        if (slide.text == null) yield break;
        float t = 0;
        float startAlpha = slide.text.color.a;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startAlpha, 0f, t / fadeTime);
            SetAlpha(slide.text, a);
            yield return null;
        }
        SetAlpha(slide.text, 0f);
    }

    void SetAlpha(Graphic g, float a)
    {
        if (g == null) return;
        var c = g.color;
        c.a = Mathf.Clamp01(a);
        g.color = c;
    }
}
