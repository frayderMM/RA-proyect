using UnityEngine;

/// <summary>
/// Skins de un personaje: un material por textura de Assets/Textures/&lt;id&gt;/. El indice 0 es la
/// skin Base. Lo arma el paso de Editor "Conectar skins"; no hace falta tocarlo en el Inspector.
/// </summary>
public class CharacterSkins : MonoBehaviour
{
    [Tooltip("Orden = AppCatalog.Characters[].skins. El 0 es el material base.")]
    [SerializeField] private Material[] materials;

    public int Count => materials != null ? materials.Length : 0;

    /// <summary>Pone la skin en todos los slots de los SkinnedMeshRenderer del personaje.</summary>
    public void Apply(int skinIndex)
    {
        // Con una sola skin no se toca nada: el personaje ya tiene sus materiales originales.
        if (Count <= 1) return;

        var material = materials[Mathf.Clamp(skinIndex, 0, Count - 1)];
        if (material == null) return;

        foreach (var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var slots = renderer.sharedMaterials;
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = material;
            }
            renderer.sharedMaterials = slots;
        }
    }
}
