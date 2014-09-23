using System;
using ININ.Alliances.ChatBotInterface;

namespace ININ.Alliances.ChatBotService.ServiceProviders
{
    internal class ChatBotServiceProvider : IServiceProvider
    {
        #region Private Fields

        private readonly AttributeService _attributeService;
        private readonly ChatMessageService _chatMessageService;
        private readonly ChatReassignmentService _chatReassignmentService;

        #endregion



        #region Public Properties

        internal IChatBot Bot { get; set; }

        #endregion



        internal ChatBotServiceProvider(IChatBot bot, ServiceProviderDelegates delegates)
        {
            Bot = bot;
            _attributeService = new AttributeService
            {
                Bot = bot,
                GetAttributeMethod = delegates.GetAttributeMethod,
                GetAttributesMethod = delegates.GetAttributesMethod,
                SetAttributeMethod = delegates.SetAttributeMethod,
                SetAttributesMethod = delegates.SetAttributesMethod
            };
            _chatMessageService = new ChatMessageService
            {
                Bot = bot,
                SendChatMessageMethod = delegates.SendChatMessageMethod
            };
            _chatReassignmentService = new ChatReassignmentService
            {
                Bot = bot,
                ReassignChatMethod = delegates.ReassignChatMethod
            };
        }



        #region Private Methods



        #endregion



        #region Public Methods

        public object GetService(Type serviceType)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    if (serviceType == typeof(TopicTracer)) return Trace.Bot;
                    if (serviceType == typeof(IAttributeService)) return _attributeService;
                    if (serviceType == typeof(IChatMessageService)) return _chatMessageService;
                    if (serviceType == typeof(IChatReassignmentService)) return _chatReassignmentService;
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
                return null;
            }
        }

        #endregion
    }
}
