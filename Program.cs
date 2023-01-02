using Nethermind.Specs;
using Nethermind.Evm.EOF;

var spec = (ReleaseSpec)Shanghai.Instance;
if (args.Length > 0) {
    if(args[0] == "--deactivate" || args[0] == "-d") {
        if(args.Length < 2) {
            Console.WriteLine("Usage: --deactivate <Eip-Number>+ \n\t Eip-Number must be an integer and must be a valid EIP number. \n\t EIP deactivates all EIPs that depend on it.");
            return;
        }

        foreach(var arg in args.Skip(1)) {
            spec = spec.DeactivateEip(int.Parse(arg));
        }
        Console.WriteLine($"Current Activated Eof Eips => 3540 : {spec.IsEip3540Enabled}, 3670 : {spec.IsEip3670Enabled}, 4200 : {spec.IsEip4200Enabled}, 4750 : {spec.IsEip4750Enabled}, 5450 : {spec.IsEip5450Enabled}");
    } else if(args[0] == "--diff") {
        if(args.Length < 3) {
            Console.WriteLine("Usage: --diff <Source-File> <Target-File>+ \n\t Source-File and Target-File must be a valid file path.");
            return;
        }
        DiffChecker.Check(args.Skip(1).ToArray());
        return;
    } 
}

EvmObjectFormat? parser = new EvmObjectFormat(spec);

string line;
int idx = 1;
while((line = Console.ReadLine()) != null) {
    var bytecode = line.toByteArray();
    var actual = parser.IsValidEof(bytecode);
    var result = actual switch {
        Success<EofHeader?> success => $"OK [{success.Value?.ToString()}]",
        Failure<string> failure => $"err: {failure.Message}",
        _ => "Unknown result"
    };
    idx ++;
    Console.WriteLine(result);
}
