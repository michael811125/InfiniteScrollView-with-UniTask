using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HowTungTung
{
    public class HorizontalGridInfiniteScrollView : InfiniteScrollView
    {
        public Vector2 spacing;
        public int rowCount = 1;
        public bool isAtLeft = true;
        public bool isAtRight = true;

        public override void RefreshCellVisibility()
        {
            if (rowCount <= 0)
            {
                rowCount = 1;
            }
            float viewportInterval = scrollRect.viewport.rect.width;
            float minViewport = -scrollRect.content.anchoredPosition.x;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);
            float contentWidth = padding.left;
            for (int i = 0; i < dataList.Count; i += rowCount)
            {
                for (int j = 0; j < rowCount; j++)
                {
                    int index = i + j;
                    if (index >= dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentWidth, contentWidth + dataList[index].cellSize.x);
                    if (visibleRange.y < viewportRange.x || visibleRange.x > viewportRange.y)
                    {
                        RecycleCell(index);
                    }
                }
                contentWidth += dataList[i].cellSize.x + spacing.x;
            }
            contentWidth = padding.left;
            for (int i = 0; i < dataList.Count; i += rowCount)
            {
                for (int j = 0; j < rowCount; j++)
                {
                    int index = i + j;
                    if (index >= dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentWidth, contentWidth + dataList[index].cellSize.x);
                    if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                    {
                        SetupCell(index, new Vector2(contentWidth, (dataList[index].cellSize.y + spacing.y) * -j + -(padding.top - padding.bottom)));
                        if (visibleRange.y >= viewportRange.x)
                            cellList[index].transform.SetAsLastSibling();
                        else
                            cellList[index].transform.SetAsFirstSibling();
                    }
                }
                contentWidth += dataList[i].cellSize.x + spacing.x;
            }
            if (scrollRect.content.sizeDelta.x > viewportInterval)
            {
                isAtLeft = viewportRange.x + extendVisibleRange + dataList[0].cellSize.x <= dataList[0].cellSize.x;
                isAtRight = scrollRect.content.sizeDelta.x - viewportRange.y + extendVisibleRange + dataList[dataList.Count - 1].cellSize.x <= dataList[dataList.Count - 1].cellSize.x;
            }
            else
            {
                isAtLeft = true;
                isAtRight = true;
            }
        }

        public sealed override async UniTask Refresh()
        {
            if (!IsInitialized)
            {
                await InitializePool();
            }
            if (scrollRect.viewport.rect.width == 0)
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

            float width = padding.left;
            for (int i = 0; i < dataList.Count; i += rowCount)
            {
                width += dataList[i].cellSize.x + spacing.x;
            }
            for (int i = 0; i < cellList.Count; i++)
            {
                RecycleCell(i);
            }
            width += padding.right;
            scrollRect.content.sizeDelta = new Vector2(width, scrollRect.content.sizeDelta.y);
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
            if (index >= dataList.Count ||
                index < 0)
                return;
            var columeNumber = index / rowCount;
            float width = padding.left;
            for (int i = 0; i < columeNumber; i++)
            {
                width += dataList[i * rowCount].cellSize.x + spacing.x;
            }

            width = this.CalculateSnapPos(ScrollType.Horizontal, this.snapAlign, width, dataList[index]);

            if (scrollRect.content.anchoredPosition.x != width)
            {
                DoSnapping(new Vector2(-width, 0), duration);
            }
        }
    }
}

