using System;

namespace ININ.Alliances.ChatBotConsole
{
    public class Program
    {
        private static ChatBotService.ChatBotService _chatBot;

        static void Main(string[] args)
        {
            try
            {
                _chatBot = new ChatBotService.ChatBotService();
                Console.WriteLine("Chat bot initialized. Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Error encountered. Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
