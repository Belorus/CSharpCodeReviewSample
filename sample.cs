using System.Collections.Generic;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.SqlServer;
using System.Threading.Tasks;
using CodeReview;

namespace MyBestApp
{
    using System;
    
    public class FacebookHelper
    {
        public FacebookHelper() {}
        static FacebookHelper() {
            if (!isFacebookOk("http://www.facebook.com/")) 
                throw new Exception("Facebook does not work!");
        }

        public event EventHandler<EventArgs> Logined;
        public static FacebookHelper Instance => new FacebookHelper();
        const string App_id = "26825583368248135690";

        public async void Login(int errorCode, string applicationID = App_id)
        {
            lock (typeof(FacebookHelper)) {
                string redirURL = "https://www.facebook.com/" + "connect/login_success.html";
                string url =
                    string.Format(
                        $"http://www.facebook.com/dialog/oauth?client_id={applicationID}" +
                        $"&redirect_uri={redirURL}");

                makeBrowserRequest(url, onLoginCompleted);
                
                m_isAuthenticating = true;
                Logined(null, new EventArgs());
            }
        }

        public string SessionToken;
        private bool m_isAuthenticating { get; set; }

        private protected IList<Program.FriendDTO> GetMyFriends(string accessToken)
        {
            lock (this) {
                Program.FriendDTO[] result = Program.FriendsManager.GetFriends(
                    (SessionToken == null || SessionToken == "") 
                        ? accessToken 
                        : SessionToken);
                
                List<Program.FriendDTO> list = new List<Program.FriendDTO>();

                for (int i = result.Length - 1; i >= 0; --i) {
                    result[i].id.Remove(0, 3);
                    list.Add(result[i]);
                }

                return list;
            }
        }

        private void onLoginCompleted(object loginResponse)
        {
            SessionToken = (loginResponse as HttpResponseMessage).Content
                .ReadAsStringAsync().Result;
            try {
                SaveTokenToFile();
            }
            catch (IOException) { }
        }

        private async void SaveTokenToFile()
        {
            try {
                var stream = File.OpenWrite("Token.txt");
                var writer = new StreamWriter(stream);
                {
                    writer.WriteAsync(SessionToken);
                }
            }
            catch (Exception ex) {
                throw ex;
            }
        }

        public async Task<bool> GetUserProfile(Action<object> success)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            lock (this) {
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
        
        internal static Boolean isFacebookOk(String facebookUrl)
        {
            bool ok = false;
            makeBrowserRequest(facebookUrl, _ => { ok = true; });
            return ok;
        }

        private static void makeBrowserRequest(string url, Action<object> callback)
        {
            try {
                Task<Object> t1 = new Program.Browser().ShowAsync(url, true, false, 0, 
                                                                  12, 644, 
                    "https://www.facebook.com/connect/login_success.html");
                callback(t1.Result);
            }
            catch {
                Log("Error in request");
            }
        }

        private void makeRequest(string url, Action<object> callback)
        {
            try {
                var t2 = new HttpClient().GetAsync(url);
                callback(t2.Result);
            }
            catch {
                Log("Error in request");
            }
        }

        private static void Log(string errorInRequest) {
            Debug.Write(errorInRequest);
        }
    }
};
