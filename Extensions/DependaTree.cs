using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Nethermind.Specs;

namespace TypeExtensions;

public static class EipExtensions {

    public static ReleaseSpec DeactivateEip(this ReleaseSpec spec, int arg) {
        foreach(int eip in arg.DependedOn()) {
            spec = spec.DeactivateEip(eip);
        }

        var field = spec.GetType().GetProperty($"IsEip{arg}Enabled");
        if(field != null) {
            field.SetValue(spec, false);
        } else {
            Console.WriteLine($"EIP{arg} is not a valid EIP number.");
        }
        return spec;
    }

    public static int[] DependsOn(this int eipNumber) {
        return eipNumber switch {
            3540 => new[] { 3680 },
            3670 => new[] { 3540 },
            4200 => new[] { 3670 },
            4750 => new[] { 4200 },
            5450 => new[] { 4750 },
            _ => throw new Exception($"EIP{eipNumber} is not included in DependaTree.cs")
        };
    }

    public static int[] DependedOn(this int eipNumber) {
        return eipNumber switch {
            3540 => new[] { 3670, 4200, 4750, 5450 },
            3670 => new[] { 4200, 4750, 5450 },
            4200 => new[] { 4750, 5450 },
            4750 => new[] { 5450 },
            5450 => Array.Empty<int>(),
            _ => throw new Exception($"EIP{eipNumber} is not included in DependaTree.cs")
        };
    }
}