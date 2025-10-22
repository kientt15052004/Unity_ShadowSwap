using UnityEngine;
using TMPro; // QUAN TRỌNG: Cần thư viện TextMeshPro

public class DamageText : MonoBehaviour
{
    public float lifetime = 1f; // Thời gian hiển thị
    public float floatSpeed = 1f; // Tốc độ bay lên
    public float randomness = 0.5f; // Ngẫu nhiên độ lệch ngang

    private TextMeshProUGUI textComponent;
    private Transform textTransform; // Sử dụng Transform cho World Space
    private Vector3 floatDirection;

    void Awake()
    {
        textTransform = GetComponent<Transform>(); // Lấy Transform
        textComponent = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        // Tạo hướng bay lên hơi ngẫu nhiên trong không gian World
        floatDirection = new Vector3(Random.Range(-randomness, randomness), 1, 0).normalized;

        // Hủy đối tượng sau thời gian hiển thị
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Di chuyển lên trong World Space
        textTransform.position += floatDirection * floatSpeed * Time.deltaTime;

        // Làm mờ dần chữ
        Color textColor = textComponent.color;
        textColor.a -= Time.deltaTime / lifetime;
        textComponent.color = textColor;
    }

    public void SetDamageValue(int damage)
    {
        if (textComponent != null)
        {
            textComponent.text = damage.ToString();
        }
    }
}