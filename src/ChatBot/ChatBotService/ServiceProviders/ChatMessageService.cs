using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.ChatBotService.ServiceProviders
{
    internal class ChatMessageService : IChatMessageService
    {
        #region Private Fields



        #endregion



        #region Public Properties

        internal IChatBot Bot { get; set; }

        internal delegate void SendChatMessageDelegate(long interactionId, IChatMessage message, IChatBot bot);

        internal SendChatMessageDelegate SendChatMessageMethod { get; set; }

        #endregion



        #region Private Methods



        #endregion



        #region Public Methods

        public void SendChatMessage(long interactionId, IChatMessage message)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    if (SendChatMessageMethod == null) return;
                    SendChatMessageMethod(interactionId, message, Bot);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        #endregion
    }
}
