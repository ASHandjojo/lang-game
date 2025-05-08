using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class SettingsMenuEvents : UIBase
{
    private UIDocument selfDocument;
    [SerializeField] private UIDocument otherDocument;
    private Button backButton;
    private SoundHandler sh;
    [Header("Audio")]
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip selectionClip;

    private Button right_listener;
    private Button left_listener;

    private Button inter_listener;
    private Button dict_listener;
    private Button ret_listener;
    private Button settings_listener;

    private bool shouldListen;
    private int current_keybind = -1;

    private Sprite[] azSprites = {null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null};




    void Awake()
    {
        selfDocument = GetComponent<UIDocument>();
        sh = GetComponent<SoundHandler>();

        char start = (char)97;
        for (int i = 0; i < 26; i++) {
            //Debug.Log(start);
            if (i % 23 == 0 || i % 23 == 1) {
                azSprites[i] = Resources.Load<Sprite>("Keys/" + start + "_key_light");
            } else {
                azSprites[i] = Resources.Load<Sprite>("Keys/" + start + "_light");
            }
            
            start = (char) (start + 1);
        }

        //vSprite.push(Resources.Load<Sprite>("Keys/v_light"));

        //if (vSprite != null) {
        //    Debug.Log("Sprite Loaded correctly");
        //}

        // Add events to back button
        backButton = selfDocument.rootVisualElement.Q("BackButton") as Button;
        backButton.RegisterCallback<ClickEvent>(ToggleMenu);


        // Add input listeners for Keybinds
        right_listener =  selfDocument.rootVisualElement.Q("MoveRightButton") as Button;
        right_listener.RegisterCallback<ClickEvent>(ListenForInputRight);
        left_listener = selfDocument.rootVisualElement.Q("MoveLeftButton") as Button;
        left_listener.RegisterCallback<ClickEvent>(ListenForInputLeft);
        inter_listener = selfDocument.rootVisualElement.Q("InteractButton") as Button;
        inter_listener.RegisterCallback<ClickEvent>(ListenForInputInters);
        dict_listener = selfDocument.rootVisualElement.Q("DictionaryButton") as Button;
        dict_listener.RegisterCallback<ClickEvent>(ListenForInputDict);
        ret_listener = selfDocument.rootVisualElement.Q("ReturnButton") as Button;
        ret_listener.RegisterCallback<ClickEvent>(ListenForInputBack);
        settings_listener = selfDocument.rootVisualElement.Q("SettingMenuButton") as Button;
        settings_listener.RegisterCallback<ClickEvent>(ListenForInputSettings);

        shouldListen = false;



        //Initialize Keybinds on Settings Menu
        SetRightImage(Keybinds.instance.getRightKey());
        SetLeftImage(Keybinds.instance.getLeftKey());
        SetIntersImage(Keybinds.instance.getIntersKey());
        SetRetImage(Keybinds.instance.getBackKey());
        SetDictImage(Keybinds.instance.getDictKey());
        SetSettingsImage(Keybinds.instance.getSettingsKey());
        
        // Add sounds
        backButton.RegisterCallback<ClickEvent>(OnButtonClick);
        backButton.RegisterCallback<MouseEnterEvent>(OnButtonHover);

        // Begin with settings menu not displayed
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;

    }    

    // Get rid of button events
    void OnDisable()
    {
        backButton.UnregisterCallback<ClickEvent>(ToggleMenu);

        backButton.UnregisterCallback<ClickEvent>(OnButtonClick);
        backButton.UnregisterCallback<MouseEnterEvent>(OnButtonHover);

        right_listener.UnregisterCallback<ClickEvent>(ListenForInputRight);
        left_listener.UnregisterCallback<ClickEvent>(ListenForInputLeft);
        inter_listener.UnregisterCallback<ClickEvent>(ListenForInputInters);
        dict_listener.UnregisterCallback<ClickEvent>(ListenForInputDict);
        ret_listener.UnregisterCallback<ClickEvent>(ListenForInputBack);
        settings_listener.UnregisterCallback<ClickEvent>(ListenForInputSettings);
        
    }

    // Return to main menu or gameHud
     private void ToggleMenu(ClickEvent e)
    {
        backButton.SetEnabled(false);

        otherDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        selfDocument.rootVisualElement.style.display = DisplayStyle.None;

        backButton.SetEnabled(true);

        // If a Player Instance is available (not in main menu), return movement and interactions
        if(PlayerController.Instance != null)
        {
            EnableWorldActions();
        }
    }


    // Play sound when a button is clicked
    private void OnButtonClick(ClickEvent e)
    {
        //Debug.Log("Click");
        sh.PlaySoundUI(selectionClip);
    }

    // Play sound when cursor is over a button
    private void OnButtonHover(MouseEnterEvent e)
    {
        sh.PlaySoundUI(hoverClip);
    }

    void Update()
    {
        if (shouldListen) {
            if (Input.anyKeyDown) {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode))) {
                    if (Input.GetKeyDown(keyCode)) {
                        if ((int) keyCode > 96 && (int) keyCode < 123)
                        switch(current_keybind) {
                            case 0: if (KeyCodeNotThis(0, keyCode)) {Keybinds.instance.setRightKey(keyCode); SetRightImage(keyCode); }
                            break;
                            case 1: if (KeyCodeNotThis(1, keyCode)) {Keybinds.instance.setLeftKey(keyCode); SetLeftImage(keyCode); }
                            break;
                            case 2: if (KeyCodeNotThis(2, keyCode)) {Keybinds.instance.setDictKey(keyCode); SetDictImage(keyCode); }
                            break;
                            case 3: if (KeyCodeNotThis(3, keyCode)) {Keybinds.instance.setBackKey(keyCode); SetRetImage(keyCode); }
                            break;
                            case 4: if (KeyCodeNotThis(4, keyCode)) {Keybinds.instance.setIntersKey(keyCode); SetIntersImage(keyCode); }
                            break;
                            case 5: if (KeyCodeNotThis(5, keyCode)) {Keybinds.instance.setSettingsKey(keyCode); SetSettingsImage(keyCode); }
                            break;
                            default: 
                            break;
                        }
                        //Debug.Log("Rebinded");
                        break;
                    }
                }
                shouldListen = false;
                current_keybind = -1;
            }
        }
    }

    void ListenForInputRight(ClickEvent e) {
        //Debug.Log("Right Click");
        shouldListen = true;
        current_keybind = 0;
    }

    void ListenForInputLeft(ClickEvent e) {
        //Debug.Log("Left Click");
        shouldListen = true;
        current_keybind = 1;
    }

    void ListenForInputDict(ClickEvent e) {
        //Debug.Log("Dict Click");
        shouldListen = true;
        current_keybind = 2;
    }

    void ListenForInputBack(ClickEvent e) {
        //Debug.Log("Back Click");
        shouldListen = true;
        current_keybind = 3;
    }

    void ListenForInputInters(ClickEvent e) {
        //Debug.Log("Inters Click");
        shouldListen = true;
        current_keybind = 4;
    }

    void ListenForInputSettings(ClickEvent e) {
        //Debug.Log("Settings Click");
        shouldListen = true;
        current_keybind = 5;
    }



    bool KeyCodeNotThis(int num, KeyCode key) {
        switch(num) {
            case 0: return (key != Keybinds.instance.getLeftKey() && key != Keybinds.instance.getDictKey() && key != Keybinds.instance.getBackKey() && key != Keybinds.instance.getIntersKey() && key != Keybinds.instance.getSettingsKey());
            case 1: return (key != Keybinds.instance.getRightKey() && key != Keybinds.instance.getDictKey() && key != Keybinds.instance.getBackKey() && key != Keybinds.instance.getIntersKey() && key != Keybinds.instance.getSettingsKey());
            case 2: return (key != Keybinds.instance.getRightKey() && key != Keybinds.instance.getLeftKey() && key != Keybinds.instance.getBackKey() && key != Keybinds.instance.getIntersKey() && key != Keybinds.instance.getSettingsKey());
            case 3: return (key != Keybinds.instance.getRightKey() && key != Keybinds.instance.getLeftKey() && key != Keybinds.instance.getDictKey() && key != Keybinds.instance.getIntersKey() && key != Keybinds.instance.getSettingsKey());
            case 4: return (key != Keybinds.instance.getRightKey() && key != Keybinds.instance.getLeftKey() && key != Keybinds.instance.getDictKey() && key != Keybinds.instance.getBackKey() && key != Keybinds.instance.getSettingsKey());
            case 5: return (key != Keybinds.instance.getRightKey() && key != Keybinds.instance.getLeftKey() && key != Keybinds.instance.getDictKey() && key != Keybinds.instance.getBackKey() && key != Keybinds.instance.getIntersKey());
            default: return false;
        }
    }


    // Change .hotkey-image based on assigned Keybind

    void SetRightImage(KeyCode num) {
      selfDocument.rootVisualElement.Q("MoveRightImage").style.backgroundImage = Background.FromSprite(azSprites[(int)num - 97]);
    }

    void SetLeftImage(KeyCode num) {
        selfDocument.rootVisualElement.Q("MoveLeftImage").style.backgroundImage = Background.FromSprite(azSprites[(int)num - 97]);
    }

    void SetIntersImage(KeyCode num) {
        selfDocument.rootVisualElement.Q("InteractImage").style.backgroundImage = Background.FromSprite(azSprites[(int)num - 97]);
    }

    void SetDictImage(KeyCode num) {
        selfDocument.rootVisualElement.Q("DictionaryImage").style.backgroundImage = Background.FromSprite(azSprites[(int)num - 97]);
    }

    void SetRetImage(KeyCode num) {
        selfDocument.rootVisualElement.Q("ReturnImage").style.backgroundImage = Background.FromSprite(azSprites[(int)num - 97]);
    }

    void SetSettingsImage(KeyCode num) {
        selfDocument.rootVisualElement.Q("SettingsImage").style.backgroundImage = Background.FromSprite(azSprites[(int)num - 97]);
    }


    // Change dialogue text speed based on slider position 
}
