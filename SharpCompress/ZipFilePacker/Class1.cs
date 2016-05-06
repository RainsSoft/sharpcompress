//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.IO;

//namespace ClassLibrary1 {
//    class Test
//    {
//        static void test() {
//            LFSFilePacker p = new LFSFilePacker { Name = "Test_Data" };
//            var sysfa = p.Get("Sys");
//            var tmpfa = p.Get("Tmp");

//            IRQ_SceneFile scene = new IRQ_SceneFile();
//            scene.Name = "test_s1";
//            scene.Data = new byte[1024];
//            scene.MiniMapData = new byte[2048];

//            FileService.Save<IRQ_SceneFile>(sysfa, scene, null);

//            var newscene = FileService.Load<IRQ_SceneFile>(sysfa, "test_s1", null);

//            IRQ_SceneFile scene2 = new IRQ_SceneFile();
//            scene2.Name = "test_s2";

//            scene2.Data = newscene.Data;
//            byte[] tmpp = new byte[newscene.Data.Length];
//            Array.Copy(newscene.Data, tmpp, tmpp.Length);
//            scene2.Data = tmpp;

//            scene2.MiniMapData = newscene.MiniMapData;

//            FileService.Save<IRQ_SceneFile>(sysfa, scene2, null);

//            byte[] mip = newscene.MiniMapData;
//            byte[] data = newscene.Data;



//            scene.Dispose();
//        }
//    }
//    public interface IFileInfo : IDisposable {
//        string Name {
//            get;
//            set;
//        }

//        /// <summary>
//        /// 实体文件列表，包括缩略图、默认机器人等，以及额外的一些资源文件。
//        /// </summary>
//        List<IRQ_ExtFile> ExtFile {
//            get;
//            set;
//        }
//        IFileSerializer Serializer {
//            get;
//        }
//    }

//    /// <summary>
//    /// 实体文件列表项
//    /// </summary>
//    public class IRQ_ExtFile {
//        public bool NeedSave {
//            get;
//            set;
//        }
//        public string FileName {
//            get;
//            set;
//        }
//        public string FullPath {
//            get;
//            set;
//        }
//    }
//    public class IRQ_VPLFile : IFileInfo {
//        #region IFileInfo 成员

//        public string Name {
//            get {
//                throw new NotImplementedException();
//            }
//            set {
//                throw new NotImplementedException();
//            }
//        }

//        public List<IRQ_ExtFile> ExtFile {
//            get {
//                throw new NotImplementedException();
//            }
//            set {
//                throw new NotImplementedException();
//            }
//        }

//        public IFileSerializer Serializer {
//            get { throw new NotImplementedException(); }
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    public class IRQ_RobotFile : IFileInfo {
//        #region IFileInfo 成员

//        public string Name {
//            get {
//                throw new NotImplementedException();
//            }
//            set {
//                throw new NotImplementedException();
//            }
//        }

//        public List<IRQ_ExtFile> ExtFile {
//            get {
//                throw new NotImplementedException();
//            }
//            set {
//                throw new NotImplementedException();
//            }
//        }

//        public IFileSerializer Serializer {
//            get { throw new NotImplementedException(); }
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    public class IRQ_SceneFile : IFileInfo {
//        private IFileSerializer m_serializer = new IRQ_SceneFile_ZipFileSerializer();
//        public IRQ_SceneFile() {
//            this.ExtFile = new List<IRQ_ExtFile>();
//        }
//        public byte[] MiniMapData {
//            get {
//                return m_serializer.GetExtFile("_minimap_.png");
//            }
//            set {
//                SaveTempAndAddToExtFileList("_minimap_.png", value);
//            }
//        }

//        public byte[] Data {
//            get {
//                return m_serializer.GetExtFile("_data_.json");
//            }
//            set {
//                SaveTempAndAddToExtFileList("_data_.json", value);
//            }
//        }




//        private void SaveTempAndAddToExtFileList(string key, byte[] value) {
//            this.ExtFile.RemoveAll(_ => _.FileName == key);
//            string fullpath = Utility.GetTempFile(this.GetHashCode().ToString(), key);
//            File.WriteAllBytes(fullpath, value);
//            this.ExtFile.Add(new IRQ_ExtFile { FileName = key, NeedSave = true, FullPath = fullpath });

//        }

//        #region IFileInfo 成员

//        public List<IRQ_ExtFile> ExtFile {
//            get;
//            set;
//        }



//        public string Name {
//            get;
//            set;
//        }

//        #endregion




//        #region IFileInfo 成员


//        public IFileSerializer Serializer {
//            get { return m_serializer; }
//        }

//        #endregion


//        #region IDisposable 成员
//        private bool m_Disposed = false;
//        public void Dispose() {
//            if (m_serializer != null) {
//                m_serializer.Dispose();
//            }
//            //删除缓存文件
//            string tmpdir = Utility.GetTempPath(this.GetHashCode().ToString());
//            if (Directory.Exists(tmpdir)) {
//                Directory.Delete(tmpdir, true);
//            }
//            m_Disposed = true;
//            GC.SuppressFinalize(this);
//        }
//        ~IRQ_SceneFile() {
//            if (m_Disposed == false) {
//                Dispose();
//            }
//        }
//        #endregion
//    }

//    /// <summary>
//    /// 普通的各种文件
//    /// </summary>
//    public class IRQ_NormalFile : IFileInfo {

//        #region IFileInfo 成员

//        public string Name {
//            get {
//                throw new NotImplementedException();
//            }
//            set {
//                throw new NotImplementedException();
//            }
//        }

//        public List<IRQ_ExtFile> ExtFile {
//            get {
//                throw new NotImplementedException();
//            }
//            set {
//                throw new NotImplementedException();
//            }
//        }

//        public IFileSerializer Serializer {
//            get { throw new NotImplementedException(); }
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    public interface IFilePacker {
//        string Name {
//            get;
//            set;
//        }

//        IFileAccess Get(string name);

//    }
//    public interface IFileAccess {
//        string Name {
//            get;
//            set;
//        }
//        IFilePacker Packer {
//            get;
//            set;
//        }
//        void AddOrUpdate(IFileInfo file, byte[] data, DateTime dt);
//        byte[] GetFile(string filename);
//        void DelFile(string fileanme);
//        void DelAll();
//        List<IFileInfo> GetFileList();
//        event Func<string, int, bool> Process;
//    }

//    /// <summary>
//    /// 本地基于目录和zip的文件系统
//    /// </summary>
//    public class LFSFilePacker : IFilePacker {
//        Dictionary<string, IFileAccess> m_dict = new Dictionary<string, IFileAccess>();
//        #region IFilePacker 成员

//        public string Name {
//            get;
//            set;
//        }

//        public IFileAccess Get(string name) {
//            IFileAccess ret;
//            if (m_dict.TryGetValue(name, out ret) == false) {
//                ret = new LFSFileAccess(name, this);
//                m_dict.Add(name, ret);
//            }

//            return ret;
//        }

//        #endregion
//    }

//    /// <summary>
//    /// 本地基于目录的文件访问
//    /// </summary>
//    public class LFSFileAccess : IFileAccess {
//        private List<IFileInfo> m_list = new List<IFileInfo>();
//        private string IndexFile = "index.json";

//        private string m_dir;
//        internal LFSFileAccess(string name, IFilePacker packer) {
//            this.Name = name;
//            this.Packer = packer;
//            m_dir = Path.Combine(packer.Name, name);
//            if (Directory.Exists(m_dir) == false) {
//                Directory.CreateDirectory(m_dir);
//            }
//        }
//        #region IFileAccess 成员

//        public string Name {
//            get;
//            set;
//        }

//        public IFilePacker Packer {
//            get;
//            set;
//        }

//        public void AddOrUpdate(IFileInfo file, byte[] data, DateTime dt) {
//            File.WriteAllBytes(Path.Combine(m_dir, file.Name), data);
//            //写索引
//            m_list.Add(file);
//            SaveList();
//        }



//        public byte[] GetFile(string filename) {
//            return File.ReadAllBytes(Path.Combine(m_dir, filename));
//        }

//        public void DelFile(string filename) {
//            File.Delete(Path.Combine(m_dir, filename));
//        }

//        public void DelAll() {

//        }

//        public event Func<string, int, bool> Process;




//        public List<IFileInfo> GetFileList() {
//            //从索引文件中读取列表
//            //使用json整体序列化

//            throw new NotImplementedException();
//        }
//        private void SaveList() {
//            //json序列化Index
//        }
//        #endregion
//    }

//    public class HTTPFilePacker : IFilePacker {

//        #region IFilePacker 成员

//        public string Name {
//            get;
//            set;
//        }

//        public IFileAccess Get(string name) {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    public class HTTPFileAccess : IFileAccess {

//        #region IFileAccess 成员

//        public string Name {
//            get;
//            set;
//        }

//        public IFilePacker Packer {
//            get;
//            set;
//        }


//        public void AddOrUpdate(IFileInfo file, byte[] data, DateTime dt) {
//            string url = Packer.Name + "&op=post&name=" + Name + "&file=" + file.Name;
//            Utility.HttpPost(url, data);

//        }

//        public byte[] GetFile(string filename) {
//            throw new NotImplementedException();
//        }

//        public void DelFile(string fileanme) {

//        }

//        public void DelAll() {
//            string url = Packer.Name + "&op=delall&name=" + this.Name;

//        }

//        public event Func<string, int, bool> Process;




//        public List<IFileInfo> GetFileList() {
//            throw new NotImplementedException();
//        }

//        #endregion


//    }

//    /// <summary>
//    /// 文件解析/序列化接口
//    /// </summary>
//    public interface IFileSerializer : IDisposable {


//        void LightDeSerialize(IFileInfo obj, byte[] data);

//        byte[] GetExtFile(string key);

//        byte[] Serialize(IFileInfo obj);
//    }


//    //public static class FileSerializer {
//    //    public static IFileSerializer<T> Get<T>(T obj) where T : IFileInfo {

//    //    }
//    //    public static IFileSerializer<IRQ_VPLFile> VPL;
//    //    public static IFileSerializer<IRQ_RobotFile> Robot;
//    //    public static IFileSerializer<IRQ_NormalFile> Normal;
//    //}


//    public class IRQ_VPLFile_ZipFileSerializer : IFileSerializer {

//        #region IFileSerializer<IRQ_RobotFile> 成员

//        public byte[] GetExtFile(string filename) {
//            throw new NotImplementedException();
//        }

//        public byte[] Serialize(IRQ_RobotFile obj) {
//            throw new NotImplementedException();
//        }

//        #endregion

//        #region IFileSerializer<IRQ_VPLFile> 成员

//        public void LightDeSerialize(IFileInfo obj, byte[] data) {
//            throw new NotImplementedException();
//        }

//        public byte[] Serialize(IFileInfo obj) {
//            throw new NotImplementedException();
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    public class IRQ_RobotFile_ZipFileSerializer : IFileSerializer {

//        #region IFileSerializer<IRQ_RobotFile> 成员

//        public void LightDeSerialize(IFileInfo obj, byte[] data) {
//            throw new NotImplementedException();
//        }

//        public byte[] GetExtFile(string key) {
//            throw new NotImplementedException();
//        }

//        public byte[] Serialize(IFileInfo obj) {
//            throw new NotImplementedException();
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    /// <summary>
//    /// 基于zip的场景文件解析/序列化
//    /// </summary>
//    public class IRQ_SceneFile_ZipFileSerializer : IFileSerializer {
//        string m_temp;
//        #region IFileSerializer<IRQ_SceneFile> 成员



//        public byte[] GetExtFile(string key) {
//            //从缓存文件中获取指定数据
//            return Utility.Zip_GetFileData(m_temp, key);
//        }

//        public byte[] Serialize(IFileInfo obj) {
//            string zipfile = Utility.GetTempFile(Guid.NewGuid().ToString("N"));
//            try {
//                //增加一些属性

//                //增加文件
//                foreach (var v in obj.ExtFile) {
//                    if (v.NeedSave) {
//                        Utility.Zip_AddFile(zipfile, v.FileName, File.ReadAllBytes(v.FullPath));
//                    }
//                }
//                return File.ReadAllBytes(zipfile);
//            }
//            finally {
//                if (File.Exists(zipfile)) {
//                    File.Delete(zipfile);
//                }
//            }
//        }

//        #endregion


//        #region IFileSerializer<IRQ_SceneFile> 成员

//        public void LightDeSerialize(IFileInfo obj, byte[] data) {
//            m_temp = Utility.GetTempFile(Guid.NewGuid().ToString("N"));
//            //返回轻量对象，不包含byte数据,并缓存data到文件
//            File.WriteAllBytes(m_temp, data);
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            if (File.Exists(m_temp)) {
//                File.Delete(m_temp);
//            }

//        }

//        #endregion
//    }



//    public class IRQ_NormalFile_BinaryFileSerializer : IFileSerializer {

//        #region IFileSerializer<IRQ_NormalFile> 成员


//        public byte[] Serialize(IFileInfo obj) {
//            throw new NotImplementedException();
//        }



//        public void LightDeSerialize(IFileInfo obj, byte[] data) {
//            throw new NotImplementedException();
//        }

//        public byte[] GetExtFile(string key) {
//            throw new NotImplementedException();
//        }

//        #endregion

//        #region IDisposable 成员

//        public void Dispose() {
//            throw new NotImplementedException();
//        }

//        #endregion
//    }

//    public static class Utility {
//        static string TEMP {
//            get {
//#if UNITY3D
//#else
//                string dir = Path.Combine(Path.GetTempPath(), "IRobotQ3D/Temp");
//                if (Directory.Exists(Path.Combine(Path.GetTempPath(), "IRobotQ3D")) == false) {
//                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "IRobotQ3D"));
//                }
//                if (Directory.Exists(dir) == false) {
//                    Directory.CreateDirectory(dir);
//                }
//                return dir;
//#endif
//            }
//        }
//        public static string GetTempFile(string filename) {
//            //获取一个临时目录
//            return Path.Combine(TEMP, filename);


//        }
//        public static string GetTempFile(string dir, string filename) {
//            //获取一个临时目录
//            string dir1 = Path.Combine(TEMP, dir);
//            if (Directory.Exists(dir1) == false) {
//                Directory.CreateDirectory(dir1);
//            }
//            return Path.Combine(dir1, filename);
//        }
//        public static string GetTempPath(string dir) {
//            return Path.Combine(TEMP, dir);
//        }

//        public static byte[] Zip_GetFileData(string zipfile, string key) {
//            SharpCompress.Archive.Zip.ZipArchive zip = SharpCompress.Archive.Zip.ZipArchive.Open(zipfile, "");
//            var z = zip[key];
//            if (z != null) {
//                MemoryStream ms = new MemoryStream();
//                Stream stream = z.OpenEntryStream();
//                byte[] buf = new byte[32 * 1024];
//                while (true) {
//                    int read = stream.Read(buf, 0, buf.Length);
//                    if (read == 0) {
//                        break;
//                    }
//                    ms.Write(buf, 0, read);
//                }
//                return ms.ToArray();
//            }
//            return null;
//        }
//        public static void Zip_AddFile(string zipfile, string key, byte[] data) {
//            SharpCompress.Archive.Zip.ZipArchive zip;
//            if (File.Exists(zipfile) == false) {
//                zip = SharpCompress.Archive.Zip.ZipArchive.Create();
//            }
//            else {
//                zip = SharpCompress.Archive.Zip.ZipArchive.Open(zipfile, "");
//            }
//            zip.AddEntry(key, new MemoryStream(data), data.Length, null);
//            using (MemoryStream ms = new MemoryStream()) {
//                var type = new SharpCompress.Common.CompressionInfo();
//                type.Type = SharpCompress.Common.CompressionType.Deflate;
//                zip.SaveTo(ms, type);
//                zip.Dispose();
//                File.WriteAllBytes(zipfile, ms.ToArray());
//            }
//        }

//        internal static void HttpPost(string url, byte[] data) {
//            throw new NotImplementedException();
//        }
//    }


//    /// <summary>
//    /// 文件访问服务
//    /// </summary>
//    public static class FileService {
//        public static T Load<T>(IFileAccess fa, string filename, Func<string, int, bool> cb) where T : IFileInfo, new() {
//            if (cb != null) {
//                fa.Process += cb;
//            }
//            byte[] buf = fa.GetFile(filename);
//            if (cb != null) {
//                fa.Process -= cb;
//            }
//            if (buf == null) {
//                return default(T);
//            }

//            var f = new T();
//            f.Serializer.LightDeSerialize(f, buf);
//            return f;
//        }
//        public static List<IFileInfo> LoadList(IFileAccess fa) {
//            return fa.GetFileList();
//        }

//        public static void Save<T>(IFileAccess fa, T f, Func<string, int, bool> cb) where T : IFileInfo, new() {
//            if (cb != null) {
//                fa.Process += cb;
//            }
//            byte[] buf = f.Serializer.Serialize(f);
//            fa.AddOrUpdate(f, buf, DateTime.Now);

//            if (cb != null) {
//                fa.Process -= cb;
//            }

//        }
//    }
//    public delegate TResult Func<U, V, TResult>(U u, V v);
//}
