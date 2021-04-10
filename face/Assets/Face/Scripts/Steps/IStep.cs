public interface IStep
{
    bool IsFinished();
    void Update();
    void Finish();
    void Initialize();
    IStep Next();
    IStep SetNext(IStep next);
    FaceManager GetFaceManager();
    IStep SetFaceManager(FaceManager manager);
    
}



public abstract class StepBase : IStep
{

    protected FaceManager _manager;
    protected IStep _next;
    protected bool _isInited;
    

    protected StepBase(FaceManager manager)
    {
        SetFaceManager(manager);
    }

    public abstract void Finish();

    public FaceManager GetFaceManager()
    {
        return _manager;
    }

    public virtual IStep Next()
    {
        return _next;
    }

    public IStep SetFaceManager(FaceManager manager)
    {
        _manager = manager;
        return this;
    }

    public virtual IStep SetNext(IStep next)
    {
        this._next = next;
        return next;
    }

    public abstract bool IsFinished();

    public abstract void Update();

    

    protected virtual void Init()
    {

    }

    public void Initialize()
    {
        if (!_isInited)
        {
            Init();
            _isInited = true;
        }
    }
}