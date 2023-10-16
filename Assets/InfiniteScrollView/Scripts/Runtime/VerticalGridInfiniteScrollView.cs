using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HowTungTung
{
    public class VerticalGridInfiniteScrollView : InfiniteScrollView
    {
        public Vector2 spacing;
        public int columeCount = 1;
        public bool isAtTop = true;
        public bool isAtBottom = true;

        public override void RefreshCellVisibility()
        {
            if (columeCount <= 0)
            {
                columeCount = 1;
            }
            float viewportInterval = scrollRect.viewport.rect.height;
            float minViewport = scrollRect.content.anchoredPosition.y;
            Vector2 viewportRange = new Vector2(minViewport, minViewport + viewportInterval);
            float contentHeight = padding.top;
            for (int i = 0; i < dataList.Count; i += columeCount)
            {
                for (int j = 0; j < columeCount; j++)
                {
                    int index = i + j;
                    if (index >= dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentHeight, contentHeight + dataList[index].cellSize.y);
                    if (visibleRange.y < viewportRange.x || visibleRange.x > viewportRange.y)
                    {
                        RecycleCell(index);
                    }
                }
                contentHeight += dataList[i].cellSize.y + spacing.y;
            }
            contentHeight = padding.top;
            for (int i = 0; i < dataList.Count; i += columeCount)
            {
                for (int j = 0; j < columeCount; j++)
                {
                    int index = i + j;
                    if (index >= dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentHeight, contentHeight + dataList[index].cellSize.y);
                    if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                    {
                        SetupCell(index, new Vector2((dataList[index].cellSize.x + spacing.x) * j + (padding.left - padding.right), -contentHeight));
                        if (visibleRange.y >= viewportRange.x)
                            cellList[index].transform.SetAsLastSibling();
                        else
                            cellList[index].transform.SetAsFirstSibling();
                    }
                }
                contentHeight += dataList[i].cellSize.y + spacing.y;
            }
            if (scrollRect.content.sizeDelta.y > viewportInterval)
            {
                isAtTop = viewportRange.x + extendVisibleRange <= dataList[0].cellSize.y;
                isAtBottom = scrollRect.content.sizeDelta.y - viewportRange.y + extendVisibleRange <= dataList[dataList.Count - 1].cellSize.y;
            }
            else
            {
                isAtTop = true;
                isAtBottom = true;
            }
        }

        public sealed override async UniTask Refresh()
        {
            if (!IsInitialized)
            {
                await Initialize();
            }
            if (scrollRect.viewport.rect.height == 0)
            {
                await DelayToRefresh();
            }
            else
            {
                DoRefresh();
            }
        }

        private void DoRefresh()
        {
            if (scrollRect == null) return;

            float height = padding.top;
            for (int i = 0; i < dataList.Count; i += columeCount)
            {
                height += dataList[i].cellSize.y + spacing.y;
            }
            for (int i = 0; i < cellList.Count; i++)
            {
                RecycleCell(i);
            }
            height += padding.bottom;
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, height);
            this.RefreshCellVisibility();
            onRefresh?.Invoke();
        }

        private async UniTask DelayToRefresh()
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            DoRefresh();
        }

        public override void Snap(int index, float duration)
        {
            if (!IsInitialized)
                return;
            if (index >= dataList.Count)
                return;
            var rowNumber = index / columeCount;
            float height = padding.top;
            for (int i = 0; i < rowNumber; i++)
            {
                height += dataList[i * columeCount].cellSize.y + spacing.y;
            }

            height = this.CalculateSnapPos(ScrollType.Vertical, this.snapAlign, height, dataList[index]);

            if (scrollRect.content.anchoredPosition.y != height)
            {
                DoSnapping(new Vector2(0, height), duration);
            }
        }
    }
}