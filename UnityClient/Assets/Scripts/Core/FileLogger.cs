using UnityEngine;
using System.IO;
using System;

// 专门负责将 Unity 控制台日志拦截并写入本地文本的工具类
public class FileLogger : MonoBehaviour
{
    private string logFilePath;
    private StreamWriter logWriter;

    void Awake()
    {
        // 保持后端的好习惯：将日志统一放在项目根目录（Assets同级）的 Logs 文件夹下
        string logDirectory = Path.Combine(Application.dataPath, "../Logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // 每天/每次启动生成一个带时间戳的 log 文件
        string dateStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        logFilePath = Path.Combine(logDirectory, $"GameLog_{dateStr}.log");

        try
        {
            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true; // 开启自动刷入磁盘，防止游戏崩溃时丢失最后几行日志

            // 核心钩子：拦截 Unity 引擎产生的所有 Debug.Log、LogWarning 和 LogError
            Application.logMessageReceived += HandleLog;
            
            Debug.Log($"[FileLogger] 后端级文件日志服务已启动，日志路径: {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FileLogger] 无法创建日志文件: {e.Message}");
        }
    }

    // 事件回调：每当引擎有日志输出时，都会走这里
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logWriter == null) return;

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string logEntry = $"[{timestamp}] [{type}] {logString}";
        
        logWriter.WriteLine(logEntry);
        
        // 如果是报错或异常，把堆栈信息也写进去（极其方便排查 NullReferenceException）
        if (type == LogType.Exception || type == LogType.Error)
        {
            logWriter.WriteLine(stackTrace);
        }
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        if (logWriter != null)
        {
            logWriter.Close();
            logWriter = null;
        }
    }
}
