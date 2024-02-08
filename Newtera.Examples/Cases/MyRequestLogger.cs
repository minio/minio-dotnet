﻿/*
 * Newtera .NET Library for Newtera TDM, (C) 2017 Newtera, Inc.
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

using System.Text;
using Newtera.DataModel.Tracing;
using Newtera.Handlers;

namespace Newtera.Examples.Cases;

internal sealed class MyRequestLogger : IRequestLogger
{
    public void LogRequest(RequestToLog requestToLog, ResponseToLog responseToLog, double durationMs)
    {
        var sb = new StringBuilder();

        _ = sb.AppendLine("My logger says:");
        _ = sb.Append("statusCode: ");
        _ = sb.AppendLine(responseToLog.StatusCode.ToString());
        _ = sb.AppendLine();

        _ = sb.AppendLine("Response: ");
        _ = sb.Append(responseToLog.Content);

        Console.WriteLine(sb.ToString());
    }
}
