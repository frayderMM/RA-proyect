using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Utilidad de Editor que conecta todo lo que no se puede fijar de forma confiable escribiendo
/// YAML a mano (fileIDs internos de los AnimationClip dentro de cada FBX, Loop Time, avatares
/// Humanoid, referencias de escena). Usa las APIs de Unity en vez de adivinar IDs.
///
/// La escena se guarda sola al terminar: las asignaciones a los personajes viven en la
/// ESCENA (no en los assets) y sin guardar se pierden al recargar.
/// </summary>
public static class ARGuideSetup
{
    private const string ControllerPath = "Assets/Animator/CharacterAnimator.controller";
    private const string ImageTargetName = "Image Target QR";
    private const string BaseCharacterName = "Character";

    /// <summary>
    /// Submenu de las piezas sueltas, numeradas en el mismo orden en que las corre
    /// "Ejecutar TODO". Solo hacen falta para repetir un paso puntual.
    /// </summary>
    private const string Steps = "AR Guide/Pasos individuales/";

    /// <summary>Modelos nuevos: nombre del GameObject -> ruta del FBX.</summary>
    private static readonly (string name, string fbxPath)[] NewCharacters =
    {
        ("wolf", "Assets/Resources/Characters/wolf.fbx"),
        ("bull", "Assets/Resources/Characters/bull.fbx"),
        ("viking", "Assets/Resources/Characters/viking.fbx"),
        ("lion", "Assets/Resources/Characters/lion.fbx"),
    };

    /// <summary>
    /// UNICO paso necesario: deja la app entera lista (proyecto, assets, escena y los 5
    /// personajes). Los items numerados de abajo son las piezas sueltas, por si hace falta
    /// repetir alguna; no hace falta correrlos uno por uno.
    /// </summary>
    [MenuItem("AR Guide/★ Ejecutar TODO", false, 0)]
    public static void RunFullSetup()
    {
        // 1) Proyecto
        ForcePortraitOrientation();
        FixAndroidPlayerSettings();
        SetAppIcon();
        ImportTMPEssentials();

        // 2) Assets: rigs humanoides, clips del controller y loops
        SetupNewCharacterRigs();
        WireAnimatorClips();
        FixClipLoopSettings();

        // 3) Escena: mete los 5 personajes y los cablea
        InstantiateNewCharacters();
        WireSceneObjects();

        // 4) Animaciones y texturas propias por personaje (quien no tenga, usa las compartidas)
        WirePerCharacterAnimations();
        WireCharacterMaterials();
        WireCharacterSkins();

        // 5) Ajustes finales
        FixWalkDistance();
        WireAudioClips();

        // 6) Guardar. Las asignaciones a los personajes viven en la ESCENA, no en los assets:
        // si esto no se guarda, al recargar se pierden y los personajes vuelven a verse blancos.
        SaveSceneNow();

        Debug.Log("<b>AR Guide: setup completo y escena guardada.</b> Ya podés darle Play o Build and Run.");
    }

    /// <summary>Usa Assets/Icons/logo_app.png como icono de la app en Android.</summary>
    [MenuItem(Steps + "03. Poner ícono de la app (Android)", false, 103)]
    public static void SetAppIcon()
    {
        // Se toma la imagen de Assets/Icons. Si hay varias, gana "logo_app"; si no, la primera
        // por orden alfabetico (los nombres pueden traer espacios o sufijos).
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Icons" });
        if (guids.Length == 0)
        {
            Debug.LogError("No hay ninguna imagen en Assets/Icons para usar como icono.");
            return;
        }

        string iconPath = null;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path).Replace(" ", "") == "logo_app")
            {
                iconPath = path;
                break;
            }
            if (iconPath == null || string.CompareOrdinal(path, iconPath) < 0) iconPath = path;
        }

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
        {
            Debug.LogError($"No se pudo cargar {iconPath} como Texture2D.");
            return;
        }

        if (guids.Length > 1)
        {
            Debug.LogWarning($"Hay {guids.Length} imagenes en Assets/Icons; se usa '{iconPath}'. " +
                             "Dejá solo la que querés para evitar ambigüedad.");
        }

        MakeIconUncompressed(iconPath);

        var target = NamedBuildTarget.Android;
        int kindsSet = 0;

        // Android define varios tipos de icono (Legacy, Round, Adaptive) y varias resoluciones
        // por tipo. Se usa la misma imagen en todos y Unity la reescala al compilar.
        foreach (var kind in PlayerSettings.GetSupportedIconKinds(target))
        {
            var icons = PlayerSettings.GetPlatformIcons(target, kind);
            if (icons == null || icons.Length == 0) continue;

            foreach (var platformIcon in icons)
            {
                // Los iconos adaptativos tienen 2 capas (fondo y frente): se rellenan ambas.
                var layers = new Texture2D[Mathf.Max(1, platformIcon.maxLayerCount)];
                for (int layer = 0; layer < layers.Length; layer++)
                {
                    layers[layer] = icon;
                }
                platformIcon.SetTextures(layers);
            }

            PlayerSettings.SetPlatformIcons(target, kind, icons);
            kindsSet++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Ícono de la app asignado desde {iconPath} ({kindsSet} tipo(s) de icono).");
    }

    /// <summary>La app es vertical: se bloquea la rotacion a portrait.</summary>
    [MenuItem(Steps + "01. Forzar orientación vertical", false, 101)]
    public static void ForcePortraitOrientation()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        Debug.Log("Orientación fijada a vertical (portrait).");
    }

    /// <summary>
    /// Ajustes de Player para Android: nombre de paquete propio y OpenGLES3 en vez de Vulkan
    /// (Vuforia avisa que su soporte de Vulkan es experimental).
    /// </summary>
    [MenuItem(Steps + "02. Ajustes de Android (paquete + gráficos)", false, 102)]
    public static void FixAndroidPlayerSettings()
    {
        const string identifier = "com.esan.raguide";
        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != identifier)
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, identifier);
            Debug.Log($"Nombre de paquete: {identifier}");
        }

        // Vuforia recomienda OpenGLES3; con Vulkan el tracking puede ser inestable.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        Debug.Log("API gráfica de Android fijada en OpenGLES3 (Vuforia no soporta Vulkan del todo).");
    }

    /// <summary>El icono comprimido pierde calidad; se importa sin compresion.</summary>
    private static void MakeIconUncompressed(string iconPath)
    {
        var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer == null) return;

        var settings = importer.GetPlatformTextureSettings("DefaultTexturePlatform");
        if (settings.textureCompression == TextureImporterCompression.Uncompressed) return;

        settings.textureCompression = TextureImporterCompression.Uncompressed;
        settings.overridden = true;
        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
        Debug.Log($"Icono importado sin compresión: {iconPath}");
    }

    [MenuItem(Steps + "04. Importar TMP Essentials", false, 104)]
    public static void ImportTMPEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
        {
            Debug.Log("TMP Essentials ya estaban importados.");
            return;
        }

        string packagePath = FindTMPEssentialsPackage();
        if (string.IsNullOrEmpty(packagePath))
        {
            Debug.LogError("No se encontro 'TMP Essential Resources.unitypackage'. Importalo a mano desde " +
                           "Window > TextMeshPro > Import TMP Essential Resources.");
            return;
        }

        AssetDatabase.ImportPackage(packagePath, false);
        Debug.Log($"Importando TMP Essentials desde {packagePath}");
    }

    private static string FindTMPEssentialsPackage()
    {
        foreach (var root in new[] { "Library/PackageCache", "Packages" })
        {
            if (!System.IO.Directory.Exists(root)) continue;

            var matches = System.IO.Directory.GetFiles(root, "TMP Essential Resources.unitypackage",
                System.IO.SearchOption.AllDirectories);
            if (matches.Length > 0) return matches[0];
        }
        return null;
    }

    [MenuItem(Steps + "05. Configurar rigs Humanoid", false, 105)]
    public static void SetupNewCharacterRigs()
    {
        foreach (var (name, fbxPath) in NewCharacters)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogError($"No se encontro el modelo {fbxPath}");
                continue;
            }

            // Cada modelo necesita su PROPIO avatar: los esqueletos tienen distinta cantidad de
            // huesos que Character.fbx, asi que "Copy From Other Avatar" no sirve. Con avatares
            // humanoides propios, Mecanim retargetea los mismos clips a los 5 personajes.
            if (importer.animationType == ModelImporterAnimationType.Human &&
                importer.avatarSetup == ModelImporterAvatarSetup.CreateFromThisModel)
            {
                continue; // ya configurado
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            Debug.Log($"{name}: rig configurado como Humanoid con avatar propio.");
        }

        // Verifica que el avatar haya quedado valido (el auto-mapeo de Mixamo suele funcionar).
        foreach (var (name, fbxPath) in NewCharacters)
        {
            var avatar = FindAvatar(fbxPath);
            if (avatar == null)
            {
                Debug.LogError($"{name}: no se genero Avatar. Abri el FBX > Rig > Configure y mapea los huesos a mano.");
            }
            else if (!avatar.isValid || !avatar.isHuman)
            {
                Debug.LogError($"{name}: el Avatar quedo invalido (isValid={avatar.isValid}, isHuman={avatar.isHuman}). " +
                               "Abri el FBX > Rig > Configure y revisa el mapeo.");
            }
        }
    }

    private static Avatar FindAvatar(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is Avatar avatar) return avatar;
        }
        return null;
    }

    [MenuItem(Steps + "06. Conectar clips del Animator", false, 106)]
    public static void WireAnimatorClips()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"No se encontro {ControllerPath}");
            return;
        }

        AssignMotion(controller, "Idle", "Assets/Animations/Stop Walking.fbx");
        AssignMotion(controller, "Waving", "Assets/Animations/Waving.fbx");
        AssignMotion(controller, "TurnAway", "Assets/Animations/Walking Turn 180.fbx");
        AssignMotion(controller, "Walking", "Assets/Animations/Walking.fbx");
        AssignMotion(controller, "TurnBack", "Assets/Animations/Walking Turn 180.fbx");
        AssignMotion(controller, "Beckoning", "Assets/Animations/Beckoning.fbx");
        AssignMotion(controller, "Dance", "Assets/Animations/Tut Hip Hop Dance.fbx");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("CharacterAnimator: clips reasignados usando los AnimationClip reales de cada FBX.");
    }

    [MenuItem(Steps + "07. Ajustar loops de los clips", false, 107)]
    public static void FixClipLoopSettings()
    {
        SetLoop("Assets/Animations/Waving.fbx", false);
        SetLoop("Assets/Animations/Walking Turn 180.fbx", false);
        SetLoop("Assets/Animations/Stop Walking.fbx", false);
        SetLoop("Assets/Animations/Beckoning.fbx", false);
        SetLoop("Assets/Animations/Walking.fbx", true);
        SetLoop("Assets/Animations/Tut Hip Hop Dance.fbx", true);

        AssetDatabase.Refresh();
        Debug.Log("Loop Time actualizado en los clips usados por la secuencia.");
    }

    [MenuItem(Steps + "09. Conectar objetos de la escena", false, 109)]
    public static void WireSceneObjects()
    {
        var scene = EditorSceneManager.GetActiveScene();
        GameObject imageTarget = FindInScene(scene, ImageTargetName);
        if (imageTarget == null)
        {
            Debug.LogError($"No se encontro un GameObject llamado '{ImageTargetName}' en la escena abierta.");
            return;
        }

        GameObject baseCharacter = FindInScene(scene, BaseCharacterName);
        if (baseCharacter == null)
        {
            Debug.LogError($"No se encontro el personaje '{BaseCharacterName}' en la escena.");
            return;
        }

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

        // Configura el personaje original y cualquier modelo nuevo que ya se haya agregado.
        var present = new List<GameObject> { baseCharacter };
        foreach (var (name, _) in NewCharacters)
        {
            GameObject go = FindInScene(scene, name);
            if (go != null) present.Add(go);
        }

        foreach (var go in present)
        {
            ConfigureCharacter(go, controller);
        }

        // CharacterManager en el Image Target, con los personajes que existan hoy.
        var manager = imageTarget.GetComponent<CharacterManager>();
        if (manager == null) manager = imageTarget.AddComponent<CharacterManager>();

        var managerSO = new SerializedObject(manager);
        var arrayProp = managerSO.FindProperty("characters");
        arrayProp.arraySize = present.Count;
        for (int i = 0; i < present.Count; i++)
        {
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = present[i];
        }
        managerSO.ApplyModifiedProperties();

        // El handler de Vuforia ya no necesita referencias: usa CharacterManager.Active.
        if (imageTarget.GetComponent<VuforiaTargetHandler>() == null)
        {
            imageTarget.AddComponent<VuforiaTargetHandler>();
        }

        // Todos arrancan apagados: el personaje recien aparece al entrar a la pantalla AR,
        // cuando ARGuideUIController llama a CharacterManager.ApplySelection().
        foreach (var go in present)
        {
            go.SetActive(false);
            EditorUtility.SetDirty(go);
        }

        EditorUtility.SetDirty(imageTarget);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"Escena conectada con {present.Count} personaje(s), todos ocultos hasta la pantalla AR. " +
                  "Guarda la escena (Ctrl+S).");
    }

    private const string OverridesFolder = "Assets/Animator/Overrides";

    private const string TexturesFolder = "Assets/Textures";
    private const string SkinMaterialsFolder = "Assets/Materials/Skins";

    /// <summary>
    /// Crea un material URP/Lit por personaje con su textura base y se lo asigna al
    /// SkinnedMeshRenderer. Cada personaje tiene su carpeta con texturas numeradas:
    ///   Assets/Textures/&lt;id&gt;/&lt;id&gt;_texture_01_base.jpg   (la 01 siempre es la Base)
    ///   Assets/Textures/&lt;id&gt;/&lt;id&gt;_texture_02_artico.png  (las demas son skins)
    /// Sombra ("Character") ya tiene el suyo en Assets/Materials/CharacterMaterial.mat.
    /// </summary>
    [MenuItem(Steps + "11. Conectar texturas de los personajes", false, 111)]
    public static void WireCharacterMaterials()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("No se encontro el shader 'Universal Render Pipeline/Lit'.");
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        int wired = 0;

        foreach (var data in AppCatalog.Characters)
        {
            // Sombra usa CharacterMaterial.mat, armado a mano: no se reemplaza.
            if (data.id == BaseCharacterName) continue;

            var textures = FindSkinTextures(data.id);
            if (textures.Count == 0) continue; // sin textura propia: se deja como esta
            string texturePath = textures[0];
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) continue;

            string materialPath = $"Assets/Materials/{data.id}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);

            var go = FindInScene(scene, data.id);
            if (go == null)
            {
                Debug.LogWarning($"{data.id}: material creado, pero el personaje no esta en la escena.");
                continue;
            }

            var renderers = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning($"{data.id}: no tiene SkinnedMeshRenderer.");
                continue;
            }

            int slots = 0;
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                    slots++;
                }
                renderer.sharedMaterials = materials;
            }

            EditorUtility.SetDirty(go);
            wired++;
            Debug.Log($"{data.id}: textura conectada en {renderers.Length} renderer(s) / {slots} slot(s) ({texturePath}).");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        SaveSceneNow();
        Debug.Log($"Texturas conectadas en {wired} personaje(s).");
    }

    /// <summary>
    /// Texturas de un personaje ordenadas por numero: Assets/Textures/&lt;id&gt;/&lt;id&gt;_texture_NN_*.
    /// </summary>
    private static List<string> FindSkinTextures(string id)
    {
        var result = new List<string>();
        string folder = $"{TexturesFolder}/{id}";
        if (!AssetDatabase.IsValidFolder(folder)) return result;

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileName(path).StartsWith($"{id}_texture_")) result.Add(path);
        }

        result.Sort(string.CompareOrdinal); // _01_, _02_, ... gracias al cero a la izquierda
        return result;
    }

    /// <summary>
    /// Arma las skins de cada personaje: la 01 es el material que ya tiene puesto (Base) y por
    /// cada textura extra crea una copia de ese material con la textura cambiada en
    /// Assets/Materials/Skins/. La lista queda en el componente CharacterSkins del personaje, en
    /// el mismo orden que los nombres de AppCatalog.Characters[].skins.
    /// </summary>
    [MenuItem(Steps + "12. Conectar skins de los personajes", false, 112)]
    public static void WireCharacterSkins()
    {
        if (!AssetDatabase.IsValidFolder(SkinMaterialsFolder))
        {
            AssetDatabase.CreateFolder("Assets/Materials", "Skins");
        }

        var scene = EditorSceneManager.GetActiveScene();
        int wired = 0;

        foreach (var data in AppCatalog.Characters)
        {
            var go = FindInScene(scene, data.id);
            if (go == null) continue;

            var renderer = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (renderer == null || renderer.sharedMaterial == null)
            {
                Debug.LogWarning($"{data.id}: sin SkinnedMeshRenderer/material, no se arman skins.");
                continue;
            }

            var baseMaterial = renderer.sharedMaterial;
            var textures = FindSkinTextures(data.id);
            var materials = new List<Material> { baseMaterial };

            for (int i = 1; i < textures.Count; i++)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textures[i]);
                if (texture == null) continue;

                string materialPath = $"{SkinMaterialsFolder}/{System.IO.Path.GetFileNameWithoutExtension(textures[i])}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(baseMaterial);
                    AssetDatabase.CreateAsset(material, materialPath);
                }

                // Mismo shader y ajustes que la Base; solo cambia la textura.
                material.shader = baseMaterial.shader;
                material.CopyPropertiesFromMaterial(baseMaterial);
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
                EditorUtility.SetDirty(material);
                materials.Add(material);
            }

            var skins = go.GetComponent<CharacterSkins>();
            if (skins == null) skins = go.AddComponent<CharacterSkins>();

            var so = new SerializedObject(skins);
            var array = so.FindProperty("materials");
            array.arraySize = materials.Count;
            for (int i = 0; i < materials.Count; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);

            int names = data.skins != null ? data.skins.Length : 1;
            if (names != materials.Count)
            {
                Debug.LogWarning($"{data.id}: {materials.Count} skin(s) por textura pero {names} nombre(s) " +
                                 "en AppCatalog.Characters[].skins. Deben coincidir en cantidad y orden.");
            }

            wired++;
            Debug.Log($"{data.id}: {materials.Count} skin(s) conectada(s).");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        SaveSceneNow();
        Debug.Log($"Skins conectadas en {wired} personaje(s).");
    }

    /// <summary>
    /// Slots de animacion: archivo esperado por personaje -> clip del controller base que
    /// reemplaza -> si cicla. TurnAway y TurnBack comparten clip, asi que un solo archivo
    /// "Turn180" cubre los dos giros.
    /// </summary>
    private static readonly (string slotFile, string baseClipPath, bool loop)[] AnimationSlots =
    {
        ("Idle",      "Assets/Animations/Stop Walking.fbx",      false),
        ("Waving",    "Assets/Animations/Waving.fbx",            false),
        ("Turn180",   "Assets/Animations/Walking Turn 180.fbx",  false),
        ("Walking",   "Assets/Animations/Walking.fbx",           true),
        ("Beckoning", "Assets/Animations/Beckoning.fbx",         false),
        ("Dance",     "Assets/Animations/Tut Hip Hop Dance.fbx", true),
    };

    /// <summary>
    /// Conecta animaciones propias por personaje sin duplicar el Animator Controller.
    ///
    /// Espera los FBX en Assets/Animations/&lt;id&gt;/ con el nombre del slot, por ejemplo:
    ///   Assets/Animations/bull/Idle.fbx, Waving.fbx, Turn180.fbx, Walking.fbx,
    ///   Beckoning.fbx, Dance.fbx
    ///
    /// Los slots que falten siguen usando el clip compartido de Character X.
    /// Por cada personaje crea un AnimatorOverrideController que hereda los 7 estados del
    /// controller base y solo reemplaza los clips que existan.
    /// </summary>
    [MenuItem(Steps + "10. Conectar animaciones propias por personaje", false, 110)]
    public static void WirePerCharacterAnimations()
    {
        var baseController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (baseController == null)
        {
            Debug.LogError($"No se encontro {ControllerPath}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OverridesFolder))
        {
            AssetDatabase.CreateFolder("Assets/Animator", "Overrides");
        }

        var scene = EditorSceneManager.GetActiveScene();
        int charactersWired = 0;

        foreach (var data in AppCatalog.Characters)
        {
            string folder = $"Assets/Animations/{data.id}";
            if (!AssetDatabase.IsValidFolder(folder)) continue;

            // Junta los reemplazos disponibles: clip original -> clip propio del personaje.
            var replacements = new Dictionary<AnimationClip, AnimationClip>();
            var slotsFound = new List<string>();

            foreach (var (slotFile, baseClipPath, loop) in AnimationSlots)
            {
                string fbxPath = $"{folder}/{slotFile}.fbx";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath) == null) continue;

                ConfigureCharacterClipImport(fbxPath, loop);

                var newClip = FindClip(fbxPath);
                var baseClip = FindClip(baseClipPath);
                if (newClip == null || baseClip == null)
                {
                    Debug.LogError($"{data.id}/{slotFile}: no se pudo resolver el AnimationClip.");
                    continue;
                }

                replacements[baseClip] = newClip;
                slotsFound.Add(slotFile);
            }

            if (replacements.Count == 0)
            {
                Debug.Log($"{data.id}: carpeta encontrada pero sin FBX con nombre de slot.");
                continue;
            }

            string overridePath = $"{OverridesFolder}/{data.id}.overrideController";
            var overrideController = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);
            if (overrideController == null)
            {
                overrideController = new AnimatorOverrideController(baseController);
                AssetDatabase.CreateAsset(overrideController, overridePath);
            }
            overrideController.runtimeAnimatorController = baseController;

            // Se sobrescribe por REFERENCIA al clip original, no por nombre: todos los clips de
            // Mixamo se llaman "mixamo.com", asi que por nombre seria ambiguo.
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            overrideController.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                if (replacements.TryGetValue(overrides[i].Key, out var replacement))
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, replacement);
                }
            }
            overrideController.ApplyOverrides(overrides);
            EditorUtility.SetDirty(overrideController);

            // Se asigna al Animator del personaje en la escena.
            var go = FindInScene(scene, data.id);
            if (go != null)
            {
                var animator = go.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.runtimeAnimatorController = overrideController;
                    EditorUtility.SetDirty(go);
                }
            }
            else
            {
                Debug.LogWarning($"{data.id}: override creado, pero el personaje no esta en la escena. " +
                                 "Corre '08. Agregar los 4 personajes nuevos'.");
            }

            charactersWired++;
            Debug.Log($"{data.id}: {replacements.Count} slot(s) propios -> {string.Join(", ", slotsFound)}");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        SaveSceneNow();
        Debug.Log($"Animaciones propias conectadas en {charactersWired} personaje(s).");
    }

    /// <summary>Deja un FBX de animacion como Humanoid con avatar propio y con el loop pedido.</summary>
    private static void ConfigureCharacterClipImport(string fbxPath, bool loop)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool changed = false;

        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        var clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.clipAnimations;
        if (clips != null && clips.Length > 0)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i].loopTime != loop)
                {
                    clips[i].loopTime = loop;
                    changed = true;
                }
            }
            importer.clipAnimations = clips;
        }

        if (changed) importer.SaveAndReimport();
    }


    /// <summary>
    /// Suma wolf/bull/viking/lion a la escena y los deja cableados.
    /// Ya esta incluido en "Run Full Setup"; queda suelto por si hace falta repetirlo.
    /// </summary>
    [MenuItem(Steps + "08. Agregar los 4 personajes nuevos", false, 108)]
    public static void AddNewCharacters()
    {
        SetupNewCharacterRigs();
        InstantiateNewCharacters();
        WireSceneObjects();
        WirePerCharacterAnimations();
        WireAudioClips();
        FixWalkDistance();
        SaveSceneNow();
        Debug.Log("Personajes nuevos agregados.");
    }

    /// <summary>Solo instancia los modelos que falten en la escena, sin cablear nada mas.</summary>
    private static void InstantiateNewCharacters()
    {
        var scene = EditorSceneManager.GetActiveScene();
        GameObject imageTarget = FindInScene(scene, ImageTargetName);
        GameObject baseCharacter = FindInScene(scene, BaseCharacterName);
        if (imageTarget == null || baseCharacter == null)
        {
            Debug.LogError($"Faltan '{ImageTargetName}' o '{BaseCharacterName}' en la escena.");
            return;
        }

        foreach (var (name, fbxPath) in NewCharacters)
        {
            if (FindInScene(scene, name) != null) continue;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (asset == null)
            {
                Debug.LogError($"No se pudo cargar {fbxPath}");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, imageTarget.transform);
            instance.name = name;

            // Arranca con la misma pose que el personaje que ya funcionaba.
            instance.transform.localPosition = baseCharacter.transform.localPosition;
            instance.transform.localRotation = baseCharacter.transform.localRotation;
            instance.transform.localScale = baseCharacter.transform.localScale;

            Debug.Log($"{name}: instanciado bajo '{ImageTargetName}'.");
        }
    }

    /// <summary>Animator + controller + ARCharacterSequence + AudioSource en un personaje.</summary>
    private static void ConfigureCharacter(GameObject character, AnimatorController controller)
    {
        if (character == null) return;

        var animator = character.GetComponent<Animator>();
        if (animator == null) animator = character.AddComponent<Animator>();
        if (controller != null) animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        if (character.GetComponent<ARCharacterSequence>() == null)
        {
            character.AddComponent<ARCharacterSequence>();
        }

        var audioSource = character.GetComponent<AudioSource>();
        if (audioSource == null) audioSource = character.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EditorUtility.SetDirty(character);
    }

    [MenuItem(Steps + "13. Ajustar distancia de caminata", false, 113)]
    public static void FixWalkDistance()
    {
        foreach (var sequence in FindAllSequences())
        {
            var so = new SerializedObject(sequence);
            so.FindProperty("moveSpeed").floatValue = 0.2f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(sequence.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Move Speed = 0.2 en todos los personajes (~0.4 m en 2 segundos).");
    }

    [MenuItem(Steps + "14. Conectar audios", false, 114)]
    public static void WireAudioClips()
    {
        var appearClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Aparece.mp3");
        var comeHereClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Ven.mp3");
        var danceClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Bailar.mp3");

        if (appearClip == null) Debug.LogError("No se encontro Assets/Audio/Aparece.mp3");
        if (comeHereClip == null) Debug.LogError("No se encontro Assets/Audio/Ven.mp3");
        if (danceClip == null) Debug.LogError("No se encontro Assets/Audio/Bailar.mp3");

        int count = 0;
        foreach (var sequence in FindAllSequences())
        {
            var audioSource = sequence.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = sequence.gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            var so = new SerializedObject(sequence);
            so.FindProperty("appearClip").objectReferenceValue = appearClip;
            so.FindProperty("comeHereClip").objectReferenceValue = comeHereClip;
            so.FindProperty("danceClip").objectReferenceValue = danceClip;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(sequence.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Audio conectado en {count} personaje(s): Aparece/Ven/Bailar.");
    }

    /// <summary>Todos los ARCharacterSequence de la escena, incluidos los de objetos inactivos.</summary>
    private static List<ARCharacterSequence> FindAllSequences()
    {
        var result = new List<ARCharacterSequence>();
        var scene = EditorSceneManager.GetActiveScene();

        foreach (var root in scene.GetRootGameObjects())
        {
            result.AddRange(root.GetComponentsInChildren<ARCharacterSequence>(true));
        }

        if (result.Count == 0)
        {
            Debug.LogError("No hay ningun ARCharacterSequence en la escena. Corre primero '09. Conectar objetos de la escena'.");
        }
        return result;
    }

    private static void AssignMotion(AnimatorController controller, string stateName, string fbxPath)
    {
        AnimationClip clip = FindClip(fbxPath);
        if (clip == null)
        {
            Debug.LogError($"No se encontro un AnimationClip dentro de {fbxPath}");
            return;
        }

        var rootStateMachine = controller.layers[0].stateMachine;
        foreach (var childState in rootStateMachine.states)
        {
            if (childState.state.name == stateName)
            {
                childState.state.motion = clip;
                return;
            }
        }
        Debug.LogError($"No se encontro el estado '{stateName}' en el AnimatorController.");
    }

    private static AnimationClip FindClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (asset is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    private static void SetLoop(string fbxPath, bool loop)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogError($"No se pudo abrir el ModelImporter de {fbxPath}");
            return;
        }

        var clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0) clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            Debug.LogError($"{fbxPath} no tiene clips de animacion detectables.");
            return;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = loop;
        }
        importer.clipAnimations = clips;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Guarda la escena abierta. Los materiales, overrides y clips se guardan solos como assets,
    /// pero asignarlos a un GameObject es un cambio de escena y sin esto se pierde al recargar.
    /// </summary>
    private static void SaveSceneNow()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("La escena no tiene ruta (nunca se guardó). Guardala a mano con Ctrl+S.");
            return;
        }

        if (EditorSceneManager.SaveScene(scene))
        {
            Debug.Log($"Escena guardada: {scene.path}");
        }
        else
        {
            Debug.LogWarning("No se pudo guardar la escena automáticamente. Hacelo con Ctrl+S.");
        }
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            var found = FindRecursive(root.transform, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
