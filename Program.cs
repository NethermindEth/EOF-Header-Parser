using System.Reflection.Metadata.Ecma335;
using Nethermind.Specs;

EvmObjectFormat? parser = new EvmObjectFormat();
var spec = Shanghai.Instance;

string line;
while((line = Console.ReadLine()) != null) {
    var bytecode = line.toByteArray();
    var actual = parser.ValidateInstructions(bytecode, spec);
    var result = actual switch {
        Success<EofHeader> success => $"OK {bytecode[success.Value.CodeSectionOffsets].ToHexString()}",
        Failure<string> failure => $"err: {failure.Message}",
        _ => "Unknown result"
    };
    Console.WriteLine(result);
}
