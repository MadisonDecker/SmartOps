using System;
using System.Collections.Generic;
using System.Text;
using SmartManagement.Repo.Models;

namespace SmartOpsManagement.Bus
{
    public partial class SmartOpsBusinessLogic
    {
        private readonly SmartOpsContext _context;

        public SmartOpsBusinessLogic(SmartOpsContext context)
        {
            _context = context;
        }
    }
}
