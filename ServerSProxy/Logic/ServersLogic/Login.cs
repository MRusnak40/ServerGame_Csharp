using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.ServersLogic
{
    internal class Login
    {
        List<Login> list = new();
        private string _username;
        private string _password;
        private string pathToJson = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "JSON", "Accounts.json");

        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

        public Login() { }

        public Login(string username, string hashedPassword)
        {
            _username = username;
            _password = hashedPassword;
        }

        public string Username { get => _username; set => _username = value; }
        public string Password { get => _password; set => _password = value; }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
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
            List<Login> logins = await LoadLogins();
            string hashedInput = HashPassword(password);
            foreach (Login login in logins)
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

            await _fileSemaphore.WaitAsync();
            try
            {
                await File.WriteAllTextAsync(pathToJson, jsonData);
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async Task<List<Login>> LoadLogins()
        {
            await _fileSemaphore.WaitAsync();
            try
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
            finally
            {
                _fileSemaphore.Release();
            }
        }
    }
}