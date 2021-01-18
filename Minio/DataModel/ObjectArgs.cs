/*
 * MinIO .NET Library for Amazon S3 Compatible Cloud Storage, (C) 2020 MinIO, Inc.
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


namespace Minio
{
    public abstract class ObjectArgs<T> : BucketArgs<T>
                            where T : ObjectArgs<T>
    {
        internal string ObjectName { get; set; }
        internal object RequestBody { get; set; }

        public T WithObject(string obj)
        {
            this.ObjectName = obj;
            return (T)this;
        }

        public T WithRequestBody(object data)
        {
            this.RequestBody = data;
            return (T)this;
        }

        public override void Validate()
        {
            base.Validate();
            utils.ValidateObjectName(this.ObjectName);
        }
    }
}