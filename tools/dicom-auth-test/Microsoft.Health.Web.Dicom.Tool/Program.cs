﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using FellowOakDicom;
using Microsoft.Health.Dicom.Client;

namespace Microsoft.Health.Web.Dicom.Tool;

public static class Programstore
{
    public static void Main(string[] args)
    {
        ParseArgumentsAndExecute(args);
    }

    private static void ParseArgumentsAndExecute(string[] args)
    {
        var rootCommand = new RootCommand();

        var dicomWebCommand = new Command("execute", "Execute Store Get and Delete")
        {
            new Option<string>(
                    "--dicomServiceUrl",
                    description: "DicomService Url ex: https://testdicomweb-testdicom.dicom.azurehealthcareapis.com"),
        };

        dicomWebCommand.Handler = CommandHandler.Create<string>(StoreImageAsync);
        rootCommand.AddCommand(dicomWebCommand);
        rootCommand.Invoke(args);
    }

    private static async Task StoreImageAsync(string dicomServiceUrl)
    {
        var dicomFile = await DicomFile.OpenAsync(@"./Image/blue-circle.dcm");

        using var httpClient = new HttpClient();

        httpClient.BaseAddress = new Uri(dicomServiceUrl);

        // Use VM assigned managed identity.
        var credential = new DefaultAzureCredential();

        // Access token will expire after a certain period of time.
        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://dicom.healthcareapis.azure.com/.default" }));
        var accessToken = token.Token;

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        IDicomWebClient client = new DicomWebClient(httpClient);

        var response = await client.StoreAsync(dicomFile);

        string output = new string("Image saved with statuscode: ");
        Console.WriteLine(output + response.StatusCode);

        string studyInstanceUid = dicomFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID);

        var responseGet = await client.RetrieveStudyAsync(studyInstanceUid);

        output = new string("Image retrieved with statuscode: ");
        Console.WriteLine(output + responseGet.StatusCode);

        var responseDelete = await client.DeleteStudyAsync(studyInstanceUid);

        output = new string("Image deleted with statuscode: ");
        Console.WriteLine(output + responseDelete.StatusCode);
    }
}