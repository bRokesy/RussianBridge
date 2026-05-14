using Firebase.Firestore;

[FirestoreData]
public class AppUserProfile
{
    [FirestoreProperty]
    public string Uid { get; set; }

    [FirestoreProperty]
    public string Email { get; set; }

    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string LanguageLevel { get; set; }

    [FirestoreProperty]
    public int AvatarId { get; set; }

    [FirestoreProperty]
    public int CompletedLessons { get; set; }

    [FirestoreProperty]
    public int Experience { get; set; }

    [FirestoreProperty]
    public int StreakDays { get; set; }

    [FirestoreProperty]
    public string CurrentLesson { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty]
    public Timestamp LastLogin { get; set; }
}
