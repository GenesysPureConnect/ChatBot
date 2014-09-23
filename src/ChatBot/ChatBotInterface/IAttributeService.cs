using System.Collections.Generic;

namespace ININ.Alliances.ChatBotInterface
{
    public interface IAttributeService
    {
        #region Public Methods

        /// <summary>
        /// Gets a single attribute
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="attributeName">The name of the interaction attribute to retrieve</param>
        /// <returns>The value of the interaction attribute</returns>
        string GetAttribute(long interactionId, string attributeName);

        /// <summary>
        /// Gets a set of attributes in a single request
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="attributeNames">The names of the interaction attributes to retrieve</param>
        /// <returns>The values of the interaction attributes (IDictionary&lt;attrName, attrValue&gt;)</returns>
        IDictionary<string, string> GetAttributes(long interactionId, IEnumerable<string> attributeNames);

        /// <summary>
        /// Sets an attribute on an interaction
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="attributeName">The name of the attribute to set</param>
        /// <param name="attributeValue">The value to which the attribute will be set</param>
        void SetAttribute(long interactionId, string attributeName, string attributeValue);

        /// <summary>
        /// Sets attributes on an interaction
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="attributes">The interaction attributes to set (IDictionary&lt;attrName, attrValue&gt;)</param>
        void SetAttributes(long interactionId, IDictionary<string, string> attributes);

        #endregion
    }
}
