using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InputListener : MonoBehaviour
{

    [SerializeField] private Button this_button;

    [SerializeField] private TextMeshProUGUI this_text;


    [SerializeField] private string action;

    private int action_number;

    private bool shouldListen;

    private float back_timer = 0;

    private bool back_timing = false;
    void Start()
    {
        shouldListen = false;
        this_button.onClick.AddListener(ListenForInput);
        if (action == "Move Right") {
            action_number = 0;
            this_text.SetText(Keybinds.instance.getRightKey().ToString());
        } else if (action == "Move Left") {
            action_number = 1;
            this_text.SetText(Keybinds.instance.getLeftKey().ToString());
        } else if (action == "Dictionary") {
            action_number = 2;
            this_text.SetText(Keybinds.instance.getDictKey().ToString());
        } else if (action == "Return/Back") {
            action_number = 3;
            this_text.SetText(Keybinds.instance.getBackKey().ToString());
        } else if (action == "Interactions") {
            action_number = 4;
            this_text.SetText(Keybinds.instance.getIntersKey().ToString());
        } else if (action == "Settings") {
            action_number = 5;
            this_text.SetText(Keybinds.instance.getSettingsKey().ToString());
        } else {
            action_number = -1;
        }
    }

 
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
                Keybinds.instance.setChangeBack(false);
            }
        }
    }

    void ChangeKeybinds(KeyCode new_keyCode) {
       if (action_number < 3) {
         if (action_number < 1) {
           if (action_number < 0) {
            return;
           }
           if (new_keyCode != Keybinds.instance.getLeftKey() && new_keyCode != Keybinds.instance.getDictKey() && new_keyCode != Keybinds.instance.getBackKey()
           && new_keyCode != Keybinds.instance.getIntersKey() && new_keyCode != Keybinds.instance.getSettingsKey()) {
              Keybinds.instance.setRightKey(new_keyCode);
              this_text.SetText(Keybinds.instance.getRightKey().ToString());
           } else {
              this_text.SetText("Already Exists");
           }
           
         } else {
           if (action_number < 2) {
              if (new_keyCode != Keybinds.instance.getRightKey() && new_keyCode != Keybinds.instance.getDictKey() && new_keyCode != Keybinds.instance.getBackKey()
                  && new_keyCode != Keybinds.instance.getIntersKey() && new_keyCode != Keybinds.instance.getSettingsKey()) {
                Keybinds.instance.setLeftKey(new_keyCode);
                this_text.SetText(Keybinds.instance.getLeftKey().ToString());
              } else {
                this_text.SetText("Already Exists");
              }
           } else {
              if (new_keyCode != Keybinds.instance.getLeftKey() && new_keyCode != Keybinds.instance.getRightKey() && new_keyCode != Keybinds.instance.getBackKey()
                  && new_keyCode != Keybinds.instance.getIntersKey() && new_keyCode != Keybinds.instance.getSettingsKey()) {
                Keybinds.instance.setDictKey(new_keyCode);
                this_text.SetText(Keybinds.instance.getDictKey().ToString());
              } else {
                this_text.SetText("Already Exists");
              }
           }
         }
       } else {
        if (action_number < 4) {
          if (new_keyCode != Keybinds.instance.getLeftKey() && new_keyCode != Keybinds.instance.getDictKey() && new_keyCode != Keybinds.instance.getRightKey()
              && new_keyCode != Keybinds.instance.getIntersKey() && new_keyCode != Keybinds.instance.getSettingsKey()) {
            Keybinds.instance.setBackKey(new_keyCode);
            this_text.SetText(Keybinds.instance.getBackKey().ToString());
            back_timing = true;
            back_timer = 1;
            Keybinds.instance.setChangeBack(true);
          } else {
            this_text.SetText("Already Exists");
          }
        } else if (action_number == 4) {
          if (new_keyCode != Keybinds.instance.getLeftKey() && new_keyCode != Keybinds.instance.getDictKey() && new_keyCode != Keybinds.instance.getBackKey()
              && new_keyCode != Keybinds.instance.getRightKey() && new_keyCode != Keybinds.instance.getSettingsKey()) {
            Keybinds.instance.setIntersKey(new_keyCode);
            this_text.SetText(Keybinds.instance.getIntersKey().ToString());
          } else {
            this_text.SetText("Already Exists");
          }
        } else {
          if (new_keyCode != Keybinds.instance.getLeftKey() && new_keyCode != Keybinds.instance.getDictKey() && new_keyCode != Keybinds.instance.getBackKey()
              && new_keyCode != Keybinds.instance.getIntersKey() && new_keyCode != Keybinds.instance.getSettingsKey()) {
            Keybinds.instance.setSettingsKey(new_keyCode);
            this_text.SetText(Keybinds.instance.getSettingsKey().ToString());
          } else {
            this_text.SetText("Already Exists");
          }
        }
       }
    }

    void ListenForInput() {
        shouldListen = true;
        this_text.SetText("Rebinding...");
    }
}
