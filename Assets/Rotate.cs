using UnityEngine;

public class MagicSpin : MonoBehaviour
{
    public enum Axis { Y, X, Z }
    public enum Side { Left, Right }

    [Header("Spin")]
    public Axis rotateAxis = Axis.Y;          // which axis to spin around (Y = side rotation)
    public Side direction = Side.Right;       // left or right
    [Tooltip("Degrees per second.")]
    public float rotationSpeed = 45f;

    [Header("Wobble (up & down)")]
    [Tooltip("Peak vertical offset from the start height.")]
    public float wobbleHeight = 0.15f;
    [Tooltip("How fast it bobs (cycles per second).")]
    public float wobbleSpeed = 1.2f;

    [Header("Smoothness")]
    [Tooltip("Higher = snappier, lower = floaty. 8–14 feels good.")]
    public float smooth = 10f;

    // internals
    float baseY;
    float phase;
    Vector3 axisVec;

    void Awake()
    {
        baseY = transform.position.y;
        phase = Random.value * 10f; // slight desync if you duplicate many
        axisVec = rotateAxis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _      => Vector3.up
        };
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --- Spin ---
        float dir = (direction == Side.Right) ? 1f : -1f;
        float angle = dir * rotationSpeed * dt;
        transform.Rotate(axisVec, angle, Space.Self);

        // --- Wobble (up/down) ---
        float targetY = baseY + Mathf.Sin((Time.time + phase) * (Mathf.PI * 2f) * wobbleSpeed) * wobbleHeight;

        // exponential smoothing (frame-rate independent)
        float t = 1f - Mathf.Exp(-smooth * dt);
        Vector3 p = transform.position;
        p.y = Mathf.Lerp(p.y, targetY, t);
        transform.position = p;
    }
}