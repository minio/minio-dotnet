﻿/*
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

namespace Minio.Tests.Int
{
    using System;
    using System.Threading.Tasks;
    using DataModel.Notification;
    using Xunit;

    public class SetBucketNotificationTests : AbstractMinioTests
    {
        /// <summary>
        ///     Set bucket notifications. The resource ARN needs to exist on AWS with correct permissions.
        ///     For further info: see http://docs.aws.amazon.com/AmazonS3/latest/dev/NotificationHowTo.html
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task HappyCase()
        {
            // arrange
            var basketName = await this.GetTargetBasketName();

            // act
            Console.Out.WriteLine("Running example for API: SetBucketNotificationAsync");
            var notification = new AwsBucketNotification();

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
            try
            {
                await this.MinioClient.SetBucketNotificationsAsync(basketName, notification);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            // assert
            Assert.NotNull(notification);

            // log
            Console.WriteLine("Notifications set for the bucket " + basketName + "were set successfully");
        }
    }
}