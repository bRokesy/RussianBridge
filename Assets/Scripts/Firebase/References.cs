using UnityEngine;

public class References : MonoBehaviour
{
    private const string DefaultLanguageLevel = "A1";

    public static string userId = "";
    public static string userName = "";
    public static string userEmail = "";

    public static string languageLevel = DefaultLanguageLevel;
    public static int avatarId;
    public static int completedLessons;
    public static int experience;
    public static int streakDays;
    public static string currentLesson = "";

    public static void SetUserProfile(AppUserProfile profile)
    {
        if (profile == null)
        {
            Clear();
            return;
        }

        userId = profile.Uid;
        userName = profile.Name;
        userEmail = profile.Email;
        languageLevel = string.IsNullOrEmpty(profile.LanguageLevel)
            ? DefaultLanguageLevel
            : profile.LanguageLevel;
        avatarId = profile.AvatarId;
        completedLessons = profile.CompletedLessons;
        experience = profile.Experience;
        streakDays = profile.StreakDays;
        currentLesson = profile.CurrentLesson ?? "";
    }

    public static void Clear()
    {
        userId = "";
        userName = "";
        userEmail = "";
        languageLevel = DefaultLanguageLevel;
        avatarId = 0;
        completedLessons = 0;
        experience = 0;
        streakDays = 0;
        currentLesson = "";
    }
}
