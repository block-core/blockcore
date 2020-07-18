using System;
using System.Collections.Generic;
using System.Text;
using Blockcore.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blockcore.Features.Storage.Controllers
{
    [Authorize]
    [Route("api/data")]
    public class DataController : FeatureController
    {

    }
}
