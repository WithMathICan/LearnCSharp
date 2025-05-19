using EFPractice01.Data;
using EFPractice01.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFPractice01 {
    internal class OperationId {

        internal async Task SaveWithOperationIdCheck(Guid operationId, CourseContext context) {
            var transaction = await context.Database.BeginTransactionAsync();
            try {
                context.Operations.Add(new Operation { OperationId = operationId });
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            } catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627)) {
                // Unique constraint violation (duplicate OperationId)
                Console.WriteLine("Duplicate operation detected.");
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Operation already processed.", ex);
            } catch (Exception ex) {
                // Other errors
                Console.WriteLine($"Error: {ex.Message}");
                await transaction.RollbackAsync();
                throw;
            }
        }

        internal async Task TestOperationId() {
            using var context = CourseContext.Create();
            var operationId = Guid.NewGuid();
            Instructor instructor = context.Instructors.First();
            instructor.Name += "!";
            context.Update(instructor);
            await SaveWithOperationIdCheck(operationId, context);
            await SaveWithOperationIdCheck(operationId, context);
        }
    }
}
