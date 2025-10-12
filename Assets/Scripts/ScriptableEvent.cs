using System.Collections.Generic;
using UnityEngine;

namespace DoubleADev.Scriptables
{
    [CreateAssetMenu(fileName = "NewEvent", menuName = "Scriptable Event/Default")]
    public class ScriptableEvent : ScriptableObject
    {
        private List<ScriptableEventListener> listeners = new List<ScriptableEventListener>();

        public void Raise()
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                listeners[i].OnEventRaised();
            }
        }

        public void RegisterListener(ScriptableEventListener listener)
        {
            listeners.Add(listener);
        }

        public void UnregisterListener(ScriptableEventListener listener)
        {
            listeners.Remove(listener);
        }
    }

    [System.Serializable]
    public class ScriptableEvent<T> : ScriptableObject
    {
        private List<ScriptableEventListener<T>> listeners = new List<ScriptableEventListener<T>>();

        public void Raise(T action)
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                listeners[i].OnEventRaised(action);
            }
        }

        public void RegisterListener(ScriptableEventListener<T> listener)
        {
            listeners.Add(listener);
        }

        public void UnregisterListener(ScriptableEventListener<T> listener)
        {
            listeners.Remove(listener);
        }
    }
}
