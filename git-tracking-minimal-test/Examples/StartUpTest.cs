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

using System.IO;
using git_tracking_minimal;
using LibGit2Sharp;
using NUnit.Framework;

namespace git_tracking_minimal_test.Examples
{
    internal class StartUpTest : RepositoryBaseTest
    {
        [Test]
        public void StartedWithNoRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            Directory.CreateDirectory(dir);
            var sut = new GitTracker(dir);

            Assert.IsFalse(sut.State.IsGitEnabled);
            Assert.AreEqual(Path.Combine(DirTestRoot, "SomeDir"), sut.State.TrackedFolder);
            Assert.Null(sut.State.GitFolder);
        }

        [Test]
        public void StartedWithRepository()
        {
            Assert.IsTrue(Sut.State.IsGitEnabled);
            Assert.AreEqual(DirRepo, Sut.State.TrackedFolder);
            Assert.AreEqual(DirGit, Sut.State.GitFolder);
        }

        [Test]
        public void CanBeStartedOnRepositoriesThatAreStillMissingFiles()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var git = Path.Combine(dir, ".git");
            Directory.CreateDirectory(git);
            var sut = new GitTracker(dir);

            Assert.IsTrue(sut.State.IsGitEnabled);
            Assert.AreEqual(dir, sut.State.TrackedFolder);
            Assert.AreEqual(git, sut.State.GitFolder);
        }

        [Test]
        public void WillHandleRemovalOfRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var git = Path.Combine(dir, ".git");
            Directory.CreateDirectory(git);

            var sut = new GitTracker(dir);
            Assert.IsTrue(sut.State.IsGitEnabled);

            Directory.Delete(git, true);

            Assert.IsFalse(sut.State.IsGitEnabled);
            Assert.AreEqual(Path.Combine(DirTestRoot, "SomeDir"), sut.State.TrackedFolder);
            Assert.Null(sut.State.GitFolder);
        }

        [Test]
        public void WillHandleRemovalAndReadditionOfRepository()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            var git = Path.Combine(dir, ".git");
            Directory.CreateDirectory(git);

            var sut = new GitTracker(dir);
            Assert.IsTrue(sut.State.IsGitEnabled);
            Directory.Delete(git, true);
            Assert.IsFalse(sut.State.IsGitEnabled);
            Directory.CreateDirectory(git);

            Assert.IsTrue(sut.State.IsGitEnabled);
            Assert.AreEqual(dir, sut.State.TrackedFolder);
            Assert.AreEqual(git, sut.State.GitFolder);
        }

        [Test]
        public void CanBeStartedOnFolderThatWillBecomeRepositories()
        {
            var dir = Path.Combine(DirTestRoot, "SomeDir");
            Directory.CreateDirectory(dir);
            var sut = new GitTracker(dir);

            var hasNotification = false;
            sut.GitEnabled += () => hasNotification = true;

            Assert.IsFalse(sut.State.IsGitEnabled);
            Assert.AreEqual(Path.Combine(DirTestRoot, "SomeDir"), sut.State.TrackedFolder);
            Assert.Null(sut.State.GitFolder);

            Repository.Init(dir);

            Assert.IsTrue(hasNotification);
            Assert.IsTrue(sut.State.IsGitEnabled);
            Assert.AreEqual(Path.Combine(DirTestRoot, "SomeDir"), sut.State.TrackedFolder);
            Assert.AreEqual(Path.Combine(DirTestRoot, "SomeDir", ".git"), sut.State.GitFolder);
        }

        [Test]
        public void GitFolderLivesInParentFolder()
        {
            Assert.Fail();
        }
    }
}