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
        InputController keyboardManager = FindFirstObjectByType<InputController>();

        if (keyValue == "delete")
        {
            keyboardManager.Backspace();
        }
        else
        {
            Debug.Log($"Inputted {keyValue}");
            keyboardManager.InsertCharacter(keyValue);
        }
    }
}