using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public void ChangeSceneNoTransition(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
