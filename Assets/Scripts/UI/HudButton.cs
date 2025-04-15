using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;  
using System.Collections;
using System.Collections.Generic;

public class HudButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
      public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Mouse is over UI element");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Mouse left UI element");
    }

}
