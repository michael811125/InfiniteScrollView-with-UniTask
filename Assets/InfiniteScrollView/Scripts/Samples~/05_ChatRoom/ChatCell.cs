using InfiniteScrollViews;
using UnityEngine;
using UnityEngine.UI;

public class ChatCell : InfiniteCell
{
    // Cell template components
    public Text speakerText;
    public Text messageText;

    public override void OnRefresh()
    {
        // Get cell data
        ChatCellData data = (ChatCellData)CellData.data;
        
        // Set cell data to cell template
        speakerText.text = data.speaker;
        messageText.text = data.message;
        speakerText.alignment = data.isSelf ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        messageText.alignment = data.isSelf ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        RectTransform.sizeDelta = CellData.cellSize;
    }
}
