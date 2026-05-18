using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LessonButton : MonoBehaviour
{
    public LessonData lessonData;
    [SerializeField] private Sprite doneLessonSprite;

    private Button button;
    private Image image;
    private int lessonNumber;

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
            Debug.Log($"LessonButton: урок {lessonNumber} закрыт. Сначала пройдите предыдущий урок.");
            return;
        }

        LessonData selectedLesson = ResolveLessonData();
        if (selectedLesson == null)
        {
            Debug.LogError($"LessonButton: lessonData не назначен для {gameObject.name}.");
            return;
        }

        ProgressManager progressManager = ProgressManager.Instance;
        if (progressManager == null)
        {
            Debug.LogError("LessonButton: ProgressManager не найден в сцене.");
            return;
        }

        if (progressManager.lessons == null)
            progressManager.lessons = new List<LessonData>();

        progressManager.lessons.Clear();
        progressManager.lessons.Add(selectedLesson);

        SceneManager.LoadScene(SceneNames.LessonScene);
    }

    private void ResolveComponents()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (image == null)
            image = GetComponent<Image>();
    }

    private LessonData ResolveLessonData()
    {
        if (lessonData != null)
            return lessonData;

        ProgressManager progressManager = ProgressManager.Instance;
        if (progressManager == null || progressManager.lessons == null)
            return null;

        return lessonNumber > 0 && progressManager.lessons.Count >= lessonNumber
            ? progressManager.lessons[lessonNumber - 1]
            : null;
    }

    private void RefreshState()
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
        return lessonNumber <= 1 || References.completedLessons >= lessonNumber - 1;
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

        Debug.LogWarning($"LessonButton: не удалось определить номер урока из имени объекта '{objectName}'.");
        return 0;
    }
}
