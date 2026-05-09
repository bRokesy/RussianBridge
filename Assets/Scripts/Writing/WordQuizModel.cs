using UnityEngine;

public class WordQuizModel : MonoBehaviour
{
    private WritingData.Question currentQuestion;

    public string[] CorrectWords => currentQuestion?.correctWords;
    public AudioClip[] WordClips => currentQuestion?.wordClips;

    public void LoadQuestion(WritingData.Question question)
    {
        currentQuestion = question;
    }
}