using System.Text.Encodings.Web;
using System.Text.Json;

namespace HybridWebView
{
    public partial class HybridWebView : WebView
    {
        //todo 绑定
        /// <summary>
        /// 是否使用本地资源
        /// </summary>
        public bool UseResources { get; set; } = false;

        public string MainFile { get; set; }

        /// <summary>
        ///  The path within the app's "Raw" asset resources that contain the web app's contents. For example, if the
        ///  files are located in "ProjectFolder/Resources/Raw/hybrid_root", then set this property to "hybrid_root".
        /// </summary>
        public string HybridAssetRoot { get; set; }

        /// <summary>
        /// Hosts objects that are accessible (methods only) to Javascript.
        /// </summary>
        public HybridWebViewObjectHost ObjectHost { get; private set; }

        public event EventHandler<HybridWebViewRawMessageReceivedEventArgs> RawMessageReceived;

        public event EventHandler SwipeLeft;
        public event EventHandler SwipeRight;

        private readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { Encoder= JavaScriptEncoder .UnsafeRelaxedJsonEscaping};

        /// <summary>
        /// 为前端发送事件
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Task<string> OnMessageCallback(string type, object message)
        {
           return MainThread.InvokeOnMainThreadAsync(() => {
                return InvokeJsMethodAsync("__MediJSBridge__.OnMessageCallback", new EventMessage
                {
                    Type = type,
                    Data = message
                });
            });
        }

        public void OnSwipeLeft() =>
            SwipeLeft?.Invoke(this, null);

        public void OnSwipeRight() =>
            SwipeRight?.Invoke(this, null);


        public HybridWebView()
        {
            ObjectHost = new HybridWebViewObjectHost(this, _options);
        }

        protected override void OnHandlerChanged()
        {
            base.OnHandlerChanged();

            InitializeHybridWebView();
        }

        partial void InitializeHybridWebView();

        /// <summary>
        /// Invokes a JavaScript method named <paramref name="methodName"/> and optionally passes in the parameter values specified
        /// by <paramref name="paramValues"/> by JSON-encoding each one.
        /// </summary>
        /// <param name="methodName">The name of the JavaScript method to invoke.</param>
        /// <param name="paramValues">Optional array of objects to be passed to the JavaScript method by JSON-encoding each one.</param>
        /// <returns>A string containing the return value of the called method.</returns>
        public async Task<string> InvokeJsMethodAsync(string methodName, params object[] paramValues)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                throw new ArgumentException($"The method name cannot be null or empty.", nameof(methodName));
            }

            return await EvaluateJavaScriptAsync($"{methodName}({(paramValues == null ? string.Empty : string.Join(", ", paramValues.Select(v => JsonSerializer.Serialize(v, _options))))})");
        }

        /// <summary>
        /// Invokes a JavaScript method named <paramref name="methodName"/> and optionally passes in the parameter values specified
        /// by <paramref name="paramValues"/> by JSON-encoding each one.
        /// </summary>
        /// <typeparam name="TReturnType">The type of the return value to deserialize from JSON.</typeparam>
        /// <param name="methodName">The name of the JavaScript method to invoke.</param>
        /// <param name="paramValues">Optional array of objects to be passed to the JavaScript method by JSON-encoding each one.</param>
        /// <returns>An object of type <typeparamref name="TReturnType"/> containing the return value of the called method.</returns>
        public async Task<TReturnType> InvokeJsMethodAsync<TReturnType>(string methodName, params object[] paramValues)
        {
            var stringResult = await InvokeJsMethodAsync(methodName, paramValues);

            return JsonSerializer.Deserialize<TReturnType>(stringResult, _options);
        }

        public void LogInfo(string message)
        {
            if (this.Dispatcher.IsDispatchRequired)
            {
                this.Dispatcher.Dispatch(() => { LogInfo(message); });
                return;
            }

            this.EvaluateJavaScriptAsync($"console.log('{message}')").ContinueWith(t =>
            {
                if (t.Status == TaskStatus.Faulted)
                {
                    //TODO: Report error, add a new event maybe?
                    var ex = t.Exception;
                }
            });
        }

        public void LogError(string message)
        {
            if (this.Dispatcher.IsDispatchRequired)
            {
                this.Dispatcher.Dispatch(() => { LogError(message); });
                return;
            }

            this.EvaluateJavaScriptAsync($"console.error('{message}')").ContinueWith(t =>
            {
                if (t.Status == TaskStatus.Faulted)
                {
                    //TODO: Report error, add a new event maybe?
                    var ex = t.Exception;
                }
            });
        }

        // TODO: Better name of this method
        internal void RaiseMessageReceived(string message)
        {
            OnMessageReceived(message);
        }

        protected virtual void OnMessageReceived(string message)
        {
            var messageData = JsonSerializer.Deserialize<WebMessageData>(message, _options);
            switch (messageData.MessageType)
            {
                case 0: // "raw" message (just a string)
                    RawMessageReceived?.Invoke(this, new HybridWebViewRawMessageReceivedEventArgs(messageData.MessageContent));
                    break;
                case 1: // "invoke" message
                    var invokeData = JsonSerializer.Deserialize<HybridWebViewObjectHost.JSInvokeMethodData>(messageData.MessageContent, _options);
                    ObjectHost.InvokeDotNetMethod(invokeData);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown message type: {messageData.MessageType}. Message contents: {messageData.MessageContent}");
            }
        }

        private sealed class WebMessageData
        {
            public int MessageType { get; set; }
            public string MessageContent { get; set; }
        }

        internal static async Task<string> GetAssetContentAsync(string assetPath)
        {
            using var stream = await GetAssetStreamAsync(assetPath);
            if (stream == null)
            {
                return null;
            }
            using var reader = new StreamReader(stream);

            var contents = reader.ReadToEnd();

            return contents;
        }

        internal static async Task<Stream> GetAssetStreamAsync(string assetPath)
        {
            if (!await FileSystem.AppPackageFileExistsAsync(assetPath))
            {
                return null;
            }
            return await FileSystem.OpenAppPackageFileAsync(assetPath);
        }
        class EventMessage
        {
            public string Type { get; set; }

            public object Data { get; set; }
        }
    }

}
