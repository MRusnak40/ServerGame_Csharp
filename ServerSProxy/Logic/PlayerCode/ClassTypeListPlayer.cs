using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ServerSProxy.Logic.PlayerCode
{
    internal class  ClassTypeListPlayer

    {
        public static Dictionary<string, PlayerClassTemplate> AvailableClasses { get;  set; } = new Dictionary<string, PlayerClassTemplate>();

        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "JSON", "PlayerClassType.json");


        public ClassTypeListPlayer() { }




        public async Task LoadFromFile()
        {

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[ERROR] Soubor s třídami nebyl nalezen na cestě: {filePath}");
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);

               
                var loadedList = JsonSerializer.Deserialize<List<PlayerClassTemplate>>(jsonString);

                
                AvailableClasses.Clear();
                foreach (var classTemplate in loadedList)
                {
                 
                    AvailableClasses.Add(classTemplate.ClassId, classTemplate);
                }

                Console.WriteLine($"[SYSTEM] Úspěšně načteno {AvailableClasses.Count} herních tříd z JSONu.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Chyba při načítání tříd: {ex.Message}");
            }

        }


           
        }
    }
