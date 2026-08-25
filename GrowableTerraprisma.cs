using System;
using Terraria.ModLoader;
using GrowableTerraprisma.Behaviors;

namespace GrowableTerraprisma
{
    public class GrowableTerraprisma : Mod
    {
        public override object Call(params object[] args)
        {
            if (args is null || args.Length == 0)
                throw new ArgumentException("GrowableTerraprisma: Call requires at least one argument.");

            if (args[0] is string cmd)
            {
                switch (cmd)
                {
                    case "RegisterBehavior":
                        if (args.Length < 2)
                            throw new ArgumentException("GrowableTerraprisma: RegisterBehavior requires an IUprismaBehavior argument.");
                        if (args[1] is not IUprismaBehavior behavior)
                            throw new ArgumentException($"GrowableTerraprisma: Expected IUprismaBehavior, got {args[1]?.GetType().Name ?? "null"}.");
                        UprismaBehaviorRegistry.Register(behavior);
                        return true;
                }
            }

            return null;
        }
    }
}