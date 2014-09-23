using System;
using System.Collections.Generic;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.TimeBot
{
    public class TimeBot : IChatBot
    {
        private readonly List<string> _requestedAttributes = new List<string>(
            new[]
            {
                "Eic_RemoteName",
                "Eic_RemoteNumber"
            });

        private TopicTracer _trace;
        private IAttributeService _attributeService;
        private IChatMessageService _chatMessageService;
        private IChatReassignmentService _chatReassignmentService;



        #region IChatBot Members

        public string BotName { get { return "Time Bot"; } }
        public IEnumerable<string> RequestedAttributes { get { return _requestedAttributes; } }
        public ushort Priority { get { return 0; } }


        public void OnLoad(IServiceProvider serviceProvider)
        {
            try
            {
                _trace = serviceProvider.GetService(typeof(TopicTracer)) as TopicTracer;
                _attributeService = serviceProvider.GetService(typeof(IAttributeService)) as IAttributeService;
                _chatMessageService = serviceProvider.GetService(typeof(IChatMessageService)) as IChatMessageService;
                _chatReassignmentService = serviceProvider.GetService(typeof(IChatReassignmentService)) as IChatReassignmentService;

                Console.WriteLine("[{0}] - OnLoad", BotName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void OnUnload()
        {
            Console.WriteLine("[{0}] - OnUnload", BotName);
        }

        public bool ClaimInteraction(long interactionId, IDictionary<string, string> interactionAttributes, bool prettyPlease)
        {
            Console.WriteLine("[{0}] - ClaimInteraction ({1})", BotName, interactionId);

            return true;
        }

        public void InteractionAssigned(long interactionId, IDictionary<string, string> interactionAttributes)
        {
            Console.WriteLine("[{0}] - InteractionAssigned ({1})", BotName, interactionId);

            _chatMessageService.SendChatMessage(interactionId,
                new TextChatMessage { Text = "Welcome to the time bot. Say anything and I will tell you the current time." });
        }

        public IChatMessage GetResponse(long interactionId, IChatMessage message)
        {
            Console.WriteLine("[{0}] - GetResponse ({1})", BotName, interactionId);

            return new TextChatMessage { Text = "The current time is " + DateTime.Now.ToLongTimeString() };
        }

        public void InteractionUnassigned(long interactionId, UnassignmentReason reason)
        {
            Console.WriteLine("[{0}] - InteractionUnassigned ({1}, {2})", BotName, interactionId, reason);
        }

        #endregion



        public void Dispose()
        {
            
        }
    }
}
