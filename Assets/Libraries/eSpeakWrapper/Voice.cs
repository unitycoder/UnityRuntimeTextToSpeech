using System;

namespace ESpeakWrapper
{
    public class Voice
    {
        public string Name;
        public string Languages;
        public int Priority;
        public string Identifier;

        public override string ToString()
        {
            return String.Format("Name: {0}, Languages: {1}, Identifier: {2}, Priority: {3}",
                this.Name,
                this.Languages,
                this.Identifier,
                this.Priority
                );
        }
    }

}
