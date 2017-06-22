using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.Composition;
using Windows.UI.Core;

namespace CompositionTetris
{
    class MainView : IFrameworkView
    {
        public void Initialize(CoreApplicationView applicationView)
        {
            _applicationView = applicationView;
            _applicationView.Activated += OnActivated;
        }

        private void OnActivated(CoreApplicationView applicationView, IActivatedEventArgs args)
        {
            _window.Activate();
        }

        public void SetWindow(CoreWindow window)
        {
            _window = window;
            _dispatcher = _window.Dispatcher;
        }

        public void Load(string entryPoint)
        {
        }

        public void Run()
        {
            _compositor = new Compositor();
            _target = _compositor.CreateTargetForCurrentView();
            _root = _compositor.CreateContainerVisual();

            _target.Root = _root;
            _root.RelativeSizeAdjustment = Vector2.One;

            // Undo scale
            var displayInfo = DisplayInformation.GetForCurrentView();
            var dpi = displayInfo.LogicalDpi;
            UpdateInverseScale(dpi);
            displayInfo.DpiChanged += (s, a) =>
            {
                UpdateInverseScale(s.LogicalDpi);
            };

            _game = new Game(_root);

            bool quit = false;
            while (!quit)
            {
                _dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

                quit = _game.Update();
            }
        }

        public void Uninitialize()
        {
            _applicationView = null;
            _window = null;

            _compositor.Dispose();
            _compositor = null;
            _target = null;
            _root = null;
        }

        private void UpdateInverseScale(float dpi)
        {
            var dpiScale = dpi / 96.0f;
            _root.Scale = new Vector3(1.0f / dpiScale, 1.0f / dpiScale, 1.0f);
        }

        private CoreApplicationView _applicationView;
        private CoreWindow _window;
        private CoreDispatcher _dispatcher;

        private Compositor _compositor;
        private CompositionTarget _target;
        private ContainerVisual _root;

        private Game _game;
    }

    class MainViewFactory : IFrameworkViewSource
    {
        public IFrameworkView CreateView()
        {
            return new MainView();
        }

        static int Main(string[] args)
        {
            var source = new MainViewFactory();
            CoreApplication.Run(source);
            return 0;
        }
    }

}
