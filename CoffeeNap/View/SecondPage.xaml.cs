using CoffeeNap.ViewModels;

namespace CoffeeNap
{
    /// <summary>
    /// Альтернативный макет главного экрана. Страница присутствует в проекте,
    /// но не зарегистрирована в AppShell и из текущей навигации не открывается.
    /// </summary>
    public partial class SecondPage : ContentPage
    {
        /// <summary>Загружает альтернативную XAML-разметку.</summary>
        public SecondPage()
        {
            InitializeComponent();
        }
    }
}
