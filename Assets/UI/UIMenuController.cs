using System.Collections;
using UnityEngine;

public abstract class UIMenuController : MonoBehaviour
{
    public abstract IEnumerator Open();
    public abstract IEnumerator Close();
}
