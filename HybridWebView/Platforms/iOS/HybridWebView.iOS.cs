using Foundation;
using UIKit;
using WebKit;

namespace HybridWebView
{
    partial class HybridWebView
    {
        internal const string AppHostAddress = "0.0.0.0";

        internal const string AppOrigin = "app://" + AppHostAddress + "/";
        internal static readonly Uri AppOriginUri = new(AppOrigin);

        partial void InitializeHybridWebView()
        {
            var wv = (WKWebView)Handler.PlatformView;

            UISwipeGestureRecognizer leftgestureRecognizer = new UISwipeGestureRecognizer(SwipeEvent);

            leftgestureRecognizer.Direction = UISwipeGestureRecognizerDirection.Left;

            UISwipeGestureRecognizer rightgestureRecognizer = new UISwipeGestureRecognizer(SwipeEvent);
            rightgestureRecognizer.Direction = UISwipeGestureRecognizerDirection.Right;

            leftgestureRecognizer.Delegate = new MyWebViewDelegate();
            rightgestureRecognizer.Delegate = new MyWebViewDelegate();

            wv.AddGestureRecognizer(leftgestureRecognizer);
            wv.AddGestureRecognizer(rightgestureRecognizer);

            if (this.UseResources)
            {
              
                using var nsUrl = new NSUrl(AppOrigin);
                using var request = new NSUrlRequest(nsUrl);
                wv.LoadRequest(request);
            }
        }

        [Export("SwipeEvent:")]
        void SwipeEvent(UISwipeGestureRecognizer recognizer)
        {
            if (recognizer.Direction == UISwipeGestureRecognizerDirection.Left)
            {
                this.OnSwipeLeft();
            }
            else if (recognizer.Direction == UISwipeGestureRecognizerDirection.Right)
            {
                this.OnSwipeRight();
            }
        }

        private class MyWebViewDelegate : UIGestureRecognizerDelegate
        {
            public override bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
            {
                return false;
            }
        }
    }
}
