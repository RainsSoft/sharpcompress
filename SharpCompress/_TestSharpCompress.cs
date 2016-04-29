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
    using IRQ_Packer = IRobotQ.PackerDisk.DiskZip_FilePacker;
    static class _TestSharpCompress
    {
        public static void Main(string[] args) {
            int timestart = Environment.TickCount;
            ResourceService.Init();
            //增加文件
            IFilePackerStrategy fps = ResourceService.GetLib(IRQ_FileType.TempLeadInRes);
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
            fps.UpdateFile("/test/dir2/mypack2.data.zip",new byte[]{1,2,3,4},DateTime.Now);
            fps.AddFile("/test2/mypack.data.zip", buf_add, DateTime.Now);//addfile
            fps.RenameFile("/test2/mypack.data.zip","/test2/mypack2.data.zip");
            fps.RenameDir("test","test3");
            List<string> getDirs = new List<string>();
            fps.GetDirs(out getDirs);
            fps.Clean();

            fps.AddFile("/test3/mypack.data.zip", buf_add, FileEntryInfo.DateTimeFromStr_STC( "2016-01-01 12:12:12"));//addfile
            fps.AddFile("/test4/mypack.data.zip", buf_add, DateTime.Now);//addfile
            getDirs = new List<string>();
            fps.GetDirs(out getDirs);
            DateTime dtupdate = fps.GetUpdateDate("test3/mypack.data.zip");
            List<string> filenames = new List<string>();
            int totalSize = 0;
            fps.GetFiles("test3",out filenames,out totalSize);
            fps.DelDir("test3");
            fps.RenameDir("test4","test");
         
            fps.Packer.EndUpdate(fps, true);
            Console.WriteLine("耗时："+(Environment.TickCount-timestart).ToString()+" ms");
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

        static Dictionary<IRQ_FileType, IFilePackerStrategy> Libs = new Dictionary<IRQ_FileType, IFilePackerStrategy>();
        public static IFilePackerStrategy GetLib(string tableName) {
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
        public static IFilePackerStrategy GetLib(IRQ_FileType fileTyp) {
            if (Libs.ContainsKey(fileTyp))
                return Libs[fileTyp];
            else {
                throw new IOException(string.Format("不存在指定的库：{0} ", fileTyp.ToString()));
            }
            return null;
        }
        public static List<IFilePackerStrategy> GetLibs(IRQ_FileType fileTyp) {
            List<IFilePackerStrategy> filepacks = new List<IFilePackerStrategy>();
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
        public static IFilePackerStrategy TempLeadInResLib;
        /// <summary>
        /// 用户机器人库
        /// </summary>
        public static IFilePackerStrategy UserRobotLib;
        /// <summary>
        /// 用户场景库
        /// </summary>
        public static IFilePackerStrategy UserSceneLib;
        /// <summary>
        /// 用户程序库
        /// </summary>
        public static IFilePackerStrategy UserCtrlLib;

        /// <summary>
        /// 用户配置库
        /// </summary>
        public static IFilePackerStrategy UserSettingLib;
        /// <summary>
        /// 用户快速启动包库
        /// </summary>
        public static IFilePackerStrategy QuickLanchLib;
        /// <summary>
        /// 在线系统场景库
        /// </summary>
        public static IFilePackerStrategy OnLineSceneLib;

        /// <summary>
        /// 离线场景库
        /// </summary>
        public static IFilePackerStrategy OfflineSceneLib;

        /// <summary>
        /// 用户完整仿真包
        /// </summary>
        public static IFilePackerStrategy FullSimpackLib;
        /// <summary>
        /// 场景模板
        /// </summary>
        public static IFilePackerStrategy TemplateSceneLib;
        /// <summary>
        /// VPL模板
        /// </summary>
        public static IFilePackerStrategy TemplateVPLLib;
        /// <summary>
        /// 机器人模板
        /// </summary>
        public static IFilePackerStrategy TemplateRobotLib;
        //以下为特殊的
        /// <summary>
        /// 系统机器人
        /// </summary>
        public static IFilePackerStrategy SystemRobotLib;
        /// <summary>
        /// 系统控制程序
        /// </summary>
        public static IFilePackerStrategy SystemCtrlLib;
        /// <summary>
        /// 系统快速仿真包
        /// </summary>
        public static IFilePackerStrategy SystemQuickSimPackLib;
        /// <summary>
        /// 机器人临时文件存放
        /// </summary>
        public static IFilePackerStrategy TempUserRobotLib;
        /// <summary>
        ///控制程序临时文件存放
        /// </summary>
        public static IFilePackerStrategy TempUserCtrlLib;



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
            IRobotQ.PackerDisk.DiskZip_FilePacker.CheckDirectory(m_CurrentUserDataDir);

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
    }
}




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
    public class DiskZip_FilePacker : IFilePacker
    {

        /// <summary>
        /// 根目录,根目录下创建多个数据库，一个库对应一张数据表
        /// </summary>
        public string RootDir { get; private set; }
        /// <summary>
        /// 数据库设计结构为：一张表对应一个数据库文件
        /// 这样就能使用对象来存储数据了
        /// </summary>
        private Dictionary<string, DiskZip_ConnectInfo> m_Conns = new Dictionary<string, DiskZip_ConnectInfo>();
        protected DiskZip_FilePacker(string rootDir) {
            this.RootDir = _getFileLegalLowerDir(rootDir);
            if (!Directory.Exists(rootDir)) {
                CheckDirectory(rootDir);
            }
        }
        internal static DiskZip_FilePacker OpenPacker(string rootDir) {
            DiskZip_FilePacker packer = new DiskZip_FilePacker(rootDir);
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
        internal DiskZip_ConnectInfo CheckConnection(string tableName) {

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
                DiskZip_ConnectInfo ci = new DiskZip_ConnectInfo();
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

                DiskZip_ConnectInfo m_Conn = CheckConnection(tableName);
                IFilePackerStrategy ret = new DiskZip_FileTable(this, m_Conn);
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
            DiskZip_ConnectInfo m_Conn = CheckConnection(tableName);

            IFilePackerStrategy ret = new DiskZip_FileTable(this, m_Conn);
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
                DiskZip_ConnectInfo m_Conn = CheckConnection(tableName);
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
            DiskZip_ConnectInfo conn = CheckConnection(tableName);
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
            DiskZip_ConnectInfo conn = CheckConnection(tableName);
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
    public class DiskZip_ConnectInfo
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

        public void AddFile(DiskZip_FileItemInfo file) {
            this.Open();
            //ToDo:
            MemoryStream ms = new MemoryStream(file.FileData);
            string filefullpath = DiskZip_FilePacker._getFileLegalLowerDir(Path.Combine(file.FileDir, file.FileName));
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
    public class DiskZip_FileItemInfo : FileEntryInfo
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
    internal class DiskZip_FileTable : IFilePackerStrategy
    {
        /// <summary>
        /// 一份关联
        /// </summary>
        private DiskZip_ConnectInfo m_Conn;

        private DiskZip_FilePacker m_Packer;
        //
        internal DiskZip_FileTable(DiskZip_FilePacker packer, DiskZip_ConnectInfo conn) {
            m_Conn = conn;
            //m_buf = arg;
            m_Packer = packer;
        }
        public IFilePacker Packer {
            get { return m_Packer; }
        }
        /// <summary>
        /// tablename
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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            DiskZip_FileItemInfo fi = new DiskZip_FileItemInfo();
            string strFile;
            string strDir = GetFirstDir(strFileName, out strFile);
            fi.FileDir = strDir;//Path.GetDirectoryName(strFileName);
            fi.FileName = strFile;//Path.GetFileName(strFileName);
            fi.FileLen = fileData.Length;
            fi.FileUpdateTime = fi.DateTimeToStr(date);//.ToString("yyyy-MM-dd HH:mm");
            fi.FileData = fileData;
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            strNewFile = DiskZip_FilePacker._getFileLegalLowerDir(strNewFile);
            if (string.IsNullOrEmpty(strFileName) || string.IsNullOrEmpty(strNewFile)) return;//名称不能为空
            if (string.Compare(strFileName, strNewFile) == 0) return;//没有修改

            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strDirName = DiskZip_FilePacker._getFileLegalLowerDir(strDirName);// +"/";//规范化
            strNewDirName = DiskZip_FilePacker._getFileLegalLowerDir(strNewDirName);// +"/";//规范化
            if (string.IsNullOrEmpty(strDirName) || string.IsNullOrEmpty(strNewDirName)) {
                return;//根目录名称不能改
            }
            if (string.Compare(strDirName, strNewDirName) == 0) {
                return;//没有改名
            }
            strDirName = strDirName + "/";
            strNewDirName = strNewDirName + "/";

            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;

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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            //
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strDir = DiskZip_FilePacker._getFileLegalLowerDir(strDir);
            if (string.IsNullOrEmpty(strDir)) {
                return;//不能删除所有？
            }
            strDir = strDir + "/";
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                return;
            }
            //压缩包文件存储格式
            //目录 "a/b/c/"
            //文件 "a/test.txt"            
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                return false;
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strDir = DiskZip_FilePacker._getFileLegalLowerDir(strDir);
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            DiskZip_ConnectInfo conn = this.m_Conn;
            conn.Open();
            string fileInDir = "";
            string strFile;
            foreach (var ze in conn.ZipTarget.Entries) {
                if (ze.Key != "/") {//最顶层就不要了
                    fileInDir = DiskZip_FilePacker._getFileLegalLowerDir(ze.Key);
                    fileInDir = GetFirstDir(fileInDir, out strFile);
                    if (!dirs.Contains(fileInDir)) {
                        dirs.Add(fileInDir);
                    }
                }
            }

            return dirs.Count;
        }



        public byte[] OpenFile(string strFileName) {
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            strFileName = DiskZip_FilePacker._getFileLegalLowerDir(strFileName);
            if (string.IsNullOrEmpty(strFileName)) {
                throw new ArgumentNullException(strFileName);
            }
            this.m_Conn = this.m_Packer.CheckConnection(this.Name);
            DiskZip_ConnectInfo conn = this.m_Conn;
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
            DiskZip_ConnectInfo conn = this.m_Conn;
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

    }
    internal class DiskZip_AccessPackerException : Exception
    {
        public DiskZip_AccessPackerException(string msg, Exception inner) : base(msg, inner) { }
    }
}
