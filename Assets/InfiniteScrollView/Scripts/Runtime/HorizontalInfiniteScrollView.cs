using Cysharp.Threading.Tasks;
using UnityEngine;

namespace HowTungTung
{
    public class HorizontalInfiniteScrollView : InfiniteScrollView
    {
        public float spacing;

        public override void RefreshCellVisibility()
        {
            if (dataList.Count == 0)
                return;

            // Viewport
            float viewportInterval = scrollRect.viewport.rect.width;

            // Check content direction pivot
            if (this._contentDirCoeff == 0) this._contentDirCoeff = scrollRect.content.pivot.x > 0 ? 1f : -1f;

            // Set content direction
            float minViewport = scrollRect.content.anchoredPosition.x * this._contentDirCoeff;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);

            // Hide
            float contentWidth = padding.left;
            for (int i = 0; i < dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentWidth, contentWidth + dataList[i].cellSize.x);
                if (visibleRange.y < viewportRange.x || visibleRange.x > viewportRange.y)
                {
                    RecycleCell(i);
                }
                contentWidth += dataList[i].cellSize.x + spacing;
            }

            // Show
            contentWidth = padding.left;
            for (int i = 0; i < dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentWidth, contentWidth + dataList[i].cellSize.x);
                if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                {
                    InfiniteCell cell = null;
                    if (cellList[i] == null)
                    {
                        if (_cellPool.Count > 0) cell = _cellPool.Dequeue();
                        else Debug.Log("<color=#ff4242>The cell display error occurred, not enough cells in the cell pool!!!</color>");
                    }
                    // Check cell direciton pivot
                    float dirCoeff = 1f;
                    if (cell != null) dirCoeff = cell.RectTransform.pivot.x > 0 ? -1f : 1f;
                    SetupCell(cell, i, new Vector2(contentWidth * dirCoeff, -(padding.top - padding.bottom)));
                    if (visibleRange.y >= viewportRange.x)
                        cellList[i]?.transform.SetAsLastSibling();
                    else
                        cellList[i]?.transform.SetAsFirstSibling();
                }
                contentWidth += dataList[i].cellSize.x + spacing;
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
            for (int i = 0; i < dataList.Count; i++)
            {
                width += dataList[i].cellSize.x + spacing;
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
            if (scrollRect.content.rect.width < scrollRect.viewport.rect.width)
                return;
            float width = padding.left;
            for (int i = 0; i < index; i++)
            {
                width += dataList[i].cellSize.x + spacing;
            }

            width = this.CalculateSnapPos(ScrollType.Horizontal, this.snapAlign, width, dataList[index]);

            if (scrollRect.content.anchoredPosition.x != width)
            {
                // Check content direction pivot
                DoSnapping(new Vector2(width * this._contentDirCoeff, 0), duration);
            }
        }

        public override async UniTask Remove(int index, bool withRefresh = true)
        {
            if (index >= dataList.Count ||
                index < 0)
                return;

            var removeCell = dataList[index];
            await base.Remove(index, withRefresh);
            scrollRect.content.anchoredPosition -= new Vector2(removeCell.cellSize.x + spacing, 0);
        }
    }
}