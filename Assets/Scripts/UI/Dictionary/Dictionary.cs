using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Dictionary : MonoBehaviour
{
    public List<GameObject> childObjects;
    

    void Start()
    {
        // Populate with children if none given in inspector
        if(childObjects.Count == 0)
        {
            foreach(Transform child in this.gameObject.transform)
            {
                childObjects.Add(child.gameObject);
            }
        }
    }

    void OnEnable()
    {
        // Activate all children
        foreach(GameObject child in childObjects)
        {
            child.gameObject.SetActive(true);
        }
    }

    void OnDisable()
    {
        // Inactivate all children
        foreach(GameObject child in childObjects)
        {
            child.gameObject.SetActive(false);
        }
    }
}
