using CtfDeck.WsServer;
using CtfDeck.WsServer.WebSocket;

var server = new WebSocketServer("localhost", 42712);
using var runner = new ServerHostRunner(server);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = runner.RequestShutdown();
};

await runner.RunAsync();
