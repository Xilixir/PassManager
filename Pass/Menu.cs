using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pass {
    public abstract class Menu {
        public string[] options { get; set; }

        public Menu(string[] options) {
            this.options = options;
        }

        public void showMenu(string message) {
            int index = 0;
            foreach (string s in options) {
                string str = index < 10 ? "  " : (index < 100 ? " " : "");
                
                Console.WriteLine(String.Format(" [{0}] " + str + "{1}", index, s));
                index++;
            }
            int i = -1;
            while (i == -1) {
                Console.WriteLine();
                Console.WriteLine(message);
                try {
                    i = int.Parse(Console.ReadLine());
                } catch (Exception ex) {
                    i = -1;
                }
            }
            onInput(i);
        }

        public abstract void onInput(int arg);
    }
}
