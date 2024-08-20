﻿using System;
using System.Collections.Generic;
using System.Threading;
using NewLife.NoDb.IO;

namespace NewLife.NoDb.Collections
{
    /// <summary>内存循环队列</summary>
    /// <remarks>
    /// 单进程访问安全。
    /// </remarks>
    public class MemoryQueue<T> : MemoryCollection<T>, IReadOnlyCollection<T> where T : struct
    {
        #region 属性
        private Int32 _Count;
        /// <summary>当前元素个数</summary>
        public Int32 Count => _Count;

        private Int32 _ReadPosition;
        /// <summary>读取指针</summary>
        public Int32 ReadPosition => _ReadPosition;

        private Int32 _WritePosition;
        /// <summary>写入指针</summary>
        public Int32 WritePosition => _WritePosition;

        /// <summary>获取集合大小</summary>
        /// <returns></returns>
        protected override Int32 GetCount() => Count;
        #endregion

        #region 构造
        static MemoryQueue() => _HeadSize = 24;

        /// <summary>实例化一个内存队列</summary>
        /// <param name="mf">内存文件</param>
        /// <param name="offset">内存偏移</param>
        /// <param name="size">内存大小。为0时自动增长</param>
        /// <param name="init">是否初始化为空</param>
        public MemoryQueue(MemoryFile mf, Int64 offset = 0, Int64 size = 0, Boolean init = true) : base(mf, offset, size)
        {
            if (init)
                OnSave();
            else
                OnLoad();
        }
        #endregion

        #region 基本方法
        /// <summary>元素个数</summary>
        Int32 IReadOnlyCollection<T>.Count => Count;

        /// <summary>获取栈顶</summary>
        /// <returns></returns>
        public T Peek()
        {
            var n = Count;
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(Count));

            var p = ReadPosition;
            View.Read<T>(GetP(p), out var val);

            return val;
        }

        /// <summary>弹出队列</summary>
        /// <returns></returns>
        public T Dequeue()
        {
            var n = 0;
            do
            {
                n = Count;
                if (n <= 0) throw new ArgumentOutOfRangeException(nameof(Count));
            }
            while (Interlocked.CompareExchange(ref _Count, n - 1, n) != n);

            // 抢位置
            var p = 0;
            var p2 = 0;
            do
            {
                p = _ReadPosition;
                p2 = p + 1;
                if (p2 >= Capacity) p2 -= Capacity;
            } while (Interlocked.CompareExchange(ref _ReadPosition, p2, p) != p);

            View.Read<T>(GetP(p), out var val);

            // 定时保存
            Commit();

            return val;
        }

        /// <summary>进入队列</summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            var n = 0;
            do
            {
                n = Count;
                if (n >= Capacity) throw new InvalidOperationException("容量不足");
            }
            while (Interlocked.CompareExchange(ref _Count, n + 1, n) != n);

            // 抢位置
            var p = 0;
            var p2 = 0;
            do
            {
                p = _WritePosition;
                p2 = p + 1;
                if (p2 >= Capacity) p2 -= Capacity;
            } while (Interlocked.CompareExchange(ref _WritePosition, p2, p) != p);

            View.Write(GetP(p), ref item, _ItemSize);

            // 定时保存
            Commit();
        }

        /// <summary>枚举数</summary>
        /// <returns></returns>
        public override IEnumerator<T> GetEnumerator()
        {
            var n = Count;
            var p = ReadPosition;
            for (var i = 0L; i < n; i++)
            {
                View.Read<T>(GetP(p), out var val);
                yield return val;

                if (++p >= Capacity) p = 0;
            }
        }
        #endregion

        #region 定时保存
        /// <summary>定时保存数据到文件</summary>
        protected override void OnSave()
        {
            View.Write(0, _Count);
            View.Write(4, _ReadPosition);
            View.Write(8, _WritePosition);
        }

        private void OnLoad()
        {
            _Count = View.ReadInt32(0);
            _ReadPosition = View.ReadInt32(4);
            _WritePosition = View.ReadInt32(8);
        }
        #endregion
    }
}