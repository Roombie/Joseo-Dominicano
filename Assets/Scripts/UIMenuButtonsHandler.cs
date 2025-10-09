using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuButtonsHandler : MonoBehaviour
{
    [SerializeField] private UIMenuButton[] _buttonActions;

    private void OnValidate()
    {
        InjectButtonActions();
    }

    private void Start()
    {
        if (Application.isPlaying)
        {
            InjectButtonActions();
        }
    }

    private void InjectButtonActions()
    {
        if (_buttonActions == null) return;
        for (int i = 0; i < _buttonActions.Length; i++)
        {
            var buttonAction = _buttonActions[i];
            if (buttonAction.button == null) 
            {
                buttonAction.name = "Button " + i;
                continue;
            }
            buttonAction.name = buttonAction.button.name;
            buttonAction.button.interactable = !buttonAction.locked;
            var label = buttonAction.button.GetComponentInChildren<TMP_Text>();
            label.text = buttonAction.name;
            buttonAction.button.image.enabled = label.enabled = !buttonAction.hided;
            buttonAction.FetchButtonActions();
        }
    }
}

[System.Serializable]
public struct UIMenuButton
{
    [HideInInspector] public string name;
    [SerializeField] private Button _button;
    public Button button => _button;
    [SerializeField] private bool _hided;
    public bool hided => _hided;
    [SerializeField] private bool _locked;
    public bool locked => _locked;
    [SerializeField] private Button.ButtonClickedEvent _actions;
    public Button.ButtonClickedEvent actions => _actions;
    private Button.ButtonClickedEvent _registeredActions;

    public void FetchButtonActions()
    {
        // CLEAN
        if (_registeredActions != null)
        {
            _button.onClick.RemoveListener(_registeredActions.Invoke);
        }

        // REGISTER
        if (actions != null)
        {
            _registeredActions = actions;
            _button.onClick.AddListener(actions.Invoke);
        }
    }
}