using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HowTungTung
{
    [RequireComponent(typeof(ScrollRect))]
    public abstract class InfiniteScrollView : UIBehaviour
    {
        public enum SnapAlign
        {
            Start = 1,
            Center = 2,
            End = 3,
        }

        protected enum ScrollType
        {
            Vertical = 1,
            Horizontal = 2
        }

        [Serializable]
        public class Padding
        {
            public int top;
            public int bottom;
            public int left;
            public int right;
        }

        public bool initializePoolOnAwake = false;
        public int cellPoolSize = 20;
        public float extendVisibleRange;

        public InfiniteCell cellPrefab;
        public ScrollRect scrollRect;
        public List<InfiniteCellData> dataList = new List<InfiniteCellData>();
        public List<InfiniteCell> cellList = new List<InfiniteCell>();
        protected Queue<InfiniteCell> cellPool = new Queue<InfiniteCell>();
        private CancellationTokenSource _cts;
        public event Action onRectTransformUpdate;
        public Action<Vector2> onValueChanged;
        public event Action<InfiniteCell> onCellSelected;
        public Action onRefresh;
        public SnapAlign snapAlign = SnapAlign.Start;
        public Padding padding;

        public bool IsInitialized
        {
            get;
            protected set;
        }

        protected override async void Awake()
        {
            if (this.initializePoolOnAwake)
            {
                await this.InitializePool();
            }
        }

        /// <summary>
        /// Init infinite-cell of scrollView
        /// </summary>
        /// <returns></returns>
        public virtual async UniTask InitializePool(object args = null)
        {
            if (IsInitialized)
                return;

            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            scrollRect.onValueChanged.RemoveAllListeners();
            scrollRect.onValueChanged.AddListener(OnValueChanged);

            foreach (Transform trans in this.scrollRect.content)
            {
                Destroy(trans.gameObject);
            }

            dataList.Clear();
            cellList.Clear();
            cellPool.Clear();

            for (int i = 0; i < cellPoolSize; i++)
            {
                var newCell = Instantiate(cellPrefab, scrollRect.content);
                await newCell.OnCreate(args);
                newCell.gameObject.SetActive(false);
                cellPool.Enqueue(newCell);
            }
            IsInitialized = true;
        }

        protected void OnValueChanged(Vector2 normalizedPosition)
        {
            this.RefreshCellVisibility();
            this.onValueChanged?.Invoke(normalizedPosition);
        }

        /// <summary>
        /// Refresh visible cells
        /// </summary>
        public abstract void RefreshCellVisibility();

        /// <summary>
        /// Refresh scrollView (doesn't need to await, if scrollView already initialized)
        /// </summary>
        /// <returns></returns>
        public abstract UniTask Refresh();

        /// <summary>
        /// Add cell data (doesn't need to await, if scrollView already initialized)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="autoRefresh"></param>
        /// <returns></returns>
        public virtual async UniTask Add(InfiniteCellData data, bool autoRefresh = false)
        {
            if (!IsInitialized)
            {
                await InitializePool();
            }

            dataList.Add(data);
            cellList.Add(null);
            this.RefreshCellDataIndex(dataList.Count - 1);
            if (autoRefresh) await Refresh();
        }

        /// <summary>
        /// Insert cell data (doesn't need to await, if scrollView already initialized)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual async UniTask Insert(int index, InfiniteCellData data)
        {
            if (!IsInitialized)
            {
                await InitializePool();
            }

            // Insert including max count
            if (index > dataList.Count ||
                index < 0)
                return;

            dataList.Insert(index, data);
            cellList.Insert(index, null);
            this.RefreshCellDataIndex(index);
        }

        /// <summary>
        /// Remove cell data (doesn't need to await, if scrollView already initialized)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="autoRefresh"></param>
        /// <returns></returns>
        public virtual async UniTask Remove(int index, bool autoRefresh = true)
        {
            if (!IsInitialized)
            {
                await InitializePool();
            }

            if (index >= dataList.Count ||
                index < 0)
                return;

            dataList.RemoveAt(index);
            this.RefreshCellDataIndex(index);
            RecycleCell(index);
            cellList.RemoveAt(index);
            if (autoRefresh) await Refresh();
        }

        /// <summary>
        /// Move to bottom
        /// </summary>
        public void ScrollToBottom()
        {
            if (this.scrollRect == null) return;
            this.scrollRect.verticalNormalizedPosition = 0;
        }

        /// <summary>
        /// Move to top
        /// </summary>
        public void ScrollToTop()
        {
            if (this.scrollRect == null) return;
            this.scrollRect.verticalNormalizedPosition = 1;
        }

        /// <summary>
        /// Move to target
        /// </summary>
        /// <param name="child"></param>
        public void ScrollToTarget(Transform child)
        {
            if (this.scrollRect == null || child == null) return;
            Canvas.ForceUpdateCanvases();
            Vector2 viewportLocalPosition = this.scrollRect.viewport.localPosition;
            Vector2 childLocalPosition = child.localPosition;
            Vector2 result = new Vector2(
                0 - (viewportLocalPosition.x + childLocalPosition.x),
                0 - (viewportLocalPosition.y + childLocalPosition.y)
            );
            this.scrollRect.content.localPosition = result;
        }

        /// <summary>
        /// Check view to top 
        /// </summary>
        /// <param name="topDistance"></param>
        /// <returns></returns>
        public bool IsScrollToTop(float topDistance = 0)
        {
            if (this.scrollRect == null) return false;
            if (this.scrollRect.content.anchoredPosition.y <= 0 + topDistance) return true;
            return false;
        }

        /// <summary>
        /// Check view to bottom
        /// </summary>
        /// <param name="bottomDistance"></param>
        /// <returns></returns>
        public bool IsScrollToBottom(float bottomDistance = 0)
        {
            if (this.scrollRect == null) return false;
            if (this.scrollRect.content.anchoredPosition.y + this.scrollRect.viewport.rect.height >= this.scrollRect.content.rect.height - bottomDistance) return true;
            return false;
        }

        /// <summary>
        /// Move to specific cell by index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="duration"></param>
        public abstract void Snap(int index, float duration);

        public void SnapLast(float duration)
        {
            Snap(dataList.Count - 1, duration);
        }

        protected void DoSnapping(Vector2 target, float duration)
        {
            StopSnapping();

            this._cts = new CancellationTokenSource();
            this.ProcessSnapping(target, duration).Forget();
        }

        private void StopSnapping()
        {
            if (this._cts != null)
            {
                this._cts.Cancel();
                this._cts.Dispose();
                this._cts = null;
            }
        }

        private async UniTask ProcessSnapping(Vector2 target, float duration)
        {
            scrollRect.velocity = Vector2.zero;

            if (duration <= 0)
            {
                scrollRect.content.anchoredPosition = target;
                this.RefreshCellVisibility();
            }
            else
            {
                Vector2 startPos = scrollRect.content.anchoredPosition;
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime / duration;
                    scrollRect.content.anchoredPosition = Vector2.Lerp(startPos, target, t);
                    var normalizedPos = scrollRect.normalizedPosition;
                    if (normalizedPos.y < 0 || normalizedPos.x > 1)
                    {
                        break;
                    }
                    await UniTask.Yield(PlayerLoopTiming.Update, this._cts.Token);
                }
            }

            // After snap end to release cts
            this._cts.Cancel();
            this._cts.Dispose();
            this._cts = null;
        }

        protected void SetupCell(int index, Vector2 pos)
        {
            if (cellList[index] == null)
            {
                var cell = cellPool.Dequeue();
                cell.gameObject.SetActive(true);
                cell.CellData = dataList[index];
                cell.RectTransform.anchoredPosition = pos;
                cellList[index] = cell;
                cell.onSelected += OnCellSelected;
            }
        }

        protected void RecycleCell(int index)
        {
            if (cellList[index] != null)
            {
                var cell = cellList[index];
                cellList[index] = null;
                cellPool.Enqueue(cell);
                cell.gameObject.SetActive(false);
                cell.OnRecycle();
                cell.onSelected -= OnCellSelected;
            }
        }

        private void OnCellSelected(InfiniteCell selectedCell)
        {
            onCellSelected?.Invoke(selectedCell);
        }

        /// <summary>
        /// Clear cell data (doesn't need to await, if scrollView already initialized)
        /// </summary>
        /// <returns></returns>
        public virtual async UniTask Clear()
        {
            if (IsInitialized == false)
                await InitializePool();
            scrollRect.velocity = Vector2.zero;
            scrollRect.content.anchoredPosition = Vector2.zero;
            dataList.Clear();
            for (int i = 0; i < cellList.Count; i++)
            {
                RecycleCell(i);
            }
            cellList.Clear();
            await Refresh();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (scrollRect)
            {
                onRectTransformUpdate?.Invoke();
            }
        }

        private void RefreshCellDataIndex(int beginIndex)
        {
            // Optimized refresh efficiency
            for (int i = beginIndex; i < dataList.Count; i++)
            {
                dataList[i].index = i;
            }
        }

        protected float CalculateSnapPos(ScrollType scrollType, SnapAlign snapPosType, float originValue, InfiniteCellData cellData)
        {
            float newValue = 0;
            float viewPortRectSizeValue = 0;
            float contentRectSizeValue = 0;
            float cellSizeValue = 0;

            switch (scrollType)
            {
                case ScrollType.Horizontal:

                    viewPortRectSizeValue = scrollRect.viewport.rect.width;
                    contentRectSizeValue = scrollRect.content.rect.width;
                    cellSizeValue = cellData.cellSize.x;

                    break;

                case ScrollType.Vertical:

                    viewPortRectSizeValue = scrollRect.viewport.rect.height;
                    contentRectSizeValue = scrollRect.content.rect.height;
                    cellSizeValue = cellData.cellSize.y;

                    break;
            }

            // Cannot scoll, if Content size < Viewport size return 0 directly
            if (contentRectSizeValue < viewPortRectSizeValue) return 0;

            switch (snapPosType)
            {
                case SnapAlign.Start:
                    newValue = Mathf.Clamp(originValue, 0, contentRectSizeValue - viewPortRectSizeValue);
                    break;
                case SnapAlign.Center:
                    newValue = Mathf.Clamp(originValue - (viewPortRectSizeValue / 2 - cellSizeValue / 2), 0, contentRectSizeValue - viewPortRectSizeValue);
                    break;
                case SnapAlign.End:
                    newValue = Mathf.Clamp(originValue - (viewPortRectSizeValue - cellSizeValue), 0, contentRectSizeValue - viewPortRectSizeValue);
                    break;
            }

            return newValue;
        }
    }
}