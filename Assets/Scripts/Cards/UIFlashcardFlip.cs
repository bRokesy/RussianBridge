using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFlashcardFlip : MonoBehaviour
{
    [Header("Sides")]
    [SerializeField] private GameObject frontSide;
    [SerializeField] private GameObject backSide;

    [Header("Front")]
    [SerializeField] private TextMeshProUGUI foreignText;

    [Header("Back")]
    [SerializeField] private TextMeshProUGUI translationText;
    [SerializeField] private Image picture;

    [Header("Example (опционально)")]
    [SerializeField] private TextMeshProUGUI exampleForeignText;
    [SerializeField] private TextMeshProUGUI exampleTranslationText;
    [SerializeField] private GameObject exampleContainer;

    [Header("Flip Settings")]
    [SerializeField] private float halfFlipDuration = 0.15f;
    [SerializeField] private AnimationCurve easing = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private RectTransform rect;
    private AudioSource audioSource;
    private bool isFront = true;
    private bool isFlipping = false;

    private AudioClip frontAudio;
    private AudioClip backAudio;

    void Awake()
    {
        rect = (RectTransform)transform;

        // AudioSource — добавить автоматически если нет
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(TryFlip);
    }

    public void SetData(string foreignWord, string translation, Sprite img,
                        string exampleForeign = "", string exampleTranslation = "" , AudioClip frontClip = null, AudioClip backClip = null)
    {
        if (foreignText)     foreignText.text     = foreignWord;
        if (translationText) translationText.text = translation;
        if (picture)         picture.sprite       = img;

        frontAudio = frontClip;
        backAudio  = backClip;

        bool hasExample = !string.IsNullOrEmpty(exampleTranslation) && !string.IsNullOrEmpty(exampleForeign);
        if (exampleForeignText) exampleForeignText.text = hasExample ? exampleForeign : "";
        if (exampleTranslationText) exampleTranslationText.text = hasExample ? exampleTranslation : "";
        if (exampleContainer != null)
        {
            exampleContainer.SetActive(hasExample);
        } 
        else if (exampleForeignText != null && exampleForeignText != null)
        {
            exampleForeignText.gameObject.SetActive(hasExample);
            exampleTranslationText.gameObject.SetActive(hasExample);
        }

        ResetToFront();
    }

    public void ResetToFront()
    {
        isFront = true;
        if (frontSide) frontSide.SetActive(true);
        if (backSide)  backSide.SetActive(false);
        if (rect)      rect.localScale = Vector3.one;
        isFlipping = false;
    }

    public void TryFlip()
    {
        if (isFlipping) return;
        StartCoroutine(FlipRoutine());
    }

    IEnumerator FlipRoutine()
    {
        isFlipping = true;

        // Схлопнуть
        yield return ScaleX(1f, 0f, halfFlipDuration);

        // Сменить сторону
        isFront = !isFront;
        if (frontSide) frontSide.SetActive(isFront);
        if (backSide)  backSide.SetActive(!isFront);

        // Раскрыть
        yield return ScaleX(0f, 1f, halfFlipDuration);

        // Воспроизвести аудио новой стороны
        PlayCurrentAudio();

        isFlipping = false;
    }

    void PlayCurrentAudio()
    {
        AudioClip clip = isFront ? frontAudio : backAudio;
        if (clip == null || audioSource == null) return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    IEnumerator ScaleX(float from, float to, float duration)
    {
        float t = 0f;
        Vector3 s = rect.localScale;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float x = Mathf.Lerp(from, to, easing.Evaluate(Mathf.Clamp01(t / duration)));
            rect.localScale = new Vector3(x, s.y, s.z);
            yield return null;
        }
        rect.localScale = new Vector3(to, s.y, s.z);
    }
}