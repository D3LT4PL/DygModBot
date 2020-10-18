﻿using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DygBot.Models;
using DygBot.TypeReaders;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DygBot.Services.GitHubService;
using static DygBot.Modules.ModerationModule.ReactionRoleClass;
using System.Net.Http;
using System.IO;

namespace DygBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IServiceProvider _provider;
        private readonly GitHubService _git;
        private readonly LoggingService _logging;
        private readonly InteractiveService _interactive;
        private readonly HttpClient _httpClient;

        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IServiceProvider provider,
            GitHubService git,
            LoggingService logging,
            InteractiveService interactiveService,
            HttpClient httpClient)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _git = git;
            _logging = logging;
            _interactive = interactiveService;
            _httpClient = httpClient;

            _discord.MessageReceived += Discord_MessageReceived;   // Bind MessageReceived event
            _discord.JoinedGuild += Discord_JoinedGuild;   // Bind JoinedGuild event
            _discord.UserVoiceStateUpdated += Discord_UserVoiceStateUpdated;
            _discord.ReactionAdded += Discord_ReactionAdded;
            _discord.ReactionRemoved += Discord_ReactionRemoved;
            _discord.Ready += async () => await _logging.OnLogAsync(new LogMessage(LogSeverity.Info, "Discord", $"Logged in as: {_discord.CurrentUser.Username}"));

            _commands.AddTypeReader<object>(new ObjectTypeReader());
            _commands.AddTypeReader<Uri>(new UriTypeReader());
            _commands.AddTypeReader<TimeSpan>(new CustomTimeSpanTypeReader(), true);
            _commands.AddTypeReader<IEmote>(new IEmoteTypeReader());
            _commands.AddTypeReader<IMessage>(new IMessageTypeReader());
        }

        

        private async Task Discord_ReactionAdded(Cacheable<IUserMessage, ulong> userCacheable, ISocketMessageChannel socketMessageChannel, SocketReaction sockReaction)
        {
            // Return if reaction is from self
            var user = sockReaction.User.GetValueOrDefault();
            if (user.Id == _discord.CurrentUser.Id || user.IsBot)
                return;


            // Initialize used variables
            var channel = socketMessageChannel as SocketTextChannel;
            var guild = channel.Guild;
            var message = await userCacheable.GetOrDownloadAsync();

            // Check if underage was selected
            if (sockReaction.Emote.ToString() == "🚫" && channel.Id == 737304061862477834 && guild.Id == 683084560451633212)
            {
                var inviteLink = await guild.DefaultChannel.CreateInviteAsync(null, null, false, false);
                var dmChannel = await user.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle("**Szanowny użytkowniku!**")
                    .WithDescription($"Dygawka jest serwerem z zawartością nieodpowiednią dla nieletnich. Deklarując się jako osoba poniżej 18ego roku życia, Twoje konto zostało usunięte z listy dygaczy. Nie martw się, __nie zostało zbanowane__. Jeśli wybór roli *Underage* był efektem pomyłki, możesz nadal dołączyć do grona naszych użytkowników potwierdzając swoją pełnoletniość na mocy punktu nr 15 w naszym regulaminie. Jeśli zaś jesteś osobą nieletnią, zapraszamy na nasz serwer w przyszłości!\n\nMożesz dołączyć na serwer ponownie **[klikając w ten link]({inviteLink.Url})**")
                    .WithColor(new Color(0xFF277F))
                    .Build());
                await guild.GetTextChannel(708805642349051984).SendMessageAsync(embed: new EmbedBuilder()
                    .WithTitle("Osoba niepełnoletnia")
                    .WithDescription($"**{user.Username}#{user.Discriminator}** ({user.Id}) wybrał rolę *Underage*")
                    .WithColor(new Color(0xFF277F))
                    .Build());
                await message.RemoveReactionAsync(sockReaction.Emote, user);
                await (user as SocketGuildUser).KickAsync();
                return;
            }


            // Check reaction limits
            if (_git.Config.Servers[guild.Id].AllowedReactions.TryGetValue(sockReaction.Emote.ToString(), out HashSet<ulong> channels))
            {
                if (!channels.Contains(channel.Id))
                {
                    await message.RemoveReactionAsync(sockReaction.Emote, user);
                }
            }


            // Reaction roles
            if (user is SocketGuildUser member)
            {
                if (_git.Config.Servers[guild.Id].ReactionRoles.TryGetValue(channel.Id, out var rrDict))
                {
                    if (rrDict.TryGetValue(message.Id, out var reactionRoles))
                    {
                        foreach (var reaction in reactionRoles)
                        {
                            ulong roleId;
                            switch (reaction.Action)
                            {
                                case ReactionAction.GiveRemove:
                                case ReactionAction.Give:
                                    if(reaction.Roles.TryGetValue(sockReaction.Emote.ToString(), out roleId))
                                    {
                                        var role = guild.GetRole(roleId);
                                        if (role != null)
                                        {
                                            if (!member.Roles.Contains(role))
                                            {
                                                await member.AddRoleAsync(role);
                                            }
                                        }
                                    }
                                    break;

                                case ReactionAction.Remove:
                                    if (reaction.Roles.TryGetValue(sockReaction.Emote.ToString(), out roleId))
                                    {
                                        var role = guild.GetRole(roleId);
                                        if (role != null)
                                        {
                                            if (member.Roles.Contains(role))
                                            {
                                                await member.RemoveRoleAsync(role);
                                            }
                                        }
                                    }
                                    break;

                                case ReactionAction.OneOfMany:
                                    IEmote emote;
                                    foreach (var kvp in reaction.Roles)
                                    {
                                        var role = guild.GetRole(kvp.Value);
                                        if (role != null)
                                        {
                                            if (kvp.Key == sockReaction.Emote.ToString())
                                            {
                                                if (!member.Roles.Contains(role))
                                                {
                                                    await member.AddRoleAsync(role);
                                                }
                                            }
                                            else
                                            {
                                                if (member.Roles.Contains(role))
                                                {
                                                    await member.RemoveRoleAsync(role);
                                                }

                                                if (Emote.TryParse(kvp.Key, out Emote emoteTmp))
                                                    emote = emoteTmp;
                                                else
                                                    emote = new Emoji(kvp.Key);
                                                if (emote != null)
                                                    await message.RemoveReactionAsync(emote, member);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private async Task Discord_ReactionRemoved(Cacheable<IUserMessage, ulong> userCacheable, ISocketMessageChannel socketMessageChannel, SocketReaction sockReaction)
        {
            // Return if reaction is from self
            var user = sockReaction.User.GetValueOrDefault();
            if (user.Id == _discord.CurrentUser.Id || user.IsBot)
                return;


            // Initialize used variables
            var channel = socketMessageChannel as SocketTextChannel;
            var guild = channel.Guild;
            var message = await userCacheable.GetOrDownloadAsync();


            // Reaction roles
            if (user is SocketGuildUser member)
            {
                if (_git.Config.Servers[guild.Id].ReactionRoles.TryGetValue(socketMessageChannel.Id, out var rrDict))
                {
                    if (rrDict.TryGetValue(message.Id, out var reactionRoles))
                    {
                        foreach (var item in reactionRoles)
                        {
                            switch (item.Action)
                            {
                                case ReactionAction.GiveRemove:
                                case ReactionAction.OneOfMany:
                                    if (item.Roles.TryGetValue(sockReaction.Emote.ToString(), out ulong roleId))
                                    {
                                        var role = guild.GetRole(roleId);
                                        if (role != null)
                                        {
                                            if (member.Roles.Contains(role))
                                            {
                                                await member.RemoveRoleAsync(role);
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private enum VcChangeState  // Enum with states of user being in VC
        {
            None,
            Left,
            Joined,
            Moved
        }

        private async Task Discord_UserVoiceStateUpdated(SocketUser socketUser, SocketVoiceState beforeState, SocketVoiceState afterState)
        {
            if (socketUser is SocketGuildUser user)
            {
                // Work out user action
                var state = VcChangeState.None;
                if (beforeState.VoiceChannel == null && afterState.VoiceChannel != null)
                    state = VcChangeState.Joined;
                if (beforeState.VoiceChannel != null && afterState.VoiceChannel == null)
                    state = VcChangeState.Left;
                if (beforeState.VoiceChannel != null && afterState.VoiceChannel != null)
                    state = VcChangeState.Moved;


                try
                {
                    ulong roleId;

                    switch (state)
                    {
                        case VcChangeState.Joined:
                            _git.Config.Servers[afterState.VoiceChannel.Guild.Id].VcTextRole.TryGetValue(afterState.VoiceChannel.Id, out roleId); // Try get role ID for channel
                            if (roleId != default)
                            {
                                var role = afterState.VoiceChannel.Guild.GetRole(roleId);  // Get role object
                                if (role != null)
                                {
                                    await user.AddRoleAsync(role, new RequestOptions { AuditLogReason = "Joined VC" }); // Add role
                                }
                            }
                            break;

                        case VcChangeState.Left:
                            _git.Config.Servers[beforeState.VoiceChannel.Guild.Id].VcTextRole.TryGetValue(beforeState.VoiceChannel.Id, out roleId);
                            if (roleId != default)
                            {
                                var role = beforeState.VoiceChannel.Guild.GetRole(roleId);
                                if (role != null)
                                {
                                    await user.RemoveRoleAsync(role, new RequestOptions { AuditLogReason = "Left VC" });    // Remove role
                                }
                            }
                            break;

                        case VcChangeState.Moved:
                            _git.Config.Servers[beforeState.VoiceChannel.Guild.Id].VcTextRole.TryGetValue(beforeState.VoiceChannel.Id, out roleId);
                            if (roleId != default)
                            {
                                var role = beforeState.VoiceChannel.Guild.GetRole(roleId);
                                if (role != null)
                                {
                                    await user.RemoveRoleAsync(role, new RequestOptions { AuditLogReason = "Left VC" });    // Remove role
                                }
                            }

                            _git.Config.Servers[afterState.VoiceChannel.Guild.Id].VcTextRole.TryGetValue(afterState.VoiceChannel.Id, out roleId);
                            if (roleId != default)
                            {
                                var role = afterState.VoiceChannel.Guild.GetRole(roleId);
                                if (role != null)
                                {
                                    await user.AddRoleAsync(role, new RequestOptions { AuditLogReason = "Joined VC" }); // Add role
                                }
                            }

                            break;
                    }
                }
                catch (Exception) { }
            }
        }

        private async Task Discord_JoinedGuild(SocketGuild arg)
        {
            var serverConfig = new ConfigClass.ServerConfigClass();    // Create new server config object
            var guildId = arg.Id;    // Get the guild ID
            _git.Config.Servers[guildId] = serverConfig;    // Add server config to global config
            await _git.UploadConfig();  // Upload new config to GitHub
        }

        private async Task Discord_MessageReceived(SocketMessage s)
        {
            // Ensure the message is from a user/bot
            if (!(s is SocketUserMessage msg))
                return;
            if (msg.Author.Id == _discord.CurrentUser.Id)
                return;     // Ignore self when checking commands

            var context = new SocketCommandContext(_discord, msg);     // Create the command context

            if (s.Channel is SocketDMChannel)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
                 {
                     if (!context.IsPrivate)
                         return;

                     if (msg.Content.StartsWith("db!"))
                     {
                         if (msg.Content.StartsWith("db!oc"))
                         {
                             if (_discord.GetGuild(683084560451633212).GetUser(msg.Author.Id) == null)
                             {
                                 await context.Channel.SendMessageAsync("Nie jesteś członkiem Dygawki, musisz dołączyć na serwer [klikając w ten link](https://discord.gg/dygawka)");
                                 return;
                             }
                             var embed = new EmbedBuilder()
                                 .WithTitle("Wybierz docelowy kanał na swoje zdjęcie")
                                 .WithDescription("Dostępne opcje:\n• oc-males | 0\n• oc-females | 1\n• oc-others | 2")
                                 .WithColor(new Color(0xFF277F))
                                 .WithFooter(footer =>
                                 {
                                     footer.WithText("Krok 1/2 (wyślij 'cancel', aby anulować)");
                                 })
                                 .Build();
                             var embedMsg = await context.Channel.SendMessageAsync(embed: embed);

                             var response = await _interactive.NextMessageAsync(context, timeout: TimeSpan.FromMinutes(1));
                             if (response == null)
                             {
                                 await embedMsg.DeleteAsync();
                                 await context.Channel.SendMessageAsync("Czas minął, spróbuj jeszcze raz");
                                 return;
                             }
                             else if (response.Content == "cancel")
                             {
                                 await embedMsg.DeleteAsync();
                                 await context.Channel.SendMessageAsync("Wysyłanie zdjęcia anulowane");
                                 return;
                             }
                             var channel = response.Content == "oc-males" || response.Content == "0" ? 683718299778940944 : (response.Content == "oc-females" || response.Content == "1" ? 683718257835900964 : (response.Content == "oc-others" || response.Content == "2" ? 734707341282246706 : -1));
                             if (channel == -1)
                             {
                                 await embedMsg.DeleteAsync();
                                 await context.Channel.SendMessageAsync("Zły kanał");
                                 return;
                             }

                             embed = new EmbedBuilder()
                                 .WithTitle("Wyślij zdjęcie i opcjonalny opis")
                                 .WithDescription("Wyślij tylko jedno zdjęcie w załączniku. Do zdjęcia możesz dodać wiadomość, która zostanie dołączona do zdjęcia na kanale. Jeśli nie chcesz opisu, nic nie pisz.")
                                 .WithColor(new Color(0xFF277F))
                                 .WithFooter(footer =>
                                 {
                                     footer.WithText("Krok 2/2 (wyślij 'cancel', aby anulować)");
                                 })
                                 .Build();
                             embedMsg.ModifyAsync((x) =>
                             {
                                 x.Embed = embed;
                             });
                             response = await _interactive.NextMessageAsync(context, timeout: TimeSpan.FromMinutes(1));
                             if (response == null)
                             {
                                 await embedMsg.DeleteAsync();
                                 await context.Channel.SendMessageAsync("Czas minął, spróbuj jeszcze raz");
                                 return;
                             }
                             else if (response.Content == "cancel")
                             {
                                 await embedMsg.DeleteAsync();
                                 await context.Channel.SendMessageAsync("Wysyłanie zdjęcia anulowane");
                                 return;
                             }
                             else if (response.Attachments.Count != 1)
                             {
                                 await embedMsg.DeleteAsync();
                                 await context.Channel.SendMessageAsync($"Anulowano. Otrzymano zdjęć: {response.Attachments.Count} - oczekiwano: 1");
                                 return;
                             }
                             var description = response.Content;

                             var extension = response.Attachments.ElementAt(0).Filename.Split('.').Last();

                             var imgStream = await (await _httpClient.GetAsync(response.Attachments.ElementAt(0).Url)).Content.ReadAsStreamAsync();

                             await _discord.GetGuild(683084560451633212).GetTextChannel((ulong)channel).SendFileAsync(imgStream, $"anonymous-oc.{extension}", description);

                             await embedMsg.DeleteAsync();
                             await context.Channel.SendMessageAsync("Wysłano zdjęcie");

                             var logEmbed = new EmbedBuilder()
                                .WithTitle("Anonimowe zdjęcie na OC")
                                .WithDescription("======================")
                                .WithColor(new Color(0xFF277F))
                                .WithThumbnailUrl(msg.Author.GetAvatarUrl())
                                .WithImageUrl(response.Attachments.ElementAt(0).Url)
                                .AddField("Użytkownik", $"{msg.Author.Mention} ({msg.Author.Id})")
                                .AddField("Opis", $"{(string.IsNullOrWhiteSpace(description) ? "(brak)" : description)}")
                                .AddField("Kanał", $"{_discord.GetGuild(683084560451633212).GetTextChannel((ulong)channel).Mention}")
                                .Build();

                             await _discord.GetGuild(683084560451633212).GetTextChannel(722415107576954890).SendMessageAsync(embed: logEmbed);
                         }
                     }
                 });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                var guildId = context.Guild.Id;

                if (_git.Config.Servers[guildId].AutoReact.ContainsKey(context.Channel.Id))
                {
                    if (string.IsNullOrWhiteSpace(context.Message.Content))
                    {
                        await context.Message.DeleteAsync(new RequestOptions { AuditLogReason = "Wiadomość bez podpisu" });
                        await _interactive.ReplyAndDeleteAsync(context, "Twoja wiadomość nie zawiera podpisu", timeout: TimeSpan.FromSeconds(3));
                        return;
                    }
                }

                if (_git.Config.Servers[guildId].AutoReact.ContainsKey(context.Channel.Id))  // Check if channel is set to be auto reacted in
                {
                    var emotesString = _git.Config.Servers[guildId].AutoReact[context.Channel.Id];   // Get strings of emotes
                    List<IEmote> emotes = new List<IEmote>(emotesString.Count); // Create a list of emotes

                    // Parse emotes
                    foreach (var text in emotesString)
                    {
                        IEmote emote;
                        try
                        {
                            emote = Emote.Parse(text);
                        }
                        catch (Exception)
                        {
                            emote = new Emoji(text);
                        }
                        emotes.Add(emote);
                    }
                    await msg.AddReactionsAsync(emotes.ToArray());  // React with emotes
                }
                string prefix = _git.Config.Servers[guildId]?.Prefix ?? "db!";

                int argPos = 0;     // Check if the message has a valid command prefix
                if (msg.HasStringPrefix(prefix, ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
                {
                    bool executeCommand = false;
                    var commandStr = msg.Content.Split(' ')[0].Substring(argPos);
                    var command = _commands.Commands.FirstOrDefault(x => x.Aliases.Contains(commandStr));
                    if (command != null)
                    {
                        commandStr = command.Name;
                    }
                    if (_git.Config.Servers[guildId].CommandLimit.ContainsKey(commandStr))
                    {
                        if (_git.Config.Servers[guildId].CommandLimit[commandStr].Contains(context.Channel.Id))
                        {
                            executeCommand = true;
                        }
                        else
                            executeCommand = false;
                    }
                    else
                        executeCommand = true;


                    if (executeCommand)
                    {
                        var result = await _commands.ExecuteAsync(context, argPos, _provider);     // Execute the command

                        if (!result.IsSuccess)
                        {
                            switch (result.Error)
                            {
                                case CommandError.BadArgCount:
                                    await context.Channel.SendMessageAsync("Zła ilość argumentów");
                                    break;
                                case CommandError.UnmetPrecondition:
                                    if (result.ErrorReason == "Module precondition group Permission failed")
                                        await context.Channel.SendMessageAsync("Nie spełniasz wymogów polecenia - nie masz wymaganych uprawnień");
                                    else
                                        await context.Channel.SendMessageAsync("Nie spełniasz wymogów polecenia");
                                    break;
                                case CommandError.Unsuccessful:
                                case CommandError.Exception:
                                case CommandError.ParseFailed:
                                    await context.Channel.SendMessageAsync("Miałem problem z tym poleceniem");
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}
