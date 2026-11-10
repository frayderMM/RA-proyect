using UnityEngine;

/// <summary>
/// Estado del usuario compartido por las 4 pantallas. Singleton con DontDestroyOnLoad y
/// respaldo en PlayerPrefs, asi sobrevive tanto a cambios de pantalla como a cerrar la app.
/// </summary>
public class AppStateManager : MonoBehaviour
{
    public static AppStateManager Instance { get; private set; }

    [SerializeField] private string userName = "";
    [SerializeField] private string career = "";
    [SerializeField] private string selectedEventId = "";
    [SerializeField] private string selectedCharacterId = "";

    private const string KeyName = "RAGuide.userName";
    private const string KeyCareer = "RAGuide.career";
    private const string KeyEvent = "RAGuide.selectedEventId";
    private const string KeyCharacter = "RAGuide.selectedCharacterId";

    public string UserName => userName;
    public string Career => career;
    public string SelectedEventId => selectedEventId;
    public string SelectedCharacterId => selectedCharacterId;

    public EventData SelectedEvent => AppCatalog.GetEvent(selectedEventId);
    public CharacterData SelectedCharacter => AppCatalog.GetCharacter(selectedCharacterId);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AppStateManager");
        go.AddComponent<AppStateManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void SetUser(string newName, string newCareer)
    {
        userName = newName;
        career = newCareer;
        Save();
    }

    public void SetEvent(string eventId)
    {
        selectedEventId = eventId;
        Save();
    }

    public void SetCharacter(string characterId)
    {
        selectedCharacterId = characterId;
        Save();
    }

    private void Load()
    {
        userName = PlayerPrefs.GetString(KeyName, "");
        career = PlayerPrefs.GetString(KeyCareer, "");
        selectedEventId = PlayerPrefs.GetString(KeyEvent, AppCatalog.Events[0].id);
        selectedCharacterId = PlayerPrefs.GetString(KeyCharacter, AppCatalog.Characters[0].id);
    }

    private void Save()
    {
        PlayerPrefs.SetString(KeyName, userName);
        PlayerPrefs.SetString(KeyCareer, career);
        PlayerPrefs.SetString(KeyEvent, selectedEventId);
        PlayerPrefs.SetString(KeyCharacter, selectedCharacterId);
        PlayerPrefs.Save();
    }
}
