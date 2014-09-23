namespace ININ.Alliances.ChatBotInterface.ChatMessages
{
    public class FilePathChatMessage : IChatMessage
    {
        public ChatMessageType MessageType { get { return ChatMessageType.FilePath; } }
        public string Path { get; set; }
    }
}