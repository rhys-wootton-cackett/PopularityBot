using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace CTGPPopularityTracker
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class RequirePollStartChannelAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return null;
        }
    }
}
