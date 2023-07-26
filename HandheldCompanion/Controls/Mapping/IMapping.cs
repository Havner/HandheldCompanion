using HandheldCompanion.Actions;
using System.Windows.Controls;

namespace HandheldCompanion.Controls
{
    public class IMapping : UserControl
    {
        protected object Value;

        protected IActions Actions;

        public event DeletedEventHandler Deleted;
        public delegate void DeletedEventHandler(object sender);
        public event UpdatedEventHandler Updated;
        public delegate void UpdatedEventHandler(object sender, IActions action);

        protected void Update()
        {
            Updated?.Invoke(Value, Actions);
        }

        protected void Delete()
        {
            this.Actions = null;
            Deleted?.Invoke(Value);
        }

        protected void SetIActions(IActions actions)
        {
            this.Actions = actions;
        }
    }
}
