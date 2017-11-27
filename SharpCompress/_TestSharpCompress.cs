#define PACK

namespace SharpCompress
{
    using System;
    using System.Collections.Generic;
    //using System.Linq;
    using System.Text;
    using System.IO;
    using SharpCompress.Archive;
    using SharpCompress.Archive.Zip;
    using SharpCompress.Common;
    using System.Diagnostics;
    using IRobotQ.Packer;
    using IRQ_Packer = IRobotQ.PackerDisk.Disk_FilePacker;
    static class _TestSharpCompress
    {
        public static void Main(string[] args) {
            //测试序列化

            List<IRQ_VPLDocInfo_Json> vpldocjsons = new List<IRQ_VPLDocInfo_Json>();
            for(int i=0;i<1;i++){
                IRQ_VPLDocInfo_Json docjs = new IRQ_VPLDocInfo_Json();
                docjs.Author = "me_"+i.ToString();
                docjs.CodeType = VPL_CodeType.CSharp;
                docjs.CreateDate = DateTime.Now;
                docjs.Description = "";
                docjs.IsCodeMode = false;
                docjs.IsNoRobotMode = true;
                docjs.LastUpdate = DateTime.Now;
                docjs.NoRobotMode_LastPlatId = "";
                docjs.OnCloudSvr = false;
                docjs.PackageUniqueId = "";
                docjs.PackageVersion = "2.0";
                docjs.Source = IRQ_FileDocSource.User;
                docjs.Ver = "2.0";
                vpldocjsons.Add(docjs);
            }
            string docJsonStr = SimpleJsonEx.SimpleJson.SerializeObject(vpldocjsons);
            using (FileStream fs = new FileStream("test_array.json", FileMode.OpenOrCreate, FileAccess.Write)) {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8)) {
                    sw.Write(docJsonStr);
                }
            }
            //反序列化
            string docJosnStr2 = "";
            using (StreamReader sr = new StreamReader("test_array.json", Encoding.UTF8)) {
                docJosnStr2 = sr.ReadToEnd();
            }
            List<IRQ_VPLDocInfo_Json> redocJsons = SimpleJsonEx.SimpleJson.DeserializeObject<List<IRQ_VPLDocInfo_Json>>(docJosnStr2);

            char[] invalid1 = Path.GetInvalidFileNameChars();
            char[] invalid2 = Path.GetInvalidPathChars();
            List<char> invalidChars = new List<char>();
            invalidChars.AddRange(invalid1);
            for (int i = 0; i < invalid2.Length; i++) {
                if (invalidChars.Contains(invalid2[i]) == false) {
                    invalidChars.Add(invalid2[i]);
                }
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < invalidChars.Count; i++) {
                sb.Append(string.Format("{0}",(int)invalidChars[i]));
                if(i!=invalidChars.Count-1){
                    sb.Append(",");
                }
            }
            string chars = sb.ToString();
            Console.WriteLine(chars);
            // "'\"','<','>','|','\0','','','','','','','\a','\b','\t','\n','\v','\f','\r','','','','','','','','','','','','','','','','','','',':','*','?','\\','/'"
            int timestart = Environment.TickCount;
            ResourceService.Init();
           // Stopwatch swRead = new Stopwatch();
           // swRead.Start();
           // IFilePackerStrategy urobot = ResourceService.GetLib(IRQ_FileType.Robot);
           //byte[] strInfo= urobot.OpenFile("kaka/1.png");
           //urobot.DelDir("kaka");
           //swRead.Stop();
           //Console.WriteLine("end:"+swRead.ElapsedMilliseconds.ToString()+" ms");

           //return;
            //
            //增加文件
            IFileSysPackerStrategy fps = ResourceService.GetLib(IRQ_FileType.TempLeadInRes);
            FileStream fsadd = new FileStream("mypack.data.zip", FileMode.Open, FileAccess.Read);
            byte[] buf_add = new byte[fsadd.Length];
            fsadd.Read(buf_add, 0, buf_add.Length);
            fsadd.Close();
            fps.Packer.BeginUpdate(fps);
            if (fps.FileExists("/test/mypack.data.zip")) {
                byte[] testReadBuf = fps.OpenFile("/test/mypack.data.zip");
                fps.DelFile("/test/mypack.data.zip");
            }
            fps.AddFile("/test/mypack.data.zip", buf_add, DateTime.Now);//addfile
            fps.AddFile("/test/dir2/mypack2.data.zip", buf_add, DateTime.Now);//addfile
            fps.UpdateFile("/test/dir2/mypack2.data.zip", new byte[] { 1, 2, 3, 4 }, DateTime.Now);
            fps.AddFile("/test2/mypack.data.zip", buf_add, DateTime.Now);//addfile
            fps.RenameFile("/test2/mypack.data.zip", "/test2/mypack2.data.zip");
            fps.RenameDir("test", "test3");
            List<string> getDirs = new List<string>();
            fps.GetDirs(out getDirs);
            fps.Clean();

            fps.AddFile("/test3/mypack.data.zip", buf_add, FileEntryInfo.DateTimeFromStr_STC("2016-01-01 12:12:12"));//addfile
            fps.AddFile("/test4/mypack.data.zip", buf_add, DateTime.Now);//addfile
            getDirs = new List<string>();
            fps.GetDirs(out getDirs);
            DateTime dtupdate = fps.GetUpdateDate("test3/mypack.data.zip");
            List<string> filenames = new List<string>();
            int totalSize = 0;
            fps.GetFiles("test3", out filenames, out totalSize);
            fps.DelDir("test3");
            fps.RenameDir("test4", "test");

            fps.Packer.EndUpdate(fps, true);
            Console.WriteLine("耗时：" + (Environment.TickCount - timestart).ToString() + " ms");
            Console.Read();
            return;
            string SCRATCH_FILES_PATH = "ziptest";
            //
            {
                //test
                //CompressionType.LZMA    10次 34175ms 242k
                //CompressionType.PPMd    10次 68678ms 319k
                //CompressionType.Deflate 10次 3006ms 428k
                //CompressionType.BZip2 10次 10103ms 335k
                //CompressionType.GZip not support
                //CompressionType.Rar  not support
                //CompressionType.BCJ2 not support
                //CompressionType.BCJ not support
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //for (int i = 0; i < 10; i++) {
                using (var archive = ZipArchive.Create()) {
                    DirectoryInfo di = new DirectoryInfo(SCRATCH_FILES_PATH);
                    foreach (var fi in di.GetFiles()) {
                        archive.AddEntry(fi.Name, fi.OpenRead(), true);
                    }
                    FileStream fs_scratchPath = new FileStream("compresstimetest.zip", FileMode.OpenOrCreate, FileAccess.Write);
                    archive.SaveTo(fs_scratchPath, CompressionType.Deflate);
                    fs_scratchPath.Close();
                }
                //break;
                //} 
                sw.Stop();
                Console.WriteLine("10time (ms):" + sw.ElapsedMilliseconds.ToString());

            }

            string scratchPath = "ziptest.zip";

            using (var archive = ZipArchive.Create()) {
                DirectoryInfo di = new DirectoryInfo(SCRATCH_FILES_PATH);
                foreach (var fi in di.GetFiles()) {
                    archive.AddEntry(fi.Name, fi.OpenRead(), true);
                }
                FileStream fs_scratchPath = new FileStream(scratchPath, FileMode.OpenOrCreate, FileAccess.Write);
                archive.SaveTo(fs_scratchPath, CompressionType.LZMA);
                fs_scratchPath.Close();
                //archive.AddAllFromDirectory(SCRATCH_FILES_PATH); 
                //archive.SaveTo(scratchPath, CompressionType.Deflate);
                using (FileStream fs = new FileStream("ziphead.zip", FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                    MyHead mh = new MyHead();
                    byte[] headData = mh.Create();
                    fs.Write(headData, 0, headData.Length);
                    //
                    SharpCompress.IO.OffsetStream ofs = new IO.OffsetStream(fs, fs.Position);
                    archive.SaveTo(ofs, CompressionType.Deflate);
                }
            }
            //write my zipfile with head data
            using (FileStream fs = new FileStream("mypack.data.zip", FileMode.Create, FileAccess.ReadWrite, FileShare.Read)) {
                MyHead mh = new MyHead();
                byte[] headData = mh.Create();
                fs.Write(headData, 0, headData.Length);
                using (FileStream fs2 = new FileStream(scratchPath, FileMode.Open, FileAccess.Read)) {
                    byte[] buf = new byte[1024];
                    int rc = 0;
                    while ((rc = fs2.Read(buf, 0, buf.Length)) > 0) {
                        fs.Write(buf, 0, rc);
                    }
                }
            }
            //
            //read my zip file with head
            //
            using (FileStream fs = new FileStream("mypack.data.zip", FileMode.Open, FileAccess.Read, FileShare.Read)) {
                byte[] buf = new byte[1024];
                int offset = fs.Read(buf, 0, buf.Length);
                System.Diagnostics.Debug.Assert(offset == 1024);
                //fs.Position = 0L;
                SharpCompress.IO.OffsetStream substream = new SharpCompress.IO.OffsetStream(fs, offset);
                ZipArchive zip = ZipArchive.Open(substream, Options.KeepStreamsOpen);//cann't read
                //ZipArchive zip = ZipArchive.Open(fs, Options.None); //will throw exption
                //ZipArchive zip = ZipArchive.Open(fs, Options.KeepStreamsOpen);//cann't read

                foreach (ZipArchiveEntry zf in zip.Entries) {
                    Console.WriteLine(zf.Key);
                    //bug:the will not none in zipfile
                }

                int jjj = 0;
                jjj++;
            }
        }
        public class MyHead
        {
            public int Id = 10086;
            public string Ver = "1.0.1";
            public string Flag = "MyHead";
            public byte[] Create() {
                byte[] buf = null;
                using (MemoryStream ms = new MemoryStream(1024)) {
                    ms.SetLength(1024);
                    using (BinaryWriter bw = new BinaryWriter(ms, Encoding.UTF8)) {
                        bw.Write(this.Flag);
                        bw.Write(this.Id);
                        bw.Write(this.Ver);

                    }

                    buf = ms.ToArray();
                }
                return buf;
            }
        }
    }
    public class IRQ_VPLDocInfo_Json
    {
        /// <summary>
        /// 序列化函数 必须有公开构造函数
        /// </summary>
        public IRQ_VPLDocInfo_Json() {
            this.AttachFiles = new List<string>();
            this.Author = "";
            this.CodeType = VPL_CodeType.CSharp;
            this.CreateDate = DateTime.Now;
            this.Description = "";
            this.IsCodeMode = false;
            this.IsNoRobotMode = true;
            this.LastUpdate = DateTime.Now;
            this.Name = "undefinevpl";
            this.NoRobotMode_LastPlatId = "";
            this.OnCloudSvr = false;
            this.PackageUniqueId = "";
            this.PackageVersion = "";
            this.Source = IRQ_FileDocSource.User;
            this.UserDefines = new Dictionary<string, string>();
            this.Ver = "2.0";
        }
        public string Name { get; set; }
        public string Ver { get; set; }
        public DateTime LastUpdate { get; set; }
        public IRQ_FileDocSource Source { get; set; }
        public bool OnCloudSvr { get; set; }
        public List<string> AttachFiles { get; set; }
        //
        public string Author { get; set; }
        public VPL_CodeType CodeType { get; set; }
        public DateTime CreateDate { get; set; }
        public string Description { get; set; }
        public bool IsCodeMode { get; set; }
        public bool IsNoRobotMode { get; set; }
        public string NoRobotMode_LastPlatId { get; set; }
        public string PackageUniqueId { get; set; }
        public string PackageVersion { get; set; }
        public Dictionary<string, string> UserDefines { get; set; }
        //public static IRQ_VPLDocInfo_Json CreateBy(IRQ_VPLDocInfo vpldoc) {
        //    IRQ_VPLDocInfo_Json docjs = new IRQ_VPLDocInfo_Json();
        //    docjs.CopyFrom(vpldoc);
        //    return docjs;
        //}
       
        public static string ToJson(IRQ_VPLDocInfo_Json vpldoc) {
            string js = SimpleJsonEx.SimpleJson.SerializeObject(vpldoc);
            return js;
        }
        public static IRQ_VPLDocInfo_Json LoadFromJson(string vpldoc_json) {
            IRQ_VPLDocInfo_Json doc = SimpleJsonEx.SimpleJson.DeserializeObject<IRQ_VPLDocInfo_Json>(vpldoc_json);
            return doc;
        }
    }
    public enum IRQ_FileDocSource : byte
    {
        /// <summary>
        /// 暂时作为大厅任务专用???
        /// </summary>
        None = 0,
        /// <summary>
        /// 自己制作
        /// </summary>
        User = 1,
        /// 系统文件
        /// </summary>
        System = 2,
        /// <summary>
        /// 临时
        /// </summary>
        Temp = 4
    }
    /// <summary>
    /// 生成代码类型
    /// </summary>
    public enum VPL_CodeType
    {
        /// <summary>
        /// 不生成
        /// </summary>
        None = 0,
        /// <summary>
        /// C/C++语言
        /// </summary>
        C = 1,
        /// <summary>
        /// c#
        /// </summary>
        CSharp = 2,
        /// <summary>
        /// VB.net
        /// </summary>
        VB = 3,
        /// <summary>
        /// java
        /// </summary>
        Java = 4,
        /// <summary>
        /// javascript
        /// </summary>
        Js = 5,
        /// <summary>
        /// lua
        /// </summary>
        Lua = 6,
        Python = 7,
        /// <summary>
        /// logo
        /// </summary>
        Logo = 8
    }
    public enum IRQ_FileType : int
    {
        UnKnow,
        /// <summary>
        /// 官方在线任务
        /// </summary>
        OnLineMession = 1,
        /// <summary>
        /// 离线任务
        /// </summary>
        OffLineMession = 2,
        /// <summary>
        /// 用户任务,用户自己制作的任务.
        /// </summary>
        UserMession = 4,
        /// <summary>
        /// 机器人
        /// </summary>
        Robot = 8,
        /// <summary>
        /// 控制程序
        /// </summary>
        CtrlFile = 16,
        /// <summary>
        /// 快速仿真包
        /// </summary>
        QuickSimPack = 32,

        /// <summary>
        /// 完整仿真包
        /// </summary>
        FullSimpack = 128,
        //ToDo:以下为特殊的
        /// <summary>
        /// 系统提供的机器人
        /// </summary>
        SystemRobot = 256,
        /// <summary>
        /// 系统提供的控制程序
        /// </summary>
        SystemCtrlFile = 512,
        /// 用户临时机器人文件
        /// </summary>
        TempUserRobot = 1024,
        /// <summary>
        /// 用户临时控制程序文件
        /// </summary>
        TempUserCtrl = 2048,

        /// <summary>
        /// 场景模板文件
        /// </summary>
        TemplateScene = 4096,

        /// <summary>
        /// 机器人模板文件
        /// </summary>
        TemplateRobot = 8192,
        /// <summary>
        /// VPL模板文件
        /// </summary>
        TemplateVPL = 16384,
        /// <summary>
        /// 导入资源
        /// </summary>
        TempLeadInRes = 32768,
        /// <summary>
        /// 外部可执行程序
        /// </summary>
        ExtExe = 65536,
        /// <summary>
        /// 系统快速仿真包
        /// </summary>
        SystemQuickSimPack = 131072
    }
    public static class ResourceService
    {
        static string m_buf = null;// { 118, 128, 138, 148, 158, 168, 178, 188 };
        static IRQ_Packer m_userData;
        static IRQ_Packer m_sysData;
        static IRQ_Packer m_OnlineScene;
        static IRQ_Packer m_tempData;
        static IRQ_Packer m_sysData_RobotVPL;

        static Dictionary<IRQ_FileType, IFileSysPackerStrategy> Libs = new Dictionary<IRQ_FileType, IFileSysPackerStrategy>();
        public static IFileSysPackerStrategy GetLib(string tableName) {
            foreach (var v in Libs.Keys) {
                int i = Libs[v].Name.LastIndexOf(Path.DirectorySeparatorChar);
                //
                string lastname = Libs[v].Name.Substring(i + 1, Libs[v].Name.Length - i - 1);
                if (lastname.ToLower().CompareTo(tableName.ToLower()) == 0) {
                    return Libs[v];
                }
            }
            return null;
        }
        public static IFileSysPackerStrategy GetLib(IRQ_FileType fileTyp) {
            if (Libs.ContainsKey(fileTyp))
                return Libs[fileTyp];
            else {
                throw new IOException(string.Format("不存在指定的库：{0} ", fileTyp.ToString()));
            }
            return null;
        }
        public static List<IFileSysPackerStrategy> GetLibs(IRQ_FileType fileTyp) {
            List<IFileSysPackerStrategy> filepacks = new List<IFileSysPackerStrategy>();
            //          
            if ((fileTyp & IRQ_FileType.CtrlFile) != 0) {
                filepacks.Add(Libs[IRQ_FileType.CtrlFile]);
            }
            if ((fileTyp & IRQ_FileType.FullSimpack) != 0) {
                filepacks.Add(Libs[IRQ_FileType.FullSimpack]);
            }
            if ((fileTyp & IRQ_FileType.OnLineMession) != 0) {
                filepacks.Add(Libs[IRQ_FileType.OnLineMession]);
            }
            if ((fileTyp & IRQ_FileType.QuickSimPack) != 0) {
                filepacks.Add(Libs[IRQ_FileType.QuickSimPack]);
            }
            if ((fileTyp & IRQ_FileType.Robot) != 0) {
                filepacks.Add(Libs[IRQ_FileType.Robot]);
            }
            if ((fileTyp & IRQ_FileType.OffLineMession) != 0) {
                filepacks.Add(Libs[IRQ_FileType.OffLineMession]);
            }
            if ((fileTyp & IRQ_FileType.UserMession) != 0) {
                filepacks.Add(Libs[IRQ_FileType.UserMession]);
            }
            //
            if ((fileTyp & IRQ_FileType.SystemCtrlFile) != 0) {
                filepacks.Add(Libs[IRQ_FileType.SystemCtrlFile]);
            }
            if ((fileTyp & IRQ_FileType.SystemRobot) != 0) {
                filepacks.Add(Libs[IRQ_FileType.SystemRobot]);
            }
            if ((fileTyp & IRQ_FileType.TempUserCtrl) != 0) {
                filepacks.Add(Libs[IRQ_FileType.TempUserCtrl]);
            }
            if ((fileTyp & IRQ_FileType.TempUserRobot) != 0) {
                filepacks.Add(Libs[IRQ_FileType.TempUserRobot]);
            }
            if ((fileTyp & IRQ_FileType.TemplateScene) != 0) {
                filepacks.Add(Libs[IRQ_FileType.TemplateScene]);
            }
            if ((fileTyp & IRQ_FileType.TemplateRobot) != 0) {
                filepacks.Add(Libs[IRQ_FileType.TemplateRobot]);
            }
            if ((fileTyp & IRQ_FileType.TemplateVPL) != 0) {
                filepacks.Add(Libs[IRQ_FileType.TemplateVPL]);
            }
            if ((fileTyp & IRQ_FileType.TempLeadInRes) != 0) {
                filepacks.Add(Libs[IRQ_FileType.TempLeadInRes]);
            }
            if ((fileTyp & IRQ_FileType.SystemQuickSimPack) != 0) {
                filepacks.Add(Libs[IRQ_FileType.SystemQuickSimPack]);
            }
            return filepacks;
        }
        public static IFileSysPackerStrategy TempLeadInResLib;
        /// <summary>
        /// 用户机器人库
        /// </summary>
        public static IFileSysPackerStrategy UserRobotLib;
        /// <summary>
        /// 用户场景库
        /// </summary>
        public static IFileSysPackerStrategy UserSceneLib;
        /// <summary>
        /// 用户程序库
        /// </summary>
        public static IFileSysPackerStrategy UserCtrlLib;

        /// <summary>
        /// 用户配置库
        /// </summary>
        public static IFileSysPackerStrategy UserSettingLib;
        /// <summary>
        /// 用户快速启动包库
        /// </summary>
        public static IFileSysPackerStrategy QuickLanchLib;
        /// <summary>
        /// 在线系统场景库
        /// </summary>
        public static IFileSysPackerStrategy OnLineSceneLib;

        /// <summary>
        /// 离线场景库
        /// </summary>
        public static IFileSysPackerStrategy OfflineSceneLib;

        /// <summary>
        /// 用户完整仿真包
        /// </summary>
        public static IFileSysPackerStrategy FullSimpackLib;
        /// <summary>
        /// 场景模板
        /// </summary>
        public static IFileSysPackerStrategy TemplateSceneLib;
        /// <summary>
        /// VPL模板
        /// </summary>
        public static IFileSysPackerStrategy TemplateVPLLib;
        /// <summary>
        /// 机器人模板
        /// </summary>
        public static IFileSysPackerStrategy TemplateRobotLib;
        //以下为特殊的
        /// <summary>
        /// 系统机器人
        /// </summary>
        public static IFileSysPackerStrategy SystemRobotLib;
        /// <summary>
        /// 系统控制程序
        /// </summary>
        public static IFileSysPackerStrategy SystemCtrlLib;
        /// <summary>
        /// 系统快速仿真包
        /// </summary>
        public static IFileSysPackerStrategy SystemQuickSimPackLib;
        /// <summary>
        /// 机器人临时文件存放
        /// </summary>
        public static IFileSysPackerStrategy TempUserRobotLib;
        /// <summary>
        ///控制程序临时文件存放
        /// </summary>
        public static IFileSysPackerStrategy TempUserCtrlLib;



        public static void Init() {
            //把静态构造函数中的初始化操作放到这里
            try {
#if !机房版
                //if (IRQ_GameNet.UserService.CurrentUser.Id == 0) {
                //    DebugLog.RaiseErrorReport(ErrorLevel.Dead, "尚未登录,ResourceService须在登录后才能正常初始化。");
                //    return;
                //}
#endif
                if (!System.IO.Directory.Exists("Data")) {
                    System.IO.Directory.CreateDirectory("Data");
                }
                if (!System.IO.Directory.Exists("Users")) {
                    System.IO.Directory.CreateDirectory("Users");
                }
#if PACK
                //--用户数据             

                InitUserData();

                //在线数据
                m_OnlineScene = IRQ_Packer.OpenPacker("Data\\DataD2_dat");
                OnLineSceneLib = m_OnlineScene.GetFileTable("OnlineScene");
                if (OnLineSceneLib == null) {
                    OnLineSceneLib = m_OnlineScene.AddFileTable("OnlineScene");
                }

                //---官方数据
                m_sysData = IRQ_Packer.OpenPacker("Data\\DataD_dat");

                OfflineSceneLib = m_sysData.GetFileTable("OfflineScene");
                if (OfflineSceneLib == null) {
                    OfflineSceneLib = m_sysData.AddFileTable("OfflineScene");
                }
                //自带机器人、程序

                m_sysData_RobotVPL = IRQ_Packer.OpenPacker("Data\\DataD1_dat");
                SystemRobotLib = m_sysData_RobotVPL.GetFileTable("SystemRobot");
                if (SystemRobotLib == null) {
                    SystemRobotLib = m_sysData_RobotVPL.AddFileTable("SystemRobot");
                }

                SystemCtrlLib = m_sysData_RobotVPL.GetFileTable("SystemVPL");
                if (SystemCtrlLib == null) {
                    SystemCtrlLib = m_sysData_RobotVPL.AddFileTable("SystemVPL");
                }
                SystemQuickSimPackLib = m_sysData_RobotVPL.GetFileTable("SystemQuick");
                if (SystemQuickSimPackLib == null) {
                    SystemQuickSimPackLib = m_sysData_RobotVPL.AddFileTable("SystemQuick");
                }
                //--缓存数据
                m_tempData = IRQ_Packer.OpenPacker("Data\\DataE_dat");

                TempUserCtrlLib = m_tempData.GetFileTable("TempVPL");
                if (TempUserCtrlLib == null) {
                    TempUserCtrlLib = m_tempData.AddFileTable("TempVPL");
                }

                TempUserRobotLib = m_tempData.GetFileTable("TempRobot");
                if (TempUserRobotLib == null) {
                    TempUserRobotLib = m_tempData.AddFileTable("TempRobot");
                }
                TempLeadInResLib = m_tempData.GetFileTable("TempLeadInRes");
                if (TempLeadInResLib == null) {
                    TempLeadInResLib = m_tempData.AddFileTable("TempLeadInRes");
                }

#else

                m_CurrentUserDataDir = Path.Combine("Users", IRQ_GameNet.UserService.CurrentUser.Id.ToString());
                IRQ_Utility.CheckDirectory(m_CurrentUserDataDir);

                UserRobotLib = new FilesSystemPackerStrategy();
                UserRobotLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\Robot";
                if (!Directory.Exists(UserRobotLib.Name)) {
                    Directory.CreateDirectory(UserRobotLib.Name);
                }

                UserSceneLib = new FilesSystemPackerStrategy();
                UserSceneLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\Scene";
                if (!Directory.Exists(UserSceneLib.Name)) {
                    Directory.CreateDirectory(UserSceneLib.Name);
                }

                UserCtrlLib = new FilesSystemPackerStrategy();
                UserCtrlLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\VPL";
                if (!Directory.Exists(UserCtrlLib.Name)) {
                    Directory.CreateDirectory(UserCtrlLib.Name);
                }

                QuickLanchLib = new FilesSystemPackerStrategy();
                QuickLanchLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\Quick";
                if (!Directory.Exists(QuickLanchLib.Name)) {
                    Directory.CreateDirectory(QuickLanchLib.Name);
                }
                FullSimpackLib = new FilesSystemPackerStrategy();
                FullSimpackLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\FullQuick";
                if (!Directory.Exists(FullSimpackLib.Name)) {
                    Directory.CreateDirectory(FullSimpackLib.Name);
                }
                TemplateSceneLib = new FilesSystemPackerStrategy();
                TemplateSceneLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\TemplateScene";
                if (!Directory.Exists(TemplateSceneLib.Name)) {
                    Directory.CreateDirectory(TemplateSceneLib.Name);
                }
                TemplateRobotLib = new FilesSystemPackerStrategy();
                TemplateRobotLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\TemplateRobot";
                if (!Directory.Exists(TemplateRobotLib.Name)) {
                    Directory.CreateDirectory(TemplateRobotLib.Name);
                }

                TemplateVPLLib = new FilesSystemPackerStrategy();
                TemplateVPLLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\TemplateVPL";
                if (!Directory.Exists(TemplateVPLLib.Name)) {
                    Directory.CreateDirectory(TemplateVPLLib.Name);
                }

                UserSettingLib = new FilesSystemPackerStrategy();
                UserSettingLib.Name = "Users\\" + IRQ_GameNet.UserService.CurrentUser.Id.ToString() + "\\Setting";
                if (!Directory.Exists(UserSettingLib.Name)) {
                    Directory.CreateDirectory(UserSettingLib.Name);
                }

                OnLineSceneLib = new FilesSystemPackerStrategy();
                OnLineSceneLib.Name = "Data\\DataD2\\OnlineScene";
                if (!Directory.Exists(OnLineSceneLib.Name)) {
                    Directory.CreateDirectory(OnLineSceneLib.Name);
                }

                OfflineSceneLib = new FilesSystemPackerStrategy();
                OfflineSceneLib.Name = "Data\\DataD\\OfflineScene";
                if (!Directory.Exists(OfflineSceneLib.Name)) {
                    Directory.CreateDirectory(OfflineSceneLib.Name);
                }

                //特殊处理的
                SystemRobotLib = new FilesSystemPackerStrategy();
                SystemRobotLib.Name = "Data\\DataD1\\SystemRobot";
                if (!Directory.Exists(SystemRobotLib.Name)) {
                    Directory.CreateDirectory(SystemRobotLib.Name);
                }
                SystemCtrlLib = new FilesSystemPackerStrategy();
                SystemCtrlLib.Name = "Data\\DataD1\\SystemVPL";
                if (!Directory.Exists(SystemCtrlLib.Name)) {
                    Directory.CreateDirectory(SystemCtrlLib.Name);
                }

                SystemQuickSimPackLib = new FilesSystemPackerStrategy();
                SystemQuickSimPackLib.Name = "Data\\DataD1\\SystemQuick";
                if (!Directory.Exists(SystemQuickSimPackLib.Name)) {
                    Directory.CreateDirectory(SystemQuickSimPackLib.Name);
                }

                TempUserRobotLib = new FilesSystemPackerStrategy();
                TempUserRobotLib.Name = "Data\\DataE\\TempRobot";
                if (!Directory.Exists(TempUserRobotLib.Name)) {
                    Directory.CreateDirectory(TempUserRobotLib.Name);
                }
                TempUserCtrlLib = new FilesSystemPackerStrategy();
                TempUserCtrlLib.Name = "Data\\DataE\\TempVPL";
                if (!Directory.Exists(TempUserCtrlLib.Name)) {
                    Directory.CreateDirectory(TempUserCtrlLib.Name);
                }
                TempLeadInResLib = new FilesSystemPackerStrategy();
                TempLeadInResLib.Name = "Data\\DataE\\TempLeadInRes";
                if (!Directory.Exists(TempLeadInResLib.Name)) {
                    Directory.CreateDirectory(TempLeadInResLib.Name);
                }
#endif
                RegistorAllLibs();
            }
            catch (Exception ee) {
                Console.WriteLine(ee.ToString());
            }
        }

        private static void RegistorAllLibs() {
            Libs.Clear();
            Libs.Add(IRQ_FileType.CtrlFile, UserCtrlLib);
            Libs.Add(IRQ_FileType.FullSimpack, FullSimpackLib);
            Libs.Add(IRQ_FileType.OffLineMession, OfflineSceneLib);
            Libs.Add(IRQ_FileType.OnLineMession, OnLineSceneLib);
            Libs.Add(IRQ_FileType.QuickSimPack, QuickLanchLib);
            Libs.Add(IRQ_FileType.Robot, UserRobotLib);
            Libs.Add(IRQ_FileType.SystemCtrlFile, SystemCtrlLib);
            Libs.Add(IRQ_FileType.SystemRobot, SystemRobotLib);
            Libs.Add(IRQ_FileType.TemplateScene, TemplateSceneLib);
            Libs.Add(IRQ_FileType.TemplateRobot, TemplateRobotLib);
            Libs.Add(IRQ_FileType.TemplateVPL, TemplateVPLLib);
            Libs.Add(IRQ_FileType.TempUserCtrl, TempUserCtrlLib);
            Libs.Add(IRQ_FileType.UserMession, UserSceneLib);
            Libs.Add(IRQ_FileType.TempUserRobot, TempUserRobotLib);
            Libs.Add(IRQ_FileType.TempLeadInRes, TempLeadInResLib);
            Libs.Add(IRQ_FileType.SystemQuickSimPack, SystemQuickSimPackLib);
        }
        static ResourceService() {

        }
        private static string m_CurrentUserDataDir = string.Empty;
        //private static string m_newuserlibfile = string.Empty;
        public static string GetCurrentUserDataDir() {
            return m_CurrentUserDataDir;
        }
        //public static string GetCurrentUserDataFile() {
        //    return m_newuserlibfile;
        //}
        private static void InitUserData() {


            //m_newuserlibfile = string.Empty;

            //if (IRQ_GameNet.UserService.CurrentUser.Id == 0) {
            m_CurrentUserDataDir = Path.Combine("Users", "0");
            IRobotQ.PackerDisk.DiskReadZip_FilePacker.CheckDirectory(m_CurrentUserDataDir);

            try {
                m_userData = IRQ_Packer.OpenPacker(m_CurrentUserDataDir);
            }
            catch (Exception ee) {
                //DebugLog.Log("打开游客数据失败", ee, true);
                Console.WriteLine(ee.ToString());
            }
            //}


            UserRobotLib = m_userData.GetFileTable("Robot");
            if (UserRobotLib == null) {
                UserRobotLib = m_userData.AddFileTable("Robot");
            }

            UserSceneLib = m_userData.GetFileTable("Scene");
            if (UserSceneLib == null) {
                UserSceneLib = m_userData.AddFileTable("Scene");
            }

            UserCtrlLib = m_userData.GetFileTable("VPL");
            if (UserCtrlLib == null) {
                UserCtrlLib = m_userData.AddFileTable("VPL");
            }

            QuickLanchLib = m_userData.GetFileTable("Quick");
            if (QuickLanchLib == null) {
                QuickLanchLib = m_userData.AddFileTable("Quick");
            }

            FullSimpackLib = m_userData.GetFileTable("FullQuick");
            if (FullSimpackLib == null) {
                FullSimpackLib = m_userData.AddFileTable("FullQuick");
            }

            TemplateSceneLib = m_userData.GetFileTable("TemplateScene");
            if (TemplateSceneLib == null) {
                TemplateSceneLib = m_userData.AddFileTable("TemplateScene");
            }

            TemplateRobotLib = m_userData.GetFileTable("TemplateRobot");
            if (TemplateRobotLib == null) {
                TemplateRobotLib = m_userData.AddFileTable("TemplateRobot");
            }

            TemplateVPLLib = m_userData.GetFileTable("TemplateVPL");
            if (TemplateVPLLib == null) {
                TemplateVPLLib = m_userData.AddFileTable("TemplateVPL");
            }
            UserSettingLib = m_userData.GetFileTable("Setting");
            if (UserSettingLib == null) {
                UserSettingLib = m_userData.AddFileTable("Setting");
            }
        }

        public static bool CloseUserData() {
            try {
                m_userData.Close();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        public static bool ReOpenUserData() {
            try {
                InitUserData();

                RegistorAllLibs();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }

    }
}





