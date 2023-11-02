using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InfiniteScrollViews
{
    public class VerticalInfiniteScrollView : InfiniteScrollView
    {
        public float spacing;

        protected override void RefreshCellVisibility()
        {
            if (_dataList.Count == 0)
                return;

            // Viewport
            float viewportInterval = scrollRect.viewport.rect.height;

            // Check content direction pivot
            if (this._contentDirCoeff == 0) this._contentDirCoeff = scrollRect.content.pivot.y > 0 ? 1f : -1f;

            // Set content direction
            float minViewport = scrollRect.content.anchoredPosition.y * this._contentDirCoeff;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);

            // Hide
            float contentHeight = padding.top;
            for (int i = 0; i < _dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentHeight, contentHeight + _dataList[i].cellSize.y);
                if (visibleRange.y < viewportRange.x || visibleRange.x > viewportRange.y)
                {
                    RecycleCell(i);
                }
                contentHeight += _dataList[i].cellSize.y + spacing;
            }

            // Show
            contentHeight = padding.top;
            for (int i = 0; i < _dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentHeight, contentHeight + _dataList[i].cellSize.y);
                if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                {
                    InfiniteCell cell = null;
                    if (_cellList[i] == null)
                    {
                        if (_cellPool.Count > 0) cell = _cellPool.Dequeue();
                        else Debug.Log("<color=#ff4242>The cell display error occurred, not enough cells in the cell pool!!!</color>");
                    }
                    // Check cell direciton pivot
                    float dirCoeff = 1f;
                    if (cell != null) dirCoeff = cell.RectTransform.pivot.y > 0 ? -1f : 1f;
                    SetupCell(cell, i, new Vector2(padding.left - padding.right, contentHeight * dirCoeff));
                    if (visibleRange.y >= viewportRange.x)
                        _cellList[i]?.transform.SetAsLastSibling();
                    else
                        _cellList[i]?.transform.SetAsFirstSibling();
                }
                contentHeight += _dataList[i].cellSize.y + spacing;
            }

            // Check scroll position
            if (scrollRect.content.sizeDelta.y > viewportInterval)
            {
                this._isAtTop = viewportRange.x + extendVisibleRange <= 0.001f;
                this._isAtBottom = scrollRect.content.sizeDelta.y - viewportRange.y + extendVisibleRange <= 0.001f;
            }
            else
            {
                this._isAtTop = true;
                this._isAtBottom = true;
                this._isAtLeft = false;
                this._isAtRight = false;
            }
        }

        public sealed override void Refresh(bool disabledRefreshCells = true)
        {
            if (!IsInitialized()) return;

            if (scrollRect.viewport.rect.height == 0)
            {
                this.DelayToRefresh(disabledRefreshCells).Forget();
            }
            else
            {
                this.DoRefresh(disabledRefreshCells);
            }
        }

        protected sealed override void DoRefresh(bool disabledRefreshCells)
        {
            if (scrollRect == null) return;

            if (!disabledRefreshCells)
            {
                // Refresh content size
                float height = padding.top;
                for (int i = 0; i < _dataList.Count; i++)
                {
                    height += _dataList[i].cellSize.y + spacing;
                }
                height += padding.bottom;
                scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, height);

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

        protected override void RefreshAndCheckVisibleInfo()
        {
            // Reset visible count
            this.visibleCount = 0;

            // Viewport
            float viewportInterval = scrollRect.viewport.rect.height;

            // Check content direction pivot
            if (this._contentDirCoeff == 0) this._contentDirCoeff = scrollRect.content.pivot.y > 0 ? 1f : -1f;

            // Set content direction
            float minViewport = scrollRect.content.anchoredPosition.y * this._contentDirCoeff;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);

            // Show
            float contentHeight = padding.top;
            for (int i = 0; i < _dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentHeight, contentHeight + _dataList[i].cellSize.y);
                if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                {
                    // Calcuate visible count
                    this.visibleCount++;

                    // Check filled flag
                    if (_cellList[i] == null) this.isVisibleRangeFilled = false;
                    else this.isVisibleRangeFilled = true;
                }
                contentHeight += _dataList[i].cellSize.y + spacing;
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
            if (scrollRect.content.rect.height < scrollRect.viewport.rect.height)
                return;
            float height = padding.top;
            for (int i = 0; i < index; i++)
            {
                height += _dataList[i].cellSize.y + spacing;
            }

            height = this.CalculateSnapPos(ScrollType.Vertical, this.snapAlign, height, _dataList[index]);

            if (scrollRect.content.anchoredPosition.y != height)
            {
                // Check content direction pivot
                DoSnapping(new Vector2(0, height * this._contentDirCoeff), duration);
            }
        }

        public override bool Remove(int index, bool withRefresh = true)
        {
            if (!this.IsInitialized())
                return false;
            if (index >= _dataList.Count ||
                index < 0)
                return false;

            var removeCell = _dataList[index];
            bool result = base.Remove(index, withRefresh);
            scrollRect.content.anchoredPosition -= new Vector2(0, removeCell.cellSize.y + spacing);
            return result;
        }
    }
}