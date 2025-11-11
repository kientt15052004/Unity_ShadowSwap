using UnityEngine;
using UnityEngine.UI;

public class WorldSelectManager : MonoBehaviour
{
    public Button[] worldButtons;

    private void Start()
    {
        // Nếu lần đầu chơi → chỉ mở World 1
        if (!PlayerPrefs.HasKey("WorldUnlocked"))
            PlayerPrefs.SetInt("WorldUnlocked", 1);

        int unlocked = PlayerPrefs.GetInt("WorldUnlocked");

        for (int i = 0; i < worldButtons.Length; i++)
        {
            bool isUnlocked = (i + 1) <= unlocked;

            worldButtons[i].interactable = isUnlocked;

            // Làm mờ nếu bị khóa
            Color c = worldButtons[i].image.color;
            c.a = isUnlocked ? 1f : 0.35f;
            worldButtons[i].image.color = c;

            // 🔒 Tìm icon khóa (theo tên)
            Transform lockIcon = worldButtons[i].transform.Find("LockIcon");
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!isUnlocked);
        }

    }
}