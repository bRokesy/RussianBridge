using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;

public class FirebaseAuthManager : MonoBehaviour
{
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

        if (dependencyStatus == DependencyStatus.Available)
        {
            InitializeFirebase();

            yield return new WaitForEndOfFrame();

            StartCoroutine(CheckForAutoLogin());
        }
        else
        {
            Debug.LogError("Firebase недоступен: " + dependencyStatus);
        }
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        database = FirebaseFirestore.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);

        Debug.Log("Firebase успешно инициализирован");
    }

    private IEnumerator CheckForAutoLogin()
    {
        if (user != null)
        {
            var reloadUserTask = user.ReloadAsync();

            yield return new WaitUntil(() => reloadUserTask.IsCompleted);

            AutoLogin();
        }
        else
        {
            UIManager.Instance.OpenLoginPanel();
        }
    }

    private void AutoLogin()
    {
        if (user != null)
        {
            if (user.IsEmailVerified)
            {
                StartCoroutine(LoadUserProfileAndOpenMainScene(user));
            }
            else
            {
                SendEmailForVerification();
            }
        }
        else
        {
            UIManager.Instance.OpenLoginPanel();
        }
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("SignedOut: " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("SignedIn: " + user.UserId);
            }
        }
    }

    public void Login()
    {
        StartCoroutine(LoginAsync(inputLoginEmail.text, inputLoginPassword.text));
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogError(loginTask.Exception);

            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            string failedMessage = "Ошибка входа: ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "неверный Email";
                    break;

                case AuthError.WrongPassword:
                    failedMessage += "неверный пароль";
                    break;

                case AuthError.MissingEmail:
                    failedMessage += "Email не введён";
                    break;

                case AuthError.MissingPassword:
                    failedMessage += "пароль не введён";
                    break;

                default:
                    failedMessage += "не удалось войти";
                    break;
            }

            Debug.LogError(failedMessage);
        }
        else
        {
            user = loginTask.Result.User;

            Debug.Log("Пользователь вошёл: " + user.DisplayName);

            if (user.IsEmailVerified)
            {
                StartCoroutine(LoadUserProfileAndOpenMainScene(user));
            }
            else
            {
                SendEmailForVerification();
            }
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(
            inputRegName.text,
            inputRegEmail.text,
            inputRegPassword.text,
            inputRegConfirmPassword.text
        ));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrEmpty(name))
        {
            Debug.LogError("Имя не может быть пустым");
            yield break;
        }

        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("Email не может быть пустым");
            yield break;
        }

        if (string.IsNullOrEmpty(password))
        {
            Debug.LogError("Пароль не может быть пустым");
            yield break;
        }

        if (password != confirmPassword)
        {
            Debug.LogError("Пароли не совпадают");
            yield break;
        }

        var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.Exception != null)
        {
            Debug.LogError(registerTask.Exception);

            FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;
            AuthError authError = (AuthError)firebaseException.ErrorCode;

            string failedMessage = "Ошибка регистрации: ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "неверный Email";
                    break;

                case AuthError.WeakPassword:
                    failedMessage += "слабый пароль";
                    break;

                case AuthError.MissingEmail:
                    failedMessage += "Email не введён";
                    break;

                case AuthError.MissingPassword:
                    failedMessage += "пароль не введён";
                    break;

                case AuthError.EmailAlreadyInUse:
                    failedMessage += "этот Email уже используется";
                    break;

                default:
                    failedMessage += "регистрация не удалась";
                    break;
            }

            Debug.LogError(failedMessage);
        }
        else
        {
            user = registerTask.Result.User;

            Firebase.Auth.UserProfile firebaseProfile = new Firebase.Auth.UserProfile
            {
                DisplayName = name
            };

            var updateProfileTask = user.UpdateUserProfileAsync(firebaseProfile);

            yield return new WaitUntil(() => updateProfileTask.IsCompleted);

            if (updateProfileTask.Exception != null)
            {
                Debug.LogError(updateProfileTask.Exception);

                var deleteTask = user.DeleteAsync();
                yield return new WaitUntil(() => deleteTask.IsCompleted);

                Debug.LogError("Не удалось обновить имя пользователя. Аккаунт удалён.");
                yield break;
            }

            yield return StartCoroutine(CreateUserProfileInDatabase(user, name));

            if (user.IsEmailVerified)
            {
                UIManager.Instance.OpenLoginPanel();
            }
            else
            {
                SendEmailForVerification();
            }
        }
    }

    private IEnumerator CreateUserProfileInDatabase(FirebaseUser firebaseUser, string name)
    {
        AppUserProfile profile = new AppUserProfile
        {
            Uid = firebaseUser.UserId,
            Email = firebaseUser.Email,
            Name = name,
            LanguageLevel = "A1",
            AvatarId = 0,
            CompletedLessons = 0,
            Experience = 0,
            StreakDays = 0,
            CreatedAt = Timestamp.GetCurrentTimestamp(),
            LastLogin = Timestamp.GetCurrentTimestamp()
        };

        DocumentReference docRef = database
            .Collection("users")
            .Document(firebaseUser.UserId);

        var setProfileTask = docRef.SetAsync(profile);

        yield return new WaitUntil(() => setProfileTask.IsCompleted);

        if (setProfileTask.Exception != null)
        {
            Debug.LogError("Ошибка создания профиля в Firestore: " + setProfileTask.Exception);
        }
        else
        {
            Debug.Log("Профиль пользователя создан в Firestore");
        }
    }

    private IEnumerator LoadUserProfileAndOpenMainScene(FirebaseUser firebaseUser)
    {
        yield return StartCoroutine(UpdateLastLogin(firebaseUser.UserId));

        DocumentReference docRef = database
            .Collection("users")
            .Document(firebaseUser.UserId);

        var getProfileTask = docRef.GetSnapshotAsync();

        yield return new WaitUntil(() => getProfileTask.IsCompleted);

        if (getProfileTask.Exception != null)
        {
            Debug.LogError("Ошибка загрузки профиля: " + getProfileTask.Exception);
            yield break;
        }

        DocumentSnapshot snapshot = getProfileTask.Result;

        if (snapshot.Exists)
        {
            AppUserProfile profile = snapshot.ConvertTo<AppUserProfile>();

            References.SetUserProfile(profile);

            Debug.Log("Профиль загружен");
            Debug.Log("Имя: " + References.userName);
            Debug.Log("Уровень языка: " + References.languageLevel);
            Debug.Log("Опыт: " + References.experience);

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        }
        else
        {
            Debug.LogWarning("Профиль не найден. Создаю новый профиль.");

            string name = string.IsNullOrEmpty(firebaseUser.DisplayName)
                ? "User"
                : firebaseUser.DisplayName;

            yield return StartCoroutine(CreateUserProfileInDatabase(firebaseUser, name));
            yield return StartCoroutine(LoadUserProfileAndOpenMainScene(firebaseUser));
        }
    }

    private IEnumerator UpdateLastLogin(string uid)
    {
        DocumentReference docRef = database
            .Collection("users")
            .Document(uid);

        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "LastLogin", Timestamp.GetCurrentTimestamp() }
        };

        var updateTask = docRef.UpdateAsync(updates);

        yield return new WaitUntil(() => updateTask.IsCompleted);

        if (updateTask.Exception != null)
        {
            Debug.LogWarning("Не удалось обновить LastLogin: " + updateTask.Exception);
        }
    }

    public void SendEmailForVerification()
    {
        StartCoroutine(SendEmailForVerificationAsync());
    }

    private IEnumerator SendEmailForVerificationAsync()
    {
        if (user != null)
        {
            var sendEmailTask = user.SendEmailVerificationAsync();

            yield return new WaitUntil(() => sendEmailTask.IsCompleted);

            if (sendEmailTask.Exception != null)
            {
                FirebaseException firebaseException = sendEmailTask.Exception.GetBaseException() as FirebaseException;
                AuthError error = (AuthError)firebaseException.ErrorCode;

                string errorMessage = "Ошибка отправки письма: ";

                switch (error)
                {
                    case AuthError.Cancelled:
                        errorMessage += "отменено";
                        break;

                    case AuthError.TooManyRequests:
                        errorMessage += "слишком много запросов";
                        break;

                    case AuthError.InvalidRecipientEmail:
                        errorMessage += "неверный Email получателя";
                        break;

                    default:
                        errorMessage += "не удалось отправить письмо";
                        break;
                }

                UIManager.Instance.ShowVerificationResponce(false, user.Email, errorMessage);
            }
            else
            {
                Debug.Log("Письмо подтверждения отправлено");
                UIManager.Instance.ShowVerificationResponce(true, user.Email, null);
            }
        }
    }

    public void Logout()
    {
        References.Clear();

        if (auth != null)
        {
            auth.SignOut();
        }

        UIManager.Instance.OpenLoginPanel();
    }

    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= AuthStateChanged;
        }
    }
}