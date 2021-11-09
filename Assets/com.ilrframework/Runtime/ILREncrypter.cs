using System.IO;

namespace com.ilrframework.Runtime
{
    // TODO: 正式的加解密，现在只是临时的取反运算
    public static class ILREncrypter
    {
        /// <summary>
        /// 对热更文件进行加密
        /// </summary>
        /// <param name="originalFilePath"></param>
        /// <param name="outputFilePath"></param>
        public static void EncryptHotFixFile(string originalFilePath, string outputFilePath) {
            //using (var originalStream = File.OpenRead(originalFilePath))
            //using (var outputStream = File.Create(outputFilePath)) {
            //    using (var binaryReader = new BinaryReader(originalStream)) {
            //        var fileData = binaryReader.ReadBytes((int)originalStream.Length);
            //        for (var i = 0; i < fileData.Length; i++) {
            //            outputStream.Position = i;

            //            var b = fileData[i];
            //            var neg = (byte) ~b;
                    
            //            outputStream.WriteByte(neg);
            //        }
            //        outputStream.Flush(true);
            //    }
            //}

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }
            File.Copy(originalFilePath, outputFilePath);
        }

        /// <summary>
        /// 对热更文件进行解密
        /// </summary>
        /// <param name="encryptBytes"></param>
        /// <returns></returns>
        public static byte[] DecryptHotFixBytes(byte[] encryptBytes) {
            var len = encryptBytes.Length;
            var ret = new byte[len];
            
            for (var i = 0; i < len; i++) {
                //var neg = encryptBytes[i];
                //var b = (byte) ~neg;
                //ret[i] = b;
                ret[i] = encryptBytes[i];
            }
            
            return ret;
        }
    }
}