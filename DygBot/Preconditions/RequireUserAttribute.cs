﻿using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DygBot.Preconditions
{
    public class RequireUserAttribute : PreconditionAttribute
    {
        private readonly ulong _userId;

        public RequireUserAttribute(ulong userId) => _userId = userId;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                if (gUser.Id == _userId)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError("You can't use that command"));
            }
            else
                return Task.FromResult(PreconditionResult.FromError("You need to be in the guild to use that command"));
        }
    }
}
