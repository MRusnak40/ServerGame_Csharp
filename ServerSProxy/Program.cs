namespace ServerSProxy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {                             
                new TranslationServer(5000);

                
                new TranslationProxy(4000, "127.0.0.1", 5000);
                
                Console.ReadLine();
            }
            catch(Exception e) 
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
