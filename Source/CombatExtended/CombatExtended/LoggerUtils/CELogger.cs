using Verse;

namespace CombatExtended.CombatExtended.LoggerUtils
{
    class CELogger
    {
        /// <summary>
        /// Am I in debug mode? Set this to <c>true</c> and recompile for more log output.
        /// TODO: Maybe change this to a toggleable setting if I ever have the time to figure out how <3.
        /// </summary>
        private static readonly bool isInDebugMode = false;

        public static void Message(string message, bool showOutOfDebugMode = false, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (!isInDebugMode && !showOutOfDebugMode)
            {
                return;
            }
            Log.Message($"CE - {memberName}() ({Find.TickManager.TicksGame}) - {message}");
        }
        public static void Warn(string message, bool showOutOfDebugMode = false, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (!isInDebugMode && !showOutOfDebugMode)
            {
                return;
            }
            Log.Warning($"CE - {memberName}() ({Find.TickManager.TicksGame}) - {message}");
        }
        public static void Error(string message, bool showOutOfDebugMode = true, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (!isInDebugMode && !showOutOfDebugMode)
            {
                return;
            }
            Log.Error($"CE - {memberName}() ({Find.TickManager.TicksGame}) - {message}");
        }

        public static void ErrorOnce(string message, bool showOutOfDebugMode = true, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "", [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!isInDebugMode && !showOutOfDebugMode)
            {
                return;
            }
            Log.ErrorOnce($"CE - {memberName}() ({Find.TickManager.TicksGame}) - {message}", sourceLineNumber);
        }
    }
}
