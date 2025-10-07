using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class VideoScreenZone : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;                 // Main Camera
    public Transform playerView;                // Where the camera should return to (child of Player)
    public Transform watchView;                 // A point in front of the screen
    public Transform lookTarget;                // Usually the screen's center (optional)
    public GameObject promptUI;
    public VideoPlayer videoPlayer;

    [Header("Player control (optional)")]
    public MonoBehaviour[] movementScriptsToDisable; // player & camera controllers you want off during watch

    [Header("Camera move")]
    public float moveDuration = 0.6f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public enum FacingMode { LookAtTarget, FixedAxis }
    public FacingMode facing = FacingMode.LookAtTarget;

    public enum Axis { PlusX, MinusX, PlusY, MinusY, PlusZ, MinusZ }
    public Axis fixedAxis = Axis.PlusZ;        // used only if facing == FixedAxis

    bool playerInside, isWatching, isTransitioning;

    // original parent (for safety if playerView not assigned)
    Transform camOriginalParent;

    void Awake()
    {
        if (promptUI) promptUI.SetActive(false);
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Update()
    {
        if (!playerInside || isTransitioning) return;

        // Keyboard X + common gamepad buttons
        if (Input.GetKeyDown(KeyCode.X) ||
            Input.GetKeyDown(KeyCode.JoystickButton2) ||   // X (Xbox) / Square (PS)
            Input.GetKeyDown(KeyCode.JoystickButton0))     // A (Xbox) / Cross (PS) fallback
        {
            if (!isWatching) EnterWatchMode();
            else ExitWatchMode();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = true;
        if (promptUI) promptUI.SetActive(true);
        if (videoPlayer) videoPlayer.Play();  // resumes from Pause automatically
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerInside = false;
        if (promptUI) promptUI.SetActive(false);

        // If they leave while watching, exit back to the *current* playerView
        if (isWatching) ExitWatchMode();

        if (videoPlayer) videoPlayer.Pause(); // do NOT reset time -> preserves progress
    }

    void EnterWatchMode()
    {
        isWatching = true;
        isTransitioning = true;

        // Detach so no parent moves it
        var camT = playerCamera.transform;
        camOriginalParent = camT.parent;
        camT.SetParent(null, true);

        // Disable motion scripts
        foreach (var m in movementScriptsToDisable) if (m) m.enabled = false;

        StopAllCoroutines();
        Vector3 targetPos = watchView.position;
        Quaternion targetRot = ComputeTargetRotation(targetPos);

        StartCoroutine(MoveCamera(camT, targetPos, targetRot, moveDuration, () =>
        {
            // Parent to watchView so it stays locked there
            camT.SetParent(watchView, true);
            isTransitioning = false;
        }));
    }

    void ExitWatchMode()
    {
        isWatching = false;
        isTransitioning = true;

        var camT = playerCamera.transform;

        // Unparent BEFORE moving back
        camT.SetParent(null, true);

        StopAllCoroutines();

        // Go straight to player's current anchor (no mid point)
        Vector3 targetPos;
        Quaternion targetRot;

        if (playerView != null)
        {
            targetPos = playerView.position;
            targetRot = playerView.rotation;
        }
        else
        {
            // Fallback: return to original parent position instantly
            targetPos = camOriginalParent ? camOriginalParent.position : camT.position;
            targetRot = camOriginalParent ? camOriginalParent.rotation : camT.rotation;
        }

        StartCoroutine(MoveCamera(camT, targetPos, targetRot, moveDuration, () =>
        {
            // Reattach to player anchor so it follows the player again
            if (playerView) camT.SetParent(playerView, true);
            else if (camOriginalParent) camT.SetParent(camOriginalParent, true);

            // Re-enable controls
            foreach (var m in movementScriptsToDisable) if (m) m.enabled = true;

            isTransitioning = false;
        }));
    }

    Quaternion ComputeTargetRotation(Vector3 fromPos)
    {
        if (facing == FacingMode.LookAtTarget && lookTarget)
        {
            return Quaternion.LookRotation((lookTarget.position - fromPos).normalized, Vector3.up);
        }

        Vector3 dir = fixedAxis switch
        {
            Axis.PlusX  => Vector3.right,
            Axis.MinusX => Vector3.left,
            Axis.PlusY  => Vector3.up,
            Axis.MinusY => Vector3.down,
            Axis.PlusZ  => Vector3.forward,
            Axis.MinusZ => Vector3.back,
            _ => Vector3.forward
        };
        return Quaternion.LookRotation(dir, Vector3.up);
    }

    IEnumerator MoveCamera(Transform cam, Vector3 targetPos, Quaternion targetRot, float duration, System.Action onDone)
    {
        isTransitioning = true;

        float t = 0f;
        Vector3 startPos = cam.position;
        Quaternion startRot = cam.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float k = ease.Evaluate(t);
            cam.position = Vector3.Lerp(startPos, targetPos, k);
            cam.rotation = Quaternion.Slerp(startRot, targetRot, k);
            yield return null;
        }

        onDone?.Invoke();
    }
}
