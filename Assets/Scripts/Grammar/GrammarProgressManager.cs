using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GrammarProgressManager : MonoBehaviour
{
    public enum ExerciseType { FillBlank, MakeSentence }

    [System.Serializable]
    public class LessonConfig
    {
        public string lessonName;
        public ExerciseType exerciseType;
        public List<FillBlankData> fillBlankExercises = new List<FillBlankData>();
        public List<MakeSentenceData> makeSentenceExercises = new List<MakeSentenceData>();
    }

    [Header("Lessons (по порядку)")]
    public List<LessonConfig> lessons = new List<LessonConfig>();

    [Header("Managers")]
    public FillBlankManager fillBlankManager;
    public MakeSentenceManager makeSentenceManager;

    [Header("UI Panels")]
    public GameObject fillBlankPanel;
    public GameObject makeSentencePanel;

    [Header("Navigation UI")]
    public TextMeshProUGUI progressLabel;
    public Button nextButton;
    public Button prevButton;

    public static GrammarProgressManager instance;

    private int currentLesson;
    private int currentExercise;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        nextButton?.onClick.AddListener(NextExercise);
        prevButton?.onClick.AddListener(PrevExercise);

        LoadCurrent();
    }

    private void OnDestroy()
    {
        nextButton?.onClick.RemoveListener(NextExercise);
        prevButton?.onClick.RemoveListener(PrevExercise);

        if (instance == this)
            instance = null;
    }

    public void NextExercise()
    {
        if (!TryGetCurrentLesson(out LessonConfig lesson))
            return;

        int total = GetExerciseCount(lesson);

        if (currentExercise < total - 1)
        {
            currentExercise++;
        }
        else if (currentLesson < lessons.Count - 1)
        {
            currentLesson++;
            currentExercise = 0;
        }
        else
        {
            OnAllLessonsComplete();
            return;
        }

        LoadCurrent();
    }

    public void PrevExercise()
    {
        if (lessons == null || lessons.Count == 0)
            return;

        if (currentExercise > 0)
        {
            currentExercise--;
        }
        else if (currentLesson > 0)
        {
            currentLesson--;
            currentExercise = Mathf.Max(0, GetExerciseCount(lessons[currentLesson]) - 1);
        }

        LoadCurrent();
    }

    public void ResetProgress()
    {
        currentLesson = 0;
        currentExercise = 0;
        LoadCurrent();
    }

    private void LoadCurrent()
    {
        if (!TryGetCurrentLesson(out LessonConfig lesson))
            return;

        int total = GetExerciseCount(lesson);
        if (total <= 0)
        {
            Debug.LogWarning($"GrammarProgressManager: в уроке {lesson.lessonName} нет упражнений.");
            return;
        }

        currentExercise = Mathf.Clamp(currentExercise, 0, total - 1);

        UpdateProgressLabel(lesson, total);
        UpdateNavButtons(total);
        LoadExercise(lesson);
    }

    private void LoadExercise(LessonConfig lesson)
    {
        switch (lesson.exerciseType)
        {
            case ExerciseType.FillBlank:
                ShowPanel(fillBlankPanel, makeSentencePanel);
                fillBlankManager?.LoadExercise(lesson.fillBlankExercises[currentExercise]);
                break;
            case ExerciseType.MakeSentence:
                ShowPanel(makeSentencePanel, fillBlankPanel);
                makeSentenceManager?.LoadExercise(lesson.makeSentenceExercises[currentExercise]);
                break;
        }
    }

    private bool TryGetCurrentLesson(out LessonConfig lesson)
    {
        lesson = null;

        if (lessons == null || lessons.Count == 0)
        {
            Debug.LogWarning("GrammarProgressManager: список lessons пустой.");
            return false;
        }

        currentLesson = Mathf.Clamp(currentLesson, 0, lessons.Count - 1);
        lesson = lessons[currentLesson];

        return lesson != null;
    }

    private static int GetExerciseCount(LessonConfig lesson)
    {
        return lesson.exerciseType == ExerciseType.FillBlank
            ? lesson.fillBlankExercises?.Count ?? 0
            : lesson.makeSentenceExercises?.Count ?? 0;
    }

    private static void ShowPanel(GameObject show, GameObject hide)
    {
        show?.SetActive(true);
        hide?.SetActive(false);
    }

    private void UpdateProgressLabel(LessonConfig lesson, int total)
    {
        if (progressLabel != null)
            progressLabel.text = $"{lesson.lessonName}  •  {currentExercise + 1} / {total}";
    }

    private void UpdateNavButtons(int total)
    {
        bool isFirst = currentLesson == 0 && currentExercise == 0;
        bool isLast = currentLesson == lessons.Count - 1 && currentExercise == total - 1;

        if (prevButton != null)
            prevButton.interactable = !isFirst;

        if (nextButton != null)
            nextButton.interactable = !isLast;
    }

    private void OnAllLessonsComplete()
    {
        if (progressLabel != null)
            progressLabel.text = "Все уроки пройдены!";

        if (nextButton != null)
            nextButton.interactable = false;

        Debug.Log("GrammarProgressManager: все уроки завершены.");
    }
}
