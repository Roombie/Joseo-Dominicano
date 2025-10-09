using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] Animator _mainMenuAnimator;
    [SerializeField] string _onPlayMainMenuAnimation;
    [SerializeField] GameObject _testGameplayScreen;

    // private void Start()
    // {
    //     _mainMenuAnimator
    // }

    public void PlayGame()
    {
        _mainMenuAnimator.Play(_onPlayMainMenuAnimation);
    }

    public void OnPlayMainMenuAnimationEnd()
    {
        _testGameplayScreen.SetActive(true);
    }
    
    public void QuitGame()
    {
        
    }
}
