using Nethermind.Specs;
using Nethermind.Evm.EOF;

if(Config.TryParse(String.Join(' ', args), out var config)) {
    config.Run();
} else {
    Console.WriteLine("Invalid config");
}