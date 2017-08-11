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
    public interface IChangeSize
    {
        int LinesAdded { get; }
        int LinesRemoved { get; }
        int Churn { get; }
    }

    // not yet sure how this will be tracked.. maybe this is part of the reflog?
    public enum GitResetMode
    {
        Unknown,
        Soft,
        Mixed,
        Hard
    }

    public class GitState
    {
        public bool IsGitEnabled { get; set; }
        public string TrackedFolder { get; set; }
        public string GitFolder { get; set; }

        public string CurrentCommit { get; set; }
        public string CurrentBranch { get; set; }

        public Dictionary<string, IChangeSize> WorkingDir { get; set; }
        public Dictionary<string, IChangeSize> Index { get; set; }
        public Dictionary<string, string> Refs { get; set; } // path, hash (both in file system and packed-refs file)
    }

    public interface IGitActionTracker { }

    public interface IGitStateTracker
    {
        GitState State { get; }

        void Run();

        event Action<string, IChangeSize> FileChange;
        event Action ConfigChange;
        event Action GitEnabled;
        event Action HeadChange;
        event Action IndexChange;
        event Action RefsChange;

        // index

        event Action<string, bool> FileAdd; // file, working dir dirty?
        event Action<string, bool> FileReset; // file, still in index?
        event Action<string, string, GitResetMode> BranchReset; // name, toHash, 


        event Action<string, string, string, bool> Commit; // parent, hash, message, amend?

        // tagging

        event Action<string, string> TagAdded; // name, hash
        event Action<string> TagRemoved; // name

        // remotes

        event Action<string> RemoteAdded;
        event Action<string> RemoteDeleted;
        event Action<string> Fetch;
        event Action<string> Pull;
        event Action<string> Push;
        event Action<string> Merge;
        event Action<string> Rebase;

        // branching

        event Action<string, string> BranchAdded; // name, hash 
        event Action<string> BranchRemove; // name
        event Action<string, string, string> BranchRename; // nameOld, nameNew, hash
        event Action<string, string> BranchCheckout; // name, hash
    }

    public class GitTracker : IDisposable
    {
        private readonly string _dirLogs;
        private readonly string _dirRefs;
        private readonly string _dirRepo;

        private readonly string _dirSln;


        private readonly List<string> _seenHeadLines = new List<string>();
        private readonly object _sync = new object();
        private Repository _repo;
        private FileSystemWatcher _watcherHead;
        private FileSystemWatcher _watcherObj;
        private FileSystemWatcher _watcherRefs;
        private FileSystemWatcher _watcherRepo;
        private FileSystemWatcher _watcherWork;

        public GitTracker(string dirSln)
        {
            State = new GitState
            {
                TrackedFolder = dirSln
            };
            var gitDir = Path.Combine(dirSln, ".git");
            if (!Directory.Exists(gitDir))
            {
                return;
            }

            State.IsGitEnabled = true;
            State.GitFolder = gitDir;

            var fileIndex = Path.Combine(gitDir, "index");
            if (!File.Exists(fileIndex))
            {
                File.Create(fileIndex).Dispose();
            }

            var dirLogs = Path.Combine(gitDir, "logs");
            if (!Directory.Exists(dirLogs))
            {
                Directory.CreateDirectory(dirLogs);
            }
            var fileHead = Path.Combine(gitDir, "logs", "HEAD");
            if (!File.Exists(fileHead))
            {
                File.Create(fileHead).Dispose();
            }

            var dirRefs = Path.Combine(gitDir, "refs");
            if (!Directory.Exists(dirRefs))
            {
                Directory.CreateDirectory(dirRefs);
            }

            _dirLogs = dirLogs;
            _dirRefs = dirRefs;
            _dirRepo = gitDir;
            _dirSln = dirSln;
        }

        public GitState State { get; }

        public string TimeStamp => $"{DateTime.Now:HH:mm:ss.fff}";

        public void Dispose()
        {
            _watcherWork?.Dispose();
            _watcherRepo?.Dispose();
            _watcherObj?.Dispose();
            _watcherRefs?.Dispose();
            _watcherHead?.Dispose();
            _repo?.Dispose();
        }

        public event Action GitEnabled;

        private static void Log(string msg)
        {
            Console.WriteLine(msg);
        }

        public void Run()
        {
            if (State.IsGitEnabled)
            {
                Instrument();
            }
        }

        public void Instrument()
            {
                _repo = new Repository(_dirRepo);
            foreach (var tag in _repo.Tags)
            {
                // ...
            }

            ReadNewHeadLines();

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
            _watcherWork.Error += OnError;

            _watcherRepo = new FileSystemWatcher
            {
                Path = _dirRepo,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            _watcherRepo.Created += GeneralHandler;
            _watcherRepo.Changed += GeneralHandler;
            _watcherRepo.Deleted += GeneralHandler;
            _watcherRepo.Renamed += GeneralHandler;
            _watcherRepo.Error += OnError;

            _watcherHead = new FileSystemWatcher
            {
                Path = _dirLogs,
                Filter = "HEAD",
                EnableRaisingEvents = true,
                IncludeSubdirectories = false
            };
            _watcherHead.Changed += OnHeadChange;
            _watcherHead.Error += OnError;

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

        private void SlnHandler(object sender, FileSystemEventArgs args)
        {
            lock (_sync)
            {
                if (Directory.Exists(args.FullPath))
                {
                    return;
                }
                if (args.FullPath.StartsWith(_dirRepo))
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
            var pathHead = Path.Combine(_dirLogs, "HEAD");

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

        private void OnError(object sender, ErrorEventArgs e)
        {
            lock (_sync)
            {
                Log($"[{TimeStamp}] error!");
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