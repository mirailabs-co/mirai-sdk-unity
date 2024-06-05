using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS 
using System.Runtime.InteropServices;
#endif

namespace Mirai
{
    public static class Utils
    {
        public static int FindAvailablePort(int startPort, int endPort)
        {
            return Enumerable.Range(startPort, endPort - startPort + 1).FirstOrDefault(IsPortAvailable);
            bool IsPortAvailable(int port)
            {
                try { new TcpListener(IPAddress.Loopback, port).Start(); return true; }
                catch (SocketException) { return false; }
            }
        }
        public static async Task<IDictionary<string,string>> WaitDeepLinkQueryCallback(string startWith="",CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<IDictionary<string,string>>();
            HttpListener listener = null;
            await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                Application.deepLinkActivated += HandleDeepLinkCallback;
                if (startWith.StartsWith("http://localhost:"))
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add($"http://*:{startWith.Split(":").Last()}/");
                    listener.Start();
                    listener.BeginGetContext(ListenerCallback, listener);
                }
                
                try
                {
                    await tcs.Task;
                }
                finally
                {
                    Application.deepLinkActivated -= HandleDeepLinkCallback;
                    listener?.Stop();
                }
            }
            return tcs.Task.Result;
            
            void ListenerCallback(IAsyncResult result)
            {
                if (!listener.IsListening) return;
                var context = listener.EndGetContext(result);
                var request = context.Request;
                var response = context.Response;
                response.StatusCode = (int) HttpStatusCode.OK;
                response.ContentType = "text/plain";
                const string text = "Go back to your app";
                var textBytes = Encoding.UTF8.GetBytes(text);
                response.OutputStream.Write(textBytes, 0, textBytes.Length);
                response.OutputStream.Close();
                HandleDeepLinkCallback(request.Url.ToString());
                listener.BeginGetContext(ListenerCallback, listener);
            }
            void HandleDeepLinkCallback(string deepLinkData)
            {
                Debug.Log(deepLinkData);
                if (!deepLinkData.StartsWith(startWith)) return;
                var linkParams = deepLinkData.TrimEnd('#','=','_').Split('?')?.Last()
                    ?.Split('&')
                    ?.Select(s => s.Split('='))
                    .Where(s => s.Length == 2)
                    .ToDictionary(s => s[0], s => s[1]) ?? new Dictionary<string, string>();
                tcs.TrySetResult(linkParams);
            }
        }
        
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern bool _IOSCanOpenURL(string url);
        public static bool CheckApp(string appID) => _IOSCanOpenURL(appID + "://");
#elif UNITY_ANDROID && !UNITY_EDITOR
        public static bool CheckApp(string appID)
        {
            var pluginClass = new AndroidJavaClass("android.content.pm.PackageManager");
            var jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
            var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");

            var flag = pluginClass.GetStatic<int>("GET_META_DATA");
            var packages = packageManager.Call<AndroidJavaObject>("getInstalledPackages", flag);
            var count = packages.Call<int>("size");
            for (var i = 0; i < count; i++)
            {
                var pkg = packages.Call<AndroidJavaObject>("get", i);
                var pkgName = pkg.Get<string>("packageName");
                //Debug.Log(pkgName);
                if (pkgName == appID)
                    return true;
            }
            return false;
        }
#else
        public static bool CheckApp(string appID) => false; 
#endif
    }
}