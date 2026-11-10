using UnityEngine;

/// <summary>
/// Activa un solo personaje segun lo elegido en la pantalla 3 (galeria de personajes).
/// Todos comparten el mismo CharacterAnimator.controller y los mismos clips: Mecanim
/// retargetea las animaciones Humanoid a cualquier avatar humanoide, asi que no hace falta
/// duplicar clips ni controladores.
///
/// Arranca con todos ocultos: el personaje recien aparece cuando ARGuideUIController
/// (pantalla 4) llama a ApplySelection(). En las pantallas 1-3 no se ve ni reacciona al QR.
/// Se coloca en el GameObject "Image Target QR" (padre de los personajes).
/// </summary>
public class CharacterManager : MonoBehaviour
{
    public const string PrefKey = "RAGuide.SelectedCharacter";

    public static CharacterManager Instance { get; private set; }

    [Tooltip("Los personajes de la escena. El nombre del GameObject debe coincidir con AppCatalog.Characters[].id.")]
    [SerializeField] private GameObject[] characters;

    /// <summary>Secuencia del personaje actualmente activo (la que usan los botones de la UI).</summary>
    public ARCharacterSequence Active { get; private set; }

    /// <summary>El personaje original, el unico que hoy tiene animaciones propias.</summary>
    public static string DefaultCharacterName => AppCatalog.Characters[0].id;

    private void Awake()
    {
        Instance = this;
        HideAll();
    }

    /// <summary>Oculta a todos y apaga la deteccion del QR (estado de las pantallas 1-3).</summary>
    public void HideAll()
    {
        if (characters != null)
        {
            foreach (var go in characters)
            {
                if (go != null) go.SetActive(false);
            }
        }

        Active = null;

        var handler = GetComponent<VuforiaTargetHandler>();
        if (handler != null) handler.enabled = false;
    }

    private GameObject FindCharacter(string characterName)
    {
        foreach (var go in characters)
        {
            if (go != null && go.name == characterName) return go;
        }
        return null;
    }

    public static void SaveSelection(string characterName)
    {
        PlayerPrefs.SetString(PrefKey, characterName);
        PlayerPrefs.Save();
    }

    /// <summary>La skin se recuerda por personaje: volver a la galeria la muestra marcada.</summary>
    private static string SkinPrefKey(string characterName) => "RAGuide.SelectedSkin." + characterName;

    public static void SaveSkin(string characterName, int skinIndex)
    {
        PlayerPrefs.SetInt(SkinPrefKey(characterName), skinIndex);
        PlayerPrefs.Save();
    }

    public static int GetSavedSkin(string characterName) => PlayerPrefs.GetInt(SkinPrefKey(characterName), 0);

    /// <summary>
    /// Lee PlayerPrefs, activa solo ese personaje y apaga el resto.
    /// La llama ARGuideUIController al entrar a la pantalla AR.
    /// </summary>
    public void ApplySelection()
    {
        // Si habia otro personaje corriendo, lo deja limpio antes de cambiar.
        if (Active != null && Active.gameObject.activeSelf)
        {
            Active.ResetSequence();
        }

        Active = null;

        if (characters == null || characters.Length == 0)
        {
            Debug.LogError("CharacterManager: la lista 'characters' esta vacia. Corre AR Guide > Ejecutar TODO.");
            return;
        }

        string selected = PlayerPrefs.GetString(PrefKey, DefaultCharacterName);

        // Los modelos wolf/bull/viking/lion estan en stand-by (todavia sin animaciones propias):
        // si la carrera elegida apunta a uno que aun no esta en la escena, se usa el personaje
        // por defecto en vez de dejar la escena sin nadie.
        if (FindCharacter(selected) == null)
        {
            Debug.Log($"CharacterManager: '{selected}' todavia no esta en la escena; se usa '{DefaultCharacterName}'.");
            selected = DefaultCharacterName;
        }

        foreach (var go in characters)
        {
            if (go == null) continue;

            bool isSelected = go.name == selected;
            go.SetActive(isSelected);
            if (isSelected)
            {
                Active = go.GetComponent<ARCharacterSequence>();

                var skins = go.GetComponent<CharacterSkins>();
                if (skins != null) skins.Apply(GetSavedSkin(selected));
            }
        }

        if (Active == null)
        {
            Debug.LogWarning($"CharacterManager: no se encontro un personaje llamado '{selected}' con ARCharacterSequence.");
            return;
        }

        // Entrar desde el lobby re-arma la deteccion del QR (un Reset previo la deja apagada).
        var handler = GetComponent<VuforiaTargetHandler>();
        if (handler != null)
        {
            handler.enabled = true;
        }

        Debug.Log($"CharacterManager: personaje activo -> {selected}");
    }
}
