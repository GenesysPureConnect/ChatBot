namespace ININ.Alliances.ChatBotService
{
    public class MainTopic : TopicTracer
    {
        public static int hdl = I3Trace.initialize_topic("ChatBot.Main", 80);

        public override int get_handle()
        {
            return hdl;
        }
    }
    public class BotTopic : TopicTracer
    {
        public static int hdl = I3Trace.initialize_topic("ChatBot.Bot", 80);

        public override int get_handle()
        {
            return hdl;
        }
    }
    public class BotManagerTopic : TopicTracer
    {
        public static int hdl = I3Trace.initialize_topic("ChatBot.BotManager", 80);

        public override int get_handle()
        {
            return hdl;
        }
    }

    public class Trace
    {
        public static MainTopic Main = new MainTopic();
        public static BotTopic Bot = new BotTopic();
        public static BotManagerTopic BotManager = new BotManagerTopic();
    }
}
