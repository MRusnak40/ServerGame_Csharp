using ServerSProxy.Logic.PlayerCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.ServersLogic
{
    internal class Login
    {
        List<Login> list = new();

        public Login() { }

        private string _username;
        private string _password;
        private string pathToJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "JSON", "Accounts.json");

        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                    builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }

        public async Task<bool> VerifyPassword(string userName, string password)
        {
            List<Login> list = await LoadLogins();
            string hashedInput = HashPassword(password);

            foreach (Login login in list)
            {
                if (login.Username == userName)
                    return login.Password == hashedInput;
            }
            return false;
        }

        public async Task<string> CreateAcc(string userName, string password)
        {
            await _lock.WaitAsync();
            try
            {
                list = await LoadLogins();

                foreach (Login login in list)
                {
                    if (login.Username == userName)
                        return "@\"\r\n    ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼\r\n    █▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀█\r\n    █  Prezdivka je zabrana  ☠️   █\r\n    █▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄█\r\n    ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲\r\n\" ";
                }

               
                string hashed = HashPassword(password);
                list.Add(new Login(userName, hashed));
                await SaveLogins(list);
                return "ok";
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task SaveLogins(List<Login> logins)
        {
            if (logins == null) logins = new List<Login>();
            string jsonData = System.Text.Json.JsonSerializer.Serialize(logins);
            await File.WriteAllTextAsync(pathToJson, jsonData);
        }

        public async Task<List<Login>> LoadLogins()
        {
            if (!File.Exists(pathToJson))
                return new List<Login>();

            string jsonData = await File.ReadAllTextAsync(pathToJson);

            if (string.IsNullOrWhiteSpace(jsonData))
                return new List<Login>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Login>>(jsonData)
                       ?? new List<Login>();
            }
            catch
            {
                return new List<Login>();
            }
        }

        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

      
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

      
        public Login(string username, string hashedPassword)
        {
            _username = username;
            _password = hashedPassword;
        }
    }
}