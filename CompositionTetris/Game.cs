using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;

namespace CompositionTetris
{
    class Game
    {
        public Game(ContainerVisual visual)
        {
            _compositor = visual.Compositor;
            _gameRoot = _compositor.CreateContainerVisual();
            _contentRoot = _compositor.CreateContainerVisual();
            _background = _compositor.CreateSpriteVisual();

            _gameRoot.RelativeSizeAdjustment = Vector2.One;
            _contentRoot.RelativeSizeAdjustment = Vector2.One;
            _background.RelativeSizeAdjustment = Vector2.One;
            _background.Brush = _compositor.CreateColorBrush(Colors.Black);

            visual.Children.InsertAtTop(_gameRoot);
            _gameRoot.Children.InsertAtBottom(_background);
            _gameRoot.Children.InsertAtTop(_contentRoot);

            _timer = new StepTimer();
        }

        public bool Update()
        {
            bool quit = false;

            _timer.Tick(() =>
            {
                var elapsedSeconds = _timer.GetElapsedSeconds();

                quit = UpdateInternal(elapsedSeconds);
            });

            return quit;
        }

        private bool UpdateInternal(double elapsedSeconds)
        {
            return false;
        }

        private Compositor _compositor;
        private ContainerVisual _gameRoot;
        private ContainerVisual _contentRoot;
        private SpriteVisual _background;

        private StepTimer _timer;
    }
}
