﻿// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Bot Framework: http://botframework.com
// 
// Bot Builder SDK GitHub:
// https://github.com/Microsoft/BotBuilder
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.FormFlow.Advanced
{
    [Serializable]
    // TODO-MK: now we are resolving to a Stream - we could probably return stream + additional data (original name, type, etc)
    // discuss what about to return an Attachment with the Stream or byte[] in the 'object content' property
    // download attachment data for validation and store in another place?
    // use data annotation validators as parameter to ctor and validate source?
    public class AwaitableAttachment : IAwaitable<Stream>, IAwaiter<Stream>, ISerializable
    {
        private readonly IAwaiter<Stream> awaiter;

        private readonly Attachment attachment;

        public AwaitableAttachment(Attachment attachment)
        {
            this.attachment = attachment;

            this.awaiter = Awaitable.FromSource(attachment, this.ResolveFromSourceAsync) as IAwaiter<Stream>;
        }

        public Attachment Attachment
        {
            get
            {
                return this.attachment;
            }
        }

        protected AwaitableAttachment(SerializationInfo info, StreamingContext context)
        {
            // constructor arguments
            var jsonAttachment = default(string);

            SetField.NotNullFrom(out jsonAttachment, nameof(this.attachment), info);
            this.attachment = JsonConvert.DeserializeObject<Attachment>(jsonAttachment);

            this.awaiter = Awaitable.FromSource(this.attachment, this.ResolveFromSourceAsync) as IAwaiter<Stream>;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // constructor arguments
            info.AddValue(nameof(this.attachment), JsonConvert.SerializeObject(this.attachment));
        }

        public bool IsCompleted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IAwaiter<Stream> GetAwaiter()
        {
            return this.awaiter;
        }

        public Stream GetResult()
        {
            throw new NotImplementedException();
        }

        public void OnCompleted(Action continuation)
        {
            throw new NotImplementedException();
        }

        public virtual async Task<bool> IsValidAsync<T>(IField<T> field) where T: class
        {
            return true;
        }

        protected virtual async Task<Stream> ResolveFromSourceAsync(Attachment source)
        {
            var stream = new MemoryStream();

            // TODO-MK: handle specific channel stuff - ie. authorization, etc
            // here another client/stream type could be used as well to handle big payloads
            using (HttpClient httpClient = new HttpClient())
            {
                var bytes = await httpClient.GetByteArrayAsync(source.ContentUrl);
                await stream.WriteAsync(bytes, 0, bytes.Length);

                // reset to start
                stream.Position = 0;
            }

            // TODO-MK: add some validation when type resolved?

            return stream;
        }
    }
}
