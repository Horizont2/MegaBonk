using UnityEngine;

// A dead-man's switch on the console.
//
// A single script writing to a missing animator parameter every frame, on every
// unit of a crowd, produced thousands of console entries a second. Unity keeps
// every entry — with its stack trace — in memory, and the editor spends real time
// formatting each one. That is enough to take a whole machine down: it did, and
// it needed a hard reboot.
//
// The specific offender is fixed. This exists because the next one should cost a
// line in the console instead of somebody's afternoon. A cinematic that renders
// perfectly but can lock the editor is not finished, and "do not write a bug like
// that again" is not a safeguard.
//
// Two measures, both cheap:
//
//   Stack traces off for Log and Warning while the trailer runs. Capturing a
//   managed stack trace is most of what a log call costs, in both time and
//   memory, and nothing in a trailer needs one.
//
//   A rate limiter. Past a sane number of messages per second the logger is
//   switched off entirely, after saying once, loudly, that it has done so and
//   what the last message was. Losing the console is survivable; losing the
//   machine is not.
public static class TrailerLogGuard
{
    private const int MessagesPerSecondLimit = 400;

    private static bool s_armed;
    private static int s_count;
    private static float s_windowStart;
    private static string s_lastMessage = "";

    private static StackTraceLogType s_prevLog, s_prevWarning;

    public static void Arm()
    {
        if (s_armed) return;
        s_armed = true;

        s_prevLog = Application.GetStackTraceLogType(LogType.Log);
        s_prevWarning = Application.GetStackTraceLogType(LogType.Warning);
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        // Errors keep their traces: those are the ones worth reading.

        s_windowStart = Time.realtimeSinceStartup;
        s_count = 0;
        Application.logMessageReceived += OnMessage;
    }

    public static void Disarm()
    {
        if (!s_armed) return;
        s_armed = false;

        Application.logMessageReceived -= OnMessage;
        Application.SetStackTraceLogType(LogType.Log, s_prevLog);
        Application.SetStackTraceLogType(LogType.Warning, s_prevWarning);
        Debug.unityLogger.logEnabled = true;
    }

    private static void OnMessage(string message, string stack, LogType type)
    {
        float now = Time.realtimeSinceStartup;
        if (now - s_windowStart >= 1f)
        {
            s_windowStart = now;
            s_count = 0;
        }

        s_lastMessage = message;
        if (++s_count < MessagesPerSecondLimit) return;

        // Detach first, so the message below cannot re-enter this handler.
        Application.logMessageReceived -= OnMessage;
        Debug.LogError($"[TrailerLogGuard] Over {MessagesPerSecondLimit} console messages in one second — " +
                       $"logging is now OFF so this cannot take the editor down. " +
                       $"Last message was: \"{s_lastMessage}\". " +
                       $"Find whatever is writing that every frame; re-enter play mode to restore logging.");
        Debug.unityLogger.logEnabled = false;
    }
}
