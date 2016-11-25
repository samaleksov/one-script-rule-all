﻿using System;

namespace ReactNative.UIManager.Annotations
{
    /// <summary>
    /// Annotates a group of properties of native views that should be exposed
    /// to JavaScript. It is a batched version of the 
    /// <see cref="ReactPropAttribute"/> annotation.
    /// </summary>
    /// <remarks>
    /// This annotation is meant to be used of similar properties. That is why
    /// it only supports a set of properties of the same type. A good example
    /// is supporting "border", where there are many variations of that
    /// property and each has very similar handling.
    /// 
    /// Each annotated method should return <see cref="void"/>.
    /// 
    /// In cases when the property has been removed from the corresponding 
    /// React component, the annotated setter will be called and a default
    /// value will be provided as a value parameter. Default values can be
    /// customized using, e.g., <see cref="ReactPropBaseAttribute.DefaultInt32"/>.
    /// In all other cases, <code>null</code> will be provided as a default.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReactPropGroupAttribute : ReactPropBaseAttribute
    {        
        /// <summary>
        /// Instantiates the <see cref="ReactPropGroupAttribute"/>.
        /// </summary>
        /// <param name="names">The property group names.</param>
        public ReactPropGroupAttribute(params string[] names)
        {
            Names = names;
        }

        /// <summary>
        /// The set of property group names.
        /// </summary>
        public string[] Names { get; }
    }
}
