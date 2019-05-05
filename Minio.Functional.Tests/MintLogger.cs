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

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Minio.Functional.Tests
{
    enum TestStatus
    {
      PASS,
      FAIL,
      NA
    }

    static class TestStatusExtender
    {
        public static String AsText(this TestStatus status)
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
      // SDK Name       
      public string name {get; private set;} = "minio-dotnet";
      // Test function name      
      public string function {get; private set;}

      // Test function description
      public string description {get; private set;}

      // Key-value pair of args relevant to test
      public Dictionary<string, string> args {get; private set;}
      
      // duration of the whole test
      public int duration {get; private set;}
      
      // test status : can be PASS, FAIL, NA
      public string status {get; private set;}
      
      // alert message Information like whether this is a Blocker/ Gateway, Server etc can go here
      public string alert {get; private set;}
      // descriptive error message
      public string message {get; private set;}

      // actual low level exception/error thrown by the program
      public string error {get; private set;}
      public MintLogger(string testName, string function, string description, TestStatus status,System.TimeSpan duration, string alert = null,string message=null, string error=null, Dictionary<string,string> args=null)
      {
        this.function = function;
        this.duration = (int)duration.TotalMilliseconds;
        this.name = this.name + ": " + testName;
        this.alert = alert;
        this.message = message;
        this.error = error;
        this.args = args;
        this.status = status.AsText();
      }  
      public void Log()
      {
            Console.Out.WriteLine(JsonConvert.SerializeObject(this, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }
    }
}