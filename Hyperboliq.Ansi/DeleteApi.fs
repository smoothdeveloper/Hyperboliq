﻿namespace Hyperboliq

open System
open System.Linq.Expressions
open FSharp.Quotations
open Hyperboliq
open Hyperboliq.Types
open Hyperboliq.Domain.AST
open Hyperboliq.Domain.SqlGen
open Hyperboliq.Domain.ExpressionParts
open Hyperboliq.Domain.ExpressionVisitor

type FluentDeleteBase(expr : DeleteExpression) =
    member x.Expression with internal get() = expr

    member x.ToSqlExpression () = (x :> ISqlExpressionTransformable).ToSqlExpression ()
    interface ISqlExpressionTransformable with
        member x.ToSqlExpression () = SqlExpression.Delete(expr)

    member x.ToSql (dialect : ISqlDialect) = (x :> ISqlStatement).ToSql(dialect)
    interface ISqlStatement with
        member x.ToSql(dialect : ISqlDialect) = x.ToSqlExpression() |> SqlifyExpression dialect

type DeleteWhere internal (expr : DeleteExpression) =
    inherit FluentDeleteBase(expr)
    let New expr = DeleteWhere(expr)

    member x.And<'a>(predicate : Expression<Func<'a, bool>>) =
        { expr with Where = Some(AddOrCreateWhereAndClause expr.Where (LinqExpression(predicate)) [| TableReferenceFromType<'a> |]) }
        |> New
    member x.And<'a>([<ReflectedDefinition>] predicate : Quotations.Expr<'a -> bool>) =
        { expr with Where = Some(AddOrCreateWhereAndClause expr.Where (Quotation(predicate)) [| TableReferenceFromType<'a> |]) }
        |> New

    member x.And<'a, 'b>(predicate : Expression<Func<'a, 'b, bool>>) =
        { expr with Where = Some(AddOrCreateWhereAndClause expr.Where (LinqExpression(predicate)) [| TableReferenceFromType<'a>; TableReferenceFromType<'b> |]) }
        |> New
    member x.And<'a, 'b>([<ReflectedDefinition>] predicate : Quotations.Expr<'a -> 'b -> bool>) =
        { expr with Where = Some(AddOrCreateWhereAndClause expr.Where (Quotation(predicate)) [| TableReferenceFromType<'a>; TableReferenceFromType<'b> |]) }
        |> New


    member x.Or<'a>(predicate : Expression<Func<'a, bool>>) =
        { expr with Where = Some(AddOrCreateWhereOrClause expr.Where (LinqExpression(predicate)) [| TableReferenceFromType<'a> |]) }
        |> New
    member x.Or<'a>([<ReflectedDefinition>] predicate : Quotations.Expr<'a -> bool>) =
        { expr with Where = Some(AddOrCreateWhereOrClause expr.Where (Quotation(predicate)) [| TableReferenceFromType<'a> |]) }
        |> New

    member x.Or<'a, 'b>(predicate : Expression<Func<'a, 'b, bool>>) =
        { expr with Where = Some(AddOrCreateWhereOrClause expr.Where (LinqExpression(predicate)) [| TableReferenceFromType<'a>; TableReferenceFromType<'b> |]) }
        |> New
    member x.Or<'a, 'b>([<ReflectedDefinition>] predicate : Quotations.Expr<'a -> 'b -> bool>) =
        { expr with Where = Some(AddOrCreateWhereOrClause expr.Where (Quotation(predicate)) [| TableReferenceFromType<'a>; TableReferenceFromType<'b> |]) }
        |> New


type DeleteFrom<'a> internal () =
    inherit FluentDeleteBase({ 
                                From = { Tables = [ TableIdentifier<'a>() ]; Joins = [] }
                                Where = None 
                             })
    
    member x.Where<'a>(predicate : Expression<Func<'a, bool>>) = DeleteWhere(x.Expression).And(predicate)
    member x.Where<'a>([<ReflectedDefinition>] predicate : Quotations.Expr<'a -> bool>) = DeleteWhere(x.Expression).And(predicate)

    member x.Where<'a, 'b>(predicate : Expression<Func<'a, bool>>) = DeleteWhere(x.Expression).And(predicate)
    member x.Where<'a, 'b>([<ReflectedDefinition>] predicate : Quotations.Expr<'a -> 'b -> bool>) = DeleteWhere(x.Expression).And(predicate)

type Delete private () =
    static member From<'a>() = DeleteFrom<'a> ()
