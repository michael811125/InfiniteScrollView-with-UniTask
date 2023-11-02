using Cysharp.Threading.Tasks;
using InfiniteScrollViews;
using UnityEngine;

public class TestGUI_02 : MonoBehaviour
{
    private InfiniteScrollView infiniteScrollView;

    private string removeIndex = "0";
    private string snapIndex = "0";

    private async void Awake()
    {
        infiniteScrollView = FindObjectOfType<InfiniteScrollView>();
        // Init cells first
        await infiniteScrollView.InitializePool();
    }

    private void OnGUI()
    {
        #region Add
        if (GUILayout.Button("Add 100 Random Width Cell"))
        {
            for (int i = 0; i < 100; i++)
            {
                infiniteScrollView.Add(new InfiniteCellData(new Vector2(50, 0)));
            }
            infiniteScrollView.Refresh();
        }

        GUILayout.Label("Add New Cell Width");
        if (GUILayout.Button("Add"))
        {
            infiniteScrollView.Add(new InfiniteCellData(new Vector2(50, 0)));
            infiniteScrollView.Refresh();
            infiniteScrollView.SnapLast(0.1f);
        }
        #endregion

        #region Remove
        GUILayout.Label("Remove Index");
        removeIndex = GUILayout.TextField(removeIndex);
        if (GUILayout.Button("Remove"))
        {
            infiniteScrollView.Remove(int.Parse(removeIndex));
        }
        #endregion

        #region Snap
        GUILayout.Label("Snap Index");
        snapIndex = GUILayout.TextField(snapIndex);
        if (GUILayout.Button("Snap"))
        {
            infiniteScrollView.Snap(int.Parse(snapIndex), 0.1f);
        }
        #endregion

        #region Scroll
        GUILayout.Label("Horizontal");
        if (GUILayout.Button("Scroll to left"))
        {
            infiniteScrollView.ScrollToLeft();
        }

        if (GUILayout.Button("Scroll to right"))
        {
            infiniteScrollView.ScrollToRight();
        }
        #endregion
    }
}
