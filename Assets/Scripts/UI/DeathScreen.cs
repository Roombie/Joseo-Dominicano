using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] Material _sepiaFXMaterial;
    [SerializeField] float _transitionDuration = 2;
    [SerializeField] float _afterTransitionDuration = 2;
    [SerializeField] bool _resetAfterTransition = true;
    [SerializeField] string _shaderTargetVariable = "_Intensity";
    [SerializeField] DeathScreenParameters _startPoint;
    [SerializeField] DeathScreenParameters _endPoint;
    [SerializeField] UnityEvent _onTransitionStart;
    [SerializeField] UnityEvent<float> _onTransitionTick;
    [SerializeField] UnityEvent _onTransitionEnd;
    [System.Serializable]
    public struct DeathScreenParameters
    {
        public float fxIntensity;
        public float timeScale;
    }

    void OnDestroy()
    {
        ResetChanges();
    }

    void SetMaterialFloat(float value)
    {
        _sepiaFXMaterial.SetFloat(_shaderTargetVariable, value);
    }

    public void StartTransition()
    {
        StopAllCoroutines();
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        float t = 0;
        float deltaDuration = 1 / _transitionDuration;
        SetMaterialFloat(_startPoint.fxIntensity);
        _onTransitionStart?.Invoke();
        while (t < 1)
        {
            Time.timeScale = Mathf.Lerp(_startPoint.timeScale, _endPoint.timeScale, t);
            SetMaterialFloat(Mathf.Lerp(_startPoint.fxIntensity, _endPoint.fxIntensity, t));
            _onTransitionTick?.Invoke(t);
            t += deltaDuration * Time.unscaledDeltaTime;
            yield return null;
        }
        SetMaterialFloat(_endPoint.fxIntensity);
        _onTransitionTick?.Invoke(1);
        yield return new WaitForSecondsRealtime(_afterTransitionDuration);
        if (_resetAfterTransition) ResetChanges();
        _onTransitionEnd?.Invoke();
    }
    
    public void ResetChanges()
    {
        SetMaterialFloat(0);
        Time.timeScale = 1;
    }
    
}
