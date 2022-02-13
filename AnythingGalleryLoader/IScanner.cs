using System;
using System.Collections.Generic;
using System.Text;

namespace AnythingGalleryLoader
{
    public interface IScanner
    {
        void MUpdate();
        void ClearData();
        void StartNewQuery(string query);
        void StartNewElaborateQuery(string query);
    }
    public interface IImageScanner : IScanner
    {
        bool TryGetPainting(out string url, out string description);
    }
    public interface IInfoScanner : IScanner
    {
        bool TryGetInfoText(out string description, out string info);
    }
    public interface IRelatedScanner : IScanner
    {
        bool TryGetRelatedQuery(out string relatedSearch);
    }
    public interface IVideoScanner : IScanner
    {
        bool TryGetVideo(out string url, out string title);
    }
}
