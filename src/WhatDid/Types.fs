module Types

type CommitSha = CommitSha of string
  with
    static member Shorten (sha: string) = sha.Substring (0, 8)
    static member GetShortSha (CommitSha sha) = CommitSha.Shorten sha
    member this.ShortSha with get () = CommitSha.GetShortSha this
    member this.FullSha
      with get () =
        let (CommitSha sha) = this
        sha
type BranchName = BranchName of string
type TagName = TagName of string

type Revision =
| Commit of CommitSha
| Branch of BranchName * CommitSha
| Tag of TagName * CommitSha
  with
    static member GetSha =
      function
      | Commit (CommitSha sha) -> sha
      | Branch (_, CommitSha sha) -> sha
      | Tag (_, CommitSha sha) -> sha
    static member GetShortSha = Revision.GetSha >> CommitSha.Shorten
    member this.ShortSha with get () = Revision.GetShortSha this
    member this.FullSha with get () = Revision.GetSha this

type RawParts = {
  owner: string option
  repo: string option
  baseRev: string option
  headRev: string option
}
  with
    static member Empty = { owner = None; repo = None; baseRev = None; headRev = None }

type FullParts = {
  owner: string
  repo: string
  baseRevision: Revision
  headRevision: Revision
}

module Temp =
  let (|Full|_|) (parts: RawParts) =
    match parts with
    | { owner = Some owner
        repo = Some repo
        baseRev = Some baseRev
        headRev = Some headRev } -> Some (owner, repo, baseRev, headRev)
    | _ -> None
  let failMissingPieces (parts: RawParts) =
    eprintfn "WARNING: Must have values for owner, repo, baseRev, headRev. %A" parts
    exn "FIXME"
