using HowTungTung;
using UnityEngine;
using UnityEngine.UI;

public class DemoTabPageCell : InfiniteCell
{
    public Text text;

    public override void OnUpdate()
    {
        DemoTabPageData data = (DemoTabPageData)CellData.data;
        RectTransform.sizeDelta = CellData.cellSize;
        text.text = data.content;
    }
}
