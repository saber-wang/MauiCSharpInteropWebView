using Android.Views;
using Android.Webkit;
using Java.Interop;
using AWebView = Android.Webkit.WebView;

namespace HybridWebView
{
    partial class HybridWebView
    {
        // Using an IP address means that WebView2 doesn't wait for any DNS resolution,
        // making it substantially faster. Note that this isn't real HTTP traffic, since
        // we intercept all the requests within this origin.
        internal static readonly string AppHostAddress = "0.0.0.0";

        /// <summary>
        /// Gets the application's base URI. Defaults to <c>https://0.0.0.0/</c>
        /// </summary>
        internal static readonly string AppOrigin = $"https://{AppHostAddress}/";

        internal static readonly Uri AppOriginUri = new(AppOrigin);

        private HybridWebViewJavaScriptInterface _javaScriptInterface;

        partial void InitializeHybridWebView()
        {
            var awv = (AWebView)Handler.PlatformView;
            awv.Settings.JavaScriptEnabled = true;
            AWebView.SetWebContentsDebuggingEnabled(true);
            awv.SetOnTouchListener(new MyOnTouchListener(this));

            _javaScriptInterface = new HybridWebViewJavaScriptInterface(this);
            awv.AddJavascriptInterface(_javaScriptInterface, "hybridWebViewHost");
            if (this.UseResources)
                awv.LoadUrl(AppOrigin);
        }

        private sealed class HybridWebViewJavaScriptInterface : Java.Lang.Object
        {
            private readonly HybridWebView _hybridWebView;

            public HybridWebViewJavaScriptInterface(HybridWebView hybridWebView)
            {
                _hybridWebView = hybridWebView;
            }

            [JavascriptInterface]
            [Export("sendMessage")]
            public void SendMessage(string message)
            {
                _hybridWebView.OnMessageReceived(message);
            }
        }

        private sealed class MyOnTouchListener : Java.Lang.Object, Android.Views.View.IOnTouchListener
        {
            float posX;

            float curposX;

            HybridWebView myWebView;
            public MyOnTouchListener(HybridWebView webView)
            {
                myWebView = webView;
            }
            public bool OnTouch(Android.Views.View v, MotionEvent e)
            {
                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        posX = e.GetX(0);
                        break;
                    case MotionEventActions.Move:
                        curposX = e.GetX(0);
                        break;
                    case MotionEventActions.Up:
                        if (curposX - posX > 0 && Math.Abs(curposX - posX) > 150)
                        { 
                            myWebView.OnSwipeRight();
                        }
                        else if (curposX - posX < 0 && Math.Abs(curposX - posX) > 150)
                        {
                            myWebView.OnSwipeLeft();
                        }
                        //重置为当前位置
                        curposX= e.GetX(0);
                        break;
                }

                return false;
            }
        }
    }
}
