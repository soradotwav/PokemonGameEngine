﻿using Kermalis.PokemonBattleEngine.Utils;
using System.IO;
using System.Reflection;

namespace Kermalis.PokemonGameEngine.Util
{
    internal static class Utils
    {
        private const string AssemblyPrefix = "Kermalis.PokemonGameEngine.Assets.";
        private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
        public static Stream GetResourceStream(string resource)
        {
            return _assembly.GetManifestResourceStream(AssemblyPrefix + resource);
        }

        public static string WorkingDirectory { get; private set; }
        public static void SetWorkingDirectory(string workingDirectory)
        {
            PBEUtils.InitEngine(workingDirectory);
            WorkingDirectory = workingDirectory;
        }
    }
}
