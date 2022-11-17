using System.Reflection.Metadata.Ecma335;
using Nethermind.Specs;

EvmObjectFormat? parser = new EvmObjectFormat();
var spec = Shanghai.Instance;

string line;
while((line = Console.ReadLine()) != null) {
    var bytecode = line.toByteArray();
    var actual = parser.ValidateInstructions(bytecode, spec);
    actual.Handle(
        success: (EofHeader? result) => 
        {
            var (start, size) = result.CodeSectionOffsets;
            Console.WriteLine($"OK {bytecode[start .. (start + size)].ToHexString()}");
        },
        failure: (string error) => {
            Console.WriteLine($"err: {error}");
        }
    );
}
