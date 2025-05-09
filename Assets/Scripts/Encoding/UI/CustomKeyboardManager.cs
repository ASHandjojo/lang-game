using System;
using System.Collections.Generic;

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
        // Dirty as fuck
        activeInputField = controller.InputField;
        activeInputField.text = "";

        activeInputField.SetEnabled(true);
        activeInputField.style.visibility = Visibility.Visible;
    }

    public void CloseKeyboard()
    {
        // Dirty as fuck
        activeInputField = controller.InputField;

        keyboardPanel.SetActive(false);
        activeInputField.style.visibility = Visibility.Hidden;
    }

    public void InsertCharacter(string character)
    {
        // Dirty as fuck
        activeInputField = controller.InputField;
        if (activeInputField != null) activeInputField.text += character;

        Debug.Log(":)");
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