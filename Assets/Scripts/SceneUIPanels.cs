using UnityEngine;

public class SceneUIPanels : MonoBehaviour
{
    [Header("Назначь только те панели, которые есть в этой сцене")]
    public GameObject fillBlankPanel;
    public GameObject makeSentencePanel;
    public GameObject translatePanel;
    public GameObject writingPanel;
    public GameObject flashcardsPanel;

    void Awake()
    {
        SetAll(false);
    }

    void Start()
    {
        // Передать ссылки в ProgressManager
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.RegisterPanels(this);
    }

    public void ShowOnly(LessonData.ExerciseType type)
    {
        SetAll(false);

        switch (type)
        {
            case LessonData.ExerciseType.FillBlank:
                fillBlankPanel?.SetActive(true);    break;
            case LessonData.ExerciseType.MakeSentence:
                makeSentencePanel?.SetActive(true); break;
            case LessonData.ExerciseType.Translate:
                translatePanel?.SetActive(true);    break;
            case LessonData.ExerciseType.Writing:
                writingPanel?.SetActive(true);      break;
            case LessonData.ExerciseType.Flashcards:
                flashcardsPanel?.SetActive(true);   break;
        }
    }

    void SetAll(bool state)
    {
        if (fillBlankPanel) fillBlankPanel?.SetActive(state);
        if (makeSentencePanel) makeSentencePanel?.SetActive(state);
        if (translatePanel) translatePanel?.SetActive(state);
        if (writingPanel) writingPanel?.SetActive(state);
        if (flashcardsPanel) flashcardsPanel?.SetActive(state);
    }
}