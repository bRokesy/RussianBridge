using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirebaseAuthManager : MonoBehaviour
{
    private const string DefaultUserName = "User";
    private const string DefaultLanguageLevel = "A1";

    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;
    public FirebaseFirestore database;

    [Space]
    [Header("Login")]
    public TMP_InputField inputLoginEmail;
    public TMP_InputField inputLoginPassword;

    [Space]
    [Header("Registration")]
    public TMP_InputField inputRegName;
    public TMP_InputField inputRegEmail;
    public TMP_InputField inputRegPassword;
    public TMP_InputField inputRegConfirmPassword;

    private void Start()
    {
        StartCoroutine(CheckAndFixDependenciesAsync());
    }

    private IEnumerator CheckAndFixDependenciesAsync()
    {
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => dependencyTask.IsCompleted);

        dependencyStatus = dependencyTask.Result;

        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError("Firebase недоступен: " + dependencyStatus);
            OpenLoginPanel();
            yield break;
        }

        InitializeFirebase();

        yield return new WaitForEndOfFrame();
        yield return StartCoroutine(CheckForAutoLogin());
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        database = FirebaseFirestore.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, EventArgs.Empty);

        Debug.Log("Firebase успешно инициализирован");
    }

    private IEnumerator CheckForAutoLogin()
    {
        if (user == null)
        {
            OpenLoginPanel();
            yield break;
        }

        var reloadUserTask = user.ReloadAsync();
        yield return new WaitUntil(() => reloadUserTask.IsCompleted);

        if (reloadUserTask.Exception != null)
        {
            Debug.LogWarning("Не удалось обновить текущего пользователя: " + reloadUserTask.Exception);
            OpenLoginPanel();
            yield break;
        }

        AutoLogin();
    }

    private void AutoLogin()
    {
        if (user == null)
        {
            OpenLoginPanel();
            return;
        }

        if (user.IsEmailVerified)
            StartCoroutine(LoadUserProfileAndOpenMainScene(user));
        else
            SendEmailForVerification();
    }

    private void AuthStateChanged(object sender, EventArgs eventArgs)
    {
        if (auth == null || auth.CurrentUser == user)
            return;

        bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

        if (!signedIn && user != null)
            Debug.Log("SignedOut: " + user.UserId);

        user = auth.CurrentUser;

        if (signedIn)
            Debug.Log("SignedIn: " + user.UserId);
    }

    public void Login()
    {
        if (!IsFirebaseReady()) return;

        string email = inputLoginEmail != null ? inputLoginEmail.text : string.Empty;
        string password = inputLoginPassword != null ? inputLoginPassword.text : string.Empty;

        StartCoroutine(LoginAsync(email, password));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email.Trim(), password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError(loginTask.Exception);
            Debug.LogError(BuildLoginErrorMessage(loginTask.Exception));
            yield break;
        }

        user = loginTask.Result.User;
        Debug.Log("Пользователь вошёл: " + user.DisplayName);

        if (user.IsEmailVerified)
            StartCoroutine(LoadUserProfileAndOpenMainScene(user));
        else
            SendEmailForVerification();
    }

    public void Register()
    {
        if (!IsFirebaseReady()) return;

        string name = inputRegName != null ? inputRegName.text : string.Empty;
        string email = inputRegEmail != null ? inputRegEmail.text : string.Empty;
        string password = inputRegPassword != null ? inputRegPassword.text : string.Empty;
        string confirmPassword = inputRegConfirmPassword != null ? inputRegConfirmPassword.text : string.Empty;

        StartCoroutine(RegisterAsync(name, email, password, confirmPassword));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        name = name.Trim();
        email = email.Trim();

        if (!TryValidateRegistration(name, email, password, confirmPassword))
            yield break;

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError(registerTask.Exception);
            Debug.LogError(BuildRegistrationErrorMessage(registerTask.Exception));
            yield break;
        }

        user = registerTask.Result.User;

        bool displayNameUpdated = false;
        yield return StartCoroutine(UpdateFirebaseDisplayName(user, name, success => displayNameUpdated = success));

        if (!displayNameUpdated)
            yield break;

        yield return StartCoroutine(CreateUserProfileInDatabase(user, name));

        if (user.IsEmailVerified)
            OpenLoginPanel();
        else
            SendEmailForVerification();
    }

    private bool TryValidateRegistration(string name, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(name))
            return LogValidationError("Имя не может быть пустым");

        if (string.IsNullOrWhiteSpace(email))
            return LogValidationError("Email не может быть пустым");

        if (string.IsNullOrEmpty(password))
            return LogValidationError("Пароль не может быть пустым");

        if (password != confirmPassword)
            return LogValidationError("Пароли не совпадают");

        return true;
    }

    private static bool LogValidationError(string message)
    {
        Debug.LogError(message);
        return false;
    }

    private IEnumerator UpdateFirebaseDisplayName(FirebaseUser firebaseUser, string name, Action<bool> onComplete)
    {
        UserProfile firebaseProfile = new UserProfile
        {
            DisplayName = name
        };

        var updateProfileTask = firebaseUser.UpdateUserProfileAsync(firebaseProfile);
        yield return new WaitUntil(() => updateProfileTask.IsCompleted);

        if (updateProfileTask.Exception == null)
        {
            onComplete?.Invoke(true);
            yield break;
        }

        Debug.LogError(updateProfileTask.Exception);

        var deleteTask = firebaseUser.DeleteAsync();
        yield return new WaitUntil(() => deleteTask.IsCompleted);

        Debug.LogError("Не удалось обновить имя пользователя. Аккаунт удалён.");
        onComplete?.Invoke(false);
    }

    private IEnumerator CreateUserProfileInDatabase(FirebaseUser firebaseUser, string name)
    {
        AppUserProfile profile = new AppUserProfile
        {
            Uid = firebaseUser.UserId,
            Email = firebaseUser.Email,
            Name = name,
            LanguageLevel = DefaultLanguageLevel,
            AvatarId = 0,
            CompletedLessons = 0,
            Experience = 0,
            StreakDays = 0,
            CurrentLesson = string.Empty,
            CreatedAt = Timestamp.GetCurrentTimestamp(),
            LastLogin = Timestamp.GetCurrentTimestamp()
        };

        DocumentReference docRef = GetUserDocument(firebaseUser.UserId);
        var setProfileTask = docRef.SetAsync(profile);

        yield return new WaitUntil(() => setProfileTask.IsCompleted);

        if (setProfileTask.Exception != null)
            Debug.LogError("Ошибка создания профиля в Firestore: " + setProfileTask.Exception);
        else
            Debug.Log("Профиль пользователя создан в Firestore");
    }

    private IEnumerator LoadUserProfileAndOpenMainScene(FirebaseUser firebaseUser)
    {
        yield return StartCoroutine(UpdateLastLogin(firebaseUser.UserId));

        DocumentReference docRef = GetUserDocument(firebaseUser.UserId);
        var getProfileTask = docRef.GetSnapshotAsync();

        yield return new WaitUntil(() => getProfileTask.IsCompleted);

        if (getProfileTask.Exception != null)
        {
            Debug.LogError("Ошибка загрузки профиля: " + getProfileTask.Exception);
            yield break;
        }

        DocumentSnapshot snapshot = getProfileTask.Result;

        if (!snapshot.Exists)
        {
            Debug.LogWarning("Профиль не найден. Создаю новый профиль.");

            string name = string.IsNullOrEmpty(firebaseUser.DisplayName)
                ? DefaultUserName
                : firebaseUser.DisplayName;

            yield return StartCoroutine(CreateUserProfileInDatabase(firebaseUser, name));
            yield return StartCoroutine(LoadUserProfileAndOpenMainScene(firebaseUser));
            yield break;
        }

        AppUserProfile profile = snapshot.ConvertTo<AppUserProfile>();
        References.SetUserProfile(profile);

        Debug.Log("Профиль загружен");
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    private IEnumerator UpdateLastLogin(string uid)
    {
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { FirestoreFields.LastLogin, Timestamp.GetCurrentTimestamp() }
        };

        var updateTask = GetUserDocument(uid).SetAsync(updates, SetOptions.MergeAll);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
            Debug.LogWarning("Не удалось обновить LastLogin: " + updateTask.Exception);
    }

    public void SendEmailForVerification()
    {
        StartCoroutine(SendEmailForVerificationAsync());
    }

    private IEnumerator SendEmailForVerificationAsync()
    {
        if (user == null)
            yield break;

        var sendEmailTask = user.SendEmailVerificationAsync();
        yield return new WaitUntil(() => sendEmailTask.IsCompleted);

        if (sendEmailTask.Exception != null)
        {
            string errorMessage = BuildEmailVerificationErrorMessage(sendEmailTask.Exception);
            UIManager.Instance?.ShowVerificationResponce(false, user.Email, errorMessage);
            yield break;
        }

        Debug.Log("Письмо подтверждения отправлено");
        UIManager.Instance?.ShowVerificationResponce(true, user.Email, null);
    }

    public void Logout()
    {
        References.Clear();

        if (auth != null)
            auth.SignOut();

        OpenLoginPanel();
    }

    private bool IsFirebaseReady()
    {
        if (auth != null && database != null)
            return true;

        Debug.LogError("Firebase ещё не инициализирован.");
        return false;
    }

    private DocumentReference GetUserDocument(string uid)
    {
        if (database == null)
            database = FirebaseFirestore.DefaultInstance;

        return database.Collection(FirestoreCollections.Users).Document(uid);
    }

    private static string BuildLoginErrorMessage(Exception exception)
    {
        switch (GetAuthError(exception))
        {
            case AuthError.InvalidEmail:
                return "Ошибка входа: неверный Email";
            case AuthError.WrongPassword:
                return "Ошибка входа: неверный пароль";
            case AuthError.MissingEmail:
                return "Ошибка входа: Email не введён";
            case AuthError.MissingPassword:
                return "Ошибка входа: пароль не введён";
            default:
                return "Ошибка входа: не удалось войти";
        }
    }

    private static string BuildRegistrationErrorMessage(Exception exception)
    {
        switch (GetAuthError(exception))
        {
            case AuthError.InvalidEmail:
                return "Ошибка регистрации: неверный Email";
            case AuthError.WeakPassword:
                return "Ошибка регистрации: слабый пароль";
            case AuthError.MissingEmail:
                return "Ошибка регистрации: Email не введён";
            case AuthError.MissingPassword:
                return "Ошибка регистрации: пароль не введён";
            case AuthError.EmailAlreadyInUse:
                return "Ошибка регистрации: этот Email уже используется";
            default:
                return "Ошибка регистрации: регистрация не удалась";
        }
    }

    private static string BuildEmailVerificationErrorMessage(Exception exception)
    {
        switch (GetAuthError(exception))
        {
            case AuthError.Cancelled:
                return "отменено";
            case AuthError.TooManyRequests:
                return "слишком много запросов";
            case AuthError.InvalidRecipientEmail:
                return "неверный Email получателя";
            default:
                return "не удалось отправить письмо";
        }
    }

    private static AuthError GetAuthError(Exception exception)
    {
        FirebaseException firebaseException = exception?.GetBaseException() as FirebaseException;
        return firebaseException != null ? (AuthError)firebaseException.ErrorCode : (AuthError)(-1);
    }

    private static void OpenLoginPanel()
    {
        UIManager.Instance?.OpenLoginPanel();
    }

    private void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= AuthStateChanged;
    }
}
