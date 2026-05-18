using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Writing Data", fileName = "NewWritingData")]
public class WritingData : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        [Tooltip("Все допустимые варианты правильного ответа")]
        public string[] correctWords = System.Array.Empty<string>();

        [Tooltip("Аудиоклипы слова")]
        public AudioClip[] wordClips = System.Array.Empty<AudioClip>();
    }

    [Header("Display")]
    public string lessonTitle;

    [Header("Questions")]
    public List<Question> questions = new List<Question>();
}
