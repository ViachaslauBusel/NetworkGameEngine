using NetworkGameEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class TestComponent : Component, IReactCommand<TestCommand>
{


    public TestComponent(int myIntValue)
    {

    }
    public override void Init()
    {
    }

    public override void Start()
    {
    }

    public override void Update()
    {
    }

    public void React(TestCommand command)
    {
    }

    public override void OnDestroy()
    {
    }

    

    //------------------------------------------------------

    public void ReactCommand(ref TestCommand command)
    {
    }
}

