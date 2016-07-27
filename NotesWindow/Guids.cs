// Guids.cs
// MUST match guids.h
using System;

namespace SteveHall.NotesWindow
{
    static class GuidList
    {
        public const string guidNotesWindowPkgString = "b5f01db8-5402-408b-a5bd-ec1133d30da5";
        public const string guidNotesWindowCmdSetString = "17b1626a-f254-4998-901d-f813e0a2e6d7";
        public const string guidToolWindowPersistanceString = "b61fa3d7-4b57-408b-870f-e829fb821fa3";

        public static readonly Guid guidNotesWindowCmdSet = new Guid(guidNotesWindowCmdSetString);
    };
}