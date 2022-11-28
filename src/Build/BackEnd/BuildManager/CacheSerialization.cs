﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.BackEnd;
using Microsoft.Build.Shared;

#nullable disable

namespace Microsoft.Build.Execution
{
    internal static class CacheSerialization
    {
        public static string SerializeCaches(IConfigCache configCache, IResultsCache resultsCache, string outputCacheFile, IsolateProjects isolateProjects)
        {
            ErrorUtilities.VerifyThrowInternalNull(outputCacheFile, nameof(outputCacheFile));

            try
            {
                if (string.IsNullOrWhiteSpace(outputCacheFile))
                {
                    return ResourceUtilities.FormatResourceStringIgnoreCodeAndKeyword("EmptyOutputCacheFile");
                }

                var fullPath = FileUtilities.NormalizePath(outputCacheFile);

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                // Use FileStream constructor (File.OpenWrite should not be used as it doesn't reset the length of the file!)
                using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    var translator = BinaryTranslator.GetWriteTranslator(fileStream);

                    ConfigCache configCacheToSerialize = null;
                    ResultsCache resultsCacheToSerialize = null;

                    switch (configCache)
                    {
                        case ConfigCache asConfigCache:
                            configCacheToSerialize = asConfigCache;
                            break;
                        case ConfigCacheWithOverride configCacheWithOverride:
                            configCacheToSerialize = configCacheWithOverride.CurrentCache;
                            break;
                        default:
                            ErrorUtilities.ThrowInternalErrorUnreachable();
                            break;
                    }

                    switch (resultsCache)
                    {
                        case ResultsCache asResultsCache:
                            resultsCacheToSerialize = asResultsCache;
                            break;
                        case ResultsCacheWithOverride resultsCacheWithOverride:
                            resultsCacheToSerialize = resultsCacheWithOverride.CurrentCache;
                            break;
                        default:
                            ErrorUtilities.ThrowInternalErrorUnreachable();
                            break;
                    }

                    // Avoid creating new config and results caches if no projects were built in violation
                    // of isolation mode.
                    if (configCacheToSerialize.Count() > 1)
                    {
                        // We need to preserve all configurations to enable the scheduler to dump them and their
                        // associated requests, so create new caches to serialize storing solely data
                        // associated with the project specified to be built in isolation (and not any
                        // data associated with referenced projects needed for said project to complete
                        // its build).
                        var tempConfigCacheToSerialize = new ConfigCache();

                        // The project that was built in isolation mode has the
                        // smallest configuration id.
                        int smallestCacheConfigId = configCacheToSerialize.GetSmallestConfigId();
                        tempConfigCacheToSerialize.AddConfiguration(configCacheToSerialize[smallestCacheConfigId]);
                        configCacheToSerialize = tempConfigCacheToSerialize;
                        var tempResultsCacheToSerialize = new ResultsCache();
                        tempResultsCacheToSerialize.AddResult(resultsCacheToSerialize.GetResultsForConfiguration(smallestCacheConfigId));
                        resultsCacheToSerialize = tempResultsCacheToSerialize;
                    }

                    translator.Translate(ref configCacheToSerialize);
                    translator.Translate(ref resultsCacheToSerialize);
                }
            }
            catch (Exception e)
            {
                return ResourceUtilities.FormatResourceStringIgnoreCodeAndKeyword("ErrorWritingCacheFile", outputCacheFile, e.Message);
            }

            return null;
        }

        public static (IConfigCache ConfigCache, IResultsCache ResultsCache, Exception exception) DeserializeCaches(string inputCacheFile)
        {
            try
            {
                ConfigCache configCache = null;
                ResultsCache resultsCache = null;

                using (var fileStream = File.OpenRead(inputCacheFile))
                {
                    using var translator = BinaryTranslator.GetReadTranslator(fileStream, null);

                    translator.Translate(ref configCache);
                    translator.Translate(ref resultsCache);
                }

                ErrorUtilities.VerifyThrowInternalNull(configCache, nameof(configCache));
                ErrorUtilities.VerifyThrowInternalNull(resultsCache, nameof(resultsCache));

                return (configCache, resultsCache, null);
            }
            catch (Exception e)
            {
                return (null, null, e);
            }
        }
    }
}
