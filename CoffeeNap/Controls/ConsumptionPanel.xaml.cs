using System.Collections;
using System.Diagnostics;

namespace CoffeeNap.Controls;

/// <summary>
/// Выдвижная нижняя панель с историей употреблений. Пользователь тянет панель
/// за ручку, после чего она анимированно фиксируется в свёрнутом или раскрытом положении.
/// Данные передаются снаружи через bindable-свойство <see cref="ItemsSource"/>.
/// </summary>
public partial class ConsumptionPanel : ContentView
{
    // Панель фиксируется только в двух устойчивых положениях.
    private enum ConsumptionPanelState
    {
        Collapsed,
        Expanded
    }

    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource), typeof(IEnumerable), typeof(ConsumptionPanel));

    // Верхняя координата свёрнутой панели зависит от высоты карточек MainPage,
    // поэтому приходит извне и пересчитывается при изменении layout.
    public static readonly BindableProperty CollapsedTopInsetProperty = BindableProperty.Create(
        nameof(CollapsedTopInset),
        typeof(double),
        typeof(ConsumptionPanel),
        0d,
        propertyChanged: OnCollapsedTopInsetChanged);

    private const string SnapAnimationName = "ConsumptionPanelSnap";
    // Длительность задаётся в миллисекундах, частота кадров анимации ниже — 16 мс.
    private const uint SnapAnimationDuration = 200;
    // Расстояние и скорость, после которых жест однозначно считается свайпом.
    private const double SnapDistanceThreshold = 64;
    private const double SnapVelocityThreshold = 600;
    private const double DragActivationThreshold = 4;
    private const double ComfortableBottomSpacing = 20;

    // Допустимые координаты TranslationY. Меньшее значение находится выше на экране.
    private double _expandedTranslationY;
    private double _collapsedTranslationY;

    // Снимок текущего жеста: начальная позиция, пройденный путь и последняя скорость.
    private double _gestureStartTranslationY;
    private double _gestureDistanceY;
    private double _gestureVelocityY;
    private double _lastSampleTranslationY;
    private double _lastAllocatedHeight;
    private long _lastSampleTimestamp;

    // Счётчик поколений позволяет отличить актуальную анимацию от уже отменённой.
    private int _animationGeneration;

    // Флаги разделяют этапы измерения, жеста и анимации и защищают их от конфликтов.
    private bool _hasMeasured;
    private bool _isDragging;
    private bool _isDragActivated;
    private bool _isAnimating;
    private ConsumptionPanelState _panelState = ConsumptionPanelState.Collapsed;

    /// <summary>Создаёт контрол и загружает его визуальное дерево из XAML.</summary>
    public ConsumptionPanel()
    {
        InitializeComponent();
    }

    /// <summary>Коллекция записей, отображаемая внутренним CollectionView.</summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Координата верхнего края свёрнутой панели относительно области содержимого.
    /// На MainPage привязана к фактической высоте блока FixedTopContent.
    /// </summary>
    public double CollapsedTopInset
    {
        get => (double)GetValue(CollapsedTopInsetProperty);
        set => SetValue(CollapsedTopInsetProperty, value);
    }

    /// <summary>
    /// После измерения контрола вычисляет позиции фиксации для текущей высоты экрана.
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (height <= 0)
        {
            return;
        }

        // MAUI может несколько раз сообщить почти одинаковый размер.
        // Игнорируем такие вызовы, чтобы панель не дёргалась при раскладке.
        if (Math.Abs(height - _lastAllocatedHeight) < 0.5)
        {
            _hasMeasured = true;
            return;
        }

        _lastAllocatedHeight = height;
        RecalculatePositions(height);
        if (!_isDragging && !_isAnimating)
        {
            TranslationY = GetTranslationForState(_panelState);
            UpdateScrollableBottomInset();
        }

        _hasMeasured = true;
    }

    private static void OnCollapsedTopInsetChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Callback статический по требованиям BindableProperty; фактическую работу
        // выполняем над экземпляром панели, переданным во входном параметре.
        var panel = (ConsumptionPanel)bindable;
        if (panel.Height <= 0 || Math.Abs((double)newValue - (double)oldValue) < 0.5)
        {
            return;
        }

        panel.RecalculatePositions(panel.Height);
        if (!panel._isDragging && !panel._isAnimating)
        {
            panel.TranslationY = panel.GetTranslationForState(panel._panelState);
            panel.UpdateScrollableBottomInset();
        }
    }

    private void RecalculatePositions(double availableHeight)
    {
        // Координаты вычисляются относительно высоты экрана, но ограничиваются,
        // чтобы панель оставалась удобной и на маленьких, и на больших устройствах.
        _expandedTranslationY = Math.Clamp(availableHeight * 0.06, 20, 48);
        var minimumVisibleHeight = Math.Clamp(availableHeight * 0.22, 120, 220);
        var lowestAllowedTop = Math.Max(_expandedTranslationY, availableHeight - minimumVisibleHeight);
        _collapsedTranslationY = Math.Clamp(
            CollapsedTopInset,
            _expandedTranslationY,
            lowestAllowedTop);
    }

    private async void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        try
        {
            await ProcessPanelPanAsync(e);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Consumption panel gesture failed: {exception}");
        }
    }

    private async Task ProcessPanelPanAsync(PanUpdatedEventArgs e)
    {
        // До первого layout неизвестны безопасные границы перемещения.
        if (!_hasMeasured)
        {
            return;
        }

        // Один PanGestureRecognizer присылает последовательность Started → Running → Completed.
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                BeginPanelDrag();
                break;
            case GestureStatus.Running when _isDragging:
                UpdatePanelDrag(e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_isDragging)
                {
                    await CompletePanelDragAsync();
                }

                break;
        }
    }

    private void BeginPanelDrag()
    {
        // Новый жест становится источником истины и прерывает текущую snap-анимацию.
        _isDragging = true;
        CancelSnapAnimation();
        _gestureStartTranslationY = TranslationY;
        _gestureDistanceY = 0;
        _gestureVelocityY = 0;
        _isDragActivated = false;
        _lastSampleTranslationY = TranslationY;
        _lastSampleTimestamp = Stopwatch.GetTimestamp();
    }

    private void UpdatePanelDrag(double totalY)
    {
        // Небольшое начальное движение игнорируется: обычное касание ручки
        // не должно случайно сдвигать панель.
        if (!_isDragActivated)
        {
            if (Math.Abs(totalY) < DragActivationThreshold)
            {
                return;
            }

            _isDragActivated = true;
        }

        // Вычитаем уже пройденную «мёртвую зону», чтобы при активации не было скачка.
        var effectiveTotalY = totalY - Math.CopySign(DragActivationThreshold, totalY);
        var nextTranslation = Math.Clamp(
            _gestureStartTranslationY + effectiveTotalY,
            _expandedTranslationY,
            _collapsedTranslationY);

        // Скорость берём по двум последним замерам. Она нужна, чтобы быстрый
        // короткий свайп сработал даже без прохождения порога расстояния.
        var now = Stopwatch.GetTimestamp();
        var elapsedSeconds = (now - _lastSampleTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds > 0.008)
        {
            _gestureVelocityY = (nextTranslation - _lastSampleTranslationY) / elapsedSeconds;
            _lastSampleTranslationY = nextTranslation;
            _lastSampleTimestamp = now;
        }

        // Субпиксельные изменения не видны, но создают лишние обновления layout.
        if (Math.Abs(nextTranslation - TranslationY) >= 0.35)
        {
            TranslationY = nextTranslation;
        }

        _gestureDistanceY = TranslationY - _gestureStartTranslationY;
    }

    private async Task CompletePanelDragAsync()
    {
        _isDragging = false;

        var targetState = ResolveSnapState();
        await SnapToStateAsync(targetState);
    }

    private ConsumptionPanelState ResolveSnapState()
    {
        // Сначала учитываем явно направленный свайп, затем — ближайшее положение.
        if (_gestureVelocityY <= -SnapVelocityThreshold || _gestureDistanceY <= -SnapDistanceThreshold)
        {
            return ConsumptionPanelState.Expanded;
        }

        if (_gestureVelocityY >= SnapVelocityThreshold || _gestureDistanceY >= SnapDistanceThreshold)
        {
            return ConsumptionPanelState.Collapsed;
        }

        // Без выраженного свайпа выбираем ближайшую к текущей позиции границу.
        var midpoint = _expandedTranslationY + ((_collapsedTranslationY - _expandedTranslationY) / 2);
        return TranslationY <= midpoint
            ? ConsumptionPanelState.Expanded
            : ConsumptionPanelState.Collapsed;
    }

    private async Task SnapToStateAsync(ConsumptionPanelState targetState)
    {
        // В каждый момент должна работать не более чем одна анимация панели.
        CancelSnapAnimation();
        _panelState = targetState;

        var targetTranslation = GetTranslationForState(targetState);
        if (Math.Abs(TranslationY - targetTranslation) < 0.5)
        {
            TranslationY = targetTranslation;
            UpdateScrollableBottomInset();
            return;
        }

        _isAnimating = true;
        // Номер поколения не даёт завершившейся старой анимации перезаписать
        // состояние, если пользователь уже начал новый жест.
        var generation = _animationGeneration;
        // MAUI Animation сообщает о завершении callback-ом. TaskCompletionSource
        // преобразует его в Task, чтобы продолжение читалось как обычный async-код.
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = new Animation(
            value => TranslationY = value,
            TranslationY,
            targetTranslation,
            Easing.CubicOut);

        // Easing задаётся самой Animation, а Commit запускается с линейным ходом,
        // чтобы easing не применился повторно.
        animation.Commit(
            this,
            SnapAnimationName,
            16,
            SnapAnimationDuration,
            Easing.Linear,
            (_, wasCanceled) => completion.TrySetResult(!wasCanceled));

        await completion.Task;
        if (generation == _animationGeneration)
        {
            TranslationY = targetTranslation;
            _isAnimating = false;
            UpdateScrollableBottomInset();
        }
    }

    private void CancelSnapAnimation()
    {
        // Инкремент делает continuation предыдущей анимации устаревшим.
        _animationGeneration++;
        this.AbortAnimation(SnapAnimationName);
        _isAnimating = false;
    }

    private double GetTranslationForState(ConsumptionPanelState state)
    {
        // TranslationY отсчитывается вниз: раскрытая панель имеет меньшее значение.
        return state == ConsumptionPanelState.Expanded
            ? _expandedTranslationY
            : _collapsedTranslationY;
    }

    private void UpdateScrollableBottomInset()
    {
        // Дополнительное место внизу позволяет прокрутить последний элемент
        // выше нижней границы панели и не прижимать его к навигации.
        BottomScrollSpacer.HeightRequest = GetTranslationForState(_panelState) + ComfortableBottomSpacing;
    }
}
