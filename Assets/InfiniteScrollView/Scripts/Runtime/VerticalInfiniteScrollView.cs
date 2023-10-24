using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace HowTungTung
{
    public class VerticalInfiniteScrollView : InfiniteScrollView
    {
        public float spacing;
        public bool isAtTop = true;
        public bool isAtBottom = true;

        public override async UniTask InitializePool(object args = null)
        {
            if (this.IsInitialized) return;

            await base.InitializePool(args);
            isAtTop = true;
            isAtBottom = true;
        }

        public override void RefreshCellVisibility()
        {
            if (dataList.Count == 0)
                return;
            float viewportInterval = scrollRect.viewport.rect.height;
            float minViewport = scrollRect.content.anchoredPosition.y;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);
            float contentHeight = padding.top;
            for (int i = 0; i < dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentHeight, contentHeight + dataList[i].cellSize.y);
                if (visibleRange.y < viewportRange.x || visibleRange.x > viewportRange.y)
                {
                    RecycleCell(i);
                }
                contentHeight += dataList[i].cellSize.y + spacing;
            }
            contentHeight = padding.top;
            for (int i = 0; i < dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentHeight, contentHeight + dataList[i].cellSize.y);
                if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                {
                    SetupCell(i, new Vector2(padding.left - padding.right, -contentHeight));
                    if (visibleRange.y >= viewportRange.x)
                        cellList[i].transform.SetAsLastSibling();
                    else
                        cellList[i].transform.SetAsFirstSibling();
                }
                contentHeight += dataList[i].cellSize.y + spacing;
            }
            if (scrollRect.content.sizeDelta.y > viewportInterval)
            {
                isAtTop = viewportRange.x + extendVisibleRange <= 0.001f;
                isAtBottom = scrollRect.content.sizeDelta.y - viewportRange.y + extendVisibleRange <= 0.001f;
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
            for (int i = 0; i < dataList.Count; i++)
            {
                height += dataList[i].cellSize.y + spacing;
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
            if (scrollRect.content.rect.height < scrollRect.viewport.rect.height)
                return;
            float height = padding.top;
            for (int i = 0; i < index; i++)
            {
                height += dataList[i].cellSize.y + spacing;
            }

            height = this.CalculateSnapPos(ScrollType.Vertical, this.snapAlign, height, dataList[index]);

            if (scrollRect.content.anchoredPosition.y != height)
            {
                DoSnapping(new Vector2(0, height), duration);
            }
        }

        public override async UniTask Remove(int index, bool withRefresh = true)
        {
            if (index >= dataList.Count ||
                index < 0)
                return;

            var removeCell = dataList[index];
            await base.Remove(index, withRefresh);
            scrollRect.content.anchoredPosition -= new Vector2(0, removeCell.cellSize.y + spacing);
        }
    }
}