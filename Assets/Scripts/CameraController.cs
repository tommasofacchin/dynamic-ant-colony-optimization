using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Camera cam;
    public float moveSpeed = 10f;
    public float zoomSpeed = 2f;
    public float minZoom = 5f;
    public float maxZoom = 20f;
    public Vector2 xBounds = new Vector2(-10f, 10f);
    public Vector2 yBounds = new Vector2(-10f, 10f);
    public Vector2 xBoundsMin = new Vector2(-10f, 10f);
    public Vector2 yBoundsMin = new Vector2(-10f, 10f);

    private float targetAspectRatio = 16f / 9f;


    void Update()
    {
        HandleMovement();
        HandleZoom();
        SetCameraAspect();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal") * moveSpeed * Time.deltaTime;
        float vertical = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;

        Vector3 move = new Vector3(horizontal, vertical, 0);
        transform.Translate(move, Space.World);

        ClampCameraPosition();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            Vector3 mouseWorldBeforeZoom = cam.ScreenToWorldPoint(Input.mousePosition);

            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);

            Vector3 mouseWorldAfterZoom = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 offset = mouseWorldBeforeZoom - mouseWorldAfterZoom;

            transform.position += offset;

            ClampCameraPosition();
        }
    }

    void ClampCameraPosition()
    {
        float t = (cam.orthographicSize - minZoom) / (maxZoom - minZoom);

        float minX = Mathf.Lerp(xBoundsMin.x, xBounds.x, t);
        float maxX = Mathf.Lerp(xBoundsMin.y, xBounds.y, t);
        float minY = Mathf.Lerp(yBoundsMin.x, yBounds.x, t);
        float maxY = Mathf.Lerp(yBoundsMin.y, yBounds.y, t);

        float clampedX = Mathf.Clamp(transform.position.x, minX, maxX);
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, transform.position.z);
    }


    void SetCameraAspect()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspectRatio;

        if (scaleHeight < 1.0f)
        {
            Rect rect = cam.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            cam.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = cam.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            cam.rect = rect;
        }
    }

}
