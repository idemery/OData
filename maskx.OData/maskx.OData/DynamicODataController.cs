﻿using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Library;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Query;
using System.Web.OData.Routing;

namespace maskx.OData
{
    public class DynamicODataController : ODataController
    {

        public HttpResponseMessage Get()
        {
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            var options = BuildQueryOptions();
            EdmEntityObjectCollection rtv = null;
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Get,
                    QueryOptions = options,
                    Target = options.Context.Path.Segments[0].ToString()
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            try
            {
                rtv = ds.Get(options);
                return Request.CreateResponse(HttpStatusCode.OK, rtv);
            }
            catch (Exception err)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, err);
            }

        }
        public IHttpActionResult GetSimpleFunction()
        {
            ODataPath path = Request.ODataProperties().Path;

            UnboundFunctionPathSegment seg = path.Segments.FirstOrDefault() as UnboundFunctionPathSegment;
            IEdmType edmType = seg.Function.Function.ReturnType.Definition;

            IEdmType elementType = edmType.TypeKind == EdmTypeKind.Collection
                ? (edmType as IEdmCollectionType).ElementType.Definition
                : edmType;
            ODataQueryContext queryContext = new ODataQueryContext(Request.ODataProperties().Model, elementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, Request);

            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            JObject pars = new JObject();
            foreach (var p in seg.Function.Function.Parameters)
            {
                try
                {
                    var n = seg.GetParameterValue(p.Name);
                    pars.Add(p.Name, new JValue(n));
                }
                catch { }
            }
            var ri = new RequestInfo()
            {
                DataSourceName = dsName,
                Method = MethodType.Func,
                Parameters = pars,
                Target = seg.FunctionName,
                QueryOptions = queryOptions
            };
            if (DynamicOData.BeforeExcute != null)
            {
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Content(ri.StatusCode, ri.Message);
            }
            var b = ds.InvokeFunction(seg.Function.Function, ri.Parameters, ri.QueryOptions);
            if (b is EdmComplexObjectCollection)
                return Ok(b as EdmComplexObjectCollection);
            else
                return Ok(b as EdmComplexObject);
        }
        public IHttpActionResult PostComplexFunction()
        {
            ODataPath path = Request.ODataProperties().Path;
            UnboundFunctionPathSegment seg = path.Segments.FirstOrDefault() as UnboundFunctionPathSegment;
            IEdmType edmType = seg.Function.Function.ReturnType.Definition;
            IEdmType elementType = edmType.TypeKind == EdmTypeKind.Collection
                ? (edmType as IEdmCollectionType).ElementType.Definition
                : edmType;
            ODataQueryContext queryContext = new ODataQueryContext(Request.ODataProperties().Model, elementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, Request);
            var jobj = Request.Content.ReadAsAsync<JObject>().Result;

            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            var ri = new RequestInfo()
            {
                DataSourceName = dsName,
                Method = MethodType.Func,
                Parameters = jobj,
                Target = seg.FunctionName,
                QueryOptions = queryOptions
            };
            if (DynamicOData.BeforeExcute != null)
            {

                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Content(ri.StatusCode, ri.Message);
            }
            var b = ds.InvokeFunction(seg.Function.Function, ri.Parameters, ri.QueryOptions);
            if (b is EdmComplexObjectCollection)
                return Ok(b as EdmComplexObjectCollection);
            else
                return Ok(b as EdmComplexObject);
        }
        public HttpResponseMessage GetCount()
        {
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            var options = BuildQueryOptions();
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Count,
                    QueryOptions = options,
                    Target = options.Context.Path.Segments[0].ToString(),
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            int count = ds.GetCount(BuildQueryOptions());
            return Request.CreateResponse(HttpStatusCode.OK, count);
        }
        public HttpResponseMessage GetFuncResultCount()
        {
            ODataPath path = Request.ODataProperties().Path;
            UnboundFunctionPathSegment seg = path.Segments.FirstOrDefault() as UnboundFunctionPathSegment;
            IEdmType edmType = seg.Function.Function.ReturnType.Definition;
            IEdmType elementType = edmType.TypeKind == EdmTypeKind.Collection
                ? (edmType as IEdmCollectionType).ElementType.Definition
                : edmType;
            ODataQueryContext queryContext = new ODataQueryContext(Request.ODataProperties().Model, elementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, Request);
            JObject pars;
            if (Request.Method == HttpMethod.Get)
            {
                pars = new JObject();
                foreach (var p in seg.Function.Function.Parameters)
                {
                    try
                    {
                        var n = seg.GetParameterValue(p.Name);
                        pars.Add(p.Name, new JValue(n));
                    }
                    catch { }
                }
            }
            else
            {
                pars = Request.Content.ReadAsAsync<JObject>().Result;
            }


            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            var ri = new RequestInfo()
            {
                DataSourceName = dsName,
                Method = MethodType.FuncResultCount,
                Parameters = pars,
                Target = seg.FunctionName,
                QueryOptions = queryOptions
            };
            if (DynamicOData.BeforeExcute != null)
            {
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message); ;
            }
            var count = ds.GetFuncResultCount(seg.Function.Function, ri.Parameters, ri.QueryOptions);
            return Request.CreateResponse(HttpStatusCode.OK, count);
        }
        private ODataQueryOptions BuildQueryOptions()
        {
            ODataPath path = Request.ODataProperties().Path;
            IEdmType edmType = path.Segments[0].GetEdmType(path.EdmType);
            IEdmType elementType = edmType.TypeKind == EdmTypeKind.Collection
                ? (edmType as IEdmCollectionType).ElementType.Definition
                : edmType;
            ODataQueryContext queryContext = new ODataQueryContext(Request.ODataProperties().Model, elementType, path);
            ODataQueryOptions queryOptions = new ODataQueryOptions(queryContext, Request);
            return queryOptions;
        }
        //Get entityset(key)
        public HttpResponseMessage Get(string key)
        {
            var options = BuildQueryOptions();
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Get,
                    QueryOptions = options,
                    Target = options.Context.Path.Segments[0].ToString()
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            var b = ds.Get(key, options);
            return Request.CreateResponse(HttpStatusCode.OK, b);
        }
        public HttpResponseMessage Post(IEdmEntityObject entity)
        {
            ODataPath path = Request.ODataProperties().Path;
            IEdmType edmType = path.EdmType;
            if (edmType.TypeKind != EdmTypeKind.Collection)
            {
                throw new Exception("we are serving POST {entityset}");
            }
            string rtv = null;
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Create,
                    Target = (edmType as EdmEntityType).Name
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            try
            {
                rtv = ds.Create(entity);
            }
            catch (Exception err)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, err);
            }
            return Request.CreateResponse(HttpStatusCode.Created, rtv);
        }
        public HttpResponseMessage Delete(string key)
        {
            var path = Request.ODataProperties().Path;
            var edmType = path.Segments[0].GetEdmType(path.EdmType);
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Delete,
                    Target = (edmType as EdmEntityType).Name
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            var count = ds.Delete(key, edmType);
            return Request.CreateResponse(HttpStatusCode.OK, count);
        }
        public HttpResponseMessage Patch(string key, IEdmEntityObject entity)
        {
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Merge,
                    Target = (entity.GetEdmType().Definition as EdmEntityType).Name
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            var count = ds.Merge(key, entity);
            return Request.CreateResponse(HttpStatusCode.OK, count);
        }
        public HttpResponseMessage Put(string key, IEdmEntityObject entity)
        {
            string dsName = (string)Request.Properties[Constants.ODataDataSource];
            var ds = DataSourceProvider.GetDataSource(dsName);
            if (DynamicOData.BeforeExcute != null)
            {
                var ri = new RequestInfo()
                {
                    DataSourceName = dsName,
                    Method = MethodType.Replace,
                    Target = (entity.GetEdmType().Definition as EdmEntityType).Name
                };
                DynamicOData.BeforeExcute(ri);
                if (!ri.Result)
                    return Request.CreateResponse(ri.StatusCode, ri.Message);
            }
            var count = ds.Replace(key, entity);
            return Request.CreateResponse(HttpStatusCode.OK, count);
        }
    }
}