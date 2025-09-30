using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleWalker : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4.0f;          // Walking speed
    public bool sprintEnabled = false;      // Leave off; here just in case
    public float sprintMultiplier = 1.6f;   // Used only if you flip sprintEnabled on later

    [Header("Mouse Look")]
    public Transform cameraPivot;           // Drag your Main Camera here
    public float mouseSensitivity = 120f;   // Degrees/second per mouse delta
    [Tooltip("How far the player can look down (negative).")]
    public float minPitch = -25f;
    [Tooltip("How far the player can look up (positive).")]
    public float maxPitch = 25f;

    [Header("Walk Wobble (Head Bob)")]
    [Tooltip("Vertical bob height. Keep tiny (0.005–0.04).")]
    public float bobAmplitude = 0.02f;
    [Tooltip("Bob speed in cycles per second while walking.")]
    public float bobFrequency = 8f;
    [Tooltip("How strongly current speed affects the bobbing.")]
    public float bobSpeedInfluence = 1f;

    [Header("Misc")]
    public bool lockCursor = true;

    CharacterController controller;
    float yaw;     // left-right rotation of the player body
    float pitch;   // up-down rotation of the camera
    Vector3 camDefaultLocalPos;
    float bobTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraPivot == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cameraPivot = cam.transform;
        }
        if (cameraPivot != null) camDefaultLocalPos = cameraPivot.localPosition;

        // Start yaw/pitch from current transforms
        yaw = transform.eulerAngles.y;
        if (cameraPivot != null) pitch = cameraPivot.localEulerAngles.x;
        pitch = NormalizePitch(pitch);

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        Look();
        Move();
        HeadBob();
    }

    void Look()
    {
        // Mouse delta in "degrees per second" style feel
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw += mx * mouseSensitivity * Time.deltaTime;
        pitch -= my * mouseSensitivity * Time.deltaTime; // invert to feel natural

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Apply rotations: body yaw, camera pitch
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        // WASD relative to player’s yaw
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        float speed = moveSpeed;
        if (sprintEnabled && Input.GetKey(KeyCode.LeftShift)) speed *= sprintMultiplier;

        // CharacterController.SimpleMove applies gravity automatically
        Vector3 worldMove = transform.TransformDirection(input) * speed;
        controller.SimpleMove(worldMove);
    }

    void HeadBob()
    {
        if (cameraPivot == null) return;

        // Bob only while moving on ground
        Vector3 horizontalVel = controller.velocity; horizontalVel.y = 0f;
        float moveMagnitude = horizontalVel.magnitude;

        if (moveMagnitude > 0.05f && controller.isGrounded)
        {
            bobTimer += Time.deltaTime * (bobFrequency + moveMagnitude * bobSpeedInfluence * 0.1f);
            float offsetY = Mathf.Sin(bobTimer * Mathf.PI * 2f) * bobAmplitude;
            cameraPivot.localPosition = camDefaultLocalPos + new Vector3(0f, offsetY, 0f);
        }
        else
        {
            // Ease back to default when not moving
            bobTimer = 0f;
            cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, camDefaultLocalPos, 10f * Time.deltaTime);
        }
    }

    // Unity gives 0..360; convert to -180..180 so clamping works
    float NormalizePitch(float eulerX)
    {
        if (eulerX > 180f) eulerX -= 360f;
        return eulerX;
    }
}
