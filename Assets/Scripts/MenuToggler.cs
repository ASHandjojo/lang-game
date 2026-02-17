using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary> Handles menu opening and closing, as well as input action map toggling </summary>
/// <remarks>
/// NOTE: input action map toggling is a somewhat separate concern;
///   should be refactored out into its own class if more features are added
/// </remarks>
public class MenuToggler : MonoBehaviour
{
    public static MenuToggler Instance { get; private set; }

    private InputActionMap uiActionMap;

    private InputActionMap prevActionMap;
    // SideScrolling is the default starting action map for now; could change later

    private InputAction settingsAction;
    private InputAction dictionaryAction;

    [SerializeField] private SettingsMenuEvents settingsMenu;
    [SerializeField] private GameHUDEvents dictionaryMenu;

    private IOpenClosable currentMenu;
    public IOpenClosable CurrentMenu 
    { 
        get => currentMenu; 
        set
        {
            if (value == currentMenu) return;

            if (currentMenu == null)
            {
                //prevActionMap = *get current action map*;
                prevActionMap.Disable();
                DisableWorldActions();

                uiActionMap.Enable();
            }
            else
            {
                currentMenu.Close();
            }

            currentMenu = value;

            if (currentMenu == null)
            {
                uiActionMap.Disable();

                prevActionMap.Enable();
                EnableWorldActions();
            } else
            {
                currentMenu.Open();
            }
        }
    }

    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogWarning($"Duplicate instance has been created of {nameof(MenuToggler)}! Destroying duplicate instance.");
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);
        Instance = this;

        uiActionMap = InputSystem.actions.FindActionMap("UI");

        // Set initial state of action maps
        InputSystem.actions.Disable();

        // This should always be enabled
        InputSystem.actions.FindActionMap("MenuToggles").Enable();

        // Enable default starting action map ("SideScrolling" for now)
        prevActionMap = InputSystem.actions.FindActionMap("SideScrolling");
        prevActionMap.Enable();

        // Log which action maps are currently enabled
        foreach (var map in InputSystem.actions.actionMaps)
        {
            Debug.Log($"Map: {map.name} is {(map.enabled ? "ACTIVE" : "OFF")}");
        }


        settingsAction = InputSystem.actions.FindAction("Settings");
        dictionaryAction = InputSystem.actions.FindAction("Dictionary");
    }

    private void Update()
    {
        if (settingsAction.WasPerformedThisFrame()) OnMenuKey(settingsMenu);
        if (dictionaryAction.WasPerformedThisFrame()) OnMenuKey(dictionaryMenu);
    }

    /// <summary>
    /// Pressing a menu key while in another menu will close that menu, and open the new menu;
    /// pressing a menu key while already being in that menu will just close the menu. <br/>
    ///   - Can be changed to just ignore the keypresses instead (like how it was previously)
    /// </summary>
    /// <param name="menu">The menu to switch to.</param>
    private void OnMenuKey(IOpenClosable menu)
    {
        if (currentMenu == menu) CurrentMenu = null;
        else CurrentMenu = menu;
    }

    // Disable box collider to prevent further interaction & freeze position to prevent movement
    private void DisableWorldActions()
    {
        PlayerController.Instance.GetComponent<BoxCollider2D>().enabled = false;
        PlayerController.Instance.GetComponent<Rigidbody2D>().constraints |= RigidbodyConstraints2D.FreezePositionX;
    }

    // Restore movement & Re-enable box collider
    private void EnableWorldActions()
    {
        PlayerController.Instance.GetComponent<Rigidbody2D>().constraints &= ~RigidbodyConstraints2D.FreezePositionX;
        PlayerController.Instance.GetComponent<BoxCollider2D>().enabled = true;
    }
}
