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
    private double expandedTranslationY;
    private double collapsedTranslationY;

    // Снимок текущего жеста: начальная позиция, пройденный путь и последняя скорость.
    private double gestureStartTranslationY;
    private double gestureDistanceY;
    private double gestureVelocityY;
    private double lastSampleTranslationY;
    private double lastAllocatedHeight;
    private long lastSampleTimestamp;

    // Счётчик поколений позволяет отличить актуальную анимацию от уже отменённой.
    private int animationGeneration;

    // Флаги разделяют этапы измерения, жеста и анимации и защищают их от конфликтов.
    private bool hasMeasured;
    private bool isDragging;
    private bool isDragActivated;
    private bool isAnimating;
    private ConsumptionPanelState panelState = ConsumptionPanelState.Collapsed;

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
        if (Math.Abs(height - lastAllocatedHeight) < 0.5)
        {
            hasMeasured = true;
            return;
        }

        lastAllocatedHeight = height;
        RecalculatePositions(height);
        if (!isDragging && !isAnimating)
        {
            TranslationY = GetTranslationForState(panelState);
            UpdateScrollableBottomInset();
        }

        hasMeasured = true;
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
        if (!panel.isDragging && !panel.isAnimating)
        {
            panel.TranslationY = panel.GetTranslationForState(panel.panelState);
            panel.UpdateScrollableBottomInset();
        }
    }

    private void RecalculatePositions(double availableHeight)
    {
        // Координаты вычисляются относительно высоты экрана, но ограничиваются,
        // чтобы панель оставалась удобной и на маленьких, и на больших устройствах.
        expandedTranslationY = Math.Clamp(availableHeight * 0.06, 20, 48);
        var minimumVisibleHeight = Math.Clamp(availableHeight * 0.22, 120, 220);
        var lowestAllowedTop = Math.Max(expandedTranslationY, availableHeight - minimumVisibleHeight);
        collapsedTranslationY = Math.Clamp(
            CollapsedTopInset,
            expandedTranslationY,
            lowestAllowedTop);
    }

    private void OnDragHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        // Event handler оставлен тонким, чтобы автомат состояний жеста был отдельно.
        ProcessPanelPan(e);
    }

    private void ProcessPanelPan(PanUpdatedEventArgs e)
    {
        // До первого layout неизвестны безопасные границы перемещения.
        if (!hasMeasured)
        {
            return;
        }

        // Один PanGestureRecognizer присылает последовательность Started → Running → Completed.
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                BeginPanelDrag();
                break;
            case GestureStatus.Running when isDragging:
                UpdatePanelDrag(e.TotalY);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (isDragging)
                {
                    CompletePanelDrag();
                }

                break;
        }
    }

    private void BeginPanelDrag()
    {
        // Новый жест становится источником истины и прерывает текущую snap-анимацию.
        isDragging = true;
        CancelSnapAnimation();
        gestureStartTranslationY = TranslationY;
        gestureDistanceY = 0;
        gestureVelocityY = 0;
        isDragActivated = false;
        lastSampleTranslationY = TranslationY;
        lastSampleTimestamp = Stopwatch.GetTimestamp();
    }

    private void UpdatePanelDrag(double totalY)
    {
        // Небольшое начальное движение игнорируется: обычное касание ручки
        // не должно случайно сдвигать панель.
        if (!isDragActivated)
        {
            if (Math.Abs(totalY) < DragActivationThreshold)
            {
                return;
            }

            isDragActivated = true;
        }

        // Вычитаем уже пройденную «мёртвую зону», чтобы при активации не было скачка.
        var effectiveTotalY = totalY - Math.CopySign(DragActivationThreshold, totalY);
        var nextTranslation = Math.Clamp(
            gestureStartTranslationY + effectiveTotalY,
            expandedTranslationY,
            collapsedTranslationY);

        // Скорость берём по двум последним замерам. Она нужна, чтобы быстрый
        // короткий свайп сработал даже без прохождения порога расстояния.
        var now = Stopwatch.GetTimestamp();
        var elapsedSeconds = (now - lastSampleTimestamp) / (double)Stopwatch.Frequency;
        if (elapsedSeconds > 0.008)
        {
            gestureVelocityY = (nextTranslation - lastSampleTranslationY) / elapsedSeconds;
            lastSampleTranslationY = nextTranslation;
            lastSampleTimestamp = now;
        }

        // Субпиксельные изменения не видны, но создают лишние обновления layout.
        if (Math.Abs(nextTranslation - TranslationY) >= 0.35)
        {
            TranslationY = nextTranslation;
        }

        gestureDistanceY = TranslationY - gestureStartTranslationY;
    }

    private async void CompletePanelDrag()
    {
        // Обработчик события имеет void-сигнатуру; асинхронно ждём завершения
        // анимации перед окончательной фиксацией позиции.
        isDragging = false;

        var targetState = ResolveSnapState();
        await SnapToStateAsync(targetState);
    }

    private ConsumptionPanelState ResolveSnapState()
    {
        // Сначала учитываем явно направленный свайп, затем — ближайшее положение.
        if (gestureVelocityY <= -SnapVelocityThreshold || gestureDistanceY <= -SnapDistanceThreshold)
        {
            return ConsumptionPanelState.Expanded;
        }

        if (gestureVelocityY >= SnapVelocityThreshold || gestureDistanceY >= SnapDistanceThreshold)
        {
            return ConsumptionPanelState.Collapsed;
        }

        // Без выраженного свайпа выбираем ближайшую к текущей позиции границу.
        var midpoint = expandedTranslationY + ((collapsedTranslationY - expandedTranslationY) / 2);
        return TranslationY <= midpoint
            ? ConsumptionPanelState.Expanded
            : ConsumptionPanelState.Collapsed;
    }

    private async Task SnapToStateAsync(ConsumptionPanelState targetState)
    {
        // В каждый момент должна работать не более чем одна анимация панели.
        CancelSnapAnimation();
        panelState = targetState;

        var targetTranslation = GetTranslationForState(targetState);
        if (Math.Abs(TranslationY - targetTranslation) < 0.5)
        {
            TranslationY = targetTranslation;
            UpdateScrollableBottomInset();
            return;
        }

        isAnimating = true;
        // Номер поколения не даёт завершившейся старой анимации перезаписать
        // состояние, если пользователь уже начал новый жест.
        var generation = animationGeneration;
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
        if (generation == animationGeneration)
        {
            TranslationY = targetTranslation;
            isAnimating = false;
            UpdateScrollableBottomInset();
        }
    }

    private void CancelSnapAnimation()
    {
        // Инкремент делает continuation предыдущей анимации устаревшим.
        animationGeneration++;
        this.AbortAnimation(SnapAnimationName);
        isAnimating = false;
    }

    private double GetTranslationForState(ConsumptionPanelState state)
    {
        // TranslationY отсчитывается вниз: раскрытая панель имеет меньшее значение.
        return state == ConsumptionPanelState.Expanded
            ? expandedTranslationY
            : collapsedTranslationY;
    }

    private void UpdateScrollableBottomInset()
    {
        // Дополнительное место внизу позволяет прокрутить последний элемент
        // выше нижней границы панели и не прижимать его к навигации.
        BottomScrollSpacer.HeightRequest = GetTranslationForState(panelState) + ComfortableBottomSpacing;
    }
}
