using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuAutoSelect : MonoBehaviour
{
    [SerializeField] private GameObject firstSelected;

    void OnEnable() => StartCoroutine(SelectNextFrame());

    IEnumerator SelectNextFrame()
    {
        yield return null; // wait a frame
        if (EventSystem.current != null && firstSelected != null)
            EventSystem.current.SetSelectedGameObject(firstSelected);
    }

    void OnDisable()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}