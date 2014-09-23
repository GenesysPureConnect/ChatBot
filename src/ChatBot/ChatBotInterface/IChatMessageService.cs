using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.ChatBotInterface
{
    public interface IChatMessageService
    {
        /// <summary>
        /// Sends a message to a chat
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="message">The message to send</param>
        void SendChatMessage(long interactionId, IChatMessage message);
    }
}
