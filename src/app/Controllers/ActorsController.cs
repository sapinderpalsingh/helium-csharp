﻿using System;
using CSE.Helium.DataAccessLayer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading.Tasks;
using CSE.Helium.Interfaces;
using CSE.Helium.Validation;
using Helium.Model;

namespace CSE.Helium.Controllers
{
    /// <summary>
    /// Handle all of the /api/actors requests
    /// </summary>
    [Route("api/[controller]")]
    public class ActorsController : Controller
    {
        private readonly ILogger logger;
        private readonly IDAL dal;
        private readonly IParameterValidator parameterValidator;

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="logger">log instance</param>
        /// <param name="dal">data access layer instance</param>
        public ActorsController(ILogger<ActorsController> logger, IDAL dal, IParameterValidator parameterValidator)
        {
            // save to local for use in handlers
            this.logger = logger;
            this.dal = dal;
            this.parameterValidator = parameterValidator;
        }

        /// <summary>
        /// Returns a JSON array of Actor objects
        /// </summary>
        /// <param name="q">(optional) The term used to search Actor name</param>
        /// <param name="pageNumber">1 based page index</param>
        /// <param name="pageSize">page size (1000 max)</param>
        /// <response code="200">JSON array of Actor objects or empty array if not found</response>
        [HttpGet]
        public async Task<IActionResult> GetActorsAsync([FromQuery]ActorQueryParameters queryParameters)
        {
            _ = queryParameters ?? throw new ArgumentNullException(nameof(queryParameters));

            string method = GetMethodText(queryParameters.Q, queryParameters.PageNumber, queryParameters.PageSize);

            if (!ModelState.IsValid)
                return ValidationProcessor.GetAndLogInvalidSearchParameter(method, logger);

            // validate query string parameters
            //var result = parameterValidator.ValidateCommonParameters(HttpContext?.Request?.Query, queryParameters.Q, queryParameters.PageNumber, queryParameters.PageSize, method, logger);
            //if (result != null)
            //{
            //    return result;
            //}

            // convert to zero based index
            queryParameters.PageNumber = queryParameters.PageNumber > 1 ? queryParameters.PageNumber - 1 : 0;

            return await ResultHandler.Handle(dal.GetActorsAsync(queryParameters.Q, queryParameters.PageNumber * queryParameters.PageSize, queryParameters.PageSize), method, Constants.ActorsControllerException, logger).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a single JSON Actor by actorId
        /// </summary>
        /// <param name="actorId">The actorId</param>
        /// <response code="404">actorId not found</response>
        [HttpGet("{actorId}")]
        public async Task<IActionResult> GetActorByIdAsync(string actorId)
        {
            string method = "GetActorByIdAsync " + actorId;

            // validate actorId
            var result = parameterValidator.ValidateActorId(actorId, method, logger);
            if (result != null)
            {
                return result;
            }

            // return result
            return await ResultHandler.Handle(dal.GetActorAsync(actorId), method, "Actor Not Found", logger).ConfigureAwait(false);
        }

        /// <summary>
        /// Add parameters to the method name if specified in the query string
        /// </summary>
        /// <param name="q"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        private string GetMethodText(string q, int? pageNumber, int? pageSize)
        {
            string method = "GetActorsAsync";

            if (HttpContext != null && HttpContext.Request != null && HttpContext.Request.Query != null)
            {
                // add the query parameters to the method name if exists
                if (HttpContext.Request.Query.ContainsKey("q"))
                {
                    method = string.Format(CultureInfo.InvariantCulture, $"{method}:q:{q}");
                }
                if (HttpContext.Request.Query.ContainsKey("pageNumber"))
                {
                    method = string.Format(CultureInfo.InvariantCulture, $"{method}:pageNumber:{pageNumber}");
                }
                if (HttpContext.Request.Query.ContainsKey("pageSize"))
                {
                    method = string.Format(CultureInfo.InvariantCulture, $"{method}:pageSize:{pageSize}");
                }
            }

            return method;
        }
    }
}
