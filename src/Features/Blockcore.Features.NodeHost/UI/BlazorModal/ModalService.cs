using System;
using Microsoft.AspNetCore.Components;

namespace Blockcore.UI.BlazorModal
{
    public class ModalService
    {
        public event Action<string, RenderFragment> OnShow;

        public event Action OnClose;

        public  object Parameter { get; set; }

        public void Show(string title, Type contentType)
        {
            if (contentType.BaseType != typeof(ComponentBase))
            {
                throw new ArgumentException($"{contentType.FullName} must be a Blazor Component");
            }

            var content = new RenderFragment(x => { x.OpenComponent(1, contentType); x.CloseComponent(); });

            this.OnShow?.Invoke(title, content);
        }

        public void Show(string title, Type contentType, object _parameter)
        {
            this.Parameter =_parameter;
            this.Show(title, contentType);

        }


        public void Close()
        {
            this.OnClose?.Invoke();
        }
    }
}