# Test Coverage Report - minio-dotnet

**Generated:** 2026-03-13

## Coverage Summary

- **Total API Methods (IBucketOperations + IObjectOperations):** 65
- **With Unit Tests:** 1  
- **With Functional Tests:** 23
- **Without Tests:** 41

---

## API Coverage by Category

### Bucket Operations (IBucketOperations)

| Method Name | Unit Tests | Functional Tests | Notes |
|-------------|-----------|------------------|-------|
| MakeBucketAsync | 0 | 5 | Core functionality tested |
| ListBucketsAsync | 0 | 1 | Basic functionality tested |
| BucketExistsAsync | 0 | 1 | Core functionality tested |
| RemoveBucketAsync | 0 | 2 | Core functionality tested |
| ListObjectsAsync | 0 | 8 | All listing scenarios |
| GetBucketNotificationsAsync | 0 | 0 | Missing |
| SetBucketNotificationsAsync | 0 | 0 | Missing |
| RemoveAllBucketNotificationsAsync | 0 | 0 | Missing |
| ListenBucketNotificationsAsync (3 overloads) | 0 | 3 | Multiple scenarios |
| GetBucketTagsAsync | 0 | 1 | GET operation tested |
| SetBucketTagsAsync | 0 | 1 | SET operation tested |
| RemoveBucketTagsAsync | 0 | 1 | REMOVE operation tested |
| SetObjectLockConfigurationAsync | 0 | 1 | Core functionality tested |
| GetObjectLockConfigurationAsync | 0 | 1 | Core functionality tested |
| RemoveObjectLockConfigurationAsync | 0 | 1 | REMOVE operation tested |
| GetVersioningAsync | 0 | 1 | Core functionality tested |
| SetVersioningAsync | 0 | 1 | Core functionality tested |
| SetBucketEncryptionAsync |  0 | 1 | Core functionality tested |
| GetBucketEncryptionAsync | 0 | 1 | Core functionality tested |
| RemoveBucketEncryptionAsync | 0 | 1 | REMOVE operation tested |
| SetBucketLifecycleAsync | 0 | 2 | Multiple configuration scenarios |
| GetBucketLifecycleAsync | 0 | 2 | Core functionality tested |
| RemoveBucketLifecycleAsync | 0 | 2 | REMOVE operation tested |
| GetBucketReplicationAsync | 0 | 0 | Missing |
| SetBucketReplicationAsync | 0 | 0 | Missing |
| RemoveBucketReplicationAsync | 0 | 0 | Missing |
| GetPolicyAsync | 0 | 1 | Core functionality tested |
| SetPolicyAsync | 0 | 2 | Core functionality tested |
| RemovePolicyAsync | 0 | 1 | REMOVE operation tested |

**Bucket Operations Summary:** 27 methods
- With Unit Tests: 0
- With Functional Tests: 20
- Without Tests: 7

---

### Object Operations (IObjectOperations)

| Method Name | Unit Tests | Functional Tests | Notes |
|-------------|-----------|------------------|-------|
| GetObjectLegalHoldAsync | 0 | 1 | Core functionality tested |
| SetObjectLegalHoldAsync | 0 | 1 | Core functionality tested |
| SetObjectRetentionAsync | 0 | 1 | Core functionality tested |
| GetObjectRetentionAsync | 0 | 1 | Core functionality tested |
| ClearObjectRetentionAsync | 0 | 1 | CLEAR operation tested |
| RemoveObjectAsync | 0 | 1 | Basic delete tested |
| RemoveObjectsAsync | 0 | 3 | Multi-object delete scenarios |
| CopyObjectAsync (9 overloads) | 0 | 9 | Comprehensive coverage |
| GetObjectAsync | 0 | 2 | Standard and encrypted gets |
| PutObjectAsync (10 overloads) | 0 | 10 | Extensive coverage including multipart |
| SelectObjectContentAsync | 0 | 1 | SQL select functionality tested |
| ListIncompleteUploadsAsync | 0 | 3 | Prefix/recursive scenarios |
| RemoveIncompleteUploadAsync | 0 | 1 | Core functionality tested |
| PresignedGetObjectAsync | 1 | 3 | With and without headers |
| PresignedPostPolicyAsync (2 overloads) | 0 | 1 | Policy-based upload tested |
| PresignedPutObjectAsync | 0 | 2 | URL upload tested |
| StatObjectAsync | 0 | 5 | Comprehensive metadata |
| GetObjectTagsAsync | 0 | 1 | GET operation tested |
| SetObjectTagsAsync | 0 | 1 | SET operation tested |
| RemoveObjectTagsAsync | 0 | 1 | REMOVE operation tested |

**Object Operations Summary:** 38 methods
- With Unit Tests: 1
- With Functional Tests: 23
- Without Tests: 14

---

## Functional Test Operations

Tested in `FunctionalTest.cs` (6316 lines, ~40 test methods):

### Core Bucket Operations
1. MakeBucket with various configurations
2. ListBuckets - list all buckets  
3. BucketExists - check bucket existence
4. RemoveBucket - delete buckets
5. ListObjects - list objects with prefix/recursive
6. ListObjects with versions (versioning enabled)
7. ListenBucketNotifications - listen for events (3 variations)
8. Get/Set/Remove Bucket Tags
9. Get/Set/Remove Object Lock Configuration
10. Get/Set/Remove Versioning
11. Get/Set/Remove Bucket Encryption
12. Get/Set/Remove Bucket Lifecycle
13. Get/Set/Remove Bucket Policy

### Core Object Operations
14. PutObject - small and large (multipart) uploads with progress
15. GetObject - standard and encrypted downloads
16. FGetObject - download to file
17. StatObject - get object metadata
18. RemoveObject - single object delete
19. RemoveObjects - multi-object delete
20. CopyObject - 9 test variations (ETag conditions, byte range, metadata replacement, encrypted copy)
21. SelectObjectContent - SQL queries on objects
22. ListIncompleteUploads - list multipart uploads
23. RemoveIncompleteUpload - cancel multipart uploads
24. PresignedGetObject - 3 variations with headers
25. PresignedPutObject - URL-based uploads
26. PresignedPostPolicy - policy-based upload
27. Legal Hold status management
28. Object Retention management
29. Object Tags management

### Additional Functional Tests
30. Encrypted object operations (SSE-C, SSE-S3)
31. s3Zip file extraction
32. Object versioning with multiple versions
33. Large object uploads (64MB)

---

## Missing Coverage

### Functional Test Missing (7 methods)
1. GetBucketNotificationsAsync
2. SetBucketNotificationsAsync
3. RemoveAllBucketNotificationsAsync
4. GetBucketReplicationAsync
5. SetBucketReplicationAsync  
6. RemoveBucketReplicationAsync

### Unit Test Missing (52 methods)
- All 27 IBucketOperations methods
- All 38 IObjectOperations methods (except PresignedGetObjectAsync)
- 4 additional IMinioClient methods

---

## Test Files Summary

### Unit Test Files (Minio.Tests/) - 81 tests total
1. OperationsTest.cs - 2 tests (PresignedGetObject)
2. NegativeTest.cs - 3 tests
3. UnitTest1.cs - 11 tests (all ignored)
4. UnitTest2.cs - 14 tests
5. UtilsTest.cs - 16 tests
6. EndpointTest.cs - 5 tests
7. UrlTests.cs - 10 tests
8. AuthenticatorTest.cs - 7 tests
9. RegionTest.cs - 1 test
10. NotificationTest.cs - 4 tests
11. DateTimeTests.cs - 7 tests
12. ReuseTcpConnectionTest.cs - 1 test
13. RetryHandlerTest.cs - 2 tests

### Functional Test Files
- FunctionalTest.cs - 6316 lines, ~40 test methods

---

## Recommendations

### High Priority - Functional Tests
1. Bucket Notifications (3 methods)
2. Bucket Replication (3 methods)

### High Priority - Unit Tests  
1. All Bucket Operations (27)
2. All Object Operations (38)
3. CopyObjectAsync (9 overloads)

---

**Note:** Functional tests require a running MinIO server with credentials via environment variables.
