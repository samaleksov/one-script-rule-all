using ReactNative.Bridge;
using ReactNative.DevSupport;
using ReactNative.Modules.Core;
using ReactNative.UIManager;
using System;
using System.Collections.Generic;

namespace ReactNative
{
    /// <summary>
    /// This interface manages instances of <see cref="IReactInstance" />. 
    /// It exposes a way to configure React instances using 
    /// <see cref="IReactPackage"/> and keeps track of the lifecycle of that
    /// instance. It also sets up a connection between the instance and the
    /// developer support functionality of the framework.
    ///
    /// An instance of this manager is required to start the JavaScript 
    /// application in <see cref="ReactRootView"/>
    /// (<see cref="ReactRootView.StartReactApplication(IReactInstanceManager, string)"/>).
    ///
    /// The lifecycle of the instance of <see cref="IReactInstanceManager"/>
    /// should be bound to the application that owns the 
    /// <see cref="ReactRootView"/> that is used to render the React 
    /// application using this instance manager. It is required to pass
    /// lifecycle events to the instance manager (i.e., <see cref="OnSuspend"/>,
    /// <see cref="IAsyncDisposable.DisposeAsync"/>, and <see cref="OnResume(Action)"/>).
    /// </summary>
    public interface IReactInstanceManager : IAsyncDisposable
    {
        /// <summary>
        /// Event triggered when a React context has been initialized.
        /// </summary>
        event EventHandler<ReactContextInitializedEventArgs> ReactContextInitialized;

        /// <summary>
        /// The developer support manager for the instance.
        /// </summary>
        IDevSupportManager DevSupportManager { get; }

        /// <summary>
        /// Signals whether <see cref="CreateReactContextInBackground"/> has 
        /// been called. Will return <code>false</code> after 
        /// <see cref="IAsyncDisposable.DisposeAsync"/> until a new initial 
        /// context has been created.
        /// </summary>
        bool HasStartedCreatingInitialContext { get; }

        /// <summary>
        /// The URL where the last bundle was loaded from.
        /// </summary>
        string SourceUrl { get; }

        /// <summary>
        /// The current React context.
        /// </summary>
        ReactContext CurrentReactContext { get; }

        /// <summary>
        /// Trigger the React context initialization asynchronously in a 
        /// background task. This enables applications to pre-load the
        /// application JavaScript, and execute global core code before the
        /// <see cref="ReactRootView"/> is available and measure. This should
        /// only be called the first time the application is set up, which is
        /// enforced to keep developers from accidentally creating their
        /// applications multiple times.
        /// </summary>
        void CreateReactContextInBackground();

        /// <summary>
        /// Method that gives JavaScript the opportunity to consume the back
        /// button event. If JavaScript does not consume the event, the
        /// default back press action will be invoked at the end of the
        ///roundtrip to JavaScript.
        /// </summary>
        void OnBackPressed();

        /// <summary>
        /// Invoked when the application is suspended.
        /// </summary>
        void OnSuspend();

        /// <summary>
        /// Used when the application resumes to reset the back button handling
        /// in JavaScript.
        /// </summary>
        /// <param name="onBackPressed">
        /// The action to take when back is pressed.
        /// </param>
        void OnResume(Action onBackPressed);

        /// <summary>
        /// Attach given <paramref name="rootView"/> to a React instance
        /// manager and start the JavaScript application using the JavaScript
        /// module provided by the <see cref="ReactRootView.JavaScriptModuleName"/>. If
        /// the React context is currently being (re-)created, or if the react
        /// context has not been created yet, the JavaScript application
        /// associated with the provided root view will be started
        /// asynchronously. This view will then be tracked by this manager and
        /// in case of React instance restart, it will be re-attached.
        /// </summary>
        /// <param name="rootView">The root view.</param>
        void AttachMeasuredRootView(ReactRootView rootView);

        /// <summary>
        /// Detach given <paramref name="rootView"/> from the current react
        /// instance. This method is idempotent and can be called multiple
        /// times on the same <see cref="ReactRootView"/> instance.
        /// </summary>
        /// <param name="rootView">The root view.</param>
        void DetachRootView(ReactRootView rootView);

        /// <summary>
        /// Uses the configured <see cref="IReactPackage"/> instances to create
        /// all <see cref="IViewManager"/> instances.
        /// </summary>
        /// <param name="reactContext">
        /// The application context.
        /// </param>
        /// <returns>The list of view managers.</returns>
        IReadOnlyList<IViewManager> CreateAllViewManagers(ReactContext reactContext);
    }
}
