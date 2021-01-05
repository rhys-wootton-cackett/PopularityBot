using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace CTGPPopularityTracker.Commands
{
    public class CustomHelpFormatter : DefaultHelpFormatter
    {
        public CustomHelpFormatter(CommandContext ctx) : base(ctx) { }

        public override CommandHelpMessage Build()
        {
            EmbedBuilder.Color = new DiscordColor("#FE0002");
            return base.Build();
        }
    }
}
