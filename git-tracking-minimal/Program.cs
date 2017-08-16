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
using git_tracking_minimal.GitTracking;

namespace git_tracking_minimal
{
    internal class Program
    {
        private const string Repo1 = @"c:\examples\Repo1\";
        private const string NoRepo = @"c:\examples\NoRepo\";
        private static readonly object Lock = new object();

        private static void Main(string[] args)
        {
            Case1(@"C:\examples\foo1");
            Case2(@"C:\examples\foo2");
            Case3(@"C:\examples\foo3");
            Case4(@"C:\examples\foo4");
        }

        private static void Case1(string dir)
        {
            Console.WriteLine("Case 1");
            Directory.CreateDirectory(dir);
            Directory.Delete(dir, true);
            Console.ReadKey();
        }

        private static void Case2(string dir)
        {
            Console.WriteLine("Case 2");
            Directory.CreateDirectory(dir);
            var w = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(dir),
                Filter = "*",
                EnableRaisingEvents = true
            };
            w.Deleted += (o, a) =>
            {
                lock (Lock)
                {
                    Console.WriteLine($"{a.ChangeType}: {a.FullPath}");
                }
            };
            Directory.Delete(dir, true);
            Console.ReadKey();
            w.Dispose();
        }

        private static void Case3(string dir)
        {
            Console.WriteLine("Case 3");
            Directory.CreateDirectory(dir);
            var w = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(dir),
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            w.Deleted += (o, a) =>
            {
                lock (Lock)
                {
                    Console.WriteLine($"{a.ChangeType}: {a.FullPath}");
                }
            };
            Directory.Delete(dir, true);
            Console.ReadKey();
            w.Dispose();
        }

        private static void Case4(string dir)
        {
            Console.WriteLine("Case 4");
            var subdir = Path.Combine(dir, "sub");
            Directory.CreateDirectory(subdir);
            var w = new FileSystemWatcher
            {
                Path = dir,
                Filter = "*",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };
            w.Deleted += (o, a) =>
            {
                lock (Lock)
                {
                    Console.WriteLine($"{a.ChangeType}: {a.FullPath}");
                }
            };
            Directory.Delete(dir, true);
            Console.ReadKey();
            w.Dispose();
        }

        private static void Main2(string[] args)
        {
            var tracker = new GitTracker(NoRepo);
            tracker.StartTracking();

            var tracker2 = new GitTracker(Repo1);
            tracker2.StartTracking();

            while (!Console.ReadLine().Equals("quit")) { }
            tracker.Dispose();
            tracker2.Dispose();
        }
    }
}