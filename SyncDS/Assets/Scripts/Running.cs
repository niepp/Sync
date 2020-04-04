using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum OperateType
{
    OT_None = 0,
    OT_StartRun,
    OT_StopRun,
}

public struct PlayerOperate
{
    public int seq;
    public OperateType optype;
    public bool isRun;
    public bool isRight;
    public void Reset()
    {
        optype = OperateType.OT_None;
        seq = 0;
        isRun = false;
        isRight = false;
    }
}

public class ClientPredict
{
    public Vector3 vel = Vector3.zero;

    private float predictLimit = 0.3f;
    private float curT = 0;

    public ClientPredict(float _limit)
    {
        predictLimit = _limit;
    }

    public Vector3 Predict(Vector3 _curPos, float _fixedDt)
    {
        if (curT < predictLimit)
        {
            curT += _fixedDt;
            _curPos += vel * _fixedDt;
        }
        return _curPos;
    }

    public void SetPredict(Vector3 _vel)
    {
        curT = 0;
        vel = _vel;
    }

}

public class Running : MonoBehaviour
{
    public float m_speed = 5.0f;
    bool m_run = false;
    bool m_right = true;
    CharacterController m_cc;

    int m_moveSeq = 0;

    ClientPredict m_clientPredict = new ClientPredict(0.3f);

    Sync m_syncHandle = null;

    PlayerOperate m_playerOperate = new PlayerOperate();

    public int MoveSeq
    {
        get { return m_moveSeq; }
    }

    public bool IsRun
    {
        get
        {
            return m_run;
        }
    }

    public Vector3 Vel
    {
        get
        {
            if (m_run)
            {
                return (m_right ? Vector3.right : Vector3.left) * m_speed;
            }
            return Vector3.zero;
        }
    }

    // Use this for initialization
    void Start()
    {
        m_cc = GetComponent<CharacterController>();
        m_syncHandle = GetComponent<Sync>();
    }

#if !DEDICATED_SERVER
    void OnGUI()
    {
        if (m_syncHandle.IsOwner)
        {
            bool pressL = false;
            bool pressR = false;
            if (GUI.RepeatButton(new Rect(0, Screen.height - 60, 80, 60), "RunL"))
            {
                StartRun(false);
                pressL = true;
            }

            if (GUI.RepeatButton(new Rect(80, Screen.height - 60, 80, 60), "RunR"))
            {
                StartRun(true);
                pressR = true;
            }

            if (!pressL && !pressR)
            {
                StopRun();
            }
        }
    }

    void FixedUpdate()
    {
        if (m_syncHandle.IsOwner)
        {
            MoveStep(Time.fixedDeltaTime);
            if (AppRoot.IsConnected)
            {
                m_playerOperate.seq = this.m_moveSeq;
                m_syncHandle.SendServerMove(transform.position, m_playerOperate);
            }
        }
        else
        {
            UpdateProxy(Time.fixedDeltaTime);
        }
    }
#endif

    public void StartRun(bool isRight)
    {
        m_run = true;
        m_right = isRight;

        m_playerOperate.optype = OperateType.OT_StartRun;
        m_playerOperate.isRun = true;
        m_playerOperate.isRight = isRight;
    }

    public void StopRun()
    {
        m_run = false;

        m_playerOperate.optype = OperateType.OT_StopRun;
        m_playerOperate.isRun = false;
    }

    public void MoveStep(float fixedDt)
    {
        ++m_moveSeq;
        if (m_run)
        {
            Vector3 motion = Vector3.zero;
            motion.x = (m_right ? m_speed : -m_speed) * fixedDt;
            m_cc.Move(motion);
        }
    }

    public void OnRecvNew(Vector3 newPos, Vector3 vel)
    {
        m_clientPredict.SetPredict(vel);
        Vector3 curPos = transform.position;
        transform.position = (newPos - curPos) * 0.75f + curPos;
    }

    void UpdateProxy(float fixedDt)
    {
        Vector3 pos = m_clientPredict.Predict(transform.position, fixedDt);
        transform.position = pos;
    }

    public void GetSnapShot(ref AdjustSnapShot snapShot)
    {
        snapShot.seq = this.m_moveSeq;
        snapShot.pos = this.transform.position;
        snapShot.isRun = this.m_run;
        snapShot.isRight = this.m_right;
        snapShot.speed = this.m_speed;
    }

    public void ResetByDS(int seq, Vector3 dsPos, bool isRun, bool isRight, float speed)
    {
        m_moveSeq = seq;
        transform.position = dsPos;
        m_run = isRun;
        m_right = isRight;
        m_speed = speed;
    }

    public void AdjustMove(PlayerOperateCycleQueue ownerOperateRecords)
    {
        PlayerOperate curOptRec = m_playerOperate;
        for (int i = 0; i < ownerOperateRecords.Count; ++i)
        {
            m_playerOperate.Reset();
            var opRec = ownerOperateRecords[i];
            switch (opRec.optype)
            {
                case OperateType.OT_None:
                    break;
                case OperateType.OT_StartRun:
                    this.StartRun(opRec.isRight);
                    break;
                case OperateType.OT_StopRun:
                    this.StopRun();
                    break;
                default:
                    break;
            }
            this.MoveStep(Time.fixedDeltaTime);
        }
        m_playerOperate = curOptRec;
    }

}
