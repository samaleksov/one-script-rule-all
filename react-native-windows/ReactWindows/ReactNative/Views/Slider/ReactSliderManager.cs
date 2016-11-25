using ReactNative.UIManager;
using ReactNative.UIManager.Annotations;
using System.Collections.Generic;
using Windows.UI.Xaml;

namespace ReactNative.Views.Slider
{
    /// <summary>
    /// A view manager responsible for rendering Slider.
    /// </summary>
    public class ReactSliderManager : BaseViewManager<Windows.UI.Xaml.Controls.Slider, ReactSliderShadowNode>
    {
        private const double Epsilon = 1e-4;
        private const double Undefined = double.NegativeInfinity;

        /// <summary>
        /// The name of the view manager.
        /// </summary>
        public override string Name
        {
            get
            {
                return "RCTSlider";
            }
        }

        /// <summary>
        /// The exported custom direct event types.
        /// </summary>
        public override IReadOnlyDictionary<string, object> ExportedCustomDirectEventTypeConstants
        {
            get
            {
                return new Dictionary<string, object>
                {
                    {
                        "topSlidingComplete",
                        new Dictionary<string, object>()
                        {
                            { "registrationName", "onSlidingComplete" },
                        }
                    },
                };
            }
        }

        /// <summary>
        /// Sets whether a slider is disabled.
        /// </summary>
        /// <param name="view">a slider view.</param>
        /// <param name="disabled">
        /// Set to <code>true</code> if the picker should be disabled,
        /// otherwise, set to <code>false</code>.
        /// </param>
        [ReactProp("disabled")]
        public void SetDisabled(Windows.UI.Xaml.Controls.Slider view, bool disabled)
        {
            view.IsEnabled = !disabled;
        }

        /// <summary>
        /// Sets to change slider minimum value.
        /// </summary>
        /// <param name="view">a slider view.</param>
        /// <param name="minimum">The minimum slider value.</param>
        [ReactProp("minimumValue")]
        public void SetMinimumValue(Windows.UI.Xaml.Controls.Slider view, double minimum)
        {
            view.Minimum = minimum;
        }

        /// <summary>
        /// Sets to change slider maximum value.
        /// </summary>
        /// <param name="view">a slider view.</param>
        /// <param name="maximum">The maximum slider value.</param>
        [ReactProp("maximumValue")]
        public void SetMaximumValue(Windows.UI.Xaml.Controls.Slider view, double maximum)
        {
            view.Maximum = maximum;
        }

        /// <summary>
        /// Sets slider value.
        /// </summary>
        /// <param name="view">The slider view.</param>
        /// <param name="value">Slider value.</param>
        [ReactProp(ViewProps.Value)]
        public void SetValue(Windows.UI.Xaml.Controls.Slider view, double value)
        {
            view.ValueChanged -= OnValueChange;
            view.Value = value;
            view.ValueChanged += OnValueChange;
        }

        /// <summary>
        /// Sets slider step.
        /// </summary>
        /// <param name="view">The slider view.</param>
        /// <param name="step">Slider step.</param>
        [ReactProp("step", DefaultDouble = Undefined)]
        public void SetStep(Windows.UI.Xaml.Controls.Slider view, double step)
        {
            if (step != Undefined)
            {
                if (step == 0)
                {
                    step = Epsilon;
                }

                view.StepFrequency = step;
            }
            else
            {
                view.StepFrequency = 1;
            }
        }

        /// <summary>
        /// This method should return the <see cref="ReactSliderShadowNode"/>
        /// which will be then used for measuring the position and size of the
        /// view. 
        /// </summary>
        /// <returns>The shadow node instance.</returns>
        public override ReactSliderShadowNode CreateShadowNodeInstance()
        {
            return new ReactSliderShadowNode();
        }

        /// <summary>
        /// Called when view is detached from view hierarchy and allows for 
        /// additional cleanup by the <see cref="IViewManager"/> subclass.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="view">The view.</param>
        public override void OnDropViewInstance(ThemedReactContext reactContext, Windows.UI.Xaml.Controls.Slider view)
        {
            base.OnDropViewInstance(reactContext, view);
            view.ValueChanged -= OnValueChange;
            view.PointerReleased -= OnPointerReleased;
            view.PointerCaptureLost -= OnPointerReleased;
        }

        /// <summary>
        /// Implement this method to receive optional extra data enqueued from
        /// the corresponding instance of <see cref="ReactShadowNode"/> in
        /// <see cref="ReactShadowNode.OnCollectExtraUpdates"/>.
        /// </summary>
        /// <param name="root">The root view.</param>
        /// <param name="extraData">The extra data.</param>
        public override void UpdateExtraData(Windows.UI.Xaml.Controls.Slider root, object extraData)
        {
        }

        /// <summary>
        /// Returns the view instance for <see cref="Slider"/>.
        /// </summary>
        /// <param name="reactContext"></param>
        /// <returns></returns>
        protected override Windows.UI.Xaml.Controls.Slider CreateViewInstance(ThemedReactContext reactContext)
        {
            return new Windows.UI.Xaml.Controls.Slider();
        }

        /// <summary>
        /// Subclasses can override this method to install custom event 
        /// emitters on the given view.
        /// </summary>
        /// <param name="reactContext">The React context.</param>
        /// <param name="view">The view instance.</param>
        protected override void AddEventEmitters(ThemedReactContext reactContext, Windows.UI.Xaml.Controls.Slider view)
        {
            base.AddEventEmitters(reactContext, view);
            view.ValueChanged += OnValueChange;
            view.PointerReleased += OnPointerReleased;
            view.PointerCaptureLost += OnPointerReleased;
        }

        private void OnValueChange(object sender, RoutedEventArgs e)
        {
            var slider = (Windows.UI.Xaml.Controls.Slider)sender;
            var reactContext = slider.GetReactContext();
            reactContext.GetNativeModule<UIManagerModule>()
                .EventDispatcher
                .DispatchEvent(
                    new ReactSliderChangeEvent(
                        slider.GetTag(),
                        slider.Value));
        }

        private void OnPointerReleased(object sender, RoutedEventArgs e)
        {
            var slider = (Windows.UI.Xaml.Controls.Slider)sender;
            var reactContext = slider.GetReactContext();
            reactContext.GetNativeModule<UIManagerModule>()
                .EventDispatcher
                .DispatchEvent(
                    new ReactSliderCompleteEvent(
                        slider.GetTag(),
                        slider.Value));
        }
    }
}
