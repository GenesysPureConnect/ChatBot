namespace ININ.Alliances.ChatBotInterface.ChatMessages
{
    public class TextChatMessage : IChatMessage
    {
        public ChatMessageType MessageType { get { return ChatMessageType.Text; } }
        public string Text { get; set; }
    }
}