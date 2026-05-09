using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProfileUI : MonoBehaviour
{
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
        if (nameText != null)
            nameText.text = References.userName;

        if (emailText != null)
            emailText.text = References.userEmail;

        if (languageLevelText != null)
            languageLevelText.text = "Уровень: " + References.languageLevel;

        if (completedLessonsText != null)
            completedLessonsText.text = "Пройдено уроков: " + References.completedLessons;

        if (experienceText != null)
            experienceText.text = "Опыт: " + References.experience;

        if (streakDaysText != null)
            streakDaysText.text = "Дней подряд: " + References.streakDays;

        if (avatarImage != null && avatarSprites != null && avatarSprites.Length > 0)
        {
            int id = References.avatarId;

            if (id >= 0 && id < avatarSprites.Length)
            {
                avatarImage.sprite = avatarSprites[id];
            }
        }
    }
}