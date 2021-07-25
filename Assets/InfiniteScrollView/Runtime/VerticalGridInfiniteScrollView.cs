﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HowTungTung
{
    public class VerticalGridInfiniteScrollView : InfiniteScrollView
    {
        public bool isAtTop = true;
        public bool isAtBottom = true;
        public int columeCount = 1;

        protected override void OnValueChanged(Vector2 normalizedPosition)
        {
            if (columeCount <= 0)
            {
                columeCount = 1;
            }
            float viewportInterval = scrollRect.viewport.rect.height;
            float minViewport = scrollRect.content.anchoredPosition.y;
            Vector2 viewportRange = new Vector2(minViewport, minViewport + viewportInterval);
            float contentHeight = padding.x;
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
                contentHeight += dataList[i].cellSize.y + spacing;
            }
            contentHeight = padding.x;
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
                        SetupCell(index, new Vector2((dataList[index].cellSize.x + spacing) * j, -contentHeight));
                        if (visibleRange.y >= viewportRange.x)
                            cellList[index].transform.SetAsLastSibling();
                        else
                            cellList[index].transform.SetAsFirstSibling();
                    }
                }
                contentHeight += dataList[i].cellSize.y + spacing;
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

        public sealed override void Refresh()
        {
            if (!IsInitialized)
            {
                Initialize();
            }
            if (scrollRect.viewport.rect.height == 0)
                StartCoroutine(DelayToRefresh());
            else
                DoRefresh();
        }

        private void DoRefresh()
        {
            float height = padding.x;
            for (int i = 0; i < dataList.Count; i += columeCount)
            {
                height += dataList[i].cellSize.y + spacing;
            }
            for (int i = 0; i < cellList.Count; i++)
            {
                RecycleCell(i);
            }
            height += padding.y;
            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, height);
            OnValueChanged(scrollRect.normalizedPosition);
            onRefresh?.Invoke();
        }

        private IEnumerator DelayToRefresh()
        {
            yield return waitEndOfFrame;
            DoRefresh();
        }

        public override void Snap(int index, float duration)
        {
            if (!IsInitialized)
                return;
            if (index >= dataList.Count)
                return;
            var rowNumber = index / columeCount;
            var height = padding.x;
            for (int i = 0; i < rowNumber; i++)
            {
                height += dataList[i * columeCount].cellSize.y + spacing;
            }
            height = Mathf.Min(scrollRect.content.rect.height - scrollRect.viewport.rect.height, height);
            if (scrollRect.content.anchoredPosition.y != height)
            {
                DoSnapping(new Vector2(0, height), duration);
            }
        }
    }
}