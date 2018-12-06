module Types

type CommitSha = CommitSha of string
type BranchName = BranchName of string
type TagName = TagName of string

type RevisionId =
| Commit of CommitSha
| Branch of BranchName
| Tag of TagName

type UnambiguousRevisionId =
| UCommit of CommitSha
| UBranch of BranchName * CommitSha
| UTag of TagName * CommitSha
  with
    member this.TEMP_GetShaString () =
      let shorten (sha: string) = sha.Substring (0, 8)
      match this with
      | UCommit (CommitSha sha) -> shorten sha
      | UBranch (_, CommitSha sha) -> shorten sha
      | UTag (_, CommitSha sha) -> shorten sha

type Parts = {
  owner: string option
  repo: string option
  baseRev: string option
  headRev: string option
}
  with
    static member Empty = { owner = None; repo = None; baseRev = None; headRev = None }
