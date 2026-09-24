using ObjCRuntime;
using UIKit;

namespace CoffeeNap
{
    /// <summary>Native entry point for macOS through Mac Catalyst.</summary>
    public class Program
    {
        // UIKit starts the application and assigns AppDelegate to handle lifecycle events.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
