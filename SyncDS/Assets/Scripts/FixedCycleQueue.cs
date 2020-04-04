using System;
using System.Collections.Generic;

/*
 固定长度的GC友好的先进先出队列，仅支持有序序列！
  |------| <- m_tail
  |------|
  |------|
  |------|
  |------|
  |------|
  |------|  
  |------|
  |------| <- m_head
 
  m_head -> m_tail 
  
 */

public class FixedCycleQueue<T>
    where T : struct
{
    protected T[] m_queue = null;
    protected int m_capcity = 0;
    protected int m_head = -1, m_tail = -1;

    public int Count
    {
        get
        {
            if (m_head < 0)
            {
                return 0;
            }

            if (m_tail < m_head)
            {
                return m_tail + m_capcity - m_head + 1;
            }
            else
            {
                return m_tail - m_head + 1;
            }
        }
    }

    public bool IsEmpty
    {
        get
        {
            return m_head < 0; // 实际上m_head, m_tail都是-1
        }
    }

    public virtual T this[int _index]
    {
        get
        {
            if (_index < 0 || _index >= this.Count)
            {
                throw new System.Exception(string.Format("Bad Index! {0} / {1}", _index, this.Count));
            }
            return m_queue[(m_head + _index) % m_capcity];
        }
    }

    public FixedCycleQueue(int _capcity)
    {
        if (_capcity <= 0)
        {
            throw new System.Exception("capcity must larger then zero!");
        }
        m_capcity = _capcity;
        m_queue = new T[_capcity];
        m_head = -1;
        m_tail = -1;
    }

    public virtual void Clear()
    {
        m_head = -1;
        m_tail = -1;
    }

    protected int _Push()
    {
        if (this.IsEmpty)
        {
            m_head = 0;
            m_tail = 0;
        }
        else if (m_tail >= m_head)
        {
            if (m_tail + 1 == m_capcity)
            {
                if (m_head == 0) // full
                {
                    ++m_head;
                }
                m_tail = 0;
            }
            else
            {
                ++m_tail;
            }
        }
        else
        {
            if (m_tail + 1 == m_head) // full
            {
                m_head = (m_head + 1 == m_capcity) ? 0 : m_head + 1;
                ++m_tail;
            }
            else
            {
                ++m_tail;
            }
        }
        return m_tail;
    }

    public virtual void Push(T _item)
    {
        int index = this._Push();
        m_queue[index] = _item;
    }

    public virtual T Pop()
    {
        T _item = default(T);
        Pop(out _item);
        return _item;
    }

    public virtual void Pop(out T _item)
    {
        if (this.IsEmpty)
        {
            throw new System.Exception("Pop Queue is EMPTY!");
        }

        if (m_head == m_tail)
        {
            _item = m_queue[m_head];
            m_head = -1;
            m_tail = -1;
        }
        else
        {
            _item = m_queue[m_head];
            m_head = (m_head + 1 == m_capcity) ? 0 : m_head + 1;
        }
    }

    public virtual T Head()
    {
        if (this.IsEmpty)
        {
            throw new System.Exception("Queue is EMPTY!");
        }
        return m_queue[m_head];
    }

    public virtual T Tail()
    {
        if (this.IsEmpty)
        {
            throw new System.Exception("Queue is EMPTY!");
        }
        return m_queue[m_tail];
    }

}

public class PlayerOperateCycleQueue : FixedCycleQueue<PlayerOperate>
{
    public PlayerOperateCycleQueue(int capcity) :
        base(capcity)
    {
    }

    public void RemoveAllLessThen(int _seq)
    {
        if (this.IsEmpty)
        {
            return;
        }
        int index = -1;
        int l = m_head;
        int r = (m_tail < m_head) ? m_tail + m_capcity : m_tail;
        for (; l <= r; ++l)
        {
            if (m_queue[l % m_capcity].seq > _seq)
            {
                index = l;
                break;
            }
        }

        if (index < 0)
        {
            this.Clear();
        }
        else
        {
            m_head = (index % m_capcity);
        }
    }

}