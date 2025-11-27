using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class FadeText : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float delayBetweenFades = 0.5f;
    [SerializeField] private bool fadeOnStart = true;
    [SerializeField] private bool loop = false;
    [SerializeField] private bool startVisible = false;

    private TMP_Text tmpText;
    private Coroutine fadeRoutine;

    private void OnDisable()
    {
        // Ensure no coroutine keeps running while disabled
        StopCurrentFade();
    }

    private void OnEnable()
    {
        InitializeFadeState();
    }

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        InitializeFadeState();
    }
    
    private void InitializeFadeState()
    {
        // Reset alpha based on startVisible
        Color c = tmpText.color;
        c.a = startVisible ? 1f : 0f;
        tmpText.color = c;

        // Run fade if requested
        if (fadeOnStart)
        {
            if (loop)
                FadeLoop();
            else
                FadeIn();
        }
    }

    public void FadeIn()
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(Fade(0f, 1f));
    }

    public void FadeOut()
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(Fade(1f, 0f));
    }

    public void FadeInOut()
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(FadeSequenceOnce());
    }

    public void FadeLoop()
    {
        StopCurrentFade();
        fadeRoutine = StartCoroutine(FadeLoopRoutine());
    }

    private void StopCurrentFade()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
    }

    private IEnumerator FadeSequenceOnce()
    {
        yield return Fade(0f, 1f);
        yield return new WaitForSeconds(delayBetweenFades);
        yield return Fade(1f, 0f);
    }

    private IEnumerator FadeLoopRoutine()
    {
        while (true)
        {
            yield return Fade(0f, 1f);
            yield return new WaitForSeconds(delayBetweenFades);
            yield return Fade(1f, 0f);
            yield return new WaitForSeconds(delayBetweenFades);
        }
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;
        Color color = tmpText.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            tmpText.color = color;
            yield return null;
        }

        color.a = endAlpha;
        tmpText.color = color;
    }
}