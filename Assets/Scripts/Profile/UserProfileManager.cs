using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Firestore;

public class UserProfileManager : MonoBehaviour
{
    private FirebaseFirestore database;

    private void Start()
    {
        database = FirebaseFirestore.DefaultInstance;
    }

    public void UpdateName(string newName)
    {
        StartCoroutine(UpdateNameAsync(newName));
    }

    private IEnumerator UpdateNameAsync(string newName)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("UID пользователя пустой");
            yield break;
        }

        DocumentReference docRef = database.Collection("users").Document(uid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", newName }
        };

        var updateTask = docRef.UpdateAsync(updates);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError("Ошибка обновления имени: " + updateTask.Exception);
        }
        else
        {
            References.userName = newName;
            Debug.Log("Имя обновлено: " + newName);
        }
    }

    public void UpdateAvatar(int newAvatarId)
    {
        StartCoroutine(UpdateAvatarAsync(newAvatarId));
    }

    private IEnumerator UpdateAvatarAsync(int newAvatarId)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("UID пользователя пустой");
            yield break;
        }

        DocumentReference docRef = database.Collection("users").Document(uid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "AvatarId", newAvatarId }
        };

        var updateTask = docRef.UpdateAsync(updates);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError("Ошибка обновления аватара: " + updateTask.Exception);
        }
        else
        {
            References.avatarId = newAvatarId;
            Debug.Log("Аватар обновлён: " + newAvatarId);
        }
    }

    public void AddExperience(int amount)
    {
        StartCoroutine(AddExperienceAsync(amount));
    }

    private IEnumerator AddExperienceAsync(int amount)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("UID пользователя пустой");
            yield break;
        }

        int newExperience = References.experience + amount;

        DocumentReference docRef = database.Collection("users").Document(uid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Experience", newExperience }
        };

        var updateTask = docRef.UpdateAsync(updates);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError("Ошибка обновления опыта: " + updateTask.Exception);
        }
        else
        {
            References.experience = newExperience;
            Debug.Log("Опыт обновлён: " + newExperience);
        }
    }

    public void CompleteLesson()
    {
        StartCoroutine(CompleteLessonAsync());
    }

    private IEnumerator CompleteLessonAsync()
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("UID пользователя пустой");
            yield break;
        }

        int newCompletedLessons = References.completedLessons + 1;

        DocumentReference docRef = database.Collection("users").Document(uid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "CompletedLessons", newCompletedLessons }
        };

        var updateTask = docRef.UpdateAsync(updates);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError("Ошибка обновления количества уроков: " + updateTask.Exception);
        }
        else
        {
            References.completedLessons = newCompletedLessons;
            Debug.Log("Пройдено уроков: " + newCompletedLessons);
        }
    }

    public void UpdateLanguageLevel(string newLevel)
    {
        StartCoroutine(UpdateLanguageLevelAsync(newLevel));
    }

    private IEnumerator UpdateLanguageLevelAsync(string newLevel)
    {
        string uid = References.userId;

        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogError("UID пользователя пустой");
            yield break;
        }

        DocumentReference docRef = database.Collection("users").Document(uid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "LanguageLevel", newLevel }
        };

        var updateTask = docRef.UpdateAsync(updates);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError("Ошибка обновления уровня языка: " + updateTask.Exception);
        }
        else
        {
            References.languageLevel = newLevel;
            Debug.Log("Уровень языка обновлён: " + newLevel);
        }
    }
}