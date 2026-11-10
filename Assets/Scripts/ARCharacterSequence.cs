using System.Collections;
using UnityEngine;

/// <summary>
/// Reproduce la secuencia Waving -> giro -> Walking -> giro -> Beckoning -> Idle.
/// Va en CADA uno de los 5 personajes (solo uno esta activo a la vez, lo decide CharacterManager).
/// Los estados citados por nombre (Idle, Waving, TurnAway, Walking, TurnBack, Beckoning, Dance)
/// deben existir tal cual en Assets/Animator/CharacterAnimator.controller.
/// La UI vive en ARGuideUIController (pantalla 4), no aca.
/// Reset oculta al personaje y APAGA la deteccion del QR: solo vuelve a reconocerlo con Start.
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class ARCharacterSequence : MonoBehaviour
{
    [Header("Movimiento (unidades = metros reales bajo Vuforia)")]
    [SerializeField] private float moveSpeed = 0.2f;
    [SerializeField] private float walkDuration = 2f;
    [SerializeField] private float turnDuration = 1f;

    [Header("Referencia de camara (opcional, usa Camera.main si se deja vacio)")]
    [SerializeField] private Transform cameraTransform;

    [Header("Audio (Assets/Audio)")]
    [SerializeField] private AudioClip appearClip; // suena al aparecer / Waving
    [SerializeField] private AudioClip comeHereClip; // suena en Beckoning ("ven")
    [SerializeField] private AudioClip danceClip; // suena mientras baila (boton Dance)

    private Animator animator;
    private AudioSource audioSource;
    private VuforiaTargetHandler qrHandler;
    private Coroutine sequenceCoroutine;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    /// <summary>True desde que arranca la secuencia hasta que se hace Reset.</summary>
    public bool SequenceStarted { get; private set; }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        qrHandler = GetComponentInParent<VuforiaTargetHandler>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    /// <summary>
    /// Punto de entrada llamado por VuforiaTargetHandler y por el boton Start.
    /// Si ya se ejecuto, no hace nada hasta que se llame a ResetSequence().
    /// </summary>
    public void StartSequence()
    {
        if (SequenceStarted) return;
        BeginSequence();
    }

    /// <summary>
    /// Detiene la secuencia, oculta al personaje, lo repone en su pose inicial y apaga la
    /// deteccion del QR (VuforiaTargetHandler.enabled = false): aunque vuelvas a enfocar el
    /// marcador no reaparece solo. Se re-arma recien al presionar Start.
    /// </summary>
    public void ResetSequence()
    {
        StopSequenceCoroutine();

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        animator.Play("Idle", 0, 0f);
        audioSource.Stop();
        audioSource.loop = false;

        SequenceStarted = false;
        gameObject.SetActive(false);

        if (qrHandler != null)
        {
            qrHandler.enabled = false;
        }

        ARGuideUIController.Instance?.OnSequenceReset();
    }

    /// <summary>Baile en loop (animacion + musica). Lo usa el boton Dance.</summary>
    public void PlayDance()
    {
        animator.Play("Dance", 0, 0f);
        PlayLoop(danceClip);
    }

    private void BeginSequence()
    {
        SequenceStarted = true;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // Vuelve a habilitar la deteccion del QR (por si un Reset previo la habia apagado).
        if (qrHandler != null)
        {
            qrHandler.enabled = true;
        }

        // Avisa a la UI aunque el disparo haya venido del QR y no del boton.
        ARGuideUIController.Instance?.OnSequenceStarted();

        sequenceCoroutine = StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        PlaySound(appearClip);
        yield return PlayOnce("Waving");

        Quaternion awayRotation = GetAwayFromCameraRotation();
        yield return TurnTo(awayRotation, "TurnAway");

        yield return WalkForward();

        Quaternion facingCameraRotation = GetFacingCameraRotation();
        yield return TurnTo(facingCameraRotation, "TurnBack");

        PlaySound(comeHereClip);
        yield return PlayOnce("Beckoning");

        animator.Play("Idle", 0, 0f);
        sequenceCoroutine = null;

        ARGuideUIController.Instance?.OnSequenceFinished();
    }

    private void StopSequenceCoroutine()
    {
        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = null;
        }
    }

    private IEnumerator PlayOnce(string stateName)
    {
        animator.Play(stateName, 0, 0f);
        yield return null; // deja que el Animator actualice GetCurrentAnimatorStateInfo
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
    }

    private IEnumerator TurnTo(Quaternion targetRotation, string stateName)
    {
        animator.Play(stateName, 0, 0f);

        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;
        while (elapsed < turnDuration)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / turnDuration);
            yield return null;
        }
        transform.rotation = targetRotation;
    }

    private IEnumerator WalkForward()
    {
        animator.Play("Walking", 0, 0f);

        float elapsed = 0f;
        while (elapsed < walkDuration)
        {
            elapsed += Time.deltaTime;
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            yield return null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayLoop(AudioClip clip)
    {
        if (clip == null) return;
        audioSource.loop = true;
        audioSource.clip = clip;
        audioSource.Play();
    }

    /// <summary>
    /// Rotacion horizontal que deja al personaje mirando en direccion opuesta a la camara, para
    /// que "adelante" al caminar sea siempre alejandose, sin importar como quedo orientado el modelo.
    /// </summary>
    private Quaternion GetAwayFromCameraRotation()
    {
        Transform cam = ResolveCamera();
        if (cam == null)
        {
            return transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        }

        Vector3 away = transform.position - cam.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
        {
            return transform.rotation * Quaternion.Euler(0f, 180f, 0f);
        }
        return Quaternion.LookRotation(away.normalized, Vector3.up);
    }

    /// <summary>
    /// Inverso del anterior: deja al personaje mirando a la camara. Se usa antes de Beckoning,
    /// en vez de asumir que la rotacion inicial en escena ya miraba a camara.
    /// </summary>
    private Quaternion GetFacingCameraRotation()
    {
        Transform cam = ResolveCamera();
        if (cam == null)
        {
            return initialRotation;
        }

        Vector3 towardCamera = cam.position - transform.position;
        towardCamera.y = 0f;
        if (towardCamera.sqrMagnitude < 0.0001f)
        {
            return initialRotation;
        }
        return Quaternion.LookRotation(towardCamera.normalized, Vector3.up);
    }

    private Transform ResolveCamera()
    {
        if (cameraTransform != null) return cameraTransform;
        return Camera.main != null ? Camera.main.transform : null;
    }
}
