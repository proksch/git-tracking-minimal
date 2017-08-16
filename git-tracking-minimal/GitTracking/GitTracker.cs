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

namespace git_tracking_minimal.GitTracking
{
    public class GitTracker : IDisposable
    {
        private readonly string _dirSln;

        private StaticGitTracker _cur;
        private string _dirGit;

        private FileSystemWatcher _watcherRepo;

        public GitState State { get; }

        public bool IsGitEnabled => false;

        public GitTracker(string dirSln)
        {
            _dirSln = dirSln;

            if (File.Exists(_dirSln))
            {
                throw new ArgumentException("cannot track files");
            }
            State = new GitState
            {
                TrackedFolder = _dirSln
            };
        }

        public void Dispose()
        {
            _watcherRepo?.Dispose();
            Disable();
        }

        public event Action GitEnableChanged;

        public void StartTracking()
        {
            // TODO also watch in parents
            _dirGit = Path.Combine(_dirSln, ".git");
            if (Directory.Exists(_dirGit))
            {
                Enable(_dirGit);
            }

            // TODO also watch in parents
            _watcherRepo = new FileSystemWatcher
            {
                Path = _dirSln,
                Filter = ".git",
                EnableRaisingEvents = true
            };
            _watcherRepo.Created += (target, args) =>
            {
                _dirGit = args.FullPath;
                // creation of repo takes longer then creation of base dir
                Thread.Sleep(500);
                Enable(args.FullPath);
            };
            _watcherRepo.Deleted += (target, args) =>
            {
                  Disable();
            };
        }

        private void Enable(string dirGit)
        {
            State.GitFolder = dirGit;
            _cur = new StaticGitTracker(State);
            GitEnableChanged?.Invoke();
        }

        private void Disable()
        {
            if (_cur != null)
            {
                _cur.Dispose();
                _cur = null;
            }
            State.GitFolder = null;
            GitEnableChanged?.Invoke();
        }
    }
}