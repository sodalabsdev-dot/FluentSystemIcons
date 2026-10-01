using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Threading.Tasks;

namespace FluentSystemIcons.Gallery.Helpers
{
    public static class AnimationExtensions
    {

        public static Task BeginAsync(this Storyboard storyboard)
        {
            var tcs = new TaskCompletionSource<bool>();

            void onCompleted(object? sender, object e)
            {
                storyboard.Completed -= onCompleted;
                tcs.SetResult(true);
            }

            storyboard.Completed += onCompleted;
            storyboard.Begin();
            return tcs.Task;
        }
    }
}