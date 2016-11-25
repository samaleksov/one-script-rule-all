﻿using System;

namespace ReactNative.Bridge
{
    /// <summary>
    /// An attribute for annotating methods in an
    /// <see cref="Bridge.INativeModule"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ReactMethodAttribute : Attribute
    {
    }
}
