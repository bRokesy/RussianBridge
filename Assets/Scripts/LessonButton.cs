using UnityEngine;
using UnityEngine.SceneManagement;

public class LessonButton : MonoBehaviour
{
    public LessonData lessonData;

    public void OnClick()
    {
        ProgressManager.Instance.lessons.Clear();
        ProgressManager.Instance.lessons.Add(lessonData);

        SceneManager.LoadScene("LessonScene");
    }
}
