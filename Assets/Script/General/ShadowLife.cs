using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowLife : MonoBehaviour
{
    private void OnDestroy()
    {
        // Phát âm thanh khi bóng bị hủy (do hết thời gian)
        AudioManager.Instance?.PlayShadowDisappear();
    }
}
