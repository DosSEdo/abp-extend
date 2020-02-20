﻿using JetBrains.Annotations;
using Volo.Abp.MongoDB;

namespace Taitans.Abp.Identity.MongoDB
{
    public class IdentityMongoModelBuilderConfigurationOptions : AbpMongoModelBuilderConfigurationOptions
    {
        public IdentityMongoModelBuilderConfigurationOptions(
            [NotNull] string collectionPrefix = "")
            : base(collectionPrefix)
        {
        }
    }
}