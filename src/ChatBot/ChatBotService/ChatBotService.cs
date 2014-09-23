using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;
using ININ.Alliances.ChatBotService.ServiceProviders;
using ININ.IceLib.Connection;
using ININ.IceLib.Interactions;

namespace ININ.Alliances.ChatBotService
{
    public class ChatBotService:IDisposable
    {
        #region Private Fields

        private readonly BotManager _botManager;

        private readonly Session _session = new Session();
        private readonly List<InteractionQueue> _queues = new List<InteractionQueue>();

        private readonly string[] _queueAttributes = new[]
        {
            InteractionAttributeName.InteractionId,
            InteractionAttributeName.RemoteName,
            InteractionAttributeName.RemoteAddress,
            InteractionAttributeName.State,
            InteractionAttributeName.StateDescription,
            InteractionAttributeName.UserQueueNames
        };

        private readonly bool _enableCommands = false;

        #endregion



        public ChatBotService()
        {
            using (Trace.Main.scope())
            {
                try
                {
                    var useWindowsAuth = false;
                    
                    // Parse some settings
                    bool.TryParse(ConfigurationManager.AppSettings["CicUseWindowsAuth"], out useWindowsAuth);
                    bool.TryParse(ConfigurationManager.AppSettings["EnableCommands"], out _enableCommands);
                    
                    // Connect IceLib
                    if (useWindowsAuth)
                    {
                        _session.Connect(new SessionSettings(),
                            new HostSettings(new HostEndpoint(ConfigurationManager.AppSettings["CicServer"])),
                            new WindowsAuthSettings(),
                            new StationlessSettings());
                    }
                    else
                    {
                        _session.Connect(new SessionSettings(),
                            new HostSettings(new HostEndpoint(ConfigurationManager.AppSettings["CicServer"])),
                            new ICAuthSettings(ConfigurationManager.AppSettings["CicUser"],
                                ConfigurationManager.AppSettings["CicPassword"]),
                            new StationlessSettings());
                    }

                    // Set up bot manager
                    _botManager = new BotManager(new ServiceProviderDelegates
                    {
                        GetAttributeMethod = GetAttribute,
                        GetAttributesMethod = GetAttributes,
                        SetAttributeMethod = SetAttribute,
                        SetAttributesMethod = SetAttributes,
                        SendChatMessageMethod = SendChatMessage,
                        ReassignChatMethod = ReassignChat
                    });
                    _botManager.LoadBots();

                    // Watch queues
                    var queueNames = ConfigurationManager.AppSettings["QueueList"].Split(new[] { '|' });
                    foreach (var queueName in queueNames)
                    {
                        try
                        {
                            Console.WriteLine("Watching queue " + queueName);
                            var queue = new InteractionQueue(InteractionsManager.GetInstance(_session),
                                new QueueId(QueueType.Workgroup, queueName));
                            queue.QueueContentsChanged += QueueOnQueueContentsChanged;
                            queue.StartWatching(_queueAttributes);
                            _queues.Add(queue);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                            Trace.Main.exception(ex);
                        }
                    }

                    // Make sure we have some queues
                    if (_queues.Count == 0)
                        throw new Exception("Not watching any queues after initialization!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Trace.Main.exception(ex);
                    throw;
                }
            }
        }



        #region Private Methods

        private void QueueOnQueueContentsChanged(object sender, QueueContentsChangedEventArgs e)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    foreach (var interaction in e.ItemsAdded)
                    {
                        try
                        {
                            // Is a chat?
                            if (!(interaction.Interaction is ChatInteraction)) continue;
                            var chat = interaction.Interaction as ChatInteraction;

                            // Assigned to user?
                            if (!string.IsNullOrEmpty(chat.UserQueueNames[0])) continue;

                            // This is new, so assign it
                            AssignChat(chat);
                        }
                        catch (Exception ex)
                        {
                            Trace.Main.exception(ex);
                        }
                    }

                    foreach (var interaction in e.ItemsChanged)
                    {
                        try
                        {
                            // Is a chat?
                            if (!(interaction.Interaction is ChatInteraction)) continue;
                            var chat = interaction.Interaction as ChatInteraction;

                            // User is assigned
                            if (interaction.InteractionAttributeNames.Contains(InteractionAttributeName.UserQueueNames) &&
                                !string.IsNullOrEmpty(chat.UserQueueNames[0]))
                            {
                                UnassignChat(chat, UnassignmentReason.AcdAssigned);
                                continue;
                            }

                            // Chat is disconnected
                            if (interaction.InteractionAttributeNames.Contains(InteractionAttributeName.State) &&
                                chat.IsDisconnected)
                            {
                                UnassignChat(chat, UnassignmentReason.Disconnected);
                                continue;
                            }

                            // Conditions for assignment
                            if (interaction.InteractionAttributeNames.Contains(InteractionAttributeName.UserQueueNames) &&
                                string.IsNullOrEmpty(chat.UserQueueNames[0]))
                            {
                                AssignChat(chat);
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.Main.exception(ex);
                        }
                    }

                    foreach (var interaction in e.ItemsRemoved)
                    {
                        try
                        {
                            // Is a chat?
                            if (!(interaction.Interaction is ChatInteraction)) continue;
                            var chat = interaction.Interaction as ChatInteraction;

                            // Unassign
                            UnassignChat(chat, UnassignmentReason.Removed);
                        }
                        catch (Exception ex)
                        {
                            Trace.Main.exception(ex);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void AssignChat(ChatInteraction chat)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    if (!_botManager.AssignBot(chat))
                    {
                        Trace.Main.warning("Chat {} was not assigned to any bot!", chat.InteractionId.Id);
                        return;
                    }

                    // Set up watches
                    chat.TextAdded += ChatOnTextAdded;
                    chat.UrlAdded += ChatOnUrlAdded;
                    chat.ChatStartWatching();
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void UnassignChat(ChatInteraction chat, UnassignmentReason reason)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Unassign
                    _botManager.UnassignBot(chat.InteractionId.Id, reason);

                    // Stop watches
                    if (chat.ChatIsWatching()) chat.ChatStopWatching();
                    if (chat.IsWatching()) chat.StopWatching();

                    // Remove event handlers
                    chat.TextAdded -= ChatOnTextAdded;
                    chat.UrlAdded -= ChatOnUrlAdded;
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void ChatOnUrlAdded(object sender, ChatUpdateEventArgs e)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // We only want to act on messages from an external party
                    if (e.ChatMember.ChatMemberType != ChatMemberType.External) return;

                    // Get a response from a bot
                    var chat = sender as ChatInteraction;
                    var response = _botManager.GetResponse(chat.InteractionId.Id,
                        new UrlChatMessage {Uri = new Uri(e.Url)});
                    if (response == null) return;
                    SendToChat(chat, response);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void ChatOnTextAdded(object sender, ChatUpdateEventArgs e)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // We only want to act on messages from an external party
                    if (e.ChatMember.ChatMemberType != ChatMemberType.External) return;
                    var chat = sender as ChatInteraction;

                    // Check for commands
                    // These are special commands, probably to be used for testing. They can be disabled via the EnableCommands parameter in the config file
                    if (_enableCommands)
                    {
                        // Return a list of bot names
                        if (e.Text.Trim().Equals("/bots", StringComparison.InvariantCultureIgnoreCase))
                        {
                            SendToChat(chat, new TextChatMessage
                            {
                                Text = "Bot names: " + _botManager.Bots.Select(bot => bot.BotName).Aggregate((a, b) => a + ", " + b)
                            });
                            return;
                        }

                        // Reassign the chat to a specific bot
                        if (e.Text.Trim().StartsWith("/bot", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ReassignChat(chat.InteractionId.Id, e.Text.Substring(4).Trim(), null);
                            return;
                        }
                    }

                    // Get a response from a bot
                    var response = _botManager.GetResponse(chat.InteractionId.Id, new TextChatMessage {Text = e.Text});
                    if (response == null) return;
                    SendToChat(chat, response);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void SendToChat(ChatInteraction chat, IChatMessage message)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Send the message to the chat
                    switch (message.MessageType)
                    {
                        case ChatMessageType.Text:
                            var text = message as TextChatMessage;
                            chat.SendText(text.Text);
                            break;
                        case ChatMessageType.Url:
                            var url = message as UrlChatMessage;
                            chat.SendUrl(url.Uri);
                            break;
                        case ChatMessageType.FilePath:
                            var filePath = message as FilePathChatMessage;
                            chat.SendFile(filePath.Path);
                            break;
                        case ChatMessageType.FileStream:
                            var fileStream = message as FileStreamChatMessage;
                            chat.SendFile(fileStream.Stream, fileStream.Filename);
                            break;
                        default:
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void SendToChat(long interactionId, IChatMessage message)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    var chat = GetChat(interactionId);
                    if (chat == null)
                        throw new Exception("Interaction " + interactionId + " was not able to be retrieved as a chat!");

                    SendToChat(chat, message);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private ChatInteraction GetChat(long interactionId)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    return
                        InteractionsManager.GetInstance(_session).CreateInteraction(new InteractionId(interactionId)) as
                            ChatInteraction;
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                    return null;
                }
            }
        }

        private string GetAttribute(long interactionId, string attributeName, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Protect against cross-bot calls
                    _botManager.ValidateBotAssignmentAndThrow(interactionId, bot);

                    // Is chat?
                    var chat = GetChat(interactionId);
                    if (chat == null)
                        throw new Exception("Interaction " + interactionId + " was not able to be retrieved as a chat!");

                    // Return attribute (try to use watched call if possible)
                    return chat.IsWatching(attributeName)
                        ? chat.GetWatchedStringAttribute(attributeName)
                        : chat.GetStringAttribute(attributeName);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                    return "";
                }
            }
        }

        private IDictionary<string, string> GetAttributes(long interactionId, IEnumerable<string> attributeNames, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Protect against cross-bot calls
                    _botManager.ValidateBotAssignmentAndThrow(interactionId, bot);

                    // Is chat?
                    var chat = GetChat(interactionId);
                    if (chat == null)
                        throw new Exception("Interaction " + interactionId + " was not able to be retrieved as a chat!");

                    // Return attributes
                    return chat.GetStringAttributes(attributeNames.ToArray());
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                    return new Dictionary<string, string>();
                }
            }
        }

        private void SetAttribute(long interactionId, string attributeName, string attributeValue, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Protect against cross-bot calls
                    _botManager.ValidateBotAssignmentAndThrow(interactionId, bot);

                    // Is chat?
                    var chat = GetChat(interactionId);
                    if (chat == null)
                        throw new Exception("Interaction " + interactionId + " was not able to be retrieved as a chat!");

                    // Set attribute
                    chat.SetStringAttribute(attributeName, attributeValue);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void SetAttributes(long interactionId, IDictionary<string, string> attributes, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Protect against cross-bot calls
                    _botManager.ValidateBotAssignmentAndThrow(interactionId, bot);

                    // Is chat?
                    var chat = GetChat(interactionId);
                    if (chat == null)
                        throw new Exception("Interaction " + interactionId + " was not able to be retrieved as a chat!");

                    // Set attributes
                    chat.SetStringAttributes(attributes);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void SendChatMessage(long interactionId, IChatMessage message, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Protect against cross-bot calls
                    _botManager.ValidateBotAssignmentAndThrow(interactionId, bot);

                    // Send message
                    SendToChat(interactionId, message);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        private void ReassignChat(long interactionId, string suggestedBotName, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    _botManager.ReassignChat(GetChat(interactionId), suggestedBotName, bot);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
            }
        }

        #endregion



        #region Public Methods

        public void Dispose()
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Unload all the bots
                    _botManager.UnloadBots();

                    // Unwatch each queue
                    foreach (var queue in _queues)
                        if (queue.IsWatching()) queue.StopWatching();

                    // Disconnect the session
                    if (_session.ConnectionState == ConnectionState.Up) _session.Disconnect();
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
