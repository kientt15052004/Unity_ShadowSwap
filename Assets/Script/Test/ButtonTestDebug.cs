using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Script debug để kiểm tra tại sao button không click được
/// Gắn vào BUTTON (không phải GameManager)
/// </summary>
public class ButtonTestDebug : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private Image image;

    void Start()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();

        Debug.Log("=== BUTTON DEBUG START ===");
        Debug.Log($"Button name: {gameObject.name}");

        // Kiểm tra components
        if (button != null)
        {
            Debug.Log($"✓ Button component: OK");
            Debug.Log($"  - Interactable: {button.interactable}");
        }
        else
        {
            Debug.LogError("✗ KHÔNG có Button component!");
        }

        if (image != null)
        {
            Debug.Log($"✓ Image component: OK");
            Debug.Log($"  - Raycast Target: {image.raycastTarget}");
        }
        else
        {
            Debug.LogError("✗ KHÔNG có Image component!");
        }

        // Kiểm tra Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            Debug.Log($"✓ Canvas: {canvas.name}");

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                Debug.Log($"✓ Graphic Raycaster: OK");
            }
            else
            {
                Debug.LogError("✗ Canvas KHÔNG có Graphic Raycaster!");
            }
        }
        else
        {
            Debug.LogError("✗ KHÔNG tìm thấy Canvas!");
        }

        // Kiểm tra EventSystem
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            Debug.Log($"✓ Event System: {eventSystem.name}");
        }
        else
        {
            Debug.LogError("✗ KHÔNG có Event System trong scene!");
        }

        Debug.Log("=== BUTTON DEBUG END ===\n");
    }

    // Khi chuột hover vào
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"🖱️ HOVER vào button: {gameObject.name}");
    }

    // Khi chuột rời khỏi
    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"🖱️ Rời khỏi button: {gameObject.name}");
    }

    // Khi click
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"🖱️ CLICKED button: {gameObject.name}");
    }

    void Update()
    {
        // Kiểm tra raycast mỗi frame
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;

            // Kiểm tra button có ở vị trí chuột không
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, mousePos))
            {
                Debug.Log($"✓ Chuột click TRONG button: {gameObject.name}");

                if (!button.interactable)
                {
                    Debug.LogWarning("⚠️ Nhưng button đang bị DISABLE!");
                }

                if (!image.raycastTarget)
                {
                    Debug.LogWarning("⚠️ Nhưng Raycast Target đang TẮT!");
                }
            }
        }
    }
}