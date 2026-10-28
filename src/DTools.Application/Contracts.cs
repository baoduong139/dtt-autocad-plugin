using System;

namespace DTools.Application
{
    /// <summary>
    /// Giao diện điều phối lệnh an toàn giữa giao diện người dùng và AutoCAD CommandStack
    /// </summary>
    public interface ICommandDispatcher
    {
        void SendCommand(string commandName);
        void ExecuteSync(Action action);
    }

    /// <summary>
    /// Giao diện ghi log chuẩn người dùng, lưu vào LocalAppData
    /// </summary>
    public interface ILoggerService
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception ex = null);
    }

    /// <summary>
    /// Giao diện quản lý phạm vi Undo an toàn
    /// </summary>
    public interface IUndoScope : IDisposable
    {
        void Commit();
        void Rollback();
    }
}
