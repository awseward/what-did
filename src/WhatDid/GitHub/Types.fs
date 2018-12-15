module GitHub.Types

type CommitListNestedObj = { message: string }
type CommitListOuterObj = { sha: string; commit: CommitListNestedObj }

type HasSha = { sha: string }
type BranchResp = { commit: HasSha }
type TagResp = { object: HasSha }
