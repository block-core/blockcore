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
            ModalService.OnShow += ShowModal;
            ModalService.OnClose += CloseModal;
        }

        public void ShowModal(string title, RenderFragment content)
        {
            Title = title;
            Content = content;
            IsVisible = true;

            StateHasChanged();
        }

        public void CloseModal()
        {
            IsVisible = false;
            Title = "";
            Content = null;

            StateHasChanged();
        }

        public void Dispose()
        {
            ModalService.OnShow -= ShowModal;
            ModalService.OnClose -= CloseModal;
        }
    }
}