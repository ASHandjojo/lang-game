using UnityEngine;
using TMPro;

public class CustomKeyboardManager : MonoBehaviour
{
    public GameObject keyboardPanel; // The keyboard UI (can be a Canvas or Panel)
    public TMP_InputField activeInputField;

    public void OpenKeyboard(TMP_InputField inputField)
    {
        activeInputField = inputField;
        keyboardPanel.SetActive(true);
    }

    public void CloseKeyboard()
    {
        keyboardPanel.SetActive(false);
    }

    public void InsertCharacter(string character)
    {
        if (activeInputField != null) activeInputField.text += character;
    }

    public void Backspace()
    {
        if (activeInputField != null && activeInputField.text.Length > 0) {
            activeInputField.text = activeInputField.text.Substring(0, activeInputField.text.Length - 1);
        }
    }

    public void SubmitInput()
    {
        FindFirstObjectByType<AnswerChecker>().CheckAnswer(activeInputField.text);
        CloseKeyboard();
    }
}