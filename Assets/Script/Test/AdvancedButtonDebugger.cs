using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Debug script nâng cao để tìm lỗi button không click được
/// Gắn vào CANVAS (không phải button)
/// </summary>
public class AdvancedButtonDebugger : MonoBehaviour
{
    void Update()
    {
        // Khi click chuột
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("=== MOUSE CLICK DETECTED ===");
            Debug.Log($"Mouse Position: {Input.mousePosition}");

            // Kiểm tra raycast
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Input.mousePosition;

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            Debug.Log($"Raycast hit {results.Count} objects:");

            foreach (var result in results)
            {
                Debug.Log($"  - {result.gameObject.name} (Layer: {LayerMask.LayerToName(result.gameObject.layer)})");

                // Kiểm tra có button không
                Button btn = result.gameObject.GetComponent<Button>();
                if (btn != null)
                {
                    Debug.Log($"    ✓ Có Button component!");
                    Debug.Log($"    ✓ Interactable: {btn.interactable}");
                    Debug.Log($"    ✓ OnClick listeners: {btn.onClick.GetPersistentEventCount()}");

                    if (btn.onClick.GetPersistentEventCount() > 0)
                    {
                        for (int i = 0; i < btn.onClick.GetPersistentEventCount(); i++)
                        {
                            Debug.Log($"      Listener {i}: {btn.onClick.GetPersistentMethodName(i)}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("    ⚠️ Button KHÔNG có OnClick listener!");
                    }
                }
            }

            if (results.Count == 0)
            {
                Debug.LogError("❌ Raycast KHÔNG hit object nào! Có thể:");
                Debug.LogError("  - EventSystem bị lỗi");
                Debug.LogError("  - Canvas không có Graphic Raycaster");
                Debug.LogError("  - Button ở layer bị ignore");
            }

            Debug.Log("=== END ===\n");
        }
    }
}