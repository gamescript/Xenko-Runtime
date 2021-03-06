// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using SiliconStudio.Core.Storage;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.BuildEngine
{
    public interface ICommandContext
    {
        Command CurrentCommand { get; }
        LoggerResult Logger { get; }

        IEnumerable<IDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups();

        void RegisterInputDependency(ObjectUrl url);
        void RegisterOutput(ObjectUrl url, ObjectId hash);
        void RegisterCommandLog(IEnumerable<ILogMessage> logMessages);

        void AddTag(ObjectUrl url, TagSymbol tagSymbol);
    }
}
