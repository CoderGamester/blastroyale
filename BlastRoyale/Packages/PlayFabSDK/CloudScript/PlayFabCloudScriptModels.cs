#if !DISABLE_PLAYFABENTITY_API
using System;
using System.Collections.Generic;
using PlayFab.SharedModels;

namespace PlayFab.CloudScriptModels
{

    [Serializable]
    public class ExecuteFunctionResult : PlayFabResultCommon
    {
        /// <summary>
        /// Error in the retained legacy function-result envelope.
        /// </summary>
        public FunctionExecutionError Error;
        /// <summary>
        /// The amount of time the function took to execute
        /// </summary>
        public int ExecutionTimeMilliseconds;
        /// <summary>
        /// The name of the function that executed
        /// </summary>
        public string FunctionName;
        /// <summary>
        /// The object returned from the function, if any
        /// </summary>
        public object FunctionResult;
        /// <summary>
        /// The size in bytes of the object returned from the function, if any
        /// </summary>
        public int? FunctionResultSize;
        /// <summary>
        /// Flag indicating if the FunctionResult was too large and was subsequently dropped from this event.
        /// </summary>
        public bool? FunctionResultTooLarge;
    }

    [Serializable]
    public class FunctionExecutionError : PlayFabBaseModel
    {
        /// <summary>
        /// Error code carried by the retained legacy function-result envelope.
        /// </summary>
        public string Error;
        /// <summary>
        /// Details about the error
        /// </summary>
        public string Message;
        /// <summary>
        /// Point during the execution of the function at which the error occurred, if any
        /// </summary>
        public string StackTrace;
    }
}
#endif
