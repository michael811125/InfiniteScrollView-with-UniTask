using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HowTungTung
{
    public class VerticalGridInfiniteScrollView : InfiniteScrollView
    {
        public Vector2 spacing;
        public int columeCount = 1;

        public override void RefreshCellVisibility()
        {
            if (columeCount <= 0)
            {
                columeCount = 1;
            }

            // Viewport
            float viewportInterval = scrollRect.viewport.rect.height;

            // Check content direction pivot
            if (this._contentDirCoeff == 0) this._contentDirCoeff = scrollRect.content.pivot.y > 0 ? 1f : -1f;

            // Set content direction
            float minViewport = scrollRect.content.anchoredPosition.y * this._contentDirCoeff;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);

            // Hide
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

            // Show
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
                        InfiniteCell cell = null;
                        if (cellList[index] == null)
                        {
                            if (_cellPool.Count > 0) cell = _cellPool.Dequeue();
                            else Debug.Log("<color=#ff4242>The cell display error occurred, not enough cells in the cell pool!!!</color>");
                        }
                        // Check cell direciton pivot
                        float dirCoeff = 1f;
                        if (cell != null) dirCoeff = cell.RectTransform.pivot.y > 0 ? -1f : 1f;
                        SetupCell(cell, index, new Vector2((dataList[index].cellSize.x + spacing.x) * j + (padding.left - padding.right), contentHeight * dirCoeff));
                        if (visibleRange.y >= viewportRange.x)
                            cellList[index]?.transform.SetAsLastSibling();
                        else
                            cellList[index]?.transform.SetAsFirstSibling();
                    }
                }
                contentHeight += dataList[i].cellSize.y + spacing.y;
            }

            // Check scroll position
            if (scrollRect.content.sizeDelta.y > viewportInterval)
            {
                this._isAtTop = viewportRange.x + extendVisibleRange <= dataList[0].cellSize.y;
                this._isAtBottom = scrollRect.content.sizeDelta.y - viewportRange.y + extendVisibleRange <= dataList[dataList.Count - 1].cellSize.y;
            }
            else
            {
                this._isAtTop = true;
                this._isAtBottom = true;
                this._isAtLeft = false;
                this._isAtRight = false;
            }
        }

        public sealed override async UniTask Refresh()
        {
            if (!IsInitialized)
            {
                await InitializePool();
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
            onRefreshed?.Invoke();
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
            var rowNumber = index / columeCount;
            float height = padding.top;
            for (int i = 0; i < rowNumber; i++)
            {
                height += dataList[i * columeCount].cellSize.y + spacing.y;
            }

            height = this.CalculateSnapPos(ScrollType.Vertical, this.snapAlign, height, dataList[index]);

            if (scrollRect.content.anchoredPosition.y != height)
            {
                // Check content direction pivot
                DoSnapping(new Vector2(0, height * this._contentDirCoeff), duration);
            }
        }
    }
}