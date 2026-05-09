using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lesson Data/Writing Data", fileName = "NewWritingData")]
public class WritingData : ScriptableObject
{
    [System.Serializable]
    public class Question
    {
        [Tooltip("Все допустимые варианты правильного ответа")]
        public string[] correctWords;

        [Tooltip("Аудиоклипы слова")]
        public AudioClip[] wordClips;
    }

    [Header("Display")]
    public string lessonTitle;

    [Header("Questions")]
    public List<Question> questions;
}