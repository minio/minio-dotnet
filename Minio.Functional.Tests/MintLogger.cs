/*
* MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 MinIO, Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Minio.Functional.Tests;

internal enum TestStatus
{
    PASS,
    FAIL,
    NA
}

internal static class TestStatusExtender
{
    public static string AsText(this TestStatus status)
    {
        switch (status)
        {
            case TestStatus.PASS: return "PASS";
            case TestStatus.FAIL: return "FAIL";
            default: return "NA";
        }
    }
}

internal class MintLogger
{
    public MintLogger(string testName, string function, string description, TestStatus status, TimeSpan duration,
        string alert = null, string message = null, string error = null, Dictionary<string, string> args = null)
    {
        this.function = function;
        this.duration = (int)duration.TotalMilliseconds;
        name = $"{name} : {testName}";
        this.alert = alert;
        this.message = message;
        this.error = error;
        this.args = args;
        this.status = status.AsText();
    }

    /// <summary>
    ///     SDK Name
    /// </summary>
    public string name { get; } = "minio-dotnet";

    /// <summary>
    ///     Test function name
    /// </summary>
    public string function { get; }

    /// <summary>
    ///     Test function description
    /// </summary>
    public string description { get; private set; }

    /// <summary>
    ///     Key-value pair of args relevant to test
    /// </summary>
    public Dictionary<string, string> args { get; }

    /// <summary>
    ///     duration of the whole test
    /// </summary>
    public int duration { get; }

    /// <summary>
    ///     test status : can be PASS, FAIL, NA
    /// </summary>
    public string status { get; }

    /// <summary>
    ///     alert message Information like whether this is a Blocker/ Gateway, Server etc can go here
    /// </summary>
    public string alert { get; }

    /// <summary>
    ///     descriptive error message
    /// </summary>
    public string message { get; }

    /// <summary>
    ///     actual low level exception/error thrown by the program
    /// </summary>
    public string error { get; }

    public void Log()
    {
        Console.WriteLine(JsonConvert.SerializeObject(this, Formatting.None,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
    }
}