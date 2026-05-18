using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Firestore;
using UnityEngine;

public class UserProfileManager : MonoBehaviour
{
    private FirebaseFirestore database;

    private void Awake()
    {
        database = FirebaseFirestore.DefaultInstance;
    }

    public void UpdateName(string newName)
    {
        StartCoroutine(UpdateUserFieldAsync(
            FirestoreFields.Name,
            newName,
            "name",
            () => References.userName = newName));
    }

    public void UpdateAvatar(int newAvatarId)
    {
        StartCoroutine(UpdateUserFieldAsync(
            FirestoreFields.AvatarId,
            newAvatarId,
            "avatar",
            () => References.avatarId = newAvatarId));
    }

    public void AddExperience(int amount)
    {
        int newExperience = References.experience + amount;

        StartCoroutine(UpdateUserFieldAsync(
            FirestoreFields.Experience,
            newExperience,
            "experience",
            () => References.experience = newExperience));
    }

    public void CompleteLesson()
    {
        int newCompletedLessons = References.completedLessons + 1;

        StartCoroutine(UpdateUserFieldAsync(
            FirestoreFields.CompletedLessons,
            newCompletedLessons,
            "completed lessons",
            () => References.completedLessons = newCompletedLessons));
    }

    public void UpdateLanguageLevel(string newLevel)
    {
        StartCoroutine(UpdateUserFieldAsync(
            FirestoreFields.LanguageLevel,
            newLevel,
            "language level",
            () => References.languageLevel = newLevel));
    }

    private IEnumerator UpdateUserFieldAsync(string fieldName, object value, string logName, Action onSuccess)
    {
        if (!TryGetUserDocument(out DocumentReference docRef))
            yield break;

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { fieldName, value }
        };

        var updateTask = docRef.UpdateAsync(updates);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogError($"Failed to update {logName}: {updateTask.Exception}");
            yield break;
        }

        onSuccess?.Invoke();
        Debug.Log($"Updated {logName}: {value}");
    }

    private bool TryGetUserDocument(out DocumentReference docRef)
    {
        docRef = null;

        if (string.IsNullOrEmpty(References.userId))
        {
            Debug.LogError("User UID is empty.");
            return false;
        }

        if (database == null)
            database = FirebaseFirestore.DefaultInstance;

        if (database == null)
        {
            Debug.LogError("Firestore is not initialized.");
            return false;
        }

        docRef = database.Collection(FirestoreCollections.Users).Document(References.userId);
        return true;
    }
}
