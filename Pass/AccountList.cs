using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pass {
    public class AccountList : Menu {
        public int selection = 0;
        public string input = "";

        public AccountList() : base(new string[] { "Edit Email", "Edit Username", "Edit Password", "Regenerate Password", "Delete Account" }) {
            //
        }

        public override void onInput(int arg) {
            if (arg == 0) {
                Console.WriteLine(" Enter a new email for the account:");
                input = Console.ReadLine();
            } else if (arg == 1) {
                Console.WriteLine(" Enter a new username for the account:");
                input = Console.ReadLine();
            } else if (arg == 2) {
                Console.WriteLine(" Enter a new password for the account:");
                input = Console.ReadLine();
            }
            selection = arg;
        }
    }
}
