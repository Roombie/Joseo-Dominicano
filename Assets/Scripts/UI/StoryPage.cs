using System.Collections;
using UnityEngine;

public class StoryPage : MonoBehaviour
{
    [Header("Animator & States")]
    public Animator animator;

    [Header("Audio")]
    public AudioClip slidePageClip;

    [Tooltip("Trigger to start the 'appear' animation.")]
    public string appearTrigger = "Appear";
    [Tooltip("Trigger to start the 'leave' animation.")]
    public string leaveTrigger = "Leave";

    [Tooltip("Name of the state that plays during appear (for completion checks).")]
    public string appearStateName = "In";
    [Tooltip("Name of the state that plays during leave (for completion checks).")]
    public string leaveStateName = "Out";

    [Tooltip("Animator layer to check (usually 0).")]
    public int layerIndex = 0;

    int appearStateHash;
    int leaveStateHash;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        appearStateHash = Animator.StringToHash(appearStateName);
        leaveStateHash = Animator.StringToHash(leaveStateName);
        gameObject.SetActive(false);
    }

    public IEnumerator PlayAppear()
    {
        if (!animator) yield break;
        gameObject.SetActive(true);

        animator.ResetTrigger(leaveTrigger);
        animator.SetTrigger(appearTrigger);
        yield return WaitForStateToFinish(appearStateHash);
    }

    public IEnumerator PlayLeave()
    {
        if (!animator) yield break;

        animator.ResetTrigger(appearTrigger);
        animator.SetTrigger(leaveTrigger);
        yield return WaitForStateToFinish(leaveStateHash);

        // maybe deactivate when done (I need to test to see if it'll look good)
        // gameObject.SetActive(false);
    }

    /// <summary>
    /// Force the current animation (appear/leave) to its end instantly.
    /// This uses Play(state, layer, 0.99f) to jump to the end.
    /// </summary>
    public void SkipCurrent()
    {
        if (!animator) return;

        var st = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (st.shortNameHash == appearStateHash)
        {
            animator.Play(appearStateHash, layerIndex, 0.99f);
        }
        else if (st.shortNameHash == leaveStateHash)
        {
            animator.Play(leaveStateHash, layerIndex, 0.99f);
        }
        // If transitioning, the jump will apply next frame.
        animator.Update(0f); // apply immediately
    }

    IEnumerator WaitForStateToFinish(int targetHash)
    {
        // Wait until we are actually in the target state
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (st.shortNameHash == targetHash && !animator.IsInTransition(layerIndex))
                break;
            yield return null;
        }
        // Now wait until the state finishes (normalizedTime >= 1)
        while (true)
        {
            var st = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (st.shortNameHash == targetHash && st.normalizedTime >= 1f && !animator.IsInTransition(layerIndex))
                break;
            yield return null;
        }
    }

    public void PlaySlidePageSound()
    {
        if (slidePageClip != null)
        {
            AudioManager.Instance.Play(slidePageClip, SoundCategory.SFX);
        }
    }
}