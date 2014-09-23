using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Text;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.TranslatorBot
{
    public class TranslatorBot : IChatBot
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

        private Languages _language = Languages.None;



        #region IChatBot Members

        public string BotName { get { return "Translator Bot"; } }
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
                new TextChatMessage { Text = "Welcome to the translator bot! Say: \"/setlang <language>\" to set the translation language. Available languages: PigLatin, binary" });
        }

        public IChatMessage GetResponse(long interactionId, IChatMessage message)
        {
            using (_trace.scope())
            {
                try
                {
                    Console.WriteLine("[{0}] - GetResponse ({1})", BotName, interactionId);

                    var textMessage = message as TextChatMessage;
                    if (textMessage == null) return null;
                    
                    // Set language
                    if (textMessage.Text.StartsWith("/setlang", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var parts = textMessage.Text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            // This would be really cool if the google translate api was implemented here. But it costs money. Going to have to settle for pig latin for now... https://cloud.google.com/translate/
                            if (parts[1].Equals("piglatin", StringComparison.InvariantCultureIgnoreCase))
                            {
                                _language = Languages.PigLatin;
                                return new TextChatMessage
                                {
                                    Text = "Language set to Pig Latin. Say something to have it translated."
                                };
                            }
                            if (parts[1].Equals("binary", StringComparison.InvariantCultureIgnoreCase))
                            {
                                _language = Languages.Binary;
                                return new TextChatMessage
                                {
                                    Text = "Language set to Binary. Say something to have it translated."
                                };
                            }
                            else
                            {
                                return new TextChatMessage
                                {
                                    Text = "Sorry, I can't translate to " + parts[1]
                                };
                            }
                        }
                    }

                    // Translate
                    switch (_language)
                    {
                        case Languages.PigLatin:
                            return new TextChatMessage {Text = ToPigLatin(textMessage.Text)};
                            break;
                        case Languages.Binary:
                            return new TextChatMessage {Text = ToBinaryString(textMessage.Text)};
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (Exception ex)
                {
                    _trace.exception(ex);
                    return new TextChatMessage { Text = "A failure was encountered while translating the message" };
                }
            }
        }

        private string ToBinaryString(string text)
        {
            var sb = new StringBuilder();
            foreach (var ch in text)
            {
                sb.Append(Convert.ToString(ch, 2));
            }
            return sb.ToString();
        }

        private string ToPigLatin(string text)
        {
            // Note that this logic isn't perfect, but this example is just for fun

            var sb = new StringBuilder();
            var words = text.Split(new[] {' '});
            foreach (var word in words)
            {
                var thisWord = word.Trim();

                // Find out if the last character is punctuation
                var lastChar = thisWord.Substring(thisWord.Length - 1);
                var endsWithPunctuation = ".?!,;".Contains(lastChar);
                
                // Strip punctuation
                if (endsWithPunctuation)
                    thisWord = thisWord.Substring(0, thisWord.Length - 1);

                // Translate to pig latin!
                if (IsVowel(thisWord.ToCharArray()[0]))
                {
                    thisWord = thisWord.Substring(1) + thisWord.Substring(0, 1) + "way";
                }
                else
                {
                    thisWord = thisWord.Substring(1) + thisWord.Substring(0, 1) + "ay";
                }

                // Re-add punctuation
                if (endsWithPunctuation)
                    thisWord += lastChar;

                // Add to string builder
                sb.Append(thisWord);
                sb.Append(" ");
            }

            return sb.ToString();
        }

        public void InteractionUnassigned(long interactionId, UnassignmentReason reason)
        {
            Console.WriteLine("[{0}] - InteractionUnassigned ({1}, {2})", BotName, interactionId, reason);
        }

        #endregion



        private bool IsVowel(char c)
        {
            return "aeiouAEIOU".IndexOf(c) >= 0;
        }

        public void Dispose()
        {

        }
    }
}
