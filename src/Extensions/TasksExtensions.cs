﻿using System;
using System.Threading.Tasks;

namespace GranSteL.DialogflowBalancer.Extensions
{
    public static class TasksExtensions
    {
        /// <summary>
        /// Fire-and-forget
        /// Позволяет не дожидаться завершения задачи.
        /// </summary>
        /// <param name="task"></param>
        public static void Forget(this Task task)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                }
            }).ConfigureAwait(false);
        }
    }
}