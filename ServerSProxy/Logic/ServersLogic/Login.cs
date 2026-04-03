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
        public Login() { }

        //List<Login> _logins = new List<Login>() {};

        private string _username;
        private string _password;
        
        

        public string HashPassword(string password)
        {
            // Implementace hashování hesla (např. pomocí SHA256)
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public bool VerifyPassword(string password)
        {
            string hashedInput = HashPassword(password);
            return _password == hashedInput;
        }



        public string Username
        {
            get { return _username; }
            set { _username = value; }
        }

        public string Password
        {
            get { return _password; }
            set { _password = HashPassword(value); }
        }


        public Login(string username, string password) {
            Password = password;
            Username = username;


        }

    }
}
