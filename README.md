Chat Bot
========

The Chat Bot is an IceLib example application to allow a 'bot', denoting a computer program or robot with a very specific function, to interact with a CIC web chat user. The basic premise of a chat bot is that the human user types a message into the chat, the bot reads that message, and then responds with an appropriate response.


Chat Bot Serivce Functionality
------------------------------

The chat bot service does not directly provide any bot functionality, but instead dynamically loads bots and assigns a bot to each chat it monitors. A bot is seleceted when the chat is assigned to a workgroup queue and the bot is unassigned when the chat is assigned to a user, disconnected (abandon), or leaves the workgroup queue for any reason. The code could easily be modified to allow a bot to be assigned while a customer and agent are joined to the chat (like the translation use case below).


### Queue Monitoring

The chat bot service monitors for chats in queues defined via the config file. The _QueueList_ setting defines the pipe-seperated names of workgroup queues to be monitored. The service will monitor all of these queues for new chats and will assign a bot to each new chat that comes into any of the queues.


### Bot Assignment

Each bot provides its priority via `IChatBot.Priority` (ushort - 0 to 65,535), with a higher number being a higher priority. When a new chat arrives on a monitored queue, the chat bot service will invoke the `IChatBot.ClaimInteraction(...)` method for each bot, in order of highest priority to lowest, until a bot claims the interaction or all bots have refused the interaction. If no bots claim the interaction on the first pass, a second pass will be made to ask each bot to reconsider. This allows the first pass to find a bot that is best suited for the chat, based on the bot's internal criteria, but allow a less-suitable bot to be used if a first choice was not found.

Once a bot is selected, meaning a bot returns true from `IChatBot.ClaimInteraction(...)`, the chat bot service will invoke `IChatBot.InteractionAssigned(...)` for the bot to let it know that it has successfully been assigned to the chat.

When the chat is assigned to a CIC agent, the user abandons, or the chat is removed from the workgroup queue for any reason, the chat is unassigned from the bot and `IChatBot.InteractionUnassigned(...)` is invoked to inform the bot that it is no longer assigned to that interaction (so it can clean up if it was keeping track) and also provides the reason for unassignment.

_Note that this assignment process does not involve actually assigning the chat to any CIC user or queue, but is only an internal process within the chat bot service._


Example Bots
============

All bots are dynamically loaded by the Chat Bot Service using .NET reflection. The compiled DLL for each bot should be placed in the _bots_ directory with the service executable. For example, if the service is installed to C:\chatbot\, the bots should be placed in C:\chatbot\bots\ or any subfolder within that path. For a bot to be loaded, it must expose a public class that implements the `IChatBot` interface.


[Time Bot]
----------

Thankfully, this bot is not the T-800 variety of time bot, but a simple program that will respond with the current time. The human user can say anything, and the bot will say: _The current time is {time}_.

The purpose of this bot is to illustrate the most basic concept of of a bot, which is to respond with a simple message.


[Attribute Bot]
---------------

The Attribute Bot is a sample bot that illustrates the use of the `IAttributeService` interface. This bot allows the human user to get the value of an attribute (or attributes) by inputting the pipe seperated list of attributes to retrieve. It also allows the user to set the value of an attribute by inputting the name and desired value, such as _Eic\_RemoteName=Roland Orzabal_. This exact use case isn't realistic for an actual implementation, but illustrates that a bot can get and set attributes on an interaction as necessary.


[Google Lucky Bot]
------------------

The Google Lucky Bot is an example of using an external source for obtaining information to use when responding to a message. The bot will take in the input from the user, search google for the query, and return the link for the "I'm feeling lucky" result.


[Translator Bot]
----------------

The Translator Bot is an example of potentially useful functionality for the chat bot. The bot allows the human user to specify the language to which they want the bot to translate to, and then translates any input into that language. Currently, the only translation use cases that exist are for pigs that can use computers and computers themselves, so the only implemented languages are pig latin and binary. 

A potential use case for this bot would be to translate human languages during a chat interaction using [Google's Translate API]. If a Spanish speaking customer is chatting with an English speaking agent, the translator bot could translate the customer's Spanish text into English and the agent's English text into Spanish.


Chat Bot Services
=================

The `IChatBot` interface provides an `IServiceProvider` object in the OnLoad method. The service provider supports the following services:


TopicTracer
-----------

This will return a reference to an ININ trace topic that can be used to log messages to the Chat Bot Service's ININ trace log.


IAttributeService
-----------------

This service provides an interface to allow a bot to get and set attributes on an interaction. It supports getting and setting either single attributes or collections of attributes.


IChatMessageService
-------------------

This service provides an interface to allow a bot to send a chat message to a chat. This would be used outside of the request/response pattern for chat messages used by `IChatBot.GetResponse(...)`. 


IChatReassignmentService
------------------------

This service provides an interface to allow a bot to request that the chat be assigned to a different bot. If a bot name is specified in the call to `IChatReassignmentService.ReassignChat(...)`, the Chat Bot Service will attempt to assign the chat to a bot with the matching name before simply retrying the default assignment logic.



[Google's Translate API]:https://cloud.google.com/translate/
[Attribute Bot]:https://github.com/InteractiveIntelligence/ChatBot/blob/master/src/ExampleBots/AttributeBot/AttributeBot.cs
[Google Lucky Bot]:https://github.com/InteractiveIntelligence/ChatBot/blob/master/src/ExampleBots/GoogleLuckyBot/GoogleBot.cs
[Time Bot]:https://github.com/InteractiveIntelligence/ChatBot/blob/master/src/ExampleBots/TimeBot/TimeBot.cs
[Translator Bot]:https://github.com/InteractiveIntelligence/ChatBot/blob/master/src/ExampleBots/TranslatorBot/TranslatorBot.cs
