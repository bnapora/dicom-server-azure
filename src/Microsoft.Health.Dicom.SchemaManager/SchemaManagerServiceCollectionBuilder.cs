// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Health.Dicom.SqlServer.Features.Schema;
using Microsoft.Health.SqlServer.Configs;
using Microsoft.Health.SqlServer.Features.Schema.Manager;
using Microsoft.Health.SqlServer.Features.Schema.Messages.Notifications;
using Microsoft.Health.SqlServer.Registration;

namespace Microsoft.Health.Dicom.SchemaManager;

public static class SchemaManagerServiceCollectionBuilder
{
    public static IServiceCollection AddSchemaManager(this IServiceCollection services, IConfiguration config)
    {
        services.AddCliCommands();

        services.SetCommandLineOptions(config);

        services.AddOptions<SqlServerDataStoreConfiguration>().Configure<IOptions<CommandLineOptions>>((s, c) =>
        {
            s.ConnectionString = c.Value.ConnectionString;

#pragma warning disable CS0618 // Type or member is obsolete
            s.AuthenticationType = c.Value.AuthenticationType ?? SqlServerAuthenticationType.ConnectionString;

            if (!string.IsNullOrWhiteSpace(c.Value.ManagedIdentityClientId))
            {
                s.ManagedIdentityClientId = c.Value.ManagedIdentityClientId;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        });

        services.AddSqlServerConnection();

        services.AddSqlServerManagement<SchemaVersion>();

        services.AddSingleton<BaseSchemaRunner>();
        services.AddSingleton<IBaseSchemaRunner, DicomBaseSchemaRunner>();

        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<SchemaUpgradedNotification>());

        services.AddSingleton<ISchemaClient, DicomSchemaClient>();
        services.AddSingleton<ISchemaManager, SqlSchemaManager>();
        services.AddLogging(configure => configure.AddConsole());
        return services;
    }
}
