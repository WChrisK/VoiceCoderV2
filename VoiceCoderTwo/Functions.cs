using System;
using System.Linq;
using System.Reflection;

namespace VoiceCoderTwo
{
    /// <summary>
    /// A list of functions that can be called. These should all be public and
    /// static to let reflection find them.
    /// </summary>
    public static class Functions
    {
        public static void VoiceCodeMute(string[] words)
        {
            EntryPoint.HaltVoiceCoder = words[2] != "start";
        }

        public static void ChangeMode(string[] words)
        {
            EntryPoint.ChangeMode(words[2]);
        }

        public static void ExitMode(string[] words)
        {
            EntryPoint.ExitMode();
        }

        public static void SwitchSiblingMode(string[] words)
        {
            ExitMode(words);
            ChangeMode(words);
        }

        public static Action<string[]>? GetCallable(string funcName)
        {
            try
            {
                foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(c => c.Name == nameof(Functions)))
                {
                    MethodInfo info = type.GetMethod(funcName, BindingFlags.Public | BindingFlags.Static);
                    if (info != null && info.Name != nameof(GetCallable))
                        return args => info.Invoke(null, new object[] { args });
                }
            }
            catch
            {
                // Want to fail silently.
            }

            return null;
        }
    }
}
