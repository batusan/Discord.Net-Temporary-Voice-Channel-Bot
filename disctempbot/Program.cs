using System;
using System.Collections;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace disctempbot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;
        private ulong _roleId;
        private ArrayList channelIds = new ArrayList();


        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _roleId = 195633968857612288;

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .BuildServiceProvider();

            string token = "NzAzOTY0MTcxMjQyMzA3NjM0.XqbdVQ.eEkTEB6B7UwdbM37MbhtCXlXjYc";

            _client.Log += _client_Log;

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            await Task.Delay(-1);

            }


        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.UserJoined += AnnounceJoinedUser;
            _client.UserLeft += AnnounceLeftUser;
            _client.MessageReceived += HandleCommandAsync;
            _client.UserVoiceStateUpdated += VoiceStateUpdate;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        
        public Task VoiceStateUpdate(SocketUser user, SocketVoiceState oldVoiceState, SocketVoiceState newVoiceState)
        {
            Console.WriteLine($"VoiceStateUpdate: {user} - {oldVoiceState.VoiceChannel?.Name ?? "null"} -> {newVoiceState.VoiceChannel?.Name ?? "null"}");
            if(newVoiceState.VoiceChannel != null)
            {
                if (newVoiceState.VoiceChannel.Id == 728693079464214630 && newVoiceState.VoiceChannel.Id != null)
                {
                    //User joined
                    Console.WriteLine($"User (Name: {user.Username} ID: {user.Id}) joined to a VoiceChannel (Name: {newVoiceState.VoiceChannel.Name} ID: {newVoiceState.VoiceChannel.Id})");
                    createAndMoveUsers(user.Username, user.Id);
                }
                checkRoomStatus();
            }
            else
            {
                checkRoomStatus();
            }
            return Task.CompletedTask;
        }

        public void checkRoomStatus()
        {
            for (int i = 0; i < channelIds.Count; i++)
            {
                ulong chId = Convert.ToUInt64(channelIds[i].ToString());

                if (_client.GetGuild(125200074559979520).GetVoiceChannel(chId).Users.Count == 0)
                {
                    _ = _client.GetGuild(125200074559979520).GetVoiceChannel(chId).DeleteAsync();
                    Console.WriteLine(chId + " idli oda siliniyor.");
                    if (channelIds.Contains(chId))
                    {
                        channelIds.RemoveAt(channelIds.IndexOf(chId));
                    }
                }
            }
            //foreach (ulong chId in channelIds)
            //{
            //   // Console.WriteLine(_client.GetGuild(125200074559979520).GetVoiceChannel(chId).Users.Count);

            //}
        }

        public async void createAndMoveUsers(string name, ulong Id)
        {
            var roomName = name + "'nin Odası";
            var clientVoice = await _client.GetGuild(125200074559979520).CreateVoiceChannelAsync(name + "'nin Odası");

            await clientVoice.ModifyAsync(x =>
            {
                x.CategoryId = 724955739130429464;
            });
            foreach (SocketGuildChannel chan in _client.GetGuild(125200074559979520).Channels)
            {
                if (roomName == chan.Name)
                {
                    channelIds.Add(chan.Id);
                    _ = _client.GetGuild(125200074559979520).GetUser(Id).ModifyAsync(x =>
                    {
                        x.ChannelId = chan.Id;
                    });

                    ITextChannel logChannel = _client.GetChannel(724944760258822215) as ITextChannel;
                    var EmbedBuilderLog = new EmbedBuilder()
                        .WithDescription($"{name} bir oda oluşturdu.\n**ID** {Id}\n Tarih : {DateTime.Now}")
                        .WithFooter(footer =>
                        {
                            footer
                            .WithText("User Room Log")
                            .WithIconUrl("https://communityroundtable.com/wp-content/uploads/2017/10/Moderation_Icon.png");
                        });
                    Embed embedLog = EmbedBuilderLog.Build();
                    await logChannel.SendMessageAsync(embed: embedLog);
                    //SocketMessage message = null;
                    //SocketGuild guild = ((SocketGuildChannel)message.Channel).Guild;
                    //IEmote emote = guild.Emotes.First(e => e.Name == "poggersslide");
                    //message.AddReactionAsync(emote);
                }
            }

        }
        public async Task AnnounceLeftUser(SocketGuildUser user)
        {
            var channel = _client.GetChannel(463007417672663049) as SocketTextChannel; // Gets the channel to send the message in
            await channel.SendMessageAsync($"Görüşürüz {user.Mention}, sunucusuya tekrar bekleriz , "); //Welcomes the new user
        }

        public async Task AnnounceJoinedUser(SocketGuildUser user) //Welcomes the new user
        {
            var channel = _client.GetChannel(463007417672663049) as SocketTextChannel; // Gets the channel to send the message in
            await channel.SendMessageAsync($"{channel.Guild.Name} sunucusuna , Hoşgeldin {user.Mention}!"); //Welcomes the new user

            var guild = user.Guild;
            // Check if the desired role exist within this guild.
            // If not, we simply bail out of the handler.
            var role = guild.GetRole(_roleId);
            if (role == null) return;

            // Finally, we call AddRoleAsync
            await user.AddRoleAsync(role);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);
            if (message.Author.IsBot) return;

            int argPos = 0;
            if (message.HasStringPrefix("!", ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
                if (result.Error.Equals(CommandError.UnmetPrecondition)) await message.Channel.SendMessageAsync(result.ErrorReason);
            }
        }
    }
}