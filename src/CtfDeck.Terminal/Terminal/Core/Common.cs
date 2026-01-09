namespace CtfDeck.Terminal.Terminal.Core;

/// <summary>
/// Delegate for streaming output events
/// </summary>
public delegate Task OutputReceivedHandler(string data, bool isError);
