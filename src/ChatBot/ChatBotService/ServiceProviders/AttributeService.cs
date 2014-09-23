using System.Collections.Generic;
using ININ.Alliances.ChatBotInterface;

namespace ININ.Alliances.ChatBotService.ServiceProviders
{
    internal class AttributeService : IAttributeService
    {
        internal IChatBot Bot { get; set; }

        internal delegate string GetAttributeDelegate(long interactionId, string attributeName, IChatBot bot);
        internal delegate IDictionary<string, string> GetAttributesDelegate(long interactionId, IEnumerable<string> attributeNames, IChatBot bot);
        internal delegate void SetAttributeDelegate(long interactionId, string attributeName, string attributeValue, IChatBot bot);
        internal delegate void SetAttributesDelegate(long interactionId, IDictionary<string, string> attributes, IChatBot bot);

        internal GetAttributeDelegate GetAttributeMethod { get; set; }
        internal GetAttributesDelegate GetAttributesMethod { get; set; }
        internal SetAttributeDelegate SetAttributeMethod { get; set; }
        internal SetAttributesDelegate SetAttributesMethod { get; set; }





        public string GetAttribute(long interactionId, string attributeName)
        {
            return GetAttributeMethod == null 
                ? "" 
                : GetAttributeMethod(interactionId, attributeName, Bot);
        }

        public IDictionary<string, string> GetAttributes(long interactionId, IEnumerable<string> attributeNames)
        {
            return GetAttributesMethod == null
                ? new Dictionary<string, string>()
                : GetAttributesMethod(interactionId, attributeNames, Bot);
        }

        public void SetAttribute(long interactionId, string attributeName, string attributeValue)
        {
            if (SetAttributeMethod == null) return;
            SetAttributeMethod(interactionId, attributeName, attributeValue, Bot);
        }

        public void SetAttributes(long interactionId, IDictionary<string, string> attributes)
        {
            if (SetAttributesMethod == null) return;
            SetAttributesMethod(interactionId, attributes, Bot);
        }
    }
}
