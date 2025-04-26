using UnityEngine;
using UnityEngine.Rendering;

public class Keybinds : MonoBehaviour
{
  public static Keybinds instance { get; private set;}
  // right
  [SerializeField] private KeyCode right_key = KeyCode.RightArrow;
  // left
  [SerializeField] private KeyCode left_key = KeyCode.LeftArrow;
  // dictionary
  [SerializeField] private KeyCode dict_key = KeyCode.W;
  // back
  [SerializeField] private KeyCode back_key = KeyCode.E;
  // interactions
  [SerializeField] private KeyCode inters_key = KeyCode.S;
  // settings menu  
  [SerializeField] private KeyCode settings_key = KeyCode.Q;

  private bool just_changed_back = false;

    private void Awake()
    {
        if (instance != null && instance != this) 
        {
          Destroy(this);
        }
        else 
        {
          instance = this;
          Object.DontDestroyOnLoad(this);
        }
    }

  public void setRightKey(KeyCode new_right) {
    right_key = new_right;
  }

  public void setLeftKey(KeyCode new_left) {
    left_key = new_left;
  }

  public void setDictKey(KeyCode new_dict) {
    dict_key = new_dict;
  }

  public void setBackKey(KeyCode new_back) {
    back_key = new_back;
  }

  public void setIntersKey(KeyCode new_inters) {
    inters_key = new_inters;
  }

  public void setSettingsKey(KeyCode new_settings) {
    settings_key = new_settings;
  }

  public void setChangeBack(bool new_back) {
    just_changed_back = new_back;
  }

  public bool getChangeBack() {
    return just_changed_back;
  }

  public KeyCode getRightKey() {
    return right_key;
  }

  public KeyCode getLeftKey() {
    return left_key;
  }

  public KeyCode getDictKey() {
    return dict_key;
  }

  public KeyCode getBackKey() {
    return back_key;
  }

  public KeyCode getIntersKey() {
    return inters_key;
  }

  public KeyCode getSettingsKey() {
    return settings_key;
  }


  
    
}
