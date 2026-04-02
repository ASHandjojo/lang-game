using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;



[DisallowMultipleComponent]
public sealed class InventoryMenuEvents : UIMenuController
{
    
    private UIDocument document;

    private SoundHandler soundHandler;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    void Awake()
    {
        document = GetComponent<UIDocument>();
        soundHandler = GetComponent<SoundHandler>();

        // Begin with settings menu not displayed
        document.rootVisualElement.style.display = DisplayStyle.None;
    }

    void Start()
    {

    } 

    void OnEnable()
    {
        //// Add events to back button
        //backButton = document.rootVisualElement.Q("BackButton") as Button;
        //backButton.RegisterCallback<ClickEvent>(e => MenuToggler.Instance.ClearAllMenus());

        //// Add sounds
        //backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);
    }

    // Get rid of button events
    void OnDisable()
    {
        //backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        //backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);
    }

    public override IEnumerator Open()
    {
        document.rootVisualElement.style.display = DisplayStyle.Flex;
        yield break;
    }

    // Return to main menu or gameHud
    public override IEnumerator Close()
    {
        //backButton.SetEnabled(false);
        document.rootVisualElement.style.display = DisplayStyle.None;

        //backButton.SetEnabled(true);

        yield break;
    }


    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        soundHandler.PlaySoundUI(selectionClip);
        MenuToggler.Instance.UseMenu(this);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        soundHandler.PlaySoundUI(hoverClip);
    }

    
}
