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

    private RectTransform rectTransform;
    private AudioSource audioSource;
    private Button button;
    private AudioClip frontAudio;
    private AudioClip backAudio;
    private bool isFront = true;
    private bool isFlipping;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        button = GetComponent<Button>();
        button?.onClick.AddListener(TryFlip);
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveListener(TryFlip);
    }

    public void SetData(
        string foreignWord,
        string translation,
        Sprite image,
        string exampleForeign = "",
        string exampleTranslation = "",
        AudioClip frontClip = null,
        AudioClip backClip = null)
    {
        ProjectUtilities.SetText(foreignText, foreignWord);
        ProjectUtilities.SetText(translationText, translation);

        if (picture != null)
            picture.sprite = image;

        frontAudio = frontClip;
        backAudio = backClip;

        SetExample(exampleForeign, exampleTranslation);
        ResetToFront();
    }

    public void ResetToFront()
    {
        isFront = true;
        isFlipping = false;

        frontSide?.SetActive(true);
        backSide?.SetActive(false);

        if (rectTransform != null)
            rectTransform.localScale = Vector3.one;
    }

    public void TryFlip()
    {
        if (isFlipping)
            return;

        StartCoroutine(FlipRoutine());
    }

    private void SetExample(string exampleForeign, string exampleTranslation)
    {
        bool hasExample = !string.IsNullOrEmpty(exampleForeign) && !string.IsNullOrEmpty(exampleTranslation);

        ProjectUtilities.SetText(exampleForeignText, hasExample ? exampleForeign : string.Empty);
        ProjectUtilities.SetText(exampleTranslationText, hasExample ? exampleTranslation : string.Empty);

        if (exampleContainer != null)
        {
            exampleContainer.SetActive(hasExample);
        }
        else
        {
            exampleForeignText?.gameObject.SetActive(hasExample);
            exampleTranslationText?.gameObject.SetActive(hasExample);
        }
    }

    private IEnumerator FlipRoutine()
    {
        isFlipping = true;

        yield return ScaleX(1f, 0f, halfFlipDuration);

        isFront = !isFront;
        frontSide?.SetActive(isFront);
        backSide?.SetActive(!isFront);

        yield return ScaleX(0f, 1f, halfFlipDuration);

        PlayCurrentAudio();
        isFlipping = false;
    }

    private void PlayCurrentAudio()
    {
        AudioClip clip = isFront ? frontAudio : backAudio;
        if (clip == null || audioSource == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private IEnumerator ScaleX(float from, float to, float duration)
    {
        if (rectTransform == null)
            yield break;

        Vector3 scale = rectTransform.localScale;

        if (duration <= 0f)
        {
            rectTransform.localScale = new Vector3(to, scale.y, scale.z);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float x = Mathf.Lerp(from, to, easing.Evaluate(progress));
            rectTransform.localScale = new Vector3(x, scale.y, scale.z);
            yield return null;
        }

        rectTransform.localScale = new Vector3(to, scale.y, scale.z);
    }
}
