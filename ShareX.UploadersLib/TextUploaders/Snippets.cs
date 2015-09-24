#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2015 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using Newtonsoft.Json;
using ShareX.UploadersLib.HelperClasses;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace ShareX.UploadersLib.TextUploaders
{
    public sealed class Snippets : TextUploader, IOAuth2
    {
        public Privacy PublishType { get; set; }
        public OAuth2Info AuthInfo { get; set; }

        public Snippets(OAuth2Info oauth)
        {
            AuthInfo = oauth;
        }

        public string GetAuthorizationURL()
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("client_id", AuthInfo.Client_ID);
            args.Add("redirect_uri", HelpersLib.Links.URL_CALLBACK);
            args.Add("response_type", "code");

            return CreateQuery("https://bitbucket.org/site/oauth2/authorize", args);
        }

        public bool GetAccessToken(string code)
        {
            Dictionary<string, string> args = new Dictionary<string, string>();
            args.Add("redirect_uri", HelpersLib.Links.URL_CALLBACK);
            args.Add("grant_type", "authorization_code");
            args.Add("code", code);

            NameValueCollection headers = CreateAuthenticationHeader(AuthInfo.Client_ID, AuthInfo.Client_Secret);

            string response = SendRequest(HttpMethod.POST, "https://bitbucket.org/site/oauth2/access_token", args, headers);

            if (!string.IsNullOrEmpty(response))
            {
                OAuth2Token token = JsonConvert.DeserializeObject<OAuth2Token>(response);

                if (token != null && !string.IsNullOrEmpty(token.access_token))
                {
                    token.UpdateExpireDate();
                    AuthInfo.Token = token;
                    return true;
                }
            }

            return false;
        }

        public bool RefreshAccessToken()
        {
            if (OAuth2Info.CheckOAuth(AuthInfo) && !string.IsNullOrEmpty(AuthInfo.Token.refresh_token))
            {
                Dictionary<string, string> args = new Dictionary<string, string>();
                args.Add("grant_type", "refresh_token");
                args.Add("refresh_token", AuthInfo.Token.refresh_token);

                NameValueCollection headers = CreateAuthenticationHeader(AuthInfo.Client_ID, AuthInfo.Client_Secret);

                string response = SendRequest(HttpMethod.POST, "https://bitbucket.org/site/oauth2/access_token", args, headers);

                if (!string.IsNullOrEmpty(response))
                {
                    OAuth2Token token = JsonConvert.DeserializeObject<OAuth2Token>(response);

                    if (token != null && !string.IsNullOrEmpty(token.access_token))
                    {
                        token.UpdateExpireDate();
                        AuthInfo.Token = token;
                        return true;
                    }
                }
            }

            return false;
        }

        private NameValueCollection GetAuthHeaders()
        {
            NameValueCollection headers = new NameValueCollection();
            headers.Add("Authorization", "Bearer " + AuthInfo.Token.access_token);
            return headers;
        }

        public bool CheckAuthorization()
        {
            if (OAuth2Info.CheckOAuth(AuthInfo))
            {
                if (AuthInfo.Token.IsExpired && !RefreshAccessToken())
                {
                    Errors.Add("Refresh access token failed.");
                    return false;
                }
            }
            else
            {
                Errors.Add("Snippets login is required.");
                return false;
            }

            return true;
        }

        public override UploadResult UploadText(string text, string fileName)
        {
            UploadResult uploadResult = new UploadResult();

            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(fileName))
            {
                Dictionary<string, string> args = new Dictionary<string, string>();
                NameValueCollection headers;

                var SnippetsUploadObject = new
                {
                    access_token = AuthInfo.Token.access_token,
                    is_private = PublishType == Privacy.Private ? "true" : "false",
                    file = new Dictionary<string, object>
                    {
                        { fileName, new { content = text } }
                    }
                };

                headers = GetAuthHeaders();

                string url = "https://api.bitbucket.org/2.0/snippets/";
                string argsJson = JsonConvert.SerializeObject(SnippetsUploadObject);
                string response = SendRequestJSON(url, argsJson, headers);

                UploadData(
                if (response != null)
                {
                    SnippetsResponse snippetsResult = JsonConvert.DeserializeObject<SnippetsResponse>(response);
                    uploadResult.URL = snippetsResult.links.html.href;
                }
            }
            return uploadResult;
        }
    }

    public class SnippetsResponseLink
    {
        public string href { get; set; }
    }

    public class SnippetsResponseFile
    {
        public class links
        {
            public SnippetsResponseLink html { get; set; }
            public SnippetsResponseLink self { get; set; }
        }
    }

    public class SnippetsResponseUser
    {
        public string display_name { get; set; }

        public class links
        {
            public SnippetsResponseLink avatar { get; set; }
            public SnippetsResponseLink html { get; set; }
            public SnippetsResponseLink self { get; set; }
        }

        public string username { get; set; }
        public string uuid { get; set; }
    }

    public class SnippetsResponseResultLinks
    {
        public SnippetsResponseLink comments { get; set; }
        public SnippetsResponseLink commits { get; set; }
        public SnippetsResponseLink diff { get; set; }
        public SnippetsResponseLink html { get; set; }
        public SnippetsResponseLink patch { get; set; }
        public SnippetsResponseLink self { get; set; }
        public SnippetsResponseLink watchers { get; set; }
    }

    public class SnippetsResponse
    {
        public DateTime created_on { get; set; }

        public SnippetsResponseUser creator { get; set; }
        public List<SnippetsResponseFile> files { get; set; }
        public string id { get; set; }
        public bool is_private { get; set; }
        public SnippetsResponseResultLinks links { get; set; }
        public SnippetsResponseUser owner { get; set; }
        public string scm { get; set; }
        public string title { get; set; }
        public DateTime updated_one { get; set; }
    }
}