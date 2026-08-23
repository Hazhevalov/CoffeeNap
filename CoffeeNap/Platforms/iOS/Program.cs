using ObjCRuntime;
using UIKit;

namespace CoffeeNap
{
    /// <summary>Нативная точка входа iOS-приложения.</summary>
    public class Program
    {
        // UIKit создаёт цикл приложения и передаёт события жизненного цикла AppDelegate.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
