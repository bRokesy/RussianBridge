using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LessonButton : MonoBehaviour
{
    [SerializeField] private Sprite doneLessonSprite;

    private Button button;
    private Image image;
    private int lessonNumber;
    private bool isOpening;

    private void Awake()
    {
        ResolveComponents();
        lessonNumber = GetLessonNumber();
    }

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        ResolveComponents();

        if (lessonNumber <= 0)
            lessonNumber = GetLessonNumber();

        RefreshState();
    }

    public void OnClick()
    {
        if (!IsUnlocked())
        {
            Debug.Log($"LessonButton: lesson {lessonNumber} is locked or disabled.");
            return;
        }

        if (isOpening)
            return;

        StartCoroutine(OpenLesson());
    }

    private IEnumerator OpenLesson()
    {
        isOpening = true;

        ProgressManager progressManager = ProgressManager.Instance;
        if (progressManager == null)
            progressManager = FindFirstObjectByType<ProgressManager>();

        if (progressManager == null)
        {
            Debug.LogError("LessonButton: ProgressManager was not found in the scene.");
            isOpening = false;
            yield break;
        }

        yield return progressManager.EnsureLessonsLoaded();

        LessonData selectedLesson = ResolveLessonData(progressManager);
        if (selectedLesson == null)
        {
            Debug.LogError($"LessonButton: lesson data is not assigned for {gameObject.name}.");
            isOpening = false;
            yield break;
        }

        progressManager.SetActiveLesson(selectedLesson);
        SceneManager.LoadScene(SceneNames.LessonScene);
    }

    private void ResolveComponents()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (image == null)
            image = GetComponent<Image>();
    }

    private LessonData ResolveLessonData(ProgressManager progressManager = null)
    {
        if (progressManager == null)
            progressManager = ProgressManager.Instance;

        return progressManager?.GetLessonByNumber(lessonNumber);
    }

    public void RefreshState()
    {
        if (lessonNumber <= 0)
            return;

        if (button != null)
            button.interactable = IsUnlocked();

        if (IsCompleted() && image != null && doneLessonSprite != null)
            image.sprite = doneLessonSprite;
    }

    private bool IsUnlocked()
    {
        ProgressManager progressManager = ProgressManager.Instance;
        if (progressManager == null)
            progressManager = FindFirstObjectByType<ProgressManager>();

        return progressManager != null
            ? progressManager.IsLessonUnlocked(lessonNumber)
            : lessonNumber <= 1 || References.completedLessons >= lessonNumber - 1;
    }

    private bool IsCompleted()
    {
        return References.completedLessons >= lessonNumber;
    }

    private int GetLessonNumber()
    {
        string objectName = gameObject.name;
        var digits = new StringBuilder();

        foreach (char character in objectName)
        {
            if (char.IsDigit(character))
                digits.Append(character);
        }

        if (int.TryParse(digits.ToString(), out int parsedNumber))
            return parsedNumber;

        Debug.LogWarning($"LessonButton: failed to detect lesson number from object name '{objectName}'.");
        return 0;
    }
}
