﻿using Microsoft.Samples.AzureAD.Graph.API.Models;
using Microsoft.Store.PartnerCenter.Samples.Common;
using Microsoft.Store.PartnerCenter.Samples.Common.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Samples.AzureAD.Graph.API
{
    /// <summary>
    /// Object utilized to interface with the Azure AD Graph API.
    /// </summary>
    /// <seealso cref="IGraphClient" />
    public class GraphClient : IGraphClient
    {
        private AuthorizationToken _aadToken;
        private Communication _comm;
        private string _userAssertionToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphClient" /> class.
        /// </summary>
        /// <param name="userAssertionToken">The user assertion token.</param>
        /// <exception cref="ArgumentNullException">userAssertionToken</exception>
        public GraphClient(string userAssertionToken)
        {
            if (string.IsNullOrEmpty(userAssertionToken))
            {
                throw new ArgumentNullException("userAssertionToken");
            }

            _comm = new Communication();
            _userAssertionToken = userAssertionToken;
        }

        /// <summary>
        /// Gets a collection of users that belong to the specified tenant identifer.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>
        /// A collection of users that belong to the specified tenant identifer.
        /// </returns>
        /// <exception cref="ArgumentNullException">tenantId</exception>
        public List<User> GetUsers(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException("tenantId");
            }

            return SynchronousExecute(() => GetUsersAsync(tenantId));
        }

        /// <summary>
        /// Asynchronously get a collection of users that belong to the specified tenant identifier.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>
        /// A collection of users that belong to the specified tenant identifer.
        /// </returns>
        /// <exception cref="ArgumentNullException">tenantId</exception>
        public async Task<List<User>> GetUsersAsync(string tenantId)
        {
            Result<User> users;
            string requestUri;

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException("tenantId");
            }

            requestUri = string.Format(
                "{0}/{1}/users?api-version=1.6",
                Configuration.AzureADGraphAPIRoot,
                tenantId
            );

            users = await _comm.GetAsync<Result<User>>(
                requestUri,
                new MediaTypeWithQualityHeaderValue("application/json"),
                GetAADToken(tenantId).AccessToken,
                Guid.NewGuid().ToString()
            );

            return users.Value;
        }

        /// <summary>
        /// Gets an Azure Active Directory asynchronously. 
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns></returns>
        private async Task<AuthorizationToken> GetAADTokenAsync(string tenantId)
        {
            HttpContent content;
            List<KeyValuePair<string, string>> values;
            string requestUri;

            try
            {
                values = new List<KeyValuePair<string, string>>();

                values.Add(new KeyValuePair<string, string>("assertion", _userAssertionToken));
                values.Add(new KeyValuePair<string, string>("client_id", Configuration.ApplicationId));
                values.Add(new KeyValuePair<string, string>("client_secret", Configuration.ApplicationSecret));
                values.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"));
                values.Add(new KeyValuePair<string, string>("requested_token_use", "on_behalf_of"));
                values.Add(new KeyValuePair<string, string>("resource", Configuration.AzureADGraphAPIRoot));
                values.Add(new KeyValuePair<string, string>("scope", "openid"));

                content = new FormUrlEncodedContent(values);

                requestUri = string.Format(
                    "{0}/{1}/oauth2/token",
                    Configuration.Authority,
                    tenantId
                );

                return await _comm.PostAsync<AuthorizationToken>(requestUri, content);
            }
            finally
            {
                content = null;
                values = null;
            }
        }

        /// <summary>
        /// Synchronously obtains an Azure AD token token.
        /// </summary>
        /// <param name="tenantId">The tenant identifier.</param>
        /// <returns>
        /// An instance of <see cref="AuthorizationToken"/> containing the access token.
        /// </returns>
        private AuthorizationToken GetAADToken(string tenantId)
        {
            if (_aadToken == null || _aadToken.IsNearExpiry())
            {
                _aadToken = SynchronousExecute(
                    () => GetAADTokenAsync(tenantId)
                );
            }

            return _aadToken;
        }

        /// <summary>
        /// Synchronously executes an asynchronous function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation">The operation.</param>
        /// <returns></returns>
        private T SynchronousExecute<T>(Func<Task<T>> operation)
        {
            try
            {
                return Task.Run(async () => await operation()).Result;
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}