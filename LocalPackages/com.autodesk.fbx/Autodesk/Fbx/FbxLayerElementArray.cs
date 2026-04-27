namespace Autodesk.Fbx {

public class FbxLayerElementArray : global::System.IDisposable {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal FbxLayerElementArray(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(FbxLayerElementArray obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  ~FbxLayerElementArray() {
    Dispose();
  }

  public virtual void Dispose() {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          NativeMethods.delete_FbxLayerElementArray(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
      global::System.GC.SuppressFinalize(this);
    }
  }

  public FbxLayerElementArray(EFbxType pDataType) : this(NativeMethods.new_FbxLayerElementArray((int)pDataType), true) {
  }

  public int GetCount() {
    int ret = NativeMethods.FbxLayerElementArray_GetCount(swigCPtr);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public bool SetCount(int pCount, EFbxMemoryClearMode pInitializeMode) {
    bool ret = NativeMethods.FbxLayerElementArray_SetCount__SWIG_0(swigCPtr, pCount, (int)pInitializeMode);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public bool SetCount(int pCount) {
    bool ret = NativeMethods.FbxLayerElementArray_SetCount__SWIG_1(swigCPtr, pCount);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public int Add(int pItem) {
    int ret = NativeMethods.FbxLayerElementArray_Add__SWIG_1(swigCPtr, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public int Add(FbxColor pItem) {
    int ret = NativeMethods.FbxLayerElementArray_Add__SWIG_2(swigCPtr, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public int Add(FbxVector2 pItem) {
    int ret = NativeMethods.FbxLayerElementArray_Add__SWIG_3(swigCPtr, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public int Add(FbxVector4 pItem) {
    int ret = NativeMethods.FbxLayerElementArray_Add__SWIG_4(swigCPtr, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
    return ret;
  }

  public void SetAt(int pIndex, int pItem) {
    NativeMethods.FbxLayerElementArray_SetAt__SWIG_1(swigCPtr, pIndex, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
  }

  public void SetAt(int pIndex, FbxColor pItem) {
    NativeMethods.FbxLayerElementArray_SetAt__SWIG_2(swigCPtr, pIndex, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
  }

  public void SetAt(int pIndex, FbxVector2 pItem) {
    NativeMethods.FbxLayerElementArray_SetAt__SWIG_3(swigCPtr, pIndex, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
  }

  public void SetAt(int pIndex, FbxVector4 pItem) {
    NativeMethods.FbxLayerElementArray_SetAt__SWIG_4(swigCPtr, pIndex, pItem);
    if (NativeMethods.SWIGPendingException.Pending) throw NativeMethods.SWIGPendingException.Retrieve();
  }

}

}