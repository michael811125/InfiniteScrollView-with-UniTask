using UnityEngine;
using UnityEngine.SceneManagement;

public class TestGUI_04 : MonoBehaviour
{
    private void OnGUI()
    {
        if (GUILayout.Button("NextScene"))
        {
            SceneManager.LoadScene((int)Mathf.Repeat(SceneManager.GetActiveScene().buildIndex + 1, SceneManager.sceneCountInBuildSettings));
        }
    }
}
