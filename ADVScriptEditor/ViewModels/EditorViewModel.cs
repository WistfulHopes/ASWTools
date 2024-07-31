using System;

namespace ADVScriptEditor.ViewModels;

public class EditorViewModel : ViewModelBase
{
    public virtual void PrepareSave()
    {
        throw new NotImplementedException("Pure virtual function that should never be called!");
    }
}