using UnityEngine;

public class Page : MonoBehaviour
{
     private Transform container;

    void OnEnable()
    {
       container = this.gameObject.transform.GetChild(0);

    }
 
    public void PageTurned()
    {
        // Change page index

        // Modify container contents based on index
    }
}
