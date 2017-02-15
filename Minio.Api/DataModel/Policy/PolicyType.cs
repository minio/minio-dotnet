﻿/*
 * Minio .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2015 Minio, Inc.
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.DataModel
{
  
    public class  PolicyType
    {
        private PolicyType(string value) { Value = value; }
        public string Value { get; set; }

        public static PolicyType NONE {  get { return new PolicyType("none"); } }
        public static PolicyType READ_ONLY { get { return new PolicyType("readonly"); } }
        public static PolicyType READ_WRITE { get { return new PolicyType("readwrite"); } }
        public static PolicyType WRITE_ONLY { get { return new PolicyType("writeonly"); } }

    }

}
