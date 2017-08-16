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
using git_tracking_minimal.GitTracking;
using LibGit2Sharp;
using NUnit.Framework;

namespace git_tracking_minimal_test.GitTracking.StaticGitTrackerTestSuite
{
    internal class ExamplesTest : FolderBasedTest
    {
        protected Signature Sig = new Signature("Test", "em@i.l", DateTimeOffset.Now);
        protected Repository Repo { get; private set; }
        protected StaticGitTracker Sut { get; private set; }

        [Test]
        public void SimpleCommit()
        {
            Write("b.txt", 1, '2', "3", 0x4);
            Commands.Stage(Repo, "*");
            Commit("another file");
        }

        protected void Commit(string message)
        {
            Repo.Commit(message, Sig, Sig, new CommitOptions {AllowEmptyCommit = false});
        }

        protected void AssertNumEvents(int i) { }

        protected void Write(string fileName, params object[] lines)
        {
            var path = Path.Combine(DirTestRoot, fileName);
            var contents = string.Join("\n", lines);
            File.WriteAllText(path, contents);
        }
    }
}