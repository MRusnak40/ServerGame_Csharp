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
        private string pathToJson;


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

        public async Task<bool> VerifyPassword(string userName,string password )
        {
            List<Login> list = new();
            list = await LoadLogins(); // Načte loginy z JSON souboru


            foreach (Login login in list)
            {
                if (login.Username == userName)
                {
                    string hashedInput = HashPassword(password);
                    return login.Password == hashedInput;
                }

            }


            return false; // Pokud nenalezen žádný shodný uživatel nebo heslo, vrátí false
        }



        //createing acc
        public async Task<string> CreateAcc(string userName,string password)
        {
            //vytvorit player bude pred metodou
            List<Login> list = new();
            list = await LoadLogins(); // Načte loginy z JSON souboru


            foreach (Login login in list)
            {
                if (login.Username == userName)
                {
                    string hashedInput = HashPassword(password);
                    bool isGood = login.Password == hashedInput;

                    if (isGood)
                    {
                        Console.WriteLine($"uzivatel s {userName} a s heslem {HashPassword(password)} zadal spravne heslo presmerovani na prihlaseni");
                        return "Tento účet již existuje, přihlaš se";
                    }
                    else
                    {
                        Console.WriteLine($"uzivatel s {userName} a s heslem {HashPassword(password)} se znasi vytvorit acc" );
                        return "Prezdivka je jiz zabrana";

                    }
                }
            }


            return "ok"; 


        }







        //LOAD z jsonu bude pokazde a bude fungovat ze se nahraje do lsitu a z listu se pak zkontoruluje jestli se shoduje username a password, pokud ano, tak se prihlasi, pokud ne, tak ne


        //file working
        public async Task SaveLogins(List<Login> logins)
        {
          
            string jsonData = System.Text.Json.JsonSerializer.Serialize(logins);
            await File.WriteAllTextAsync(pathToJson, jsonData);
        }

        public async Task<List<Login>> LoadLogins()
        {
           
            if (File.Exists(pathToJson))
            {
                string jsonData = await File.ReadAllTextAsync(pathToJson);
                return System.Text.Json.JsonSerializer.Deserialize<List<Login>>(jsonData);
            }
            return new List<Login>(); // Vrátí prázdný seznam, pokud soubor neexistuje
        }



        //default gettery a settery
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


        public Login(string username, string password)
        {
            Password = password;
            Username = username;


        }

    }
}
