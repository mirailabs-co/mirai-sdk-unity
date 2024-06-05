using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Mirai
{
    public static class RestAPI
    {
        public enum Method
        {
            GET,
            POST,
            PUT,
            DELETE,
        }
        
        public class Connection
        {
            private readonly string _baseUrl;
            public readonly Dictionary<string, string> Headers;

            public Connection(string baseUrl, Dictionary<string, string> headers=null)
            {
                _baseUrl = baseUrl;
                Headers = headers ?? new Dictionary<string, string>();
            }
            public Task<T> Request<T>(string route,
                string selectToken = "$",
                object data = null,
                Method? method = null,
                CancellationToken cancellationToken = default)
                => RestAPI.Request<T>($"{_baseUrl}{route}", selectToken, data, Headers, method, cancellationToken);
            public Task<string> Request(string route,
                object data = null,
                Method? method = null,
                CancellationToken cancellationToken = default)
                => RestAPI.Request($"{_baseUrl}{route}", data, Headers, method, cancellationToken);
        }
        
        public static async Task<T> Request<T>(string url, 
            string selectToken = "$",
            object data = null,
            Dictionary<string, string> headers = null, 
            Method? method = null,
            CancellationToken cancellationToken = default)
        {
            var jToken = JToken.Parse(await Request(url, data, headers, method, cancellationToken)).SelectToken(selectToken);
            return jToken != null ? jToken.ToObject<T>() : default;
        }
        public static async Task<string> Request(string url, 
            object data = null,
            Dictionary<string, string> headers = null,
            Method? method = null,
            CancellationToken cancellationToken = default)
        {
            var jsonText = data!=null?JsonConvert.SerializeObject(data):"";
            var uploadHandler = data != null ? new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonText)) : null;
            using var req = new UnityWebRequest(url, (method ?? (data == null ? Method.GET : Method.POST)).ToString(), new DownloadHandlerBuffer(), uploadHandler);
            req.SetRequestHeader("Content-Type", "application/json");
            if (headers != null)
            {
                foreach (var header in headers)
                    req.SetRequestHeader(header.Key, header.Value);
            }
            Debug.LogWarning($"<color=green>[{req.method}]{req.url}</color>\n{(headers != null ? string.Join('\n', headers.Select(h => h.Key + ": " + h.Value))+"\n" : "")}<color=orange>{jsonText}</color>");
            req.SendWebRequest();
            while (!req.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    req.Abort();
                    throw new Exception("Web request canceled.");
                }
                await Task.Yield();
            }
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"<color=yellow>[{req.method}]{req.url}</color>\n<color=orange>{req.downloadHandler.text}</color>");
                return req.downloadHandler.text;
            }
            var errMessage = req.error + req.downloadHandler.text;
            var exception=new Exception($"<color=red>{errMessage}</color>");
            throw exception;
        }
    }
}
