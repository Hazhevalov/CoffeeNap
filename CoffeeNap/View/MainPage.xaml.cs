using CoffeeNap.ViewModels;

namespace CoffeeNap
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel viewModel;

        public MainPage()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            viewModel.StartPeriodicUpdates();
        }

        protected override void OnDisappearing()
        {
            viewModel.StopPeriodicUpdates();
            base.OnDisappearing();
        }
    }
}
