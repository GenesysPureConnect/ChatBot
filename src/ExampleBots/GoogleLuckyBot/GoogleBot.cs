using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Web;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;

namespace ININ.Alliances.GoogleLuckyBot
{
    public class GoogleBot : IChatBot
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

        public string BotName { get { return "Google Bot"; } }
        public IEnumerable<string> RequestedAttributes { get { return _requestedAttributes; } }
        public ushort Priority { get { return 100; } }



        public void OnLoad(IServiceProvider serviceProvider)
        {
            try
            {
                _trace = serviceProvider.GetService(typeof(TopicTracer)) as TopicTracer;
                _attributeService = serviceProvider.GetService(typeof(IAttributeService)) as IAttributeService;
                _chatMessageService = serviceProvider.GetService(typeof(IChatMessageService)) as IChatMessageService;
                _chatReassignmentService = serviceProvider.GetService(typeof(IChatReassignmentService)) as IChatReassignmentService;

                ServicePointManager.Expect100Continue = false;
                ServicePointManager.DefaultConnectionLimit = 2000;
                WebRequest.DefaultWebProxy = null;

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
                new TextChatMessage {Text = "Welcome to the google bot! Ask me anything!"});
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

                    var page = "http://www.google.com/search?q=" + HttpUtility.UrlEncode(textMessage.Text) + "&btnI=1";
                    return new UrlChatMessage {Uri = new Uri(GetResponseUrl(page))};
                }
                catch (Exception ex)
                {
                    _trace.exception(ex);
                }
                return null;
            }
        }

        public void InteractionUnassigned(long interactionId, UnassignmentReason reason)
        {
            Console.WriteLine("[{0}] - InteractionUnassigned ({1}, {2})", BotName, interactionId, reason);
        }

        #endregion



        private string GetResponseUrl(string url)
        {
            Console.WriteLine("[" + BotName + "] - fetching {0}", url);
            var sw = new Stopwatch();
            sw.Start();
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = null;
            request.AllowAutoRedirect = false;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                sw.Stop();
                Console.WriteLine("[" + BotName + "] - got response in {0} seconds", sw.Elapsed.TotalSeconds);
                response.Close();

                return response.StatusCode == HttpStatusCode.Redirect
                    ? GetResponseUrl(response.Headers["Location"])
                    : response.ResponseUri.AbsoluteUri;
            }
        }


        public void Dispose()
        {

        }
    }
}
