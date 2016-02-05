using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RingCentral.SDK;
using RingCentral.SDK.Http;
using RingCentral.Subscription;
using PubNubMessaging.Core;
using Newtonsoft.Json.Linq;
using System.Threading;

using System.Diagnostics;
using System.Security.Cryptography;

using Newtonsoft.Json;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

using System.Timers;

namespace ConsoleApplication1
{
    class Program
    {
        string encryptkey;

        public string Encryptkey
        {
            get
            {
                return encryptkey;
            }
            set
            {
                encryptkey = value;
            }
        }

        void DecryptMessage(string message)
        {
            var deserializedMessage = JsonConvert.DeserializeObject<List<string>>(message.ToString());
            byte[] decodedEncryptionKey = Convert.FromBase64String(encryptkey);
            byte[] data = Convert.FromBase64String(deserializedMessage[0]);
            byte[] iv = new byte[16];
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.BlockSize = 128;
            aes.KeySize = 128;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform decrypt = aes.CreateDecryptor(decodedEncryptionKey, iv))
            {
                byte[] dest = decrypt.TransformFinalBlock(data, 0, data.Length);
                decrypt.Dispose();
                Console.WriteLine(Encoding.UTF8.GetString(dest));
            }
        }

        void DisplaySubscribeReturnMessage(string result)
        {
            Console.WriteLine("SUBSCRIBE REGULAR CALLBACK:");
            DecryptMessage(result);
        }
         void DisplaySubscribeConnectStatusMessage(string result)
        {
            Console.WriteLine("SUBSCRIBE CONNECT CALLBACK");
        }
         void DisplayErrorMessage(PubnubClientError pubnubError)
        {
            Console.WriteLine(pubnubError.StatusCode);
        }
        void run(Platform platform,Response response)
        {
           
            System.Console.WriteLine(response.GetBody());
            
            //adding event filters
            var body = "{\r\n  \"eventFilters\": [ \r\n    \"/restapi/v1.0/account/~/extension/~/presence\", \r\n    \"/restapi/v1.0/account/~/extension/~/message-store\" \r\n  ], \r\n  \"deliveryMode\": { \r\n    \"transportType\": \"PubNub\", \r\n    \"encryption\": \"false\" \r\n  } \r\n}";
            Request request = new Request("/restapi/v1.0/subscription", body);
            response = platform.Post(request);
            
            //getSubscription
            JToken bodyString = response.GetJson();
            string encryptionKey = (string)bodyString.SelectToken("deliveryMode").SelectToken("encryptionKey");
            string subscriberKey = (string)bodyString.SelectToken("deliveryMode").SelectToken("subscriberKey");
            string address = (string)bodyString.SelectToken("deliveryMode").SelectToken("address");
            string secretKey = "<SECRET_KEY>";//deliveryMode.getString("secretKey");
            encryptkey = encryptionKey;
            System.Console.WriteLine(subscriberKey);
            
            //Subscribe to Pubnub's subscription channel
            Pubnub pubnub = new Pubnub("", subscriberKey, secretKey);
            pubnub.Subscribe<string>(address, DisplaySubscribeReturnMessage, DisplaySubscribeConnectStatusMessage, DisplayErrorMessage);
            System.Console.ReadLine();
        }
        
          public void downloadFaxes(Platform platform, string token)
        {
            string url = "https://platform.devtest.ringcentral.com/restapi/v1.0/account/131192004/extension/131192004/message-store/1261827004/content/1261827004";


            string filepath = "C:\\Users\\vyshakh.babji\\Desktop\\recording\\fax.pdf";


            HttpWebRequest requests = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;
            requests.KeepAlive = true;
            requests.Method = "GET";
            requests.ContentLength = 0;
            //  requests.ContentType = "application/json";

            ////Add access token to Request header
            requests.Headers.Add("Authorization", String.Format("Bearer {0}", token));

            using (HttpWebResponse httpResponse = requests.GetResponse() as System.Net.HttpWebResponse)
            {
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    // throw everything you read into a MemoryStream and get the byte array in the end
                    var bytes = default(byte[]);
                    using (var mem = new MemoryStream())
                    {
                        reader.BaseStream.CopyTo(mem);
                        bytes = mem.ToArray();
                        //write the byte array as .mp3 file
                        File.WriteAllBytes(filepath, bytes);
                    }
                }
            }


        }
        
        public void downloadOneRecording(Platform platform, string token)
        {
            string filepath = "C:\\Users\\vyshakh.babji\\Desktop\\recording\\1.mp3";

            //call-log api endpoint
            Request request = new Request("/restapi/v1.0/account/~/extension/~/call-log?withRecording=true");
            Response response = platform.Get(request);
            JObject bodyString = response.GetJson();

            //fetch records from call-log
            JArray records = (JArray)bodyString.GetValue("records");

            //fetch recording data among records and get content uri for recording
            JToken contentUri = (string)records[0].SelectToken("recording").SelectToken("contentUri");

            // access the content uri from the call-log https://media.devtest.ringcentral.com:443/restapi/v1.0/account/131192004/recording/1333302004/content
            string url = (string)contentUri;
            Console.WriteLine(url);

            //make a get request for content uri by passing access token
            HttpWebRequest requests = System.Net.WebRequest.Create(url) as System.Net.HttpWebRequest;
            requests.KeepAlive = true;
            requests.Method = "GET";
            requests.ContentLength = 0;
            requests.ContentType = "application/json";

            ////Add access token to Request header
            requests.Headers.Add("Authorization", String.Format("Bearer {0}", token));

            //Get HttpWebResponse from GET request
            using (HttpWebResponse httpResponse = requests.GetResponse() as System.Net.HttpWebResponse) { 
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    // throw everything you read into a MemoryStream and get the byte array in the end
                    var bytes = default(byte[]);
                    using (var mem = new MemoryStream())
                    {
                        reader.BaseStream.CopyTo(mem);
                        bytes = mem.ToArray();
                        //write the byte array as .mp3 file
                        File.WriteAllBytes(filepath, bytes);
                    }
                } 
            }
        }


        static void Main(string[] args)
        {
            //add appkey, app secret, Username=Sandbox phone number, Extension and Sandbpx password
            var sdk = new SDK("<APP KEY>", "<APP SECRET>", "https://platform.devtest.ringcentral.com/", "1", "1");
            var platform = sdk.GetPlatform();
            Response response = platform.Authorize("<USERNAME>", "<EXT>", "<PASSWORD>", true);
            
            //fetch access token
            string accesstoken = (string)response.GetJson().SelectToken("access_token");
            Program p = new Program();
            //subscription
            p.run(platform,response);
            
            //access call-log 
            //download call-recording
            p.downloadOneRecording(platform);
        }
    }
}
