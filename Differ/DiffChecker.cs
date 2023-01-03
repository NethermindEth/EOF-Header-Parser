using static TypeExtensions.FileExtensions;
public class DiffChecker {
    public static void Check(string[] paths) {

        var diff = new DiffChecker(paths);
        if(diff.Check() is Failure<String> failure) {
            Console.WriteLine(failure.Message);
        } else {
            Console.WriteLine("No diffs found");
        }
    }

    private readonly string[] _paths;

    private DiffChecker(string[] paths) => _paths = paths; 

    public Result CheckFileLength(int[] lengths) {
        List<String> diffs = new();
        for(int i = 0; i < _paths.Length; i++)
        {
            int size = lengths[i];
            int expectedSize = lengths.First();
            if(size != expectedSize) {
                diffs.Add($"Source and Target files have different number of lines. \n\tSource: {_paths[0]} has {expectedSize} lines, \n\tTarget: {_paths[i]} has {size} lines");
            }
        }
        return diffs.Count == 0 ? Success<String>.From("No diffs found") : Failure<String>.From(String.Join('\n', diffs));
    }

    private Result Check() {
        var files = _paths.Select(path => FetchFile(path)).ToArray();

        if(CheckFileLength(files.Select(file => file.Length).ToArray()) is Failure<String> lengthFailure) {
            return lengthFailure;
        }
        
        List<String> diffs = new();
        for(int i = 0; i < files[0].Length; i++) {
            var res = CheckResultDifference(
                i + 1, files.Select(file => file[i]).ToArray()
            );
            if(res is Failure<String> failure) {
                diffs.Add(failure.Message);
            }
        }

        return diffs.Count == 0 ? Success<String>.From("No diffs found") : Failure<String>.From(String.Join('\n', diffs));
    }

    public Result CheckResultDifference(int line, params string[] Lines) {
        bool isError(string line) => line.StartsWith("err: ");
        var sourceLine = Lines[0];
        
        List<String> diffs = new();

        for(int i = 0; i < Lines.Length; i++) {
            var targetLine = Lines[i];
            if(isError(sourceLine) == isError(targetLine)) {
                continue;
            } else {
                diffs.Add($"File {i + 1} differs. line :{line} \n\tSource: {sourceLine}, \n\tTarget {_paths[i]} : {targetLine}");
            }
        }
        return diffs.Count == 0 ? Success<String>.From("No diffs found") : Failure<String>.From(String.Join('\n', diffs));
    }
}
