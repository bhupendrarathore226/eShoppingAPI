using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Swagger.Configurations
{
    public class SwaggerConfig
    {
        public const string Name = "SwaggerConfig";

        public string BasePath { get; set; }

        public bool Enable { get; set; }
    }
}
