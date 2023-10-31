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

        [Header("------ Cell Pool Options ------")]
        public bool initializePoolOnAwake = false;
        public int cellPoolSize = 20;
        public InfiniteCell cellPrefab;

        [Space()]

        [Header("------ Cell View Options ------")]
        public float extendVisibleRange;
        [HideInInspector] public ScrollRect scrollRect;
        public List<InfiniteCellData> dataList = new List<InfiniteCellData>();
        [HideInInspector] public List<InfiniteCell> cellList = new List<InfiniteCell>();
        protected Queue<InfiniteCell> _cellPool = new Queue<InfiniteCell>();
        public SnapAlign snapAlign = SnapAlign.Start;
        public Padding padding;

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

            if (scrollRect == null) scrollRect = this.GetComponent<ScrollRect>();
            scrollRect.onValueChanged.RemoveAllListeners();
            scrollRect.onValueChanged.AddListener(OnValueChanged);

            // Clear children
            foreach (Transform trans in this.scrollRect.content)
            {
                Destroy(trans.gameObject);
            }

            dataList.Clear();
            cellList.Clear();
            _cellPool.Clear();

            for (int i = 0; i < cellPoolSize; i++)
            {
                var newCell = Instantiate(cellPrefab, scrollRect.content);
                await newCell.OnCreate(args);
                newCell.gameObject.SetActive(false);
                _cellPool.Enqueue(newCell);
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

        protected void SetupCell(InfiniteCell cell, int index, Vector2 pos)
        {
            if (cell == null) return;

            cell.gameObject.SetActive(true);
            cell.CellData = dataList[index];
            cell.RectTransform.anchoredPosition = pos;
            cellList[index] = cell;
            cell.onSelected += OnCellSelected;
        }

        protected void RecycleCell(int index)
        {
            if (cellList[index] != null)
            {
                var cell = cellList[index];
                cellList[index] = null;
                _cellPool.Enqueue(cell);
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
                onRectTransformDimensionsChanged?.Invoke();
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