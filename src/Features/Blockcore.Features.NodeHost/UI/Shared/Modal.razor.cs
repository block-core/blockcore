using System;
using Blockcore.UI.BlazorModal;
using Microsoft.AspNetCore.Components;

namespace BlazorModal
{
    public class ModalBase : ComponentBase, IDisposable
    {
        [Inject] ModalService ModalService { get; set; }

        protected bool IsVisible { get; set; }
        protected string Title { get; set; }
        protected RenderFragment Content { get; set; }

        protected override void OnInitialized()
        {
            this.ModalService.OnShow += ShowModal;
            this.ModalService.OnClose += CloseModal;
        }

        public void ShowModal(string title, RenderFragment content)
        {
            this.Title = title;
            this.Content = content;
            this.IsVisible = true;

            StateHasChanged();
        }

        public void CloseModal()
        {
            this.IsVisible = false;
            this.Title = "";
            this.Content = null;

            StateHasChanged();
        }

        public void Dispose()
        {
            this.ModalService.OnShow -= ShowModal;
            this.ModalService.OnClose -= CloseModal;
        }
    }
}