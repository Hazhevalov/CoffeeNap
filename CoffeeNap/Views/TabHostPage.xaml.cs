using CoffeeNap.Services;
using CoffeeNap.ViewModels;
using Microsoft.Extensions.Logging;

namespace CoffeeNap.Views;

/// <summary>
/// Permanent mobile-first host for the three root sections. Keeping both views
/// in one visual tree avoids fragile Shell fragment transforms and keeps the
/// bottom navigation stationary while content slides.
/// </summary>
public partial class TabHostPage : ContentPage
{
    private const uint TransitionDuration = 230;
    private const double OutgoingOpacity = 0.82;
    private static readonly TimeSpan FirstLayoutTimeout = TimeSpan.FromMilliseconds(180);

    private readonly AppPageFactory _pageFactory;
    private readonly ILogger<TabHostPage> _logger;
    private readonly Dictionary<NavigationTab, ITabContent> _contents = [];
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    private ITabContent _activeContent = null!;
    private bool _isHostVisible;
    private readonly ApplicationVisibility _visibility;
    private bool _observingVisibility;
    private ITabContent? _activatedContent;

    // Initializes the tab host page.
    public TabHostPage(
        AppPageFactory pageFactory,
        BottomNavigationViewModel navigation,
        ILogger<TabHostPage> logger,
        ApplicationVisibility visibility)
    {
        _pageFactory = pageFactory;
        _logger = logger;
        _visibility = visibility;
        BottomNavigation = navigation;
        BottomNavigation.ActiveTab = NavigationTab.Home;

        InitializeComponent();
        BindingContext = this;

        _activeContent = GetOrCreateContent(NavigationTab.Home);
        ShowImmediately(_activeContent);
        Loaded += OnHostLoaded;
        Unloaded += OnHostUnloaded;
    }

    public BottomNavigationViewModel BottomNavigation { get; }

    internal NavigationTab ActiveTab => _activeContent.Tab;

    // Activates the current tab when the host appears.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isHostVisible = true;
        BottomNavigation.SetActiveTab(_activeContent.Tab, animate: false);
        ActivateIfVisible();
    }

    // Deactivates the current tab when the host disappears.
    protected override void OnDisappearing()
    {
        _isHostVisible = false;
        _activeContent.Deactivate();
        _activatedContent = null;
        base.OnDisappearing();
    }

    // Activates tab content after the host loads.
    private void OnHostLoaded(object? sender, EventArgs args)
    {
        if (_observingVisibility) return;
        _observingVisibility = true;
        _visibility.Changed += OnVisibilityChanged;
        ActivateIfVisible();
    }

    // Deactivates tab content when the host unloads.
    private void OnHostUnloaded(object? sender, EventArgs args)
    {
        _activeContent.Deactivate();
        _activatedContent = null;
        _visibility.Changed -= OnVisibilityChanged;
        _observingVisibility = false;
    }

    // Updates tab activity when application visibility changes.
    private void OnVisibilityChanged(object? sender, EventArgs args)
    {
        if (!_visibility.IsActive)
        {
            _activeContent.Deactivate();
            _activatedContent = null;
        }
        else ActivateIfVisible();
    }

    // Activates the current content only when the host is visible.
    private void ActivateIfVisible()
    {
        if (!_observingVisibility || !_isHostVisible || !_visibility.IsActive ||
            ReferenceEquals(_activatedContent, _activeContent)) return;
        _activatedContent = _activeContent;
        _ = ActivateSafelyAsync(_activeContent);
    }

    // Releases all cached tab content.
    internal void Release()
    {
        _isHostVisible = false;
        Loaded -= OnHostLoaded;
        Unloaded -= OnHostUnloaded;
        OnHostUnloaded(this, EventArgs.Empty);
        foreach (var content in _contents.Values) content.Release();
        _contents.Clear();
    }

    // Switches tabs and coordinates their lifecycle and animations.
    internal async Task NavigateToAsync(
        NavigationTab targetTab,
        BottomNavigationViewModel? sourceNavigation = null)
    {
        if (targetTab is not (
            NavigationTab.Home or
            NavigationTab.AddConsumption or
            NavigationTab.Calendar))
        {
            throw new ArgumentOutOfRangeException(nameof(targetTab));
        }

        await _transitionLock.WaitAsync();
        var previousHostInput = HostRoot.InputTransparent;
        HostRoot.InputTransparent = true;

        ITabContent? source = null;
        ITabContent? destination = null;
        var destinationCommitted = false;

        try
        {
            if (_activeContent.Tab == targetTab)
            {
                CommitSelection(
                    targetTab,
                    sourceNavigation,
                    animateHostSelection: false);
                return;
            }

            source = _activeContent;
            destination = GetOrCreateContent(targetTab);
            var sourceView = (View)source;
            var destinationView = (View)destination;
            var direction = Math.Sign(GetIndex(targetTab) - GetIndex(source.Tab));
            destinationView.IsVisible = true;
            destinationView.InputTransparent = true;
            destinationView.Opacity = 0;
            AutomationProperties.SetExcludedWithChildren(destinationView, true);

            var destinationIsReady = !_isHostVisible ||
                                     await WaitForFirstLayoutAsync(destinationView);
            var shouldAnimate = _isHostVisible && _visibility.IsActive &&
                                destinationIsReady &&
                                NavigationAnimation.IsEnabled &&
                                ContentLayer.Width > 0;

            source.Deactivate();
            _activatedContent = null;
            _activeContent = destination;
            destinationCommitted = true;
            CommitSelection(targetTab, sourceNavigation, shouldAnimate);

            if (_isHostVisible && _visibility.IsActive)
            {
                ActivateIfVisible();
            }

            if (!shouldAnimate)
            {
                return;
            }

            var width = ContentLayer.Width;
            sourceView.CancelAnimations();
            destinationView.CancelAnimations();
            sourceView.TranslationX = 0;
            sourceView.Opacity = 1;
            destinationView.TranslationX = direction * width;
            destinationView.Opacity = 1;
            AutomationProperties.SetExcludedWithChildren(sourceView, true);
            AutomationProperties.SetExcludedWithChildren(destinationView, false);

            try
            {
                await Task.WhenAll(
                    sourceView.TranslateToAsync(
                        -direction * width,
                        0,
                        TransitionDuration,
                        Easing.CubicInOut),
                    sourceView.FadeToAsync(
                        OutgoingOpacity,
                        TransitionDuration,
                        Easing.CubicInOut),
                    destinationView.TranslateToAsync(
                        0,
                        0,
                        TransitionDuration,
                        Easing.CubicInOut));
            }
            catch (Exception exception)
            {
                // Motion is decorative and the destination is already the
                // committed state. Log lifecycle cancellation and snap cleanly.
                _logger.LogWarning(
                    exception,
                    "Tab transition from {SourceTab} to {TargetTab} was interrupted.",
                    source.Tab,
                    targetTab);
            }
        }
        finally
        {
            if (source is not null && destination is not null)
            {
                if (destinationCommitted)
                {
                    Hide(source);
                    ShowImmediately(destination);
                }
                else
                {
                    Hide(destination);
                    ShowImmediately(source);
                }
            }

            HostRoot.InputTransparent = previousHostInput;
            _transitionLock.Release();
        }
    }

    // Returns cached tab content or creates it on demand.
    private ITabContent GetOrCreateContent(NavigationTab tab)
    {
        if (_contents.TryGetValue(tab, out var existing))
        {
            return existing;
        }

        ITabContent content = tab switch
        {
            NavigationTab.Home => _pageFactory.CreateMainPage(),
            NavigationTab.AddConsumption => _pageFactory.CreateAddConsumptionPage(),
            NavigationTab.Calendar => _pageFactory.CreateCalendarPage(),
            _ => throw new ArgumentOutOfRangeException(nameof(tab))
        };

        var view = (View)content;
        view.IsVisible = false;
        view.InputTransparent = true;
        AutomationProperties.SetExcludedWithChildren(view, true);
        ContentLayer.Children.Add(view);
        _contents.Add(tab, content);
        return content;
    }

    // Activates tab content and reports activation failures.
    private async Task ActivateSafelyAsync(ITabContent content)
    {
        try
        {
            await content.ActivateAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Tab {Tab} activation failed.", content.Tab);
        }
    }

    // Commits the active tab and updates navigation state.
    private void CommitSelection(
        NavigationTab targetTab,
        BottomNavigationViewModel? sourceNavigation,
        bool animateHostSelection)
    {
        BottomNavigation.SetActiveTab(targetTab, animateHostSelection);
        if (sourceNavigation is not null &&
            !ReferenceEquals(sourceNavigation, BottomNavigation))
        {
            sourceNavigation.SetActiveTab(
                targetTab,
                animate: NavigationAnimation.IsEnabled);
        }
    }

    // Waits until the view has a handler and nonzero layout bounds.
    private static async Task<bool> WaitForFirstLayoutAsync(View view)
    {
        if (view.Handler is not null && view.Width > 0 && view.Height > 0)
        {
            return true;
        }

        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        // Completes the layout wait once the view is ready.
        void CompleteWhenReady(object? sender, EventArgs eventArgs)
        {
            if (view.Handler is not null && view.Width > 0 && view.Height > 0)
            {
                completion.TrySetResult(true);
            }
        }

        view.Loaded += CompleteWhenReady;
        view.SizeChanged += CompleteWhenReady;
        view.InvalidateMeasure();

        try
        {
            return await completion.Task.WaitAsync(FirstLayoutTimeout);
        }
        catch (TimeoutException)
        {
            return view.Handler is not null && view.Width > 0 && view.Height > 0;
        }
        finally
        {
            view.Loaded -= CompleteWhenReady;
            view.SizeChanged -= CompleteWhenReady;
        }
    }

    // Maps a navigation tab to its visual index.
    private static int GetIndex(NavigationTab tab) => tab switch
    {
        NavigationTab.AddConsumption => 0,
        NavigationTab.Home => 1,
        NavigationTab.Calendar => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(tab))
    };

    // Shows tab content with its final visual state.
    private static void ShowImmediately(ITabContent content)
    {
        var view = (View)content;
        view.CancelAnimations();
        view.TranslationX = 0;
        view.Opacity = 1;
        view.IsVisible = true;
        view.InputTransparent = false;
        AutomationProperties.SetExcludedWithChildren(view, false);
    }

    // Hides tab content and resets its visual state.
    private static void Hide(ITabContent content)
    {
        var view = (View)content;
        view.CancelAnimations();
        view.TranslationX = 0;
        view.Opacity = 1;
        view.IsVisible = false;
        view.InputTransparent = true;
        AutomationProperties.SetExcludedWithChildren(view, true);
    }
}
