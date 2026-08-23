using Microsoft.Extensions.DependencyInjection;

namespace CoffeeNap
{
    /// <summary>
    /// Корневой объект MAUI-приложения. Глобальные ресурсы загружаются из App.xaml,
    /// а здесь создаётся первое окно с навигационной оболочкой.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>Инициализирует общие XAML-ресурсы приложения.</summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Создаёт главное окно. AppShell становится корневым визуальным элементом
        /// и далее управляет переходами между страницами.
        /// </summary>
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
