using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace CTGPPopularityTracker.Commands
{
    public class CustomHelpFormatter : BaseHelpFormatter
    {
        public DiscordEmbedBuilder EmbedBuilder { get; }
        private Command Command { get; set; }

        public CustomHelpFormatter(CommandContext ctx) : base(ctx)
        {
            this.EmbedBuilder = new DiscordEmbedBuilder()
            {
                Title = "Help",
                Color = new Optional<DiscordColor>(0xFE0002)
            };
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            this.Command = command;

            this.EmbedBuilder.WithDescription($"{Formatter.InlineCode(command.Name)}: {command.Description ?? "No description provided."}");

            if (command is CommandGroup cgroup && cgroup.IsExecutableWithoutSubcommands)
                this.EmbedBuilder.WithDescription($"{this.EmbedBuilder.Description}\n\nThis group can be executed as a standalone command.");

            if (command.Aliases?.Any() == true)
                this.EmbedBuilder.AddField("Aliases", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)), false);

            if (command.Overloads?.Any() != true) return this;
            
            var sb = new StringBuilder();

            foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
            {
                sb.Append('`').Append(command.QualifiedName);

                foreach (var arg in ovl.Arguments)
                    sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name).Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');

                sb.Append("`\n");

                foreach (var arg in ovl.Arguments)
                    sb.Append('`').Append(arg.Name).Append(" (").Append(this.CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ").Append(arg.Description ?? "No description provided.").Append('\n');

                sb.Append('\n');
            }

            this.EmbedBuilder.AddField("Arguments", sb.ToString().Trim(), false);

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
        {
            if (this.Command != null)
            {
                this.EmbedBuilder.AddField("Subcommands", string.Join(", ", cmds.Select(x => Formatter.InlineCode(x.Name)))); ;
                return this;
            }

            //Format default help
            var sbList = new List<StringBuilder>()
                {new StringBuilder(), new StringBuilder(), new StringBuilder(), new StringBuilder()};

            foreach (var cmd in cmds)
            {
                if (cmd.Name.Contains("nin")) sbList[0].Append($"`{cmd.Name}`, ");
                else if (cmd.Name.Contains("ctgp")) sbList[1].Append($"`{cmd.Name}`, ");
                else if (cmd.Name.Contains("poll")) sbList[2].Append($"`{cmd.Name}`, ");
                else sbList[3].Append($"`{cmd.Name}`, ");
            }

            //Add fields to help
            this.EmbedBuilder.AddField("Popularity Commands - Nintendo", sbList[0].ToString().Remove(sbList[0].Length - 2));
            this.EmbedBuilder.AddField("Popularity Commands - CTGP", sbList[1].ToString().Remove(sbList[1].Length - 2));
            this.EmbedBuilder.AddField("Poll Commands", sbList[2].ToString().Remove(sbList[2].Length - 2));
            this.EmbedBuilder.AddField("Other Commands", sbList[3].ToString().Remove(sbList[3].Length - 2));
            
            return this;
        }

        public override CommandHelpMessage Build()
        {
            if (this.Command == null)
                this.EmbedBuilder.WithDescription("Listing all top-level commands and groups. Specify a command to see more information.");

            return new CommandHelpMessage(embed: this.EmbedBuilder.Build());
        }
    }
}
