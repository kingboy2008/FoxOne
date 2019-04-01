using FoxOne.Core;
using System;

namespace FoxOne.Business.OAuth
{
    public class DingDingAuthenticationHandler : AuthenticationHandler
    {
        public DingDingAuthenticationHandler(AuthenticationOptions options) : base(options)
        {

        }

        public override string GetAuthorizationUrl(AuthenticationScope scope)
        {
            return string.Format("{0}/connect/qrconnect?response_type=code&appid={1}&scope={2}&state={3}&redirect_uri={4}", _options.AuthorizeUrl, _options.AppId, scope.Scope, scope.State, Uri.EscapeDataString(string.Concat(_options.Host, _options.Callback)));
        }
        public override AuthenticationTicket PreAuthorization(AuthenticationTicket ticket)
        {
            string tokenEndpoint = string.Concat(_options.AuthorizeUrl, "/sns/gettoken?appid={0}&appsecret={1}");
            var url = string.Format(
                     tokenEndpoint,
                     Uri.EscapeDataString(_options.AppId),
                     Uri.EscapeDataString(_options.AppSecret));
            string tokenResponse = HttpHelper.Get(url);
            Logger.Info("请求url：{0}，返回值：{1}", url, tokenResponse);
            var payload = JSONHelper.Deserialize(tokenResponse, typeof(Callback)) as Callback;
            ticket.AccessToken = payload.access_token;
            return ticket;
        }
        public override AuthenticationTicket AuthenticateCore(AuthenticationTicket ticket)
        {
            string tokenEndpoint = string.Concat(_options.AuthorizeUrl, "/sns/get_persistent_code?access_token={0}");
            var url = string.Format(
                     tokenEndpoint, ticket.AccessToken);
            string tokenResponse = HttpHelper.Post(url, "{{\"tmp_auth_code\": \"{0}\"}}".FormatTo(ticket.Code));
            Logger.Info("请求url：{0}，返回值：{1}", url, tokenResponse);
            var payload = JSONHelper.Deserialize(tokenResponse, typeof(Callback)) as Callback;
            ticket.OpenId = payload.openid;
            ticket.UnionId = payload.unionid;
            return ticket;
        }

        private class Callback
        {
            /// <summary>
            /// 客户端Id
            /// </summary>
            public string unionid { get; set; }


            public string persistent_code { get; set; }

            /// <summary>
            /// 用户Id
            /// </summary>
            public string openid { get; set; }


            public string access_token { get; set; }


            public string errcode { get; set; }


            public string errmsg { get; set; }
        }
    }
}
