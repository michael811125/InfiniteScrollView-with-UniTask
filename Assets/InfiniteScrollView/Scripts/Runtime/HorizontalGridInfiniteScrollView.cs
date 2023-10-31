using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HowTungTung
{
    public class HorizontalGridInfiniteScrollView : InfiniteScrollView
    {
        public Vector2 spacing;
        public int rowCount = 1;

        public override void RefreshCellVisibility()
        {
            if (rowCount <= 0)
            {
                rowCount = 1;
            }

            // Viewport
            float viewportInterval = scrollRect.viewport.rect.width;

            // Check content direction pivot
            if (this._contentDirCoeff == 0) this._contentDirCoeff = scrollRect.content.pivot.x > 0 ? 1f : -1f;

            // Set content direction
            float minViewport = scrollRect.content.anchoredPosition.x * this._contentDirCoeff;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);

            // Hide
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

            // Show
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
                        InfiniteCell cell = null;
                        if (cellList[index] == null)
                        {
                            if (_cellPool.Count > 0) cell = _cellPool.Dequeue();
                            else Debug.Log("<color=#ff4242>The cell display error occurred, not enough cells in the cell pool!!!</color>");
                        }
                        // Check cell direciton pivot
                        float dirCoeff = 1f;
                        if (cell != null) dirCoeff = cell.RectTransform.pivot.x > 0 ? -1f : 1f;
                        SetupCell(cell, index, new Vector2(contentWidth * dirCoeff, (dataList[index].cellSize.y + spacing.y) * -j + -(padding.top - padding.bottom)));
                        if (visibleRange.y >= viewportRange.x)
                            cellList[index].transform.SetAsLastSibling();
                        else
                            cellList[index].transform.SetAsFirstSibling();
                    }
                }
                contentWidth += dataList[i].cellSize.x + spacing.x;
            }

            // Check scroll position
            if (scrollRect.content.sizeDelta.x > viewportInterval)
            {
                this._isAtLeft = viewportRange.x + extendVisibleRange + dataList[0].cellSize.x <= dataList[0].cellSize.x;
                this._isAtRight = scrollRect.content.sizeDelta.x - viewportRange.y + extendVisibleRange + dataList[dataList.Count - 1].cellSize.x <= dataList[dataList.Count - 1].cellSize.x;
            }
            else
            {
                this._isAtTop = false;
                this._isAtBottom = false;
                this._isAtLeft = true;
                this._isAtRight = true;
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
            var columeNumber = index / rowCount;
            float width = padding.left;
            for (int i = 0; i < columeNumber; i++)
            {
                width += dataList[i * rowCount].cellSize.x + spacing.x;
            }

            width = this.CalculateSnapPos(ScrollType.Horizontal, this.snapAlign, width, dataList[index]);

            if (scrollRect.content.anchoredPosition.x != width)
            {
                // Check content direction pivot
                DoSnapping(new Vector2(width * this._contentDirCoeff, 0), duration);
            }
        }
    }
}

