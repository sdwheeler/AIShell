﻿using System.CommandLine;
using System.Text;
using ShellCopilot.Kernel;

namespace ShellCopilot.App;

internal class Program
{
    static int Main(string[] args)
    {
        // TODO-1: Currently the syntax is `aish [<query>] [command] [options]`, namely,
        // one can run `aish "hello" list`, and it will execute the list command.
        // Ideally, we want it to be like dotnet, where `dotnet applicat-path` is clearly
        // separated from the `dotnet sdk` commands such as `dotnet run`.
        //
        // TODO-2: Add exception handling. The default exception handling is just to write
        // out the stack trace. We need to have our own exception handling to make it less
        // scary and more useful.
        //
        // TODO-3: System.CommandLine is undergoing lots of design changes, with breaking
        // changes to the existing public APIs. We will need to evaluate whether we want to
        // keep depending on it when this project moves beyond a prototype.

        Console.OutputEncoding = Encoding.Default;
        Argument<string> query = new("query", getDefaultValue: () => null, "The query term used to get response from AI.");
        Option<bool> use_alt_buffer = new("--use-alt-buffer", "Use the alternate screen buffer for an interactive session.");

        query.AddValidator(result =>
        {
            string value = result.GetValueForArgument(query);

            if (value is not null && value.StartsWith('-'))
            {
                result.ErrorMessage = $"Bad flag or option syntax: {value}";
            }
        });

        RootCommand rootCommand = new("AI for the command line.")
        {
            query, use_alt_buffer
        };

        rootCommand.SetHandler(StartShellAsync, query, use_alt_buffer);
        return rootCommand.Invoke(args);
    }

    private async static Task StartShellAsync(string query, bool use_alt_buffer)
    {
        Shell shell;
        if (query is not null)
        {
            shell = new(interactive: false, useAlternateBuffer: false);

            if (Console.IsInputRedirected)
            {
                string context = Console.In.ReadToEnd();
                if (context is not null && context.Length > 0)
                {
                    query = string.Concat(query, "\n\n", context);
                }
            }

            await shell.RunOnceAsync(query);
            return;
        }

        if (Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("Cannot run interactively when the stdin, stdout, or stderr is redirected.");
            Console.Error.WriteLine("To run non-interactively, specify the <query> argument and try again.");
            return;
        }

        shell = new(interactive: true, use_alt_buffer);
        await shell.RunREPLAsync();
    }
}