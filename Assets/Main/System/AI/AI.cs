using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public partial class AI
{
    private readonly GameCore core;
    private WorldData World { get; }

    private StrategyActions StrategyActions => core.StrategyActions;
    private PersonalActions PersonalActions => core.PersonalActions;

    public AI(GameCore core)
    {
        this.core = core;
        World = core.World;
    }
}
