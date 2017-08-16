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

namespace git_tracking_minimal.GitTracking
{
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
}