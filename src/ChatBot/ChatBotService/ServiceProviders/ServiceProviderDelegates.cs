using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ININ.Alliances.ChatBotService.ServiceProviders
{
    internal struct ServiceProviderDelegates
    {
        internal AttributeService.GetAttributeDelegate GetAttributeMethod { get; set; }
        internal AttributeService.GetAttributesDelegate GetAttributesMethod { get; set; }
        internal AttributeService.SetAttributeDelegate SetAttributeMethod { get; set; }
        internal AttributeService.SetAttributesDelegate SetAttributesMethod { get; set; }

        internal ChatMessageService.SendChatMessageDelegate SendChatMessageMethod { get; set; }

        internal ChatReassignmentService.ReassignChatDelegate ReassignChatMethod { get; set; }
    }
}
