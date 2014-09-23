using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ININ.Alliances.ChatBotInterface;

namespace ININ.Alliances.ChatBotService.ServiceProviders
{
    internal class ChatReassignmentService : IChatReassignmentService
    {
        internal IChatBot Bot { get; set; }

        internal delegate void ReassignChatDelegate(long interactionId, string suggestedBotName, IChatBot bot);

        internal ReassignChatDelegate ReassignChatMethod { get; set; }

        public void ReassignChat(long interactionId, string suggestedBotName)
        {
            if (ReassignChatMethod == null) return;
            ReassignChatMethod(interactionId, suggestedBotName, Bot);
        }
    }
}
