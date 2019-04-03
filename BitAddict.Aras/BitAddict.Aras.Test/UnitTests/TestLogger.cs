﻿using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MSTestHacks;

namespace BitAddict.Aras.Test.UnitTests
{
    [TestClass]
    public class TestLogger : TestBase
    {
        private string _logdir;
        private readonly List<string> _tempFiles = new List<string>();

        [TestInitialize]
        public void Initialize()
        {
            _logdir = Path.Combine(TestContext.TestDir,
                $"{TestContext.TestName}_logfile.txt");

            Directory.CreateDirectory(_logdir);
        }

        [TestCleanup]
        public void DeleteFiles()
        {
            foreach (var file in _tempFiles)
                File.Delete(file);

            Directory.Delete(_logdir);
        }

        [TestMethod]
        public void TestSingleThread()
        {
            const string msg = "test";
            string logfile;
            using (var logger = new Logger(_logdir))
            {
                logfile = logger.LogFile;
                _tempFiles.Add(logfile);
                logger.Log(msg);
            }

            var logText = File.ReadAllText(logfile);
            Assert.AreEqual(msg + "\n", logText);
        }

        [TestMethod]
        public void TestSequential()
        {
            string file1, file2;

            using (var l1 = new Logger(_logdir))
            {
                file1 = l1.LogFile;
                _tempFiles.Add(file1);
            }

            using (var l2 = new Logger(_logdir))
            {
                file2 = l2.LogFile;
                _tempFiles.Add(file2);
            }

            Assert.AreEqual(file1, file2);
        }

        [TestMethod]
        public void TestParallell()
        {
            string file1, file2;

            using (var l1 = new Logger(_logdir))
            {
                file1 = l1.LogFile;
                _tempFiles.Add(file1);
                l1.Log("test1");

                using (var l2 = new Logger(_logdir))
                {
                    file2 = l2.LogFile;
                    _tempFiles.Add(file2);
                    l2.Log("test2");
                }
            }

            Assert.AreNotEqual(file1, file2);

            Assert.AreEqual("test1\n", File.ReadAllText(file1));
            Assert.AreEqual("test2\n", File.ReadAllText(file2));
        }


        [TestMethod]
        public void TestLogOrder()
        {
            string file;

            using (var l1 = new Logger(_logdir))
            {
                file = l1.LogFile;
                _tempFiles.Add(file);

                for (var i = 0; i < 100; ++i)
                    l1.Log($"test {i}\n");
            }

            var text = File.ReadAllText(file);

            var lines = text.Split('\n').GetEnumerator();
            var l = 0;
            for (; l < 100 && lines.MoveNext(); ++l)
                Assert.AreEqual($"test {l}", lines.Current);

            Assert.AreEqual(100, l);
        }
    }
}
