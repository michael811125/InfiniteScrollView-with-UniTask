using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InfiniteScrollViews
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

        [Header("------ Cell Pool Options ------")]
        public bool initializePoolOnAwake = false;
        public int cellPoolSize = 20;
        public InfiniteCell cellPrefab;

        [Space()]

        [Header("------ Cell View Options ------")]
        public float extendVisibleRange;
        [HideInInspector] public ScrollRect scrollRect;
        protected List<InfiniteCellData> _dataList = new List<InfiniteCellData>();
        protected List<InfiniteCell> _cellList = new List<InfiniteCell>();
        protected Queue<InfiniteCell> _cellPool = new Queue<InfiniteCell>();
        public SnapAlign snapAlign = SnapAlign.Start;
        public Padding padding;

        // Visible info
        protected bool _disabledRefreshCells = false;
        public int visibleCount { get; protected set; } = 0;
        public int lastMaxVisibleCount { get; protected set; } = 0;
        public bool isVisibleRangeFilled { get; protected set; } = false;

        // Direction pivot 
        protected float _contentDirCoeff = 0;

        // Scroll status
        protected bool _isAtTop = false;
        protected bool _isAtBottom = false;
        protected bool _isAtLeft = false;
        protected bool _isAtRight = false;

        // Callbacks
        public Action<Vector2> onValueChanged;
        public event Action onRectTransformDimensionsChanged;
        public event Action<InfiniteCell> onCellSelected;
        public Action onRefreshed;

        // Task cancellation
        private CancellationTokenSource _cts;

        public bool isInitialized
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
        /// Get data count (Equals to cell count)
        /// </summary>
        /// <returns></returns>
        public int DataCount()
        {
            return this._dataList.Count;
        }

        /// <summary>
        /// Init infinite-cell of scrollView
        /// </summary>
        /// <returns></returns>
        public virtual async UniTask InitializePool(object args = null)
        {
            if (isInitialized)
                return;

            if (scrollRect == null) scrollRect = this.GetComponent<ScrollRect>();
            scrollRect.onValueChanged.RemoveAllListeners();
            scrollRect.onValueChanged.AddListener(OnValueChanged);

            // Clear children
            foreach (Transform trans in this.scrollRect.content)
            {
                Destroy(trans.gameObject);
            }

            _dataList.Clear();
            _cellList.Clear();
            _cellPool.Clear();

            for (int i = 0; i < cellPoolSize; i++)
            {
                var newCell = Instantiate(cellPrefab, scrollRect.content);
                await newCell.OnCreate(args);
                newCell.gameObject.SetActive(false);
                _cellPool.Enqueue(newCell);
            }
            isInitialized = true;
        }

        protected void OnValueChanged(Vector2 normalizedPosition)
        {
            // If ever set to false, must refresh all once
            if (this._disabledRefreshCells)
            {
                this._disabledRefreshCells = false;
                this.Refresh();
            }
            else this.RefreshCellVisibilityWithCheck();

            // Invoke callback
            this.onValueChanged?.Invoke(normalizedPosition);
        }

        public void RefreshCellVisibilityWithCheck()
        {
            if (!this.IsInitialized()) return;
            this.RefreshCellVisibility();
        }

        /// <summary>
        /// Refresh visible cells
        /// </summary>
        protected abstract void RefreshCellVisibility();

        /// <summary>
        /// Refresh scrollView (doesn't need to await, if scrollView already initialized)
        /// </summary>
        /// <param name="disabledRefreshCells">Disable refresh cells, when disabled will mark flag to refresh all at next scrolling.</param>
        /// <returns></returns>
        public abstract void Refresh(bool disabledRefreshCells = false);

        protected abstract void DoRefresh(bool disabledRefreshCells);

        protected abstract UniTask DelayToRefresh(bool disabledRefreshCells);

        protected abstract void RefreshAndCheckVisibleInfo();

        protected bool IsInitialized()
        {
            if (!this.isInitialized)
            {
                Debug.Log("<color=#ff0073>[InfiniteScrollView] Please InitializePool first!!!</color>");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Add cell data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="autoRefresh"></param>
        /// <returns></returns>
        public virtual void Add(InfiniteCellData data, bool autoRefresh = false)
        {
            if (!this.IsInitialized()) return;

            _dataList.Add(data);
            _cellList.Add(null);
            this.RefreshCellDataIndex(_dataList.Count - 1);
            if (autoRefresh) this.Refresh();
            this.RefreshAndCheckVisibleInfo();
        }

        /// <summary>
        /// Insert cell data
        /// </summary>
        /// <param name="index"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public virtual bool Insert(int index, InfiniteCellData data)
        {
            if (!this.IsInitialized()) return false;

            // Insert including max count
            if (index > _dataList.Count ||
                index < 0)
                return false;

            _dataList.Insert(index, data);
            _cellList.Insert(index, null);
            this.RefreshCellDataIndex(index);
            return true;
        }

        /// <summary>
        /// Remove cell data
        /// </summary>
        /// <param name="index"></param>
        /// <param name="autoRefresh"></param>
        /// <returns></returns>
        public virtual bool Remove(int index, bool autoRefresh = true)
        {
            if (!this.IsInitialized()) return false;

            if (index >= _dataList.Count ||
                index < 0)
                return false;

            this._dataList[index].Dispose();
            this._dataList.RemoveAt(index);
            this.RefreshCellDataIndex(index);
            RecycleCell(index);
            _cellList.RemoveAt(index);
            if (autoRefresh) this.Refresh();
            this.RefreshAndCheckVisibleInfo();
            return true;
        }

        /// <summary>
        /// Scroll to top
        /// </summary>
        public void ScrollToTop()
        {
            if (this.scrollRect == null) return;
            this.scrollRect.verticalNormalizedPosition = 1;
        }

        /// <summary>
        /// Scroll to bottom
        /// </summary>
        public void ScrollToBottom()
        {
            if (this.scrollRect == null) return;
            this.scrollRect.verticalNormalizedPosition = 0;
        }

        /// <summary>
        /// Scroll to left
        /// </summary>
        public void ScrollToLeft()
        {
            if (this.scrollRect == null) return;
            this.scrollRect.horizontalNormalizedPosition = 0;
        }

        /// <summary>
        /// Scroll to right
        /// </summary>
        public void ScrollToRight()
        {
            if (this.scrollRect == null) return;
            this.scrollRect.horizontalNormalizedPosition = 1;
        }

        /// <summary>
        /// Check view to top 
        /// </summary>
        /// <param name="topDistance"></param>
        /// <returns></returns>
        public bool IsAtTop()
        {
            if (this.scrollRect == null) return false;
            // Adjust direction (Vertical = 1, Vertical Reverse = -1)
            bool result = this._contentDirCoeff > 0 ? this._isAtTop : this._isAtBottom;
            return result;
        }

        /// <summary>
        /// Check view to bottom
        /// </summary>
        /// <param name="bottomDistance"></param>
        /// <returns></returns>
        public bool IsAtBottom()
        {
            if (this.scrollRect == null) return false;
            // Adjust direction (Vertical = 1, Vertical Reverse = -1)
            bool result = this._contentDirCoeff > 0 ? this._isAtBottom : this._isAtTop;
            return result;
        }

        /// <summary>
        /// Check view to left
        /// </summary>
        /// <returns></returns>
        public bool IsAtLeft()
        {
            if (this.scrollRect == null) return false;
            // Adjust direction (Horizontal = -1, Horizontal Reverse = 1)
            bool result = this._contentDirCoeff > 0 ? this._isAtRight : this._isAtLeft;
            return result;
        }

        /// <summary>
        /// Check view to right
        /// </summary>
        /// <returns></returns>
        public bool IsAtRight()
        {
            if (this.scrollRect == null) return false;
            // Adjust direction (Horizontal = -1, Horizontal Reverse = 1)
            bool result = this._contentDirCoeff > 0 ? this._isAtLeft : this._isAtRight;
            return result;
        }

        /// <summary>
        /// Move to specific cell by index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="duration"></param>
        public abstract void Snap(int index, float duration);

        /// <summary>
        /// Move to last cell
        /// </summary>
        /// <param name="duration"></param>
        public void SnapLast(float duration)
        {
            Snap(_dataList.Count - 1, duration);
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
                if (this._disabledRefreshCells)
                {
                    this._disabledRefreshCells = false;
                    this.Refresh();
                }
                else this.RefreshCellVisibilityWithCheck();
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

                /**
                 * When scrolling, OnValueChanged will be called
                 */
            }

            // After snap end to release cts
            this._cts.Cancel();
            this._cts.Dispose();
            this._cts = null;
        }

        protected void SetupCell(InfiniteCell cell, int index, Vector2 pos)
        {
            if (cell != null)
            {
                _cellList[index] = cell;
                cell.CellData = _dataList[index];
                cell.RectTransform.anchoredPosition = pos;
                cell.onSelected += OnCellSelected;
                cell.gameObject.SetActive(true);
            }
        }

        protected void RecycleCell(int index)
        {
            if (_cellList[index] != null)
            {
                var cell = _cellList[index];
                _cellList[index] = null;
                cell.onSelected -= OnCellSelected;
                cell.gameObject.SetActive(false);
                cell.OnRecycle();
                _cellPool.Enqueue(cell);
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
        public virtual void Clear()
        {
            if (!this.IsInitialized()) return;

            scrollRect.velocity = Vector2.zero;
            scrollRect.content.anchoredPosition = Vector2.zero;
            for (int i = 0; i < _dataList.Count; i++)
            {
                _dataList[i].Dispose();
            }
            _dataList.Clear();
            for (int i = 0; i < _cellList.Count; i++)
            {
                RecycleCell(i);
            }
            _cellList.Clear();
            this.Refresh();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (scrollRect)
            {
                onRectTransformDimensionsChanged?.Invoke();
            }
        }

        private void RefreshCellDataIndex(int beginIndex)
        {
            // Optimized refresh efficiency
            for (int i = beginIndex; i < _dataList.Count; i++)
            {
                _dataList[i].index = i;
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