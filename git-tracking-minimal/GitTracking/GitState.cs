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

using System.Collections.Generic;

namespace git_tracking_minimal.GitTracking
{
    public class GitState
    {
        public bool IsGitEnabled => GitFolder != null;
        public string TrackedFolder { get; set; }
        public string GitFolder { get; set; }

        public string CurrentCommit { get; set; }
        public string CurrentBranch { get; set; }

        public Dictionary<string, IChangeSize> WorkingDir { get; set; }
        public Dictionary<string, IChangeSize> Index { get; set; }
        public Dictionary<string, string> Refs { get; set; } // path, hash (both in file system and packed-refs file)
    }
}