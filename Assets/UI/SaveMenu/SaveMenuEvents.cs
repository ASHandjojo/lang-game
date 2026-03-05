using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[DisallowMultipleComponent]
public sealed class SaveMenuEvents : UIMenuController
{
    private UIDocument selfDocument;

    private Button backButton;
    private SoundHandler sh;
    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    public List<VisualElement> Slots = new List<VisualElement>();

    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        selfDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    void OnEnable()
    {
        selfDocument.rootVisualElement.BringToFront();

        // Add events to back button
        backButton = selfDocument.rootVisualElement.Q("BackButton") as Button;
        backButton.RegisterCallback<ClickEvent>(e => MenuToggler.Instance.ClearAllMenus());

        // Add sounds
        backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        var screenContainer = selfDocument.rootVisualElement.Q<VisualElement>("ScreenContainer");
        var saveList = screenContainer.Q("SaveList");

        for (int i = 0; i < 6; i++)
        {
            int slotNumber = i + 1;
            var item = saveList.Q("Slot" + slotNumber);

            if (item == null)
            {
                Debug.LogError("Could not find Slot" + slotNumber);
                continue;
            }

            var load = item.Q<Button>("Load");
            var save = item.Q<Button>("Save");
            var text = item.Q<Label>("SlotText" + slotNumber);

            string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave" + slotNumber + ".json");
            if (File.Exists(savePath))
            {
                DateTime lastModified = File.GetLastWriteTime(savePath);
                text.text = "Save " + slotNumber + ": " + lastModified.ToString();
            }
            else
            {
                text.text = "Empty Slot " + slotNumber;
            }

            load.clicked += () => OnLoadClicked(slotNumber);
            save.clicked += () => OnSaveClicked(slotNumber);

            Slots.Add(item);
        }

    }

    // Get rid of button events
    void OnDisable()
    {
        backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);

        /**
        foreach (var (buttonID, handler) in rebindButtonEventHandlers)
        {
            Button uiButton = selfDocument.rootVisualElement.Q(buttonID) as Button;
            uiButton.UnregisterCallback(handler);
        }
        */
    }

    public override IEnumerator Open()
    {
        selfDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        yield break;
    }

    // Return to main menu or gameHud
    public override IEnumerator Close()
    {
        backButton.SetEnabled(false);
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;

        backButton.SetEnabled(true);
        yield break;
    }


    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        sh.PlaySoundUI(selectionClip);
        MenuToggler.Instance.UseMenu(this);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }

    void OnLoadClicked(int index)
    {
        GameState.LoadPlayerData(index);
        MenuToggler.Instance.ClearAllMenus();
    }

    void OnSaveClicked(int index)
    {
        GameState.SavePlayerData(index);
        var item = Slots[index - 1];
        var text = item.Q<Label>("SlotText" + index);

        string savePath = Path.Combine(Application.persistentDataPath, "PlayerSave" + index + ".json");
        DateTime lastModified = File.GetLastWriteTime(savePath);
        text.text = "Save " + index + ": " + lastModified.ToString();
    }
}
