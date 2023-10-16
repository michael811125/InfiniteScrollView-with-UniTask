using Cysharp.Threading.Tasks;
using HowTungTung;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestGUI_01 : MonoBehaviour
{
    private InfiniteScrollView infiniteScrollView;

    private string removeIndex = "0";
    private string snapIndex = "0";

    private async void Awake()
    {
        infiniteScrollView = FindObjectOfType<InfiniteScrollView>();
        // Init cells first
        await infiniteScrollView.Initialize();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("NextScene"))
        {
            SceneManager.LoadScene((int)Mathf.Repeat(SceneManager.GetActiveScene().buildIndex + 1, SceneManager.sceneCountInBuildSettings));
        }
        if (GUILayout.Button("Add 100 Random Height Cell"))
        {
            for (int i = 0; i < 100; i++)
            {
                var data = new InfiniteCellData(new Vector2(0, 50));
                infiniteScrollView.Add(data).Forget();
            }
            infiniteScrollView.Refresh();
        }
        if (GUILayout.Button("Add"))
        {
            var data = new InfiniteCellData(new Vector2(0, 50));
            infiniteScrollView.Add(data).Forget();
            infiniteScrollView.Refresh();
            infiniteScrollView.SnapLast(0.1f);
        }
        GUILayout.Label("Remove Index");
        removeIndex = GUILayout.TextField(removeIndex);
        if (GUILayout.Button("Remove"))
        {
            infiniteScrollView.Remove(int.Parse(removeIndex)).Forget();
        }
        GUILayout.Label("Snap Index");
        snapIndex = GUILayout.TextField(snapIndex);
        if (GUILayout.Button("Snap"))
        {
            infiniteScrollView.Snap(int.Parse(snapIndex), 0.1f);
        }
    }
}
