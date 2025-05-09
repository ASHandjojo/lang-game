using UnityEngine;
using UnityEngine.UIElements;

using TMPro;

public class CustomKeyboardManager : MonoBehaviour
{
    public GameObject keyboardPanel; // The keyboard UI (can be a Canvas or Panel)

    public InputController controller;
    private Label activeInputField;

    public void OpenKeyboard()
    {
        keyboardPanel.SetActive(true);
    }

    public void CloseKeyboard()
    {
        keyboardPanel.SetActive(false);
    }

    public void InsertCharacter(string character)
    {
        // Dirty as fuck
        activeInputField = controller.InputField;
        if (activeInputField != null) activeInputField.text += character;
    }

    public void Backspace()
    {
        // Dirty as fuck
        activeInputField = controller.InputField;
        if (activeInputField != null && activeInputField.text.Length > 0)
        {
            activeInputField.text = activeInputField.text.Substring(0, activeInputField.text.Length - 1);
        }
    }

    public void SubmitInput()
    {
        FindFirstObjectByType<AnswerChecker>().CheckAnswer(controller.TranslatedStr);
        CloseKeyboard();
    }
}