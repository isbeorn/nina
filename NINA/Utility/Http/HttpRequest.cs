#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace NINA.Utility.Http {

    internal abstract class HttpRequest {

        public HttpRequest(string url) {
            this.Url = url;
        }

        public string Url { get; }

        public abstract Task Request(CancellationToken ct, IProgress<int> progress = null);

        public static string EncodeUrl(string s) {
            return HttpUtility.UrlEncode(s);
        }
    }

    internal abstract class HttpRequest<T> {

        public HttpRequest(string url) {
            this.Url = url;
        }

        public string Url { get; }

        public abstract Task<T> Request(CancellationToken ct, IProgress<int> progress = null);
    }
}