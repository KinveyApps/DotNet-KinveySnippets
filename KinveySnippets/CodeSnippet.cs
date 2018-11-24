using System.IO;
using Kinvey;
using Newtonsoft.Json;
#if __ANDROID__
using Android.App;
using Android.Content;
#elif __IOS__
using Foundation;
using UIKit;
#endif

namespace KinveySnippets
{
    class CodeSnippet
#if __ANDROID__
        : Activity
#elif __IOS__
        : UIViewController
#endif
    {
        protected DataStore<ToDo> todoStore;
        protected DataStore<Book> bookStore;
        protected DataStore<Book> dataStore;
        protected Book book;
        protected ToDo t;
        protected Book entity;
        protected string your_app_key;
        protected string your_app_secret;
        protected Client kinveyClient;
        protected string username;
        protected string password;
        protected string myUsername;
        protected string myPassword;
        protected string authServiceId;
        protected string myAppKey;
        protected string myAppSecret;
        protected string myProjectID;
        protected string usernameOrEmail;
        protected EventEntity myEvent;
        protected DataStore<Book> books;
        protected User myFriend1;
        protected User myFriend2;
        protected User myUser;
        protected FileMetaData uploadFMD;
        protected FileMetaData fileMetaData;
        protected string your_file_path;
        protected byte[] content;
        protected string downloadStreamFilePath;
        protected string your_file_name;
        protected string userID;
        protected string filePath;
        protected string collectionName;

        protected byte[] GetFileDataAsByteArray() { return null; }
        protected Stream GetFileDataAsStream() { return null; }
#if __ANDROID__
        protected virtual void OnNewIntent(Intent intent) { }
#endif
#if __IOS__
        public virtual bool OpenUrl(UIApplication application, NSUrl url, string sourceApplication, NSObject annotation) { return false; }
        public virtual void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken) { }
        public virtual void FailedToRegisterForRemoteNotifications(UIApplication application, NSError error) { }
#endif
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ToDo : Kinvey.Entity
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("due_date")]
        public string DueDate { get; set; }
    }
}
