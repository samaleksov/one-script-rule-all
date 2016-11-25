﻿using Newtonsoft.Json.Linq;
using System;

namespace ReactNative.Animated
{
    class DiffClampAnimatedNode : ValueAnimatedNode
    {
        private readonly NativeAnimatedNodesManager _manager;
        private readonly int _inputNodeTag;
        private readonly double _min;
        private readonly double _max;

        private double _lastValue;

        public DiffClampAnimatedNode(int tag, JObject config, NativeAnimatedNodesManager manager) 
            : base(tag, config)
        {
            _manager = manager;
            _inputNodeTag = config.Value<int>("input");
            _min = config.Value<double>("min");
            _max = config.Value<double>("max");

            Value = _lastValue = GetInputNodeValue();
        }

        public override void Update()
        {
            var value = GetInputNodeValue();

            var diff = value - _lastValue;
            _lastValue = value;
            Value = Math.Min(Math.Max(Value + diff, _min), _max);
        }

        private double GetInputNodeValue()
        {
            var valueAnimatedNode = _manager.GetNodeById(_inputNodeTag) as ValueAnimatedNode;
            if (valueAnimatedNode == null) {
                throw new InvalidOperationException(
                    "Illegal node ID set as an input for Animated.DiffClamp node.");
            }

            return valueAnimatedNode.Value;
        }
    }
}
