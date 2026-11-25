using UnityEngine;
using UnityEngine.InputSystem;

public class LidarScanner : MonoBehaviour
{
    [Header("References (drag these in)")]
    public Transform aim;
    public ParticleSystem dots;
    public LayerMask scannable;

    [Header("General")]
    public float range = 30f;
    public Gradient distanceColors;

    [Header("Basic scan (hold left click)")]
    public float dotsPerSecond = 600f;
    public float spreadAngle = 8f;

    [Header("Long scan (right click)")]
    public float longScanDuration = 2f;
    public int rows = 80;
    public int dotsPerRow = 100;
    public float horizontalAngle = 60f;
    public float verticalAngle = 45f;

    bool isScanning;
    float dotBudget;
    bool isLongScanning;
    float longScanTimer;
    int nextRow;
    Quaternion longScanRotation;

    // Called by the Player Input component when "Interaction" (left click) changes
    public void Scan(InputAction.CallbackContext context)
    {
        isScanning = context.ReadValueAsButton();
    }

    // Called by the Player Input component when "LongScan" (right click) changes
    public void LongScan(InputAction.CallbackContext context)
    {
        if (context.performed && !isLongScanning)
        {
            StartLongScan();
        }
    }

    void Update()
    {
        if (isLongScanning)
        {
            UpdateLongScan();
            return;
        }

        if (isScanning)
        {
            BasicScan();
        }
    }

    void BasicScan()
    {
        dotBudget += dotsPerSecond * Time.deltaTime;

        while (dotBudget >= 1f) 
        {
            Vector2 offset = Random.insideUnitCircle * spreadAngle * 0.5f;
            Quaternion rotation = aim.rotation * Quaternion.Euler(offset.y, offset.x, 0f);
            FireRay(rotation * Vector3.forward);
            dotBudget -= 1f;
        }
    }

    void StartLongScan()
    {
        isLongScanning = true;
        longScanTimer = 0f;
        nextRow = 0;
        longScanRotation = aim.rotation;
    }

    void UpdateLongScan()
    {
        longScanTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(longScanTimer / longScanDuration);
        int targetRow = Mathf.FloorToInt(progress * rows);

        while (nextRow < targetRow)
        {
            FireRow(nextRow);
            nextRow++;
        }

        if (progress >= 1f) 
        {
            isLongScanning = false;
        }
    }

    void FireRow(int row)
    {
        // Negative angle looks up, positive looks down
        float vertical = Mathf.Lerp(-verticalAngle, verticalAngle, (float)row / (rows - 1)) * 0.5f;

        for (int i = 0; i < dotsPerRow; i++)
        {
            float horizontal = Mathf.Lerp(-horizontalAngle, horizontalAngle, (float)i / (dotsPerRow - 1)) * 0.5f;
            Quaternion rotation = longScanRotation * Quaternion.Euler(vertical, horizontal, 0f);
            FireRay(rotation * Vector3.forward);
        }
    }

    void FireRay(Vector3 direction)
    {
        // Did the ray hit something scannable? If so, "hit" holds the details
        if (Physics.Raycast(aim.position, direction, out RaycastHit hit, range, scannable, QueryTriggerInteraction.Ignore))
        {
            var settings = new ParticleSystem.EmitParams();
            settings.position = hit.point + hit.normal * 0.01f;
            settings.startColor = distanceColors.Evaluate(hit.distance / range);
            dots.Emit(settings, 1);
        }
    }
}