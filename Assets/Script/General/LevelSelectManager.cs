using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    public void GoToLevelScreen()
    {
        // Tải màn chơi dựa trên tên Scene được truyền vào
        SceneManager.LoadScene("LevelSelectScreen");
    }
    public void LoadLevel(string levelName)
    {
        // Tải màn chơi dựa trên tên Scene được truyền vào
        SceneManager.LoadScene(levelName);
    }

    public void GoBackToStartScreen()
    {
        // Quay lại màn hình chính (StartScreen)
        // Đảm bảo tên Scene "StartScreen" trùng khớp với tên Scene gốc của bạn
        SceneManager.LoadScene("StartScreen");
    }
}
