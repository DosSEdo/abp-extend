﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.Creator;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Middleware;
using Ocelot.Responses;
using Taitans.Ocelot.Provider.Abp.Repository;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Taitans.Ocelot.Provider.Abp.Configuration
{
    public class DataBaseConfigurationProvider
    {
        public static OcelotMiddlewareConfigurationDelegate Get = async builder =>
        {
            var fileConfigRepo = builder.ApplicationServices.GetService<IFileConfigurationRepository>();
            var fileConfig = builder.ApplicationServices.GetService<IOptionsMonitor<FileConfiguration>>();
            var internalConfigCreator = builder.ApplicationServices.GetService<IInternalConfigurationCreator>();
            var internalConfigRepo = builder.ApplicationServices.GetService<IInternalConfigurationRepository>();
            if (UsingAbp(fileConfigRepo))
            {
                await SetFileConfigInDataBase(builder, fileConfigRepo, fileConfig, internalConfigCreator, internalConfigRepo);
            }


        };

        private static bool UsingAbp(IFileConfigurationRepository fileConfigRepo)
        {
            return fileConfigRepo.GetType() == typeof(FileConfigurationRepository);
        }

        private static async Task SetFileConfigInDataBase(IApplicationBuilder builder,
            IFileConfigurationRepository fileConfigRepo, IOptionsMonitor<FileConfiguration> fileConfig,
            IInternalConfigurationCreator internalConfigCreator, IInternalConfigurationRepository internalConfigRepo)
        {

            // get the config from consul.
            var fileConfigFromDataBase = await fileConfigRepo.Get();

            if (IsError(fileConfigFromDataBase))
            {
                ThrowToStopOcelotStarting(fileConfigFromDataBase);
            }
            //else if (ConfigNotStoredInDataBase(fileConfigFromDataBase))
            //{
            //    //there was no config in consul set the file in config in consul
            //    await fileConfigRepo.Set(fileConfig.CurrentValue);
            //}
            else
            {
                await fileConfigRepo.Set(fileConfigFromDataBase.Data);
                // create the internal config from consul data
                var internalConfig = await internalConfigCreator.Create(fileConfigFromDataBase.Data);

                if (IsError(internalConfig))
                {
                    ThrowToStopOcelotStarting(internalConfig);
                }
                else
                {
                    // add the internal config to the internal repo
                    var response = internalConfigRepo.AddOrReplace(internalConfig.Data);

                    if (IsError(response))
                    {
                        ThrowToStopOcelotStarting(response);
                    }
                }

                if (IsError(internalConfig))
                {
                    ThrowToStopOcelotStarting(internalConfig);
                }
            }
        }

        private static void ThrowToStopOcelotStarting(Response config)
        {
            throw new Exception($"Unable to start Ocelot, errors are: {string.Join(",", config.Errors.Select(x => x.ToString()))}");
        }

        private static bool IsError(Response response)
        {
            return response == null || response.IsError;
        }

        private static bool ConfigNotStoredInDataBase(Response<FileConfiguration> fileConfigFromDataBase)
        {
            return fileConfigFromDataBase.Data == null;
        }
    }
}
