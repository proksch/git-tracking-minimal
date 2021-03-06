﻿/*
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
using git_tracking_minimal.GitTracking;
using NUnit.Framework;

namespace git_tracking_minimal_test.GitTracking
{
    internal class GitLogEntryTest
    {
        private const string Entry1 = "fd792 c2b90 My Name <em@i.l> 1502123884\t...";
        private const string Entry2 = "fd792 c2b90 My Name <em@i.l> 1502123884 +0200\t...";
        private const string Entry3 = "fd792 c2b90 My Name <em@i.l> 1502123884 +0123\t...";
        private const string Entry4 = "fd792 c2b90 \"My Name\" <em@i.l> 1502123884\t...";

        [Test]
        public void CommitFrom()
        {
            var actual = new GitLogEntry(Entry1).CommitFrom;
            var expected = "fd792";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void CommitTo()
        {
            var actual = new GitLogEntry(Entry1).CommitTo;
            var expected = "c2b90";
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void User1_unquoted()
        {
            var actual = new GitLogEntry(Entry1).User;
            var expected = new MailAddress("em@i.l", "My Name");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void User2_quoted()
        {
            var actual = new GitLogEntry(Entry4).User;
            var expected = new MailAddress("em@i.l", "My Name");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Time1()
        {
            var actual = new GitLogEntry(Entry1).Time;
            var zero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var expected = zero.AddSeconds(1502123884);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Time2()
        {
            var actual = new GitLogEntry(Entry2).Time;
            var zero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var expected = zero.AddSeconds(1502123884); //.AddHours(-2);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Time3()
        {
            var actual = new GitLogEntry(Entry3).Time;
            var zero = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
            var expected = zero.AddSeconds(1502123884); //.AddHours(-1).AddMinutes(-23);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Msg()
        {
            var actual = new GitLogEntry(Entry1).Msg;
            var expected = "...";
            Assert.AreEqual(expected, actual);
        }
    }
}