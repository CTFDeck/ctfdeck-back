using CtfDeck.Terminal.Terminal;

class Program
{
    static async Task Main()
    {
        var executor = new TerminalExecutor();

        //Dummy loop to simulate terminal input/output
        while (true)
        {
            Console.Write(executor.GetPrompt());
            var command = Console.ReadLine();
            if (command is null || command.Trim() is "exit" or "quit")
                break;

            var result = await executor.ExecuteAsync(command);

            if (!string.IsNullOrEmpty(result.Output))
                Console.WriteLine(result.Output);

            if (!string.IsNullOrEmpty(result.Error))
                Console.Error.WriteLine(result.Error);
        }
    }
}