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
    private int currentClipIndex = 0;
    private WritingData currentData;
    private int currentIndex = 0;

    void Awake()
    {
        model       = GetComponent<WordQuizModel>();
        view        = GetComponent<WordQuizView>();
        audioSource = GetComponent<AudioSource>();

        view.InputField.onEndEdit.AddListener(CheckAnswer);
    }

    void OnDestroy()
    {
        view.InputField.onEndEdit.RemoveListener(CheckAnswer);
    }

    public void LoadExercise(WritingData data)
    {
        currentData  = data;
        currentIndex = 0;
        ShowQuestion();
    }

    void ShowQuestion()
    {
        if (currentData == null || currentIndex >= currentData.questions.Count) return;

        model.LoadQuestion(currentData.questions[currentIndex]);
        currentClipIndex = 0;
        view.ResetView();
    }

    public void PlayWord()
    {
        var clips = model.WordClips;
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = playMode == PlayMode.Random
            ? clips[Random.Range(0, clips.Length)]
            : clips[currentClipIndex++ % clips.Length];

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    void CheckAnswer(string userInput)
    {
        if (string.IsNullOrWhiteSpace(userInput)) { view.ResetView(); return; }

        string user = Normalize(userInput);
        bool isCorrect = false;

        foreach (var word in model.CorrectWords)
        {
            if (Normalize(word) == user) { isCorrect = true; break; }
        }

        if (isCorrect)
        {
            view.SetCorrect();
            StartCoroutine(NextAfterDelay());
        }
        else
        {
            view.SetWrong();
        }
        
        ProgressManager.Instance.ShowNextButton();
    }

    IEnumerator NextAfterDelay()
    {
        yield return new WaitForSeconds(ProgressManager.Instance.nextExerciseDelay);

        currentIndex++;
        if (currentIndex < currentData.questions.Count)
            ShowQuestion();
        else
            ProgressManager.Instance.ShowNextButton();
    }

    string Normalize(string input) =>
        System.Text.RegularExpressions.Regex
            .Replace(input.ToLower().Trim(), @"\s+", " ");

    public void OnExerciseLeave()
    {
        view.ResetView();
    }
}