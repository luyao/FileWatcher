/* 
 * Hawkeye使用说明：
 * Hawkeye是一个文件夹监控库，支持对文件夹内==特定文件== ==添加==以及==删除==的监控
 * 1. 确定需要监控的文件夹地址(绝对地址)，以及需要监控的文件后缀，如果不确定后缀，可以粗暴的使用*.*
 * 2. 实现两个代理函数，分别为当新增了此类型的文件如何处理，以及删除了此类型的文件如何处理；
 * 3. 提供了全局入口，不需要new 类出来，库会保证单例（非线程安全）
 * 使用方法如下：
 * // step1st 首先定义两个回调函数，分别为创建和删除文件需要的回调
 * bool create4Fun(string path){
 *      if (File.Exists(paht)) {
 *          //do something
 *      }
 * }
 * bool delete4Fun(string path){
 *      if (!File.Exists(paht)) {
 *          //do something
 *      }
 * }
 * // step 2nd 使用局部变量缓存，或者每次都使用，whatever
 * // 第一次调用的时候必须指明回调，后续的调用中，必须有path和filter
 * Hawkeye local_eye = GlobalHawkeye.instance(somePath, "*.log", create4Fun, delete4Fun);
 * //后续可以这样使用（其实也不需要使用了，目前没有设计接口）：
 * GlobalHawkeye.instance(somePath, "*.log").doSomething();
 * 
 * // 然后就没有然后了，之后对文件夹做的所有增加和删除的操作，都会调用回调
 * // Have Fun!!!
 * 
 * */
using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Permissions; //for file watcher

namespace Warptele.Hawkeye
{
    public delegate bool CreateFn(string fullPath);
    public delegate bool DeleteFn(string fullPath);
    static public class GlobalHawkeye
    {
        static public Dictionary<string, Hawkeye> mHawkeyes = new Dictionary<string,Hawkeye>();

        static public Hawkeye instance(string path, string filter,  CreateFn create,  DeleteFn delete)
        {
            if (!mHawkeyes.ContainsKey(path+filter))
            {
                Hawkeye _eye = new Hawkeye(path, filter);
                _eye.Create = create;
                _eye.Delete = delete;
                mHawkeyes.Add(path+filter, _eye);
                return _eye;
            }
            else
            {
                return mHawkeyes[path + filter];
            }
        }
        static public Hawkeye instance(string path, string filter)
        {
            if (mHawkeyes.ContainsKey(path + filter))
            {
                return mHawkeyes[path + filter];
            }
            else
            {
                return null;
            }
        }
    }

    // this class is using to access all files in the path given by configure
    public class Hawkeye
    {
        // file watcher
        private FileSystemWatcher mWatcher = new FileSystemWatcher();
        public CreateFn Create{get; set;}
        public DeleteFn Delete{get; set;}
        public Hawkeye(string path, string filter)
        {
            if (!Directory.Exists(path))
            {
                throw new Exception("找不到路径：" + path);
            }

            mWatcher.Path = path;
            mWatcher.Filter = filter;
            mWatcher.NotifyFilter = NotifyFilters.FileName;  //only care about new and delete

            // Add event handlers.
            mWatcher.Created += new FileSystemEventHandler(OnCreate);
            mWatcher.Deleted += new FileSystemEventHandler(OnDelete);

            // Begin watching.
            mWatcher.EnableRaisingEvents = true;
        }
        private bool IsFileReady(string filename)
        {
            FileInfo fi = new FileInfo(filename);
            FileStream fs = null;
            try
            {
                fs = fi.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }

        // Define the event handlers.
        private  void OnCreate(object source, FileSystemEventArgs e)
        {
            while (!IsFileReady(e.FullPath)) {
                Thread.Sleep(500);
            }

            if (File.Exists(e.FullPath)) {
                //call real OnCreate, which is template method
                Create(e.FullPath);
            }
        }
        private void OnDelete(object source, FileSystemEventArgs e)
        {
            if(!File.Exists(e.FullPath))
            {
                Delete(e.FullPath);
            }
        }

    }

    /*
    public class TestHawkeye
    {
        private static string mDirectoryPath = @"c:\logs\";
        private int index;
        private void createDirectory()
        {
            if (!Directory.Exists(mDirectoryPath))
            {
                Directory.CreateDirectory(mDirectoryPath);
            }
        }
        private void createFiles()
        {
            for (int i = 0; i < 10; i++)
            {
                var name = string.Format("{0}{1}.log", mDirectoryPath, i);
                if (!File.Exists(name))
                {
                    File.Create(name);
                }
            }
        }
        private void deleteFiles()
        {
            for (int i = 0; i < 10; i++)
            {
                var name = string.Format("{0}{1}.log", mDirectoryPath, i);
                if (File.Exists(name))
                {
                    File.Delete(name);
                }
            }
        }

        private bool TestCreate(string fullPath)
        {
            Console.WriteLine("发现文件：{0}", fullPath);
            ++index;
            return true;
        }
        private bool TestDelete(string fullPath)
        {
            Console.WriteLine("删除文件：{0}", fullPath);
            --index;
            return true;
        }

        public void tc()
        {
            Hawkeye eye = GlobalHawkeye.instance(mDirectoryPath, "*.log", TestCreate, TestDelete);
            Thread.Sleep(500000000);
        }
        public void td()
        {
            Hawkeye eye = GlobalHawkeye.instance(mDirectoryPath, "*.log", TestCreate, TestDelete);
            deleteFiles();
            //Assert.AreEqual(0, index);
        }
        static void Main(string[] args)
        {
            TestHawkeye test = new TestHawkeye();
            test.tc();
            //test.td();
        }
    }
     * */
}

