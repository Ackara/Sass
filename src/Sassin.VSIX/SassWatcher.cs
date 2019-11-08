using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace Acklann.Sassin
{
    public class SassWatcher : IVsRunningDocTableEvents3
    {
        #region IVsRunningDocTableEvents3

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            throw new NotImplementedException();
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            throw new NotImplementedException();
        }

        public int OnAfterSave(uint docCookie)
        {
            throw new NotImplementedException();
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            throw new NotImplementedException();
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            throw new NotImplementedException();
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            throw new NotImplementedException();
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            throw new NotImplementedException();
        }

        public int OnBeforeSave(uint docCookie)
        {
            throw new NotImplementedException();
        }

        #endregion IVsRunningDocTableEvents3
    }
}