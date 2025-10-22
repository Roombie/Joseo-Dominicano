using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class UISimplePopup : MonoBehaviour
{
    [SerializeField] GameObject _exitChanceVisual;
    [SerializeField] float _exitChanceDelay = 3;
    [SerializeField] KeyCode _exitKey = KeyCode.Return;
    [SerializeField] UnityEvent _onPopupEnter;
    [SerializeField] UnityEvent _onPopupExitChanceAppear;
    [SerializeField] UnityEvent _onPopupExit;
    IEnumerator Start()
    {
        _exitChanceVisual?.SetActive(false);
        _onPopupEnter?.Invoke();
        yield return new WaitForSecondsRealtime(_exitChanceDelay);
        _exitChanceVisual?.SetActive(true);
        _onPopupExitChanceAppear?.Invoke();
        yield return new WaitUntil(() => Input.GetKeyDown(_exitKey));
        gameObject.SetActive(false);
        _onPopupExit?.Invoke();
    }

    public void Open()
    {

    }

    public void Close()
    {

    }
    
    public void ForceClose()
    {
        
    }
}