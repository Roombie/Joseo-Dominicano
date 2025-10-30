using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    public Animator animator;

    public bool isTransitioning = false;

    [SerializeField] private float audioFadeOutDuration = 0.6f;

    // Precomputed animator hashes
    private static readonly int CloseTrigger = Animator.StringToHash("close");
    private static readonly int OpenTrigger = Animator.StringToHash("open");
    private static readonly int TransitionCloseTag = Animator.StringToHash("TransitionClose");
    private static readonly int TransitionOpenTag = Animator.StringToHash("TransitionOpen");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(PerformSceneTransition(sceneName));
    }

    public void LoadScene(int sceneIndex)
    {
        if (isTransitioning) return;
        StartCoroutine(PerformSceneTransition(sceneIndex));
    }

    private IEnumerator PerformSceneTransition(string sceneName)
    {
        isTransitioning = true;

        // Disable input events during transition
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        // Trigger the "close" animation
        animator.SetTrigger(CloseTrigger);

        Coroutine fadeAll = null;
        if (AudioManager.Instance != null && audioFadeOutDuration > 0f)
            fadeAll = AudioManager.Instance.FadeOutAll(audioFadeOutDuration, stopAfter: true);

        // Wait until the state with tag "TransitionClose" is active
        yield return null;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        while (state.tagHash != TransitionCloseTag)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Wait until the "close" animation finishes
        while (state.tagHash == TransitionCloseTag && state.normalizedTime < 1f)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        if (fadeAll != null) yield return fadeAll;

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded (90%)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        AudioManager.Instance.StopAll();

        // Activate the scene
        asyncLoad.allowSceneActivation = true;

        // Ensure the scene initializes at least one frame
        yield return null;

        if (Time.timeScale == 0)
            Time.timeScale = 1;

        // Trigger the "open" animation and wait until it completes
        animator.SetTrigger(OpenTrigger);

        // Wait until the state with tag "TransitionOpen" is active
        yield return null;
        state = animator.GetCurrentAnimatorStateInfo(0);
        while (state.tagHash != TransitionOpenTag)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Wait until the "open" animation finishes
        while (state.tagHash == TransitionOpenTag && state.normalizedTime < 1f)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Re-enable input and end the transition
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = true;

        isTransitioning = false;
    }

    private IEnumerator PerformSceneTransition(int sceneIndex)
    {
        isTransitioning = true;

        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = false;

        animator.SetTrigger(CloseTrigger);

        // Wait until the state with tag "TransitionClose" is active
        yield return null;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        while (state.tagHash != TransitionCloseTag)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Wait until the "close" animation finishes
        while (state.tagHash == TransitionCloseTag && state.normalizedTime < 1f)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Load the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false;

        // Wait until the scene is fully loaded (90%)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Activate the scene
        asyncLoad.allowSceneActivation = true;

        // Ensure the scene initializes at least one frame
        yield return null;

        if (Time.timeScale == 0)
            Time.timeScale = 1;

        // Trigger the "open" animation and wait until it completes
        animator.SetTrigger(OpenTrigger);

        // Wait until the state with tag "TransitionOpen" is active
        yield return null;
        state = animator.GetCurrentAnimatorStateInfo(0);
        while (state.tagHash != TransitionOpenTag)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Wait until the "open" animation finishes
        while (state.tagHash == TransitionOpenTag && state.normalizedTime < 1f)
        {
            yield return null;
            state = animator.GetCurrentAnimatorStateInfo(0);
        }

        // Re-enable input and end the transition
        if (EventSystem.current != null)
            EventSystem.current.sendNavigationEvents = true;

        isTransitioning = false;
    }
}