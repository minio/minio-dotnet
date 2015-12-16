/*
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

namespace Minio
{
    public class Acl
    {
        private String value;

        public static Acl Private = new Acl("private");
        public static Acl PublicRead = new Acl("public-read");
        public static Acl PublicReadWrite = new Acl("public-read-write");
        public static Acl AuthenticatedRead = new Acl("authenticated-read");

        public Acl(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new Errors.InvalidAclNameException("", "Acl is empty.");
            }
            if (value.Equals("private") &&
                value.Equals("public-read") &&
                value.Equals("public-read-write") &&
                value.Equals("authenticated-read"))
            {
                this.value = value;
            }
            throw new Errors.InvalidAclNameException(value, "Invalid acl value.");
        }

        public override string ToString()
        {
            return this.value;
        }
    }
}
