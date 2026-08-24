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

        /// <summary>Создаёт ViewModel и делает её источником всех XAML-привязок страницы.</summary>
        public MainPage()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            BindingContext = viewModel;
        }

        /// <summary>Запускает обновление динамических подписей, когда страница видима.</summary>
        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.RefreshUserName();
            viewModel.StartPeriodicUpdates();
        }

        /// <summary>Останавливает обновления, когда пользователь уходит со страницы.</summary>
        protected override void OnDisappearing()
        {
            viewModel.StopPeriodicUpdates();
            base.OnDisappearing();
        }
    }
}
