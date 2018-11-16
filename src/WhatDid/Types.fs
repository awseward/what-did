module Types

type Parts = {
  owner: string option
  repo: string option
  baseRev: string option
  headRev: string option
}
  with
    static member Empty = { owner = None; repo = None; baseRev = None; headRev = None }
