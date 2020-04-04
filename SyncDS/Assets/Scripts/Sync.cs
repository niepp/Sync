using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public struct AdjustSnapShot
{
    public int seq;
    public Vector3 pos;
    public bool isRun;
    public bool isRight;
    public float speed;
}

public class Sync : NetworkBehaviour
{
    const float adjustDist = 0.2f;
    const int ownerRecordLimit = 180;
    const int adjustMaxFreq = 10;

    Running m_actorRun;
    Running ActorRun
    {
        get
        {
            if (m_actorRun == null)
            {
                m_actorRun = GetComponent<Running>();
            }
            return m_actorRun;
        }
    }

    NetworkIdentity m_networkId;
    NetworkIdentity NetworkId
    {
        get
        {
            if (m_networkId == null)
            {
                m_networkId = GetComponent<NetworkIdentity>();
            }
            return m_networkId;
        }
    }

    public bool IsOwner
    {
        get
        {
            return NetworkId.isLocalPlayer;
        }
    }

    PlayerOperateCycleQueue m_ownerOperateRecords = new PlayerOperateCycleQueue(ownerRecordLimit);
    int m_lastAdjustSeq = 0;

    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        Debug.LogWarning("[Var] OnSerialize");
        Vector3 pos = ActorRun.transform.position;
        Vector3 vel = ActorRun.Vel;
        writer.Write(pos);
        writer.Write(vel);
        return true;
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        Debug.LogWarning("[Var] OnDeserialize");
        Vector3 pos = Vector3.zero;
        Vector3 vel = Vector3.zero;
        pos = reader.ReadVector3();
        vel = reader.ReadVector3();
        if (!IsOwner)
        {
            ActorRun.OnRecvNew(pos, vel);
        }
    }

    public void SendServerMove(Vector3 curPos, PlayerOperate playerOperate)
    {
        m_ownerOperateRecords.Push(playerOperate);
        CmdServerMove(ActorRun.MoveSeq, curPos, playerOperate.isRun, playerOperate.isRight);
    }

    [Command]
    void CmdServerMove(int curSeq, Vector3 curPos, bool isRun, bool isRight)
    {
        int expectedSeq = ActorRun.MoveSeq + 1;
        if (curSeq < expectedSeq)
        {
            Debug.LogWarning(string.Format("[Seq Late] expectedSeq:{0}, receive:{1}", expectedSeq, curSeq));
        }
        else if (curSeq > expectedSeq)
        {
            Debug.LogWarning(string.Format("[Seq Lost] expectedSeq:{0}, receive:{1}", expectedSeq, curSeq));
        }

        if (isRun)
        {
            ActorRun.StartRun(isRight);
        }
        else
        {
            ActorRun.StopRun();
        }
        ActorRun.MoveStep(Time.fixedDeltaTime);
        CheckCheat(curPos);

        SetDirtyBit(0xffffffff);

    }

    void CheckCheat(Vector3 curPos)
    {
        Vector3 dsPos = ActorRun.transform.position;
        float sqrDist = Vector3.SqrMagnitude(curPos - dsPos);
        if (sqrDist >= adjustDist * adjustDist)
        {
            int curSeq = ActorRun.MoveSeq;
            if (curSeq - m_lastAdjustSeq >= adjustMaxFreq)
            {
                m_lastAdjustSeq = curSeq;
                Debug.LogWarning(string.Format("Client may have cheat! netView:{0}, seq:{1}, clientPos:({2}, {3}, {4}), dsPos:({5}, {6}, {7})"
                    , NetworkId.name, curSeq, curPos.x, curPos.y, curPos.z, dsPos.x, dsPos.y, dsPos.z));
                SendAdjustOwner();
            }
        }
    }

    void SendAdjustOwner()
    {
        AdjustSnapShot snapShot = new AdjustSnapShot();
        ActorRun.GetSnapShot(ref snapShot);
        RpcAdjustOwner(snapShot.seq, snapShot.pos, snapShot.isRun, snapShot.isRight, snapShot.speed);
    }

    [ClientRpc]
    void RpcAdjustOwner(int seq, Vector3 dsPos, bool isRun, bool isRight, float speed)
    {
        if (!IsOwner)
        {
            return;
        }
        Debug.LogWarning("AdjustOwner Player By DS at seq:" + seq);

        m_ownerOperateRecords.RemoveAllLessThen(seq);

        ActorRun.ResetByDS(seq, dsPos, isRun, isRight, speed);

        ActorRun.AdjustMove(m_ownerOperateRecords);

    }

}

