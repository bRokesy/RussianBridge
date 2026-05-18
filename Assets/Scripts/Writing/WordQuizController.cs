using System.Collections;
using UnityEngine;

[RequireComponent(typeof(WordQuizModel))]
[RequireComponent(typeof(WordQuizView))]
[RequireComponent(typeof(AudioSource))]
public class WordQuizController : MonoBehaviour, IExerciseController
{
    public enum PlayMode { Random, Sequential }

    [SerializeField] private PlayMode playMode = PlayMode.Random;

    private WordQuizModel model;
    private WordQuizView view;
    private AudioSource audioSource;
    private WritingData currentData;
    private int currentClipIndex;
    private int currentIndex;

    private void Awake()
    {
        model = GetComponent<WordQuizModel>();
        view = GetComponent<WordQuizView>();
        audioSource = GetComponent<AudioSource>();

        if (view.InputField != null)
            view.InputField.onEndEdit.AddListener(CheckAnswer);
    }

    private void OnDestroy()
    {
        if (view != null && view.InputField != null)
            view.InputField.onEndEdit.RemoveListener(CheckAnswer);
    }

    public void LoadExercise(WritingData data)
    {
        currentData = data;
        currentIndex = 0;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (currentData == null || currentData.questions == null || currentIndex >= currentData.questions.Count)
            return;

        model.LoadQuestion(currentData.questions[currentIndex]);
        currentClipIndex = 0;
        view.ResetView();
    }

    public void PlayWord()
    {
        AudioClip[] clips = model.WordClips;
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = playMode == PlayMode.Random
            ? clips[Random.Range(0, clips.Length)]
            : clips[currentClipIndex++ % clips.Length];

        if (clip == null)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void CheckAnswer(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput))
        {
            view.ResetView();
            return;
        }

        bool isCorrect = IsCorrectAnswer(userInput);

        if (isCorrect)
        {
            view.SetCorrect();
            StartCoroutine(NextAfterDelay());
        }
        else
        {
            view.SetWrong();
        }

        ProgressManager.Instance?.ShowNextButton();
    }

    private bool IsCorrectAnswer(string userInput)
    {
        string[] correctWords = model.CorrectWords;
        if (correctWords == null)
            return false;

        foreach (string word in correctWords)
        {
            if (ProjectUtilities.SameAnswer(word, userInput))
                return true;
        }

        return false;
    }

    private IEnumerator NextAfterDelay()
    {
        float delay = ProgressManager.Instance != null ? ProgressManager.Instance.nextExerciseDelay : 0f;
        yield return new WaitForSeconds(delay);

        currentIndex++;

        if (currentData != null && currentData.questions != null && currentIndex < currentData.questions.Count)
            ShowQuestion();
        else
            ProgressManager.Instance?.ShowNextButton();
    }

    public void OnExerciseLeave()
    {
        view?.ResetView();
    }
}
