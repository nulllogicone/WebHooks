﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Microsoft.AspNet.WebHooks.Properties;

namespace Microsoft.AspNet.WebHooks
{
    /// <summary>
    /// Provides an <see cref="IWebHookReceiver"/> implementation which supports WebHooks generated by MailChimp.
    /// A sample WebHook URI is '<c>https://&lt;host&gt;/api/webhooks/incoming/mailchimp/{id}?code=83699ec7c1d794c0c780e49a5c72972590571fd8</c>'.
    /// For security reasons the WebHook URI must be an <c>https</c> URI and contain a 'code' query parameter with the
    /// same value as configured in the '<c>MS_WebHookReceiverSecret_MailChimp</c>' application setting.
    /// The 'code' parameter must be between 32 and 128 characters long.
    /// For details about MailChimp WebHooks, see <c>https://apidocs.mailchimp.com/webhooks/</c>.
    /// </summary>
    public class MailChimpWebHookReceiver : WebHookReceiver
    {
        internal const string RecName = "mailchimp";

        /// <summary>
        /// Gets the receiver name for this receiver.
        /// </summary>
        public static string ReceiverName
        {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override string Name
        {
            get { return RecName; }
        }

        /// <inheritdoc />
        public override async Task<HttpResponseMessage> ReceiveAsync(string id, HttpRequestContext context, HttpRequestMessage request)
        {
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Method == HttpMethod.Post)
            {
                // Ensure that we use https and have a valid code parameter
                await EnsureValidCode(request, id);

                // Read the request entity body
                var data = await ReadAsFormDataAsync(request);

                var action = data["type"];
                if (string.IsNullOrEmpty(action))
                {
                    var message = string.Format(CultureInfo.CurrentCulture, MailChimpReceiverResources.Receiver_BadBody, "type");
                    context.Configuration.DependencyResolver.GetLogger().Error(message);
                    var badType = request.CreateErrorResponse(HttpStatusCode.BadRequest, message);
                    return badType;
                }

                // Call registered handlers
                return await ExecuteWebHookAsync(id, context, request, new string[] { action }, data);
            }
            else if (request.Method == HttpMethod.Get)
            {
                // Ensure that we use https and have a valid code parameter
                await EnsureValidCode(request, id);

                return request.CreateResponse();
            }
            else
            {
                return CreateBadMethodResponse(request);
            }
        }
    }
}
