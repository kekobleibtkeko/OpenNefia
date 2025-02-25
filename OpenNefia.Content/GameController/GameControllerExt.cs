﻿using OpenNefia.Core.GameController;
using OpenNefia.Core.Timing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNefia.Content.GameController
{
    public static class GameControllerExt
    {
        public static float StepFrame(this IGameController gameController)
        {
            var dt = Love.Timer.GetDelta();
            var frameArgs = new FrameEventArgs(dt);
            gameController.Update(frameArgs);
            gameController.Draw();
            // gameController.SystemStep();
            return dt;
        }

        public static void Wait(this IGameController gameController, float time)
        {
            var remaining = time;

            while (remaining > 0f)
            {
                var dt = StepFrame(gameController);
                remaining -= dt;
            }
        }
    }
}
