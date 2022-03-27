using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

using Discord;
using Discord.WebSocket;
using System.Collections.Immutable;

namespace GabePlus
{
    class Program
    {
		public static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

        string TokenFile = "token.txt";
        string UsersFile = "users.txt";

		private DiscordSocketClient _client;
        private DiscordSocketConfig _socketConfig;
        private Discord.Rest.DiscordRestClient _restClient;
        Dictionary<ulong, DataUser> Users = new Dictionary<ulong, DataUser>();

        public async Task MainAsync()
		{

            //Client and log
            _socketConfig = new DiscordSocketConfig();
            _socketConfig.AlwaysDownloadUsers = true;

            _client = new DiscordSocketClient(_socketConfig);
            _client.Log += Log;

            _restClient = new Discord.Rest.DiscordRestClient();
            _restClient.Log += Log;

            //Bot Token
            var token = await File.ReadAllTextAsync(TokenFile);

            //Login
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await _restClient.LoginAsync(TokenType.Bot, token);

            openUsers(UsersFile, out Users);

            //Events
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
            _client.MessageReceived += MessageRecieved;
            _client.UserVoiceStateUpdated += UserVoiceStateUpdated;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }
        
        //Log
		private Task Log(LogMessage msg)
        {
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
        }

        private void openUsers(string fileName, out Dictionary<ulong, DataUser> users)
        {
            users = new Dictionary<ulong, DataUser>();
            string[] array = File.ReadAllLines(fileName);
            Console.WriteLine($"Opening users.");
            foreach (string fullitem in array)
            {
                string[] splititem = fullitem.Split(", ");
                users.Add(Convert.ToUInt64(splititem[0]), new DataUser(Convert.ToUInt64(splititem[0]), splititem[1]));
            }
            Console.WriteLine("Finish.");
        }

        private void saveUsers(string fileName, Dictionary<ulong, DataUser> users)
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<ulong, DataUser> kvp in users)
            {
                list.Add($"{kvp.Key}, {kvp.Value.UserName}");
            }
            File.WriteAllLines(fileName, list);
        }


        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reactionChanged)
        {
            var message = await cacheable.GetOrDownloadAsync();
            if (reactionChanged.UserId == message.Author.Id)
            {
                return;
            }
            if(!Users.ContainsKey(message.Author.Id))
            {
                Users.Add(message.Author.Id, new DataUser(message.Author.Id, message.Author.Username));
            }
            
            Users[message.Author.Id].addEmote(reactionChanged.Emote.ToString());
            Users[message.Author.Id].save();
            saveUsers(UsersFile, Users);
        }
        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cacheable, ISocketMessageChannel channel, SocketReaction reactionChanged)
        {
            var message = await cacheable.GetOrDownloadAsync();
            if (reactionChanged.UserId == message.Author.Id)
            {
                return;
            }
            if (!Users.ContainsKey(message.Author.Id))
            {
                Users.Add(message.Author.Id, new DataUser(message.Author.Id, message.Author.Username));
            }

            Users[message.Author.Id].subtractEmote(reactionChanged.Emote.ToString());
            Users[message.Author.Id].save();
            saveUsers(UsersFile, Users);
        }


        private async Task MessageRecieved(SocketMessage message)
        {
            //Karma Command
            if (message.Content.StartsWith("k!"))
            {
                if (message.MentionedUsers.Count == 1)
                {
                    SocketUser targetUser = message.MentionedUsers.ToImmutableList<SocketUser>()[0];
                    ulong target = targetUser.Id;
                    if (Users.ContainsKey(target))
                    {
                        await message.Channel.SendMessageAsync(
                            $"**{targetUser.Username}** has **{Users[target].getKarma()}** karma.");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"**{targetUser.Username}** has **0** karma.");
                    }
                }
                else
                {
                    if (Users.ContainsKey(message.Author.Id))
                    {
                        await message.Channel.SendMessageAsync(
                            $"You have **{Users[message.Author.Id].getKarma()}** karma.");
                    }
                    else
                    {
                        await message.Channel.SendMessageAsync($"You have **0** karma.");
                    }
                }

            }

            if (message.Content.StartsWith("e!"))
            {
                if (message.Content.Split(" ").Length > 1)
                {
                    string emote = message.Content.Split(" ")[1];
                    if (message.MentionedUsers.Count == 1)
                    {
                        SocketUser targetUser = message.MentionedUsers.ToImmutableList<SocketUser>()[0];
                        ulong target = targetUser.Id;
                        if (Users.ContainsKey(target) && Users[target].tryGetCount(emote, out uint count))
                        {
                            await message.Channel.SendMessageAsync(
                                $"**{targetUser.Username}** has **{count}**  {emote}");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"**{targetUser.Username}** has **0**  {emote}");
                        }
                    }
                    else
                    {
                        if (Users.ContainsKey(message.Author.Id) &&
                            Users[message.Author.Id].tryGetCount(emote, out uint count))
                        {
                            await message.Channel.SendMessageAsync($"You have **{count}**  {emote}");
                        }
                        else
                        {
                            await message.Channel.SendMessageAsync($"You have **0**  {emote}");
                        }
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync($"**Invalid syntax.** Usage: *e! (emote) [user]*");
                }

            }

            if (message.Content.StartsWith("ktop!"))
            {
                Dictionary<ulong, long> top = new Dictionary<ulong, long>();
                foreach (KeyValuePair<ulong, DataUser> kvp in Users)
                {
                    top.Add(kvp.Key, kvp.Value.getKarma());
                }

                var sortedTop = from entry in top orderby entry.Value descending select entry;

                bool tooLong = false;
                int maxLength = 40;

                string output = "**Top Karma of All Time**\n";
                string outputExtra = "";
                int count = 1;
                foreach (KeyValuePair<ulong, long> kvp in sortedTop)
                {
                    string currentUser = Users[kvp.Key].UserName;
                    if (count < maxLength)
                    {
                        output += String.Format("`{0, -2}. {1, -33} {2, -5}`\n", count, currentUser, kvp.Value);
                    }
                    else
                    {
                        outputExtra += String.Format("`{0, -2}. {1, -33} {2, -5}`\n", count, currentUser, kvp.Value);
                        tooLong = true;
                    }

                    count++;
                }

                await message.Channel.SendMessageAsync(output);
                if (tooLong)
                {
                    await message.Channel.SendMessageAsync(outputExtra);
                }
            }

            if (message.Content.StartsWith("etop!"))
            {
                if (message.Content.Split(" ").Length > 1)
                {
                    string emote = message.Content.Split(" ")[1];
                    Dictionary<ulong, long> top = new Dictionary<ulong, long>();
                    foreach (KeyValuePair<ulong, DataUser> kvp in Users)
                    {
                        uint found;
                        kvp.Value.tryGetCount(emote, out found);
                        if (found == 0)
                        {
                            continue;
                        }

                        top.Add(kvp.Key, found);
                    }

                    var sortedTop = from entry in top orderby entry.Value descending select entry;

                    bool tooLong = false;
                    int maxLength = 40;

                    string output = $"**Top**  {emote}  **of All Time**\n";
                    string outputExtra = "";
                    int count = 1;
                    foreach (KeyValuePair<ulong, long> kvp in sortedTop)
                    {
                        string currentUser = Users[kvp.Key].UserName;
                        if (count < maxLength)
                        {
                            output += String.Format("`{0, -2}. {1, -33} {2, -5}`\n", count, currentUser, kvp.Value);
                        }
                        else
                        {
                            outputExtra += String.Format("`{0, -2}. {1, -33} {2, -5}`\n", count, currentUser,
                                kvp.Value);
                            tooLong = true;
                        }

                        count++;
                    }

                    await message.Channel.SendMessageAsync(output);
                    if (tooLong)
                    {
                        await message.Channel.SendMessageAsync(outputExtra);
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync($"**Invalid syntax.** Usage: *etop! (emote)*");
                }
            }
        }

        private async Task UserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            String beforeChannelName = before.VoiceChannel == null ? "None" : before.VoiceChannel.Name;
            String afterChannelName = after.VoiceChannel == null ? "None" : after.VoiceChannel.Name;
            
            Console.WriteLine($"Before: {beforeChannelName}");
            Console.WriteLine($"After: {afterChannelName}");
        }
    }
}
