﻿using Newtonsoft.Json.Linq;
using System;

namespace ReactNative.Animated
{
    class MultiplicationAnimatedNode : ValueAnimatedNode
    {
        private readonly NativeAnimatedNodesManager _manager;
        private readonly int[] _inputNodes;

        public MultiplicationAnimatedNode(int tag, JObject config, NativeAnimatedNodesManager manager)
            : base(tag)
        {
            _manager = manager;
            _inputNodes = config.GetValue("input", StringComparison.Ordinal).ToObject<int[]>();
        }

        public override void Update()
        {
            Value = 1;
            foreach (var tag in _inputNodes)
            {
                var valueNode = _manager.GetNodeById(tag) as ValueAnimatedNode;
                if (valueNode == null)
                {
                    throw new InvalidOperationException(
                        "Illegal node ID set as an input for Animated.Add node.");
                }

                Value *= valueNode.Value;
            }
        }
    }
}
