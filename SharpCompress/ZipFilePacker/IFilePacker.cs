namespace IRobotQ.Packer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    /// <summary>
    /// 文件基础信息接口
    /// </summary>
    public interface IFileEntryInfo
    {

        /// <summary>
        /// 目录名,相对与自己压缩包的根路径
        /// </summary>
        string FileDir { get; set; }
        /// <summary>
        /// 文件名,不包含目录名
        /// </summary>
        string FileName { get; set; }
        /// <summary>
        /// 未压缩文件长度
        /// </summary>
        int FileLen { get; set; }
        /// <summary>
        /// 文件修改时间 FileTimeUtc,来源不是压缩文件内容
        /// </summary>
        string FileUpdateTime { get; set; }


    }
    /// <summary>
    /// 文件列表接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFilesTableInfo<T> where T : IFileEntryInfo
    {
        /// <summary>
        /// 数据表（目录名）(存储的时候需要规范化)
        /// </summary>
        string FileTableName { get; set; }
        /// <summary>
        /// 文件个数
        /// </summary>
        int FileCount { get; set; }
        int TotalSize { get; set; }
        /// <summary>
        /// 文件列表,内容关联
        /// </summary>
        List<T> FileList { get; set; }
    }

    /// <summary>
    /// 文件基础信息,所有的文件接口都要继承当前类
    /// </summary>
    public class FileEntryInfo : IFileEntryInfo
    {
        /// <summary>
        /// 目录名
        /// </summary>
        public string FileDir { get; set; }
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 文件长度
        /// </summary>
        public int FileLen { get; set; }
        /// <summary>
        /// 文件修改时间
        /// </summary>
        public string FileUpdateTime { get; set; }

        #region method help 1
        /// <summary>
        /// 字符转 Time
        /// </summary>
        /// <param name="fileUpdateUTCTime"></param>
        /// <returns></returns>
        public DateTime DateTimeFromStr(string fileUpdateUTCTime) {
            return DateTimeFromStr_STC(fileUpdateUTCTime);
        }
        public static DateTime DateTimeFromStr_STC(string fileUpdateUTCTime) {
            return DateTime.Parse(fileUpdateUTCTime);
        }
        /// <summary>
        /// Time 转字符
        /// </summary>
        /// <param name="fileUpdateUTCTime"></param>
        /// <returns></returns>
        public string DateTimeToStr(DateTime fileUpdateUTCTime) {
            return DateTimeToStr_STC(fileUpdateUTCTime);
        }
        public static string DateTimeToStr_STC(DateTime fileUpdateUTCTime) {
            return fileUpdateUTCTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        #endregion

        #region method help 2
        /// <summary>
        /// 获取规范的小写目录（带路径文件）名称:
        /// 不以'/'开头和结尾
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetDirPathToLowerNorm(string path) {
            return GetDirPathToLowerNorm_STC(path);
        }
        /// <summary>
        /// 移除 开头和结尾'/'
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetDirPathToLowerNorm_STC(string path) {
            if (string.IsNullOrEmpty(path)) return "";
            string pp = path.Replace("\\", "/");
            pp = pp.Replace("//", "/");
            pp = pp.EndsWith("/") ? pp.Remove(pp.Length - 1) : pp;
            if (pp.StartsWith("/")) {
                pp = pp.Substring(1);
            }
            return pp.ToLower();
        }
        /// <summary>
        /// 深度递归检测目录是否存在，不存在则依次创建目录
        /// </summary>
        /// <param name="path"></param>
        public void CheckDirectory(string path) {
            CheckDirectory_STC(path);
        }
        public static void CheckDirectory_STC(string path) {
            //ToDo:
            bool create = true;
            if (Directory.Exists(path) || string.IsNullOrEmpty(path) || path.EndsWith(":")) {
                //c:
                //file:
                create = false;
                return;
            }
            //if (RuntimePlatform.Android == Application.platform) {
            ////不同的平台 位置路径是不是要设定下？
            //string pathA = _getFileLegalLowerDir("jar:file://" + Application.dataPath);
            //string path2 = _getFileLegalLowerDir(path);
            //string path1 = _getFileLegalLowerDir(Application.dataPath);
            //if (string.Compare(pathA, path2, true) == 0) {
            //    create = false;
            //}
            //else if (string.Compare(path1, path2, true) == 0) {
            //    create = false;
            //}
            //}
            // else {
            //string path1 = _getFileLegalLowerDir(Application.dataPath);
            //string path2 = _getFileLegalLowerDir(path);
            //string pathB = _getFileLegalLowerDir("file://" + Application.dataPath);
            //if (string.Compare(pathB, path2, true) == 0) {
            //    create = false;
            //}
            //else if (string.Compare(path1, path2, true) == 0) {
            //    create = false;
            //}
            //}
            //
            if (!create) {
                return;
            }
            else {
                string dir = Path.GetDirectoryName(path);
                CheckDirectory_STC(dir);
                Directory.CreateDirectory(path);//最底层的
            }
        }
        /// <summary>
        /// 用过滤器过滤后的有效文件名(路径)
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string GetValidPathByFillter(string path) {
            return GetValidPathByFillter_STC(path);
        }
        public static string GetValidPathByFillter_STC(string path) {
            //ToDo: sql字符注入过滤
            return path;
        }
        /// <summary>
        /// 获取 带路径的文件名的最顶层目录名
        /// </summary>
        /// <param name="strFilePath"></param>
        /// <param name="remain">输出相对最顶层的带路径的文件名</param>
        /// <returns></returns>
        public string GetFirstDir(string strFilePath, out string remain) {
            return GetFirstDir_STC(strFilePath, out remain);
        }

        public static string GetFirstDir_STC(string strFilePath, out string remain) {
            string tmp1 = Path.GetDirectoryName(strFilePath);
            tmp1 = tmp1.Replace("\\", "/");
            tmp1 = tmp1.Replace("//", "/");
            string tmp2 = Path.GetFileName(strFilePath);
            //tmp2 = tmp2.Replace("\\", "/");
            //tmp2 = tmp2.Replace("//", "/");
            strFilePath = tmp1 + "/" + tmp2;
            int p = strFilePath.IndexOf("/");
            if (p == -1) {
                remain = strFilePath;
                return "";
            }
            string ret = strFilePath.Substring(0, p);
            if (!string.IsNullOrEmpty(ret)) ret = ret.ToLower();
            remain = strFilePath.Substring(p + 1, strFilePath.Length - p - 1);
            if (!string.IsNullOrEmpty(remain)) remain = remain.ToLower();
            return ret;
        }
        #endregion
    }
    /// <summary>
    /// 文件包接口
    /// </summary>
    public interface IFilePacker
    {
        IFilePackerStrategy AddFileTable(string name);
        IFilePackerStrategy GetFileTable(string name);
        void DelFileTable(string name);
        void RenameFileTable(string tableName, string newName);
        int GetFileTableList(out List<string> ret);
        bool IsTableExists(string name);
        void BeginUpdate(IFilePackerStrategy file);
        void BeginUpdate(string name);
        void EndUpdate(IFilePackerStrategy file, bool success);
        void EndUpdate(string name, bool success);
        void Close();
    }

    /// <summary>
    /// 文件访问接口
    /// </summary>
    public interface IFilePackerStrategy
    {

        IFilePacker Packer {
            get;
        }
        [Obsolete("不支持文件路径的隐式增加")]
        void AddFile(string strFileName);
        [Obsolete("不支持文件路径的隐式增加")]

        #region addfile
        void AddFile(string strFileName, DateTime date);
        void AddFile(string strFileName, System.IO.Stream stream);
        void AddFile(string strFileName, byte[] fileData);
        void AddFile(string strFileName, System.IO.Stream stream, DateTime date);
        /// <summary>
        /// 有限的支持，在数据库sqlite,litedb等中可加入（iszip,zipjson）字段保存压缩包内的内容，对于文件系统FILE,ZIP则不支持，否则开发状态数据的维护比较麻烦
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="stream"></param>
        /// <param name="date"></param>
        void AddZipFile(string strFileName, System.IO.Stream stream, DateTime date);
        void AddFile(string strFileName, byte[] fileData, DateTime date);
        #endregion

        void RenameFile(string strFileName, string strNewFile);
        void RenameDir(string strDirName, string strNewDirName);

        [Obsolete("不支持文件路径的隐式增加")]
        void UpdateFile(string strFileName);
        [Obsolete("不支持文件路径的隐式增加")]
        void UpdateFile(string strFileName, DateTime date);

        #region updatefile
        void UpdateFile(string strFileName, System.IO.Stream stream);
        void UpdateFile(string strFileName, byte[] fileData);
        void UpdateFile(string strFileName, System.IO.Stream stream, DateTime date);
        void UpdateFile(string strFileName, byte[] fileData, DateTime date);
        /// <summary>
        /// 有限的支持，在数据库sqlite,litedb等中可加入（iszip,zipjson）字段保存压缩包内的内容，对于文件系统FILE,ZIP则不支持，否则开发状态数据的维护比较麻烦
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="fileData"></param>
        /// <param name="date"></param>
        void UpdateZipFile(string strFileName, byte[] fileData, DateTime date);
        #endregion

        void DelDir(string strDir);
        void DelFile(string strFileName);
        bool FileExists(string strFileName);
        DateTime GetUpdateDate(string strFileName);
        int GetFiles(string strDir, out List<string> fileNames, out int totalSize);
        int GetDirs(out List<string> dirs);
        /// <summary>
        /// 对应的filetableName名称。
        /// </summary>
        string Name { get; set; }
        byte[] OpenFile(string strFileName);
        Stream OpenFileAsStream(string strFileName);
        string OpenFileAsString(string strFileName);
        void Clean();
        /// <summary>
        /// 关闭打开占用
        /// </summary>
        void Close();
    }
}
