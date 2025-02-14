// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.BuildEngine.Shared;

namespace Microsoft.Build.BuildEngine
{
    /// <summary>
    /// This class is used to store information needed to interpret the response from
    /// the parent engine when it completes the requested evaluation
    /// </summary>
    internal class NodeRequestMapping
    {
        #region Constructors

        internal NodeRequestMapping
            (int handleId, int requestId, CacheScope resultsCache)
        {
            ErrorUtilities.VerifyThrow(resultsCache != null, "Expect a non-null build result");
            this.handleId = handleId;
            this.requestId = requestId;
            this.resultsCache = resultsCache;
        }
        #endregion

        #region Properties
        internal int HandleId
        {
            get
            {
                return this.handleId;
            }
        }

        internal int RequestId
        {
            get
            {
                return this.requestId;
            }
        }
        #endregion

        #region Methods
        internal void AddResultToCache(BuildResult buildResult)
        {
            resultsCache.AddCacheEntryForBuildResults(buildResult);
        }
        #endregion

        #region Member Data
        private int handleId;
        private int requestId;
        private CacheScope resultsCache;
        #endregion
    }
}
