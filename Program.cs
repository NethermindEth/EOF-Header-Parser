using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using Nethermind.Specs;

var spec = (ReleaseSpec)Shanghai.Instance;
if (args.Length > 0) {
    if((args[0] == "--deactivate" || args[0] == "-d") && args.Length > 1) {
        foreach(var arg in args.Skip(1)) {
            spec = spec.DeactivateEip(int.Parse(arg));
        }
        Console.WriteLine($"Current Activated Eof Eips => 3540 : {spec.IsEip3540Enabled}, 3670 : {spec.IsEip3670Enabled}, 4200 : {spec.IsEip4200Enabled}, 4750 : {spec.IsEip4750Enabled}, 5450 : {spec.IsEip5450Enabled}");
    } else {
        Console.WriteLine("Usage: --deactivate <Eip-Number>+ \n\t Eip-Number must be an integer and must be a valid EIP number. \n\t EIP deactivates all EIPs that depend on it.");
        return;
    }
}



EvmObjectFormat? parser = new EvmObjectFormat();

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
