using System.IO;

namespace ININ.Alliances.ChatBotInterface.ChatMessages
{
    public class FileStreamChatMessage : IChatMessage
    {
        public ChatMessageType MessageType { get { return ChatMessageType.FileStream; } }
        public Stream Stream { get; set; }
        public string Filename { get; set; }
    }
}