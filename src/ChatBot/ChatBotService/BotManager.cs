using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ININ.Alliances.ChatBotInterface;
using ININ.Alliances.ChatBotInterface.ChatMessages;
using ININ.Alliances.ChatBotService.ServiceProviders;
using ININ.IceLib.Interactions;

namespace ININ.Alliances.ChatBotService
{
    internal class BotManager
    {
        #region Private Fields

        private readonly Dictionary<long, IChatBot> _interactionAssignments = new Dictionary<long, IChatBot>();
        private readonly ServiceProviderDelegates _delegates;

        #endregion



        #region Internal Properties

        internal List<IChatBot> Bots { get; set; }

        #endregion



        internal BotManager(ServiceProviderDelegates delegates)
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    Bots = new List<IChatBot>();
                    _delegates = delegates;
                    AppDomain.CurrentDomain.AssemblyResolve += CurrentDomainOnAssemblyResolve;
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
            }
        }



        #region Private Methods

        private Assembly CurrentDomainOnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    // If an assembly needs to be loaded and we know who's asking (should be a bot's assembly), try and load it from anywhere in that path
                    if (args.RequestingAssembly == null) return null;
                    var assemblyPath = Path.GetDirectoryName(args.RequestingAssembly.ManifestModule.FullyQualifiedName);
                    var assemblyName = args.Name.Split(new[] { ',' })[0].Trim();
                    var files = Directory.GetFiles(assemblyPath, assemblyName + "*.dll");
                    return files.Length == 0
                        ? null
                        : Assembly.LoadFile(files[0]);
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                    return null;
                }
            }
        }

        private bool AssignBot(ChatInteraction interaction, bool prettyPlease)
        {
            foreach (var bot in Bots)
            {
                try
                {
                    var attributes = interaction.GetStringAttributes(bot.RequestedAttributes.ToArray());
                    if (!bot.ClaimInteraction(interaction.InteractionId.Id, attributes, prettyPlease)) continue;
                    DoAssignBot(interaction, bot, attributes);
                    return true;
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex, "Exception from ClaimInteraction from bot {}", bot.BotName);
                }
            }
            return false;
        }

        private void DoAssignBot(ChatInteraction chat, IChatBot bot, IDictionary<string, string> attributes)
        {
            Trace.BotManager.status("Assigning interaction {} to bot {}", chat.InteractionId.Id, bot.BotName);
            Console.WriteLine("Assigning interaction {0} to bot {1}", chat.InteractionId.Id, bot.BotName);
            _interactionAssignments[chat.InteractionId.Id] = bot;
            try
            {
                bot.InteractionAssigned(chat.InteractionId.Id, attributes);
            }
            catch (Exception ex)
            {
                Trace.BotManager.exception(ex);
            }
        }

        #endregion



        #region Internal Methods

        internal void LoadBots()
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    // Make path to bots folder
                    var botPath = Path.Combine(Environment.CurrentDirectory, "bots");

                    // Get list of files
                    Trace.BotManager.note("Bots path={}", botPath);

                    // See if the directory exists
                    if (!Directory.Exists(botPath))
                    {
                        Trace.BotManager.warning("Bots directory does not exist! Unable to load any bots!");
                        return;
                    }

                    var potentialFiles = Directory.GetFiles(botPath, "*.dll", SearchOption.AllDirectories);
                    Trace.BotManager.note("Found {} files", potentialFiles.Length);

                    foreach (string potentialBotPath in potentialFiles)
                    {
                        try
                        {
                            using (Trace.BotManager.scope("Loading {}", potentialBotPath))
                            {
                                Trace.BotManager.status("Attempting to load {}", potentialBotPath);

                                // Load the DLL
                                var loadedAssembly = Assembly.LoadFile(potentialBotPath);
                                if (loadedAssembly != null)
                                {
                                    Trace.BotManager.note("Assembly loaded successfully!");

                                    // Get all the public types in the assembly
                                    var typesInAssembly = loadedAssembly.GetTypes();
                                    Trace.BotManager.note("Found {} types in the assembly: ", typesInAssembly.Length,
                                        typesInAssembly.Select(t => t.FullName).Aggregate((a, b) => a + ", " + b));

                                    // Iterate through the types
                                    foreach (var type in typesInAssembly)
                                    {
                                        // See what interfaces are implemented by this type
                                        var interfaceTypes = type.GetInterfaces();
                                        foreach (var interfaceType in interfaceTypes)
                                        {
                                            // Does the type in the assembly implement our interface?
                                            if (interfaceType == typeof (IChatBot))
                                            {
                                                try
                                                {
                                                    // Create instance of bot
                                                    Trace.BotManager.status(
                                                        "Type {} implements IChatBot, creating instance...",
                                                        type.ToString());
                                                    var tempBot = (IChatBot) Activator.CreateInstance(type);

                                                    // Initialize bot
                                                    tempBot.OnLoad(new ChatBotServiceProvider(tempBot, _delegates));

                                                    // Add bot to list
                                                    Bots.Add(tempBot);
                                                    Trace.BotManager.status("Instance of {} created successfully!",
                                                        type.ToString());
                                                }
                                                catch (Exception ex)
                                                {
                                                    Trace.BotManager.exception(ex);
                                                }
                                            }
                                            else
                                            {
                                                Trace.BotManager.verbose("Type {} does not implement IChatBot.",
                                                    type.ToString());
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Trace.BotManager.status("Unable to load the assembly at {}", potentialBotPath);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.BotManager.exception(ex, "Exception caught trying to load bot! Exception={}",
                                ex.Message);
                        }
                    }

                    // Sort bots by priority
                    Bots = new List<IChatBot>(Bots.OrderByDescending(bot => bot.Priority));
                    var orderedBots =
                        Bots.Select(bot => string.Format("[{0}] {1}", bot.Priority, bot.BotName))
                            .Aggregate((a, b) => a + Environment.NewLine + b);
                    Trace.BotManager.always("Sorted bot order:\n{}", orderedBots);
                    Console.WriteLine("Sorted bot order:\n{0}", orderedBots);

                    Trace.BotManager.status("Done loading assemblies. {} assemblies were loaded.", Bots.Count);
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
            }
        }

        internal void UnloadBots()
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    foreach (var bot in Bots)
                    {
                        using (Trace.BotManager.scope("Calling Unload for {} ({})", bot.BotName, bot.GetType()))
                        {
                            try
                            {
                                bot.OnUnload();
                                bot.Dispose();
                                Trace.BotManager.status("Bot unloaded");
                            }
                            catch (Exception ex)
                            {
                                Trace.BotManager.exception(ex, "Exception caught unloading bot: {}", ex.Message);
                            }
                        }
                    }

                    Trace.BotManager.status("All bots unloaded.");
                    Bots.Clear();
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
            }
        }

        internal bool AssignBot(ChatInteraction interaction)
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    // Assign first pass
                    if (AssignBot(interaction, false)) return true;

                    /* Beg someone to take the interaction --
                     * The idea behind this is that a bot can choose not to take the chat on the first pass 
                     * if the chat doesn't meet its criteria for selection. On the second pass, we're asking 
                     * each bot to reconsider and take the chat unless it can't possibly service that 
                     * chat. If the second pass fails, the chat will remain in queue.
                     */
                    if (AssignBot(interaction, true)) return true;

                    Trace.BotManager.error("Failed to assign interaction {} to any bot", interaction.InteractionId.Id);
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
                return false;
            }
        }

        internal void UnassignBot(long interactionId, UnassignmentReason reason)
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    Console.WriteLine("UnassignBot ({0})", interactionId);

                    // Unassign bot
                    IChatBot assignedBot = null;
                    if (_interactionAssignments.ContainsKey(interactionId))
                    {
                        assignedBot = _interactionAssignments[interactionId];
                        _interactionAssignments.Remove(interactionId);
                    }

                    // Let the bot know the bad news
                    if (assignedBot != null)
                    {
                        assignedBot.InteractionUnassigned(interactionId, reason);
                    }
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
            }
        }

        internal void ReassignChat(ChatInteraction chat, string suggestedBotName, IChatBot bot)
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    if (chat == null)
                        throw new Exception("Unable to reassign null chat");

                    ValidateBotAssignmentAndThrow(chat.InteractionId.Id, bot);

                    Console.WriteLine("ReassignChat ({0})");

                    var targetBot =
                        Bots.FirstOrDefault(
                            b => b.BotName.Equals(suggestedBotName.Trim(), StringComparison.InvariantCultureIgnoreCase));
                    if (targetBot != null)
                    {
                        var attributes = chat.GetStringAttributes(targetBot.RequestedAttributes.ToArray());

                        // Found selected bot, see if it will claim the chat
                        if (targetBot.ClaimInteraction(chat.InteractionId.Id, attributes, true))
                        {
                            UnassignBot(chat.InteractionId.Id, UnassignmentReason.Reassigned);
                            DoAssignBot(chat, targetBot, attributes);
                        }
                    }

                    // Try to assign the bot if it's not assigned to the suggested bot
                    if (targetBot==null || !ValidateBotAssignment(chat.InteractionId.Id, targetBot))
                        AssignBot(chat);
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
            }
        }

        internal IChatMessage GetResponse(long interactionId, IChatMessage message)
        {
            using (Trace.BotManager.scope())
            {
                try
                {
                    return _interactionAssignments.ContainsKey(interactionId)
                        ? _interactionAssignments[interactionId].GetResponse(interactionId, message)
                        : null;
                }
                catch (Exception ex)
                {
                    Trace.BotManager.exception(ex);
                }
                return null;
            }
        }

        internal bool ValidateBotAssignment(long interactionId, IChatBot bot)
        {
            using (Trace.Main.scope())
            {
                try
                {
                    // Bypass for internal commands; this will never be null when a bot's command gets here
                    if (bot == null) return true;

                    return _interactionAssignments.ContainsKey(interactionId) &&
                           _interactionAssignments[interactionId].Equals(bot);
                }
                catch (Exception ex)
                {
                    Trace.Main.exception(ex);
                }
                return false;
            }
        }

        /// <summary>
        /// Throws an exception if the bot is not assigned to the given interaction ID
        /// </summary>
        /// <param name="interactionId">The interaction ID</param>
        /// <param name="bot">The bot</param>
        internal void ValidateBotAssignmentAndThrow(long interactionId, IChatBot bot)
        {
            if (!ValidateBotAssignment(interactionId, bot))
                throw new Exception("Interaction " + interactionId + " is not assigned to " + bot.BotName);
        }

        #endregion
    }
}
