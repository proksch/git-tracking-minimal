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

namespace git_tracking_minimal
{
    internal class Program
    {
        private const string Repo1 = @"c:\examples\Repo1\";
        private const string NoRepo = @"c:\examples\NoRepo\";

        private static void Main(string[] args)
        {
            var tracker = new Tracker(NoRepo, Log);
            tracker.Run();

            var tracker2 = new Tracker(Repo1, Log);
            tracker2.Run();

            Console.ReadLine();
            Console.WriteLine("Cleaning up...");
            tracker.Dispose();
            tracker2.Dispose();
        }

        private static void Log(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}