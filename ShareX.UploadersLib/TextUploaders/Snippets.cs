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
using Newtonsoft.Json.Linq;
using ShareX.UploadersLib.HelperClasses;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

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
            UploadResult ur = new UploadResult();

            if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(fileName))
            {
                var gistUploadObject = new
                {
                    @public = PublishType,
                    files = new Dictionary<string, object>
                    {
                        { fileName, new { content = text } }
                    }
                };

                string argsJson = JsonConvert.SerializeObject(gistUploadObject);

                string url = "url bla bla";

                if (AuthInfo != null)
                {
                    url += "?access_token=" + AuthInfo.Token.access_token;
                }

                string response = SendRequestJSON(url, argsJson);

                if (response != null)
                {
                    var gistReturnType = new { html_url = string.Empty };
                    var gistReturnObject = JsonConvert.DeserializeAnonymousType(response, gistReturnType);
                    ur.URL = gistReturnObject.html_url;
                }
            }

            return ur;
        }

        //public override UploadResult Upload(Stream stream, string fileName)
        //{
        //    Dictionary<string, string> args = new Dictionary<string, string>();
        //    NameValueCollection headers;

        //    if (UploadMethod == AccountType.User)
        //    {
        //        if (!CheckAuthorization())
        //        {
        //            return null;
        //        }

        //        if (!string.IsNullOrEmpty(UploadAlbumID))
        //        {
        //            args.Add("album", UploadAlbumID);
        //        }

        //        headers = GetAuthHeaders();
        //    }
        //    else
        //    {
        //        headers = new NameValueCollection();
        //        headers.Add("Authorization", "Client-ID " + AuthInfo.Client_ID);
        //    }

        //    UploadResult result = UploadData(stream, "https://api.imgur.com/3/image", fileName, "image", args, headers);

        //    if (!string.IsNullOrEmpty(result.Response))
        //    {
        //        ImgurResponse imgurResponse = JsonConvert.DeserializeObject<ImgurResponse>(result.Response);

        //        if (imgurResponse != null)
        //        {
        //            if (imgurResponse.success && imgurResponse.status == 200)
        //            {
        //                ImgurImageData imageData = ((JObject)imgurResponse.data).ToObject<ImgurImageData>();

        //                if (imageData != null && !string.IsNullOrEmpty(imageData.link))
        //                {
        //                    if (DirectLink)
        //                    {
        //                        if (UseGIFV && !string.IsNullOrEmpty(imageData.gifv))
        //                        {
        //                            result.URL = imageData.gifv;
        //                        }
        //                        else
        //                        {
        //                            result.URL = imageData.link;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        result.URL = "http://imgur.com/" + imageData.id;
        //                    }

        //                    int index = result.URL.LastIndexOf('.');
        //                    string thumbnail = string.Empty;

        //                    switch (ThumbnailType)
        //                    {
        //                        case ImgurThumbnailType.Small_Square:
        //                            thumbnail = "s";
        //                            break;
        //                        case ImgurThumbnailType.Big_Square:
        //                            thumbnail = "b";
        //                            break;
        //                        case ImgurThumbnailType.Small_Thumbnail:
        //                            thumbnail = "t";
        //                            break;
        //                        case ImgurThumbnailType.Medium_Thumbnail:
        //                            thumbnail = "m";
        //                            break;
        //                        case ImgurThumbnailType.Large_Thumbnail:
        //                            thumbnail = "l";
        //                            break;
        //                        case ImgurThumbnailType.Huge_Thumbnail:
        //                            thumbnail = "h";
        //                            break;
        //                    }

        //                    result.ThumbnailURL = string.Format("http://i.imgur.com/{0}{1}.jpg", imageData.id, thumbnail); // Thumbnails always jpg
        //                    result.DeletionURL = "http://imgur.com/delete/" + imageData.deletehash;
        //                }
        //            }
        //            else
        //            {
        //                HandleErrors(imgurResponse);
        //            }
        //        }
        //    }

        //    return result;
        //}

        private void HandleErrors(ImgurResponse response)
        {
            ImgurErrorData errorData = ((JObject)response.data).ToObject<ImgurErrorData>();

            if (errorData != null)
            {
                string errorMessage = string.Format("Status: {0}, Request: {1}, Error: {2}", response.status, errorData.request, errorData.error);
                Errors.Add(errorMessage);
            }
        }
    }

    public class ImgurResponse
    {
        public object data { get; set; }
        public bool success { get; set; }
        public int status { get; set; }
    }

    public class ImgurErrorData
    {
        public string error { get; set; }
        public string request { get; set; }
        public string method { get; set; }
    }


}