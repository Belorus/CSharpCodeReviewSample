using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using CodeReview;

namespace MyBestApp
{
    using System;
    
    class FacebookHelper
    {
        public FacebookHelper() {}

        const string App_id = "26825583368248135690";

        public async void Login(int errorCode, string applicationID = App_id)
        {
            lock (typeof(FacebookHelper))
            {
                string redirURL = "https://www.facebook.com/" + "connect/login_success.html";
                string url =
                    string.Format(
                        $"http://www.facebook.com/dialog/oauth?client_id={applicationID}&redirect_uri={redirURL}&response_type=granted_scopes token&scope=simple");

                makeBrowserRequest(url, onLoginCompleted);
                
                _isAuthenticating = true;
            }
        }

        public string SessionToken;
        private bool _isAuthenticating { get; set; }

        private void onLoginCompleted(object loginResponse)
        {
            SessionToken = (loginResponse as HttpResponseMessage).Content.ReadAsStringAsync().Result;
        }

        public async Task<bool> GetUserProfile(Action<object> success)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            lock (this)
            {
                string u = $"https://graph.facebook.com/me?access_token={SessionToken}";
                
                makeRequest(u,
                    (obj) =>
                    {
                        if (obj is null) {
                            tcs.TrySetResult(true);
                            Log("Couldn't get profil");
                        }
                        else {
                            success(obj);
                            tcs.TrySetResult(true);
                        }
                    });
            }

            return await tcs.Task.ConfigureAwait(true);
        }
        
        private void makeBrowserRequest(string url, Action<object> callback)
        {
            try
            {
                var t1 = new Program.Browser().ShowAsync(url, true, false, 0, 0, 0, "https://www.facebook.com/connect/login_success.html");
                callback(t1.Result);
            }
            catch
            {
                Log("Error in request");
            }
        }

        private void makeRequest(string url, Action<object> callback)
        {
            try
            {
                var t2 = new HttpClient().GetAsync(url);
                callback(t2.Result);
            }
            catch
            {
                Log("Error in request");
            }
        }

        private static void Log(string errorInRequest) {
            Debug.Write(errorInRequest);
        }
    }

};
