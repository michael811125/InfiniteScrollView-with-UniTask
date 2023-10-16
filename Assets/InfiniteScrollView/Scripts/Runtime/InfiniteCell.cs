using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

namespace HowTungTung
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
                    rectTransform = GetComponent<RectTransform>();
                return rectTransform;
            }
        }

        private InfiniteCellData cellData;
        public InfiniteCellData CellData
        {
            set
            {
                cellData = value;
                OnUpdate();
            }
            get
            {
                return cellData;
            }
        }

        public virtual async UniTask Initialize(object args) { await UniTask.Yield(); }

        public virtual void OnUpdate() { }

        public virtual void OnRecycle() { }

        public void InvokeSelected()
        {
            if (onSelected != null)
                onSelected.Invoke(this);
        }
    }
}

