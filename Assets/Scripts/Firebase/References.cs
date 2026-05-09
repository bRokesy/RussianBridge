using UnityEngine;

public class References : MonoBehaviour
{
    public static string userId = "";
    public static string userName = "";
    public static string userEmail = "";

    public static string languageLevel = "A1";
    public static int avatarId = 0;
    public static int completedLessons = 0;
    public static int experience = 0;
    public static int streakDays = 0;

    public static void SetUserProfile(AppUserProfile profile)
    {
        userId = profile.Uid;
        userName = profile.Name;
        userEmail = profile.Email;

        languageLevel = profile.LanguageLevel;
        avatarId = profile.AvatarId;
        completedLessons = profile.CompletedLessons;
        experience = profile.Experience;
        streakDays = profile.StreakDays;
    }

    public static void Clear()
    {
        userId = "";
        userName = "";
        userEmail = "";

        languageLevel = "A1";
        avatarId = 0;
        completedLessons = 0;
        experience = 0;
        streakDays = 0;
    }
}