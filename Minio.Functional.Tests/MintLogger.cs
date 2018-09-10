/*
* Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2017 Minio, Inc.
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
using System.Text;

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
      public Dictionary<string,string> args {get; private set;}
      
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
      public MintLogger(string testName,string function, string description,TestStatus status,System.TimeSpan duration, string alert = null,string message=null, string error=null, Dictionary<string,string> args=null)
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
            var sb = new StringBuilder();

            sb.Append("function="); sb.AppendLine(this.function);
            sb.Append("duration="); sb.AppendLine(this.duration.ToString() );
            sb.Append("name=");     sb.AppendLine(this.name     );
            sb.Append("alert=");    sb.AppendLine(this.alert    );
            sb.Append("message=");  sb.AppendLine(this.message  );
            sb.Append("error=");    sb.AppendLine(this.error    );
            sb.Append("status=");   sb.AppendLine(this.status   );
            sb.Append("args=");

            if (this.args == null)
            {
                sb.AppendLine("[]");
            }
            else
            {
                foreach (var kv in this.args)
                {
                    sb.Append("[");
                    sb.Append(kv.Key);
                    sb.Append("]=");

                    sb.AppendLine(kv.Value);
                }
            }


            Console.Out.WriteLine(sb.ToString());
      }
    }
}