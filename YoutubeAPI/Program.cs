using YoutubeAPI.AppLogic;
using YoutubeAPI.Utils;

namespace YoutubeAPI
{
    public static class Program
    {
        public async static Task Main()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("###### Press ENTER to stop application #####");
                Console.ResetColor();


                WebAPI.Start();

                await Console.In.ReadLineAsync();

                await WebAPI.StopAsync();
            }
            catch (Exception e)
            {
                LoggerAsync.Log(LogLevel.FatalError, e.Message);
            }
        }
    }
}