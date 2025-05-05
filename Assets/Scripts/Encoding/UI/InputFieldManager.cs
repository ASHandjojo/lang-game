using UnityEngine;
using TMPro;

public class InputFieldManager : MonoBehaviour
{
    public TMP_InputField inputField;

    public void OnInputFieldClicked()
    {
        FindObjectOfType<CustomKeyboardManager>().OpenKeyboard(inputField);
    }
}