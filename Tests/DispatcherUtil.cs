using System.Windows.Threading;

namespace Tests
{
    /// This class helps UnitTest Dispatcher.
    /// Dispatcher does not automatically process its queue, 
    /// DoEvents method here will tell the Dispatcher to process its queue
    public static class DispatcherUtil
    {
        public static void DoEvents()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                                                     new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object frame)
        {
            ((DispatcherFrame)frame).Continue = false;
            return null;
        }
    }
}
