using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pass {
    public class MenuList : Menu {
        public int selection = 0;

        public MenuList(string[] options) : base(options) {
            //
        }

        public override void onInput(int arg) {
            selection = arg;
        }
    }
}
