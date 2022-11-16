using System.Reflection.Metadata.Ecma335;
using Nethermind.Specs;

EvmObjectFormat? parser = new EvmObjectFormat();
using StreamReader? input = File.OpenText("./Tests/All.input");
using StreamReader? output = File.OpenText("./Tests/All.output");
var spec = Shanghai.Instance;

while(!input.EndOfStream) {
    var bytecode = input.ReadLine().toByteArray();
    var actual = parser.ValidateInstructions(bytecode, spec);
    actual.Handle(
        success: (EofHeader? result) => 
        {
            var (start, size) = result.CodeSectionOffsets;
            var message = $"Ok {bytecode[start .. (start + size)].ToHexString()}";
            Console.WriteLine(message);
        },
        failure: (string error) => {
            var message = $"Err {error}";
            Console.WriteLine(message);
        }
    );
}
