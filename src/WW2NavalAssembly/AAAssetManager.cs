using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WW2NavalAssembly
{
    class AAAssetManager :SingleInstance<AAAssetManager>
    {
        public override string Name { get; } = "AA Asset Manager";

    }
}
