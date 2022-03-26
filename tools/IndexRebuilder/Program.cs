﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Configuration;
using System.Diagnostics;
using Microsoft.Health.Fhir.Store.SqlUtils;

namespace Microsoft.Health.Fhir.IndexRebuilder
{
    public static class Program
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["Database"].ConnectionString;
        private static readonly int Threads = int.Parse(ConfigurationManager.AppSettings["Threads"]);
        private static readonly bool RebuildClustered = bool.Parse(ConfigurationManager.AppSettings["RebuildClustered"]);
        private static readonly string EventLogQuery = ConfigurationManager.AppSettings["EventLogQuery"];
        private static readonly SqlService Store = new SqlService(ConnectionString);

        public static void Main()
        {
            Console.WriteLine($"IndexRebuilder.Start: Store={Store.ShowConnectionString()} Threads={Threads} at {DateTime.UtcNow:s}");
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"To monitor progress please run in the target database: {EventLogQuery}");

            var indexRebuilder = new IndexRebuilder(ConnectionString, Threads, RebuildClustered);
            indexRebuilder.Run(out var cancel, out var tables);
            Console.WriteLine($"IndexRebuilder.{(cancel.IsSet ? "FAILED" : "End")}: Store={Store.ShowConnectionString()} Threads={Threads} Tables={tables} at {DateTime.Now:s} elapsed={sw.Elapsed.TotalSeconds:N0} sec.");
        }
    }
}