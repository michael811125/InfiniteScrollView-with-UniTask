using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace HowTungTung
{
    public class HorizontalInfiniteScrollView : InfiniteScrollView
    {
        public float spacing;
        public bool isAtLeft = true;
        public bool isAtRight = true;

        public override async UniTask Initialize(object args = null)
        {
            await base.Initialize(args);
            isAtLeft = true;
            isAtRight = true;
        }

        public override void RefreshCellVisibility()
        {
            if (dataList.Count == 0)
                return;
            float viewportInterval = scrollRect.viewport.rect.width;
            float minViewport = -scrollRect.content.anchoredPosition.x;
            Vector2 viewportRange = new Vector2(minViewport - extendVisibleRange, minViewport + viewportInterval + extendVisibleRange);
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
            contentWidth = padding.left;
            for (int i = 0; i < dataList.Count; i++)
            {
                var visibleRange = new Vector2(contentWidth, contentWidth + dataList[i].cellSize.x);
                if (visibleRange.y >= viewportRange.x && visibleRange.x <= viewportRange.y)
                {
                    SetupCell(i, new Vector2(contentWidth, -(padding.top - padding.bottom)));
                    if (visibleRange.y >= viewportRange.x)
                        cellList[i].transform.SetAsLastSibling();
                    else
                        cellList[i].transform.SetAsFirstSibling();
                }
                contentWidth += dataList[i].cellSize.x + spacing;
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
                await Initialize();
            }
            if (scrollRect.viewport.rect.width == 0)
                await DelayToRefresh();
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
                DoSnapping(new Vector2(-width, 0), duration);
            }
        }

        public override async UniTask Remove(int index, bool withRefresh = true)
        {
            if (index >= dataList.Count)
                return;

            var removeCell = dataList[index];
            await base.Remove(index, withRefresh);
            scrollRect.content.anchoredPosition -= new Vector2(removeCell.cellSize.x + spacing, 0);
        }
    }
}