﻿using System;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

namespace DygBot.Preconditions
{
    public class ScopeAttribute : PreconditionAttribute
    {
        private readonly Scope _scope;

        public ScopeAttribute(Scope scope) => _scope = scope;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (_scope == Scope.Any)
                return Task.FromResult(PreconditionResult.FromSuccess());
            else if (context.Channel is SocketDMChannel)
                if (_scope == Scope.DM)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError("Komenda działa tylko na serwerze"));
            else if (context.Channel is SocketGuildChannel)
                if (_scope == Scope.Guild)
                    return Task.FromResult(PreconditionResult.FromSuccess());
                else
                    return Task.FromResult(PreconditionResult.FromError("Komenda działa tylko w wiadomości prywatnej"));
            else
                return Task.FromResult(PreconditionResult.FromError("Nieznany błąd"));
        }
    }

    public enum Scope
    {
        Any,
        Guild,
        DM
    }
}
