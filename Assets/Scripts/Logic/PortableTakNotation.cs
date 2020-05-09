using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

/// <summary>
/// Implementation of the Portable Tak Notation defined by the US tak association (https://ustak.org/portable-tak-notation/).
/// Implementation allows boards of size 3 to 8.
/// 
/// Board setup:
///   Player 2
///   +-----+
/// 3 |# + #|
/// 2 |< # >|
/// 1 |# - #|
///   +-----+
///    A B C
///   Player 1
///  
/// </summary>
public static class PortableTakNotation
{ 
    public static TakLogic.TakMove GetMoveFromWord(string word)
    {
        string placementPatter = @"([FSC]?)\1(a-i)\2(3-8)\3";
        Regex placememntRegex = new Regex(placementPatter);

        throw new NotImplementedException();
    }

    ///// <summary>
    ///// Parse a single movement instruction
    ///// </summary>
    ///// <param name="ptn"></param>
    ///// <returns></returns>
    //public static TakLogic.PlaceStone CreatePlacmenteMoveFromString(string ptn)
    //{
    //    Debug.Assert(ptn.Length == 2 || ptn.Length == 3, "Placement notation should have 2 or 3 characters.");

    //    // Default stone type is a flat stone.
    //    TakLogic.StoneType type = TakLogic.StoneType.FlatStone;
    //    if (ptn.Length == 3)
    //    {
    //        type = PortableTakNotation.GetStoneTypeFromSymbol(ptn[0]);
    //        ptn = ptn.Substring(1);
    //    }

    //    Vector2Int pos = PortableTakNotation.GetPositionFromSquareName(ptn);
    //    return new TakLogic.PlaceStone(type, pos);
    //}

    /// <summary>
    /// Converts a stone type character from PTN to type enum.
    /// </summary>
    /// <param name="symbol">Char describing the stone type, valid: FSC</param>
    /// <returns>Stone type (flat, standing or capstone).</returns>
    public static TakLogic.StoneType GetStoneTypeFromSymbol(char symbol)
    {
        switch (symbol)
        {
            case 'F':
                return TakLogic.StoneType.FlatStone;
            case 'S':
                return TakLogic.StoneType.StandingStone;
            case 'C':
                return TakLogic.StoneType.Capstone;
            default:
                throw new InvalidPTNSyntax($"Invalid stone type symbol '{symbol}'");

        }
    }
    /// <summary>
    /// Converts a direction character from PTN to the direction enum.
    /// </summary>
    /// <param name="symbol">Char describing the direction, valid: <>+-</param>
    /// <returns>Enum of direction.</returns>
    public static TakLogic.Direction GetDirectionFromSymbol(char symbol)
    {
        switch (symbol)
        {
            case '<':
                return TakLogic.Direction.Left;
            case '>':
                return TakLogic.Direction.Right;
            case '+':
                return TakLogic.Direction.Up;
            case '-':
                return TakLogic.Direction.Down;
            default:
                throw new InvalidPTNSyntax($"Invalid direction symbol '{symbol}'");

        }
    }

    /// <summary>
    /// Converts a direction character from PTN to a 2D int vector.
    /// </summary>
    /// <param name="symbol">Char describing the direction, valid: <>+-</param>
    /// <returns>Vector describing direction along right/up axes.</returns>
    public static Vector2Int GetVectorDirectionFromSymbol(char symbol)
    {
        switch (symbol)
        {
            case '<':
                return Vector2Int.left;
            case '>':
                return Vector2Int.right;
            case '+':
                return Vector2Int.up;
            case '-':
                return -Vector2Int.down;
            default:
                throw new InvalidPTNSyntax($"Invalid direction symbol '{symbol}'");

        }
    }

    /// <summary>
    /// Converts a PTN square identifier into a 2D int vector.
    /// </summary>
    /// <param name="squareName">A square identifier, i.e. a letter and digit.</param>
    /// <returns>A vector of the position in right/up directions.</returns>
    public static Vector2Int GetPositionFromSquareName(string squareName)
    {
        if (squareName.Length != 2)
            throw new InvalidPTNSyntax($"Invalid square name '{squareName}'");
        if (!char.IsLower(squareName[0]))
            throw new InvalidPTNSyntax($"Invalid x position '{squareName[0]}'");
        if (!char.IsDigit(squareName[1]))
            throw new InvalidPTNSyntax($"Invalid y position '{squareName[1]}'");
        //Debug.Assert(symbol.Length == 2, "Positions are defined by 2 chars.");
        //Debug.Assert(Char.IsLetter(symbol[0]), "First character not a letter.");
        //Debug.Assert(Char.IsDigit (symbol[1]), "Second character not a digit.");

        return new Vector2Int(squareName[0]-'a', squareName[1]-'0');
    }

    public class InvalidPTNSyntax : Exception
    {
        public InvalidPTNSyntax() { }

        public InvalidPTNSyntax(string message)
            : base(message) { }

        public InvalidPTNSyntax(string message, Exception inner)
            : base(message, inner) { }
    }



}
