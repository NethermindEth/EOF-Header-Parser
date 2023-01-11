
using System.Net;
using System.Text;

namespace TypeExtensions;
public interface ILogger {
    bool IsDebug {get; set;}
    bool IsInfo {get; set;}
    bool IsWarn {get; set;}
    bool IsError {get; set;}
    bool IsTrace {get; set;}

    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Trace(string message);
    void Clear();
    string Content {get;}

}
public class Logger : ILogger
{
    public StringBuilder ContentBuilder {get; set;} = new StringBuilder();
    public string Content => ContentBuilder.ToString();
    public bool IsDebug {get; set;}
    public bool IsInfo {get; set;}
    public bool IsWarn {get; set;}
    public bool IsError {get; set;}
    public bool IsTrace {get; set;}

    public void Clear() {
        ContentBuilder.Clear();
    }

    public void Debug(string message) {
        if(IsDebug) {
            ContentBuilder.Append($"DEBUG: {message}");
        }
    }

    public void Info(string message) {
        if(IsInfo) {
            ContentBuilder.Append($"INFO: {message}");
        }
    }

    public void Warn(string message) {
        if(IsWarn) {
            ContentBuilder.Append($"WARN: {message}");
        }
    }

    public void Error(string message) {
        if(IsError) {
            ContentBuilder.Append($"ERROR: {message}");
        }
    }

    public void Trace(string message) {
        if(IsTrace) {
            ContentBuilder.Append($"TRACE: {message}");
        }
    }
}