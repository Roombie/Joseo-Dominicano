using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DoubleADev.Scriptables
{
    public class ScriptableEventListener : MonoBehaviour
    {
        public ScriptableEvent Event;
        [SerializeField, Min(0)] private float executionDelay;
        [SerializeField] private UnityEvent actions;

        private void OnEnable()
        {
            Event?.RegisterListener(this);
        }

        private void OnDisable()
        {
            Event?.UnregisterListener(this);
        }

        public void OnEventRaised()
        {
            StartCoroutine(ExecuteEvent());
        }

        IEnumerator ExecuteEvent()
        {
            yield return new WaitForSecondsRealtime(executionDelay);
            actions.Invoke();
        }

        // public override string ToString()
        // {
        //     return gameObject.name;
        // }
    }

    [System.Serializable]
    public class ScriptableEventListener<T> : MonoBehaviour
    {
        public ScriptableEvent<T> Event;
        [SerializeField, Min(0)] private float executionDelay;
        [SerializeField] private UnityEvent<T> actions;

        private void OnEnable()
        {
            Event.RegisterListener(this);
        }

        private void OnDisable()
        {
            Event.UnregisterListener(this);
        }

        public void OnEventRaised(T action)
        {
            StartCoroutine(ExecuteEvent(action));
        }

        IEnumerator ExecuteEvent(T action)
        {
            yield return new WaitForSecondsRealtime(executionDelay);
            actions.Invoke(action);
        }

        // public override string ToString()
        // {
        //     return gameObject.name;
        // }
    }
}
