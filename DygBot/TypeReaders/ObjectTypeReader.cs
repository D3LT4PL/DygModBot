﻿using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace DygBot.TypeReaders
{
    internal class ObjectTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            object result = input;  // Get command input
            return Task.FromResult(TypeReaderResult.FromSuccess(result));   // Return input
        }
    }
}
