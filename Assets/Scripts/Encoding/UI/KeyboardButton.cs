using UnityEngine;
using UnityEngine.UI;

public class KeyboardButton : MonoBehaviour
{
    public string keyValue;
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnKeyPressed);
    }

    void OnKeyPressed()
    {
        FindObjectOfType<CustomKeyboardManager>().InsertCharacter(keyValue);
    }
}