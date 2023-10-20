using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HowTungTung;
using Cysharp.Threading.Tasks;

public class TestGUI_03 : MonoBehaviour
{
    private InfiniteScrollView infiniteScrollView;

    private string removeIndex = "0";
    private string snapIndex = "0";

    private async void Awake()
    {
        infiniteScrollView = FindObjectOfType<InfiniteScrollView>();
        // Init cells first
        await infiniteScrollView.InitializePool();
        infiniteScrollView.onCellSelected += OnCellSelected;
    }

    private void OnCellSelected(InfiniteCell selectedCell)
    {
        Debug.Log("On Cell Selected " + selectedCell.CellData.index);
    }

    private void OnGUI()
    {
        if (GUILayout.Button("NextScene"))
        {
            SceneManager.LoadScene((int)Mathf.Repeat(SceneManager.GetActiveScene().buildIndex + 1, SceneManager.sceneCountInBuildSettings));
        }
        if (GUILayout.Button("Add 1000 Cell"))
        {
            for (int i = 0; i < 1000; i++)
            {
                infiniteScrollView.Add(new InfiniteCellData(new Vector2(100, 100))).Forget();
            }
            infiniteScrollView.Refresh();
        }
        if (GUILayout.Button("Add"))
        {
            var data = new InfiniteCellData(new Vector2(100, 100));
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
