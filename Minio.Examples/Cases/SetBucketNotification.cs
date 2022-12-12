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
using System.Threading.Tasks;
using Minio.DataModel;

namespace Minio.Examples.Cases;

internal class SetBucketNotification
{
    // Set bucket notifications. The resource ARN needs to exist on AWS with correct permissions.
    // For further info: see http://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html
    public static async Task Run(IMinioClient minio,
        string bucketName = "my-bucket-name")
    {
        try
        {
            Console.WriteLine("Running example for API: SetBucketNotificationAsync");
            var notification = new BucketNotification();
            var args = new SetBucketNotificationsArgs()
                .WithBucket(bucketName)
                .WithBucketNotificationConfiguration(notification);

            // Uncomment the code below and change Arn and event types to configure.
            /* 
            Arn topicArn = new Arn("aws", "sns", "us-west-1", "730234153608", "topicminio");
            TopicConfig topicConfiguration = new TopicConfig(topicArn);
            List<EventType> events = new List<EventType>(){ EventType.ObjectCreatedPut , EventType.ObjectCreatedCopy };
            topicConfiguration.AddEvents(events);
            topicConfiguration.AddFilterPrefix("images");
            topicConfiguration.AddFilterSuffix("pg");
            notification.AddTopic(topicConfiguration);

            LambdaConfig lambdaConfiguration = new LambdaConfig("arn:aws:lambda:us-west-1:123434153608:function:lambdak1");
            lambdaConfiguration.AddEvents(new List<EventType>() { EventType.ObjectRemovedDelete });
            lambdaConfiguration.AddFilterPrefix("java");
            lambdaConfiguration.AddFilterSuffix("java");
            notification.AddLambda(lambdaConfiguration);

            QueueConfig queueConfiguration = new QueueConfig("arn:aws:sqs:us-west-1:123434153608:testminioqueue1");
            queueConfiguration.AddEvents(new List<EventType>() { EventType.ObjectCreatedCompleteMultipartUpload });
            notification.AddQueue(queueConfiguration);
            */
            await minio.SetBucketNotificationsAsync(args);

            Console.WriteLine("Notifications set for the bucket {bucketName} were set successfully");
            Console.WriteLine();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[Bucket]  Exception: {e}");
        }
    }
}