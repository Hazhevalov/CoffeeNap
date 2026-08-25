using CoffeeNap.ViewModels;

namespace CoffeeNap
{
    /// <summary>
    /// Главный экран приложения. Разметка находится в MainPage.xaml, а code-behind
    /// отвечает только за жизненный цикл MainViewModel.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        // Сохраняем типизированную ссылку отдельно от BindingContext, чтобы управлять таймером.
        private readonly MainViewModel viewModel;
        private bool isPageVisible;

        /// <summary>Создаёт ViewModel и делает её источником всех XAML-привязок страницы.</summary>
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            BindingContext = viewModel;
        }

        /// <summary>Запускает обновление динамических подписей, когда страница видима.</summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            isPageVisible = true;
            await viewModel.InitializeAsync();
            if (isPageVisible)
            {
                viewModel.StartPeriodicUpdates();
            }
        }

        /// <summary>Останавливает обновления, когда пользователь уходит со страницы.</summary>
        protected override void OnDisappearing()
        {
            isPageVisible = false;
            viewModel.StopPeriodicUpdates();
            base.OnDisappearing();
        }
    }
}
