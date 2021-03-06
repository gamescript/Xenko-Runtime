// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Analysis;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Xenko.Assets.Effect;
using SiliconStudio.Xenko.Assets.Textures;
using SiliconStudio.Xenko.Graphics;

namespace SiliconStudio.Xenko.Assets.Skyboxes
{
    [AssetCompiler(typeof(SkyboxAsset), typeof(AssetCompilationContext))]
    internal class SkyboxAssetCompiler : AssetCompilerBase
    {
        public override IEnumerable<BuildDependencyInfo> GetInputTypes(AssetItem assetItem)
        {
            yield return new BuildDependencyInfo(typeof(TextureAsset), typeof(AssetCompilationContext), BuildDependencyType.CompileContent);
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            //skybox needs many shaders to generate... todo should find which ones at some point maybe!
            foreach (var sessionPackage in assetItem.Package.Session.Packages)
            {
                foreach (var sessionPackageAsset in sessionPackage.Assets)
                {
                    if (sessionPackageAsset.Asset is EffectShaderAsset)
                    {
                        yield return new ObjectUrl(UrlType.Content, sessionPackageAsset.Location);
                    }
                }
            }
        }

        protected override void Prepare(AssetCompilerContext context, AssetItem assetItem, string targetUrlInStorage, AssetCompilerResult result)
        {
            var asset = (SkyboxAsset)assetItem.Asset;

            var colorSpace = context.GetColorSpace();

            result.BuildSteps = new AssetBuildStep(assetItem);

            var prereqs = new Queue<BuildStep>();

            // build the textures for windows (needed for skybox compilation)
            foreach (var dependency in asset.GetDependencies())
            {
                var dependencyItem = assetItem.Package.Assets.Find(dependency.Id);
                if (dependencyItem?.Asset is TextureAsset)
                {
                    var textureAsset = (TextureAsset)dependencyItem.Asset;

                    // Get absolute path of asset source on disk
                    var assetSource = GetAbsolutePath(dependencyItem, textureAsset.Source);

                    // Create a synthetic url
                    var textureUrl = SkyboxGenerator.BuildTextureForSkyboxGenerationLocation(dependencyItem.Location);

                    var gameSettingsAsset = context.GetGameSettingsAsset();
                    var renderingSettings = gameSettingsAsset.GetOrCreate<RenderingSettings>(context.Platform);

                    // Select the best graphics profile
                    var graphicsProfile = renderingSettings.DefaultGraphicsProfile >= GraphicsProfile.Level_10_0 ? renderingSettings.DefaultGraphicsProfile : GraphicsProfile.Level_10_0;

                    var textureAssetItem = new AssetItem(textureUrl, textureAsset);

                    // Create and add the texture command.
                    var textureParameters = new TextureConvertParameters(assetSource, textureAsset, PlatformType.Windows, GraphicsPlatform.Direct3D11, graphicsProfile, gameSettingsAsset.GetOrCreate<TextureSettings>().TextureQuality, colorSpace);
                    var prereqStep = new AssetBuildStep(textureAssetItem);
                    prereqStep.Add(new TextureAssetCompiler.TextureConvertCommand(textureUrl, textureParameters, assetItem.Package));
                    result.BuildSteps.Add(prereqStep);
                    prereqs.Enqueue(prereqStep);
                }
            }

            // add the skybox command itself.
            IEnumerable<ObjectUrl> InputFilesGetter()
            {
                var skyboxAsset = (SkyboxAsset)assetItem.Asset;
                foreach (var dependency in skyboxAsset.GetDependencies())
                {
                    var dependencyItem = assetItem.Package.Assets.Find(dependency.Id);
                    if (dependencyItem?.Asset is TextureAsset)
                    {
                        yield return new ObjectUrl(UrlType.Content, dependency.Location);
                    }
                }
            }

            var assetStep = new CommandBuildStep(new SkyboxCompileCommand(targetUrlInStorage, asset, assetItem.Package)
            {
                InputFilesGetter = InputFilesGetter
            });
            result.BuildSteps.Add(assetStep);
            while (prereqs.Count > 0)
            {
                var prereq = prereqs.Dequeue();
                BuildStep.LinkBuildSteps(prereq, assetStep);
            }
        }

        private class SkyboxCompileCommand : AssetCommand<SkyboxAsset>
        {
            public SkyboxCompileCommand(string url, SkyboxAsset parameters, Package package)
                : base(url, parameters, package)
            {
            }

            /// <inheritdoc/>
            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);
                writer.Write(1); // Change this number to recompute the hash when prefiltering algorithm are changed
            }

            /// <inheritdoc/>
            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                // TODO Convert SkyboxAsset to Skybox and save to Skybox object
                // TODO Add system to prefilter

                using (var context = new SkyboxGeneratorContext(Parameters))
                {
                    var result = SkyboxGenerator.Compile(Parameters, context);

                    if (result.HasErrors)
                    {
                        result.CopyTo(commandContext.Logger);
                        return Task.FromResult(ResultStatus.Failed);
                    }

                    context.Content.Save(Url, result.Skybox);
                }

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}
 
