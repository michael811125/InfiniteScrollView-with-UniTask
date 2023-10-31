## CHANGELOG

## [1.3.0] - 2023-10-31
- Modified All infiniteScrollViews can auto calculate direction by content and cell pivot.
- Modified Samples (Normal direction and Reverse direction).
- Added ScrollToLeft and ScrollToRight (Horizontal).
```C#
    public void ScrollToLeft()
    public void ScrollToRight()
```
- Added InfiniteScrollView IsAtLeft and IsAtRight.
```C#
    public bool IsAtLeft()
    public bool IsAtRight()
```
- Rename InfiniteScrollView IsScrollToTop method name to IsAtTop.
```C#
    public bool IsAtTop()
```
- Rename InfiniteScrollView IsScrollToBottom method name to IsAtBottom.
```C#
    public bool IsAtBottom()
```
- Removed ScrollToTarget method from InfiniteScrollView.
- Optimized code.

## [1.2.1] - 2023-10-24
- Modified InfiniteCell method name (OnUpdate change to OnRefresh more clear).
```C#
    public virtual void OnRefresh() { }
```
- Modified callback names in InfiniteScrollView.
  - onRectTransformUpdate change to onRectTransformDimensionsChanged.
  - onRefresh change onRefreshed.

## [1.2.0] - 2023-10-20
- Added [initializePoolOnAwake] trigger for InfiniteScrollView.
- Added OnClick in InfiniteCell for button event (Can assign event on button click).
- Modified InfiniteScrollView method name (Initialize change to InitializePool).
```C#
    public virtual async UniTask InitializePool(object args = null)
```
- Modified InfiniteCell method name (Initialize change to OnCreate).
```C#
    public virtual async UniTask OnCreate(object args) { }
```
- Modified InfiniteCellData index access modifier (Only internal can set).
- Optimizd index determines.

## [1.1.0] - 2023-10-17
- Added Cell script editor.

## [1.0.1] - 2023-10-17
- Fixed cellList count increase bug issue.
- Optimized InfiniteScrollView.

## [1.0.0] - 2023-10-16
- Added InfiniteScrollView with UniTask.