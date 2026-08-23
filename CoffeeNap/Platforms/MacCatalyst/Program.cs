using ObjCRuntime;
using UIKit;

namespace CoffeeNap
{
    /// <summary>Нативная точка входа версии для macOS через Mac Catalyst.</summary>
    public class Program
    {
        // UIKit запускает приложение и назначает AppDelegate обработчиком жизненного цикла.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
