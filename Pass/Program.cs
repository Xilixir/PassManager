using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace Pass {
    class Program {
        private static readonly string[] startup = new string[] {
                "  ____    __  __  ____     ____    __    __  ____    ______      __  __     ___     ",
                " /\\  _`\\ /\\ \\/\\ \\/\\  _`\\  /\\  _`\\ /\\ \\  /\\ \\/\\  _`\\ /\\__  _\\    /\\ \\/\\ \\  /'___`\\   ",
                " \\ \\ \\L\\_\\ \\ `\\\\ \\ \\ \\/\\_\\\\ \\ \\L\\ \\ `\\`\\\\/'/\\ \\ \\L\\ \\/_/\\ \\/    \\ \\ \\ \\ \\/\\_\\ /\\ \\  ",
                "  \\ \\  _\\L\\ \\ , ` \\ \\ \\/_/_\\ \\ ,  /`\\ `\\ /'  \\ \\ ,__/  \\ \\ \\     \\ \\ \\ \\ \\/_/// /__ ",
                "   \\ \\ \\L\\ \\ \\ \\`\\ \\ \\ \\L\\ \\\\ \\ \\\\ \\ `\\ \\ \\   \\ \\ \\/    \\ \\ \\     \\ \\ \\_/ \\ // /_\\ \\",
                "    \\ \\____/\\ \\_\\ \\_\\ \\____/ \\ \\_\\ \\_\\ \\ \\_\\   \\ \\_\\     \\ \\_\\     \\ `\\___//\\______/",
                "     \\/___/  \\/_/\\/_/\\/___/   \\/_/\\/ /  \\/_/    \\/_/      \\/_/      `\\/__/ \\/_____/ "
            };

        [STAThread]
        static void Main(string[] args) {
            foreach (string s in startup) {
                Console.WriteLine(s);
            }
            Console.WriteLine();

            DriveInfo[] driveInfo = DriveInfo.GetDrives();
            string keyPath = null, dataPath = null, ivPath = null;
            bool keyFound = false, dataFound = false, ivFound = false;
            string keyFileName = "encrypted_data_key", ivName = "initialization_vector", dataFolderName = "encrypted_data";

            foreach (DriveInfo info in driveInfo) {
                foreach (string path in Directory.GetFiles(info.RootDirectory.FullName)) {
                    string fileName = Path.GetFileName(path);
                    if (fileName.Equals(keyFileName)) {
                        keyPath = path;
                        keyFound = true;
                        Console.WriteLine(" Found key file at '" + keyPath + "'");
                    } else if (fileName.Equals(ivName)) {
                        ivPath = path;
                        ivFound = true;
                        Console.WriteLine(" Found initialization vector at '" + ivPath + "'");
                    }
                }
                foreach (string path in Directory.GetDirectories(info.RootDirectory.FullName)) {
                    string folderName = Path.GetFileName(path);
                    if (folderName.Equals(dataFolderName)) {
                        dataPath = path;
                        dataFound = true;
                        Console.WriteLine(" Found data folder at '" + dataPath + "'");
                    }
                }
            }

            if (!keyFound || !dataFound || !ivFound) {
                string s1 = "";
                bool single = false;
                if (!keyFound && !ivFound && !dataFound) {
                    s1 = "key file, data folder, and initialization vector";
                } else if (!keyFound && !ivFound) {
                    s1 = "key file and initialization vector";
                } else if (!ivFound && !dataFound) {
                    s1 = "initialization vector and data folder";
                } else if (!keyFound && !dataFound) {
                    s1 = "key file and data folder";
                } else {
                    single = true;
                    s1 = !keyFound ? "key file" : (!ivFound ? "initialization vector" : "data folder");
                }

                // create dummy data folder, key file, and IV
                Directory.CreateDirectory(dataFolderName);
                byte[] random = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                EncryptionData ed = AES_Encrypt(random);
                byte[] dummyKey = ed.key;
                byte[] dummyIv = ed.initialization_vector;
                File.WriteAllBytes(keyFileName, dummyKey);
                File.WriteAllBytes(ivName, dummyIv);

                errorWithMessage(new string[] {
                    s1 + " not found, place " + (single ? "each " : "") + "in a root directory with the generated file name.",
                    "Default files to be used are created in this program's working directory."
                });
            }

            byte[] key = File.ReadAllBytes(keyPath);
            byte[] iv = File.ReadAllBytes(ivPath);
            string[] files = Directory.GetFiles(dataPath);
            Dictionary<string, List<LoginDetails>> details = new Dictionary<string, List<LoginDetails>>();

            for (int i = 0; i < files.Length; i++) {
                string path = files[i];
                byte[] encryptedData = File.ReadAllBytes(path);
                try {
                    byte[] decryptedData = AES_Decrypt(encryptedData, key, iv);
                    string s1 = ASCIIEncoding.ASCII.GetString(decryptedData);
                    LoginDetails d = JsonConvert.DeserializeObject<LoginDetails>(s1);
                    d.filePath = path;
                    if (details.ContainsKey(d.site)) {
                        details[d.site].Add(d);
                    } else {
                        details.Add(d.site, new List<LoginDetails>(new[] { d }));
                    }
                } catch (Exception ex) {
                    errorWithMessage(new string[] { "Decryption failed!" });
                }
            }

            string[] sites = new string[details.Keys.Count];
            int index = 0;
            foreach (string s in details.Keys) {
                sites[index] = s;
                index++;
            }

            Console.WriteLine();
            MenuList firstSelection = new MenuList(new string[] { "Read", "Write"});
            firstSelection.showMenu(" Select index:");
            int sel1 = firstSelection.selection;

            if (sel1 == 0)
            {
                MenuList siteList = new MenuList(sites);
                siteList.showMenu(" Select the index of the required site:");
                int selection = siteList.selection;
                if (selection >= details.Values.Count)
                {
                    Console.WriteLine(" Array index out of bounds!");
                }
                List<LoginDetails> ld = details.Values.ElementAt(selection);

                string[] accounts = new string[ld.Count];
                for (int i = 0; i < ld.Count; i++)
                {
                    LoginDetails d = ld[i];
                    if (d.email.Length < 1) {
                        d.email = "no@email.com";
                    }
                    if (d.username.Length < 1)
                    {
                        d.username = "no_username";
                    }
                    accounts[i] = (" " + formatAccountInfo(d));
                }

                MenuList accountList = new MenuList(accounts);
                accountList.showMenu(" Select the index of the required account:");
                int sel2 = accountList.selection;
                if (sel2 >= ld.Count)
                {
                    Console.WriteLine(" Array index out of bounds!");
                }
                LoginDetails d2 = ld[sel2];

                Console.WriteLine("");
                Console.WriteLine("Password copied to clipboard!");
                Console.WriteLine("");

                Clipboard.SetText(d2.password);

                AccountList acl = new AccountList();
                acl.showMenu(" Select the index of the required action: ");
                int sel3 = acl.selection;
                string oldInfo = formatAccountInfo(d2);
                if (sel3 == 0)
                {
                    // edit email
                    string str = acl.input;
                    d2.email = str;
                }
                else if (sel3 == 1)
                {
                    // edit username
                    string str = acl.input;
                    d2.username = str;
                }
                else if (sel3 == 2)
                {
                    // edit password
                    string str = acl.input;
                    d2.password = str;
                }
                else if (sel3 == 3)
                {
                    // regenerate password
                    d2.password = generatePassword();
                }
                else if (sel3 == 4)
                {
                    // delete acccount
                    File.Delete(d2.filePath);
                }
                if (sel3 < 4)
                {
                    string json = JsonConvert.SerializeObject(d2);
                    byte[] detb = ASCIIEncoding.ASCII.GetBytes(json);
                    EncryptionData enc = AES_Encrypt(detb, key, iv);
                    File.WriteAllBytes(d2.filePath, enc.encryptedBytes);

                    Console.WriteLine();
                    Console.WriteLine(" [Old] " + oldInfo);
                    Console.WriteLine(" [New] " + formatAccountInfo(d2));
                    Console.WriteLine();
                    Console.WriteLine(" Success!");
                }

            }
            else if (sel1 == 1)
            {
                Console.WriteLine();
                Console.WriteLine(" Site:");
                string site = Console.ReadLine();
                Console.WriteLine(" Email:");
                string email = Console.ReadLine();
                Console.WriteLine(" Username:");
                string user = Console.ReadLine();
                Console.WriteLine(" Pass: (leave blank to generate)");
                string pass = Console.ReadLine();
                if (pass.Length == 0)
                {
                    pass = generatePassword();
                }

                LoginDetails det = new LoginDetails(site, email, user, pass);
                string json = JsonConvert.SerializeObject(det);
                byte[] detb = ASCIIEncoding.ASCII.GetBytes(json);
                EncryptionData enc = AES_Encrypt(detb, key, iv);
                string uuid = Guid.NewGuid().ToString();
                File.WriteAllBytes(dataPath + "/" + uuid, enc.encryptedBytes);

                Console.WriteLine(" Success!");
            }
            else {
                Console.WriteLine(" Invalid selection!");
                Environment.Exit(0);
            }
        }

        public static string formatAccountInfo(LoginDetails d) {
            return (formatSpaces(d.site, 25) + " " + formatSpaces(d.email, 25) + "  " + formatSpaces(d.username, 25) + d.password);
        }

        public static string generatePassword() {
            Random r = new Random();
            string ar = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            string re = "";
            while (re.Length < 16) {
                re += ar[r.Next(ar.Length)];
            }
            return re;
        }

        public static string formatSpaces(string str, int spaces) {
            string str2 = "";
            while ((str2.Length + str.Length) < spaces) {
                str2 = str2 + "-";
            }
            return str + " " + str2;
        }

        public static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] key, byte[] iv) {
            byte[] decryptedBytes = null;
            using (MemoryStream ms = new MemoryStream()) {
                using (RijndaelManaged AES = new RijndaelManaged()) {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Key = key;
                    AES.IV = iv;
                    AES.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write)) {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }
            return decryptedBytes;
        }

        public static EncryptionData AES_Encrypt(byte[] bytesToBeEncrypted, byte[] key, byte[] iv) {
            byte[] encryptedBytes = null;
            using (MemoryStream ms = new MemoryStream()) {
                using (RijndaelManaged AES = new RijndaelManaged()) {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.Key = key;
                    AES.IV = iv;
                    AES.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write)) {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                    return new EncryptionData(encryptedBytes, AES.Key, AES.IV);
                }
            }
        }

        public static EncryptionData AES_Encrypt(byte[] bytesToBeEncrypted) {
            byte[] encryptedBytes = null;
            using (MemoryStream ms = new MemoryStream()) {
                using (RijndaelManaged AES = new RijndaelManaged()) {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;
                    AES.GenerateKey();
                    AES.GenerateIV();
                    AES.Mode = CipherMode.CBC;
                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write)) {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                    return new EncryptionData(encryptedBytes, AES.Key, AES.IV);
                }
            }
        }

        public class LoginDetails {
            public string site { get; set; }
            public string email { get; set; }
            public string username { get; set; }
            public string password { get; set; }
            public string filePath { get; set; }

            public LoginDetails(string site, string email, string username, string password) {
                this.site = site;
                this.email = email;
                this.username = username;
                this.password = password;
            }
        }

        public class EncryptionData {
            public byte[] encryptedBytes { get; set; }
            public byte[] key { get; set; }
            public byte[] initialization_vector { get; set; }

            public EncryptionData(byte[] encryptedBytes, byte[] key, byte[] initialization_vector) {
                this.encryptedBytes = encryptedBytes;
                this.key = key;
                this.initialization_vector = initialization_vector;
            }
        }

        public static void errorWithMessage(string[] message) {
            Console.WriteLine();
            foreach (string s in message) {
                Console.WriteLine(" " + s);
            }
            Environment.Exit(1);
        }
    }
}
