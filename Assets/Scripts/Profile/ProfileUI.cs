using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
    private const string EmptyLessonText = "не выбран";

    [Header("Texts")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text emailText;
    [SerializeField] private TMP_Text languageLevelText;
    [SerializeField] private TMP_Text completedLessonsText;
    [SerializeField] private TMP_Text experienceText;
    [SerializeField] private TMP_Text streakDaysText;

    [Header("Avatar")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private Sprite[] avatarSprites;

    private void Start()
    {
        RefreshProfileUI();
    }

    public void RefreshProfileUI()
    {
        string currentLesson = string.IsNullOrEmpty(References.currentLesson)
            ? EmptyLessonText
            : References.currentLesson;

        ProjectUtilities.SetText(nameText, References.userName);
        ProjectUtilities.SetText(emailText, References.userEmail);
        ProjectUtilities.SetText(languageLevelText, "Текущий урок: " + currentLesson);
        ProjectUtilities.SetText(completedLessonsText, "Пройдено уроков: " + References.completedLessons);
        ProjectUtilities.SetText(experienceText, "Опыт: " + References.experience);
        ProjectUtilities.SetText(streakDaysText, "Дней подряд: " + References.streakDays);

        RefreshAvatar();
    }

    private void RefreshAvatar()
    {
        if (avatarImage == null || avatarSprites == null || avatarSprites.Length == 0)
            return;

        int id = References.avatarId;
        if (id >= 0 && id < avatarSprites.Length)
            avatarImage.sprite = avatarSprites[id];
    }
}
