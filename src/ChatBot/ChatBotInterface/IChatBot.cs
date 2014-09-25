using System;
using System.Collections.Generic;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.ChatBotInterface
{
    public interface IChatBot : IDisposable
    {
        #region Public Properties

        /// <summary>
        /// The name of the bot
        /// </summary>
        string BotName { get; }

        /// <summary>
        /// The interaction attributes (names) that should be provided in InteractionAdded
        /// </summary>
        IEnumerable<string> RequestedAttributes { get; }

        /// <summary>
        /// <para>The priority of this bot to be compared with other bots. A larger number is a higher priority</para>
        /// <para>0 = lowest priority</para>
        /// <para>65535 = highest priority</para>
        /// </summary>
        ushort Priority { get; }

        #endregion



        #region Public Methods

        /// <summary>
        /// Called immediately after instantiation
        /// </summary>
        /// <param name="serviceProvider">
        /// <para>Supported service types:</para>
        /// <para>TopicTracer - ININ tracing topic</para>
        /// <para>IAttributeService - Service for getting and setting attributes on interactions</para>
        /// <para>IChatMessageService - Service for sending messages to chats</para>
        /// <para>IChatReassignmentService - Service for requesting that the chat be reassigned to another bot</para>
        /// </param>
        void OnLoad(IServiceProvider serviceProvider);

        /// <summary>
        /// Called to ask this bot if it wishes to handle the interaction
        /// </summary>
        /// <param name="interactionId">The interaction ID of the interaction</param>
        /// <param name="interactionAttributes">The attribute names (key) and attribute values (value) requested via RequestedAttributes</param>
        /// <param name="prettyPlease">[True] if the bot service could not find a bot in the first pass, [False] if this is the first pass</param>
        /// <returns>[True] if this bot wishes to claim the interaction, [False] to pass on this interaction</returns>
        bool ClaimInteraction(long interactionId, IDictionary<string, string> interactionAttributes, bool prettyPlease);

        /// <summary>
        /// Called when the bot is selected for a chat. Service provider calls for the interaction will not 
        /// work prior to InteractionAssigned being called. Service provider calls will work in this method.
        /// </summary>
        /// <param name="interactionId">The interaction ID of the interaction</param>
        /// <param name="interactionAttributes">The attribute names (key) and attribute values (value) requested via RequestedAttributes</param>
        void InteractionAssigned(long interactionId, IDictionary<string, string> interactionAttributes);

        /// <summary>
        /// Called when a chat message is received
        /// </summary>
        /// <param name="interactionId">The interaction ID of the interaction</param>
        /// <param name="message">The message that was received</param>
        /// <returns>The message to send to the user</returns>
        IChatMessage GetResponse(long interactionId, IChatMessage message);

        /// <summary>
        /// Called when a chat is disconnected
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="reason">The reason the interaction was unassigned</param>
        void InteractionUnassigned(long interactionId, UnassignmentReason reason);

        /// <summary>
        /// Called when the addin is unloaded
        /// </summary>
        void OnUnload();

        #endregion
    }
}
