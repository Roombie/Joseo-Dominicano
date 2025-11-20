using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class InteractTutorialHint : MonoBehaviour
{
    [Header("Movimiento")]
    public Transform farPosition;   // donde empieza "JUGADOR"
    public Transform nearPosition;  // punto delante de la tienda
    public float moveDuration = 0.6f;
    public float holdNearTime = 0.8f;
    public float waitBetweenLoops = 0.5f;

    [Header("Eventos")]
    public UnityEvent OnEnterRange;     // se llama al llegar cerca
    public UnityEvent OnExitRange;      // se llama al alejarse
    public UnityEvent OnTutorialEnded;  // se llama cuando se completa el tuto

    private bool tutorialCompleted = false;
    private Coroutine loopRoutine;

    private void OnEnable()
    {
        transform.position = farPosition.position;
        tutorialCompleted = false;
        loopRoutine = StartCoroutine(HintLoop());
    }

    public void CompleteTutorial()
    {
        if (tutorialCompleted) return;

        tutorialCompleted = true;

        if (loopRoutine != null)
            StopCoroutine(loopRoutine);

        OnTutorialEnded?.Invoke();
    }

    private IEnumerator HintLoop()
    {
        while (!tutorialCompleted)
        {
            // acercarse
            yield return Move(farPosition.position, nearPosition.position, moveDuration);

            // ya estamos dentro del rango -> avisar
            OnEnterRange?.Invoke();

            yield return new WaitForSeconds(holdNearTime);

            // alejarse
            yield return Move(nearPosition.position, farPosition.position, moveDuration);

            // fuera del rango -> avisar (por si quieres ocultar cosas)
            OnExitRange?.Invoke();

            yield return new WaitForSeconds(waitBetweenLoops);
        }
    }

    private IEnumerator Move(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(from, to, f);
            yield return null;
        }
    }
}
