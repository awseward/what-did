module Types

type CommitSha = CommitSha of string
type BranchName = BranchName of string
type TagName = TagName of string

type RevisionId =
| Commit of CommitSha
| Branch of BranchName
| Tag of TagName

type Parts = {
  owner: string option
  repo: string option
  baseRev: string option
  headRev: string option
}
  with
    static member Empty = { owner = None; repo = None; baseRev = None; headRev = None }
