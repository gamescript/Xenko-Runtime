// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.BuildEngine.Tests.Commands
{
    public class EchoCommand : TestCommand
    {
        public string InputUrl { get; set; }
        public string Echo { get; set; }

        public EchoCommand(string inputUrl, string echo)
        {
            InputUrl = inputUrl;
            Echo = echo;
            InputFilesGetter = GetInputFilesImpl;
        }

        private IEnumerable<ObjectUrl> GetInputFilesImpl()
        {
            yield return new ObjectUrl(UrlType.File, InputUrl);
        }

        protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
        {
            Console.WriteLine(@"{0}: {1}", InputUrl, Echo);
            return Task.FromResult(ResultStatus.Successful);
        }
    }
}
