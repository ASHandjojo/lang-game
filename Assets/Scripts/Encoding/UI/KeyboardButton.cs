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
        CustomKeyboardManager keyboardManager = FindFirstObjectByType<CustomKeyboardManager>();

        if (keyValue == "delete") {
            keyboardManager.Backspace();
        } else {
            keyboardManager.InsertCharacter(keyValue);
        }
    }
}