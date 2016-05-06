namespace IRobotQ.PackerDisk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using IRobotQ.Packer;
    using SharpCompress;

    internal class Disk_FilePacker : IFileSysPacker
    {
        /// <summary>
        /// 根目录
        /// </summary>
        public string RootDir { get; private set; }
        /// <summary>
        /// 根目录下的文件存储 
        /// </summary>
        public string RootDir_ZipPath {
            get { return Path.Combine(RootDir, FilePackerDBName); }
        }
        /// <summary>
        /// ~PACKER~.z
        /// </summary>
        public const string FilePackerDBName = "~PACKER~.z";//packer
        /// <summary>
        /// ~TABLE~.z
        /// </summary>
        public const string FileTableDBName = "~TABLE~.z";//table
        public static string GetTableRootDir(string packerRootDir, string tableName) {
            string dir = Path.Combine(packerRootDir, tableName);
            return dir;
        }
        public static string GetTableZipDBPath(string packerRootDir, string tableName) {
            string dir = Path.Combine(packerRootDir, tableName);
            dir = Path.Combine(dir, FileTableDBName);
            return dir;
        }

        protected Disk_FilePacker(string rootDir) {
            this.RootDir = rootDir;// FileEntryInfo.GetDirPathToLowerNorm_STC(rootDir);
            _checkDBFilePath(rootDir, FilePackerDBName);
        }
        void _checkDBFilePath(string path, string _dbfileName) {
            //创建目录与文件 第1层，第2层的文件保存方式
            string rootDir = path;
            if (!Directory.Exists(rootDir)) {
                FileEntryInfo.CheckDirectory_STC(rootDir);
            }
            string dbname = Path.Combine(rootDir, _dbfileName);
            //创建根目录 下保存的文件对象位置
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
        }
        internal static Disk_FilePacker OpenPacker(string rootDir) {
            Disk_FilePacker packer = new Disk_FilePacker(rootDir);
            return packer;
        }

        #region IFilePacker 成员
        private Dictionary<string, Disk_FileTable> m_Tables = new Dictionary<string, Disk_FileTable>();
        /// <summary>
        /// name 不能带'/' 与 '\'
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IFileSysPackerStrategy AddFileTable(string name) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(name);
            }
            string dir = GetTableRootDir(this.RootDir, name);//Path.Combine(RootDir,name);
            _checkDBFilePath(dir, FileTableDBName);
            //建立文件
            if (!m_Tables.ContainsKey(name)) {
                Disk_FileTable df = new Disk_FileTable(this, name, dir);
                m_Tables.Add(name, df);
            }
            return m_Tables[name];
        }

        public IFileSysPackerStrategy GetFileTable(string name) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(name);
            }
            if (m_Tables.ContainsKey(name)) {
                string dir = GetTableRootDir(this.RootDir, name);//Path.Combine(RootDir, name);
                _checkDBFilePath(dir, FileTableDBName);
                return m_Tables[name];
            }
            return null;
        }

        public void DelFileTable(string name) {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException(name);
            }
            if (m_InUpdateState) {
                this.EndUpdate(name,true);
            }
            string dir = GetTableRootDir(this.RootDir, name);//Path.Combine(this.RootDir,name);
            string dirDBFile = GetTableZipDBPath(this.RootDir, name);//Path.Combine(dir, FileTableDBName);
            if (File.Exists(dirDBFile)) {
                //删除数据文件
                File.Delete(dirDBFile);
            }
            if (Directory.Exists(dir)) {
                //删除目录以及目录下的文件
                Directory.Delete(dir, true);
            }
            if (m_Tables.ContainsKey(name)) {
                //移除关联
                m_Tables.Remove(name);
            }

        }

        public void RenameFileTable(string tableName, string newName) {
            if (string.IsNullOrEmpty(tableName)) {
                throw new ArgumentNullException(tableName);
            }
            if (string.IsNullOrEmpty(newName)) {
                throw new ArgumentNullException(tableName);
            }
           
            if (string.Compare(tableName, newName, true) == 0) return;//没有更改，返回
            if (!IsTableExists(tableName)) return;
            if (m_InUpdateState) {
                this.EndUpdate(tableName, true);
            }
            //重命名
            if (IsTableExists(newName)) return;//已经存在，不允许
            string oldDir = GetTableRootDir(this.RootDir, tableName);
            string newDir = GetTableRootDir(this.RootDir, newName);
            if (Directory.Exists(newDir)) {
                Directory.Delete(newDir, true);
            }
            Directory.Move(oldDir, newDir);
        }

        public int GetFileTableList(out List<string> ret) {
            ret = new List<string>();
            DirectoryInfo di = new DirectoryInfo(this.RootDir);
            DirectoryInfo[] dirs = di.GetDirectories();
            foreach (var v in dirs) {
                string dbfile = GetTableZipDBPath(this.RootDir, v.Name); //Path.Combine(v,FilePackerDBName);
                if (File.Exists(dbfile)) {
                    ret.Add(v.Name);
                }
            }
            return ret.Count;
        }

        public bool IsTableExists(string name) {

            //string dir = Path.Combine(RootDir, name);
            string dirDBFile = GetTableZipDBPath(this.RootDir, name); //Path.Combine(dir, FilePackerDBName);
            //
            if (File.Exists(dirDBFile)) {
                return true;
            }
            return false;
        }

        bool m_InUpdateState = false;
        public void BeginUpdate(IFileSysPackerStrategy file) {
            string tableName = file.Name;
            BeginUpdate(tableName);
        }

        public void BeginUpdate(string name) {
            m_InUpdateState = true;
            m_Tables[name].BeginUpdate();
        }

        public void EndUpdate(IFileSysPackerStrategy file, bool success) {
            string tableName = file.Name;
            EndUpdate(tableName, success);

        }

        public void EndUpdate(string name, bool success) {
            m_InUpdateState = false;
            m_Tables[name].EndUpdate(success);
        }

        public void Close() {            
            foreach (var v in m_Tables.Values) {
                v.Close();
            }
        }

        #endregion
    }
    internal class Disk_FileTable : IFileSysPackerStrategy
    {

        internal Disk_FileTable(Disk_FilePacker packer, string name, string tableRootDir) {
            this.Name = name;
            this.m_TableRootDir = tableRootDir;
            this.m_Packer = packer;
            this.DirRoot_Packer = m_Packer.RootDir;

        }
        /// <summary>
        /// packer根目录
        /// </summary>
        string DirRoot_Packer = "";
        string m_TableRootDir = "";
        /// <summary>
        /// ~FILE_DATA~.z
        /// 第一层子目录对应的.Z文件名 
        /// 比如测试机器人
        /// test目录/~FILE_DATA~.z
        /// ~FILE_DATA~.z 中内容 info.json data.json
        /// </summary>
        const string FileDataDBName = "~FILE_DATA~.z";
        /// <summary>
        /// 当前table的根目录,是packer的子目录
        /// </summary>
        public string RootDir_FileTable { get { return Disk_FilePacker.GetTableRootDir(this.DirRoot_Packer, this.Name); } }
        /// <summary>
        /// TABLE 顶层文件存储.z
        /// </summary>
        string RootDir_FileTableZipDBPath { get { return Disk_FilePacker.GetTableZipDBPath(this.DirRoot_Packer, this.Name); } }

        /// <summary>
        /// table目录名,外面不能修改
        /// </summary>
        public string Name {
            get;
            set;
        }
        private Disk_FilePacker m_Packer;
        public IFileSysPacker Packer {
            get { return m_Packer; }
        }
        List<DiskZip_ConnectInfo> m_InUpdateConnZips = new List<DiskZip_ConnectInfo>();
        bool m_InUpdateState = false;
        internal void BeginUpdate() {
            m_InUpdateState = true;
        }
        /// <summary>
        /// 保存修改
        /// </summary>
        /// <param name="name"></param>
        /// <param name="success">true:保存修改,false:放弃修改</param>
        internal void EndUpdate(bool success) {
            m_InUpdateState = false;
            if (success) {
                foreach (var v in m_InUpdateConnZips) {
                    v.Save();
                }
            }
            else {
                foreach (var v in m_InUpdateConnZips) {
                    v.Close();
                }
            }
            m_InUpdateConnZips.Clear();
        }
        /// <summary>
        /// 【关键】获取相对与table的根目录 文件路径对应存储的.z文件
        /// </summary>
        /// <param name="topChildDir"></param>
        /// <returns></returns>
        string _getFileDataZipPath(string relativeTableDirFilePath) {
            string firstDir = "";
            string remainFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(relativeTableDirFilePath, out remainFileName);
            if (string.IsNullOrEmpty(firstDir)) {
                return this.RootDir_FileTableZipDBPath;
            }
            string root = this.RootDir_FileTable;
            string cp = Path.Combine(root, firstDir);
            string zipdb = Path.Combine(cp, FileDataDBName);
            return zipdb;
        }

        /// <summary>
        /// 根据文件名，取得存储数据的zip
        /// </summary>
        /// <param name="strFileName">相对与table根目录的路径</param>
        /// <returns></returns>
        DiskZip_ConnectInfo _checkFileDataZip(string strFileName) {
            string zipDataPath = _getFileDataZipPath(strFileName);
            bool fileExist = File.Exists(zipDataPath);
            if (!fileExist) {
                FileEntryInfo.CheckDirectory_STC(Path.GetDirectoryName(zipDataPath));
                using (SharpCompress.Archive.Zip.ZipArchive con = SharpCompress.Archive.Zip.ZipArchive.Create()) {
                    using (FileStream fst = new FileStream(zipDataPath, FileMode.Create, FileAccess.Write)) {
                        con.SaveTo(fst, new SharpCompress.Common.CompressionInfo());//保存完毕
                    }
                }
            }
            if (!m_FileDataZips.ContainsKey(zipDataPath)) {

                //创建
                if (fileExist) {
                    //检测是否为压缩文件
                    using (FileStream fst = new FileStream(zipDataPath, FileMode.Open, FileAccess.Read)) {
                        fileExist = SharpCompress.Archive.Zip.ZipArchive.IsZipFile(fst);//非压缩文件
                        //压缩文件系统 没必要一直处于打开状态吧 用完销毁                        
                    }
                }
                if (!fileExist) {
                    FileEntryInfo.CheckDirectory_STC(Path.GetDirectoryName(zipDataPath));
                    using (SharpCompress.Archive.Zip.ZipArchive con = SharpCompress.Archive.Zip.ZipArchive.Create()) {
                        using (FileStream fst = new FileStream(zipDataPath, FileMode.Create, FileAccess.Write)) {
                            con.SaveTo(fst, new SharpCompress.Common.CompressionInfo());//保存完毕
                        }
                    }
                }

                DiskZip_ConnectInfo dc = new DiskZip_ConnectInfo();
                dc.ConnString = zipDataPath;
                m_FileDataZips.Add(zipDataPath, dc);
            }
            return m_FileDataZips[zipDataPath];
        }
        Dictionary<string, DiskZip_ConnectInfo> m_FileDataZips = new Dictionary<string, DiskZip_ConnectInfo>();


        [Obsolete()]
        public void AddFile(string strFileName) {
            //只用于单纯文件系统中
            //throw new NotImplementedException();
        }
        [Obsolete()]
        public void AddFile(string strFileName, DateTime date) {
            //只用于单纯文件系统中
            //throw new NotImplementedException();
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
            //更新文件JSON内容
        }

        public void AddFile(string strFileName, byte[] fileData, DateTime date) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);//获取存储位置
            zipConn.Open();
            //_checkTopChildDirZipPath(firstDir);//检测ZIP，并创建
            FileEntryInfo file = new FileEntryInfo();
            file.FileDir = firstDir;
            file.FileName = childDirFileName;
            file.FileLen = fileData.Length;
            file.FileUpdateTime = file.DateTimeToStr(date);
            //            
            zipConn.AddFile(file, fileData);
            if (m_InUpdateState) {
                m_InUpdateConnZips.Add(zipConn);
            }
            else {
                zipConn.Save();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="strFileName"></param>
        /// <param name="strNewFile"></param>
        public void RenameFile(string strFileName, string strNewFile) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            strNewFile = FileEntryInfo.GetDirPathToLowerNorm_STC(strNewFile);
            if (string.IsNullOrEmpty(strFileName) || string.IsNullOrEmpty(strNewFile)) return;//名称不能为空
            if (string.Compare(strFileName, strNewFile) == 0) return;//没有修改

            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);//获取存储位置
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findfile = null;

            foreach (var ze in zipConn.ZipTarget.Entries) {
                if (ze.IsDirectory) continue;
                if (string.Compare(ze.Key, childDirFileName, true) == 0) {
                    findfile = ze;
                    break;
                }
            }
            if (findfile != null) {
                MemoryStream ms = zipConn.GetUnCompressStream(findfile);
                zipConn.ZipTarget.RemoveEntry(findfile);
                //findfile.Close();
                //
                string firstDir2 = "";
                string childDirFileName2 = "";
                firstDir2 = FileEntryInfo.GetFirstDir_STC(strNewFile, out childDirFileName2);
                DiskZip_ConnectInfo zipConn2 = _checkFileDataZip(strNewFile);//获取存储位置
                zipConn2.Open();
                zipConn2.ZipTarget.AddEntry(strNewFile, ms, true, ms.Length, findfile.LastModifiedTime);
                if (m_InUpdateState) {
                    m_InUpdateConnZips.Add(zipConn);
                    if (string.Compare(firstDir2, firstDir) != 0) {
                        m_InUpdateConnZips.Add(zipConn2);
                    }
                }
                else {
                    zipConn.Save();
                    if (string.Compare(firstDir2, firstDir) != 0) {
                        zipConn2.Save();
                    }
                }
            }
        }
        /// <summary>
        /// 只能是最顶层的目录重命名,注意：会中断 m_InUpdateState状态
        /// </summary>
        /// <param name="strFirstDirName">顶层目录</param>
        /// <param name="strNewFirstDirName">顶层目录</param>
        public void RenameDir(string strFirstDirName, string strNewFirstDirName) {
            //目录修改
            strFirstDirName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFirstDirName);// +"/";//规范化
            strNewFirstDirName = FileEntryInfo.GetDirPathToLowerNorm_STC(strNewFirstDirName);// +"/";//规范化
            if (string.IsNullOrEmpty(strFirstDirName) || string.IsNullOrEmpty(strNewFirstDirName)) {
                return;//根目录名称不能改
            }
            if (string.Compare(strFirstDirName, strNewFirstDirName) == 0) {
                return;//没有改名
            }

            strFirstDirName = strFirstDirName + "/";
            strNewFirstDirName = strNewFirstDirName + "/";
            string remian1 = "";
            string remian2 = "";
            string firstTopDir = FileEntryInfo.GetFirstDir_STC(strFirstDirName, out remian1);
            string firstTopDir2 = FileEntryInfo.GetFirstDir_STC(strFirstDirName, out remian2);
            
            string realDir1 = Path.Combine(RootDir_FileTable, strFirstDirName);
            string realDir2 = Path.Combine(RootDir_FileTable, strNewFirstDirName);
            if (!string.IsNullOrEmpty(remian1) || !string.IsNullOrEmpty(remian2)) {
                throw new DiskZip_AccessPackerException("目录必须都是最顶层目录", null);
            }
            if (Directory.Exists(realDir2)) {
                //已经存在，不能改
                return;
            }
            this.EndUpdate(true);//只能先保存，不然会被占用
            if (Directory.Exists(realDir1)) {
                Directory.Move(realDir1, realDir2);
            }
            else { 
                throw new DiskZip_AccessPackerException("目录必须都是最顶层目录",null);
            }
        }
        [Obsolete()]
        public void UpdateFile(string strFileName) {
            //只用于单纯的文件系统中
            //throw new NotImplementedException();
        }
        [Obsolete()]
        public void UpdateFile(string strFileName, DateTime date) {
            //只用于单纯的文件系统中
            //throw new NotImplementedException();
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
            UpdateFile(strFileName, buf, date);
        }

        public void UpdateFile(string strFileName, byte[] fileData, DateTime date) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);//获取存储位置
            //_checkTopChildDirZipPath(firstDir);//检测ZIP，并创建
            FileEntryInfo file = new FileEntryInfo();
            file.FileDir = firstDir;
            file.FileName = childDirFileName;
            file.FileLen = fileData.Length;
            file.FileUpdateTime = file.DateTimeToStr(date);
            //
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var ze in zipConn.ZipTarget.Entries) {
                if (ze.IsDirectory) continue;
                if (string.Compare(ze.Key, file.FileName, true) == 0) {
                    findFile = ze;
                    break;
                }
            }
            //
            if (findFile != null) {
                zipConn.ZipTarget.RemoveEntry(findFile);
                //findFile.Close();//删除老的

            }
            //修改文件
            AddFile(strFileName, fileData, date);
            if (m_InUpdateState) {
                m_InUpdateConnZips.Add(zipConn);
            }
            else {
                zipConn.Save();
            }
        }

        public void UpdateZipFile(string strFileName, byte[] fileData, DateTime date) {
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            UpdateFile(strFileName, fileData, date);
        }
        /// <summary>
        /// 传进来的必须是目录,注意：会中断 m_InUpdateState状态
        /// </summary>
        /// <param name="strDir"></param>
        public void DelDir(string strDir) {
            //删除DIR
            strDir = FileEntryInfo.GetDirPathToLowerNorm_STC(strDir);
            if (string.IsNullOrEmpty(strDir)) {
                return;//不能删除所有？
            }
            strDir = strDir + "/";//补充
            string firstDir = "";
            string childFirstDirFileName = "";
            string secendDir = "";
            //string childSecendDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strDir, out childFirstDirFileName);
            if (!string.IsNullOrEmpty(childFirstDirFileName)) {
                //有子目录
                int index = childFirstDirFileName.IndexOf('/');
                if (index != -1) {
                    secendDir = childFirstDirFileName.Substring(0, index);
                }
                else {
                    secendDir = childFirstDirFileName;
                }
            }
            //第2层目录为空，说明删除的是顶层DIR           
            if (string.IsNullOrEmpty(secendDir)) {
                //删除目录---这一块一执行就被删除了,需要注意
                string realPath = Path.Combine(this.RootDir_FileTable, firstDir);
                string zip = Path.Combine(realPath, FileDataDBName);
                if (File.Exists(zip)) {
                    DiskZip_ConnectInfo zipConn = _checkFileDataZip(firstDir + "/checkdir.zip");
                    zipConn.Close();
                    File.Delete(zip);
                }
                if (Directory.Exists(realPath)) {
                    Directory.Delete(realPath, true);
                }
                if (m_FileDataZips.ContainsKey(zip)) {
                    m_FileDataZips.Remove(zip);
                }
            }
            else {
                //场景中的机器人/VPL文件列表
                secendDir = secendDir + "/";
                //删除压缩包内的文件
                DiskZip_ConnectInfo zipConn = _checkFileDataZip(strDir + "/checkfiledata.zip");
                zipConn.Open();
                List<SharpCompress.Archive.Zip.ZipArchiveEntry> olddirs = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();

                foreach (var ze in zipConn.ZipTarget.Entries) {
                    //if (ze.IsDirectory) {
                    if (ze.Key.ToLower().StartsWith(secendDir)) {
                        olddirs.Add(ze);

                    }
                    //}
                }
                //删除目录以及其中的文件
                for (int i = olddirs.Count - 1; i >= 0; i--) {
                    zipConn.ZipTarget.RemoveEntry(olddirs[i]);
                    //olddirs[i].Close();
                }
                if (m_InUpdateState) {
                    m_InUpdateConnZips.Add(zipConn);
                }
                else {
                    zipConn.Save();
                }
            }

        }

        public void DelFile(string strFileName) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                return;
            }
            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in zipConn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, childDirFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            if (findFile != null) {
                zipConn.ZipTarget.RemoveEntry(findFile);
                //findFile.Close();
            }
            if (m_InUpdateState) {
                m_InUpdateConnZips.Add(zipConn);
            }
            else {
                zipConn.Save();
            }
        }

        public bool FileExists(string strFileName) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                return false;
            }
            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in zipConn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, childDirFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            bool find = false;
            if (findFile != null) {
                //findFile.Close();
                find = true;
            }

            return find;
        }

        public DateTime GetUpdateDate(string strFileName) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            DateTime updateTime = DateTime.MinValue;
            foreach (var v in zipConn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, strFileName, true) == 0) {
                    findFile = v;
                    updateTime = v.LastModifiedTime ?? DateTime.MinValue;
                    break;
                }
            }

            return updateTime;
        }
        /// <summary>
        /// 目录不能为空
        /// </summary>
        /// <param name="strDir"></param>
        /// <param name="fileNames"></param>
        /// <param name="totalSize"></param>
        /// <returns></returns>
        public int GetFiles(string strDir, out List<string> fileNames, out int totalSize) {
            //
            strDir = FileEntryInfo.GetDirPathToLowerNorm_STC(strDir);
            if (string.IsNullOrEmpty(strDir)) {
                throw new ArgumentNullException(strDir);
            }
            strDir = strDir + "/";//补充
            fileNames = new List<string>();
            totalSize = 0;
            string firstDir = "";
            string childDirFileName = "";
            //string strFileName= Path.Combine(strDir, "checkfiledata.zip");
            firstDir = FileEntryInfo.GetFirstDir_STC(strDir, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strDir);
            zipConn.Open();
            //
            List<SharpCompress.Archive.Zip.ZipArchiveEntry> filesInDir = new List<SharpCompress.Archive.Zip.ZipArchiveEntry>();
            bool all = string.IsNullOrEmpty(childDirFileName);          
            //ToDo:查找里面的所有文件
            foreach (var ze in zipConn.ZipTarget.Entries) {
                if (!ze.IsDirectory) {
                    if (all) { 
                         filesInDir.Add(ze);
                    }
                    else if (ze.Key.ToLower().StartsWith(firstDir)) {
                        filesInDir.Add(ze);
                    }
                }
            }
            foreach (var v in filesInDir) {
                fileNames.Add(v.Key);
                totalSize += (int)v.Size;
                //v.Close();
            }
            //            
            int count = filesInDir.Count;
            return count;
        }

        public int GetDirs(out List<string> dirs) {
            dirs = new List<string>();
            string[] dirs2 = Directory.GetDirectories(this.RootDir_FileTable);
            dirs.AddRange(dirs2);
            return dirs.Count;
        }



        public byte[] OpenFile(string strFileName) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);//获取存储位置
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in zipConn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, childDirFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            byte[] buf = null;
            if (findFile != null) {
                using (MemoryStream ms = zipConn.GetUnCompressStream(findFile)) {
                    buf = ms.ToArray();
                    ms.Close();//关闭流                
                }
            }
            return buf;

        }

        public Stream OpenFileAsStream(string strFileName) {
            strFileName = FileEntryInfo.GetDirPathToLowerNorm_STC(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            string firstDir = "";
            string childDirFileName = "";
            firstDir = FileEntryInfo.GetFirstDir_STC(strFileName, out childDirFileName);
            DiskZip_ConnectInfo zipConn = _checkFileDataZip(strFileName);//获取存储位置
            zipConn.Open();
            SharpCompress.Archive.Zip.ZipArchiveEntry findFile = null;
            foreach (var v in zipConn.ZipTarget.Entries) {
                if (v.IsDirectory) continue;
                if (string.Compare(v.Key, childDirFileName, true) == 0) {
                    findFile = v;
                    break;
                }
            }
            if (findFile != null) {
                return zipConn.GetUnCompressStream(findFile);
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
            //删除所有
            this.Close();
            string[] dirs = Directory.GetDirectories(this.RootDir_FileTable);
            foreach (var v in dirs) {
                Directory.Delete(v, true);
            }
        }

        public void Close() {
            foreach (var v in m_FileDataZips.Values) {
                v.Close();
            }
        }
    }

    /// <summary>
    /// .z文件相对路径
    /// "data source=" + strFileName + ";Compress=True;";
    /// </summary>  
    public class DiskZip_ConnectInfo
    {
        //以.z结尾
        /// <summary>
        /// 对应的.z文件全路径
        /// </summary>
        public string ConnString;

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

        public void AddFile(FileEntryInfo file, byte[] data) {
            this.Open();
            //ToDo:
            MemoryStream ms = new MemoryStream(data);
            //第一层目录是就是对应的目录,第2层
            string filefullpath = file.GetDirPathToLowerNorm(file.FileName);
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
}
