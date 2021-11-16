module GitHub.Types

type CommitListNestedObj = { message: string }
type CommitListOuterObj = { sha: string; commit: CommitListNestedObj }

type Repo = { default_branch: string }
type HasSha = { sha: string }
type BranchResp = { commit: HasSha }
type TagResp = { object: HasSha }
type PullRequestResp = { title: string; html_url: string }
