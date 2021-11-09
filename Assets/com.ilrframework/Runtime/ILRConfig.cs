using System.IO;

namespace com.ilrframework.Runtime
{
    public static class ILRConfig
    {
        public const string BINDING_PATH = "Assets/Scripts/Framework/ILRuntime/Bindings/CLRBindings";
        public const string ADAPTOR_PATH = "Assets/Scripts/Framework/ILRuntime/Bindings/CrossBindingAdaptors";
        
        public const string DLL_FILE_NAME = "HotFix.dll";
        public const string PDB_FILE_NAME = "HotFix.pdb";

        public static string DllPath => Path.Combine("HotFix", $"{DLL_FILE_NAME}.bytes");

        public static string PdbPath => Path.Combine("HotFix", $"{PDB_FILE_NAME}.bytes");
    }
}
