﻿using Newtonsoft.Json.Linq;
using System;

namespace ReactNative.Bridge
{
    class Promise : IPromise
    {
        private const string DefaultError = "EUNSPECIFIED";

        private readonly ICallback _resolve;
        private readonly ICallback _reject;

        public Promise(ICallback resolve, ICallback reject)
        {
            _resolve = resolve;
            _reject = reject;
        }

        public void Resolve(object value)
        {
            if (_resolve != null)
            {
                _resolve.Invoke(value);
            }
        }

        public void Reject(string code, string message)
        {
            Reject(code, message, default(Exception));
        }

        public void Reject(string message)
        {
            Reject(DefaultError, message, default(Exception));
        }

        public void Reject(string code, Exception e)
        {
            Reject(code, e.Message, e);
        }

        public void Reject(Exception e)
        {
            if (e == null)
                throw new ArgumentNullException(nameof(e));

            Reject(DefaultError, e.Message, e);
        }

        public void Reject(string code, string message, Exception e)
        {
            if (_reject != null)
            {
                var errorData = e?.Data;
                var userInfo = errorData != null
                    ? JToken.FromObject(errorData) 
                    : null;
                _reject.Invoke(new JObject
                {
                    { "code", code ?? DefaultError },
                    { "message", message },
                    { "stack", e?.StackTrace },
                    { "userInfo", userInfo },
                });
            }
        }
    }
}
