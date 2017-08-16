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
using System.Net.Mail;

namespace git_tracking_minimal.GitTracking
{
    public class GitLogEntry
    {
        public GitLogEntry(string line)
        {
            var idxEndCommitFrom = line.IndexOf(' ');
            var idxEndCommitTo = line.IndexOf(' ', idxEndCommitFrom + 1);
            var idxEndUser = line.IndexOf('>', idxEndCommitTo + 1);
            var idxEndTimeStamp = line.IndexOf(' ', idxEndUser + 2);
            var idxEndTimeZone = line.IndexOf('\t', idxEndUser + 2);
            if (idxEndTimeStamp == -1)
            {
                idxEndTimeStamp = idxEndTimeZone;
            }

            var cur = 0;
            CommitFrom = line.Substring(cur, idxEndCommitFrom);
            cur = idxEndCommitFrom + 1;
            CommitTo = line.Substring(cur, idxEndCommitTo - cur);
            cur = idxEndCommitTo + 1;
            var user = line.Substring(cur, idxEndUser + 1 - cur); // consider ">"
            cur = idxEndUser + 2;

            User = ExtractAddress(user);


            var timestampRaw = line.Substring(cur, idxEndTimeStamp - cur);
            Msg = line.Substring(idxEndTimeZone + 1);

            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Local);
            Time = dateTime.AddSeconds(int.Parse(timestampRaw));
        }

        public DateTime Time { get; }

        public string Msg { get; }

        public MailAddress User { get; }

        public string CommitTo { get; }

        public string CommitFrom { get; }

        private static MailAddress ExtractAddress(string user)
        {
            var open = user.IndexOf('<');
            var close = user.IndexOf('>');

            var name = user.Substring(0, open).Trim();
            var address = user.Substring(open + 1, close - open - 1).Trim();
            return new MailAddress(address, name);
        }
    }
}