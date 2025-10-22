using UnityEngine;

public class RenableChildrenAndComponents : MonoBehaviour
{
    [SerializeField] GameObject[] _gameObjects;
    [SerializeField] MonoBehaviour[] _components;

    [ContextMenu("Enable")] 
    public void Enable()
    {
        foreach (var item in _gameObjects)
        {
            item.SetActive(true);
        }

        foreach (var item in _components)
        {
            item.enabled = true;
        }
    }

    [ContextMenu("Disable")]
    public void Disable()
    {
        foreach (var item in _gameObjects)
        {
            item.SetActive(false);
        }

        foreach (var item in _components)
        {
            item.enabled = false;
        }
    }
}
