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
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        lessonNumber = GetLessonNumber();
    }

    private void Start()
    {
        RefreshState();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (image == null) image = GetComponent<Image>();
        if (lessonNumber <= 0) lessonNumber = GetLessonNumber();

        RefreshState();
    }

    public void OnClick()
    {
        if (!IsUnlocked())
        {
            Debug.Log($"LessonButton: урок {lessonNumber} закрыт. Сначала пройдите предыдущий урок.");
            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogError("LessonButton: ProgressManager не найден в сцене.");
            return;
        }

        LessonData selectedLesson = lessonData;
        if (selectedLesson == null && lessonNumber > 0 && ProgressManager.Instance.lessons != null && ProgressManager.Instance.lessons.Count >= lessonNumber)
            selectedLesson = ProgressManager.Instance.lessons[lessonNumber - 1];

        if (selectedLesson == null)
        {
            Debug.LogError($"LessonButton: lessonData не назначен для {gameObject.name}.");
            return;
        }

        ProgressManager.Instance.lessons.Clear();
        ProgressManager.Instance.lessons.Add(selectedLesson);

        SceneManager.LoadScene("LessonScene");
    }

    private void RefreshState()
    {
        if (lessonNumber <= 0) return;

        bool completed = IsCompleted();
        bool unlocked = IsUnlocked();

        if (button != null)
            button.interactable = unlocked;

        if (completed && image != null && doneLessonSprite != null)
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
        string digits = "";

        for (int i = 0; i < objectName.Length; i++)
        {
            if (char.IsDigit(objectName[i]))
                digits += objectName[i];
        }

        if (int.TryParse(digits, out int parsedNumber))
            return parsedNumber;

        Debug.LogWarning($"LessonButton: не удалось определить номер урока из имени объекта '{objectName}'.");
        return 0;
    }
}
