using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InfiniteScrollViews
{
    public class HorizontalGridInfiniteScrollView : InfiniteScrollView
    {
        public Vector2 spacing;
        public int rowCount = 1;

        protected override void RefreshCellVisibility()
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
            for (int i = 0; i < _dataList.Count; i += rowCount)
            {
                for (int j = 0; j < rowCount; j++)
                {
                    int index = i + j;
                    if (index >= _dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentWidth, contentWidth + _dataList[index].cellSize.x);
                    if (visibleRange.y < viewportRange.x || visibleRange.x > viewportRange.y)
                    {
                        RecycleCell(index);
                    }
                }
                contentWidth += _dataList[i].cellSize.x + spacing.x;
            }

            // Show
            contentWidth = padding.left;
            for (int i = 0; i < _dataList.Count; i += rowCount)
            {
                for (int j = 0; j < rowCount; j++)
                {
                    int index = i + j;
                    if (index >= _dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentWidth, contentWidth + _dataList[index].cellSize.x);
                    if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                    {
                        InfiniteCell cell = null;
                        if (_cellList[index] == null)
                        {
                            if (_cellPool.Count > 0) cell = _cellPool.Dequeue();
                            else Debug.Log("<color=#ff4242>The cell display error occurred, not enough cells in the cell pool!!!</color>");
                        }
                        // Check cell direciton pivot
                        float dirCoeff = 1f;
                        if (cell != null) dirCoeff = cell.RectTransform.pivot.x > 0 ? -1f : 1f;
                        SetupCell(cell, index, new Vector2(contentWidth * dirCoeff, (_dataList[index].cellSize.y + spacing.y) * -j + -(padding.top - padding.bottom)));
                        if (visibleRange.y >= viewportRange.x)
                            _cellList[index]?.transform.SetAsLastSibling();
                        else
                            _cellList[index]?.transform.SetAsFirstSibling();
                    }
                }
                contentWidth += _dataList[i].cellSize.x + spacing.x;
            }

            // Check scroll position
            if (scrollRect.content.sizeDelta.x > viewportInterval)
            {
                this._isAtLeft = viewportRange.x + extendVisibleRange + _dataList[0].cellSize.x <= _dataList[0].cellSize.x;
                this._isAtRight = scrollRect.content.sizeDelta.x - viewportRange.y + extendVisibleRange + _dataList[_dataList.Count - 1].cellSize.x <= _dataList[_dataList.Count - 1].cellSize.x;
            }
            else
            {
                this._isAtTop = false;
                this._isAtBottom = false;
                this._isAtLeft = true;
                this._isAtRight = true;
            }
        }

        public sealed override void Refresh(bool disabledRefreshCells = true)
        {
            if (!this.IsInitialized()) return;

            if (scrollRect.viewport.rect.width == 0)
            {
                DelayToRefresh(disabledRefreshCells).Forget();
            }
            else
            {
                DoRefresh(disabledRefreshCells);
            }
        }

        protected sealed override void DoRefresh(bool disabledRefreshCells)
        {
            if (scrollRect == null) return;

            if (!disabledRefreshCells)
            {
                // Refresh content size
                float width = padding.left;
                for (int i = 0; i < _dataList.Count; i += rowCount)
                {
                    width += _dataList[i].cellSize.x + spacing.x;
                }
                width += padding.right;
                scrollRect.content.sizeDelta = new Vector2(width, scrollRect.content.sizeDelta.y);

                // Recycle all cells first
                for (int i = 0; i < _cellList.Count; i++)
                {
                    RecycleCell(i);
                }

                // Refresh cells view
                this.RefreshCellVisibility();

                // Invoke onRefresh callback
                onRefreshed?.Invoke();
            }
            // Mark flag for refresh at next scrolling
            else this._disabledRefreshCells = true;
        }

        protected sealed override async UniTask DelayToRefresh(bool disabledRefreshCells)
        {
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            DoRefresh(disabledRefreshCells);
        }

        protected sealed override void RefreshAndCheckVisibleInfo()
        {
            // Reset visible count
            this.visibleCount = 0;

            // Viewport
            float viewportInterval = scrollRect.viewport.rect.width;

            // Check content direction pivot
            if (this._contentDirCoeff == 0) this._contentDirCoeff = scrollRect.content.pivot.x > 0 ? 1f : -1f;

            // Set content direction
            float minViewport = scrollRect.content.anchoredPosition.x * this._contentDirCoeff;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);

            // Show
            float contentWidth = padding.left;
            for (int i = 0; i < _dataList.Count; i += rowCount)
            {
                for (int j = 0; j < rowCount; j++)
                {
                    int index = i + j;
                    if (index >= _dataList.Count)
                        break;
                    var visibleRange = new Vector2(contentWidth, contentWidth + _dataList[index].cellSize.x);
                    if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                    {
                        // Calcuate visible count
                        this.visibleCount++;

                        // Check filled flag
                        if (_cellList[index] == null) this.isVisibleRangeFilled = false;
                        else this.isVisibleRangeFilled = true;
                    }
                }
                contentWidth += _dataList[i].cellSize.x + spacing.x;
            }

            // Adjust filled flag while cell removing
            if (this.visibleCount < this.lastMaxVisibleCount) this.isVisibleRangeFilled = false;
            this.lastMaxVisibleCount = this.visibleCount;
        }

        public override void Snap(int index, float duration)
        {
            if (!IsInitialized())
                return;
            if (index >= _dataList.Count ||
                index < 0)
                return;
            var columeNumber = index / rowCount;
            float width = padding.left;
            for (int i = 0; i < columeNumber; i++)
            {
                width += _dataList[i * rowCount].cellSize.x + spacing.x;
            }

            width = this.CalculateSnapPos(ScrollType.Horizontal, this.snapAlign, width, _dataList[index]);

            if (scrollRect.content.anchoredPosition.x != width)
            {
                // Check content direction pivot
                DoSnapping(new Vector2(width * this._contentDirCoeff, 0), duration);
            }
        }
    }
}

