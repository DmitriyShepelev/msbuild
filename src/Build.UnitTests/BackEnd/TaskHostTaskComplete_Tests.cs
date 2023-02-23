// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Build.BackEnd;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;
using Xunit;
using TaskHostPacketHelpers = Microsoft.Build.UnitTests.BackEnd.TaskHostConfiguration_Tests.TaskHostPacketHelpers;

#nullable disable

namespace Microsoft.Build.UnitTests.BackEnd
{
    /// <summary>
    /// Unit Tests for TaskHostTaskComplete packet.
    /// </summary>
    public class TaskHostTaskComplete_Tests
    {
        /// <summary>
        /// Tests various valid ways to construct this packet.  
        /// </summary>
        [Fact]
        public void TestConstructors()
        {
            var fileAccessData = new FileAccessData(
                    ReportedFileOperation.CreateFile,
                    RequestedAccess.Read,
                    0,
                    0,
                    DesiredAccess.GENERIC_READ,
                    FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL,
                    "foo",
                    null,
                    true);
            _ = new TaskHostTaskComplete(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success), fileAccessData, null);
            _ = new TaskHostTaskComplete(new OutOfProcTaskHostTaskResult(TaskCompleteType.Failure), fileAccessData, null);
            _ = new TaskHostTaskComplete(new OutOfProcTaskHostTaskResult(TaskCompleteType.CrashedDuringInitialization, new ArgumentOutOfRangeException()), fileAccessData, null);
            _ = new TaskHostTaskComplete(new OutOfProcTaskHostTaskResult(TaskCompleteType.CrashedDuringExecution, new ArgumentNullException()), fileAccessData, null);

            IDictionary<string, object> parameters = new Dictionary<string, object>();
            _ = new TaskHostTaskComplete(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success, parameters), default, null);

            IDictionary<string, object> parameters2 = new Dictionary<string, object>();
            parameters2.Add("Text", "Hello!");
            parameters2.Add("MyBoolValue", true);
            parameters2.Add("MyITaskItem", new TaskItem("ABC"));
            parameters2.Add("ItemArray", new ITaskItem[] { new TaskItem("DEF"), new TaskItem("GHI"), new TaskItem("JKL") });
            _ = new
            TaskHostTaskComplete(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success, parameters2), default, null);
        }

        /// <summary>
        /// Test invalid constructor permutations. 
        /// </summary>
        [Fact]
        public void TestInvalidConstructors()
        {
            AssertInvalidConstructorThrows(typeof(InternalErrorException), TaskCompleteType.CrashedDuringExecution, null, "ExceptionlessErrorMessage", null, null, null);
            AssertInvalidConstructorThrows(typeof(InternalErrorException), TaskCompleteType.CrashedDuringInitialization, null, null, null, null, null);
            AssertInvalidConstructorThrows(typeof(InternalErrorException), TaskCompleteType.Success, new ArgumentNullException(), "ExceptionlessErrorMessage", null, null, null);
            AssertInvalidConstructorThrows(typeof(InternalErrorException), TaskCompleteType.CrashedDuringExecution, null, null, new string[1] { "Foo" }, null, null);
        }

        /// <summary>
        /// Test serialization / deserialization when the parameter dictionary is null
        /// </summary>
        [Fact]
        public void TestTranslationWithNullDictionary()
        {
            TaskHostTaskComplete complete = new(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success), default, null);

            ((ITranslatable)complete).Translate(TranslationHelpers.GetWriteTranslator());
            INodePacket packet = TaskHostTaskComplete.FactoryForDeserialization(TranslationHelpers.GetReadTranslator());

            TaskHostTaskComplete deserializedComplete = packet as TaskHostTaskComplete;

            Assert.Equal(complete.TaskResult, deserializedComplete.TaskResult);
            Assert.NotNull(deserializedComplete.TaskOutputParameters);
            Assert.Empty(deserializedComplete.TaskOutputParameters);
        }

        /// <summary>
        /// Test serialization / deserialization when the parameter dictionary is empty
        /// </summary>
        [Fact]
        public void TestTranslationWithEmptyDictionary()
        {
            TaskHostTaskComplete complete = new(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success, new Dictionary<string, object>()), default, null);

            ((ITranslatable)complete).Translate(TranslationHelpers.GetWriteTranslator());
            INodePacket packet = TaskHostTaskComplete.FactoryForDeserialization(TranslationHelpers.GetReadTranslator());

            TaskHostTaskComplete deserializedComplete = packet as TaskHostTaskComplete;

            Assert.Equal(complete.TaskResult, deserializedComplete.TaskResult);
            Assert.NotNull(deserializedComplete.TaskOutputParameters);
            Assert.Equal(complete.TaskOutputParameters.Count, deserializedComplete.TaskOutputParameters.Count);
        }

        /// <summary>
        /// Test serialization / deserialization when the parameter dictionary contains only value types
        /// </summary>
        [Fact]
        public void TestTranslationWithValueTypesInDictionary()
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("Text", "Foo");
            parameters.Add("BoolValue", false);
            TaskHostTaskComplete complete = new(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success, parameters), default, null);

            ((ITranslatable)complete).Translate(TranslationHelpers.GetWriteTranslator());
            INodePacket packet = TaskHostTaskComplete.FactoryForDeserialization(TranslationHelpers.GetReadTranslator());

            TaskHostTaskComplete deserializedComplete = packet as TaskHostTaskComplete;

            Assert.Equal(complete.TaskResult, deserializedComplete.TaskResult);
            Assert.NotNull(deserializedComplete.TaskOutputParameters);
            Assert.Equal(complete.TaskOutputParameters.Count, deserializedComplete.TaskOutputParameters.Count);
            Assert.Equal(complete.TaskOutputParameters["Text"].WrappedParameter, deserializedComplete.TaskOutputParameters["Text"].WrappedParameter);
            Assert.Equal(complete.TaskOutputParameters["BoolValue"].WrappedParameter, deserializedComplete.TaskOutputParameters["BoolValue"].WrappedParameter);
        }

        /// <summary>
        /// Test serialization / deserialization when the parameter dictionary contains an ITaskItem. 
        /// </summary>
        [Fact]
        public void TestTranslationWithITaskItemInDictionary()
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("TaskItemValue", new TaskItem("Foo"));
            TaskHostTaskComplete complete = new(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success, parameters), default, null);

            ((ITranslatable)complete).Translate(TranslationHelpers.GetWriteTranslator());
            INodePacket packet = TaskHostTaskComplete.FactoryForDeserialization(TranslationHelpers.GetReadTranslator());

            TaskHostTaskComplete deserializedComplete = packet as TaskHostTaskComplete;

            Assert.Equal(complete.TaskResult, deserializedComplete.TaskResult);
            Assert.NotNull(deserializedComplete.TaskOutputParameters);
            Assert.Equal(complete.TaskOutputParameters.Count, deserializedComplete.TaskOutputParameters.Count);
            TaskHostPacketHelpers.AreEqual((ITaskItem)complete.TaskOutputParameters["TaskItemValue"].WrappedParameter, (ITaskItem)deserializedComplete.TaskOutputParameters["TaskItemValue"].WrappedParameter);
        }

        /// <summary>
        /// Test serialization / deserialization when the parameter dictionary contains an ITaskItem array. 
        /// </summary>
        [Fact]
        public void TestTranslationWithITaskItemArrayInDictionary()
        {
            IDictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("TaskItemArrayValue", new ITaskItem[] { new TaskItem("Foo"), new TaskItem("Baz") });
            TaskHostTaskComplete complete = new(new OutOfProcTaskHostTaskResult(TaskCompleteType.Success, parameters), default, null);

            ((ITranslatable)complete).Translate(TranslationHelpers.GetWriteTranslator());
            INodePacket packet = TaskHostTaskComplete.FactoryForDeserialization(TranslationHelpers.GetReadTranslator());

            TaskHostTaskComplete deserializedComplete = packet as TaskHostTaskComplete;

            Assert.Equal(complete.TaskResult, deserializedComplete.TaskResult);
            Assert.NotNull(deserializedComplete.TaskOutputParameters);
            Assert.Equal(complete.TaskOutputParameters.Count, deserializedComplete.TaskOutputParameters.Count);

            ITaskItem[] itemArray = (ITaskItem[])complete.TaskOutputParameters["TaskItemArrayValue"].WrappedParameter;
            ITaskItem[] deserializedItemArray = (ITaskItem[])deserializedComplete.TaskOutputParameters["TaskItemArrayValue"].WrappedParameter;

            TaskHostPacketHelpers.AreEqual(itemArray, deserializedItemArray);
        }

        /// <summary>
        /// Helper method for testing invalid constructors
        /// </summary>
        private void AssertInvalidConstructorThrows(Type expectedExceptionType, TaskCompleteType taskResult, Exception taskException, string taskExceptionMessage, string[] taskExceptionMessageArgs, IDictionary<string, object> taskOutputParameters, IDictionary<string, string> buildProcessEnvironment)
        {
            bool exceptionCaught = false;

            try
            {
                TaskHostTaskComplete complete = new(new OutOfProcTaskHostTaskResult(taskResult, taskOutputParameters, taskException, taskExceptionMessage, taskExceptionMessageArgs), default, buildProcessEnvironment);
            }
            catch (Exception e)
            {
                exceptionCaught = true;
                Assert.IsAssignableFrom(expectedExceptionType, e); // "Wrong exception was thrown!"
            }

            Assert.True(exceptionCaught); // "No exception was caught when one was expected!"
        }
    }
}
