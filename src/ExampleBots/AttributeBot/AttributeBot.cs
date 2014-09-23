using System;
using System.Collections.Generic;
using System.Linq;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.AttributeBot
{
    public class AttributeBot : IChatBot
    {
        private readonly List<string> _requestedAttributes = new List<string>(
            new[]
            {
                "Eic_RemoteName",
                "Eic_RemoteNumber"
            });

        private TopicTracer _trace;
        private IAttributeService _attributeService;
        private IChatMessageService _chatMessageService;
        private IChatReassignmentService _chatReassignmentService;



        #region IChatBot Members

        public string BotName { get { return "Attribute Bot"; } }
        public IEnumerable<string> RequestedAttributes { get { return _requestedAttributes; } }
        public ushort Priority { get { return 0; } }


        public void OnLoad(IServiceProvider serviceProvider)
        {
            try
            {
                _trace = serviceProvider.GetService(typeof(TopicTracer)) as TopicTracer;
                _attributeService = serviceProvider.GetService(typeof(IAttributeService)) as IAttributeService;
                _chatMessageService = serviceProvider.GetService(typeof(IChatMessageService)) as IChatMessageService;
                _chatReassignmentService = serviceProvider.GetService(typeof(IChatReassignmentService)) as IChatReassignmentService;

                Console.WriteLine("[{0}] - OnLoad", BotName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void OnUnload()
        {
            Console.WriteLine("[{0}] - OnUnload", BotName);
        }

        public bool ClaimInteraction(long interactionId, IDictionary<string, string> interactionAttributes, bool prettyPlease)
        {
            Console.WriteLine("[{0}] - ClaimInteraction ({1})", BotName, interactionId);

            return true;
        }

        public void InteractionAssigned(long interactionId, IDictionary<string, string> interactionAttributes)
        {
            Console.WriteLine("[{0}] - InteractionAssigned ({1})", BotName, interactionId);

            _chatMessageService.SendChatMessage(interactionId,
                new TextChatMessage
                {
                    Text = "Welcome to the attribute bot! " +
                           "Say an attribute name and I will return the value. Use a pipe seperated list for multiples!. " +
                           "You can also assign attributes by saying attr_name=value."
                });
        }

        public IChatMessage GetResponse(long interactionId, IChatMessage message)
        {
            using (_trace.scope())
            {
                try
                {
                    /* FAIR WARNING
                     * This bot is a fairly bad idea to actually expose to users. It's meant as a testing 
                     * bot to test the get/set attribute functionality. Set system attributes at your own risk.
                     */
                    Console.WriteLine("[{0}] - GetResponse ({1})", BotName, interactionId);

                    var textMessage = message as TextChatMessage;
                    if (textMessage == null) return null;

                    var pieces = textMessage.Text.Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                    if (pieces.Length == 0) return null;

                    // Check for set
                    if (pieces.Length == 1)
                    {
                        var setData = pieces[0].Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                        if (setData.Length == 2)
                        {
                            _attributeService.SetAttribute(interactionId, setData[0], setData[1]);
                            return new TextChatMessage {Text = "Attribute set!"};
                        }

                        // Get attribute
                        var attr = _attributeService.GetAttribute(interactionId, pieces[0]);
                        return new TextChatMessage { Text = pieces[0] + "=" + attr };
                    }

                    // Get attributes
                    var attrs = _attributeService.GetAttributes(interactionId, pieces);
                    return new TextChatMessage
                    {
                        Text =
                            attrs.Select(a => a.Key + "=" + a.Value)
                                .Aggregate((a, b) => a + Environment.NewLine + b)
                    };
                }
                catch (Exception ex)
                {
                    _trace.exception(ex);
                    return new TextChatMessage { Text = "I'm sorry. I failed. Will you give me another chance?" };
                }
            }
        }

        public void InteractionUnassigned(long interactionId, UnassignmentReason reason)
        {
            Console.WriteLine("[{0}] - InteractionUnassigned ({1}, {2})", BotName, interactionId, reason);
        }

        #endregion



        public void Dispose()
        {

        }
    }
}
