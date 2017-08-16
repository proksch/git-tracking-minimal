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
using System.Threading;
using git_tracking_minimal.GitTracking;
using LibGit2Sharp;
using NUnit.Framework;

namespace git_tracking_minimal_test.GitTracking
{
    internal class GitTrackerTest : FolderBasedTest
    {
        private GitTracker _sut;

        [TearDown]
        public void TearDown()
        {
            _sut?.Dispose();
            _sut = null;
        }

        [Test]
        public void StartedWithNoRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            Directory.CreateDirectory(dir);
            _sut = new GitTracker(dir);
            _sut.StartTracking();

            AssertInit(Path.Combine(DirTestRoot, "SomeDir"), null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void CannotBeStartedWithFiles()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var file = Path.Combine(dir, "abc.txt");
            Directory.CreateDirectory(dir);
            File.Create(file).Close();
            // ReSharper disable once ObjectCreationAsStatement
            new GitTracker(file);
        }

        [Test]
        public void StartedOnEmptyRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var git = Path.Combine(dir, ".git");
            Repository.Init(dir);
            _sut = new GitTracker(dir);
            _sut.StartTracking();
            AssertInit(dir, Path.Combine(dir, ".git"));

            Assert.True(File.Exists(Path.Combine(git, "index")));
            Assert.True(Directory.Exists(Path.Combine(git, "logs")));
            Assert.True(File.Exists(Path.Combine(git, "logs", "HEAD")));
            Assert.True(Directory.Exists(Path.Combine(git, "refs")));
        }

        [Test]
        public void StartedOnEstablishedRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            Repository.Init(dir);
            var repo = new Repository(dir);

            var file = Path.Combine(dir, "a.txt");
            File.WriteAllText(file, "...");
            Commands.Stage(repo, "a.txt");
            var sig = new Signature("Test", "em@i.l", DateTimeOffset.Now);
            repo.Commit("commit msg", sig, sig, new CommitOptions {AllowEmptyCommit = false});

            _sut = new GitTracker(dir);
            _sut.StartTracking();
            AssertInit(dir, Path.Combine(dir, ".git"));
        }

        [Test]
        public void GitFolderLivesInParentFolder()
        {
            Assert.Fail();
        }

        [Test]
        public void StartedOnFutureRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            Directory.CreateDirectory(dir);
            _sut = new GitTracker(dir);
            _sut.StartTracking();
            AssertInit(dir, null);


            var hasNotification = false;
            _sut.GitEnableChanged += () => hasNotification = true;

            Assert.IsFalse(hasNotification);
            Repository.Init(dir);

            // wait for watcher to finish
            Thread.Sleep(500);

            Assert.IsTrue(hasNotification);
            AssertInit(dir, Path.Combine(dir, ".git"));
        }

        [Test]
        public void RemovalOfRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var git = Path.Combine(dir, ".git");
            Directory.CreateDirectory(dir);
            Repository.Init(dir);

            _sut = new GitTracker(dir);
            _sut.StartTracking();

            AssertInit(dir, git);

            var hasNotification = false;
            _sut.GitEnableChanged += () => hasNotification = true;

            Assert.IsFalse(hasNotification);
            DeleteDirectory(git);

            Thread.Sleep(500);
            Assert.IsTrue(hasNotification);
            AssertInit(dir, null);
        }

        [Test]
        public void RemovalAndReaddition_Simple()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var git = Path.Combine(dir, ".git");
            Directory.CreateDirectory(git);

            var sut = new GitTracker(dir);
            sut.StartTracking();
            Assert.IsTrue(sut.State.IsGitEnabled);
            Directory.Delete(git, true);
            Assert.IsFalse(sut.State.IsGitEnabled);
            Directory.CreateDirectory(git);

            Assert.IsTrue(sut.State.IsGitEnabled);
            Assert.AreEqual(dir, sut.State.TrackedFolder);
            Assert.AreEqual(git, sut.State.GitFolder);
        }

        [Test]
        public void RemovalAndReaddition_Hierarchy()
        {
            Assert.Fail();
        }

        private void AssertInit(string pathTargetFolder, object pathDotGit)
        {
            var isEnabled = pathDotGit != null;
            Assert.AreEqual(isEnabled, _sut.State.IsGitEnabled);
            Assert.AreEqual(pathTargetFolder, _sut.State.TrackedFolder);
            Assert.AreEqual(pathDotGit, _sut.State.GitFolder);
        }
    }
}