using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NamedPath : DerivedPath
{
    public string Name;

    public override string UniqueName()
    {
        return name + " " + Name;
    }
}
