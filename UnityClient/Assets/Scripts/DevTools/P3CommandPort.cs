using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

// Lets an agent drive a running build the way MCP drives the Editor: `P3.exe -p3cmd [port]` listens on localhost
// only (default 47823) and runs one command per connection on the main thread. A command is a P3DevTools method
// with its arguments ("Describe", "Click 继续", "RunDive 40 true") or one of: help, ping, wait <s>,
// screenshot [path] [maxWidth], quit. The reply is one JSON line {"ok":..,"result":".."}.
// Off unless the flag is given. Client: tools/agent/p3cmd.py.
public class P3CommandPort : MonoBehaviour {
    public const string Flag = "-p3cmd";
    public const int DefaultPort = 47823;
    private const int ReplyTimeoutMs = 180000;

    private readonly ConcurrentQueue<Request> _requests = new ConcurrentQueue<Request>();
    private TcpListener _listener;
    private Thread _thread;
    private volatile bool _running;

    private class Request {
        public string Command;
        public string Reply;
        public bool Ok;
        public readonly ManualResetEventSlim Done = new ManualResetEventSlim(false);
    }

    [Serializable]
    private class Reply {
        public bool ok;
        public string result;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, Flag);
        if (index < 0 || FindObjectOfType<P3CommandPort>() != null) {
            return;
        }

        int port = index + 1 < args.Length && int.TryParse(args[index + 1], out int parsed) ? parsed : DefaultPort;
        GameObject root = new GameObject("P3CommandPort_Runtime");
        DontDestroyOnLoad(root);
        root.AddComponent<P3CommandPort>().Listen(port);
    }

    private void Listen(int port) {
        _listener = new TcpListener(IPAddress.Loopback, port);
        _listener.Start();
        _running = true;
        _thread = new Thread(AcceptLoop) { IsBackground = true, Name = "P3CommandPort" };
        _thread.Start();
        Debug.Log($"[P3CommandPort] Listening on 127.0.0.1:{port}.");
    }

    private void AcceptLoop() {
        while (_running) {
            try {
                using (TcpClient client = _listener.AcceptTcpClient())
                using (NetworkStream stream = client.GetStream())
                using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false)))
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false))) {
                    Request request = new Request { Command = reader.ReadLine() ?? string.Empty };
                    _requests.Enqueue(request);
                    bool finished = request.Done.Wait(ReplyTimeoutMs);
                    Reply reply = new Reply {
                        ok = finished && request.Ok,
                        result = finished ? request.Reply : "Timed out waiting for the game's main thread."
                    };
                    writer.WriteLine(JsonUtility.ToJson(reply));
                    writer.Flush();
                }
            } catch (Exception e) {
                if (_running) {
                    Debug.LogWarning("[P3CommandPort] " + e.Message);
                }
            }
        }
    }

    private void Update() {
        while (_requests.TryDequeue(out Request request)) {
            StartCoroutine(Execute(request));
        }
    }

    private void OnDestroy() {
        _running = false;
        _listener?.Stop();
    }

    private IEnumerator Execute(Request request) {
        List<string> tokens = Tokenize(request.Command);
        string name = tokens.Count > 0 ? tokens[0] : "help";
        List<string> args = tokens.Skip(1).ToList();
        request.Ok = true;
        switch (name.ToLowerInvariant()) {
            case "ping":
                request.Reply = "pong";
                break;
            case "help":
                request.Reply = Help();
                break;
            case "wait":
                float seconds = args.Count > 0 && float.TryParse(args[0], out float parsed) ? parsed : 1f;
                yield return new WaitForSecondsRealtime(seconds);
                request.Reply = $"Waited {seconds}s.";
                break;
            case "screenshot":
                yield return new WaitForEndOfFrame();
                // "screenshot", "screenshot 960", "screenshot <path>" or "screenshot <path> 960".
                string shotPath = args.Count > 0 && !int.TryParse(args[0], out _) ? args[0] : null;
                string widthArg = args.LastOrDefault();
                try {
                    request.Reply = Screenshot(shotPath, widthArg != null && int.TryParse(widthArg, out int width) ? width : 640);
                } catch (Exception e) {
                    // Every path must reach Done.Set: an exception escaping the coroutine left the client waiting and
                    // queued every later command behind it.
                    request.Ok = false;
                    request.Reply = e.Message;
                }
                break;
            case "quit":
                request.Reply = "Quitting.";
                request.Done.Set();
                yield return null;
                Application.Quit();
                yield break;
            default:
                try {
                    request.Reply = Invoke(name, args);
                } catch (Exception e) {
                    request.Ok = false;
                    request.Reply = (e is TargetInvocationException && e.InnerException != null ? e.InnerException : e).ToString();
                }
                break;
        }

        request.Done.Set();
    }

    private static string Invoke(string name, List<string> args) {
        MethodInfo method = typeof(P3DevTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase) && m.ReturnType == typeof(string))
            .Where(m => args.Count <= m.GetParameters().Length && m.GetParameters().Skip(args.Count).All(p => p.IsOptional))
            .OrderBy(m => m.GetParameters().Length)
            .FirstOrDefault();
        if (method == null) {
            throw new ArgumentException($"No P3DevTools method '{name}' taking {args.Count} argument(s). Send 'help' for the list.");
        }

        ParameterInfo[] parameters = method.GetParameters();
        object[] values = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++) {
            values[i] = i < args.Count ? Convert(args[i], parameters[i].ParameterType) : parameters[i].DefaultValue;
        }

        return (string)method.Invoke(null, values);
    }

    private static object Convert(string value, Type type) {
        if (type == typeof(string)) {
            return value;
        }

        if (type == typeof(bool)) {
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return System.Convert.ChangeType(value, type, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string Help() {
        IEnumerable<string> methods = typeof(P3DevTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.ReturnType == typeof(string))
            .Select(m => m.Name + "(" + string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name + (p.IsOptional ? " = " + (p.DefaultValue ?? "null") : string.Empty))) + ")")
            .OrderBy(s => s);
        return "Port commands: ping, help, wait <seconds>, screenshot [path] [maxWidth=640], quit\nP3DevTools: " + string.Join("; ", methods);
    }

    // The frame as the player sees it, shrunk to maxWidth so reading it back costs little. Default path: P3Cmd/ next
    // to the executable.
    private static string Screenshot(string path, int maxWidth) {
        Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
        Texture2D output = frame;
        if (maxWidth > 0 && frame.width > maxWidth) {
            int height = Mathf.RoundToInt(frame.height * (maxWidth / (float)frame.width));
            RenderTexture scaled = RenderTexture.GetTemporary(maxWidth, height);
            Graphics.Blit(frame, scaled);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = scaled;
            output = new Texture2D(maxWidth, height, TextureFormat.RGB24, false);
            output.ReadPixels(new Rect(0, 0, maxWidth, height), 0, 0);
            output.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(scaled);
        }

        if (string.IsNullOrEmpty(path)) {
            path = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "P3Cmd", $"shot_{DateTime.Now:HHmmss_fff}.png");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        File.WriteAllBytes(path, output.EncodeToPNG());
        string size = $"{output.width}x{output.height}";
        Destroy(frame);
        if (output != frame) {
            Destroy(output);
        }

        return $"Saved {path} ({size}).";
    }

    // Splits on spaces; double quotes group words ("Click \"出售全部战利品\"").
    private static List<string> Tokenize(string line) {
        List<string> tokens = new List<string>();
        StringBuilder current = new StringBuilder();
        bool quoted = false;
        foreach (char c in line.Trim()) {
            if (c == '"') {
                quoted = !quoted;
            } else if (c == ' ' && !quoted) {
                if (current.Length > 0) {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            } else {
                current.Append(c);
            }
        }

        if (current.Length > 0) {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
