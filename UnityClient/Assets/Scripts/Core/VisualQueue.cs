using System.Collections.Generic;

public static class VisualQueue {
    // 默认开启 Headless 模式，用于后端的自动化测试
    // 当前端启动时，会自动将此值置为 false
    public static bool IsHeadless = true;
    
    private static Queue<IVisualCommand> _queue = new Queue<IVisualCommand>();

    public static void Enqueue(IVisualCommand cmd) {
        if (IsHeadless) {
            // 在无头测试模式下，瞬间遍历执行完所有状态刷新（跳过 yield 的等待时间）
            var enumerator = cmd.Execute();
            while (enumerator.MoveNext()) { }
        } else {
            _queue.Enqueue(cmd);
        }
    }

    public static bool TryDequeue(out IVisualCommand cmd) {
        if (_queue.Count > 0) {
            cmd = _queue.Dequeue();
            return true;
        }
        cmd = null;
        return false;
    }

    public static int Count => _queue.Count;

    public static void Clear() {
        _queue.Clear();
    }
}