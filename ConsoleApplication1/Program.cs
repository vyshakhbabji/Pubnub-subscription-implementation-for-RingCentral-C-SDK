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
        void run()
        {
            var sdk = new SDK("<APP_KEY>", "<APP_SECRET>", "https://platform.devtest.ringcentral.com/", "", "");

            var platform = sdk.GetPlatform();
            Response response = platform.Authorize("<USERNAME>", "<EXT>", "<PASSWORD>", true);
            System.Console.WriteLine(response.GetBody());
            var body = "{\r\n  \"eventFilters\": [ \r\n    \"/restapi/v1.0/account/~/extension/~/presence\", \r\n    \"/restapi/v1.0/account/~/extension/~/message-store\" \r\n  ], \r\n  \"deliveryMode\": { \r\n    \"transportType\": \"PubNub\", \r\n    \"encryption\": \"false\" \r\n  } \r\n}";
            Request request = new Request("/restapi/v1.0/subscription", body);
            response = platform.Post(request);
            JToken bodyString = response.GetJson();
            string encryptionKey = (string)bodyString.SelectToken("deliveryMode").SelectToken("encryptionKey");
            string subscriberKey = (string)bodyString.SelectToken("deliveryMode").SelectToken("subscriberKey");
            string address = (string)bodyString.SelectToken("deliveryMode").SelectToken("address");
            string secretKey = "<SECRET_KEY>";//deliveryMode.getString("secretKey");
            encryptkey = encryptionKey;
            System.Console.WriteLine(subscriberKey);
            Pubnub pubnub = new Pubnub("", subscriberKey, secretKey);
            pubnub.Subscribe<string>(address, DisplaySubscribeReturnMessage, DisplaySubscribeConnectStatusMessage, DisplayErrorMessage);
            System.Console.ReadLine();
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.run();
        }
    }
}
