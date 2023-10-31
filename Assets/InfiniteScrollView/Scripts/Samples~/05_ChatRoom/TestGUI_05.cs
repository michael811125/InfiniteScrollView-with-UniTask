﻿using Cysharp.Threading.Tasks;
using HowTungTung;
using UnityEngine;
using UnityEngine.UI;

public class TestGUI_05 : MonoBehaviour
{
    public InfiniteScrollView chatScrollView;
    public Text heightInstrument;
    public float baseCellHeight = 20;
    public string myName = "HowTungTung";
    private string speaker = "Tester";
    private string message = "In a recent blog post we introduced the concept of Scriptable Render Pipelines. In short, SRP allow developers to control how Unity renders a frame in C#. We will release two built-in render pipelines with Unity 2018.1: the Lightweight Pipeline and HD Pipeline. In this article we’re going to focus on the Lightweight Pipeline or LWRP.";

    private async void Awake()
    {
        chatScrollView = FindObjectOfType<InfiniteScrollView>();
        // Init cells first
        await chatScrollView.InitializePool();
    }

    private void OnGUI()
    {
        GUILayout.Label("Speaker");
        speaker = GUILayout.TextField(speaker);
        GUILayout.Label("Message");
        message = GUILayout.TextArea(message, GUILayout.MaxWidth(300), GUILayout.MaxHeight(100));
        if (GUILayout.Button("Add"))
        {
            AddChatData(new ChatCellData(speaker, message, false));
        }
    }

    public void OnSubmit(string input)
    {
        AddChatData(new ChatCellData(myName, input, true));
    }

    private void AddChatData(ChatCellData chatCellData)
    {
        heightInstrument.text = chatCellData.message;
        var infiniteData = new InfiniteCellData(new Vector2(0, heightInstrument.preferredHeight + baseCellHeight), chatCellData);
        chatScrollView.Add(infiniteData).Forget();
        chatScrollView.Refresh();
        chatScrollView.SnapLast(0.1f);
    }
}
