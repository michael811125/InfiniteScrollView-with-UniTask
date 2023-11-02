using InfiniteScrollViews;
using UnityEngine;
using UnityEngine.UI;

public class DemoGridCell : InfiniteCell
{
    public Text text;

    public override void OnRefresh()
    {
        RectTransform.sizeDelta = CellData.cellSize;
        text.text = CellData.index.ToString();
    }
}
