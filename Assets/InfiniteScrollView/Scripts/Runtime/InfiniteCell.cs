using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace InfiniteScrollViews
{
    public class InfiniteCell : MonoBehaviour
    {
        public event Action<InfiniteCell> onSelected;

        private RectTransform rectTransform;
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                    rectTransform = transform as RectTransform;
                return rectTransform;
            }
        }

        private InfiniteCellData cellData;
        public InfiniteCellData CellData
        {
            set
            {
                cellData = value;
                OnRefresh();
            }
            get
            {
                return cellData;
            }
        }

        public virtual async UniTask OnCreate(object args) { }

        public virtual void OnRefresh() { }

        public virtual void OnRecycle() { }

        /// <summary>
        /// Button event
        /// </summary>
        public void OnClick()
        {
            this.InvokeSelected();
        }

        protected void InvokeSelected()
        {
            if (onSelected != null)
                onSelected.Invoke(this);
        }
    }
}

