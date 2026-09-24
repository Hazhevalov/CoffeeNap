using ObjCRuntime;
using UIKit;

namespace CoffeeNap
{
    /// <summary>Native entry point for the iOS application.</summary>
    public class Program
    {
        // UIKit creates the application loop and forwards lifecycle events to AppDelegate.
        static void Main(string[] args)
        {
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}
