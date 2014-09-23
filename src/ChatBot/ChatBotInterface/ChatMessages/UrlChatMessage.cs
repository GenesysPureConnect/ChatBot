using System;

namespace ININ.Alliances.ChatBotInterface.ChatMessages
{
    public class UrlChatMessage : IChatMessage
    {
        public ChatMessageType MessageType { get { return ChatMessageType.Url; } }
        public Uri Uri { get; set; }
    }
}