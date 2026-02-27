using System.Xml.Linq;
using Minio.Model;
using Xunit;

namespace Minio.UnitTests.Tests;

public class BucketNotificationTests
{
    [Fact]
    public void TestSerializationAndDeserialization()
    {
        var bucketNotification1 = new BucketNotification
        {
            LambdaConfigs =
            {
                new LambdaConfig
                {
                    Id = "ObjectCreatedEvents",
                    Events = { EventType.ObjectCreatedAll },
                    Filter =
                    {
                        {
                            "prefix", "images"
                        },
                        {
                            "suffix", ".jpg"
                        },
                    },
                    Lambda = "arn:aws:lambda:us-west-2:35667example:function:CreateThumbnail"
                }
            },
            TopicConfigs =
            {
                new TopicConfig
                {
                    Topic = "arn:aws:sns:us-east-1:356671443308:s3notificationtopic2",
                    Events = { EventType.ReducedRedundancyLostObject, EventType.ObjectRemovedDelete }
                }
            },
            QueueConfigs =
            {
                new QueueConfig
                {
                    Queue = "arn:aws:sqs:us-east-1:356671443308:s3notificationqueue",
                    Events = { EventType.ObjectCreatedAll }
                }
            }
        };
        var got = bucketNotification1.Serialize();
        var expected = XElement.Parse(
            """
            <NotificationConfiguration xmlns="http://s3.amazonaws.com/doc/2006-03-01/">
                <CloudFunctionConfiguration>
                    <CloudFunction>arn:aws:lambda:us-west-2:35667example:function:CreateThumbnail</CloudFunction>
                    <Id>ObjectCreatedEvents</Id>
                    <Event>s3:ObjectCreated:*</Event>
                    <Filter>
                        <S3Key>
                            <FilterRule>
                                <Name>prefix</Name>
                                <Value>images</Value>
                            </FilterRule>
                            <FilterRule>
                                <Name>suffix</Name>
                                <Value>.jpg</Value>
                            </FilterRule>
                        </S3Key>
                    </Filter>
                </CloudFunctionConfiguration>
                <TopicConfiguration>
                    <Topic>arn:aws:sns:us-east-1:356671443308:s3notificationtopic2</Topic>
                    <Event>s3:ReducedRedundancyLostObject</Event>
                    <Event>s3:ObjectRemoved:Delete</Event>
                </TopicConfiguration>
                <QueueConfiguration>
                    <Queue>arn:aws:sqs:us-east-1:356671443308:s3notificationqueue</Queue>
                    <Event>s3:ObjectCreated:*</Event>
                </QueueConfiguration>
            </NotificationConfiguration>
            """);
        Assert.Equal(expected.ToString(), got.ToString());

        var bucketNotification2 = BucketNotification.Deserialize(expected);
        Assert.Equal(bucketNotification1.ToString(), bucketNotification2.ToString());
    }
}