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
using git_tracking_minimal;
using LibGit2Sharp;
using NUnit.Framework;

namespace git_tracking_minimal_test.Examples
{
    internal class RepositoryBaseTest
    {
        private int _numEvents;

        protected Signature Sig = new Signature("Test", "em@i.l", DateTimeOffset.Now);
        protected string DirTestRoot { get; private set; }
        protected string DirRepo { get; private set; }
        protected string DirGit { get; private set; }
        protected Repository Repo { get; private set; }
        protected GitTracker Sut { get; private set; }


        [SetUp]
        public void SetUp_RepositoryBaseTest()
        {
            DirTestRoot = Path.GetTempFileName();
            DirRepo = Path.Combine(DirTestRoot, "repo");
            DirGit = DirRepo + Path.DirectorySeparatorChar + ".git";

            File.Delete(DirTestRoot); // GetTempFileName() automatically creates an empty file
            Directory.CreateDirectory(DirRepo);

            Repository.Init(DirRepo);

            Repo = new Repository(DirRepo);
            Write("a.txt", 1, 2, 3);
            Commands.Stage(Repo, "a.txt");
            Commit("Initial commit.");

            Sut = new GitTracker(DirRepo);

            _numEvents = 0;
            /*Tracker.RemoteAdded += s => _numEvents++;
            Tracker.RemoteRenamed += s => _numEvents++;
            Tracker.RemoteDeleted += s => _numEvents++;
            */
            Sut.Run();
        }

        [TearDown]
        public void TearDown_RepositoryBaseTest()
        {
            Repo?.Dispose();
            Sut?.Dispose();

            DeleteDirectory(DirTestRoot);
        }

        private static void DeleteDirectory(string dir)
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
                Directory.Delete(dir, true);
            }
        }

        protected void Commit(string message)
        {
            Repo.Commit(message, Sig, Sig, new CommitOptions {AllowEmptyCommit = false});
        }

        protected void AssertNumEvents(int i) { }

        protected void Write(string fileName, params object[] lines)
        {
            var path = Path.Combine(DirRepo, fileName);
            var contents = string.Join("\n", lines);
            File.WriteAllText(path, contents);
        }
    }
}