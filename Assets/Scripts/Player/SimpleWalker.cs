using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimpleWalker : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.0f;
    public bool sprintEnabled = false;
    public float sprintMultiplier = 1.6f;

    [Header("Look (Mouse + Controller)")]
    public Transform cameraPivot;                 // Assign Main Camera
    public float mouseSensitivity = 120f;         // degrees/sec from mouse delta
    public float padLookSpeed = 180f;             // degrees/sec from right stick
    [Tooltip("How far the player can look down (negative).")]
    public float minPitch = -25f;
    [Tooltip("How far the player can look up (positive).")]
    public float maxPitch = 25f;

    [Header("Look Smoothing (camera lag)")]
    [Tooltip("Higher = snappier, Lower = more lag. 6–14 feels good.")]
    public float lookSmoothing = 10f;             // interpolation strength

    [Header("Walk Wobble (Head Bob)")]
    [Tooltip("Vertical bob height. Keep tiny (0.005–0.04).")]
    public float bobAmplitude = 0.02f;
    [Tooltip("Bob speed in cycles per second while walking.")]
    public float bobFrequency = 8f;
    [Tooltip("How strongly current speed affects the bobbing.")]
    public float bobSpeedInfluence = 1f;
    [Tooltip("How fast the camera returns to rest when you stop.")]
    public float bobReturnSpeed = 10f;

    [Header("Misc")]
    public bool lockCursor = true;

    // Internals
    CharacterController controller;

    // Target vs smoothed rotations (for camera lag)
    float targetYaw;     // world yaw (player body)
    float targetPitch;   // local pitch (camera)
    float smoothedYaw;
    float smoothedPitch;

    Vector3 camDefaultLocalPos;
    float bobTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (!cameraPivot)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cameraPivot = cam.transform;
        }
        if (cameraPivot) camDefaultLocalPos = cameraPivot.localPosition;

        // Initialize rotations
        targetYaw = smoothedYaw = transform.eulerAngles.y;
        float startPitch = cameraPivot ? cameraPivot.localEulerAngles.x : 0f;
        startPitch = NormalizePitch(startPitch);
        targetPitch = smoothedPitch = Mathf.Clamp(startPitch, minPitch, maxPitch);

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (!controller) return;
        Look();
        Move();
        HeadBob();
    }

    void Look()
    {
        float dt = Time.deltaTime;

        // Mouse deltas
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        // Gamepad right stick (safe even if axes aren't defined)
        float rx = GetAxisSafe("Look X");
        float ry = GetAxisSafe("Look Y");

        // Combine mouse + pad
        float yawDelta   = (mx * mouseSensitivity + rx * padLookSpeed) * dt;
        float pitchDelta = (-my * mouseSensitivity + -ry * padLookSpeed) * dt;

        targetYaw   += yawDelta;
        targetPitch += pitchDelta;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // Smooth toward target (exp smoothing, framerate-independent)
        float t = 1f - Mathf.Exp(-lookSmoothing * dt);
        smoothedYaw   = Mathf.LerpAngle(smoothedYaw, targetYaw, t);
        smoothedPitch = Mathf.Lerp(smoothedPitch, targetPitch, t);

        // Apply
        transform.rotation = Quaternion.Euler(0f, smoothedYaw, 0f);
        if (cameraPivot)
            cameraPivot.localRotation = Quaternion.Euler(smoothedPitch, 0f, 0f);
    }

    void Move()
    {
        // Keyboard + left stick share these axes
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        float speed = moveSpeed;
        if (sprintEnabled && Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        Vector3 worldMove = transform.TransformDirection(input) * speed;
        controller.SimpleMove(worldMove); // includes gravity
    }

    void HeadBob()
    {
        if (!cameraPivot) return;

        Vector3 vel = controller.velocity; vel.y = 0f;
        float speed = vel.magnitude;

        if (speed > 0.05f && controller.isGrounded)
        {
            bobTimer += Time.deltaTime * (bobFrequency + speed * bobSpeedInfluence * 0.1f);
            float offsetY = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;
            Vector3 targetPos = camDefaultLocalPos + new Vector3(0f, offsetY, 0f);
            // slight smoothing for nicer feel
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, targetPos, 0.35f);
        }
        else
        {
            bobTimer = 0f;
            cameraPivot.localPosition = Vector3.Lerp(
                cameraPivot.localPosition, camDefaultLocalPos, bobReturnSpeed * Time.deltaTime);
        }
    }

    // Unity gives 0..360; convert to -180..180 so clamping works
    float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f) eulerX -= 360f;
        return eulerX;
    }

    // Prevent crashes if custom axes aren't defined yet
    float GetAxisSafe(string axisName)
    {
        try { return Input.GetAxis(axisName); }
        catch (System.ArgumentException) { return 0f; }
    }
}