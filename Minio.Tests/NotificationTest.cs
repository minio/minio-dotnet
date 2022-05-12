using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minio.DataModel;

namespace Minio.Tests;

/// <summary>
///     Summary description for NotificationTest
/// </summary>
[TestClass]
public class NotificationTest
{
    [TestMethod]
    public void TestNotificationStringHydration()
    {
        var notificationString =
            "<NotificationConfiguration xmlns=\"http://s3.amazonaws.com/doc/2006-03-01/\"><TopicConfiguration><Id>YjVkM2Y0YmUtNGI3NC00ZjQyLWEwNGItNDIyYWUxY2I0N2M4 </Id><Arn>arnstring</Arn><Topic> arn:aws:sns:us-east-1:account-id:s3notificationtopic2 </Topic><Event> s3:ReducedRedundancyLostObject </Event><Event> s3:ObjectCreated: *</Event></TopicConfiguration></NotificationConfiguration>";

        try
        {
            var contentBytes = Encoding.UTF8.GetBytes(notificationString);
            using (var stream = new MemoryStream(contentBytes))
            {
                var notification =
                    (BucketNotification)new XmlSerializer(typeof(BucketNotification)).Deserialize(stream);
                Assert.AreEqual(1, notification.TopicConfigs.Count);
            }
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.ToString());
        }
    }

    [TestMethod]
    public void TestQueueConfigEquality()
    {
        var config1 = new QueueConfig("somerandomarnstring");
        var config2 = new QueueConfig("somerandomarnstring");
        var config3 = new QueueConfig("blah");
        Assert.IsFalse(new QueueConfig().Equals(null));
        Assert.IsTrue(config1.Equals(config2));
        Assert.IsFalse(config1.Equals(config3));
    }

    [TestMethod]
    public void TestQueueConfigEqualsOverride()
    {
        var config1 = new QueueConfig("somerandomarnstring");
        var config2 = new QueueConfig("somerandomarnstring");
        var config3 = new QueueConfig("blah");
        var qConfigs = new List<QueueConfig> { config1, config2 };
        Assert.IsTrue(qConfigs.Exists(t => t.Equals(config1)));
        Assert.IsFalse(qConfigs.Exists(t => t.Equals(config3)));
    }

    [TestMethod]
    public void TestQueueConfigRemoveElement()
    {
        var config1 = new QueueConfig("somerandomarnstring");
        var config2 = new QueueConfig("somerandomarnstring");
        var config3 = new QueueConfig("blah");
        var qConfigs = new List<QueueConfig> { config1, config2, config3 };
        var numRemoved = qConfigs.RemoveAll(t => t.Equals(config3));
        Assert.IsTrue(numRemoved == 1);
        numRemoved = qConfigs.RemoveAll(t => t.Equals(new QueueConfig("notpresentinlist")));
        Assert.IsTrue(numRemoved == 0);
    }

    [TestMethod]
    public void TestBucketNotificationMethods()
    {
        var notification = new BucketNotification();
        // remove non-existent lambda, topic and queue configs
        notification.RemoveLambdaByArn(new Arn("blahblah"));
        notification.RemoveQueueByArn(new Arn("somequeue"));
        notification.RemoveTopicByArn(new Arn("nonexistenttopic"));
        // now test add & remove
        notification.AddLambda(new LambdaConfig("blahblah"));
        notification.RemoveLambdaByArn(new Arn("blahblah"));

        notification.AddQueue(new QueueConfig("somequeue"));
        notification.RemoveQueueByArn(new Arn("somequeue"));

        notification.AddTopic(new TopicConfig("nonexistenttopic"));
        notification.RemoveTopicByArn(new Arn("nonexistenttopic"));
    }
}