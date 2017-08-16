/*
 * Copyright 2017 Sebastian Proksch
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using NUnit.Framework;

namespace git_tracking_minimal_test.GitTracking
{
    internal abstract class FolderBasedTest
    {
        protected string DirTestRoot { get; private set; }


        [SetUp]
        public void SetUp_FolderBasedTest()
        {
            DirTestRoot = Path.GetTempFileName();
            File.Delete(DirTestRoot); // GetTempFileName() automatically creates an empty file
            Directory.CreateDirectory(DirTestRoot);
        }

        [TearDown]
        public void TearDown_FolderBasedTest()
        {
            DeleteDirectory(DirTestRoot);
        }

        protected static void DeleteDirectory(string dir)
        {
            try
            {
                var directory = new DirectoryInfo(dir) {Attributes = FileAttributes.Normal};

                foreach (var file in directory.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                }

                directory.Delete(true);
            }
            catch
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    if (Directory.Exists(dir))
                    {
                        throw new Exception($"Cannot delete {dir}. Retrying...");
                    }
                }
            }
        }
    }
}