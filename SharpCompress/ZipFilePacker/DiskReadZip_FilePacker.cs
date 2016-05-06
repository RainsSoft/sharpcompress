namespace IRobotQ.PackerDisk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using IRobotQ.Packer;
    using SharpCompress;
    //由于subnet的fileinfo没有修改时间等基本信息，所以原有的文件系统不太适应，使用压缩文件为存储
    //基于磁盘的文件系统(压缩) 
    public class DiskReadZip_FilePacker : IFilePacker
    {
        //文件分为2级 一个是packer，一个是table
        /// <summary>
        /// 根目录,根目录下创建多个数据库，一个库对应一张数据表
        /// </summary>
        public string RootDir { get; private set; }
        /// <summary>
        /// 数据库设计结构为：一张表对应一个数据库文件
        /// 这样就能使用对象来存储数据了
        /// </summary>
        private Dictionary<string, DiskReadZip_ConnectInfo> m_Conns = new Dictionary<string, DiskReadZip_ConnectInfo>();
        protected DiskReadZip_FilePacker(string rootDir) {
            this.RootDir = _getFileLegalLowerDir(rootDir);
            if (!Directory.Exists(rootDir)) {
                CheckDirectory(rootDir);
            }
        }
        internal static DiskReadZip_FilePacker OpenPacker(string rootDir) {
            DiskReadZip_FilePacker packer = new DiskReadZip_FilePacker(rootDir);
            return packer;
        }
        /// <summary>
        /// 检测path是否存在,如果不存在,会按序依次创建
        /// </summary>
        /// <param name="path">不带文件名的路径</param>
        public static void CheckDirectory(string path) {
            //string dir = Path.GetDirectoryName(path);
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
                CheckDirectory(dir);
                Directory.CreateDirectory(path);//最底层的
            }
        }
        /// <summary>
        /// 取得内部使用的规范 dir name 不以/ \ 开头和结尾,并转成小写
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static string _getFileLegalLowerDir(string path) {
            return FileEntryInfo.GetDirPathToLowerNorm_STC(path);
            //if (string.IsNullOrEmpty(path)) return "";
            //string pp = path.Replace("\\", "/");
            //pp = pp.Replace("//", "/");
            //pp = pp.EndsWith("/") ? pp.Remove(pp.Length - 1) : pp;
            //if (pp.StartsWith("/")) {
            //    pp = pp.Substring(1);
            //}
            //return pp.ToLower();
        }
        /// <summary>
        /// 对sql中要用的查询字符进行非法自律过滤
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string CheckSQLString(string str) {
            str = str.Replace("_", "");   // 过滤SQL注入_
            str = str.Replace("*", "");   //过滤SQL注入*
            str = str.Replace(" ", "");   //过滤SQL注入空格
            str = str.Replace(((char)34).ToString(), "");         //过滤SQL注入"
            str = str.Replace(((char)39).ToString(), "");         //过滤SQL注入'
            str = str.Replace(((char)91).ToString(), "");         //过滤SQL注入[
            str = str.Replace(((char)93).ToString(), "");         //过滤SQL注入]
            str = str.Replace(((char)37).ToString(), "");         //过滤SQL注入%
            str = str.Replace(((char)58).ToString(), "");         //过滤SQL注入:
            str = str.Replace(((char)59).ToString(), "");         //过滤SQL注入;
            str = str.Replace(((char)43).ToString(), "");         //过滤SQL注入+
            str = str.Replace("{", "");       //过滤SQL注入{
            str = str.Replace("}", "");       //过滤SQL注入}
            str = str.Replace("/", "");       //过滤SQL注入/
            str = str.Replace("'", " ");
            str = str.Replace(";", " ");
            str = str.Replace("1=1", " ");
            str = str.Replace("|", " ");
            str = str.Replace("<", " ");
            str = str.Replace(">", " ");
            return str;

        }

        const string TableDBExtension = ".z";
        /// <summary>
        /// 检测压缩文件是否存在，如果不存在则创建文件并加入表中，返回连接.z文件对象
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        internal DiskReadZip_ConnectInfo CheckConnection(string tableName) {

            if (!m_Conns.ContainsKey(tableName)) {
                //创建
                string dbname = _GetTableFileFullPath(tableName);
                bool fileExist = File.Exists(dbname);//存在文件就直接打开,否则创建
                //conn = new SQLiteConnection(dbname, fileExist ? SQLiteOpenFlags.ReadWrite : (SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create));
                if (fileExist) {
                    //检测是否为压缩文件
                    using (FileStream fst = new FileStream(dbname, FileMode.Open, FileAccess.Read)) {
                        fileExist = SharpCompress.Archive.Zip.ZipArchive.IsZipFile(fst);//非压缩文件
                        //压缩文件系统 没必要一直处于打开状态吧 用完销毁                        
                    }
                }
                if (!fileExist) {
                    using (SharpCompress.Archive.Zip.ZipArchive con = SharpCompress.Archive.Zip.ZipArchive.Create()) {
                        using (FileStream fst = new FileStream(dbname, FileMode.Create, FileAccess.Write)) {
                            con.SaveTo(fst, new SharpCompress.Common.CompressionInfo());//保存完毕
                        }
                    }
                }
                //
                //FileStream fs = new FileStream(dbname, FileMode.Open, FileAccess.ReadWrite);
                //conn =  SharpCompress.Archive.Zip.ZipArchive.Open(fs);
                DiskReadZip_ConnectInfo ci = new DiskReadZip_ConnectInfo();
                ci.ConnString = dbname;
                ci.Name = tableName;
                m_Conns.Add(tableName, ci);//添加在数据表中
                //

            }

            //打开,用的时候再打开吧
            //m_Conns[tableName].Open();

            return m_Conns[tableName];
        }

        /// <summary>
        /// tableName 支持[a-F]以及[_],内部最好别带 . / \\ ?等非法字符
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public IFilePackerStrategy AddFileTable(string tableName) {
            if (string.IsNullOrEmpty(tableName)) {
                throw new ArgumentNullException(tableName);
            }
            try {

                if (IsTableExists(tableName)) throw new DiskZip_AccessPackerException(tableName + "已经存在", null);

                DiskReadZip_ConnectInfo m_Conn = CheckConnection(tableName);
                IFilePackerStrategy ret = new DiskReadZip_FileTable(this, m_Conn);
                ret.Name = tableName;
                //建立SQLite_FileItemInfo结构的表名
                //m_Conn.CreateTable<SQLite_FileItemInfo>();
                return ret;
            }
            catch (Exception ee) {
                throw new DiskZip_AccessPackerException("访问文件目录时发生错误", ee);
            }
            return null;
        }

        public IFilePackerStrategy GetFileTable(string tableName) {
            if (string.IsNullOrEmpty(tableName)) {
                throw new ArgumentNullException(tableName);
            }
            if (!IsTableExists(tableName)) return null;
            DiskReadZip_ConnectInfo m_Conn = CheckConnection(tableName);

            IFilePackerStrategy ret = new DiskReadZip_FileTable(this, m_Conn);
            ret.Name = tableName;
            return ret;
        }

        public void DelFileTable(string tableName) {
            if (string.IsNullOrEmpty(tableName)) {
                throw new ArgumentNullException(tableName);
            }
            try {
                //string tbName = tableName.ToLower();
                if (!IsTableExists(tableName)) {
                    if (m_Conns.ContainsKey(tableName)) {
                        m_Conns.Remove(tableName);
                    }
                    return;
                }
                //
                if (m_Conns.ContainsKey(tableName)) {
                    if (m_Conns[tableName] != null && m_Conns[tableName].IsOpen) {
                        m_Conns[tableName].Close();
                    }
                    m_Conns.Remove(tableName);
                }
                File.Delete(_GetTableFileFullPath(tableName));//删除文件
            }
            catch (Exception ee) {
                throw new DiskZip_AccessPackerException("DelFileTable操作时发生错误:", ee);
                System.Diagnostics.Debug.Assert(false, "DelFileTable操作时发生错误:" + ee.ToString());
            }
        }
        /// <summary>
        /// 一般不使用
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="newName"></param>
        public void RenameFileTable(string tableName, string newName) {
            if (string.IsNullOrEmpty(tableName)) {
                throw new ArgumentNullException(tableName);
            }
            if (string.IsNullOrEmpty(newName)) {
                throw new ArgumentNullException(tableName);
            }
            if (string.Compare(tableName, newName, true) == 0) return;//没有更改，返回
            try {
                if (!IsTableExists(tableName)) return;
                DiskReadZip_ConnectInfo m_Conn = CheckConnection(tableName);
                m_Conn.Save();//保存一下
                if (IsTableExists(newName)) {
                    throw new DiskZip_AccessPackerException("重命名失败，newName已经存在!", null);
                    DelFileTable(newName);//覆盖吗？
                }
                //移除老的
                //
                File.Copy(_GetTableFileFullPath(tableName), _GetTableFileFullPath(newName), true);
                DelFileTable(tableName);//删除老文件
                //新文件要外面自己打开
            }
            catch (Exception ee) {
                throw new DiskZip_AccessPackerException("访问文件目录时发生错误", ee);
            }
        }

        public int GetFileTableList(out List<string> ret) {
            string[] tables = Directory.GetFiles(RootDir);
            ret = new List<string>();
            foreach (var v in tables) {
                ret.Add(Path.GetFileNameWithoutExtension(v));//表名不带后缀
            }
            return ret.Count;
        }
        string _GetTableFileFullPath(string tableName) {
            return RootDir + "/" + tableName + TableDBExtension;
        }
        public bool IsTableExists(string tableName) {
            string tablefile = _GetTableFileFullPath(tableName);
            return File.Exists(tablefile);
        }
        /// <summary>
        /// 打开压缩包
        /// </summary>
        public void BeginUpdate(IFilePackerStrategy file) {
            string tableName = file.Name;
            BeginUpdate(tableName);
        }
        //
        /// <summary>
        /// 打开压缩包
        /// </summary>
        /// <param name="tableName"></param>
        public void BeginUpdate(string tableName) {
            DiskReadZip_ConnectInfo conn = CheckConnection(tableName);
            //conn.BeginTransaction();
            conn.Open();
        }
        /// <summary>
        /// 保存压缩包
        /// </summary>
        /// <param name="file"></param>
        /// <param name="success">false：放弃修改，直接关闭。true：先保存到缓存，完成后覆盖到当前</param>
        public void EndUpdate(IFilePackerStrategy file, bool success) {
            string tableName = file.Name;
            EndUpdate(tableName, success);
        }
        /// <summary>
        /// 保存压缩包，中途不要保存
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="success">false：放弃修改，直接关闭。true：先保存到缓存，完成后覆盖到当前</param>
        public void EndUpdate(string tableName, bool success) {
            DiskReadZip_ConnectInfo conn = CheckConnection(tableName);
            if (success) {
                conn.Save();//代价是否大了点?             
            }
            else {
                //conn.Rollback();//放弃 重新打开
                conn.Close();//放弃,关闭
            }
            //(file as IRQ_FileTable).m_trans = null;
        }
        /// <summary>
        /// 关闭所有的已经打开压缩包，注意：里面不执行保存
        /// 需要保存请执行EndUpdate(...)
        /// </summary>
        public void Close() {
            foreach (var m_Conn in m_Conns.Values) {
                if (m_Conn != null && m_Conn.IsOpen) {
                    m_Conn.Close();
                }
            }
        }
    }
    /// <summary>
    /// .z文件相对路径
    /// "data source=" + strFileName + ";Compress=True;";
    /// </summary>  
    public class DiskReadZip_ConnectInfo
    {
        //以.z结尾
        /// <summary>
        /// 对应的.z文件全路径
        /// </summary>
        public string ConnString;
        /// <summary>
        /// tablename
        /// </summary>
        public string Name;
        /// <summary>
        /// 对应的数据库名(不带扩展名)
        /// </summary>
        public string TableName {
            get {
                string tn = Path.GetFileNameWithoutExtension(ConnString);
                System.Diagnostics.Debug.Assert(tn == Name, "Name != TableName (DiskZip)");
                return tn;
            }
        }
        /// <summary>
        /// 关联的压缩文件，用的时候赋值，否则为null
        /// </summary>
        public SharpCompress.Archive.Zip.ZipArchive ZipTarget { get; private set; }
        /// <summary>
        /// 是否处于连接状态
        /// </summary>
        public bool IsOpen {
            get {
                if (ZipTarget != null && !ZipTarget.IsDisposed) {//并且没有释放
                    return true;
                }
                return false;
            }
        }
        //
        public void Close() {
            if (this.ZipTarget != null && !this.ZipTarget.IsDisposed) {
                this.ZipTarget.Dispose();
            }
            this.ZipTarget = null;
        }
        public void Open() {
            if (this.ZipTarget != null && !this.ZipTarget.IsDisposed) {
                return;
            }
            FileStream fs = new FileStream(ConnString, FileMode.Open, FileAccess.ReadWrite);
            this.ZipTarget = SharpCompress.Archive.Zip.ZipArchive.Open(fs);
        }
        /// <summary>
        /// 保存文件,先保存临时文件中，再改名,注意当前Conn会关闭
        /// </summary>
        public void Save() {
            if (this.IsOpen) {
                string temp = this.ConnString + "." + Guid.NewGuid().ToString("N");
                using (FileStream fs = new FileStream(temp, FileMode.Create, FileAccess.Write)) {
                    this.ZipTarget.SaveTo(fs,
                        new SharpCompress.Common.CompressionInfo() {
                            DeflateCompressionLevel = SharpCompress.Compressor.Deflate.CompressionLevel.BestSpeed,
                            Type = SharpCompress.Common.CompressionType.Deflate
                        }

                        );//保存到缓存文件
                }
                this.Close();
                File.Delete(this.ConnString);//删除原始文件
                File.Copy(temp, this.ConnString, true);//复制到原始文件
                //using (FileStream input = new FileStream(temp, FileMode.Open, FileAccess.Read)) {
                //    using (FileStream output = new FileStream(this.ConnString, FileMode.Create, FileAccess.Write)) {
                //        byte[] buffer = new byte[512 * 1024]; // Fairly arbitrary size
                //        int bytesRead;
                //        //long posold = output.Position;
                //        while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0) {
                //            output.Write(buffer, 0, bytesRead);//保存到原始文件
                //        }
                //    }
                //}
                File.Delete(temp);//删除缓存文件
                //
                //using (var zip = File.OpenWrite("C:\\test.zip"))
                //using (var zipWriter = WriterFactory.Open(ArchiveType.Zip, zip)) {
                //    foreach (var filePath in filesList) {
                //        zipWriter.Write(Path.GetFileName(file), filePath);
                //    }
                //}
            }
        }

        public void AddFile(DiskReadZip_FileItemInfo file) {
            this.Open();
            //ToDo:
            MemoryStream ms = new MemoryStream(file.FileData);
            string filefullpath = DiskReadZip_FilePacker._getFileLegalLowerDir(Path.Combine(file.FileDir, file.FileName));
            this.ZipTarget.AddEntry(filefullpath, ms, true, ms.Length, file.DateTimeFromStr(file.FileUpdateTime));
        }
        public MemoryStream GetUnCompressStream(SharpCompress.Archive.Zip.ZipArchiveEntry entry) {
            MemoryStream tempStream = new MemoryStream();
            //SharpCompress.Archive.IArchiveEntryExtensions.WriteTo(entry, tempStream);
            using (var entryStream = entry.OpenEntryStream()) {
                const int bufSize = 1024 * 16;//16k
                byte[] buf = new byte[bufSize];
                int bytesRead = 0;
                while ((bytesRead = entryStream.Read(buf, 0, bufSize)) > 0)
                    tempStream.Write(buf, 0, bytesRead);
            }
            tempStream.Position = 0;
            return tempStream;
        }

    }
    public class DiskReadZip_FileItemInfo : FileEntryInfo
    {
        //public string FileDir { get; set; }
        //public string FileName { get; set; }
        //public int FileLen { get; set; }
        ///// <summary>
        ///// yyyy-MM-dd HH:mm
        ///// </summary>
        //public string FileUpdateTime { get; set; }  

        public byte[] FileData { get; set; }
    }
    /// <summary>
    /// 注意：一个访问接口只能对应一个数据库文件(由封装属性决定的)
    /// 以文件名,文件内容,文件长度组成的文件表的存储方式进行打包
    /// 结构
    /// FileDir,FileName,FileData,FileLen
    /// </summary>
    internal class DiskReadZip_FileTable : IFilePackerStrategy
    {
        /// <summary>
        /// 一份关联
        /// </summary>
        private DiskReadZip_ConnectInfo m_Conn;

        private DiskReadZip_FilePacker m_Packer;
        //
        internal DiskReadZip_FileTable(DiskReadZip_FilePacker packer, DiskReadZip_ConnectInfo conn) {
            m_Conn = conn;
            //m_buf = arg;
            m_Packer = packer;
        }
        public IFilePacker Packer {
            get { return m_Packer; }
        }
        /// <summary>
        /// tablename,改成目录
        /// </summary>
        public string Name {
            get;
            set;
        }
        [Obsolete("只能在文件系统里面使用，这里禁用.")]
        public void AddFile(string strFileName) {
            throw new NotImplementedException();
        }
        [Obsolete("只能在文件系统里面使用，这里禁用.")]
        public void AddFile(string strFileName, DateTime date) {
            throw new NotImplementedException();
        }
        public void AddFile(string strFileName, Stream stream) {
            AddFile(strFileName, stream, DateTime.Now);

        }
        public void AddFile(string strFileName, byte[] fileData) {
            AddFile(strFileName, fileData, DateTime.Now);
        }

        public void AddFile(string strFileName, Stream stream, DateTime date) {
            byte[] buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);
            AddFile(strFileName, buf, date);
        }
        public void AddFile(string strFileName, byte[] fileData, DateTime date) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            DiskReadZip_FileItemInfo fi = new DiskReadZip_FileItemInfo();
            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            fi.FileDir = strDir;//Path.GetDirectoryName(strFileName);
            fi.FileName = strFile;//Path.GetFileName(strFileName);
            fi.FileLen = fileData.Length;
            fi.FileUpdateTime = fi.DateTimeToStr(date);//.ToString("yyyy-MM-dd HH:mm");
            fi.FileData = fileData;
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            //conn.Insert(fi);           
            //注意重名问题,外面判断
            conn.AddFile(fi);
        }
        public void AddZipFile(string strFileName, Stream stream, DateTime date) {
            byte[] buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);
            AddZipFile(strFileName, buf, date);
        }
        public void AddZipFile(string strFileName, byte[] fileData, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            AddFile(strFileName, fileData, date);
        }
        /// <summary>
        /// 一般不使用吧,外面判断新文件名是否已经存在,不要'/'开头
        /// </summary>
        /// <param name="strFileName">相对目录路径名</param>
        /// <param name="strNewFile">相对目录路径名</param>
        public void RenameFile(string strFileName, string strNewFile) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            strNewFile = DiskReadZip_FilePacker._getFileLegalLowerDir(strNewFile);
            if (string.IsNullOrEmpty(strFileName) || string.IsNullOrEmpty(strNewFile)) return;//名称不能为空
            if (string.Compare(strFileName, strNewFile) == 0) return;//没有修改

            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            //ToDo:          
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findfile = null;

            foreach (var ze in conn.ZipTarget.Entries) {
                if (ze.IsDirectory) continue;
                if (string.Compare(ze.Key, strFileName, true) == 0) {
                    findfile = ze;
                    break;
                }
            }
            if (findfile != null) {
                //外面判断新文件是否存在               
                MemoryStream ms = conn.GetUnCompressStream(findfile);
                conn.ZipTarget.AddEntry(strNewFile, ms, true, ms.Length, findfile.LastModifiedTime);
                conn.ZipTarget.RemoveEntry(findfile);
                findfile.Close();
            }
        }
        /// <summary>
        /// dir要规范，不要=='/',不要'/'开头,不要带'/'结尾
        /// </summary>
        /// <param name="strDirName"></param>
        /// <param name="strNewDirName"></param>
        public void RenameDir(string strDirName, string strNewDirName) {
            strDirName = DiskReadZip_FilePacker._getFileLegalLowerDir(strDirName);// +"/";//规范化
            strNewDirName = DiskReadZip_FilePacker._getFileLegalLowerDir(strNewDirName);// +"/";//规范化
            if (string.IsNullOrEmpty(strDirName) || string.IsNullOrEmpty(strNewDirName)) {
                return;//根目录名称不能改
            }
            if (string.Compare(strDirName, strNewDirName) == 0) {
                return;//没有改名
            }
            strDirName = strDirName + "/";
            strNewDirName = strNewDirName + "/";

            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;

            conn.Open();
            //修改所有的匹配目录名称
            List<SharpCompress.Archive.Zip.ZipArchiveEntry> olddirs = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();
            List<SharpCompress.Archive.Zip.ZipArchiveEntry> filesInDir = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();
            foreach (var ze in conn.ZipTarget.Entries) {
                if (ze.IsDirectory) {
                    if (ze.Key.ToLower().StartsWith(strDirName)) {
                        olddirs.Add(ze);
                    }
                }
                else {
                    if (ze.Key.ToLower().StartsWith(strDirName)) {
                        filesInDir.Add(ze);
                    }
                }
            }
            //外面判断是否已经存在新目录名
            //改名
            string oldfName = "";
            string newfName = "";
            for (int i = filesInDir.Count - 1; i >= 0; i--) {
                oldfName = filesInDir[i].Key;
                newfName = filesInDir[i].Key.Remove(0, strDirName.Length);
                newfName = strNewDirName + newfName;
                //
                MemoryStream ms = conn.GetUnCompressStream(filesInDir[i]);
                conn.ZipTarget.AddEntry(newfName, ms, true, ms.Length, filesInDir[i].LastModifiedTime);
                conn.ZipTarget.RemoveEntry(filesInDir[i]);
                filesInDir[i].Close();
            }
            //移除老文件夹
            for (int i = olddirs.Count - 1; i >= 0; i--) {
                conn.ZipTarget.RemoveEntry(olddirs[i]);
                olddirs[i].Close();
            }



        }
        [Obsolete("只能用于文件系统")]
        public void UpdateFile(string strFileName) {
            throw new NotImplementedException();
        }
        [Obsolete("只能用于文件系统")]
        public void UpdateFile(string strFileName, DateTime date) {
            throw new NotImplementedException();
        }
        public void UpdateFile(string strFileName, Stream stream) {
            UpdateFile(strFileName, stream, DateTime.Now);
        }

        public void UpdateFile(string strFileName, byte[] fileData) {
            UpdateFile(strFileName, fileData, DateTime.Now);
        }



        public void UpdateFile(string strFileName, Stream stream, DateTime date) {
            byte[] buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);
            UpdateFile(strFileName, stream, date);
        }

        public void UpdateFile(string strFileName, byte[] fileData, DateTime date) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            //
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var ze in conn.ZipTarget.Entries) {
                if (ze.IsDirectory) continue;
                if (string.Compare(ze.Key, strFileName, true) == 0) {
                    findFile = ze;
                    break;
                }
            }
            //
            if (findFile != null) {
                conn.ZipTarget.RemoveEntry(findFile);
                findFile.Close();//删除老的
                AddFile(strFileName, fileData, date);
            }
            else {
                AddFile(strFileName, fileData, date);
            }
        }
        public void UpdateZipFile(string strFileName, byte[] fileData, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            UpdateFile(strFileName, fileData, date);
        }
        /// <summary>
        /// dir要规范，不要带'/'结尾
        /// </summary>
        /// <param name="strDir">strDir不能为""或者"/" 或者"\\"</param>
        public void DelDir(string strDir) {
            strDir = DiskReadZip_FilePacker._getFileLegalLowerDir(strDir);
            if (string.IsNullOrEmpty(strDir)) {
                return;//不能删除所有？
            }
            strDir = strDir + "/";
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            //目录对上的就要删除
            //修改所有的匹配目录名称
            List<SharpCompress.Archive.Zip.ZipArchiveEntry> olddirs = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();

            foreach (var ze in conn.ZipTarget.Entries) {
                //if (ze.IsDirectory) {
                if (ze.Key.ToLower().StartsWith(strDir)) {
                    olddirs.Add(ze);

                }
                //}
            }

            //删除目录以及其中的文件
            for (int i = olddirs.Count - 1; i >= 0; i--) {
                conn.ZipTarget.RemoveEntry(olddirs[i]);
                olddirs[i].Close();
            }

        }

        public void DelFile(string strFileName) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                return;
            }
            //压缩包文件存储格式
            //目录 "a/b/c/"
            //文件 "a/test.txt"            
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in conn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, strFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            if (findFile != null) {
                conn.ZipTarget.RemoveEntry(findFile);
                findFile.Close();
            }
        }

        public bool FileExists(string strFileName) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                return false;
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in conn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, strFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            if (findFile != null) {
                return true;
            }
            return false;
        }

        public DateTime GetUpdateDate(string strFileName) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            DateTime updateTime = DateTime.MinValue;
            foreach (var v in conn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, strFileName, true) == 0) {
                    findFile = v;
                    updateTime = v.LastModifiedTime ?? DateTime.MinValue;
                    break;
                }
            }

            return updateTime;
        }

        public int GetFiles(string strDir, out List<string> fileNames, out int totalSize) {
            fileNames = new List<string>();
            totalSize = 0;
            strDir = DiskReadZip_FilePacker._getFileLegalLowerDir(strDir);
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            //所有的匹配目录名称           
            List<SharpCompress.Archive.Zip.ZipArchiveEntry> filesInDir = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();
            if (string.IsNullOrEmpty(strDir)) {
                //最顶层 则是所有的文件
                foreach (var ze in conn.ZipTarget.Entries) {
                    if (!ze.IsDirectory) {
                        filesInDir.Add(ze);
                    }
                }
            }
            else {
                //非最顶层
                strDir = strDir + "/";
                //ToDo:查找里面的所有文件
                foreach (var ze in conn.ZipTarget.Entries) {
                    if (!ze.IsDirectory) {
                        if (ze.Key.ToLower().StartsWith(strDir)) {
                            filesInDir.Add(ze);
                        }
                    }
                }
            }
            foreach (var v in filesInDir) {
                fileNames.Add(v.Key);
                totalSize += (int)v.Size;
            }
            //            
            int count = filesInDir.Count;
            return count;
        }
        /// <summary>
        /// 返回规范化的目录名称 不以'/'开头和结尾
        /// </summary>
        /// <param name="dirs"></param>
        /// <returns></returns>
        public int GetDirs(out List<string> dirs) {
            dirs = new List<string>();
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            string fileInDir = "";
            string strFile;
            foreach (var ze in conn.ZipTarget.Entries) {
                if (ze.Key != "/") {//最顶层就不要了
                    fileInDir = DiskReadZip_FilePacker._getFileLegalLowerDir(ze.Key);
                    fileInDir = GetFirstDir(fileInDir, out strFile);
                    if (!dirs.Contains(fileInDir)) {
                        dirs.Add(fileInDir);
                    }
                }
            }

            return dirs.Count;
        }



        public byte[] OpenFile(string strFileName) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in conn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, strFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            byte[] buf = null;
            if (findFile != null) {
                using (MemoryStream ms = conn.GetUnCompressStream(findFile)) {
                    buf = ms.ToArray();
                    ms.Close();//关闭流                
                }
            }
            return buf;
        }

        public Stream OpenFileAsStream(string strFileName) {
            strFileName = DiskReadZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in conn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, strFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            if (findFile != null) {
                return conn.GetUnCompressStream(findFile);
            }
            return null;
        }

        public string OpenFileAsString(string strFileName) {
            byte[] buf = OpenFile(strFileName);
            if (buf != null) {
                string str = Encoding.UTF8.GetString(buf);
                return str;
            }
            return "";
        }

        public void Clean() {
            //清除所有
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskReadZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            List<SharpCompress.Archive.Zip.ZipArchiveEntry> allFiles = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();
            foreach (var v in conn.ZipTarget.Entries) {
                allFiles.Add(v);
            }
            //移除所有项目
            for (int i = allFiles.Count - 1; i >= 0; i--) {
                conn.ZipTarget.RemoveEntry(allFiles[i]);
                allFiles[i].Close();
            }
            allFiles.Clear();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="remain">文件名</param>
        /// <returns></returns>
        private static string GetFirstDir(string strFileName, out string remain) {
            string tmp1 = Path.GetDirectoryName(strFileName);
            tmp1 = tmp1.Replace("\\", "/");
            tmp1 = tmp1.Replace("//", "/");
            string tmp2 = Path.GetFileName(strFileName);
            //tmp2 = tmp2.Replace("\\", "/");
            //tmp2 = tmp2.Replace("//", "/");
            strFileName = tmp1 + "/" + tmp2;
            int p = strFileName.IndexOf("/");
            if (p == -1) {
                remain = strFileName;
                return "";
            }
            string ret = strFileName.Substring(0, p);
            if (!string.IsNullOrEmpty(ret)) ret = ret.ToLower();
            remain = strFileName.Substring(p + 1, strFileName.Length - p - 1);
            if (!string.IsNullOrEmpty(remain)) remain = remain.ToLower();
            return ret;
        }



        public void Close() {
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            this.m_Conn.Close();
        }
    }
    internal class DiskZip_AccessPackerException : Exception
    {
        public DiskZip_AccessPackerException(string msg, Exception inner) : base(msg, inner) { }
    }
}
