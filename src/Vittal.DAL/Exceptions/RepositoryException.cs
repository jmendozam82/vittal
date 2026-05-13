using System;

namespace Vittal.DAL.Exceptions;

/// <summary>
/// Excepción base del DAL. Envuelve errores de PostgreSQL/Dapper
/// en excepciones de dominio comprensibles para la capa BLL.
/// </summary>
public class RepositoryException : Exception
{
    public RepositoryException(string message) : base(message) { }
    public RepositoryException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// Se lanza cuando una operacion CREATE viola una restricción UNIQUE.
/// El BLL la captura y retorna un error de validación al usuario.
/// </summary>
public class DuplicateEntityException : RepositoryException
{
    public DuplicateEntityException(string message) : base(message) { }
    public DuplicateEntityException(string message, Exception inner) : base(message, inner) { }
}
