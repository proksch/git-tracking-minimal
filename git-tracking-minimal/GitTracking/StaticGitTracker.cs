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

namespace git_tracking_minimal.GitTracking
{
    public class StaticGitTracker : IDisposable
    {
        private readonly string _dirGit;
        private readonly string _dirGitLogs;
        private readonly string _dirGitRefs;
        private readonly string _dirSln;

        private readonly Repository _repo;

        private readonly List<string> _seenHeadLines = new List<string>();
        private readonly object _sync = new object();
        private readonly FileSystemWatcher _watcherHead;
        private readonly FileSystemWatcher _watcherRefs;
        private readonly FileSystemWatcher _watcherRepoContent;
        private FileSystemWatcher _watcherWork;

        public string TimeStamp => $"{DateTime.Now:HH:mm:ss.fff}";


        public StaticGitTracker(GitState state)
        {
            _dirSln = state.TrackedFolder;
            _dirGit = state.GitFolder;
            _dirGitLogs = Path.Combine(_dirGit, "logs");
            _dirGitRefs = Path.Combine(_dirGit, "refs");

            CreateGitStructureIfNotExisting();

            _repo = new Repository(_dirGit);

            ReadNewHeadLines();

            /*
            _watcherWork = new FileSystemWatcher
            {
                Path = _dirSln,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcherWork.Created += SlnHandler;
            _watcherWork.Changed += SlnHandler;
            _watcherWork.Deleted += SlnHandler;
            _watcherWork.Renamed += SlnHandler;
            _watcherWork.Error += (o, ea) => { OnError($"_watcherWork -- {ea.GetException()}"); };
            */

            /*
            _watcherRepoContent = new FileSystemWatcher
            {
                Path = _dirGit,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcherRepoContent.Created += GeneralHandler;
            _watcherRepoContent.Changed += GeneralHandler;
            _watcherRepoContent.Deleted += GeneralHandler;
            _watcherRepoContent.Renamed += GeneralHandler;
            _watcherRepoContent.Error += (o, ea) => { OnError($"_watcherRepoContent -- {ea.GetException()}"); };
            */

            _watcherHead = new FileSystemWatcher
            {
                Path = _dirGitLogs,
                Filter = "HEAD",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcherHead.Changed += OnHeadChange;
            /* _watcherHead.Error += (o, ea) =>
             {
                 OnError($"_watcherHead -- {ea.GetException()}");
             };*/

            _watcherRefs = new FileSystemWatcher
            {
                Path = _dirGitRefs,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcherRefs.Changed += OnRefsChange;
            //_watcherRefs.Error += (o, ea) => { OnError($"_watcherRefs -- {ea.GetException()}"); };
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _watcherWork?.Dispose();
                _watcherRepoContent?.Dispose();
                _watcherRefs?.Dispose();
                _watcherHead?.Dispose();
                _repo?.Dispose();
            }
        }

        private void CreateGitStructureIfNotExisting()
        {
            var fileIndex = Path.Combine(_dirGit, "index");
            if (!File.Exists(fileIndex))
            {
                File.Create(fileIndex).Dispose();
            }

            if (!Directory.Exists(_dirGitLogs))
            {
                Directory.CreateDirectory(_dirGitLogs);
            }
            var fileHead = Path.Combine(_dirGitLogs, "HEAD");
            if (!File.Exists(fileHead))
            {
                File.Create(fileHead).Dispose();
            }

            if (!Directory.Exists(_dirGitRefs))
            {
                Directory.CreateDirectory(_dirGitRefs);
            }
        }

        private static void Log(string msg)
        {
            Console.WriteLine(msg);
        }


        private void SlnHandler(object sender, FileSystemEventArgs args)
        {
            lock (_sync)
            {
                if (Directory.Exists(args.FullPath))
                {
                    return;
                }
                if (args.FullPath.StartsWith(_dirGit))
                {
                    return;
                }
                var renamedArgs = args as RenamedEventArgs;
                Log(renamedArgs != null
                    ? $"[{TimeStamp}] {renamedArgs.ChangeType}: {Shorten(renamedArgs.OldFullPath)} > {Shorten(renamedArgs.FullPath)}"
                    : $"[{TimeStamp}] {args.ChangeType}: {Shorten(args.FullPath)}");


                Console.WriteLine("diff:");
                var diff1 = _repo.Diff.Compare<Patch>();
                var diff2 = _repo.Diff.Compare<TreeChanges>();
                var diff4B = _repo.Diff.Compare<Patch>(new[] {"bbbb.txt"});
                var diff5E = _repo.Diff.Compare<Patch>(new[] {"eeeee.txt"});

                var diffIdx1 = _repo.Diff.Compare<Patch>(_repo.Head.Tip.Tree, DiffTargets.Index);
                var diffIdx2 = _repo.Diff.Compare<TreeChanges>(_repo.Head.Tip.Tree, DiffTargets.Index);

                Console.WriteLine("bbbb.txt: +{0},-{1}", diff4B.LinesAdded, diff4B.LinesDeleted);

                Console.WriteLine("status:");
                foreach (var file in _repo.RetrieveStatus())
                {
                    Log($"{file.State}: {Shorten(file.FilePath)}");
                }
            }
        }

        private void GeneralHandler(object sender, FileSystemEventArgs args)
        {
            lock (_sync)
            {
                if (Directory.Exists(args.FullPath))
                {
                    return;
                }
                if (args.FullPath.Contains(Path.Combine(".git", "rebase-apply")))
                {
                    return;
                }
                if (args.FullPath.EndsWith(".lock"))
                {
                    return;
                }
                var renamedArgs = args as RenamedEventArgs;
                Log(renamedArgs != null
                    ? $"[{TimeStamp}] {renamedArgs.ChangeType}: {Shorten(renamedArgs.OldFullPath)} > {Shorten(renamedArgs.FullPath)}"
                    : $"[{TimeStamp}] {args.ChangeType}: {Shorten(args.FullPath)}");
            }
        }

        public string Shorten(string path)
        {
            var len = _dirSln.EndsWith("\\") ? _dirSln.Length : _dirSln.Length + 1;

            if (path.StartsWith(_dirSln))
            {
                return path.Substring(len);
            }
            return path;
        }

        private void OnRefsChange(object sender, FileSystemEventArgs e)
        {
            lock (_sync)
            {
                if (Directory.Exists(e.FullPath))
                {
                    return;
                }
                Log($"[{TimeStamp}] ref changed: {e.FullPath}");
            }
        }

        private void OnHeadChange(object sender, FileSystemEventArgs e)
        {
            lock (_sync)
            {
                if (Directory.Exists(e.FullPath))
                {
                    return;
                }

                Log($"[{TimeStamp}] head changed:");
                foreach (var line in ReadNewHeadLines())
                {
                    Log($"    {line.Msg}");
                }
            }
        }

        private IList<GitLogEntry> ReadNewHeadLines()
        {
            lock (_sync)
            {
                var pathHead = Path.Combine(_dirGitLogs, "HEAD");

                var newLines = new List<GitLogEntry>();
                foreach (var line in File.ReadAllLines(pathHead))
                {
                    if (!_seenHeadLines.Contains(line))
                    {
                        _seenHeadLines.Add(line);
                        var e = new GitLogEntry(line);
                        newLines.Add(e);
                    }
                }
                return newLines;
            }
        }

        private void OnError(string message)
        {
            lock (_sync)
            {
                Log($"[{TimeStamp}] error: {message}");
            }
        }

        private void OnIndexChange(object sender, FileSystemEventArgs e)
        {
            lock (_sync)
            {
                if (Directory.Exists(e.FullPath))
                {
                    return;
                }

                var now = DateTime.Now;
                Log($"[{TimeStamp}] index {e.ChangeType}");

                var status = _repo.RetrieveStatus();
                Log("  added:");
                foreach (var x in status.Added)
                {
                    var isDirty = (x.State & FileStatus.ModifiedInWorkdir) != 0;
                    Log($"     {x.FilePath}{(isDirty ? "*" : "")}");
                }
                Log("  staged:");
                foreach (var x in status.Staged)
                {
                    var isDirty = (x.State & FileStatus.ModifiedInWorkdir) != 0;
                    Log($"     {x.FilePath}{(isDirty ? "*" : "")}");
                }
            }
        }
    }
}