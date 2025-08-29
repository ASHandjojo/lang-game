using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
public class InputListener : ScriptableObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] public Button this_button;

    //[SerializeField] public TextMeshProUGUI this_text;


    [SerializeField] public string action;

    private int action_number;

    private bool shouldListen;

    private float back_timer = 0;

    private bool back_timing = false;
    void Start()
    {
        shouldListen = false;
        this_button.RegisterCallback<ClickEvent>(ListenForInput);
        if (action == "Move Right") {
            action_number = 0;
            //this_text.SetText(Keybinds.instance.getRightKey().ToString());
        } else if (action == "Move Left") {
            action_number = 1;
            //this_text.SetText(Keybinds.instance.getLeftKey().ToString());
        } else if (action == "Dictionary") {
            action_number = 2;
            //this_text.SetText(Keybinds.instance.getDictKey().ToString());
        } else if (action == "Return/Back") {
            action_number = 3;
            //this_text.SetText(Keybinds.instance.getBackKey().ToString());
        } else if (action == "Interactions") {
            action_number = 4;
            //this_text.SetText(Keybinds.instance.getIntersKey().ToString());
        } else if (action == "Settings") {
            action_number = 5;
            //this_text.SetText(Keybinds.instance.getSettingsKey().ToString());
        } else {
            action_number = -1;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (shouldListen) {
            if (Input.anyKeyDown) {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode))) {
                    if (Input.GetKeyDown(keyCode)) {
                        //this_code = keyCode;
                        ChangeKeybinds(keyCode);
                        break;
                    }
                }
                shouldListen = false;
            }
        }

        if (back_timing) {
            if (back_timer > 0) {
                back_timer -= Time.deltaTime;
            } else {
                back_timing = false;
                Keybinds.Instance.setChangeBack(false);
            }
        }
    }

    void ChangeKeybinds(KeyCode new_keyCode) {
       if (action_number < 3) {
         if (action_number < 1) {
           if (action_number < 0) {
            return;
           }
           if (new_keyCode != Keybinds.Instance.getLeftKey() && new_keyCode != Keybinds.Instance.getDictKey() && new_keyCode != Keybinds.Instance.getBackKey()
           && new_keyCode != Keybinds.Instance.getIntersKey() && new_keyCode != Keybinds.Instance.getSettingsKey()) {
              Keybinds.Instance.setRightKey(new_keyCode);
              //this_text.SetText(Keybinds.instance.getRightKey().ToString());
           } else {
              //this_text.SetText("Already Exists");
           }
           
         } else {
           if (action_number < 2) {
              if (new_keyCode != Keybinds.Instance.getRightKey() && new_keyCode != Keybinds.Instance.getDictKey() && new_keyCode != Keybinds.Instance.getBackKey()
                  && new_keyCode != Keybinds.Instance.getIntersKey() && new_keyCode != Keybinds.Instance.getSettingsKey()) {
                Keybinds.Instance.setLeftKey(new_keyCode);
                //this_text.SetText(Keybinds.instance.getLeftKey().ToString());
              } else {
                //this_text.SetText("Already Exists");
              }
           } else {
              if (new_keyCode != Keybinds.Instance.getLeftKey() && new_keyCode != Keybinds.Instance.getRightKey() && new_keyCode != Keybinds.Instance.getBackKey()
                  && new_keyCode != Keybinds.Instance.getIntersKey() && new_keyCode != Keybinds.Instance.getSettingsKey()) {
                Keybinds.Instance.setDictKey(new_keyCode);
                //this_text.SetText(Keybinds.instance.getDictKey().ToString());
              } else {
                //this_text.SetText("Already Exists");
              }
           }
         }
       } else {
        if (action_number < 4) {
          if (new_keyCode != Keybinds.Instance.getLeftKey() && new_keyCode != Keybinds.Instance.getDictKey() && new_keyCode != Keybinds.Instance.getRightKey()
              && new_keyCode != Keybinds.Instance.getIntersKey() && new_keyCode != Keybinds.Instance.getSettingsKey()) {
            Keybinds.Instance.setBackKey(new_keyCode);
            //this_text.SetText(Keybinds.instance.getBackKey().ToString());
            back_timing = true;
            back_timer = 1;
            Keybinds.Instance.setChangeBack(true);
          } else {
            //this_text.SetText("Already Exists");
          }
        } else if (action_number == 4) {
          if (new_keyCode != Keybinds.Instance.getLeftKey() && new_keyCode != Keybinds.Instance.getDictKey() && new_keyCode != Keybinds.Instance.getBackKey()
              && new_keyCode != Keybinds.Instance.getRightKey() && new_keyCode != Keybinds.Instance.getSettingsKey()) {
            Keybinds.Instance.setIntersKey(new_keyCode);
            //this_text.SetText(Keybinds.instance.getIntersKey().ToString());
          } else {
            //this_text.SetText("Already Exists");
          }
        } else {
          if (new_keyCode != Keybinds.Instance.getLeftKey() && new_keyCode != Keybinds.Instance.getDictKey() && new_keyCode != Keybinds.Instance.getBackKey()
              && new_keyCode != Keybinds.Instance.getIntersKey() && new_keyCode != Keybinds.Instance.getSettingsKey()) {
            Keybinds.Instance.setSettingsKey(new_keyCode);
            //this_text.SetText(Keybinds.instance.getSettingsKey().ToString());
          } else {
            //this_text.SetText("Already Exists");
          }
        }
       }
    }

    public void DisableButton() {
      this_button.UnregisterCallback<ClickEvent>(ListenForInput);
    }

    void ListenForInput(ClickEvent e) {
        shouldListen = true;
        //this_text.SetText("Rebinding...");
        Debug.Log("Rebinding...");
    }
}
