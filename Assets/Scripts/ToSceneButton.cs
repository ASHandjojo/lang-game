using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ToSceneButton : MonoBehaviour
{

    [SerializeField] private Button this_button;

    [SerializeField] private string scene;

    [SerializeField] private string keybind_to_button;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        this_button.onClick.AddListener(ClickSettings);
    }

    void Update()
    {
        if (keybind_to_button == "Settings") {
          if (Input.GetKey(Keybinds.Instance.getSettingsKey())) {
            ClickSettings();
          }
        } else if (keybind_to_button == "Return/Back") {
          if (!Keybinds.Instance.getChangeBack() && Input.GetKey(Keybinds.Instance.getBackKey())) {
            ClickSettings();
          }
        }
    }

    void ClickSettings()
    {
        SceneManager.LoadScene(scene);
        //Debug.Log("You have clicked the button!");
    }
}
