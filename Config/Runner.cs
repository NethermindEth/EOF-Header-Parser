using System.Net;
using System.Reflection.Metadata;
using System.Text;
using Nethermind.Evm.EOF;
using Nethermind.Specs;
using static TypeExtensions.FileExtensions;
public record Config {
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append("Config {");
        if(Deactivated is not null) {
            sb.Append($"Deactivated : {String.Join(',', Deactivated)}, ");
        }
        if(DiffFiles is not null) {
            sb.Append($"DiffFiles : {String.Join(',', DiffFiles)}, ");
        }
        if(Bytecodes is not null) {
            sb.Append($"Bytecodes : {String.Join(',', Bytecodes)}, ");
        }
        if(Inputs is not null) {
            sb.Append($"Inputs : {String.Join(',', Inputs)}, ");
        }
        sb.Append("}");
        return sb.ToString();
    }

    public string[] Deactivated { get; set; }
    public string[] DiffFiles { get; set; }
    public string[] Bytecodes { get; set; }
    public string[] Inputs { get; set; }
    public void Run() {
        var spec = (ReleaseSpec)Shanghai.Instance;
        if(DiffFiles is not null) {
            DiffChecker.Check(DiffFiles);
            return;
        }

        if(Deactivated is not null) {
            foreach(var eip in Deactivated) {
                spec = spec.DeactivateEip(int.Parse(eip));
            }
            Console.WriteLine($"Current Activated Eof Eips => 3540 : {spec.IsEip3540Enabled}, 3670 : {spec.IsEip3670Enabled}, 4200 : {spec.IsEip4200Enabled}, 4750 : {spec.IsEip4750Enabled}, 5450 : {spec.IsEip5450Enabled}");
        } 

        if(Bytecodes is not null) {
            int idx = 0;
            foreach(var bytecode in Bytecodes) {
                HandleLine(idx++, bytecode, spec);
            }
            return;
        } else if(Inputs is not null) {
            foreach(string filePath in Inputs) {
                HandleFile(filePath, spec);
            }
            return;
        } 

        HandleStdin(spec);
    }

    public static void HandleStdin(ReleaseSpec spec) {
        string line;
        int idx = 1;
        while((line = Console.ReadLine()) != null) {
            HandleLine(idx++, line, spec);
        }
    }

    public static void HandleFile(string filePath, ReleaseSpec spec) {
        int idx = 1;
        foreach(var line in FetchFile(filePath)) {
            HandleLine(idx++, line, spec);
        }
    }
    public static void HandleLine(int idx, string line, ReleaseSpec spec) {
        EvmObjectFormat? parser = new EvmObjectFormat(spec);
        var bytecode = line.toByteArray();
        try {
            var actual = parser.IsValidEof(bytecode);
            Console.Write($"Line {idx} : ");
            switch(actual) {
                case Success<EofHeader?> success:
                    var codeSections = String.Join(",", success.Value.Value.CodeSections.Select(section => {
                        var start = section.Start;
                        var end = section.EndOffset;
                        var code = bytecode[start..end];
                        return code.ToHexString();
                    })).ToLower();
                    Console.WriteLine($"OK {codeSections}");
                    break;
                case Failure<string> failure:
                    Console.WriteLine($"err: {failure.Message}");
                    break;
            }
        } catch(Exception e) {
            Console.WriteLine($"Exception : {e.Message} at line {idx} : {e.StackTrace}");
        }
    }

    private static void PrintHelp() {
        Console.WriteLine("Usage: --Deactivate <Eip-Number>+ \n\t Eip-Number must be an integer and must be a valid EIP number. \n\t EIP deactivates all EIPs that depend on it.");
        Console.WriteLine("Usage: --DiffFiles <Source-File> <Target-File>+ \n\t Source-File and Target-File must be a valid file path.");
        Console.WriteLine("Usage: --Bytecode <Bytecode>+ \n\t Bytecode must be a valid bytecode.");
        Console.WriteLine("Usage: --Inputs <Input>+ \n\t Input must be a valid input.");
    }
    public static bool TryParse(string args,[NotNullWhen(true)] out Config config) {
        config = new Config();
        var argsArray = args.Split(' ');
        // get peropertis of config
        var properties = config.GetType().GetProperties().Select(p => $"--{p.Name}").ToArray();
        if(argsArray.Length == 0) {
            return true;
        }

        if(argsArray.Length == 1) {
            if(argsArray[0] == "--help" || argsArray[0] == "-h") {
                PrintHelp();
                return false;
            }
        }

        for(int i = 0; i < argsArray.Length; i++) {

            if(!properties.Contains(argsArray[i])) {
                continue;
            }

            List<string>? arguments = new();
            for(int j = i + 1; j < argsArray.Length; j++) {
                if(properties.Contains(argsArray[j])) {
                    break;
                }
                arguments.Add(argsArray[j]);
            }

            var property = config.GetType().GetProperty(argsArray[i].Substring(2));
            property.SetValue(config, arguments.ToArray());
        }

        if(config.ValidateConfigFile()) {
            return true;
        } else {
            config = null;
            return false;
        }
    } 

    public bool ValidateConfigFile() {
        bool isValid = true;
        if(DiffFiles is not null && (this.Bytecodes is not null || this.Inputs is not null || this.Deactivated is not null)) {
            Console.WriteLine("Invalid config file. Bytecode and input and deactivate cannot be used with diff.");
            isValid = false;
        }

        if(Bytecodes is not null && this.Inputs is not null) {
            Console.WriteLine("Invalid config file. Inputs and Bytecodes cannot be used with bytecode.");
            isValid = false;
        }
        
        if(DiffFiles is not null && DiffFiles.Length != 2) {
            Console.WriteLine("DiffFiles must have exactly 2 files");
            isValid = false;
        }

        if(!isValid) {
            PrintHelp();
        }

        return isValid;
    }
}