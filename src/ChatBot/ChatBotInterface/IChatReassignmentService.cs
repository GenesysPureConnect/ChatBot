using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ININ.Alliances.ChatBotInterface
{
    public interface IChatReassignmentService
    {
        /// <summary>
        /// Attempts to reassign the chat to another bot
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="suggestedBotName">The name of the bot to attempt to assign the chat to</param>
        void ReassignChat(long interactionId, string suggestedBotName);
    }
}
