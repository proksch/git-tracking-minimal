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
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;

namespace git_tracking_minimal
{
    internal class Tracker : IDisposable
    {
        private readonly string _dirLogs;
        private readonly string _dirRefs;
        private readonly string _dirRepo;

        private readonly Action<string> _log;


        private readonly List<string> _seenHeadLines = new List<string>();
        private Repository _repo;
        private FileSystemWatcher _watcherLogs;
        private FileSystemWatcher _watcherRefs;
        private FileSystemWatcher _watcherObj;

        public Tracker(string pathToSomeGitRepo, Action<string> log)
        {
            if (!Directory.Exists(pathToSomeGitRepo + @"\.git\"))
            {
                log($"[{TimeStamp}] {pathToSomeGitRepo} does not contain a Git repository");
                return;
            }
            var hasLogs = Directory.Exists(pathToSomeGitRepo + @"\.git\logs\");
            var hasRefs = Directory.Exists(pathToSomeGitRepo + @"\.git\refs\");
            var hasHead = File.Exists(pathToSomeGitRepo + @"\.git\logs\HEAD");
            var hasIdx = File.Exists(pathToSomeGitRepo + @"\.git\index");
            if (!hasLogs || !hasRefs || !hasHead || !hasIdx)
            {
                log($"[{TimeStamp}] {pathToSomeGitRepo} does not contain a valid Git repository");
                return;
            }

            _dirLogs = pathToSomeGitRepo + @".git\logs\";
            _dirRefs = pathToSomeGitRepo + @".git\refs\";
            _dirRepo = pathToSomeGitRepo + @".git";
            _log = log;

            _log($"[{TimeStamp}] Tracking Git repository {pathToSomeGitRepo} ...");
        }

        public string TimeStamp => $"{DateTime.Now:HH:mm:ss.fff}";

        public void Dispose()
        {
            _watcherObj?.Dispose();
            _watcherRefs?.Dispose();
            _watcherLogs?.Dispose();
            _repo?.Dispose();
        }

        public void Run()
        {
            if (_log == null)
            {
                return;
            }
            _repo = new Repository(_dirRepo);
            ReadNewHeadLines();

            _watcherLogs = new FileSystemWatcher
            {
                Path = _dirLogs,
                Filter = "HEAD",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcherLogs.Changed += OnHeadChange;
            _watcherLogs.Error += OnError;

            _watcherRefs = new FileSystemWatcher
            {
                Path = _dirRefs,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcherRefs.Changed += OnRefsChange;
            _watcherRefs.Error += OnError;

            _watcherObj = new FileSystemWatcher
            {
                Path = _dirRepo,
                Filter = "index",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcherObj.Changed += OnIndexChange;
            _watcherObj.Error += OnError;
        }

        private void OnRefsChange(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }
            _log($"[{TimeStamp}] ref changed: {e.FullPath}");
        }

        private void OnHeadChange(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }
            var newLines = ReadNewHeadLines();

            _log($"[{TimeStamp}] head changed");
        }

        private IList<string> ReadNewHeadLines()
        {
            var pathHead = Path.Combine(_dirLogs, "HEAD");

            var newLines = new List<string>();
            foreach (var line in File.ReadAllLines(pathHead))
            {
                if (!_seenHeadLines.Contains(line))
                {
                    newLines.Add(line);

                   var e = new GitLogEntry(line);


                    //Console.WriteLine("# ----------------------------------------");
                    Console.WriteLine("# " + e.CommitFrom + " > " + e.CommitTo);
                    //Console.WriteLine("# " + user);
                    Console.WriteLine("# " + e.Time);
                    Console.WriteLine("# " + e.Msg);
                }
            }

            _seenHeadLines.AddRange(newLines);
            return newLines;
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            _log($"[{TimeStamp}] error!");
        }

        private void OnIndexChange(object sender, FileSystemEventArgs e)
        {
            if (Directory.Exists(e.FullPath))
            {
                return;
            }

            var now = DateTime.Now;
            _log($"[{TimeStamp}] index {e.ChangeType}");

            var status = _repo.RetrieveStatus();
            foreach (var x in status.Added)
            {
                var isDirty = (x.State & FileStatus.ModifiedInWorkdir) != 0;
                _log($"     {x.FilePath}{(isDirty ? "*" : "")}");
            }
            foreach (var x in status.Staged)
            {
                var isDirty = (x.State & FileStatus.ModifiedInWorkdir) != 0;
                _log($"     {x.FilePath}{(isDirty ? "*" : "")}");
            }
        }
    }
}