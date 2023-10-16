using HowTungTung;
using UnityEngine;
using UnityEngine.UI;

public class DemoVerticalGridCell : InfiniteCell
{
    public Text text;

    public override void OnUpdate()
    {
        RectTransform.sizeDelta = CellData.cellSize;
        text.text = CellData.index.ToString();
    }

    public void OnClicked()
    {
        InvokeSelected();
    }
}
