using Nethermind.Specs;
using Nethermind.Evm.EOF;

var spec = (ReleaseSpec)Shanghai.Instance;
if (args.Length > 0) {
    if((args[0] == "--deactivate" || args[0] == "-d") && args.Length > 1) {
        foreach(var arg in args.Skip(1)) {
            spec = spec.DeactivateEip(int.Parse(arg));
        }
        Console.WriteLine($"Current Activated Eof Eips => 3540 : {spec.IsEip3540Enabled}, 3670 : {spec.IsEip3670Enabled}, 4200 : {spec.IsEip4200Enabled}, 4750 : {spec.IsEip4750Enabled}, 5450 : {spec.IsEip5450Enabled}");
    } else if(args[0] == "--diff") {
        bool isError(string line) => line.StartsWith("err: ");

        var fileSourcePath = args[1];
        var fileTargetPath = args[2];
        var source = File.ReadAllLines(fileSourcePath);
        var target = File.ReadAllLines(fileTargetPath);
        if(source.Length != target.Length) {
            Console.WriteLine($"Source and Target files have different number of lines. Source: {source.Length}, Target: {target.Length}");
            return;
        }
        
        for(int i = 0; i < source.Length; i++) {
            var sourceLine = source[i];
            var targetLine = target[i];
            if(isError(sourceLine) && isError(targetLine)
               || !isError(sourceLine) && !isError(targetLine)) {
                continue;
            } else {
                Console.WriteLine($"Line {i + 1} differs. Source: {sourceLine}, Target: {targetLine}");
            }
        }
    } else {
        Console.WriteLine("Usage: --deactivate <Eip-Number>+ \n\t Eip-Number must be an integer and must be a valid EIP number. \n\t EIP deactivates all EIPs that depend on it.");
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
