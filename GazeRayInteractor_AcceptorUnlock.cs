using UnityEngine;

public class GazeRayInteractor_AcceptorUnlock : MonoBehaviour
{
    [Header("Target")]
    public Transform targetSphere;
    public Material defaultMaterial;
    public Material hitMaterial;

    [Header("Acceptor")]
    public Transform acceptor;
    public float unlockRadius = 0.5f;
    public bool snapToAcceptor = true;

    [Header("Feedback")]
    public ParticleSystem successParticles;
    public AudioSource audioSource;
    public AudioClip successSound;

    [Header("Gaze Settings")]
    public float maxDistance = 10f;
    public float smoothFactor = 0.2f;

    [Header("Detection")]
    public float sphereCastRadius = 0.08f;
    public float holdTime = 0.15f;

    [Header("Lock")]
    public float lockTime = 2f;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Debug")]
    public bool flipHorizontal = true;
    public bool flipVertical = true;

    private OVRCameraRig rig;
    private Renderer sphereRenderer;
    private Vector3 smoothedDir = Vector3.forward;
    private float lookTimer = 0f;
    private float gazeTimer = 0f;
    private bool isLocked = false;
    private float lockedDistance = 2f;
    private bool hasUnlockedFeedback = false;

    void Start()
    {
        rig = FindObjectOfType<OVRCameraRig>();
        if (rig == null) Debug.LogError("OVRCameraRig not found!");

        if (targetSphere != null)
        {
            // CHANGE 1: Find renderer on child if not on parent (for cube)
            sphereRenderer = targetSphere.GetComponent<Renderer>();
            if (sphereRenderer == null)
                sphereRenderer = targetSphere.GetComponentInChildren<Renderer>();

            if (sphereRenderer != null && defaultMaterial != null)
                sphereRenderer.material = defaultMaterial;
        }

        if (!OVRPlugin.eyeTrackingEnabled)
        {
            OVRPlugin.StartEyeTracking();
            Debug.Log("Eye Tracking Started");
        }

        if (audioSource == null && acceptor != null)
            audioSource = acceptor.GetComponent<AudioSource>();
        if (audioSource != null && successSound != null)
            audioSource.clip = successSound;
    }

    void Update()
    {
        if (rig == null || targetSphere == null || sphereRenderer == null) return;

        Vector3 origin = rig.centerEyeAnchor.position;
        Vector3 direction = rig.centerEyeAnchor.forward;

        OVRPlugin.EyeGazesState state = new();
        if (OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref state) && state.EyeGazes.Length >= 2)
        {
            float l = state.EyeGazes[0].Confidence;
            float r = state.EyeGazes[1].Confidence;
            if (l > 0.5f && r > 0.5f)
            {
                Quaternion lRot = new(state.EyeGazes[0].Pose.Orientation.x, state.EyeGazes[0].Pose.Orientation.y,
                                      state.EyeGazes[0].Pose.Orientation.z, state.EyeGazes[0].Pose.Orientation.w);
                Quaternion rRot = new(state.EyeGazes[1].Pose.Orientation.x, state.EyeGazes[1].Pose.Orientation.y,
                                      state.EyeGazes[1].Pose.Orientation.z, state.EyeGazes[1].Pose.Orientation.w);
                Vector3 lDir = rig.transform.TransformDirection(lRot * Vector3.forward);
                Vector3 rDir = rig.transform.TransformDirection(rRot * Vector3.forward);
                direction = (lDir + rDir).normalized;
                if (flipHorizontal) direction.x *= -1;
                if (flipVertical) direction.y *= -1;
            }
        }

        smoothedDir = Vector3.Lerp(smoothedDir, direction, smoothFactor).normalized;

        Ray ray = new Ray(origin, smoothedDir);
        // CHANGE 2: Also accept hits on child objects (the cube's mesh)
        bool rawHit = Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, maxDistance)
                      && (hit.transform == targetSphere || (targetSphere != null && hit.transform.IsChildOf(targetSphere)));

        if (rawHit) lookTimer += Time.deltaTime;
        else lookTimer -= Time.deltaTime;
        lookTimer = Mathf.Clamp(lookTimer, 0f, holdTime);
        bool isLooking = lookTimer >= holdTime;

        sphereRenderer.material = isLooking ? hitMaterial : defaultMaterial;

        if (!isLocked)
        {
            if (isLooking)
            {
                gazeTimer += Time.deltaTime;
                if (gazeTimer >= lockTime)
                {
                    isLocked = true;
                    lockedDistance = Vector3.Distance(origin, targetSphere.position);
                    hasUnlockedFeedback = false;
                    Debug.Log("🔒 Locked");
                }
            }
            else
            {
                gazeTimer = 0f;
            }
        }

        if (isLocked)
        {
            Vector3 targetPos = origin + smoothedDir * lockedDistance;
            targetSphere.position = Vector3.Lerp(targetSphere.position, targetPos, Time.deltaTime * moveSpeed);

            if (acceptor != null)
            {
                float dist = Vector3.Distance(targetSphere.position, acceptor.position);
                if (dist <= unlockRadius && !hasUnlockedFeedback)
                {
                    isLocked = false;
                    if (snapToAcceptor)
                        targetSphere.position = acceptor.position;

                    if (successParticles != null)
                    {
                        successParticles.transform.position = acceptor.position;
                        successParticles.Play();
                    }
                    if (audioSource != null && successSound != null)
                        audioSource.PlayOneShot(successSound);

                    hasUnlockedFeedback = true;
                    Debug.Log("🔓 Unlocked – reached acceptor!");
                }
            }
        }

        Debug.DrawRay(origin, smoothedDir * maxDistance, Color.red);
    }

    // Called by manager to change target
    public void SetCurrentTarget(Transform newTarget, Transform newAcceptor)
    {
        if (isLocked)
        {
            isLocked = false;
            gazeTimer = 0f;
            lookTimer = 0f;
            hasUnlockedFeedback = false;
        }
        targetSphere = newTarget;
        acceptor = newAcceptor;

        // Re-find renderer
        sphereRenderer = targetSphere.GetComponent<Renderer>();
        if (sphereRenderer == null)
            sphereRenderer = targetSphere.GetComponentInChildren<Renderer>();
        if (sphereRenderer != null && defaultMaterial != null)
            sphereRenderer.material = defaultMaterial;

        lookTimer = 0f;
        gazeTimer = 0f;
        isLocked = false;
        hasUnlockedFeedback = false;
        Debug.Log($"New target set: {newTarget.name}");
    }
}