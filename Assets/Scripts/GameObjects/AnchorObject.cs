using UnityEngine;

public class AnchorObject : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;       // Main Camera
    [SerializeField] private Vector2 screenOffset;    // Offset in world units from corner
    [SerializeField] private AnchorSide anchorSide = AnchorSide.BottomLeft;

    public enum AnchorSide
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    private Vector3 fixedWorldPos; // store the fixed world position

    private void Start()
    {
        SetPosition();
    }

    // Optional: expose method to manually reset cannon (e.g., after resolution change)
    private void SetPosition()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        Rect safeArea = Screen.safeArea;
        Vector3 corner = Vector3.zero;

        switch (anchorSide)
        {
            case AnchorSide.BottomLeft:
                corner = new Vector3(safeArea.xMin, safeArea.yMin, -mainCamera.transform.position.z);
                break;
            case AnchorSide.BottomRight:
                corner = new Vector3(safeArea.xMax, safeArea.yMin, -mainCamera.transform.position.z);
                break;
            case AnchorSide.TopLeft:
                corner = new Vector3(safeArea.xMin, safeArea.yMax, -mainCamera.transform.position.z);
                break;
            case AnchorSide.TopRight:
                corner = new Vector3(safeArea.xMax, safeArea.yMax, -mainCamera.transform.position.z);
                break;
        }

        // Convert to world position and apply offset
        fixedWorldPos = mainCamera.ScreenToWorldPoint(corner) + new Vector3(screenOffset.x, screenOffset.y, 0f);

        // Set cannon position once
        transform.position = fixedWorldPos;
    }
    public void ResetPosition()
    {
        transform.position = fixedWorldPos;
    }
}
